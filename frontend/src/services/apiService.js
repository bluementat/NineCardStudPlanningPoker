import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json'
  }
})

export const sessionService = {
  async createSession(sessionName) {
    const response = await api.post('/sessions', { sessionName })
    return response.data
  },

  async getSession(pin) {
    const response = await api.get(`/sessions/${pin}`)
    return response.data
  },

  async joinSession(pin, name) {
    const response = await api.post(`/sessions/${pin}/participants`, { name })
    return response.data
  },

  async submitVote(pin, participantId, cardValue) {
    const response = await api.post(`/sessions/${pin}/votes`, {
      participantId,
      cardValue
    })
    return response.data
  },

  async revealVotes(pin) {
    const response = await api.post(`/sessions/${pin}/reveal`)
    return response.data
  },

  async resetSession(pin) {
    const response = await api.post(`/sessions/${pin}/reset`)
    return response.data
  },

  async getResults(pin) {
    const response = await api.get(`/sessions/${pin}/results`)
    return response.data
  }
}

export default api
