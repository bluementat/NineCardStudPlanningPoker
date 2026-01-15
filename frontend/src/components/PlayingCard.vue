<template>
  <div
    class="playing-card"
    :class="{ 'selected': isSelected, 'flipped': isFlipped, 'revealed': isRevealed }"
    @click="handleClick"
  >
    <div class="card-inner">
      <div class="card-front" :class="suitColor">
        <div class="card-corner top-left">
          <div class="card-value">{{ value }}</div>
          <div class="card-suit">{{ suitIcon }}</div>
        </div>
        <div class="card-center">
          <div class="card-suit-large">{{ suitIcon }}</div>
        </div>
        <div class="card-corner bottom-right">
          <div class="card-value">{{ value }}</div>
          <div class="card-suit">{{ suitIcon }}</div>
        </div>
      </div>
      <div class="card-back">
        <div class="card-back-pattern">
          <div class="pattern-logo">9</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'PlayingCard',
  props: {
    value: {
      type: String,
      required: true
    },
    isSelected: {
      type: Boolean,
      default: false
    },
    isFlipped: {
      type: Boolean,
      default: false
    },
    isRevealed: {
      type: Boolean,
      default: false
    },
    disabled: {
      type: Boolean,
      default: false
    }
  },
  emits: ['select'],
  computed: {
    suitIcon() {
      const icons = {
        0: '♠', // Spades
        1: '♥', // Hearts
        2: '♦', // Diamonds
        3: '♣', // Clubs
        'default': '♠'
      }
      // Use the value's length or hash to pick a suit for variety if no suit is provided
      // For Planning Poker, we usually just show values, but we can assign a suit based on value
      const charCodeSum = this.value.split('').reduce((sum, char) => sum + char.charCodeAt(0), 0)
      return icons[charCodeSum % 4]
    },
    suitColor() {
      const redSuits = ['♥', '♦']
      return redSuits.includes(this.suitIcon) ? 'red' : 'black'
    }
  },
  methods: {
    handleClick() {
      if (!this.disabled && !this.isSelected) {
        this.$emit('select', this.value)
      }
    }
  }
}
</script>

<style scoped>
.playing-card {
  width: 100px;
  height: 140px;
  perspective: 1000px;
  cursor: pointer;
  margin: 10px;
  transition: transform 0.3s ease;
}

.playing-card:hover:not(.disabled) {
  transform: translateY(-10px) scale(1.05);
}

.playing-card.selected {
  transform: translateY(-15px) scale(1.1);
  z-index: 10;
}

.playing-card.disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.card-inner {
  position: relative;
  width: 100%;
  height: 100%;
  transform-style: preserve-3d;
  transition: transform 0.6s;
}

.playing-card.flipped .card-inner {
  transform: rotateY(180deg);
}

.playing-card.revealed .card-inner {
  animation: reveal 0.6s cubic-bezier(0.23, 1, 0.32, 1);
}

@keyframes reveal {
  from {
    transform: rotateY(180deg);
  }
  to {
    transform: rotateY(0deg);
  }
}

.card-front,
.card-back {
  position: absolute;
  width: 100%;
  height: 100%;
  backface-visibility: hidden;
  border-radius: 8px;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.4);
}

.card-front {
  background: #ffffff;
  border: 1px solid #dcdcdc;
  display: flex;
  flex-direction: column;
  padding: 8px;
}

.card-front.red {
  color: #d40000;
}

.card-front.black {
  color: #1a1a1a;
}

.card-corner {
  display: flex;
  flex-direction: column;
  align-items: center;
  line-height: 1;
}

.card-corner.top-left {
  align-self: flex-start;
}

.card-corner.bottom-right {
  align-self: flex-end;
  transform: rotate(180deg);
}

.card-value {
  font-size: 20px;
  font-weight: 900;
}

.card-suit {
  font-size: 16px;
}

.card-center {
  flex-grow: 1;
  display: flex;
  justify-content: center;
  align-items: center;
}

.card-suit-large {
  font-size: 48px;
}

.card-back {
  background: #b80f0a; /* Classic red card back */
  transform: rotateY(180deg);
  padding: 6px;
  border: 4px solid white;
}

.card-back-pattern {
  width: 100%;
  height: 100%;
  background-color: #b80f0a;
  background-image: 
    radial-gradient(circle at 100% 150%, #b80f0a 24%, #9a0d08 25%, #9a0d08 28%, #b80f0a 29%, #b80f0a 36%, #9a0d08 36%, #9a0d08 40%, transparent 40%, transparent),
    radial-gradient(circle at 0    150%, #b80f0a 24%, #9a0d08 25%, #9a0d08 28%, #b80f0a 29%, #b80f0a 36%, #9a0d08 36%, #9a0d08 40%, transparent 40%, transparent),
    radial-gradient(circle at 50%  100%, #9a0d08 10%, #b80f0a 11%, #b80f0a 23%, #9a0d08 24%, #9a0d08 30%, #b80f0a 31%, #b80f0a 43%, #9a0d08 44%, #9a0d08 50%, #b80f0a 51%, #b80f0a 63%, #9a0d08 64%, #9a0d08 71%, transparent 71%, transparent),
    radial-gradient(circle at 100% 50%, #9a0d08 5%, #b80f0a 6%, #b80f0a 15%, #9a0d08 16%, #9a0d08 20%, #b80f0a 21%, #b80f0a 30%, #9a0d08 31%, #9a0d08 35%, #b80f0a 36%, #b80f0a 45%, #9a0d08 46%, #9a0d08 49%, transparent 49%, transparent),
    radial-gradient(circle at 0    50%, #9a0d08 5%, #b80f0a 6%, #b80f0a 15%, #9a0d08 16%, #9a0d08 20%, #b80f0a 21%, #b80f0a 30%, #9a0d08 31%, #9a0d08 35%, #b80f0a 36%, #b80f0a 45%, #9a0d08 46%, #9a0d08 49%, transparent 49%, transparent);
  background-size: 20px 40px;
  display: flex;
  justify-content: center;
  align-items: center;
  border-radius: 4px;
}

.pattern-logo {
  background: white;
  color: #b80f0a;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  display: flex;
  justify-content: center;
  align-items: center;
  font-weight: 900;
  font-family: serif;
  border: 2px solid #9a0d08;
}

.playing-card.selected .card-front {
  border: 3px solid #d4af37;
  box-shadow: 0 0 20px rgba(212, 175, 55, 0.8);
}

@media (max-width: 768px) {
  .playing-card {
    width: 70px;
    height: 100px;
    margin: 4px;
  }
  
  .card-value {
    font-size: 14px;
  }
  
  .card-suit {
    font-size: 12px;
  }
  
  .card-suit-large {
    font-size: 24px;
  }
}
</style>
