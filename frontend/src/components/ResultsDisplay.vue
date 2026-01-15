<template>
  <div class="results-display fade-in">
    <div class="casino-card table-marking">
      <h2 class="results-title">THE REVEAL</h2>
      <div class="votes-container">
        <div
          v-for="(vote, index) in votes"
          :key="vote.participantId"
          class="vote-item"
          :style="{ animationDelay: `${index * 0.1}s` }"
        >
          <div class="participant-name">{{ vote.participantName }}</div>
          <PlayingCard
            :value="vote.cardValue"
            :is-revealed="true"
            :disabled="true"
          />
        </div>
      </div>
      
      <div v-if="statistics" class="statistics">
        <div class="stat-item">
          <span class="stat-label">TABLE AVERAGE</span>
          <span class="stat-value">{{ statistics.average.toFixed(1) }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">LOW BET</span>
          <span class="stat-value">{{ statistics.min }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">HIGH BET</span>
          <span class="stat-value">{{ statistics.max }}</span>
        </div>
      </div>
      
      <div class="results-actions">
        <button @click="$emit('new-round')" class="casino-button">
          NEXT DEAL
        </button>
      </div>
    </div>
  </div>
</template>

<script>
import PlayingCard from './PlayingCard.vue'

export default {
  name: 'ResultsDisplay',
  components: {
    PlayingCard
  },
  props: {
    votes: {
      type: Array,
      required: true
    },
    statistics: {
      type: Object,
      default: null
    }
  },
  emits: ['new-round']
}
</script>

<style scoped>
.results-display {
  margin-top: 30px;
}

.results-title {
  color: #d4af37;
  text-align: center;
  margin-bottom: 40px;
  font-size: 42px;
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.5);
  font-family: 'Georgia', serif;
  font-variant: small-caps;
  letter-spacing: 4px;
}

.votes-container {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 20px;
  margin-bottom: 30px;
}

.vote-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  animation: fadeIn 0.5s ease-out both;
}

.participant-name {
  color: #f5d76e;
  margin-bottom: 15px;
  font-weight: 900;
  font-size: 16px;
  letter-spacing: 1px;
}

.statistics {
  display: flex;
  justify-content: space-around;
  padding: 30px;
  background: rgba(0, 0, 0, 0.4);
  border-radius: 12px;
  border: 2px solid #d4af37;
  margin-top: 40px;
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.stat-label {
  color: #f5f5dc;
  font-size: 12px;
  margin-bottom: 8px;
  letter-spacing: 2px;
  font-weight: bold;
  opacity: 0.8;
}

.stat-value {
  color: #d4af37;
  font-size: 32px;
  font-weight: 900;
  text-shadow: 0 0 10px rgba(212, 175, 55, 0.3);
}

.results-actions {
  display: flex;
  justify-content: center;
  margin-top: 40px;
}

@media (max-width: 768px) {
  .votes-container {
    gap: 10px;
  }
  
  .statistics {
    flex-direction: column;
    gap: 15px;
  }
}
</style>
