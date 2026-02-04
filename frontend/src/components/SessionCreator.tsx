import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { sessionService } from '../services/apiService';
import { Session } from '../types';

const SessionCreator: React.FC = () => {
  const [sessionName, setSessionName] = useState('');
  const [participantName, setParticipantName] = useState('');
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const createSession = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!sessionName.trim()) return;

    setLoading(true);
    try {
      const response = await sessionService.createSession(sessionName);
      setSession(response);
    } catch (error) {
      console.error('Error creating session:', error);
      alert('Failed to create session. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const copyPin = () => {
    if (session?.pin) {
      navigator.clipboard.writeText(session.pin);
      alert('PIN copied to clipboard!');
    }
  };

  const joinAndGoSession = async () => {
    if (!session?.pin || !participantName.trim()) return;

    setLoading(true);
    try {
      const participant = await sessionService.joinSession(session.pin, participantName);
      navigate(`/session/${session.pin}?participantId=${participant.participantId}&name=${encodeURIComponent(participantName)}`);
    } catch (error) {
      console.error('Error joining session:', error);
      alert('Failed to join session. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="session-creator fade-in">
      <div className="casino-card table-marking">
        <h1 className="casino-title">HOST A NEW TABLE</h1>
        {!session ? (
          <form onSubmit={createSession} className="session-form">
            <div className="form-group">
              <label htmlFor="sessionName">TABLE NAME</label>
              <input
                id="sessionName"
                value={sessionName}
                onChange={(e) => setSessionName(e.target.value)}
                type="text"
                className="casino-input"
                placeholder="e.g. Sprint 42 Planning"
                required
              />
            </div>
            <button type="submit" className="casino-button" disabled={loading}>
              {loading ? 'PREPARING...' : 'OPEN TABLE'}
            </button>
          </form>
        ) : (
          <div className="pin-section fade-in">
            <h2>YOUR TABLE PIN</h2>
            <div className="pin-display">{session.pin}</div>
            <p className="pin-instruction">SHARE THIS PIN WITH YOUR TEAM</p>

            <div className="form-group" style={{ marginBottom: '25px' }}>
              <label htmlFor="participantName">YOUR NAME</label>
              <input
                id="participantName"
                value={participantName}
                onChange={(e) => setParticipantName(e.target.value)}
                type="text"
                className="casino-input"
                placeholder="e.g. Maverick"
                required
              />
            </div>

            <div className="button-group">
              <button onClick={copyPin} className="casino-button">COPY PIN</button>
              <button
                onClick={joinAndGoSession}
                className="casino-button secondary"
                disabled={loading || !participantName.trim()}
              >
                {loading ? 'SITTING DOWN...' : 'JOIN TABLE'}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default SessionCreator;
