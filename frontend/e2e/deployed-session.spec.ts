/**
 * E2E tests for the deployed Planning Poker app.
 *
 * Run: npx playwright test e2e/deployed-session.spec.ts
 * Override URL: BASE_URL=https://other-url.io npx playwright test e2e/deployed-session.spec.ts
 *
 * Note: "return to lobby" after End Session requires the backend to send SessionEnded
 * via SignalR. If the deployed backend hub URL or CORS is not set correctly, that step may timeout.
 */
import { test, expect } from '@playwright/test';

/** Build API URL. Uses API_URL if set, otherwise BASE_URL + /api (same-origin). */
function apiUrl(path: string): string {
  const base = process.env.API_URL ?? process.env.BASE_URL ?? 'https://ncsplanningpoker-frontend.gentleocean-1639b4e1.centralus.azurecontainerapps.io';
  const apiBase = base.includes('/api') ? base.replace(/\/api\/?$/, '') : base;
  return `${apiBase.replace(/\/$/, '')}/api${path.startsWith('/') ? path : `/${path}`}`;
}

test('10 participants join a session via API, then all leave via UI', async ({ browser, request }) => {
  test.setTimeout(120000);
  const baseURL = process.env.BASE_URL ?? 'https://ncsplanningpoker-frontend.gentleocean-1639b4e1.centralus.azurecontainerapps.io';

  // --- Create session and add 10 participants via API (same origin as frontend). ---
  // Note: If your API is behind multiple instances without shared storage, create/join may hit
  // different instances and the test can fail with "Session not found". Use single instance or sticky sessions.
  const createRes = await request.post(apiUrl('/sessions'), {
    data: { sessionName: 'E2E 10 Players', hostName: 'Host' },
  });
  expect(createRes.ok()).toBeTruthy();
  const createBody = await createRes.json();
  const pin = String(createBody.pin ?? createBody.PIN ?? '').trim();
  expect(pin.length).toBeGreaterThanOrEqual(6);
  const hostParticipant = createBody.participants?.[0] ?? createBody.Participants?.[0];
  const participants: { participantId: number; name: string; isHost: boolean }[] = [
    {
      participantId: hostParticipant?.participantId ?? hostParticipant?.ParticipantId,
      name: 'Host',
      isHost: true,
    },
  ];

  for (let i = 1; i < 10; i++) {
    const joinRes = await request.post(apiUrl(`/sessions/${pin}/participants`), {
      data: { name: `Player ${i}` },
    });
    if (!joinRes.ok()) {
      const body = await joinRes.text();
      throw new Error(`Join failed (${joinRes.status()}): ${body}`);
    }
    const joinBody = await joinRes.json();
    participants.push({
      participantId: joinBody.participantId ?? joinBody.ParticipantId,
      name: joinBody.name ?? joinBody.Name ?? `Player ${i}`,
      isHost: false,
    });
  }

  // --- Open 10 browser contexts, each loading the session page ---
  const contexts = await Promise.all(Array.from({ length: 10 }, () => browser.newContext({ baseURL })));
  const pages = await Promise.all(contexts.map((c) => c.newPage()));

  await Promise.all(
    pages.map((page, i) => {
      const { participantId, name } = participants[i];
      return page.goto(`/session/${pin}?participantId=${participantId}&name=${encodeURIComponent(name)}`);
    })
  );

  await Promise.all(
    pages.map((page) => expect(page.getByTestId('voting-room')).toBeVisible({ timeout: 20000 }))
  );

  const nonHostPages = pages.filter((_, i) => !participants[i].isHost);
  const hostPage = pages.find((_, i) => participants[i].isHost)!;

  for (const page of nonHostPages) {
    await page.getByTestId('leave-game-btn').click();
    await expect(page.getByTestId('modal-leave')).toBeVisible({ timeout: 5000 });
    await page.getByTestId('modal-leave-confirm').click();
    await expect(page.getByTestId('home')).toBeVisible({ timeout: 15000 });
  }

  await hostPage.getByTestId('voting-end-session-btn').click();
  await expect(hostPage.getByTestId('modal-end-session')).toBeVisible({ timeout: 5000 });
  await hostPage.getByTestId('modal-end-session-confirm').click();
  await expect(hostPage.getByTestId('modal-end-session')).not.toBeVisible({ timeout: 10000 });

  await Promise.all(contexts.map((c) => c.close()));
});

