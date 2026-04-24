# MentoraX Backend

MentoraX is a learning and habit-building platform based on **spaced repetition**, **study planning**, and **progress tracking**.

This repository contains the **.NET backend services** that power the MentoraX ecosystem.

---

## 🚀 Features

* User authentication (JWT)
* Learning materials management
* Study plan creation
* Session scheduling
* Session tracking & completion
* Progress & streak calculation
* Background worker for session monitoring
* Clean Architecture (CQRS + layered design)

---

## 🧱 Architecture

The project follows **Clean Architecture**:

```
src/
├── MentoraX.Api             → REST API layer
├── MentoraX.Application     → Business logic (CQRS)
├── MentoraX.Domain          → Core domain models
├── MentoraX.Infrastructure  → EF Core, DB, external services
├── MentoraX.Worker          → Background processing
```

---

## 🧪 Tests

```
tests/
└── MentoraX.Tests
```

---

## ⚙️ Technologies

* .NET 8+
* Entity Framework Core
* SQL Server
* CQRS Pattern
* Worker Services
* REST API

---

## 🔧 Setup

### 1. Clone repo

```
git clone https://github.com/ethemserce/MentoraX.git
cd MentoraX
```

---

### 2. Configure database

Update:

```
src/MentoraX.Api/appsettings.json
```

Example:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=MentoraXDb;Trusted_Connection=True;"
}
```

---

### 3. Run migrations

```
dotnet ef database update --project src/MentoraX.Infrastructure --startup-project src/MentoraX.Api
```

---

### 4. Run API

```
dotnet run --project src/MentoraX.Api
```

Swagger:

```
http://localhost:5107/swagger
```

---

## 🔗 API Endpoints (Core)

### Auth

* POST `/api/auth/login`

### Materials

* GET `/api/materials`
* POST `/api/materials`

### Study Plans

* POST `/api/study-plans`
* GET `/api/study-plans`
* GET `/api/study-plans/{id}`

### Sessions

* POST `/api/sessions/start`
* POST `/api/sessions/complete`

### Dashboard

* GET `/api/mobile/dashboard`
* GET `/api/mobile/progress-summary`

---

## 🔄 Background Worker

Worker service periodically:

* scans sessions
* detects due sessions
* prepares reminders (future extension: push notifications)

---

## 📱 Mobile App

Flutter mobile application is located in:

```
mobile/mentorax
```

or in a separate repository:
https://github.com/ethemserce/mentorax_mobile

---

## 🧠 Future Improvements

* Push notifications (FCM)
* Adaptive learning algorithms
* AI-based recommendations
* Multi-device sync
* Analytics dashboard

---

## 👤 Author

Ethem Serçe
