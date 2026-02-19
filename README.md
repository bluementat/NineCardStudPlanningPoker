# Nine-Card Stud Planning Poker

A beautiful, casino-themed Planning Poker application built with .NET 8 backend and React frontend, featuring real-time updates via SignalR.

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
- React 18 with TypeScript
- React Router for navigation
- @microsoft/signalr for SignalR client
- Vite for build tooling
- Axios for HTTP requests

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

   The API will be available at `http://localhost:5076` (or the port specified in launchSettings.json)

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
   - Select a card value (Fibonacci sequence: 0, 1, 2, 3, 5, 8, 13, 21, ?)
   - Your vote is submitted automatically

4. **Reveal Votes**:
   - The session creator can click "Reveal Votes" when all participants have voted
   - All votes are displayed with statistics (average, min, max)

## Azure Deployment

You can deploy in either of two ways: as **Azure App Service + Static Web App**, or by **containerizing both projects and running them as Azure Container Apps**.

### Option 1: App Service and Static Web App

#### Backend (Azure App Service)
1. Create an Azure App Service (Windows or Linux)
2. Deploy the backend:
   ```bash
   cd backend/PlanningPoker.Api
   dotnet publish -c Release
   # Deploy using Azure CLI, VS Code extension, or Visual Studio
   ```
3. Update CORS settings in `Program.cs` to include your frontend URL
4. If using a custom database, set the connection string in App Service Configuration

#### Frontend (Azure Static Web Apps or App Service)
- **Azure Static Web Apps**: Create a Static Web App, connect your repo, and set app location `frontend`, output `dist`, build command `npm run build`.
- **Azure App Service**: Run `npm run build` in `frontend`, then deploy the `dist` folder.

Configure the frontend’s API base URL for production (e.g. environment variables or build-time config in `vite.config.js`).

### Option 2: Azure Container Apps

Both the backend and frontend can be containerized and run as **Azure Container Apps**.

1. **Backend**: Build and push the backend image (e.g. from a Dockerfile in `backend/PlanningPoker.Api`), then create a Container App using that image. Set the app URL in CORS and, if needed, the database connection string as environment variables.
2. **Frontend**: Build and push the frontend image (e.g. from the Dockerfile in `frontend`), then create a second Container App. Configure the API base URL so the frontend calls your backend Container App URL.

Deploy both container apps, ensure the backend allows the frontend’s URL in CORS, and point the frontend at the backend’s URL.

### Environment Variables

For production, set the following:

**Backend**:
- Target Port is 8080
- Internal Ingress is recommended. This allows communication only from within the container environment - Specifically from the frontend.
- You'll need the FQDN of the backend for the frontend configuration.

**Frontend**: 
- Target Port is 80
- Use External Ingress to make the frontend reachable from the internet.
- In the Enviornment Variables, set API_URL to the backend FQDN.

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

## In-Memory Database Schema

- **Sessions**: SessionId, PIN, SessionName, CreatedAt, Status
- **Participants**: ParticipantId, SessionId, Name, JoinedAt
- **Votes**: VoteId, SessionId, ParticipantId, CardValue, VotedAt

## Development Notes

- The In-Memory Database is automatically created on first run using `EnsureCreated()`
- For production, use migrations: `dotnet ef migrations add InitialCreate` and `dotnet ef database update`
- The frontend Vite dev server proxies `/api` and `/planningpokerhub` to the backend (default `http://localhost:5076`); adjust `vite.config.js` if your backend runs on a different port

## License

This project is open source and available for use.
