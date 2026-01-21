<template>
  <div class="voting-room">
    <div class="session-header table-marking">
      <h1 class="session-name">{{ sessionName }}</h1>
      <div class="session-pin">SESSION ID: {{ pin }}</div>
    </div>
    
    <div class="participants-section">
      <h2 class="section-title">Players at Table ({{ participants.length }})</h2>
      <div class="participants-list">
        <div
          v-for="participant in participants"
          :key="participant.participantId"
          class="participant-item"
          :class="{ 'has-voted': hasVoted(participant.participantId) }"
        >
          <div class="chip-avatar">{{ participant.name[0].toUpperCase() }}</div>
          <span class="participant-name">{{ participant.name }}</span>
          <span v-if="hasVoted(participant.participantId)" class="vote-indicator">READY</span>
        </div>
      </div>
    </div>
    
    <div v-if="!isRevealed" class="voting-section table-marking">
      <h2 class="section-title">Place Your Bet</h2>
      <div class="cards-container">
        <PlayingCard
          v-for="cardValue in cardValues"
          :key="cardValue"
          :value="cardValue"
          :is-selected="selectedCard === cardValue"
          :disabled="!currentParticipantId || isRevealed"
          @select="selectCard"
        />
      </div>
      <div v-if="selectedCard" class="selected-card-info">
        YOUR CHOICE: <strong>{{ selectedCard }}</strong>
      </div>
    </div>
    
    <div v-if="isHost && !isRevealed" class="host-controls">
      <button
        @click="revealVotes"
        class="casino-button reveal-button"
        :disabled="participants.length === 0"
      >
        Reveal Votes
      </button>
    </div>
    
    <ResultsDisplay
      v-if="isRevealed && results"
      :votes="results.votes"
      :statistics="results.statistics"
      :is-host="isHost"
      @new-round="startNewRound"
    />
  </div>
</template>

<script>
import PlayingCard from './PlayingCard.vue'
import ResultsDisplay from './ResultsDisplay.vue'
import { sessionService } from '../services/apiService'
import signalrService from '../services/signalrService'

export default {
  name: 'VotingRoom',
  components: {
    PlayingCard,
    ResultsDisplay
  },
  props: {
    pin: {
      type: String,
      required: true
    },
    currentParticipantId: {
      type: Number,
      default: null
    },
    currentParticipantName: {
      type: String,
      default: ''
    },
    isHost: {
      type: Boolean,
      default: false
    }
  },
  data() {
    return {
      sessionName: '',
      participants: [],
      selectedCard: '',
      cardValues: ['0', '1', '2', '3', '5', '8', '13', '21', '∞'],
      votes: {},
      isRevealed: false,
      results: null
    }
  },
  async mounted() {
    await this.loadSession()
    await this.setupSignalR()
    await this.loadResults()
  },
  beforeUnmount() {
    signalrService.off('ParticipantJoined')
    signalrService.off('VoteSubmitted')
    signalrService.off('VotesRevealed')
    signalrService.off('NewRoundStarted')
    if (this.pin) {
      signalrService.leaveSession(this.pin)
    }
  },
  methods: {
    async loadSession() {
      try {
        const session = await sessionService.getSession(this.pin)
        this.sessionName = session.sessionName
        this.participants = session.participants || []
      } catch (error) {
        console.error('Error loading session:', error)
      }
    },
    async setupSignalR() {
      try {
        await signalrService.connect()
        await signalrService.joinSession(this.pin, this.currentParticipantName || 'Participant')
        
        signalrService.onParticipantJoined((data) => {
          this.loadSession()
        })
        
        signalrService.onVoteSubmitted((data) => {
          this.votes[data.participantId] = data.cardValue
          this.$forceUpdate()
        })
        
        signalrService.onVotesRevealed(async () => {
          this.isRevealed = true
          await this.loadResults()
        })

        signalrService.onNewRoundStarted(() => {
          this.handleNewRoundStarted()
        })
      } catch (error) {
        console.error('Error setting up SignalR:', error)
      }
    },
    async selectCard(cardValue) {
      if (!this.currentParticipantId || this.isRevealed) return
      
      this.selectedCard = cardValue
      try {
        await sessionService.submitVote(this.pin, this.currentParticipantId, cardValue)
        this.votes[this.currentParticipantId] = cardValue
      } catch (error) {
        console.error('Error submitting vote:', error)
        alert('Failed to submit vote. Please try again.')
      }
    },
    async revealVotes() {
      try {
        await sessionService.revealVotes(this.pin)
        this.isRevealed = true
        await this.loadResults()
      } catch (error) {
        console.error('Error revealing votes:', error)
      }
    },
    async loadResults() {
      try {
        const results = await sessionService.getResults(this.pin)
        this.results = results
        // Update votes map
        results.votes.forEach(vote => {
          this.votes[vote.participantId] = vote.cardValue
        })
      } catch (error) {
        console.error('Error loading results:', error)
      }
    },
    hasVoted(participantId) {
      return this.votes[participantId] !== undefined
    },
    async startNewRound() {
      if (!this.isHost) return
      try {
        await sessionService.resetSession(this.pin)
      } catch (error) {
        console.error('Error resetting session:', error)
        alert('Failed to start new round. Please try again.')
      }
    },
    handleNewRoundStarted() {
      this.isRevealed = false
      this.selectedCard = ''
      this.votes = {}
      this.results = null
    }
  }
}
</script>

