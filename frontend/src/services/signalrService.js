import * as signalR from '@microsoft/signalr'

class SignalRService {
  constructor() {
    this.connection = null
    this.isConnected = false
  }

  async connect() {
    if (this.connection && this.isConnected) {
      return
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/planningpokerhub')
      .withAutomaticReconnect()
      .build()

    this.connection.onreconnecting(() => {
      this.isConnected = false
    })

    this.connection.onreconnected(() => {
      this.isConnected = true
    })

    this.connection.onclose(() => {
      this.isConnected = false
    })

    try {
      await this.connection.start()
      this.isConnected = true
    } catch (err) {
      console.error('SignalR Connection Error: ', err)
      throw err
    }
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop()
      this.isConnected = false
    }
  }

  async joinSession(pin, participantName) {
    if (!this.isConnected) {
      await this.connect()
    }
    await this.connection.invoke('JoinSession', pin, participantName)
  }

  async leaveSession(pin) {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('LeaveSession', pin)
    }
  }

  onParticipantJoined(callback) {
    if (this.connection) {
      this.connection.on('ParticipantJoined', callback)
    }
  }

  onVoteSubmitted(callback) {
    if (this.connection) {
      this.connection.on('VoteSubmitted', callback)
    }
  }

  onVotesRevealed(callback) {
    if (this.connection) {
      this.connection.on('VotesRevealed', callback)
    }
  }

  off(eventName) {
    if (this.connection) {
      this.connection.off(eventName)
    }
  }
}

export default new SignalRService()
