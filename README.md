# 🎬 Movie API (.NET Core)

A backend API for managing movies, episodes, and video content, built with ASP.NET Core using a layered architecture.

---

## 🚀 Overview

This project provides a RESTful API for:

- Managing movies, categories, episodes, and videos
- Handling authentication using JWT and refresh tokens
- Logging system activities using background jobs

---

## 🧱 Architecture

The project is organized into 4 layers:

- MovieApi.Api → Presentation (Controllers, Middleware)
- MovieApi.Application → Business Logic (Services, DTOs)
- MovieApi.Domain → Core Entities
- MovieApi.Infrastructure → Database, Services, Hangfire


### Why this structure?

- Separation of concerns
- Easier maintenance and scalability
- Independent business logic

---

## 🔐 Authentication

The system uses JWT with refresh tokens.

### Flow:

1. User logs in → receives:
   - Access Token (short-lived)
   - Refresh Token (long-lived)

2. When access token expires:
   - Client calls `/auth/refresh`
   - Backend validates refresh token
   - Issues new tokens
   - Revokes old refresh token

### Security:

- Refresh tokens stored in database
- Token rotation is applied

---

## 🎬 Core Features

### Movies
- CRUD operations
- Assign categories

### Episodes
- Each movie has multiple episodes
- Episode number must be unique per movie

### Videos
- Each episode has multiple video URLs
- Only one video can be marked as default

### Categories
- Unique slug per category

---

## ⚙️ Background Jobs (Hangfire)

Used for:

- Audit logging
- Notifications

### Why Hangfire?

- Avoid blocking request processing
- Handle async tasks reliably

---

## 🧾 Audit Logging

Tracks actions:

- CREATE
- UPDATE
- DELETE
- SET_DEFAULT

Processed asynchronously via Hangfire.

---

## 🗄 Database

- MySQL
- Entity Framework Core (Code First)

---

## 🧠 Key Technical Concepts

### Change Tracking

EF Core tracks entity states:

- Added → INSERT
- Modified → UPDATE
- Deleted → DELETE
- Unchanged → No action
- Detached → Not tracked

---

### Tracking vs NoTracking

- Tracking → used for updates
- `AsNoTracking()` → optimized for read-only queries

---

## 🚀 Deployment

- Dockerized application
- Deployed on Railway
- Uses managed MySQL

---

## ⚙️ Environment Variables

- ASPNETCORE_ENVIRONMENT=Production
- ConnectionStrings__Default=server=; port=; database=; user=; password=;SslMode=None; AllowPublicKeyRetrieval=True; Allow User Variables=true;
- JwtSettings__Issuer=movieapi
- JwtSettings__Audience=movieapi-client
- JwtSettings__Key=your_secret_key

---

## 🧪 Example Endpoints

- `POST /auth/login`
- `POST /auth/refresh`
- `GET /movies`
- `POST /movies`
- `POST /episodes`
- `POST /videos`

---

## ⚠️ Limitations

- Audit logging not applied to authentication yet
- Logging system is basic

---

## 👨‍💻 Author

This project was built as a learning project focusing on:

- Clean architecture
- Authentication & security
- Background processing
- Real-world deployment
