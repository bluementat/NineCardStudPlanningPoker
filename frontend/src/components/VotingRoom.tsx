import React, { useState, useEffect, useCallback } from 'react';
import PlayingCard from './PlayingCard';
import ResultsDisplay from './ResultsDisplay';
import { sessionService } from '../services/apiService';
import signalrService from '../services/signalrService';
import { Participant, Results } from '../types';

interface VotingRoomProps {
  pin: string;
  currentParticipantId: number | null;
  currentParticipantName: string;
  isHost: boolean;
}

const VotingRoom: React.FC<VotingRoomProps> = ({
  pin,
  currentParticipantId,
  currentParticipantName,
  isHost,
}) => {
  const [sessionName, setSessionName] = useState('');
  const [participants, setParticipants] = useState<Participant[]>([]);
  const [selectedCard, setSelectedCard] = useState('');
  const [votes, setVotes] = useState<Record<number, string>>({});
  const [isRevealed, setIsRevealed] = useState(false);
  const [results, setResults] = useState<Results | null>(null);

  const cardValues = ['0', '1', '2', '3', '5', '8', '13', '21', '∞'];

  const loadSession = useCallback(async () => {
    try {
      const session = await sessionService.getSession(pin);
      setSessionName(session.sessionName);
      setParticipants(session.participants || []);
    } catch (error) {
      console.error('Error loading session:', error);
    }
  }, [pin]);

  const loadResults = useCallback(async () => {
    try {
      const res = await sessionService.getResults(pin);
      setResults(res);
      const newVotes: Record<number, string> = {};
      res.votes.forEach((vote) => {
        newVotes[vote.participantId] = vote.cardValue;
      });
      setVotes(newVotes);
    } catch (error) {
      console.error('Error loading results:', error);
    }
  }, [pin]);

  const handleNewRoundStarted = useCallback(() => {
    setIsRevealed(false);
    setSelectedCard('');
    setVotes({});
    setResults(null);
  }, []);

  useEffect(() => {
    const setup = async () => {
      await loadSession();
      try {
        await signalrService.connect();
        await signalrService.joinSession(pin, currentParticipantName || 'Participant');

        signalrService.onParticipantJoined(() => {
          loadSession();
        });

        signalrService.onVoteSubmitted((data) => {
          setVotes((prev) => ({ ...prev, [data.participantId]: data.cardValue }));
        });

        signalrService.onVotesRevealed(async () => {
          setIsRevealed(true);
          await loadResults();
        });

        signalrService.onNewRoundStarted(() => {
          handleNewRoundStarted();
        });
      } catch (error) {
        console.error('Error setting up SignalR:', error);
      }
    };

    setup();

    return () => {
      signalrService.off('ParticipantJoined');
      signalrService.off('VoteSubmitted');
      signalrService.off('VotesRevealed');
      signalrService.off('NewRoundStarted');
      if (pin) {
        signalrService.leaveSession(pin);
      }
    };
  }, [pin, currentParticipantName, loadSession, loadResults, handleNewRoundStarted]);

  const selectCard = async (cardValue: string) => {
    if (!currentParticipantId || isRevealed) return;

    setSelectedCard(cardValue);
    try {
      await sessionService.submitVote(pin, currentParticipantId, cardValue);
      setVotes((prev) => ({ ...prev, [currentParticipantId]: cardValue }));
    } catch (error) {
      console.error('Error submitting vote:', error);
      alert('Failed to submit vote. Please try again.');
    }
  };

  const revealVotes = async () => {
    try {
      await sessionService.revealVotes(pin);
      setIsRevealed(true);
      await loadResults();
    } catch (error) {
      console.error('Error revealing votes:', error);
    }
  };

  const startNewRound = async () => {
    if (!isHost) return;
    try {
      await sessionService.resetSession(pin);
    } catch (error) {
      console.error('Error resetting session:', error);
      alert('Failed to start new round. Please try again.');
    }
  };

  const hasVoted = (participantId: number) => {
    return votes[participantId] !== undefined;
  };

  return (
    <div className="voting-room">
      <div className="session-header table-marking">
        <h1 className="session-name">{sessionName}</h1>
        <div className="session-pin">SESSION ID: {pin}</div>
      </div>

      <div className="participants-section">
        <h2 className="section-title">Players at Table ({participants.length})</h2>
        <div className="participants-list">
          {participants.map((participant) => (
            <div
              key={participant.participantId}
              className={`participant-item ${hasVoted(participant.participantId) ? 'has-voted' : ''}`}
            >
              <div className="chip-avatar">{participant.name[0].toUpperCase()}</div>
              <span className="participant-name">{participant.name}</span>
              {hasVoted(participant.participantId) && <span className="vote-indicator">READY</span>}
            </div>
          ))}
        </div>
      </div>

      {!isRevealed && (
        <div className="voting-section table-marking">
          <h2 className="section-title">Place Your Bet</h2>
          <div className="cards-container">
            {cardValues.map((cardValue) => (
              <PlayingCard
                key={cardValue}
                value={cardValue}
                isSelected={selectedCard === cardValue}
                disabled={!currentParticipantId || isRevealed}
                onSelect={selectCard}
              />
            ))}
          </div>
          {selectedCard && (
            <div className="selected-card-info">
              YOUR CHOICE: <strong>{selectedCard}</strong>
            </div>
          )}
        </div>
      )}

      {isHost && !isRevealed && (
        <div className="host-controls">
          <button
            onClick={revealVotes}
            className="casino-button reveal-button"
            disabled={participants.length === 0}
          >
            Reveal Votes
          </button>
        </div>
      )}

      {isRevealed && results && (
        <ResultsDisplay
          votes={results.votes}
          statistics={results.statistics}
          isHost={isHost}
          onNewRound={startNewRound}
        />
      )}
    </div>
  );
};

export default VotingRoom;
