# Project roadmap

This document turns the social API from a **people graph + DMs + media** foundation into a fuller product. Each section lists **what to build**, **why it matters**, **rough scope** (entities, endpoints, services), and **dependencies**. Order phases roughly by leverage: ship small vertical slices before large platforms (real-time, payments).

**Current baseline (already shipped):** JWT auth, profiles (bio, headline, links, location, job, hobbies), paged feed of non-followed users, follow/unfollow, mutual connections, bookmarks, 1:1 messages, photos, subscription tiers (follow caps, followers list, feed boost).

---

## Phase 0 — Hardening (before new surface area)

| Feature | Goal | Scope | Notes |
|--------|------|-------|--------|
| **Email verification** | Reduce fake accounts | **Done:** `EmailConfirmed` on user, HMAC-signed token (48h), `POST /api/account/confirm-email`, `POST /api/account/resend-confirmation` (auth). Logging sender when `Smtp:Host` empty; MailKit when configured. | Optional: `EmailConfirmation:SigningKey`, `App:PublicApiBaseUrl` |
| **Password reset** | Standard account recovery | **Done:** `POST /api/account/forgot-password` + `POST /api/account/reset-password`, HMAC-signed reset token (1h), non-enumerating forgot flow | Same mail infra as above; optional `PasswordReset:SigningKey` |
| **Rate limiting** | Abuse resistance | **Done:** ASP.NET rate limiter policies on `POST /api/account/register`, `POST /api/account/login`, `POST /api/follow/{userId}`, `POST /api/messages` with `429` rejection | Middleware policies: `AuthEndpoints`, `FollowEndpoint`, `MessageSendEndpoint` |
| **Block & mute** | Safety + feed quality | **Done:** `UserBlock` + `UserMute` tables; `POST/DELETE /api/block/{userId}`, `POST/DELETE /api/mute/{userId}`; block removes mutual follows + mutes between users; feed excludes block (either way) + mute (you muted them); follow/DM/thread/inbox/outbox filtered on block; profile `GET /api/users/{username}` returns 404 when blocked pair | Migration `AddUserBlockAndUserMute` |

**Exit criteria:** New users can verify email; resets work; obvious spam paths throttled; blocked users cannot interact.

---

## Phase 1 — Discovery & search

| Feature | Goal | Scope | Notes |
|--------|------|-------|--------|
| **User search** | Find people | `GET /api/users/search?q=&hobbyIds=&page=` | Full-text optional later; start with `LIKE` / EF `Contains` on username, knownAs, headline |
| **Suggested accounts** | Onboarding + growth | **Done:** `GET /api/users/suggestions?page=&pageSize=` with weighted scoring by shared hobbies, mutual connections, and same city; excludes self, already-followed, blocked, and muted users | Read-only; cache scores if slow |
| **Hashtags (optional)** | Topic discovery | **Done (hobby tags):** `GET /api/tags?limit=` and `GET /api/tags/{tag}/users?page=&pageSize=` using hobby catalog as hashtags (e.g. `#travel`) | Can still introduce `Tag` + `PostTag` with posts later |

**Exit criteria:** Client can search and show a “who to follow” list without scanning the whole directory.

---

## Phase 2 — Posts & timeline (core “social” loop)

| Feature | Goal | Scope | Notes |
|--------|------|-------|--------|
| **Post entity** | Shareable content | **Done:** `Post` with `AuthorId`, `Body`, `CreatedUtc`, `UpdatedUtc`, `Visibility` (public/followers), and soft delete (`DeletedUtc`) | Migration `AddPostsTimeline`; endpoints under `PostsController` |
| **Home timeline** | Feed from follows | **Done:** `GET /api/feed/home?page=&pageSize=` returns followed users' non-deleted posts in reverse chronological order with block filtering | Followers-only visibility respected |
| **User timeline** | Profile tab “Posts” | **Done:** `GET /api/users/{username}/posts?page=&pageSize=` | Public posts for everyone, followers-only posts for followers/owner |
| **Delete / edit post** | Basic moderation | **Done:** `PUT /api/posts/{postId}` and `DELETE /api/posts/{postId}` for author only | Soft delete for audit |
| **Media on posts** | Rich posts | **Done:** Reused `Photo` with `PostPhoto` join (`photoIds` on create/update, `mediaUrls` on read) so post media can be composed from user-owned uploaded photos | `POST/PUT /api/posts` enforce photo ownership |

**Exit criteria:** A user sees a chronological stream from people they follow, not only the “people discovery” feed.

---

## Phase 3 — Engagement on posts

