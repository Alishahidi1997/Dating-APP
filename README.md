# Dating App API

A RESTful dating app backend built with .NET. Supports registration, authentication, profiles, photo uploads, swipe-like functionality, matches, and messaging.

---

## Features

### Authentication
- **Register** – Create account with username, email, password, gender, preferences, bio, and location
- **Login** – JWT-based authentication (7-day token validity)
- Password hashing with BCrypt
- Age restriction (18+ only)

### User Profile
- **Bio** – Short description (up to 500 characters)
- **Known As** – Display name
- **Gender** – Male, female, or other
- **Looking For** – Preferred gender
- **Location** – City and country
- **Date of birth** – Used for age calculation and filtering

### Photos
- Upload profile pictures (jpg, jpeg, png, gif, webp)
- Maximum 5MB per image
- Set main/profile photo
- Delete non-main photos

### Discovery & Swiping
- **Discovery** – Browse users with filters:
  - Gender, min/max age
  - Pagination (page size up to 50)
  - Order by last active or created date
- **Like (Swipe Right)** – Like another user
- **Likes** – View users you liked (`predicate=liked`) or who liked you (`predicate=likedby`)
- **Matches** – Mutual likes only

### Messaging
- Send direct messages between users
- Inbox, outbox, and unread views
- Message thread with a specific user
- Soft delete (per-user)
- Mark messages as read

### Admin
- **`GET /api/users/all`** – List every user (admin-only; returns `403` if the JWT does not include role `Admin`)
- **Who is an admin?**
  - Usernames listed in `AdminUserNames` (comma-separated in config) get the `Admin` role on login/register.
  - Or set `IsAdmin = 1` on the `Users` row in SQLite; log in again for a new token with the role.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or .NET 8/9)

---

## How to Run

### 1. Clone or open the project

```bash
cd c:\Users\shahi\Desktop\.net\API
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Run the API

```bash
dotnet run
```

The API starts on:
- **HTTPS:** https://localhost:5001
- **HTTP:** http://localhost:5000 (if configured)

### 4. Run with hot reload (during development)

```bash
dotnet watch run
```

---

## Configuration

Edit `appsettings.json` or `appsettings.Development.json`:

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:DefaultConnection` | SQLite connection string (default: `Data Source=datingapp.db`) |
| `TokenKey` | Secret key for JWT signing (must be 64+ characters for HMAC-SHA512) |
| `AdminUserNames` | Comma-separated usernames that receive the `Admin` role in JWT (optional; case-insensitive) |

**Admin access:** Users with `Users.IsAdmin = 1` in the database also receive the `Admin` role. After changing `IsAdmin` or `AdminUserNames`, log in again so a new JWT includes the role.

Example:

```json
{
  "AdminUserNames": "admin,support"
}
```

Example for production:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=production.db"
  },
  "TokenKey": "your-production-secret-at-least-64-characters-for-hmac-sha512"
}
```

---

## API Reference

Base URL: `https://localhost:5001/api`

### Public Endpoints

#### Register
```http
POST /api/account/register
Content-Type: application/json

{
  "userName": "johndoe",
  "email": "john@example.com",
  "password": "MyP@ssw0rd",
  "gender": "male",
  "lookingFor": "female",
  "dateOfBirth": "1995-05-15",
  "bio": "Love hiking and coffee",
  "knownAs": "John",
  "city": "New York",
  "country": "USA"
}
```

#### Login
```http
POST /api/account/login
Content-Type: application/json

{
  "userName": "johndoe",
  "password": "MyP@ssw0rd"
}
```

**Response (Register & Login):**
```json
{
  "user": {
    "id": 1,
    "userName": "johndoe",
    "knownAs": "John",
    "age": 29,
    "bio": "Love hiking and coffee",
    "gender": "male",
    "lookingFor": "female",
    "city": "New York",
    "country": "USA",
    "photoUrl": null,
    "lastActive": "2025-03-23T12:00:00Z",
    "created": "2025-03-23T10:00:00Z",
    "photos": []
  },
  "token": "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9..."
}
```

