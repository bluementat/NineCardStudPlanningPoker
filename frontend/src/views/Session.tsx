import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useLocation, useNavigate } from 'react-router-dom';
import VotingRoom from '../components/VotingRoom';
import { sessionService } from '../services/apiService';

const Session: React.FC = () => {
  const { pin } = useParams<{ pin: string }>();
  const location = useLocation();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [participantId, setParticipantId] = useState<number | null>(null);
  const [participantName, setParticipantName] = useState('');
  const [isHost, setIsHost] = useState(false);

  const initializeSession = useCallback(async () => {
    if (!pin) return;

    try {
      const queryParams = new URLSearchParams(location.search);
      const pId = queryParams.get('participantId');
      const name = queryParams.get('name');

      if (pId && name) {
        setParticipantId(parseInt(pId));
        setParticipantName(name);
      }

      const session = await sessionService.getSession(pin);

      if (session.participants && session.participants.length > 0) {
        const firstParticipant = session.participants[0];
        setIsHost(parseInt(pId || '0') === firstParticipant.participantId);
      } else {
        setIsHost(true);
      }

      setLoading(false);
    } catch (err) {
      console.error('Error initializing session:', err);
      setError('Session not found or could not be loaded.');
      setLoading(false);
    }
  }, [pin, location.search]);

  useEffect(() => {
    initializeSession();
  }, [initializeSession]);

  if (loading) {
    return (
      <div className="session-view">
        <div className="loading">
          <p>Loading session...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="session-view">
        <div className="error">
          <p>{error}</p>
          <button onClick={() => navigate('/')} className="casino-button">
            Go Home
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="session-view">
      <VotingRoom
        pin={pin!}
        currentParticipantId={participantId}
        currentParticipantName={participantName}
        isHost={isHost}
      />
    </div>
  );
};

export default Session;
