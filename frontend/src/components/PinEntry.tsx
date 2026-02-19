import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { sessionService } from '../services/apiService';

interface PinEntryProps {
  initialPin?: string;
}

const PinEntry: React.FC<PinEntryProps> = ({ initialPin = '' }) => {
  const [pin, setPin] = useState(initialPin);
  const [name, setName] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const formatPin = (e: React.ChangeEvent<HTMLInputElement>) => {
    let value = e.target.value.replace(/\D/g, '');
    if (value.length > 6) {
      value = value.substring(0, 6);
    }
    setPin(value);
    setError('');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (pin.length !== 6 || !name.trim()) return;

    setLoading(true);
    setError('');

    try {
      // First verify session exists
      await sessionService.getSession(pin);

      // Join the session
      const participant = await sessionService.joinSession(pin, name);

      // Navigate to session
      navigate(`/session/${pin}?participantId=${participant.participantId}&name=${encodeURIComponent(name)}`);
    } catch (err: any) {
      console.error('Error joining session:', err);
      if (err.response?.status === 404) {
        setError('Session not found. Please check the PIN.');
      } else {
        setError('Failed to join session. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="pin-entry fade-in">
      <div className="casino-card table-marking">
        <h1 className="casino-title">TAKE A SEAT</h1>
        <form onSubmit={handleSubmit} className="join-form">
          <div className="form-group">
            <label htmlFor="pin">TABLE PIN</label>
            <input
              id="pin"
              value={pin}
              onChange={formatPin}
              type="text"
              className="casino-input pin-input"
              placeholder="000000"
              maxLength={6}
              pattern="[0-9]{6}"
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="name">PLAYER NAME</label>
            <input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value.slice(0, 30))}
              type="text"
              className="casino-input"
              placeholder="e.g. Maverick"
              maxLength={30}
              required
            />
          </div>
          <button type="submit" className="casino-button" disabled={loading || pin.trim().length !== 6}>
            {loading ? 'SITTING DOWN...' : 'JOIN TABLE'}
          </button>
          {error && <p className="error-message">{error}</p>}
        </form>
      </div>
    </div>
  );
};

export default PinEntry;