test('10 participants join at same time then exit', async ({ browser }) => {
  test.setTimeout(180000);
  const baseURL = process.env.BASE_URL ?? 'https://ncsplanningpoker-frontend.gentleocean-1639b4e1.centralus.azurecontainerapps.io';

  // --- Host: create session and go to table ---
  const hostContext = await browser.newContext({ baseURL });
  const hostPage = await hostContext.newPage();
  await hostPage.goto('/');
  await hostPage.getByTestId('home-create-table').click();
  await hostPage.getByTestId('session-name-input').fill('E2E 10 Join Same Time');
  await hostPage.getByTestId('host-name-input').fill('Host');
  await hostPage.getByTestId('session-creator-open-table').click();
  await expect(hostPage.getByTestId('session-creator-pin-section')).toBeVisible({ timeout: 15000 });
  const pin = (await hostPage.getByTestId('session-pin-display').textContent())?.trim();
  expect(pin).toBeTruthy();
  await hostPage.getByTestId('session-go-to-table').click();
  await expect(hostPage.getByTestId('voting-room')).toBeVisible({ timeout: 30000 });

  // --- 9 participants: all navigate to join page, then join at the same time (parallel) ---
  const joinContexts = await Promise.all(Array.from({ length: 9 }, () => browser.newContext({ baseURL })));
  const joinPages = await Promise.all(joinContexts.map((c) => c.newPage()));

  await Promise.all(joinPages.map((page) => page.goto(`/?pin=${pin}`)));

  await Promise.all(
    joinPages.map((page, i) =>
      expect(page.getByTestId('pin-entry')).toBeVisible({ timeout: 10000 })
    )
  );

  // Fill PIN and name on all 9 pages, then click join on all at once
  await Promise.all(
    joinPages.map((page, i) =>
      (async () => {
        await page.getByTestId('pin-input').fill(pin!);
        await page.getByTestId('pin-entry-name-input').fill(`Player ${i + 1}`);
      })()
    )
  );
  await Promise.all(joinPages.map((page) => page.getByTestId('pin-entry-join-btn').click()));

  // Wait for all 9 to land in the voting room
  await Promise.all(
    joinPages.map((page) => expect(page.getByTestId('voting-room')).toBeVisible({ timeout: 35000 }))
  );

  // --- All exit: 9 leave game, then host ends session ---
  for (const page of joinPages) {
    await page.getByTestId('leave-game-btn').click();
    await expect(page.getByTestId('modal-leave')).toBeVisible({ timeout: 5000 });
    await page.getByTestId('modal-leave-confirm').click();
    await expect(page.getByTestId('home')).toBeVisible({ timeout: 15000 });
  }

  await hostPage.getByTestId('voting-end-session-btn').click();
  await expect(hostPage.getByTestId('modal-end-session')).toBeVisible({ timeout: 5000 });
  await hostPage.getByTestId('modal-end-session-confirm').click();
  await expect(hostPage.getByTestId('modal-end-session')).not.toBeVisible({ timeout: 10000 });

  await hostContext.close();
  await Promise.all(joinContexts.map((c) => c.close()));
});