### Protected Endpoints (require `Authorization: Bearer <token>`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| DELETE | `/account` | Delete currently logged-in account |
| GET | `/users/discovery` | Discovery feed (query: `gender`, `minAge`, `maxAge`, `pageNumber`, `pageSize`, `orderBy`) |
| GET | `/users/all` | **Admin only.** List all users (same shape as profile DTOs; not paginated). Requires JWT with role `Admin`. |
| GET | `/users/{username}` | Get user profile |
| PUT | `/users` | Update own profile |
| GET | `/users/matches` | Mutual matches |
| GET | `/users/likes?predicate=liked` | Users you liked |
| GET | `/users/likes?predicate=likedby` | Users who liked you |
| POST | `/photos` | Upload photo (multipart form, field: `file`) |
| DELETE | `/photos/{id}` | Delete photo |
| PUT | `/photos/{id}/set-main` | Set main photo |
| POST | `/likes/{targetUserId}` | Like a user |
| POST | `/messages` | Send message |
| GET | `/messages` | Get messages (query: `container=Inbox|Outbox|Unread`, `pageNumber`, `pageSize`) |
| GET | `/messages/thread/{recipientId}` | Get message thread |
| DELETE | `/messages/{id}` | Delete message |
| PUT | `/messages/{id}/read` | Mark as read |

`DELETE /account` requires a valid bearer token and deletes the authenticated user account.

### Admin: list all users

Requires `Authorization: Bearer <token>` where the token was issued for a user with role **`Admin`**. Non-admin users receive **403 Forbidden**.

```http
GET /api/users/all
Authorization: Bearer <token>
```

**Example (cURL):**

```bash
curl -X GET "https://localhost:5001/api/users/all" \
  -H "Authorization: Bearer ADMIN_JWT_TOKEN"
```

**Promote a user to admin in SQLite (then log in again):**

```sql
UPDATE Users SET IsAdmin = 1 WHERE UserName = 'youradmin';
```

---

## Project Structure

```
API/
├── Controllers/          # API endpoints
│   ├── AccountController.cs
│   ├── UsersController.cs
│   ├── PhotosController.cs
│   ├── LikesController.cs
│   └── MessagesController.cs
├── Entities/             # Database models
│   ├── AppUser.cs
│   ├── Photo.cs
│   ├── UserLike.cs
│   └── Message.cs
├── Models/Dto/           # Request/response DTOs
├── Services/             # Business logic
├── Data/                 # DbContext and repositories
├── Migrations/           # EF Core migrations
├── wwwroot/images/       # Uploaded photos
├── appsettings.json
└── Program.cs
```

---

## Technology Stack

- **.NET 10** – Web API
- **Entity Framework Core** – ORM
- **SQLite** – Database
- **JWT Bearer** – Authentication
- **BCrypt.Net-Next** – Password hashing

---

## Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName
```

Apply migrations (done automatically on startup):
```bash
dotnet ef database update
```

---

## Quick Test with cURL

```bash
# Register
curl -X POST https://localhost:5001/api/account/register \
  -H "Content-Type: application/json" \
  -d '{"userName":"test","email":"test@test.com","password":"Test123!","gender":"male","lookingFor":"female","dateOfBirth":"1990-01-01"}'

# Login (use token from response)
curl -X POST https://localhost:5001/api/account/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"test","password":"Test123!"}'

# Get discovery (replace TOKEN with your JWT)
curl -X GET "https://localhost:5001/api/users/discovery" \
  -H "Authorization: Bearer TOKEN"

# List all users (admin only; replace ADMIN_TOKEN with a JWT for an admin user)
curl -X GET "https://localhost:5001/api/users/all" \
  -H "Authorization: Bearer ADMIN_TOKEN"
```

---


