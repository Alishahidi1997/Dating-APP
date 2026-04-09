# Social Networking API


.NET API for a Social Networking App.  
It has auth, profiles, likes, matches, messages, photos, hobbies, and subscription tiers.

## Run It

```bash
dotnet restore
dotnet run
```

- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`

## What It Does

- JWT login/register
- Profile edit (`knownAs`, bio, city, country, `jobTitle`, hobbies)
- Discovery feed with filters + paging
- Likes + matches
- Messaging (inbox/outbox/thread/read/delete)
- Photo upload + set main + delete
- Account delete

## Subscriptions

Three plans are built in:

- **Free**: up to 20 new likes per UTC day
- **Plus**: unlimited likes + can see who liked you
- **Premium**: everything in Plus + boosted discovery ranking

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

- Like limit applies only to Free users.
- `GET /api/users/likes?predicate=likedby` requires Plus/Premium.

## Useful Endpoints

Public:

- `POST /api/account/register`
- `POST /api/account/login`
- `GET /api/subscriptions/plans`

Auth required:

- `GET /api/users/discovery`
- `GET /api/users/{username}`
- `PUT /api/users`
- `GET /api/users/hobbies`
- `GET /api/users/matches`
- `GET /api/users/likes?predicate=liked|likedby`
- `POST /api/likes/{targetUserId}`
  - Optional body: `{ "photoId": 5 }` (photo must belong to target user)
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

- `ConnectionStrings:DefaultConnection`
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