test('12 participants: host, 4 join, host goes host-only, 7 more join, wait 5s, all leave', async ({ browser }) => {
  test.setTimeout(240000);
  const baseURL = process.env.BASE_URL ?? 'https://ncsplanningpoker-frontend.gentleocean-1639b4e1.centralus.azurecontainerapps.io';

  // --- 1st player (host): create session and go to table ---
  const hostContext = await browser.newContext({ baseURL });
  const hostPage = await hostContext.newPage();
  await hostPage.goto('/');
  await hostPage.getByTestId('home-create-table').click();
  await hostPage.getByTestId('session-name-input').fill('E2E 12 Players Host Mode');
  await hostPage.getByTestId('host-name-input').fill('Host');
  await hostPage.getByTestId('session-creator-open-table').click();
  await expect(hostPage.getByTestId('session-creator-pin-section')).toBeVisible({ timeout: 15000 });
  const pin = (await hostPage.getByTestId('session-pin-display').textContent())?.trim();
  expect(pin).toBeTruthy();
  await hostPage.getByTestId('session-go-to-table').click();
  await expect(hostPage.getByTestId('voting-room')).toBeVisible({ timeout: 30000 });

  // --- 4 join ---
  const firstJoinContexts = await Promise.all(Array.from({ length: 4 }, () => browser.newContext({ baseURL })));
  const firstJoinPages = await Promise.all(firstJoinContexts.map((c) => c.newPage()));
  for (let i = 0; i < 4; i++) {
    const page = firstJoinPages[i];
    await page.goto(`/?pin=${pin}`);
    await expect(page.getByTestId('pin-entry')).toBeVisible({ timeout: 10000 });
    await page.getByTestId('pin-input').fill(pin!);
    await page.getByTestId('pin-entry-name-input').fill(`Player ${i + 1}`);
    await page.getByTestId('pin-entry-join-btn').click();
    await expect(page.getByTestId('voting-room')).toBeVisible({ timeout: 25000 });
  }

  // --- Host goes into Host Only Mode ---
  await hostPage.getByTestId('voting-host-mode-btn').click();
  await expect(hostPage.getByTestId('voting-host-mode-btn')).toContainText('Enter Game', { timeout: 5000 });

  // --- Remaining 7 join ---
  const secondJoinContexts = await Promise.all(Array.from({ length: 7 }, () => browser.newContext({ baseURL })));
  const secondJoinPages = await Promise.all(secondJoinContexts.map((c) => c.newPage()));
  for (let i = 0; i < 7; i++) {
    const page = secondJoinPages[i];
    await page.goto(`/?pin=${pin}`);
    await expect(page.getByTestId('pin-entry')).toBeVisible({ timeout: 10000 });
    await page.getByTestId('pin-input').fill(pin!);
    await page.getByTestId('pin-entry-name-input').fill(`Player ${i + 5}`);
    await page.getByTestId('pin-entry-join-btn').click();
    await expect(page.getByTestId('voting-room')).toBeVisible({ timeout: 25000 });
  }

  // --- Wait about 5 seconds ---
  await new Promise((r) => setTimeout(r, 5000));

  // --- All leave: 11 non-hosts leave, then host ends session ---
  const allNonHostPages = [...firstJoinPages, ...secondJoinPages];
  for (const page of allNonHostPages) {
    await page.getByTestId('leave-game-btn').click();
    await expect(page.getByTestId('modal-leave')).toBeVisible({ timeout: 5000 });
    await page.getByTestId('modal-leave-confirm').click();
    await expect(page.getByTestId('home')).toBeVisible({ timeout: 15000 });
  }
  await hostPage.getByTestId('voting-end-session-btn').click();
  await expect(hostPage.getByTestId('modal-end-session')).toBeVisible({ timeout: 5000 });
  await hostPage.getByTestId('modal-end-session-confirm').click();
  await expect(hostPage.getByTestId('modal-end-session')).not.toBeVisible({ timeout: 10000 });

  await hostContext.close();
  await Promise.all(firstJoinContexts.map((c) => c.close()));
  await Promise.all(secondJoinContexts.map((c) => c.close()));
});

test('create session, play one round (vote + reveal), then end session', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByTestId('home')).toBeVisible();
  await page.getByTestId('home-create-table').click();

  await expect(page.getByTestId('session-creator')).toBeVisible();
  await page.getByTestId('session-name-input').fill('E2E One Round');
  await page.getByTestId('host-name-input').fill('E2E Host');
  await page.getByTestId('session-creator-open-table').click();

  await expect(page.getByTestId('session-creator-pin-section')).toBeVisible({ timeout: 15000 });
  await page.getByTestId('session-go-to-table').click();

  // In voting room: wait for cards and host controls
  await expect(page.getByTestId('voting-room')).toBeVisible({ timeout: 15000 });
  await expect(page.getByTestId('reveal-votes-btn')).toBeVisible();

  // Place a vote by selecting a card (e.g. 5)
  await page.getByTestId('card-5').click();

  // Reveal votes (host action)
  await page.getByTestId('reveal-votes-btn').click();

  // After reveal, results view appears with END SESSION
  await expect(page.getByTestId('results-display')).toBeVisible({ timeout: 10000 });
  await expect(page.getByTestId('results-end-session-btn')).toBeVisible();
  await page.getByTestId('results-end-session-btn').click();

  // Confirm end in modal
  await expect(page.getByTestId('modal-end-session')).toBeVisible();
  await page.getByTestId('modal-end-session-confirm').click();
  await expect(page.getByTestId('modal-end-session')).not.toBeVisible({ timeout: 10000 });
});

test('create session, join, select card, reveal, pause 3s, then leave session', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByTestId('home')).toBeVisible();
  await page.getByTestId('home-create-table').click();

  await expect(page.getByTestId('session-creator')).toBeVisible();
  await page.getByTestId('session-name-input').fill('E2E Reveal Then Leave');
  await page.getByTestId('host-name-input').fill('E2E Host');
  await page.getByTestId('session-creator-open-table').click();

  await expect(page.getByTestId('session-creator-pin-section')).toBeVisible({ timeout: 15000 });
  await page.getByTestId('session-go-to-table').click();

  await expect(page.getByTestId('voting-room')).toBeVisible({ timeout: 15000 });
  await expect(page.getByTestId('reveal-votes-btn')).toBeVisible();

  await page.getByTestId('card-5').click();
  await page.getByTestId('reveal-votes-btn').click();

  await expect(page.getByTestId('results-display')).toBeVisible({ timeout: 10000 });
  await page.waitForTimeout(3000);

  await page.getByTestId('results-end-session-btn').click();
  await expect(page.getByTestId('modal-end-session')).toBeVisible({ timeout: 5000 });
  await page.getByTestId('modal-end-session-confirm').click();
  await expect(page.getByTestId('modal-end-session')).not.toBeVisible({ timeout: 10000 });
  await expect(page.getByTestId('home')).toBeVisible({ timeout: 15000 });
});

