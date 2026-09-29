# Pipeline — Technical & Architectural Design Specification

**Date:** 2026-09-29  
**Status:** Pending User Approval  
**Project:** Pipeline — Job Search Command Center  
**Source Specification:** `README_to_do.md`

---

## 1. Executive Summary & Intent

**Pipeline** is a single-user-focused, multi-tenant/multi-user capable Job Search Command Center and CRM. It is built to replace disconnected spreadsheets with an intelligent system that links companies, applications, recruiter contacts, interaction timelines, interview prep/debriefs, CV versions, and offer decisions.

### Core Value Drivers:
1. **Relationship Memory:** Full interaction history for every contact (recruiters, hiring managers, interviewers, referrers).
2. **Next-Action Engine:** Automated daily "Do today" task list driven by rules (due follow-ups, upcoming interviews, stale candidacies, offer deadlines).
3. **CV Version Performance:** Quantitative metrics per document version (times sent, response rate, interview conversion rate).
4. **Funnel & Stage Analytics:** Conversion analysis across sources, work modes, and CV versions, with actionable rule-based insight cards.
5. **Interview Preparation & Debrief:** Structured questions, checklists, and self-assessments linked to application job descriptions.
6. **Offer Comparison:** Multi-offer weighted scoring and side-by-side criteria evaluation.
7. **Future-Proof Job Discovery:** Extensible skeleton for automated job ingestion without refactoring.

---

## 2. System Architecture

```
                    ┌──────────────────────────────────────────────┐
                    │               Web Client (PWA)               │
                    │        React 18 + TypeScript + Vite          │
                    │     Tailwind CSS + shadcn/ui + TanStack      │
                    └──────────────────────┬───────────────────────┘
                                           │ HTTPS / REST (JSON)
                                           ▼
                    ┌──────────────────────────────────────────────┐
                    │             Reverse Proxy (Nginx)            │
                    └──────────────────────┬───────────────────────┘
                                           │
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                               Pipeline.Api (.NET 8)                                    │
│  Controllers | Auth & JWT Middleware | RFC 7807 ProblemDetails | Rate Limiting | Swagger│
├────────────────────────────────────────────────────────────────────────────────────────┤
│                           Pipeline.Application                                         │
│  Commands/Queries/Services | DTOs | FluentValidation | Automations Engine Rules        │
├────────────────────────────────────────────────────────────────────────────────────────┤
│                           Pipeline.Domain                                              │
│  Entities | Enums | Value Objects | Domain Events | Interfaces                         │
├────────────────────────────────────────────────────────────────────────────────────────┤
│                           Pipeline.Infrastructure                                      │
│  EF Core 8 (PostgreSQL) | Identity | S3/MinIO Storage | MailKit SMTP | WebPush | Hangfire│
└────────┬─────────────────────────┬────────────────────────────┬────────────────────────┘
         │                         │                            │
         ▼                         ▼                            ▼
┌─────────────────┐       ┌─────────────────┐          ┌─────────────────┐
│  PostgreSQL 16  │       │  MinIO / S3     │          │ Mailpit / SMTP  │
│  (Data + Jobs)  │       │ (Doc Versions)  │          │  (Email Alerts) │
└─────────────────┘       └─────────────────┘          └─────────────────┘
```

### 2.1 Backend Project Organization (`backend/`)
- **`Pipeline.Domain`**: Clean entities with no external framework dependencies except Core abstractions.
  - `User`, `Company`, `Application`, `ApplicationStatusHistory`, `Contact`, `ApplicationContact`, `Interaction`, `Interview`, `InterviewContact`, `InterviewQuestion`, `Document`, `DocumentVersion`, `JobReference`, `ApplicationReference`, `TaskItem`, `Reminder`, `EmailTemplate`, `PushSubscription`, `RefreshToken`, `CustomStage`, `JobSource`, `DiscoveredJob`, `UserDiscoveredJobState`.
