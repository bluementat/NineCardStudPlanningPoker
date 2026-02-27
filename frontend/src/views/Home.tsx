import React, { useState, useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import SessionCreator from '../components/SessionCreator';
import PinEntry from '../components/PinEntry';
import logo from '../assets/img/NCSPPLogo.png';

const Home: React.FC = () => {
  const [showCreator, setShowCreator] = useState(false);
  const [pin, setPin] = useState('');
  const [joining, setJoining] = useState(false);
  const location = useLocation();

  useEffect(() => {
    const queryParams = new URLSearchParams(location.search);
    const pinParam = queryParams.get('pin');
    if (pinParam) {
      setPin(pinParam);
      setShowCreator(false);
      setJoining(true);
    }
  }, [location]);

  return (
    <div className={`home ${showCreator || joining ? 'bg-area' : 'bg-lobby'}`} data-testid="home">
      <div className="casino-container" data-testid="home-container">
        <div className={`welcome-section ${showCreator || joining ? 'compact' : ''}`}>
          <img src={logo} alt="Nine Card Stud Premium Planning Poker" className="main-logo" data-testid="home-logo" />
        </div>

        {!showCreator && !joining ? (
          <>
            <div className="action-cards action-cards-desktop" data-testid="home-action-cards-desktop">
              <div className="action-card casino-card table-marking" data-testid="home-host-card">
                <h2>HOST GAME</h2>
                <p>Start a session - Invite your team</p>
                <button onClick={() => setShowCreator(true)} className="casino-button" data-testid="home-create-table">
                  CREATE TABLE
                </button>
              </div>

              <div className="action-card casino-card table-marking" data-testid="home-join-card">
                <h2>JOIN GAME</h2>
                <p>Enter the table PIN to join the action</p>
                <button onClick={() => setJoining(true)} className="casino-button" data-testid="home-join-table">
                  JOIN TABLE
                </button>
              </div>
            </div>

            <div className="action-card-unified casino-card table-marking action-cards-mobile" data-testid="home-action-cards-mobile">
              <div className="action-option">
                <h2>HOST GAME</h2>
                <p>Start a session - Invite your team</p>
                <button onClick={() => setShowCreator(true)} className="casino-button" data-testid="home-create-table-mobile">
                  CREATE TABLE
                </button>
              </div>
              <div className="action-option">
                <h2>JOIN GAME</h2>
                <p>Enter the table PIN to join the action</p>
                <button onClick={() => setJoining(true)} className="casino-button" data-testid="home-join-table-mobile">
                  JOIN TABLE
                </button>
              </div>
            </div>
          </>
        ) : null}

        {showCreator || joining ? (
          <div className="back-link">
            <button
              onClick={() => {
                setShowCreator(false);
                setJoining(false);
                setPin('');
              }}
              className="text-link"
              data-testid="home-back-to-lobby"
            >
              ← BACK TO LOBBY
            </button>
          </div>
        ) : null}

        {showCreator && <SessionCreator />}
        {(joining || pin) && <PinEntry initialPin={pin} />}
      </div>
    </div>
  );
};

export default Home;
