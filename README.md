# Social Networking API

This is a .NET Web API for a social-style app. It includes JWT auth, profiles, a paged people feed, follows, mutual connections, bookmarks, direct messages, photos, hobby interests, and subscription tiers.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Optional: [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (`dotnet tool install --global dotnet-ef`)

## Run

```bash
dotnet restore
dotnet run
```

- API (Kestrel defaults): `http://localhost:5000` (see console for the exact URL)
- Swagger UI: `/swagger`

On first run, **`MigrateAsync()`** applies EF migrations (SQLite file from `ConnectionStrings:DefaultConnection`, default **`socialapp.db`**). In **Development**, optional demo data is seeded.

## Features (summary)

| Area | Notes |
|------|--------|
| **Account** | Register / login (JWT). New users get **`emailConfirmed: false`** until they confirm (see below). |
| **Email confirmation** | Signed token (48h). **`POST /api/account/confirm-email`**, **`POST /api/account/resend-confirmation`** (auth). |
| **Password reset** | Signed reset token (1h). **`POST /api/account/forgot-password`**, **`POST /api/account/reset-password`**. |
| **Rate limiting** | Policy-based limiter on auth + high-abuse actions. `429` when a policy is exceeded. |
| **Profile** | `knownAs`, bio, headline, profile links, city, country, job title, hobbies. **`emailConfirmed`** is returned on user payloads. |
| **Feed** | **`GET /api/users`** or **`GET /api/users/feed`** — paged; optional **`hobbyIds`** (comma-separated); excludes users you already follow. |
| **Discovery** | **`GET /api/users/search`** (q + hobby filter) and **`GET /api/users/suggestions`** (shared hobbies + mutuals + city scoring). |
| **Social graph** | Follow / unfollow, connections (mutual follows), following & followers lists (followers list gated by plan). |
| **Safety controls** | Block / mute users (`/api/block/{userId}`, `/api/mute/{userId}`) with feed/follow/message filtering. |
| **Bookmarks** | Save / remove bookmarked profiles. |
| **Messaging & photos** | 1:1 messages; upload photos and set main. |
| **Subscriptions** | Free / Plus / Premium (follow caps, followers list visibility, feed boost). |

## Email confirmation

### How it works

1. After **register**, the API sends a confirmation message (if sending fails, registration still succeeds and the failure is logged).
2. Without SMTP, the **`LoggingEmailSender`** writes the full message to **application logs** (Information).
3. The message includes a line **`CONFIRMATION_TOKEN:`** followed by the token. Send:

   `POST /api/account/confirm-email` with JSON `{ "token": "<paste>" }`.

4. **`App:PublicApiBaseUrl`** (optional) is embedded in the email text so clients know which host to call.

### Test it manually (no inbox)

1. Ensure **`Smtp:Host`** is empty so mail is **logged**, not sent via SMTP.
2. Run the API and **register** a user (Swagger or any HTTP client).
3. Confirm the JSON response includes **`"emailConfirmed": false`**.
4. In the **console**, find the log entry for the email body and copy the token after **`CONFIRMATION_TOKEN:`**.
5. Call **`POST /api/account/confirm-email`** with that token → **204**.
6. **Login** or fetch the profile again → **`emailConfirmed": true`**.

### Test it with automated tests

```bash
dotnet test API.Tests/API.Tests.csproj --filter "FullyQualifiedName~EmailVerificationIntegrationTests"
```

## Password reset

### How it works

1. Call **`POST /api/account/forgot-password`** with `{ "email": "user@example.com" }`.
2. The API always returns **204** to avoid account enumeration.
3. Without SMTP, the message body is logged and includes **`RESET_PASSWORD_TOKEN:`**.
4. Call **`POST /api/account/reset-password`** with:

   `{ "token": "<paste>", "newPassword": "Aa123456" }`

5. On success, the API returns **204** and the previous password no longer works.

### Run only reset tests

```bash
dotnet test API.Tests/API.Tests.csproj --filter "FullyQualifiedName~PasswordResetIntegrationTests"
```

### Test it with a real inbox

Set **`Smtp:Host`** (and port, credentials, SSL) plus **`Email:FromAddress`** in `appsettings` or user secrets. Restart the API, register, and use the token from the email the same way as above.

## Discovery: search and suggestions

### User search

Endpoint:

- `GET /api/users/search?q=&hobbyIds=&page=&pageSize=`

Behavior:

- Text match uses EF `Contains` on `userName`, `knownAs`, and `headline`.
- `hobbyIds` accepts comma-separated values (for example `1,3,5`).
- Results are paged and returned as `PagedResultDto<UserDto>`.
- Blocked users are excluded.

Example:

```http
GET /api/users/search?q=unity&hobbyIds=1,3&page=1&pageSize=20
Authorization: Bearer <token>
```

### Suggested accounts

Endpoint:

- `GET /api/users/suggestions?page=&pageSize=`

Current scoring:

- shared hobbies (highest weight)
- mutual connections
- same city

Also excluded from suggestions:

- yourself
- users you already follow
- blocked users
- users you muted

Results are sorted by score, then activity.

## Safety controls (block / mute)

### Block

- `POST /api/block/{userId}`
- `DELETE /api/block/{userId}`

When a block is created, mutual follows are removed and interaction is restricted in feed, follow actions, profile lookup, and messaging.

### Mute

- `POST /api/mute/{userId}`
- `DELETE /api/mute/{userId}`

Mute is a soft filter for your experience (for example discovery/feed visibility).

## Rate limiting

The API uses ASP.NET rate limiter policies on the highest-risk actions:

- `POST /api/account/register`
- `POST /api/account/login`
- `POST /api/follow/{userId}`
- `POST /api/messages`

When a limit is exceeded, the endpoint returns `429 Too Many Requests`.

## Subscriptions

Built-in plans:

- **Free**: up to 20 new follows per UTC day; followers list locked.
- **Plus**: unlimited follows; can see followers list.
- **Premium**: same as Plus with stronger feed placement.

Endpoints:

- `GET /api/subscriptions/plans`
- `GET /api/subscriptions/me` (auth)
- `POST /api/subscriptions/subscribe` (auth) — e.g. `{ "planId": 2, "durationDays": 30, "autoRenew": true, "renewalDays": 30 }`
- `POST /api/subscriptions/auto-renew` (auth) — `{ "enabled": true }`
- `POST /api/subscriptions/cancel` (auth)

**Note:** `GET /api/users/following?list=followers` requires a plan that includes the followers list entitlement.

## Useful HTTP routes

**Public**

- `POST /api/account/register`
- `POST /api/account/login`
- `POST /api/account/confirm-email` — body `{ "token": "..." }`
- `POST /api/account/forgot-password` — body `{ "email": "..." }`
- `POST /api/account/reset-password` — body `{ "token": "...", "newPassword": "..." }`
- `GET /api/subscriptions/plans`

**Auth (Bearer JWT)**

- `POST /api/account/resend-confirmation`
- `GET /api/users`, `GET /api/users/feed` — query: `pageNumber`, `pageSize`, `hobbyIds`, `orderBy`
- `GET /api/users/search` — query: `q`, `hobbyIds`, `page`, `pageSize`
- `GET /api/users/suggestions` — query: `page`, `pageSize`
- `GET /api/users/{username}`, `PUT /api/users`
- `GET /api/users/hobbies`, `GET /api/users/interests`
- `GET /api/users/connections`
- `GET /api/users/following?list=following` or `list=followers`
- `POST` / `DELETE /api/follow/{userId}`
- `POST` / `DELETE /api/block/{userId}`
- `POST` / `DELETE /api/mute/{userId}`
- `POST` / `DELETE /api/bookmarks/{userId}`
- `POST /api/messages`, `GET /api/messages`, `GET /api/messages/thread/{recipientId}`, etc.
- `POST /api/photos`, `PUT /api/photos/{id}/set-main`, `DELETE /api/photos/{id}`
- `DELETE /api/account`

**Development only**

- `GET /api/users/all` — any authenticated user in Development; requires **Admin** role in other environments.

## Configuration

Use **`appsettings.json`** / **`appsettings.Development.json`** (or environment variables / user secrets):

| Key | Purpose |
|-----|--------|
| `ConnectionStrings:DefaultConnection` | SQLite (default file `socialapp.db`) |
| `TokenKey` | JWT signing key (64+ chars). Also used to sign email tokens unless `EmailConfirmation:SigningKey` is set. |
| `EmailConfirmation:SigningKey` | Optional separate key for email confirmation HMAC. |
| `PasswordReset:SigningKey` | Optional separate key for password reset HMAC. |
| `App:PublicApiBaseUrl` | Optional base URL shown in confirmation emails (e.g. `https://localhost:7158`). |
| `Email:FromAddress`, `Email:FromName` | SMTP sender display. |
| `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Password`, `Smtp:UseSsl` | If **`Host`** is set, MailKit sends mail; if empty, emails are **log-only**. |
| `AdminUserNames` | Optional comma-separated usernames that receive the Admin role in JWTs. |

## Tests

Integration tests use **xUnit** + **`WebApplicationFactory<Program>`** with a **temporary SQLite** database (`Testing` environment, no dev seeder).

```bash
dotnet test API.Tests/API.Tests.csproj
```

Run only email verification tests:

```bash
dotnet test API.Tests/API.Tests.csproj --filter "FullyQualifiedName~EmailVerificationIntegrationTests"
```

Run only discovery tests:

```bash
dotnet test API.Tests/API.Tests.csproj --filter "FullyQualifiedName~SocialProfileAndFollowIntegrationTests"
```

On Windows, **stop a running `API.exe`** if the build fails because **`API.dll`** is locked.

## EF Core migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

The host also runs **`Database.MigrateAsync()`** on startup.