<style scoped>
.voting-room {
  padding: 20px;
  max-width: 1400px;
  margin: 0 auto;
}

.session-header {
  text-align: center;
  margin-bottom: 40px;
  background: rgba(0, 0, 0, 0.3);
}

.session-name {
  color: #d4af37;
  font-size: 48px;
  margin-bottom: 5px;
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.8);
  font-family: 'Georgia', serif;
  font-variant: small-caps;
}

.session-pin {
  color: #f5d76e;
  font-size: 20px;
  font-weight: 900;
  letter-spacing: 6px;
}

.participants-section {
  margin-bottom: 40px;
}

.section-title {
  color: #f5d76e;
  margin-bottom: 20px;
  font-size: 24px;
  text-transform: uppercase;
  letter-spacing: 2px;
  text-align: center;
  font-family: 'Georgia', serif;
}

.participants-list {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 15px;
}

.participant-item {
  background: rgba(0, 0, 0, 0.4);
  border: 2px solid #d4af37;
  border-radius: 40px;
  padding: 8px 20px 8px 8px;
  display: flex;
  align-items: center;
  gap: 12px;
  transition: all 0.3s ease;
  min-width: 150px;
}

.participant-item.has-voted {
  border-color: #4caf50;
  background: rgba(76, 175, 80, 0.15);
  box-shadow: 0 0 10px rgba(76, 175, 80, 0.3);
}

.chip-avatar {
  width: 36px;
  height: 36px;
  background: linear-gradient(135deg, #d4af37 0%, #b8941f 100%);
  border: 2px dashed #f5d76e;
  border-radius: 50%;
  display: flex;
  justify-content: center;
  align-items: center;
  color: #1a1a1a;
  font-weight: 900;
  box-shadow: 0 2px 4px rgba(0,0,0,0.3);
}

.participant-name {
  color: #f5f5dc;
  font-weight: bold;
  font-size: 16px;
}

.vote-indicator {
  color: #4caf50;
  font-size: 12px;
  font-weight: 900;
  margin-left: auto;
  letter-spacing: 1px;
}

.voting-section {
  margin-bottom: 40px;
  background: rgba(0, 0, 0, 0.2);
}

.cards-container {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 20px;
  margin-bottom: 30px;
}

.selected-card-info {
  text-align: center;
  color: #f5d76e;
  font-size: 20px;
  margin-top: 30px;
  font-weight: bold;
  letter-spacing: 2px;
  text-transform: uppercase;
}

.host-controls {
  text-align: center;
  margin-top: 30px;
}

.reveal-button {
  font-size: 20px;
  padding: 15px 30px;
}

@media (max-width: 768px) {
  .session-name {
    font-size: 28px;
  }
  
  .session-pin {
    font-size: 18px;
  }
  
  .cards-container {
    gap: 10px;
  }
}
</style>
