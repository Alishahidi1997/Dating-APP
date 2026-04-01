# Dating App API

Simple .NET API for a dating app with JWT auth, profiles, likes/matches, photos, messages, hobbies, and admin tools.

## Quick Start

```bash
dotnet restore
dotnet run
```

Default URLs:
- `http://localhost:5000`
- `https://localhost:5001`

Swagger:
- `http://localhost:5000/swagger`

## Main Features

- Register / login with JWT
- Profile update (bio, knownAs, city, country, jobTitle, hobbies)
- Discovery with filters (age, gender, paging, order)
- Likes + matches (max **20 new likes per UTC day** per user; **429** when over the limit)
- Each like can record **which of the target’s photos** was shown (`photoId` in body, or defaults to their main photo)
- Messages (inbox/outbox/unread, thread, read, delete)
- Photo upload / delete / set-main
- Account delete

## Config

Set these in `appsettings.json` / `appsettings.Development.json`:

- `ConnectionStrings:DefaultConnection`
- `TokenKey` (for HMAC-SHA512, keep it 64+ chars)
- `AdminUserNames` (comma-separated, optional)

Admin notes:
- In Development, `GET /api/users/all` is available to any authenticated user.
- In non-Development environments, it requires Admin role.

## Important Endpoints

Public:
- `POST /api/account/register`
- `POST /api/account/login`

Protected (Bearer token required):
- `GET /api/users/discovery`
- `GET /api/users/{username}`
- `PUT /api/users`
- `GET /api/users/hobbies`
- `GET /api/users/matches`
- `GET /api/users/likes?predicate=liked|likedby` — returns `{ member, likedPhoto }[]` (`likedPhoto` is the target’s photo for that like; for `likedby` it’s **your** photo they saw, or `null` if unknown)
- `GET /api/users/all` (rules above)
- `POST /api/likes/{targetUserId}` — optional body `{ "photoId": <int> }` (must be one of that user’s photos); empty body OK
- `POST /api/messages`
- `GET /api/messages`
- `GET /api/messages/thread/{recipientId}`
- `PUT /api/messages/{id}/read`
- `DELETE /api/messages/{id}`
- `POST /api/photos`
- `PUT /api/photos/{id}/set-main`
- `DELETE /api/photos/{id}`
- `DELETE /api/account`

## Minimal cURL Flow

```bash
# Register
curl -X POST http://localhost:5000/api/account/register \
  -H "Content-Type: application/json" \
  -d "{\"userName\":\"test1\",\"email\":\"test1@test.com\",\"password\":\"Test123A\",\"gender\":\"male\",\"lookingFor\":\"any\",\"dateOfBirth\":\"1995-01-01\"}"

# Login
curl -X POST http://localhost:5000/api/account/login \
  -H "Content-Type: application/json" \
  -d "{\"userName\":\"test1\",\"password\":\"Test123A\"}"

# Use returned token
curl -X GET http://localhost:5000/api/users/discovery \
  -H "Authorization: Bearer TOKEN"

# Like someone, recording which of their photos you saw (optional)
curl -X POST http://localhost:5000/api/likes/2 \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"photoId\":5}"
```

## Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```