test('host game, wait for human to join from another computer and pick a card, then reveal, pause 3s, end session', async ({ browser }) => {
  test.setTimeout(300000); // 5 min: time for you to join from another device and pick a card
  const baseURL = process.env.BASE_URL ?? 'https://ncsplanningpoker-frontend.gentleocean-1639b4e1.centralus.azurecontainerapps.io';

  // --- Host: create session and go to table ---
  const hostContext = await browser.newContext({ baseURL });
  const hostPage = await hostContext.newPage();
  await hostPage.goto('/');
  await hostPage.getByTestId('home-create-table').click();
  await hostPage.getByTestId('session-name-input').fill('E2E Human Second Player');
  await hostPage.getByTestId('host-name-input').fill('Host');
  await hostPage.getByTestId('session-creator-open-table').click();
  await expect(hostPage.getByTestId('session-creator-pin-section')).toBeVisible({ timeout: 15000 });
  const pin = (await hostPage.getByTestId('session-pin-display').textContent())?.trim();
  expect(pin).toBeTruthy();
  await hostPage.getByTestId('session-go-to-table').click();
  await expect(hostPage.getByTestId('voting-room')).toBeVisible({ timeout: 30000 });

  // --- Log join info so you can join from another computer ---
  const joinUrl = `${baseURL.replace(/\/$/, '')}/?pin=${pin}`;
  console.log('\n--- Join from your other computer ---');
  console.log('PIN:', pin);
  console.log('URL:', joinUrl);
  console.log('------------------------------------\n');

  // --- Wait for you to join (Players at Table (2)) ---
  await expect(hostPage.getByTestId('voting-participants-section')).toContainText('(2)', { timeout: 240000 });

  // --- Wait for you to pick a card (your icon shows voted / READY) ---
  await hostPage.locator('.participant-item.has-voted').first().waitFor({ state: 'visible', timeout: 120000 });

  // --- Host: choose a card and reveal all ---
  await hostPage.getByTestId('card-8').click();
  await hostPage.getByTestId('reveal-votes-btn').click();

  await expect(hostPage.getByTestId('results-display')).toBeVisible({ timeout: 10000 });
  await hostPage.waitForTimeout(3000);

  await hostPage.getByTestId('results-end-session-btn').click();
  await expect(hostPage.getByTestId('modal-end-session')).toBeVisible({ timeout: 5000 });
  await hostPage.getByTestId('modal-end-session-confirm').click();
  await expect(hostPage.getByTestId('modal-end-session')).not.toBeVisible({ timeout: 10000 });
  await expect(hostPage.getByTestId('home')).toBeVisible({ timeout: 15000 });

  await hostContext.close();
});

