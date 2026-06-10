# Tide Monitor

A full-stack application for monitoring tide levels and coastal conditions. Built with a .NET Web API backend and a modern frontend dashboard.

## Tech Stack

- **Backend:** .NET (C#) Web API
- **Frontend:** TypeScript/JavaScript (Vite-based dashboard)
- **Architecture:** Clean Architecture with Core, Infrastructure, and API layers

## Getting Started

1. Ensure .NET SDK and Node.js are installed
2. Run `start-dev.bat` to launch both backend and frontend:
   - Backend API: http://localhost:5000
   - Frontend: http://localhost:5173

Or start manually:

```bash
# Backend
cd backend/TideMonitor.Api
dotnet run

# Frontend (separate terminal)
cd frontend/tide-dashboard
npm install
npm run dev
```