- **`Pipeline.Application`**:
  - Application services and interfaces: `IApplicationService`, `IContactService`, `IInterviewService`, `IDocumentService`, `IAnalyticsService`, `IAutomationEngine`, `IFileStorage`, `IEmailService`, `IPushNotificationService`, `IJobSourceConnector`.
  - FluentValidation validators for all input DTOs.
  - Automation rule handlers generating system tasks.
- **`Pipeline.Infrastructure`**:
  - `PipelineDbContext`: Configured with EF Core 8 and Npgsql.
  - Global query filter on all user-owned entities: `.HasQueryFilter(e => e.UserId == _currentUserService.UserId && e.DeletedAt == null)`.
  - Full-text search configuration using PostgreSQL `tsvector` on `applications`, `companies`, and `contacts`.
  - S3 / MinIO integration using AWS SDK for .NET with short-lived presigned download URLs.
  - Background processing with Hangfire backed by PostgreSQL.
  - Email notification via MailKit; Web Push via Lib.Net.WebPush (VAPID).
- **`Pipeline.Api`**:
  - RESTful Controllers following RFC 7807 problem details for errors.
  - JWT Bearer Authentication + rotating Refresh Tokens stored in secure httpOnly cookies.
  - ASP.NET Core Identity integration.
  - Rate limiting (fixed window / token bucket on auth endpoints).
  - Health checks: `/health` checking DB and storage readiness.
