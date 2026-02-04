import React from 'react';

interface PlayingCardProps {
  value: string;
  isSelected?: boolean;
  isFlipped?: boolean;
  isRevealed?: boolean;
  disabled?: boolean;
  onSelect?: (value: string) => void;
}

const PlayingCard: React.FC<PlayingCardProps> = ({
  value,
  isSelected = false,
  isFlipped = false,
  isRevealed = false,
  disabled = false,
  onSelect,
}) => {
  const getSuitIcon = () => {
    const icons: Record<number, string> = {
      0: '♠', // Spades
      1: '♥', // Hearts
      2: '♦', // Diamonds
      3: '♣', // Clubs
    };
    const charCodeSum = value.split('').reduce((sum, char) => sum + char.charCodeAt(0), 0);
    return icons[charCodeSum % 4] || '♠';
  };

  const suitIcon = getSuitIcon();
  const suitColor = ['♥', '♦'].includes(suitIcon) ? 'red' : 'black';

  const handleClick = () => {
    if (!disabled && !isSelected && onSelect) {
      onSelect(value);
    }
  };

  return (
    <div
      className={`playing-card ${isSelected ? 'selected' : ''} ${isFlipped ? 'flipped' : ''} ${isRevealed ? 'revealed' : ''} ${disabled ? 'disabled' : ''}`}
      onClick={handleClick}
    >
      <div className="card-inner">
        <div className={`card-front ${suitColor}`}>
          <div className="card-corner top-left">
            <div className="card-value">{value}</div>
            <div className="card-suit">{suitIcon}</div>
          </div>
          <div className="card-center">
            <div className="card-suit-large">{suitIcon}</div>
          </div>
          <div className="card-corner bottom-right">
            <div className="card-value">{value}</div>
            <div className="card-suit">{suitIcon}</div>
          </div>
        </div>
        <div className="card-back">
          <div className="card-back-pattern">
            <div className="pattern-logo">9</div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default PlayingCard;
