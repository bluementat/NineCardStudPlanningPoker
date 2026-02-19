import * as signalR from '@microsoft/signalr'

class SignalRService {
  private connection: signalR.HubConnection | null = null
  private isConnected: boolean = false

  async connect(): Promise<void> {
    if (this.connection && this.isConnected) {
      return
    }

    const hubUrl = import.meta.env.VITE_API_URL 
      ? `${import.meta.env.VITE_API_URL.replace('/api', '')}/planningpokerhub`
      : '/planningpokerhub';

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
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

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop()
      this.isConnected = false
    }
  }

  async joinSession(pin: string, participantId: number, participantName: string): Promise<void> {
    if (!this.isConnected) {
      await this.connect()
    }
    if (this.connection) {
      await this.connection.invoke('JoinSession', pin, participantId, participantName)
    }
  }

  async leaveSession(pin: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('LeaveSession', pin)
    }
  }

  onParticipantJoined(callback: (data: any) => void): void {
    if (this.connection) {
      this.connection.on('ParticipantJoined', callback)
    }
  }

  onVoteSubmitted(callback: (data: any) => void): void {
    if (this.connection) {
      this.connection.on('VoteSubmitted', callback)
    }
  }

  onVotesRevealed(callback: () => void): void {
    if (this.connection) {
      this.connection.on('VotesRevealed', callback)
    }
  }

  onNewRoundStarted(callback: () => void): void {
    if (this.connection) {
      this.connection.on('NewRoundStarted', callback)
    }
  }

  onSessionEnded(callback: () => void): void {
    if (this.connection) {
      this.connection.on('SessionEnded', callback)
    }
  }

  onParticipantLeft(callback: (data: any) => void): void {
    if (this.connection) {
      this.connection.on('ParticipantLeft', callback)
    }
  }

  off(eventName: string): void {
    if (this.connection) {
      this.connection.off(eventName)
    }
  }
}

const signalrService = new SignalRService()
export default signalrService
