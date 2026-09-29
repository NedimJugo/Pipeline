# Pipeline — Job Search Command Center

> **Instructions for the AI / developer reading this file:** This README is the complete product and technical specification for an application called **Pipeline**. Build the whole app from this document. Follow the stack, structure, data model, API, and screens exactly. Where something is not specified, choose the simplest option that keeps the app easy to deploy and maintain. Build in the order given in [Build Order](#16-build-order-milestones). After each milestone the app must run end to end with `docker compose up`.

---

## 1. Product Overview

**Pipeline** is a CRM for a person's job hunt. It is **not** a job board. It tracks every application, person, conversation, document and interview in one connected system, tells the user what to do next, and shows analytics about what is working.

**Why not just an Excel sheet?** A spreadsheet cannot:
- link a recruiter to every application and conversation they were part of,
- remind you to follow up or send a thank-you note,
- tell you which CV version gets the most replies,
- show where in the funnel you lose most candidacies,
- prepare you for each interview from the job description,
- track who agreed to be your reference and where you used them.

### Unique selling points (must all be implemented)
1. **Relationship memory:** every recruiter/interviewer is a contact with a full interaction timeline (who reached out, channel, what they said, what you sent).
2. **Next-action engine:** a daily "Do today" list generated from rules (follow-ups due, interviews, prep, stale applications, offer deadlines).
3. **CV versioning with stats:** each document version shows how many times it was sent, how many replies, and how many interviews it led to.
4. **Funnel analytics with insights:** conversion by stage, source, CV version, work mode; auto-generated insight cards.
5. **Interview prep and debrief per interview:** questions asked, your answers, self-rating, lessons learned.
6. **Reference manager:** who vouches for you, consent status, which applications they were used for.
7. **Job Discovery placeholder:** architecture ready for future aggregation of jobs from many sources (see section 9).

### Target users
Job seekers of any profession (default examples are IT). Many users, each with strictly private data.

---

## 2. Tech Stack (chosen for easy deployment)

| Layer | Choice | Reason |
|---|---|---|
| Backend API | **ASP.NET Core 8 (C#) Web API** | Typed, fast, great tooling, single deployable container |
| ORM | **Entity Framework Core 8** + migrations | Automatic schema management |
| Database | **PostgreSQL 16** | Free, reliable, hosted everywhere |
| Frontend | **React 18 + TypeScript + Vite** | Fast dev, static build |
| UI | **Tailwind CSS + shadcn/ui**, **TanStack Query**, **React Router**, **Zustand**, **React Hook Form + Zod**, **Recharts**, **dnd-kit** (kanban) | Modern and productive |
| Mobile | **PWA** (installable, offline shell, push notifications). No separate native app in v1 | One codebase |
| Auth | **JWT access + refresh tokens** (ASP.NET Identity), Google OAuth | Standard |
| File storage | **S3-compatible** (MinIO locally, Cloudflare R2 / AWS S3 / Azure Blob in production) behind an `IFileStorage` interface | Portable |
| Background jobs | **Hangfire** (PostgreSQL storage) | Reminders and emails, no extra service |
| Email | **SMTP via MailKit** (Mailpit locally; Resend/Brevo/SendGrid in production) | Portable |
| Push | **Web Push (VAPID)** | Works with PWA |
| Validation | FluentValidation | Clean request validation |
| Logging | Serilog to console | Container friendly |
| API docs | Swagger / OpenAPI | Self-documenting |
| Tests | xUnit (backend), Vitest + Testing Library (frontend) | Standard |
| Containers | **Docker + Docker Compose** | One-command run |
| CI/CD | **GitHub Actions**: build, test, push images | Automated |

### Deployment targets (must be supported)
- **Local / VPS:** `docker compose up -d` starts API, web (nginx), Postgres, MinIO, Mailpit. A Caddy reverse proxy gives automatic HTTPS on a VPS.
- **Managed option (documented in README section "Deploy"):** Frontend on Azure Static Web Apps / Vercel / Cloudflare Pages; API as a container on Azure Container Apps / Fly.io / Railway; managed PostgreSQL; Azure Blob or R2 for files.
- All configuration via **environment variables** (12-factor). Provide `.env.example`.
- Health endpoint `GET /health` for orchestrators.
- Database migrations apply automatically on API startup (configurable by `AUTO_MIGRATE=true`).

---

## 3. Repository Structure

```
pipeline/
├── README.md
├── docker-compose.yml
├── docker-compose.prod.yml
├── .env.example
├── .github/workflows/ci.yml
├── backend/
│   ├── Pipeline.sln
│   ├── src/
│   │   ├── Pipeline.Api/            # controllers, middleware, DI, Program.cs
│   │   ├── Pipeline.Application/    # services, DTOs, validators, interfaces
│   │   ├── Pipeline.Domain/         # entities, enums
│   │   └── Pipeline.Infrastructure/ # EF Core, storage, email, push, Hangfire jobs
│   └── tests/
├── frontend/
│   ├── package.json
│   ├── vite.config.ts
│   ├── public/ (manifest.webmanifest, service worker, icons)
│   └── src/
│       ├── app/ (router, providers)
│       ├── features/ (applications, contacts, interviews, documents, references, analytics, calendar, discovery, settings, auth, dashboard)
│       ├── components/ui/
│       ├── lib/ (api client, auth, utils)
│       └── styles/
└── docs/ (screenshots, api.md)
```

---

## 4. Core Concepts and Enums

**Application status (default pipeline, user can customize labels/order):**
`Wishlist → Applied → Screening → Interview → Assignment → Offer → Accepted`
Terminal statuses: `Rejected`, `Withdrawn`, `Ghosted`, `Declined` (user declined an offer).

**Work mode:** `Onsite`, `Hybrid`, `Remote`.
**Employment type:** `FullTime`, `PartTime`, `Contract`, `Internship`, `Freelance`.
**Source:** `LinkedIn`, `CompanyWebsite`, `Referral`, `Recruiter`, `JobBoard`, `Event`, `Other` (plus free-text name).
**Interview type:** `HR`, `Technical`, `Culture`, `Manager`, `Final`, `Assignment`, `Other`.
**Interview format:** `Phone`, `Video`, `Onsite`.
**Interaction channel:** `Email`, `LinkedIn`, `Phone`, `Video`, `InPerson`, `Message`, `Other`.
**Interaction direction:** `Inbound` (they contacted me), `Outbound` (I contacted them).
**Document type:** `CV`, `CoverLetter`, `Portfolio`, `Certificate`, `Other`.
**Reference consent:** `NotAsked`, `Asked`, `Agreed`, `Declined`.

---

## 5. Data Model (PostgreSQL via EF Core)

All tables have `Id (uuid PK)`, `CreatedAt`, `UpdatedAt`. All user-owned tables have `UserId (FK)` and a global EF query filter `UserId == currentUser` so data can never leak between users. Use soft delete (`DeletedAt`) for applications, contacts, documents.

**users** (ASP.NET Identity): Email, PasswordHash, DisplayName, TargetRole, Seniority, Location, SalaryExpectationMin, SalaryExpectationMax, Currency, SearchStatus (`Active`, `Passive`, `Paused`), Timezone, NotificationPrefs (jsonb), StaleAfterDays (default 14), OnboardingCompleted.

**companies**: Name, Website, Industry, Size, Location, Notes, LinkedInUrl.

**applications**: CompanyId, RoleTitle, JobUrl, Source, SourceDetail, Status, StatusChangedAt, AppliedAt, WorkMode, EmploymentType, Location, SalaryMin, SalaryMax, Currency, JobDescription (text), Notes, Pros (text), Cons (text), Priority (1-3), Favorite (bool), ExcitementRating (1-5), DocumentVersionCvId (FK, CV sent), DocumentVersionCoverId (FK, nullable), ClosedReason (text, nullable), RejectionStage (nullable), LessonsLearned (text), OfferSalary, OfferBenefits (text), OfferDeadline, DiscoveredJobId (FK nullable, for the future aggregator).

**application_status_history**: ApplicationId, FromStatus, ToStatus, ChangedAt, Note.

**contacts**: CompanyId (nullable), FullName, Role, Email, Phone, LinkedInUrl, Type (`Recruiter`, `HiringManager`, `Interviewer`, `Referrer`, `Peer`, `Other`), Notes, LastContactedAt, NextFollowUpAt.

**application_contacts** (many-to-many): ApplicationId, ContactId, RoleInProcess.

**interactions**: ContactId (nullable), ApplicationId (nullable), Channel, Direction, OccurredAt, Summary (what was said), SentContent (what I sent, text), AttachmentDocumentId (nullable), FollowUpRequired (bool), FollowUpDueAt.

**interviews**: ApplicationId, Type, Format, ScheduledAt, DurationMinutes, Location, MeetingLink, Status (`Scheduled`, `Completed`, `Cancelled`, `NoShow`), PrepNotes, PrepChecklist (jsonb list of {text, done}), SelfRating (1-5), WentWell, ToImprove, ThankYouSent (bool), OutcomeNotes.

**interview_contacts** (many-to-many): InterviewId, ContactId.

**interview_questions**: InterviewId, Question, MyAnswer, Category (`Behavioral`, `Technical`, `Situational`, `Salary`, `Other`), Difficulty (1-5), WasPrepared (bool).

**documents**: Type, Title, Description.
**document_versions**: DocumentId, VersionLabel, FileKey (storage key), FileName, ContentType, SizeBytes, Notes, IsDefault.

**job_references**: FullName, Relationship, Email, Phone, Company, Consent, Notes, LastNotifiedAt.
**application_references** (many-to-many): ApplicationId, ReferenceId, SharedAt, Outcome.

**tasks**: ApplicationId (nullable), ContactId (nullable), InterviewId (nullable), Title, Notes, DueAt, CompletedAt, Source (`Manual`, `Auto`), AutoRuleKey (nullable).

**reminders**: TaskId or entity ref, RemindAt, Channel (`Push`, `Email`), SentAt.

**email_templates**: Name, Subject, Body (with placeholders `{{contactName}}`, `{{company}}`, `{{role}}`, `{{myName}}`), Category (`FollowUp`, `ThankYou`, `Negotiation`, `Withdraw`, `Other`). Seed 6 system templates.

**push_subscriptions**: Endpoint, P256dh, Auth, UserAgent.
**refresh_tokens**: standard.
**custom_stages** (optional): Order, Label, MapsToStatus.

**Future (create tables now, leave unused in UI):**
**job_sources**: Name, Type (`Api`, `Rss`, `Scraper`, `Manual`), BaseUrl, Config (jsonb), Enabled, LastRunAt.
**discovered_jobs**: SourceId, ExternalId, Title, CompanyName, Location, Url, Description, PostedAt, Tags (text[]), Hash (unique), FetchedAt. Global table (not per-user), plus **user_discovered_job_state**: UserId, DiscoveredJobId, State (`New`, `Saved`, `Dismissed`).

Indexes: `(UserId, Status)`, `(UserId, StatusChangedAt)`, `(UserId, NextFollowUpAt)`, `interviews(UserId, ScheduledAt)`, full-text (`tsvector`) on applications (RoleTitle, Notes, JobDescription), companies (Name), contacts (FullName).

---

## 6. Authentication and Security

- Register / login with email + password (min 10 chars), Google OAuth, email verification, password reset by email.
- JWT access token (15 min) + refresh token (30 days, rotating, stored hashed, httpOnly secure cookie).
- Rate limiting on auth endpoints (e.g. 10/min per IP).
- Authorization: every endpoint requires auth except auth and health. Enforce ownership in EF global filters **and** in services.
- File uploads: max 10 MB, allowed types PDF/DOCX/PNG/JPG, validated by content type and magic bytes, random storage keys, served through short-lived signed URLs (never public buckets).
- CORS restricted to the configured frontend origin. Security headers (HSTS, CSP, X-Content-Type-Options).
- Input validation with FluentValidation, EF parameterized queries only.
- **GDPR:** `GET /api/me/export` returns a ZIP (JSON + files); `DELETE /api/me` permanently deletes all user data and files.
- Secrets only from env vars. Never log tokens or PII.

---

## 7. Screens and Functionality

Design language: clean, calm, modern; light and dark mode; mobile-first responsive; keyboard shortcuts on desktop (`N` new application, `/` search, `G then D` go to dashboard).

### 7.1 Onboarding (first login, 4 steps)
1. Target role(s), seniority, location, remote preference.
2. Salary expectation range and currency.
3. Search status (active / passive / paused).
4. Optional: upload first CV (creates a Document with v1), optionally import applications from CSV.
Finish → Dashboard. Skippable.

### 7.2 Dashboard ("Today")
- Greeting and search-status badge.
- **Do today** list (auto + manual tasks): follow-ups due, interviews today/tomorrow, prep not done, thank-you not sent, stale applications, offer deadlines within 3 days. Each item has one-tap actions: Done, Snooze (1d / 3d / 1w), Open.
- **Upcoming interviews** (next 7 days) with countdown and prep progress bar.
- **This week** stats: applications sent, responses, interviews, offers.
- **Stale applications** (no update for `StaleAfterDays`): quick actions "Follow up", "Mark ghosted".
- **Quick add** floating button: application (paste URL + company + role), contact, interaction, task.

### 7.3 Applications Board
- **Kanban view** with columns per status; drag and drop changes status and writes status history; terminal statuses in a collapsed "Closed" lane.
- **Table view**: sortable, filterable columns (status, company, source, work mode, salary, applied date, priority, days in stage).
- Filters: status, source, work mode, employment type, priority, favorite, date range, text search (full-text).
- Bulk actions: change status, archive, delete.
- Card shows company logo initials, role, days in stage, next interview date, priority dot, contact avatars.
- Import CSV / export CSV.

### 7.4 Application Detail
Tabs:
- **Overview:** company, role, URL, source, work mode, type, location, salary range, priority, excitement rating, dates, status stepper (click to change), notes, pros/cons.
- **Timeline:** unified chronological feed of status changes, interactions, interviews, tasks, notes. Add entries inline.
- **Job description:** pasted text, with "keyword highlighter" (skills in bold); stored permanently because postings disappear.
- **Contacts:** linked contacts with role in process; add existing or create new.
- **Interviews:** list of interviews; add new.
- **Documents:** which CV/cover letter version was sent; upload attachments (task, offer letter).
- **Offer:** salary, bonus, benefits, deadline, negotiation notes; "Compare offers" button.
- **Closing:** when Rejected/Withdrawn/Ghosted/Declined, require rejection stage and optional lessons learned.
- Actions: duplicate, archive, delete, "Send follow-up" (opens template picker).

### 7.5 Interview Detail
- Header: type, format, date/time, duration, link, location, add-to-calendar (.ics) button.
- **Interviewers** (linked contacts).
- **Prep:** notes, editable checklist (default checklist per type is seeded: research company, review JD, prepare 3 stories, prepare questions to ask, test tech setup), link to job description.
- **Questions log:** add questions asked (during/after), your answer, category, difficulty, "prepared?" flag.
- **Debrief:** self-rating 1-5, what went well, what to improve, thank-you-sent toggle (with template button), outcome notes.
- On save as Completed: auto-create task "Send thank-you within 24h" and "Follow up in 5 days".

### 7.6 Contacts
- List with search and filters (type, company, warmth).
- **Warmth indicator:** green (<14 days), yellow (14-30), red (>30) since `LastContactedAt`.
- Contact detail: profile, LinkedIn link, linked applications, **interaction timeline**, next follow-up date, notes.
- **Log interaction** dialog: channel, direction, date, what they said, what I sent, attach document, "needs follow-up in N days".
- Logging an interaction updates `LastContactedAt` and can auto-create a follow-up task.

### 7.7 References
- List of references with consent status chips.
- Detail: relationship, contact info, applications where they were shared, last notified.
- Action "Notify before use": open email template "Heads-up to reference" prefilled.
- Rule: prevent marking a reference as shared unless consent is `Agreed` (warning, overridable).

### 7.8 Documents
- Grouped by type; each document has versions (v1, v2, tailored-for-X).
- Upload, rename, set default, download via signed URL, preview PDF in-app.
- Per-version stats: **sent count, replies, interviews, offers** (computed from applications linked to that version).
- "Compare versions" view: side-by-side stats.

### 7.9 Analytics
- **Funnel chart:** Applied → Screening → Interview → Offer → Accepted, with conversion percentages.
- Response rate and interview rate by **source**, **CV version**, **work mode**, **company size**.
- Average and median **days per stage**.
- Applications per week (bar), interviews per week.
- Rejection stage breakdown (where you get rejected).
- **Insight cards** (rule-based, computed server-side), e.g.:
  - "Referral applications convert to interviews 3.1x more than LinkedIn."
  - "CV v3 has a 22% response rate vs 9% for v2."
  - "You lose most candidacies at the Technical stage (5 of 8 rejections)."
  - "12 applications have had no news for 14+ days."
  Minimum sample size 5 before showing an insight.
- Date-range filter (30/90/365 days, all time).

### 7.10 Calendar
- Month/week/agenda views: interviews, task due dates, offer deadlines, follow-ups.
- Click to open entities. Export `.ics` feed URL (per-user secret token) so it can be subscribed to in Google/Apple/Outlook calendar. (Two-way sync is out of scope for v1.)

### 7.11 Tasks and Reminders
- Task list (Today / Upcoming / Overdue / Done) with filters.
- Reminders sent via Web Push and/or email at the chosen time; per-type toggles in Settings.

### 7.12 Email Templates
- CRUD templates; placeholders auto-filled from the selected application/contact.
- "Copy to clipboard" and "Open in mail app" (`mailto:`) buttons. (The app does **not** send emails on the user's behalf in v1.)

### 7.13 Offer Comparison
- Select 2-4 applications in Offer status; table comparing base salary, bonus, benefits, work mode, commute/location, growth, pros/cons, user's own weighted score (user sets weights per criterion).

### 7.14 Job Discovery (placeholder, see section 9)
- Menu item visible, screen shows "Coming soon: find jobs and save them straight into your pipeline" with an email-me-when-ready toggle. No functionality in v1 except the button "Add job manually" that routes to Quick Add.

### 7.15 Settings
- Profile and target preferences, timezone.
- Notifications (per type, push/email), quiet hours.
- Pipeline customization (rename/reorder stages, stale-after days).
- Data: CSV export, full JSON/ZIP export, delete account.
- Appearance: light/dark/system.
- Connected accounts (Google).
- Install app (PWA prompt).

### 7.16 Auth screens
Login, register, verify email, forgot/reset password, Google sign-in.

---

## 8. Automation Rules (the Next-Action Engine)

Implemented as Hangfire recurring jobs (hourly) and event handlers. Each rule creates a task with `Source = Auto` and a unique `AutoRuleKey` per entity to avoid duplicates. Users can disable rules in Settings.

| Rule key | Trigger | Task created |
|---|---|---|
| `follow_up_after_apply` | Status set to Applied, no change in 7 days | "Follow up on {role} at {company}" |
| `stale_application` | No update for `StaleAfterDays` | "No news from {company}, follow up or mark ghosted" |
| `thank_you` | Interview marked Completed | "Send thank-you to interviewers" (due +24h) |
| `post_interview_follow_up` | Interview completed, no status change in 5 days | "Ask {company} about next steps" |
| `prep_reminder` | Interview within 48h and prep checklist incomplete | "Finish interview prep" |
| `offer_deadline` | Offer deadline within 3 days | "Respond to offer from {company}" |
| `contact_follow_up` | Interaction with FollowUpRequired | "Follow up with {contact}" |
| `cold_contact` | Contact linked to active application, no contact in 21 days | "Reach out to {contact}" |

Auto Ghosted suggestion: if stale 30+ days after Applied, show "Mark as Ghosted?" prompt (never changes status silently).

---

## 9. Job Discovery — Extension Point (implement the skeleton only)

Purpose: leave a clean place to plug in job aggregation later without refactoring.

Backend (implement interfaces, tables, empty endpoints):
```csharp
public interface IJobSourceConnector {
    string SourceKey { get; }
    Task<IReadOnlyList<RawJob>> FetchAsync(JobSource source, CancellationToken ct);
}
public record RawJob(string ExternalId, string Title, string CompanyName,
    string? Location, string Url, string? Description, DateTimeOffset? PostedAt, string[] Tags);
```
- A Hangfire job `JobIngestionJob` iterates enabled `job_sources`, calls the matching connector, normalizes to `DiscoveredJob`, dedupes by `Hash` (hash of company + title + location), and stores.
- Endpoints (return 501 or empty list for now): `GET /api/discovery/jobs`, `POST /api/discovery/jobs/{id}/save` (converts to a Wishlist application copying title, company, URL, description, and setting `DiscoveredJobId`), `POST /api/discovery/jobs/{id}/dismiss`.
- Register no connectors in v1. Document in `docs/discovery.md` how to add one (RSS, public API, or scraper) by implementing `IJobSourceConnector` and registering in DI.

---

## 10. REST API (all under `/api`, JSON, JWT bearer, paginated with `page`, `pageSize`, sorted with `sort`)

**Auth:** `POST /auth/register`, `/auth/login`, `/auth/refresh`, `/auth/logout`, `/auth/verify-email`, `/auth/forgot-password`, `/auth/reset-password`, `/auth/google`.
**Me:** `GET/PUT /me`, `PUT /me/preferences`, `GET /me/export`, `DELETE /me`, `POST /me/push-subscription`.
**Dashboard:** `GET /dashboard` (today tasks, upcoming interviews, weekly stats, stale apps).
**Applications:** `GET /applications` (filters), `POST`, `GET /{id}`, `PUT /{id}`, `DELETE /{id}`, `PATCH /{id}/status`, `GET /{id}/timeline`, `POST /{id}/duplicate`, `POST /import` (CSV), `GET /export` (CSV).
**Companies:** CRUD + `GET /companies/search?q=`.
**Contacts:** CRUD, `GET /contacts/{id}/timeline`, `POST /contacts/{id}/interactions`, `POST /applications/{id}/contacts/{contactId}`, `DELETE` same.
**Interactions:** `GET/PUT/DELETE /interactions/{id}`.
**Interviews:** CRUD, `GET /applications/{id}/interviews`, `GET /interviews/{id}/ics`, question CRUD under `/interviews/{id}/questions`.
**Documents:** CRUD, `POST /documents/{id}/versions` (multipart), `GET /document-versions/{id}/download` (signed URL), `PUT /document-versions/{id}/default`, `GET /documents/stats`.
**References:** CRUD, `POST /applications/{id}/references/{refId}`.
**Tasks:** CRUD, `POST /tasks/{id}/complete`, `POST /tasks/{id}/snooze`.
**Templates:** CRUD, `POST /templates/{id}/render` (body: applicationId, contactId → rendered text).
**Analytics:** `GET /analytics/funnel`, `/by-source`, `/by-document`, `/stage-durations`, `/weekly`, `/insights`.
**Calendar:** `GET /calendar?from&to`, `GET /calendar/feed/{token}.ics`.
**Search:** `GET /search?q=` across applications, companies, contacts.
**Offers:** `POST /offers/compare`.
**Discovery (stubs):** see section 9.
**Health:** `GET /health`.

Return RFC 7807 problem details for errors. Use DTOs, never expose entities.

---

## 11. Frontend Details

- Route guard for auth; refresh token handled by an axios/fetch interceptor.
- Data fetching with TanStack Query (optimistic updates for kanban drag and drop).
- Forms with React Hook Form + Zod, inline validation.
- Skeleton loaders, empty states with helpful CTAs, toast notifications, confirm dialogs for destructive actions.
- Accessibility: semantic HTML, keyboard navigable kanban (move with keyboard), sufficient contrast, ARIA labels.
- PWA: `manifest.webmanifest`, service worker (Workbox) caching app shell, offline read-only fallback page, install prompt, push handling.
- i18n-ready (react-i18next), default English, structure ready to add other languages.
- Global command palette (`Ctrl/Cmd+K`) for search and quick actions.

---

## 12. Non-Functional Requirements

- **Scale:** stateless API (horizontally scalable), all state in Postgres + object storage; pagination everywhere; indexes as listed; target p95 API latency under 300 ms at 10k users.
- **Reliability:** DB migrations versioned; Hangfire jobs idempotent; graceful error handling.
- **Observability:** structured logs, request IDs, `/health` (DB + storage checks).
- **Backups:** document `pg_dump` cron example and object storage versioning in the deploy guide.
- **Testing:** unit tests for services and automation rules, integration tests (Testcontainers Postgres) for ownership isolation (user A can never read user B's data), Vitest for key components. CI must run all tests.
- **Code quality:** nullable reference types on, analyzers, ESLint + Prettier, EditorConfig.

---

## 13. Docker and Deployment Spec

**docker-compose.yml (dev):** services `db` (postgres:16), `minio` (+ bucket init), `mailpit`, `api` (hot reload via `dotnet watch`), `web` (Vite dev server).
**docker-compose.prod.yml:** services `db`, `api` (multi-stage build, non-root user), `web` (nginx serving static build, proxying `/api` to `api`), `caddy` (automatic HTTPS, `DOMAIN` env var). Volumes for Postgres and MinIO (or external S3).

**Environment variables (`.env.example`):**
```
DOMAIN=localhost
POSTGRES_USER=pipeline
POSTGRES_PASSWORD=change-me
POSTGRES_DB=pipeline
ConnectionStrings__Default=Host=db;Database=pipeline;Username=pipeline;Password=change-me
Jwt__Secret=change-me-32+chars
Jwt__Issuer=pipeline
Jwt__Audience=pipeline-web
Frontend__Origin=http://localhost:5173
Storage__Provider=S3
Storage__Endpoint=http://minio:9000
Storage__Bucket=pipeline
Storage__AccessKey=minio
Storage__SecretKey=change-me
Smtp__Host=mailpit
Smtp__Port=1025
Smtp__User=
Smtp__Password=
Smtp__From=no-reply@pipeline.local
Google__ClientId=
Google__ClientSecret=
Vapid__PublicKey=
Vapid__PrivateKey=
AUTO_MIGRATE=true
SEED_DEMO_DATA=false
```

**CI/CD (`.github/workflows/ci.yml`):** on push/PR: restore, build, test backend and frontend; on tag: build and push Docker images to GHCR; optional deploy job via SSH (`docker compose pull && up -d`).

The README in the built repo must include: quick start (3 commands), env var table, VPS deployment guide, managed cloud guide, backup/restore, and how to add a Job Discovery connector.

---

## 14. Seed and Demo Data

When `SEED_DEMO_DATA=true`, create a demo user (`demo@pipeline.local` / `Demo12345!`) with ~15 applications across all statuses, 8 contacts with interactions, 5 interviews with questions, 3 CV versions, 3 references, and default email templates, so analytics and insights are populated for screenshots.

---

## 15. Out of Scope for v1
- Sending emails/LinkedIn messages from within the app
- Two-way calendar sync
- Native iOS/Android apps
- Real job aggregation (only the skeleton)
- Team/coach accounts (design DB so a `WorkspaceId` can be added later)
- Payments (leave hooks so a subscription plan can be added later)

---

## 16. Build Order (Milestones)

1. **Foundation:** solution + repo structure, Docker Compose, Postgres, EF Core, auth (register/login/refresh, Google), health, CI, frontend shell with routing, layout, dark mode.
2. **Applications:** companies, applications CRUD, status history, kanban + table, application detail (overview + timeline), search.
3. **Contacts and interactions:** contacts CRUD, link to applications, interaction logging, warmth, timeline.
4. **Interviews:** interviews, prep checklist, questions log, debrief, `.ics` export.
5. **Documents:** S3 storage abstraction, upload, versions, signed download, per-version stats.
6. **Tasks and automation:** tasks, Hangfire, rule engine, dashboard "Do today", email + web push reminders, email templates.
7. **References and offers:** references with consent, offer section and comparison.
8. **Analytics and calendar:** funnel, breakdowns, stage durations, insight cards, calendar views and ICS feed.
9. **Polish and hardening:** onboarding, settings, GDPR export/delete, CSV import/export, PWA, rate limiting, security headers, accessibility pass, i18n scaffolding, demo seed, docs.
10. **Job Discovery skeleton:** interfaces, tables, stub endpoints, placeholder screen, `docs/discovery.md`.

### Definition of done for each milestone
- Feature works end to end from the UI.
- Backend tests pass, including user-isolation tests.
- `docker compose up` from a clean clone works with `.env.example` copied to `.env`.
- README/docs updated.

---

## 17. Acceptance Checklist (final)
- [ ] A new user can register, onboard, add an application, log a contact interaction, schedule an interview and get a reminder.
- [ ] User A can never see or modify user B's data (verified by tests).
- [ ] Dashboard "Do today" is auto-populated by the automation rules.
- [ ] Analytics show funnel and at least 3 insight types with demo data.
- [ ] CV version stats are correct.
- [ ] App is installable as a PWA and works well on a 375px wide screen.
- [ ] One-command deploy works locally and on a fresh VPS with HTTPS.
- [ ] Job Discovery skeleton exists and is documented.