- **`Pipeline.Tests`**:
  - Unit tests for services and automation rules.
  - Integration tests verifying multi-user data isolation (User A cannot access User B's entities under any condition).

### 2.2 Frontend Project Organization (`frontend/`)
- **`src/app/`**: Application shell, React Router v6 setup, TanStack Query client, App Providers.
- **`src/components/ui/`**: Reusable primitive components (shadcn/ui style, Tailwind CSS, Lucide icons, accessible Radix UI primitives).
- **`src/features/`**:
  - `auth/`: Login, Register, Forgot Password, Reset Password, Email Verification, Google OAuth.
  - `onboarding/`: 4-step wizard for new users (Role preferences, salary expectations, search status, optional initial CV upload).
  - `dashboard/`: "Do Today" action items, upcoming interview countdowns, weekly stats, stale application alerts, floating quick-add button.
  - `applications/`: Dual view (Kanban drag-and-drop powered by `@dnd-kit`, Sortable/Filterable Table), Detail view with tabs (Overview, Timeline, Job Description with skill keyword highlighting, Contacts, Interviews, Documents, Offer, Closing).
  - `contacts/`: Directory with warmth indicators (<14d green, 14-30d yellow, >30d red), interaction logger, timeline.
  - `interviews/`: Interview detail, interviewers, prep checklist, question log with categories and difficulty, post-interview debrief with auto-tasks.
  - `documents/`: CV & cover letter version manager, PDF in-app viewer, stats per version (sent, replies, interviews, offers), side-by-side version comparison.
  - `analytics/`: Funnel conversion chart, breakdown by source, work mode, company size, CV version, stage duration metrics, server-generated insight cards.
  - `calendar/`: Month/week/agenda views, iCal (.ics) download and personal subscribed calendar feed token.
  - `references/`: Reference manager with consent status chips and heads-up notifications.
  - `offers/`: Multi-offer comparison matrix with customizable criteria weights.
  - `tasks/`: Filterable task lists (Today, Upcoming, Overdue, Done).
  - `templates/`: Email template manager with placeholder replacements (`{{contactName}}`, `{{company}}`, `{{role}}`, `{{myName}}`).
  - `discovery/`: Skeleton placeholder with "Coming Soon" notification toggle and manual job add link.
  - `settings/`: Profile, pipeline stage configuration, notification toggles, quiet hours, GDPR ZIP export and account deletion.
- **`src/lib/`**: Axios API client with automatic token refresh on 401, Zustand stores (auth, command palette, global UI state), formatting helpers.

---

## 3. UI/UX & Theme Design Direction

In adherence to the `frontend-design`, `theme-factory`, and `web-design-guidelines` skills:
- **Visual Personality:** Professional, high-precision command center for ambitious professionals. Clear typographic hierarchy, avoiding generic AI-generated purple washes and excessive soft card shadows.
- **Theme Palette:**
  - *Base:* Deep slate/zinc dark mode (`#090D16` / `#0F172A`) and crisp clean light mode (`#F8FAFC` / `#FFFFFF`).
  - *Primary Accent:* Indigo / Electric Blue (`#4F46E5` / `#3B82F6`) providing focused contrast for active states, CTAs, and kanban highlights.
  - *Status Accents:* Emerald green (`#10B981`) for offer/accepted/fresh warmth; Amber (`#F59E0B`) for screening/medium warmth/deadlines; Rose/Red (`#EF4444`) for rejected/stale warmth/overdue; Violet (`#8B5CF6`) for interviews.
- **Typography:** Inter or Geist Sans for crisp tabular legibility, calibrated type scale with intentional weight differentiation.
- **Information Density:** High utility density with clear visual boundaries, keyboard shortcuts (`N`, `/`, `G then D`, `Ctrl+K`), and accessible ARIA navigation.

---

## 4. Multi-Tenant Data Isolation & Security

1. **Global Entity Framework Filter:**
   ```csharp
   builder.Entity<Application>()
          .HasQueryFilter(a => a.UserId == _currentUserService.UserId && a.DeletedAt == null);
   ```
2. **Double Verification in Services:** Every mutation explicitly verifies `entity.UserId == currentUserId`.
3. **Security Headers & Defense:** HSTS, CSP, X-Frame-Options, X-Content-Type-Options, strict CORS for frontend origin.
4. **File Safety:** Content-type and magic byte inspection, 10MB file limit, random UUID storage keys, presigned temporary URLs.
5. **GDPR Compliance:**
   - `GET /api/me/export` streams a ZIP containing user JSON exports and stored files.
   - `DELETE /api/me` triggers cascade purge of user data, files from MinIO, and revokes credentials.

---

## 5. Verification & Testing Strategy

- **Backend Tests:**
  - xUnit + FluentAssertions + Moq.
  - Explicit test suite for **User Isolation** asserting User A cannot query, update, or delete User B's records.
  - Unit tests for all 8 Next-Action Engine automation rules.
  - Unit tests for CV version statistics calculation and Funnel analytics.
- **Frontend Tests:**
  - Vitest + Testing Library for key components (Kanban card movements, Offer comparison calculations, Warmth indicator logic).
  - Playwright webapp testing for browser end-to-end verification.
- **Verification Rule:** No milestone is marked done without fresh execution evidence (`dotnet test`, frontend test runner, and running app verification).

---

## 6. Implementation Milestones

As specified in `README_to_do.md`:
1. **Milestone 1: Foundation:** Project structure, Docker Compose (API, Web, Postgres, MinIO, Mailpit), EF Core, Auth (JWT + Refresh), Health, Frontend shell & routing.
2. **Milestone 2: Applications:** Companies, Applications CRUD, Status history, Kanban + Table views, Detail view, Search.
3. **Milestone 3: Contacts & Interactions:** Contacts CRUD, Application link, Interaction logging, Warmth indicators, Timeline.
4. **Milestone 4: Interviews:** Interviews CRUD, Prep checklist, Questions log, Debrief, .ics export.
5. **Milestone 5: Documents:** S3/MinIO storage, Upload, Versions, Presigned download, Stats per version.
6. **Milestone 6: Tasks & Automation:** Hangfire rules engine, "Do Today" dashboard, Reminders, Email templates.
7. **Milestone 7: References & Offers:** References manager with consent rules, Offer comparison matrix.
8. **Milestone 8: Analytics & Calendar:** Funnel charts, Response rates, Stage durations, Insight cards, Calendar view & ICS feed.
9. **Milestone 9: Polish & Hardening:** Onboarding wizard, Settings, GDPR export/delete, CSV import/export, PWA, Seed demo data.
10. **Milestone 10: Job Discovery Skeleton:** Interfaces, tables, stub endpoints, placeholder UI, documentation.
