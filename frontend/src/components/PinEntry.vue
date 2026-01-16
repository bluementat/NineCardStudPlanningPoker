<template>
  <div class="pin-entry fade-in">
    <div class="casino-card table-marking">
      <h1 class="casino-title">TAKE A SEAT</h1>
      <form @submit.prevent="joinSession" class="join-form">
        <div class="form-group">
          <label for="pin">TABLE PIN</label>
          <input
            id="pin"
            v-model="pin"
            type="text"
            class="casino-input pin-input"
            placeholder="000000"
            maxlength="6"
            pattern="[0-9]{6}"
            required
            @input="formatPin"
          />
        </div>
        <div class="form-group">
          <label for="name">PLAYER NAME</label>
          <input
            id="name"
            v-model="name"
            type="text"
            class="casino-input"
            placeholder="e.g. Maverick"
            required
          />
        </div>
        <button type="submit" class="casino-button" :disabled="loading || pin.trim().length !== 6">
          {{ loading ? 'SITTING DOWN...' : 'JOIN TABLE' }}
        </button>
        <p v-if="error" class="error-message">{{ error }}</p>
      </form>
    </div>
  </div>
</template>

<script>
import { sessionService } from '../services/apiService'

export default {
  name: 'PinEntry',
  props: {
    initialPin: {
      type: String,
      default: ''
    }
  },
  data() {
    return {
      pin: this.initialPin,
      name: '',
      loading: false,
      error: ''
    }
  },
  methods: {
    formatPin(event) {
      let value = event.target.value.replace(/\D/g, '')
      if (value.length > 6) {
        value = value.substring(0, 6)
      }
      this.pin = value
      this.error = ''
    },
    async joinSession() {
      if (this.pin.length !== 6 || !this.name.trim()) return
      
      this.loading = true
      this.error = ''
      
      try {
        // First verify session exists
        await sessionService.getSession(this.pin)
        
        // Join the session
        const participant = await sessionService.joinSession(this.pin, this.name)
        
        // Navigate to session
        this.$router.push({
          path: `/session/${this.pin}`,
          query: { participantId: participant.participantId, name: this.name }
        })
      } catch (error) {
        console.error('Error joining session:', error)
        if (error.response?.status === 404) {
          this.error = 'Session not found. Please check the PIN.'
        } else {
          this.error = 'Failed to join session. Please try again.'
        }
      } finally {
        this.loading = false
      }
    }
  }
}
</script>

<style scoped>
.pin-entry {
  display: flex;
  justify-content: center;
  padding: 20px;
}

.casino-title {
  font-size: 32px;
  color: #d4af37;
  text-align: center;
  margin-bottom: 40px;
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.5);
  font-family: 'Georgia', serif;
  letter-spacing: 2px;
}

.form-group label {
  display: block;
  margin-bottom: 12px;
  color: #f5d76e;
  font-weight: 900;
  letter-spacing: 1px;
}

.pin-input {
  text-align: center;
  font-size: 42px;
  letter-spacing: 12px;
  font-weight: 900;
  color: #d4af37 !important;
  font-family: 'Courier New', monospace;
}

.error-message {
  color: #ff6b6b;
  margin-top: 20px;
  text-align: center;
  font-size: 14px;
  font-weight: bold;
  background: rgba(0,0,0,0.3);
  padding: 10px;
  border-radius: 4px;
}
</style>
