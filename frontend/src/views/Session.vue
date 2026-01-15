<template>
  <div class="session-view">
    <div v-if="loading" class="loading">
      <p>Loading session...</p>
    </div>
    <div v-else-if="error" class="error">
      <p>{{ error }}</p>
      <button @click="$router.push('/')" class="casino-button">
        Go Home
      </button>
    </div>
    <VotingRoom
      v-else
      :pin="pin"
      :current-participant-id="participantId"
      :current-participant-name="participantName"
      :is-host="isHost"
    />
  </div>
</template>

<script>
import VotingRoom from '../components/VotingRoom.vue'
import { sessionService } from '../services/apiService'

export default {
  name: 'Session',
  components: {
    VotingRoom
  },
  props: {
    pin: {
      type: String,
      required: true
    }
  },
  data() {
    return {
      loading: true,
      error: '',
      participantId: null,
      participantName: '',
      isHost: false,
      sessionCreatorId: null
    }
  },
  async mounted() {
    await this.initializeSession()
  },
  methods: {
    async initializeSession() {
      try {
        // Get participant info from query params if available
        const participantId = this.$route.query.participantId
        const name = this.$route.query.name
        
        if (participantId && name) {
          this.participantId = parseInt(participantId)
          this.participantName = name
        }
        
        // Load session to verify it exists
        const session = await sessionService.getSession(this.pin)
        
        // Check if this is the host (first participant or session creator)
        // For simplicity, we'll consider the first participant as host
        if (session.participants && session.participants.length > 0) {
          const firstParticipant = session.participants[0]
          this.isHost = this.participantId === firstParticipant.participantId
        } else {
          // If no participants yet, allow joining as host
          this.isHost = true
        }
        
        this.loading = false
      } catch (error) {
        console.error('Error initializing session:', error)
        this.error = 'Session not found or could not be loaded.'
        this.loading = false
      }
    }
  }
}
</script>

<style scoped>
.session-view {
  min-height: 100vh;
}

.loading,
.error {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  text-align: center;
}

.loading p,
.error p {
  color: #f5f5dc;
  font-size: 24px;
  margin-bottom: 20px;
}

.error p {
  color: #ff6b6b;
}
</style>
