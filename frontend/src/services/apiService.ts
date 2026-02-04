import axios from 'axios'
import { Session, Participant, Results } from '../types'

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json'
  }
})

api.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 429) {
      alert("You are doing that too much. Please wait a moment before trying again.");
    }
    return Promise.reject(error);
  }
);

export const sessionService = {
  async createSession(sessionName: string): Promise<Session> {
    const response = await api.post<Session>('/sessions', { sessionName })
    return response.data
  },

  async getSession(pin: string): Promise<Session> {
    const response = await api.get<Session>(`/sessions/${pin}`)
    return response.data
  },

  async joinSession(pin: string, name: string): Promise<Participant> {
    const response = await api.post<Participant>(`/sessions/${pin}/participants`, { name })
    return response.data
  },

  async submitVote(pin: string, participantId: number, cardValue: string): Promise<void> {
    await api.post(`/sessions/${pin}/votes`, {
      participantId,
      cardValue
    })
  },

  async revealVotes(pin: string): Promise<void> {
    await api.post(`/sessions/${pin}/reveal`)
  },

  async resetSession(pin: string): Promise<void> {
    await api.post(`/sessions/${pin}/reset`)
  },

  async getResults(pin: string): Promise<Results> {
    const response = await api.get<Results>(`/sessions/${pin}/results`)
    return response.data
  },

  async endSession(pin: string): Promise<void> {
    await api.delete(`/sessions/${pin}`)
  }
}

export default api
