<template>
  <div class="home">
    <div class="casino-container">
      <div class="welcome-section">
        <h1 class="main-title">9-Card Stud</h1>
        <p class="subtitle">PREMIUM PLANNING POKER</p>
      </div>
      
      <div class="action-cards" v-if="!showCreator && !joining">
        <div class="action-card casino-card table-marking">
          <h2>HOST GAME</h2>
          <p>Start a new session and invite your team</p>
          <button @click="showCreator = true" class="casino-button">
            CREATE TABLE
          </button>
        </div>
        
        <div class="action-card casino-card table-marking">
          <h2>JOIN GAME</h2>
          <p>Enter the table PIN to join the action</p>
          <button @click="joining = true" class="casino-button">
            JOIN TABLE
          </button>
        </div>
      </div>
      
      <div v-if="showCreator || joining" class="back-link">
        <button @click="showCreator = false; joining = false; pin = ''" class="text-link">← BACK TO LOBBY</button>
      </div>

      <SessionCreator v-if="showCreator" />
      <PinEntry v-if="joining || pin" :initial-pin="pin" />
    </div>
  </div>
</template>

<script>
import SessionCreator from '../components/SessionCreator.vue'
import PinEntry from '../components/PinEntry.vue'

export default {
  name: 'Home',
  components: {
    SessionCreator,
    PinEntry
  },
  data() {
    return {
      showCreator: false,
      pin: '',
      joining: false
    }
  },
  watch: {
    $route(to) {
      if (to.query.pin) {
        this.pin = to.query.pin
        this.showCreator = false
        this.joining = true
      }
    }
  },
  mounted() {
    if (this.$route.query.pin) {
      this.pin = this.$route.query.pin
      this.showCreator = false
      this.joining = true
    }
  }
}
</script>

<style scoped>
.home {
  min-height: 100vh;
  padding: 40px 20px;
}

.welcome-section {
  text-align: center;
  margin-bottom: 50px;
}

.main-title {
  font-size: 84px;
  color: #d4af37;
  text-shadow: 4px 4px 8px rgba(0, 0, 0, 0.8);
  margin-bottom: 5px;
  font-weight: 900;
  font-family: 'Georgia', serif;
  font-variant: small-caps;
  letter-spacing: 4px;
}

.subtitle {
  font-size: 18px;
  color: #f5d76e;
  letter-spacing: 12px;
  font-weight: bold;
  opacity: 0.8;
  margin-top: -10px;
}

.action-cards {
  display: flex;
  justify-content: center;
  gap: 40px;
  margin-top: 20px;
  flex-wrap: wrap;
}

.action-card {
  flex: 0 1 350px;
  text-align: center;
  padding: 40px;
}

.action-card h2 {
  color: #d4af37;
  font-size: 32px;
  margin-bottom: 10px;
  font-family: 'Georgia', serif;
}

.action-card p {
  color: #f5f5dc;
  margin-bottom: 30px;
  font-size: 16px;
  font-style: italic;
  opacity: 0.8;
}

.back-link {
  text-align: center;
  margin-bottom: 20px;
}

.text-link {
  background: none;
  border: none;
  color: #d4af37;
  font-weight: 900;
  cursor: pointer;
  letter-spacing: 2px;
  font-family: 'Georgia', serif;
}

.text-link:hover {
  color: #f5d76e;
  text-decoration: underline;
}

@media (max-width: 768px) {
  .main-title {
    font-size: 42px;
  }
  
  .subtitle {
    font-size: 18px;
  }
  
  .action-cards {
    flex-direction: column;
    align-items: center;
  }
}
</style>
