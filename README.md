# Social Networking API


.NET API for a social networking app.  
It has auth, profiles, feed, follows, connections, bookmarks, messages, photos, hobbies (interests), and subscription tiers.

## Run It

```bash
dotnet restore
dotnet run
```

- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`

## What It Does

- JWT login/register
- Profile edit (`knownAs`, bio, headline, profile links, city, country, `jobTitle`, hobbies)
- Home feed with optional hobby-topic filter + paging (`GET /api/users` or `GET /api/users/feed`)
- Follow / unfollow, mutual connections, bookmarks
- Messaging (inbox/outbox/thread/read/delete)
- Photo upload + set main + delete
- Account delete

## Subscriptions

Three plans are built in:

- **Free**: up to 20 new follows per UTC day; followers list locked
- **Plus**: unlimited follows + can see your followers list
- **Premium**: everything in Plus + stronger feed placement

Plan endpoints:

- `GET /api/subscriptions/plans`
- `GET /api/subscriptions/me` (auth)
- `POST /api/subscriptions/subscribe` (auth)
  - Example body: `{ "planId": 2, "durationDays": 30, "autoRenew": true, "renewalDays": 30 }`
- `POST /api/subscriptions/auto-renew` (auth)
  - Body: `{ "enabled": true }`
- `POST /api/subscriptions/cancel` (auth)
  - Cancels renewal; plan stays active until current expiry

Notes:

- Follow limit applies only to Free users.
- `GET /api/users/following?list=followers` requires Plus/Premium.

## Useful Endpoints

Public:

- `POST /api/account/register`
- `POST /api/account/login`
- `GET /api/subscriptions/plans`

Auth required:

- `GET /api/users` or `GET /api/users/feed` (paged feed)
- `GET /api/users/{username}`
- `PUT /api/users`
- `GET /api/users/hobbies` or `GET /api/users/interests`
- `GET /api/users/connections` (mutual follows)
- `GET /api/users/following?list=following|followers`
- `POST` / `DELETE /api/follow/{userId}`
- `POST` / `DELETE /api/bookmarks/{userId}`
- `POST /api/messages`
- `GET /api/messages`
- `GET /api/messages/thread/{recipientId}`
- `PUT /api/messages/{id}/read`
- `DELETE /api/messages/{id}`
- `POST /api/photos`
- `PUT /api/photos/{id}/set-main`
- `DELETE /api/photos/{id}`
- `DELETE /api/account`

Admin/dev note:

- `GET /api/users/all` is open to any authenticated user in Development.
- In non-Development environments, it needs Admin role.

## Config

Set in `appsettings.json` / `appsettings.Development.json`:

- `ConnectionStrings:DefaultConnection` (default SQLite file: `socialapp.db`)
- `TokenKey` (64+ chars)
- `AdminUserNames` (optional, comma-separated)

## Tests

Uses xUnit + `WebApplicationFactory` against a temp SQLite file (`ASPNETCORE_ENVIRONMENT=Testing`, no dev seeder).

```bash
dotnet test API.Tests/API.Tests.csproj
```

Stop the running API first if Windows locks `API.dll` during build.

## EF Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

