/**
 * data-testid values used across the frontend for accessibility, MCP browser testing,
 * and Playwright/getByTestId-style selectors. Use these constants in tests to avoid typos.
 */
export const TEST_IDS = {
  // App
  appMain: 'app-main',

  // Home
  home: 'home',
  homeContainer: 'home-container',
  homeLogo: 'home-logo',
  homeActionCardsDesktop: 'home-action-cards-desktop',
  homeHostCard: 'home-host-card',
  homeCreateTable: 'home-create-table',
  homeJoinCard: 'home-join-card',
  homeJoinTable: 'home-join-table',
  homeActionCardsMobile: 'home-action-cards-mobile',
  homeCreateTableMobile: 'home-create-table-mobile',
  homeJoinTableMobile: 'home-join-table-mobile',
  homeBackToLobby: 'home-back-to-lobby',

  // SessionCreator
  sessionCreator: 'session-creator',
  sessionCreatorForm: 'session-creator-form',
  sessionNameInput: 'session-name-input',
  hostNameInput: 'host-name-input',
  sessionCreatorOpenTable: 'session-creator-open-table',
  sessionCreatorPinSection: 'session-creator-pin-section',
  sessionPinDisplay: 'session-pin-display',
  sessionCopyPin: 'session-copy-pin',
  sessionGoToTable: 'session-go-to-table',
  sessionToast: 'session-toast',

  // PinEntry
  pinEntry: 'pin-entry',
  pinEntryForm: 'pin-entry-form',
  pinInput: 'pin-input',
  pinEntryNameInput: 'pin-entry-name-input',
  pinEntryJoinBtn: 'pin-entry-join-btn',
  pinEntryError: 'pin-entry-error',

  // VotingRoom
  votingRoom: 'voting-room',
  votingSessionHeader: 'voting-session-header',
  votingParticipantsSection: 'voting-participants-section',
  votingParticipantsList: 'voting-participants-list',
  votingSection: 'voting-section',
  votingCardsContainer: 'voting-cards-container',
  votingHostControls: 'voting-host-controls',
  votingHostModeBtn: 'voting-host-mode-btn',
  revealVotesBtn: 'reveal-votes-btn',
  votingEndSessionBtn: 'voting-end-session-btn',
  leaveGameBtn: 'leave-game-btn',
  modalEndSessionOverlay: 'modal-end-session-overlay',
  modalEndSession: 'modal-end-session',
  modalEndSessionStay: 'modal-end-session-stay',
  modalEndSessionConfirm: 'modal-end-session-confirm',
  modalLeaveOverlay: 'modal-leave-overlay',
  modalLeave: 'modal-leave',
  modalLeaveStay: 'modal-leave-stay',
  modalLeaveConfirm: 'modal-leave-confirm',

  // PlayingCard (values: card-0, card-1, card-2, card-3, card-5, card-8, card-13, card-21, card-infinity)
  card: (value: string) => (value === '∞' ? 'card-infinity' : `card-${value}`),

  // ResultsDisplay
  resultsDisplay: 'results-display',
  resultsActions: 'results-actions',
  resultsNextDealBtn: 'results-next-deal-btn',
  resultsEndSessionBtn: 'results-end-session-btn',

  // Session
  sessionView: 'session-view',
  sessionLoading: 'session-loading',
  sessionError: 'session-error',
  sessionGoHomeBtn: 'session-go-home-btn',
} as const;