| Feature | Goal | Scope | Notes |
|--------|------|-------|--------|
| **Reactions** | Lightweight feedback | `PostReaction`: `PostId`, `UserId`, `Kind` (enum), unique per user/post | `POST/DELETE /api/posts/{id}/reactions` |
| **Comments** | Conversation | `Comment`: `PostId`, `AuthorId`, `Body`, `ParentCommentId` nullable | Threaded replies optional in v2 |
| **Mentions** | Notify people | Parse `@username` in body; `Mention` table or extract on read | Ties to notifications (Phase 6) |
| **Repost / quote** | Reshare | `RepostOfPostId` nullable on `Post`, or separate `Repost` table | Clarify UX vs quote-post |

**Exit criteria:** Posts feel alive without opening DMs.

---

## Phase 4 — Groups & communities (optional branch)

| Feature | Goal | Scope | Notes |
|--------|------|-------|--------|
| **Groups** | Shared spaces | `Group`, `GroupMember` (role: admin/member), `GroupPost` or group-scoped posts | Permissions model |
| **Group discovery** | Growth | `GET /api/groups`, join requests | Moderation tools needed |

**Exit criteria:** You only start this if product direction is “communities,” not “Twitter-style public graph.”

---

## Phase 5 — Real-time & presence

| Feature | Goal | Scope | Notes |
|--------|------|-------|--------|
| **SignalR hub** | Live UI | Hubs for DMs typing, new message, optional live reaction counts | Scale story: Redis backplane later |
| **Online presence** | “Active now” | Last-seen already exists; optional heartbeat | Privacy setting |

**Exit criteria:** Mobile/web can show typing and instant message delivery without polling.

---

## Phase 6 — Notifications

| Feature | Goal | Scope | Notes |
|--------|------|-------|--------|
| **In-app notifications** | Engagement | `Notification`: `UserId`, `Type`, `PayloadJson`, `ReadUtc` | `GET /api/notifications`, mark read |
| **Push / email** | Off-app engagement | Queue (Hangfire, Azure Queue, or outbox table) + FCM/APNs or email | Depends on Phase 0 mail |

**Exit criteria:** Follow, mention, comment, and DM generate a durable in-app row; push is optional second step.

---

## Phase 7 — Trust, safety, admin

| Feature | Goal | Scope | Notes |
|--------|------|-------|--------|
| **Report user / content** | Safety | `Report` with target type + id, reason, status | Admin `GET/PATCH` queue |
| **Admin moderation** | Remove harmful posts | Reuse soft delete; `Admin` role endpoints | Audit log table recommended |
| **Appeals (later)** | Fairness | Status workflow on reports | Low priority |

**Exit criteria:** There is a path from “user reports” to “admin takes action” without DB surgery.

---

## Phase 8 — Monetization (real payments)

| Feature | Goal | Scope | Notes |
|--------|------|-------|--------|
| **Stripe (or similar)** | Paid plans | Checkout session, webhooks → extend `SubscriptionEndsUtc` / plan id | Replace or gate manual `subscribe` dev endpoint |
| **Invoices / receipts** | Compliance | Webhook-stored records | After first payment integration |

**Exit criteria:** Production can sell Plus/Premium without hand-editing the database.

---

## Cross-cutting concerns (every phase)

- **Migrations:** One EF migration per vertical slice; avoid mixing unrelated schema changes.
- **Tests:** For each feature add at least one integration test (factory + SQLite) covering happy path + one failure (auth, validation, or permission).
- **API versioning (optional):** If mobile clients ship slowly, consider `/api/v2` before breaking DTOs.
- **Observability:** Structured logging + request ids before scaling traffic.

---

## Suggested sequencing (summary)

1. **Phase 0** — verification, reset, rate limits, block/mute.  
2. **Phase 1** — search + suggestions.  
3. **Phase 2** — posts + home timeline (biggest product jump).  
4. **Phase 3** — reactions + comments (+ mentions when notifications exist).  
5. **Phase 6** — in-app notifications (can overlap Phase 3 for mentions).  
6. **Phase 5** — SignalR when DM/feed UX demands it.  
7. **Phase 7** — reports/admin as traffic grows.  
8. **Phase 4** — groups only if roadmap commits to communities.  
9. **Phase 8** — payments when you are ready for ops + legal.

---

## How to use this file

- Turn each **row** into a GitHub issue or project board card.  
- Keep **one primary PR per feature** (schema + API + tests together).  
- Update this roadmap when scope changes; link issues at the end of each phase section if you use a tracker.