test('host game, automated player joins with PIN, wait for human to join and pick a card, then both automated players pick a card, reveal, pause 3s, end session', async ({ browser }) => {
  test.setTimeout(300000); // 5 min: time for you to join from another device and pick a card
  const baseURL = process.env.BASE_URL ?? 'https://ncsplanningpoker-frontend.gentleocean-1639b4e1.centralus.azurecontainerapps.io';

  // --- Host: create session and go to table ---
  const hostContext = await browser.newContext({ baseURL });
  const hostPage = await hostContext.newPage();
  await hostPage.goto('/');
  await hostPage.getByTestId('home-create-table').click();
  await hostPage.getByTestId('session-name-input').fill('E2E Host + Auto + Human');
  await hostPage.getByTestId('host-name-input').fill('Host');
  await hostPage.getByTestId('session-creator-open-table').click();
  await expect(hostPage.getByTestId('session-creator-pin-section')).toBeVisible({ timeout: 15000 });
  const pin = (await hostPage.getByTestId('session-pin-display').textContent())?.trim();
  expect(pin).toBeTruthy();
  await hostPage.getByTestId('session-go-to-table').click();
  await expect(hostPage.getByTestId('voting-room')).toBeVisible({ timeout: 30000 });

  // --- Second automated player: join with PIN (another browser tab) ---
  const playerContext = await browser.newContext({ baseURL });
  const playerPage = await playerContext.newPage();
  await playerPage.goto(`/?pin=${pin}`);
  await expect(playerPage.getByTestId('pin-entry')).toBeVisible({ timeout: 10000 });
  await playerPage.getByTestId('pin-input').fill(pin!);
  await playerPage.getByTestId('pin-entry-name-input').fill('AutoPlayer');
  await playerPage.getByTestId('pin-entry-join-btn').click();
  await expect(playerPage.getByTestId('voting-room')).toBeVisible({ timeout: 25000 });

  // --- Log join info so you can join from your computer ---
  const joinUrl = `${baseURL.replace(/\/$/, '')}/?pin=${pin}`;
  console.log('\n--- Join from your other computer ---');
  console.log('PIN:', pin);
  console.log('URL:', joinUrl);
  console.log('------------------------------------\n');

  // --- Wait for you to join (Players at Table (3)) ---
  await expect(hostPage.getByTestId('voting-participants-section')).toContainText('(3)', { timeout: 240000 });

  // --- Wait for you to pick a card (your icon shows voted / READY) ---
  await hostPage.locator('.participant-item.has-voted').first().waitFor({ state: 'visible', timeout: 120000 });

  // --- Both automated players pick a card ---
  await hostPage.getByTestId('card-8').click();
  await playerPage.getByTestId('card-5').click();

  // --- Host: reveal all cards ---
  await hostPage.getByTestId('reveal-votes-btn').click();

  await expect(hostPage.getByTestId('results-display')).toBeVisible({ timeout: 10000 });
  await hostPage.waitForTimeout(3000);

  await hostPage.getByTestId('results-end-session-btn').click();
  await expect(hostPage.getByTestId('modal-end-session')).toBeVisible({ timeout: 5000 });
  await hostPage.getByTestId('modal-end-session-confirm').click();
  await expect(hostPage.getByTestId('modal-end-session')).not.toBeVisible({ timeout: 10000 });
  await expect(hostPage.getByTestId('home')).toBeVisible({ timeout: 15000 });

  await hostContext.close();
  await playerContext.close();
});

test('create session, get PIN, join table, end session and confirm', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByTestId('home')).toBeVisible();
  await page.getByTestId('home-create-table').click();

  await expect(page.getByTestId('session-creator')).toBeVisible();
  await page.getByTestId('session-name-input').fill('E2E Test Table');
  await page.getByTestId('host-name-input').fill('E2E Host');
  await page.getByTestId('session-creator-open-table').click();

  await expect(page.getByTestId('session-creator-pin-section')).toBeVisible({ timeout: 15000 });
  await expect(page.getByTestId('session-pin-display')).not.toBeEmpty();
  await page.getByTestId('session-go-to-table').click();

  await expect(page.getByTestId('voting-end-session-btn')).toBeVisible({ timeout: 15000 });
  await page.getByTestId('voting-end-session-btn').click();

  await expect(page.getByTestId('modal-end-session')).toBeVisible();
  await page.getByTestId('modal-end-session-confirm').click();

  // Modal closes after API call; navigation to lobby depends on SignalR SessionEnded
  await expect(page.getByTestId('modal-end-session')).not.toBeVisible({ timeout: 10000 });
});

test('create session, join, end session and return to lobby', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByTestId('home')).toBeVisible();
  await page.getByTestId('home-create-table').click();

  await expect(page.getByTestId('session-creator')).toBeVisible();
  await page.getByTestId('session-name-input').fill('E2E Lobby Return');
  await page.getByTestId('host-name-input').fill('E2E Host');
  await page.getByTestId('session-creator-open-table').click();

  await expect(page.getByTestId('session-creator-pin-section')).toBeVisible({ timeout: 15000 });
  await page.getByTestId('session-go-to-table').click();

  await expect(page.getByTestId('voting-end-session-btn')).toBeVisible({ timeout: 15000 });
  await page.getByTestId('voting-end-session-btn').click();
  await expect(page.getByTestId('modal-end-session')).toBeVisible();
  await page.getByTestId('modal-end-session-confirm').click();

  await expect(page.getByTestId('modal-end-session')).not.toBeVisible({ timeout: 5000 });
  // Requires backend to send SessionEnded via SignalR so client navigates to /
  await page.waitForURL((url) => new URL(url).pathname === '/', { timeout: 20000 });
  await expect(page.getByTestId('home')).toBeVisible({ timeout: 5000 });
  await expect(page.getByTestId('home-create-table')).toBeVisible();
});
