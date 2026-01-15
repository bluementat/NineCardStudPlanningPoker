# Planning Poker Casino

A beautiful, casino-themed Planning Poker application built with .NET 8 backend and Vue.js frontend, featuring real-time updates via SignalR.

## Features

- **PIN-Based Authentication**: Create sessions with 6-digit PINs (Kahoot-style)
- **Real-Time Updates**: SignalR-powered live updates for votes and participants
- **Casino Theme**: Beautiful green felt background with playing card animations
- **Animated Cards**: Flip animations and smooth transitions
- **Session Management**: Create, join, and manage Planning Poker sessions
- **Vote Reveal**: Animated reveal of all votes with statistics

## Technology Stack

### Backend
- .NET 8 Web API
- SignalR for real-time communication
- Entity Framework Core with SQL Server
- CORS configuration for frontend

### Frontend
- Vue.js 3 (Composition API)
- @microsoft/signalr for SignalR client
- Vite for build tooling
- Vue Router for navigation

## Project Structure

```
NineCardStudPokerPlanning/
├── backend/
│   ├── PlanningPoker.Api/
│   │   ├── Controllers/
│   │   ├── Hubs/
│   │   ├── Models/
│   │   ├── Data/
│   │   ├── Services/
│   │   └── Program.cs
│   └── PlanningPoker.Api.sln
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   ├── views/
│   │   ├── services/
│   │   └── assets/
│   ├── package.json
│   └── vite.config.js
└── README.md
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js 18+ and npm
- SQL Server (LocalDB or full SQL Server instance)

### Backend Setup

1. Navigate to the backend directory:
   ```bash
   cd backend/PlanningPoker.Api
   ```

2. Update the connection string in `appsettings.json` if needed (default uses LocalDB)

3. Restore packages and run:
   ```bash
   dotnet restore
   dotnet run
   ```

   The API will be available at `https://localhost:5000` (or the port specified in launchSettings.json)

### Frontend Setup

1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```

   The frontend will be available at `http://localhost:5173`

## Usage

1. **Create a Session**:
   - Click "Create Session" on the home page
   - Enter a session name
   - Copy the generated 6-digit PIN
   - Share the PIN with your team

2. **Join a Session**:
   - Click "Join Session" on the home page
   - Enter the 6-digit PIN
   - Enter your name
   - Click "Join Session"

3. **Vote**:
   - Select a card value (Fibonacci sequence: 0, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, or ?)
   - Your vote is submitted automatically

4. **Reveal Votes**:
   - The session creator can click "Reveal Votes" when all participants have voted
   - All votes are displayed with statistics (average, min, max)

## Azure Deployment

### Backend (Azure App Service)

1. Create an Azure App Service (Windows or Linux)
2. Create an Azure SQL Database
3. Update the connection string in App Service Configuration
4. Deploy the backend:
   ```bash
   cd backend/PlanningPoker.Api
   dotnet publish -c Release
   # Deploy using Azure CLI, VS Code extension, or Visual Studio
   ```

5. Update CORS settings in `Program.cs` to include your frontend URL

### Frontend (Azure Static Web Apps or App Service)

#### Option 1: Azure Static Web Apps
1. Create an Azure Static Web App
2. Connect to your Git repository
3. Configure build settings:
   - App location: `frontend`
   - Output location: `dist`
   - Build command: `npm run build`

#### Option 2: Azure App Service
1. Build the frontend:
   ```bash
   cd frontend
   npm run build
   ```
2. Deploy the `dist` folder to Azure App Service

3. Update the API endpoint in `vite.config.js` or use environment variables

### Environment Variables

For production, set the following:

**Backend (App Service Configuration)**:
- `ConnectionStrings:DefaultConnection`: Azure SQL Database connection string

**Frontend**:
- Update `vite.config.js` proxy settings or use environment variables for API URL

## API Endpoints

- `POST /api/sessions` - Create a new session
- `GET /api/sessions/{pin}` - Get session details
- `POST /api/sessions/{pin}/participants` - Join a session
- `POST /api/sessions/{pin}/votes` - Submit a vote
- `POST /api/sessions/{pin}/reveal` - Reveal all votes
- `GET /api/sessions/{pin}/results` - Get voting results

## SignalR Hub

- Endpoint: `/planningpokerhub`
- Events:
  - `ParticipantJoined` - When a new participant joins
  - `VoteSubmitted` - When a vote is submitted
  - `VotesRevealed` - When votes are revealed

## Database Schema

- **Sessions**: SessionId, PIN, SessionName, CreatedAt, Status
- **Participants**: ParticipantId, SessionId, Name, JoinedAt
- **Votes**: VoteId, SessionId, ParticipantId, CardValue, VotedAt

## Development Notes

- The database is automatically created on first run using `EnsureCreated()`
- For production, use migrations: `dotnet ef migrations add InitialCreate` and `dotnet ef database update`
- CORS is configured for local development ports (5173, 3000, 8080)
- Update CORS origins in `Program.cs` for production

## License

This project is open source and available for use.
