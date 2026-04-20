# MentoraX

MentoraX is a planned learning and spaced repetition platform built with a modular .NET architecture.

## Solution structure

- `MentoraX.Api` → REST API
- `MentoraX.Application` → use cases, CQRS, DTOs, abstractions
- `MentoraX.Domain` → entities, enums, domain rules
- `MentoraX.Infrastructure` → EF Core, persistence, implementations
- `MentoraX.Worker` → background jobs for reminders and due session checks
- `MentoraX.Tests` → starter test project

## Main capabilities in this starter

- Create learning materials
- Generate study plans
- Compute spaced repetition sessions
- List due sessions
- Mark a session as completed
- Worker job that scans upcoming and overdue sessions

## Notes

This environment does not include the .NET SDK, so I could not compile the project here.
The codebase is organized to be compile-ready in a normal .NET 10 SDK environment, but you should run restore/build locally:

```bash
dotnet restore
dotnet build
dotnet ef database update --project src/MentoraX.Infrastructure --startup-project src/MentoraX.Api
dotnet run --project src/MentoraX.Api
```

## Suggested local setup

1. Install .NET 10 SDK preview/stable matching your machine.
2. Update the SQL Server connection string in:
   - `src/MentoraX.Api/appsettings.Development.json`
   - `src/MentoraX.Worker/appsettings.Development.json`
3. Apply migrations.
4. Start API and Worker.

## Initial API endpoints

- `POST /api/materials`
- `GET /api/materials`
- `POST /api/study-plans`
- `GET /api/study-plans/{id}`
- `GET /api/study-sessions/due?userId={guid}`
- `POST /api/study-sessions/{sessionId}/complete`

## Next recommended steps

- Add authentication and authorization
- Add file upload storage
- Add AI orchestration module
- Add notification providers
- Add frontend (Next.js / mobile)

- ### Completed
- Clean Architecture
- CQRS-based application layer
- JWT authentication
- Global exception handling
- Validation pipeline
- Adaptive learning engine
- Mobile-first API endpoints

### Mobile API
- GET `/api/mobile/dashboard`
- GET `/api/mobile/study-sessions/next`
- POST `/api/mobile/study-sessions/{id}/start`
- POST `/api/mobile/study-sessions/{id}/complete`
- GET `/api/mobile/progress/summary`
- POST `/api/mobile/devices`

### Main Domain Concepts
- User
- LearningMaterial
- StudyPlan
- StudySession
- StudyProgress
- MobileDevice

### Next Phase
- Flutter mobile client
- Push notifications
- AI-assisted learning flows
