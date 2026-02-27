import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { sessionService } from '../services/apiService';
import { Session } from '../types';

const SessionCreator: React.FC = () => {
  const [sessionName, setSessionName] = useState('');
  const [hostName, setHostName] = useState('');
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(false);
  const [showToast, setShowToast] = useState(false);
  const navigate = useNavigate();

  const createSession = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!sessionName.trim() || !hostName.trim()) return;

    setLoading(true);
    try {
      const response = await sessionService.createSession(sessionName, hostName);
      setSession(response);
    } catch (error: any) {
      console.error(`Failed to create session. Name: ${sessionName}, Host: ${hostName}`, error);
      alert('Failed to create session. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const copyPin = () => {
    if (session?.pin) {
      navigator.clipboard.writeText(session.pin);
      setShowToast(true);
      setTimeout(() => setShowToast(false), 3000);
    }
  };

  const goToTable = () => {
    if (!session?.pin || !session.participants?.length) return;
    const host = session.participants[0];
    navigate(`/session/${session.pin}?participantId=${host.participantId}&name=${encodeURIComponent(host.name)}`);
  };

  return (
    <div className="session-creator fade-in" data-testid="session-creator">
      <div className="casino-card table-marking">
        <h1 className="casino-title">HOST A NEW TABLE</h1>
        {!session ? (
          <form onSubmit={createSession} className="session-form" data-testid="session-creator-form">
            <div className="form-group">
              <label htmlFor="sessionName">TABLE NAME</label>
              <input
                id="sessionName"
                value={sessionName}
                onChange={(e) => setSessionName(e.target.value.slice(0, 30))}
                type="text"
                className="casino-input"
                placeholder="e.g. Sprint 42 Planning"
                maxLength={30}
                required
                data-testid="session-name-input"
              />
            </div>
            <div className="form-group">
              <label htmlFor="hostName">YOUR NAME</label>
              <input
                id="hostName"
                value={hostName}
                onChange={(e) => setHostName(e.target.value.slice(0, 30))}
                type="text"
                className="casino-input"
                placeholder="e.g. Maverick"
                maxLength={30}
                required
                data-testid="host-name-input"
              />
            </div>
            <button type="submit" className="casino-button" disabled={loading} data-testid="session-creator-open-table">
              {loading ? 'PREPARING...' : 'OPEN TABLE'}
            </button>
          </form>
        ) : (
          <div className="pin-section fade-in" data-testid="session-creator-pin-section">
            <h2>YOUR TABLE PIN</h2>
            <p className="pin-instruction">SHARE THIS PIN WITH YOUR TEAM</p>
            <div className="pin-display" data-testid="session-pin-display">{session.pin}</div>
            <hr className="pin-section-divider" />

            <div className="button-group">
              <button onClick={copyPin} className="casino-button" data-testid="session-copy-pin">COPY PIN</button>
              <button
                onClick={goToTable}
                className="casino-button secondary"
                data-testid="session-go-to-table"
              >
                GO TO TABLE
              </button>
            </div>
          </div>
        )}
      </div>
      {showToast && (
        <div className="toast-container" data-testid="session-toast">
          <div className="casino-toast">
            PIN COPIED TO CLIPBOARD
          </div>
        </div>
      )}
    </div>
  );
};

export default SessionCreator;
