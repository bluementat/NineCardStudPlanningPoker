import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.BASE_URL ?? 'https://ncsplanningpoker-frontend.gentleocean-1639b4e1.centralus.azurecontainerapps.io';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: 'list',
  use: {
    baseURL,
    trace: 'on-first-retry',
    video: 'on-first-retry',
  },
  projects: [
    { name: 'deployed', use: { ...devices['Desktop Chrome'] } },
  ],
});
