import React from 'react';
import PlayingCard from './PlayingCard';
import { VoteResult, Statistics } from '../types';

interface ResultsDisplayProps {
  votes: VoteResult[];
  statistics: Statistics | null;
  isHost: boolean;
  onNewRound: () => void;
}

const ResultsDisplay: React.FC<ResultsDisplayProps> = ({
  votes,
  statistics,
  isHost,
  onNewRound,
}) => {
  return (
    <div className="results-display fade-in">
      <div className="casino-card table-marking">
        <h2 className="results-title">THE REVEAL</h2>
        <div className="votes-container">
          {votes.map((vote, index) => (
            <div
              key={vote.participantId}
              className="vote-item"
              style={{ animationDelay: `${index * 0.1}s` }}
            >
              <div className="participant-name">{vote.participantName}</div>
              <PlayingCard
                value={vote.cardValue}
                isRevealed={true}
                disabled={true}
              />
            </div>
          ))}
        </div>

        {statistics && (
          <div className="statistics">
            <div className="stat-item">
              <span className="stat-label">TABLE AVERAGE</span>
              <span className="stat-value">{statistics.average.toFixed(1)}</span>
            </div>
            <div className="stat-item">
              <span className="stat-label">LOW BET</span>
              <span className="stat-value">{statistics.min}</span>
            </div>
            <div className="stat-item">
              <span className="stat-label">HIGH BET</span>
              <span className="stat-value">{statistics.max}</span>
            </div>
          </div>
        )}

        {isHost && (
          <div className="results-actions">
            <button onClick={onNewRound} className="casino-button">
              NEXT DEAL
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default ResultsDisplay;
