<template>
  <div class="session-creator fade-in">
    <div class="casino-card table-marking">
      <h1 class="casino-title">HOST A NEW TABLE</h1>
      <form v-if="!session" @submit.prevent="createSession" class="session-form">
        <div class="form-group">
          <label for="sessionName">TABLE NAME</label>
          <input
            id="sessionName"
            v-model="sessionName"
            type="text"
            class="casino-input"
            placeholder="e.g. Sprint 42 Planning"
            required
          />
        </div>
        <button type="submit" class="casino-button" :disabled="loading">
          {{ loading ? 'PREPARING...' : 'OPEN TABLE' }}
        </button>
      </form>
      
      <div v-if="session" class="pin-section fade-in">
        <h2>YOUR TABLE PIN</h2>
        <div class="pin-display">{{ session.pin }}</div>
        <p class="pin-instruction">SHARE THIS PIN WITH YOUR TEAM</p>
        <div class="button-group">
          <button @click="copyPin" class="casino-button">COPY PIN</button>
          <button @click="goToSession" class="casino-button secondary">JOIN TABLE</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { sessionService } from '../services/apiService'

export default {
  name: 'SessionCreator',
  data() {
    return {
      sessionName: '',
      session: null,
      loading: false
    }
  },
  methods: {
    async createSession() {
      if (!this.sessionName.trim()) return
      
      this.loading = true
      try {
        const response = await sessionService.createSession(this.sessionName)
        this.session = {
          pin: response.pin,
          sessionId: response.sessionId,
          sessionName: response.sessionName
        }
      } catch (error) {
        console.error('Error creating session:', error)
        alert('Failed to create session. Please try again.')
      } finally {
        this.loading = false
      }
    },
    copyPin() {
      if (this.session?.pin) {
        navigator.clipboard.writeText(this.session.pin)
        alert('PIN copied to clipboard!')
      }
    },
    goToSession() {
      if (this.session?.pin) {
        this.$router.push(`/session/${this.session.pin}`)
      }
    }
  }
}
</script>

<style scoped>
.session-creator {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
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

.pin-section {
  margin-top: 30px;
  text-align: center;
  padding-top: 30px;
  border-top: 2px solid rgba(212, 175, 55, 0.3);
}

.pin-section h2 {
  color: #f5d76e;
  margin-bottom: 15px;
  font-size: 20px;
  letter-spacing: 2px;
}

.pin-instruction {
  margin: 20px 0;
  color: #f5f5dc;
  font-size: 14px;
  letter-spacing: 1px;
  opacity: 0.8;
}

.button-group {
  display: flex;
  justify-content: center;
  gap: 20px;
}
</style>
