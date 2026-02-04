export interface Participant {
  participantId: number;
  name: string;
  joinedAt: string;
}

export interface Session {
  sessionId: number;
  pin: string;
  sessionName: string;
  createdAt: string;
  status: string;
  participants?: Participant[];
}

export interface VoteResult {
  participantId: number;
  participantName: string;
  cardValue: string;
  votedAt: string;
}

export interface Statistics {
  average: number;
  min: number;
  max: number;
}

export interface Results {
  votes: VoteResult[];
  statistics: Statistics | null;
}

export interface CreateSessionRequest {
  sessionName: string;
}

export interface JoinSessionRequest {
  name: string;
}

export interface SubmitVoteRequest {
  participantId: number;
  cardValue: string;
}
