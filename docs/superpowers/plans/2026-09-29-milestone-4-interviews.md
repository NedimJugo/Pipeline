# Milestone 4: Interviews & Calendar Prep Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build complete interview management for Pipeline, including interview scheduling, default prep checklists seeded per interview type, questions log with categories and difficulty ratings, post-interview debrief with 1-5 star ratings, RFC 5545 `.ics` calendar file export, and integration into the application detail view and unified timeline.

**Architecture:** .NET 8 Clean Architecture backend (`Pipeline.Application` + `Pipeline.Infrastructure` + `Pipeline.Api`) using EF Core 8 with multi-tenant isolation, coupled with a React 18 + TypeScript + Vite frontend styled with Tailwind CSS, Lucide icons, and TanStack Query.

**Tech Stack:** C# .NET 8, EF Core 8, SQLite / PostgreSQL, React 18, TypeScript, Tailwind CSS, TanStack Query, Vitest, Playwright.

**Spec:** `docs/superpowers/specs/2026-09-29-pipeline-design.md` and `README_to_do.md` Section 7.5.

## Global Constraints

- Multi-tenant data isolation: All queries and mutations must respect EF Core query filters (`UserId == currentUserId && DeletedAt == null`).
- RFC 7807 ProblemDetails for all error responses.
- High-density command center styling following `frontend-design` and `theme-factory` (slate/zinc borders, consistent typography, accessible color contrast).
- Every milestone ends with fresh verification evidence (`verification-before-completion`): `dotnet test`, `npm test`, `npm run build`, and browser testing.

## Review Focus

1. **Checklist Persistence:** Prep checklist items stored as JSON array in `Interview.PrepChecklist` must safely handle empty, null, or malformed JSON without crashing deserialization.
2. **ICS Calendar Timezones & Formatting:** `.ics` file generation must format UTC datetimes in standard RFC 5545 format (`YYYYMMDDTHHMMSSZ`), include unique `UID`, duration/end time, meeting link/location, and `VALARM` reminder.
3. **Application Timeline Synchronization:** Adding or completing an interview must automatically appear in the application's unified timeline feed.
4. **Interviewer Contacts Many-to-Many Linking:** Interviewers linked via `InterviewContact` must correctly reflect the current user's contacts.
5. **Debrief Self-Rating Bounds:** Self-rating in debrief must be validated between 1 and 5 stars.

---

### Task 1: Backend DTOs, Default Checklist Templates, and IInterviewService Interface

**Files:**
- Create: `backend/src/Pipeline.Application/Features/Interviews/DTOs/InterviewDTOs.cs`
- Create: `backend/src/Pipeline.Application/Features/Interviews/Services/IInterviewService.cs`
- Create: `backend/src/Pipeline.Application/Features/Interviews/Templates/DefaultPrepChecklists.cs`

**Interfaces:**
- Consumes: `InterviewType`, `InterviewFormat`, `InterviewStatus`, `InterviewQuestionCategory` from `Pipeline.Domain.Enums`
- Produces: `IInterviewService`, `InterviewListItemDto`, `InterviewDetailDto`, `CreateInterviewRequest`, `UpdateInterviewRequest`, `UpdateDebriefRequest`, `AddQuestionRequest`, `UpdatePrepChecklistRequest`

- [ ] **Step 1: Define Interview DTOs** in `InterviewDTOs.cs`
- [ ] **Step 2: Define Default Prep Checklists** in `DefaultPrepChecklists.cs` providing curated checklist tasks for `HR`, `Technical`, `Culture`, `Manager`, `Final`, `Assignment`, and `Other`
- [ ] **Step 3: Define `IInterviewService`** interface in `IInterviewService.cs`
- [ ] **Step 4: Verify build with `dotnet build backend/Pipeline.sln`**

---

### Task 2: InterviewService Implementation, Calendar .ICS Generator, and Unit Tests

**Files:**
- Create: `backend/src/Pipeline.Infrastructure/Services/InterviewService.cs`
- Create: `backend/tests/Pipeline.Tests/Unit/InterviewServiceTests.cs`
- Modify: `backend/src/Pipeline.Infrastructure/DependencyInjection.cs` (register `IInterviewService`)
- Modify: `backend/src/Pipeline.Infrastructure/Services/ApplicationService.cs` (include interviews in timeline)

**Interfaces:**
- Consumes: `IInterviewService`, `PipelineDbContext`, `ICurrentUserService`
- Produces: Implemented interview management, automated checklist seeding, question log, debrief recording, RFC 5545 `.ics` calendar generation, and timeline integration

- [ ] **Step 1: Write unit tests in `InterviewServiceTests.cs`** verifying:
  - Default checklist items are automatically populated according to `InterviewType` upon creation.
  - Adding and deleting questions from an interview.
  - Updating checklist item completion states.
  - Updating debrief rating, notes, and thank-you status.
  - Generating valid `.ics` iCalendar text containing `BEGIN:VCALENDAR`, `SUMMARY`, `DTSTART`, `DTEND`, and `UID`.
- [ ] **Step 2: Implement `InterviewService.cs`** in `Pipeline.Infrastructure/Services/`
- [ ] **Step 3: Wire `InterviewService` into `ApplicationService.GetTimelineAsync`** to output Interview timeline items
- [ ] **Step 4: Register service in `DependencyInjection.cs`**
- [ ] **Step 5: Run `dotnet test backend/Pipeline.sln`** and verify all unit tests pass

---

### Task 3: Interviews REST Controllers & Integration Tests

**Files:**
- Create: `backend/src/Pipeline.Api/Controllers/InterviewsController.cs`
- Modify: `backend/src/Pipeline.Api/Controllers/ApplicationsController.cs` (add `/api/applications/{id}/interviews`)
- Create: `backend/tests/Pipeline.Tests/Integration/InterviewsApiTests.cs`

**Interfaces:**
- Consumes: `IInterviewService`, `IApplicationService`
- Produces: REST endpoints:
  - `GET /api/interviews`
  - `GET /api/interviews/{id}`
  - `POST /api/interviews`
  - `PUT /api/interviews/{id}`
  - `PATCH /api/interviews/{id}/debrief`
  - `PUT /api/interviews/{id}/checklist`
  - `POST /api/interviews/{id}/questions`
  - `DELETE /api/interviews/{id}/questions/{questionId}`
  - `DELETE /api/interviews/{id}`
  - `GET /api/interviews/{id}/calendar.ics`
  - `GET /api/applications/{id}/interviews`

- [ ] **Step 1: Write integration tests in `InterviewsApiTests.cs`** verifying:
  - Creating an interview via API and asserting seeded prep checklist
  - Updating debrief rating and answers
  - Downloading `.ics` file with correct MIME type `text/calendar`
  - Multi-tenant isolation: User A cannot see User B's interviews
- [ ] **Step 2: Implement `InterviewsController.cs`**
- [ ] **Step 3: Add interview endpoints to `ApplicationsController.cs`**
- [ ] **Step 4: Run `dotnet test backend/Pipeline.sln`** and verify all integration tests pass

---

### Task 4: Frontend Types, API Client, and TanStack Query Hooks

**Files:**
- Create: `frontend/src/features/interviews/types.ts`
- Create: `frontend/src/features/interviews/interviews-api.ts`
- Create: `frontend/src/features/interviews/useInterviews.ts`

**Interfaces:**
- Consumes: Backend REST endpoints
- Produces: Typed TypeScript interfaces, API methods, and reactive React Query hooks:
  - `useInterviews`, `useInterview`, `useApplicationInterviews`, `useCreateInterview`, `useUpdateInterview`, `useUpdateDebrief`, `useUpdatePrepChecklist`, `useAddQuestion`, `useDeleteQuestion`, `useDeleteInterview`

- [ ] **Step 1: Define TypeScript types and payloads** in `types.ts`
- [ ] **Step 2: Implement Axios API calls** in `interviews-api.ts` including `.ics` download helper
- [ ] **Step 3: Implement TanStack Query hooks and invalidations** in `useInterviews.ts`
- [ ] **Step 4: Verify with `npm run build`** in `frontend/`

---

### Task 5: Interview Prep, Question Log, and Debrief Components

**Files:**
- Create: `frontend/src/features/interviews/components/InterviewCard.tsx`
- Create: `frontend/src/features/interviews/components/ScheduleInterviewModal.tsx`
- Create: `frontend/src/features/interviews/components/PrepChecklistCard.tsx`
- Create: `frontend/src/features/interviews/components/QuestionsLogCard.tsx`
- Create: `frontend/src/features/interviews/components/DebriefCard.tsx`

**Interfaces:**
- Consumes: Hooks from `useInterviews.ts`, UI primitives
- Produces: Modular interview preparation, questioning, and debriefing widgets

- [ ] **Step 1: Implement `InterviewCard.tsx`** with countdown, status badges, and action buttons
- [ ] **Step 2: Implement `ScheduleInterviewModal.tsx`** supporting interview type, format, datetime, duration, meeting link, and interviewers
- [ ] **Step 3: Implement `PrepChecklistCard.tsx`** with checkbox toggles, progress bar, and add custom item input
- [ ] **Step 4: Implement `QuestionsLogCard.tsx`** with categories, difficulty rating, answer input, and prepared flag
- [ ] **Step 5: Implement `DebriefCard.tsx`** with 1-5 star rating, went well / to improve fields, and thank-you note toggle

---

### Task 6: Interview Detail Page, Directory Page, and Application Detail Integration

**Files:**
- Create: `frontend/src/features/interviews/InterviewDetailPage.tsx`
- Create: `frontend/src/features/interviews/InterviewsPage.tsx`
- Create: `frontend/src/features/applications/components/ApplicationInterviewsTab.tsx`
- Modify: `frontend/src/features/applications/ApplicationDetailPage.tsx` (add Interviews tab)
- Modify: `frontend/src/app/router.tsx` (register `/interviews` and `/interviews/:id`)

**Interfaces:**
- Consumes: Task 5 components, routing
- Produces: Complete interview management UI across dedicated pages and application tabs

- [ ] **Step 1: Implement `InterviewDetailPage.tsx`** with header countdown, .ics download button, prep checklist tab, questions log tab, and debrief tab
- [ ] **Step 2: Implement `InterviewsPage.tsx`** directory with upcoming vs completed tabs and filter bar
- [ ] **Step 3: Implement `ApplicationInterviewsTab.tsx`** and wire it into `ApplicationDetailPage.tsx`
- [ ] **Step 4: Register routes `/interviews` and `/interviews/:id`** in `router.tsx`
- [ ] **Step 5: Verify build with `npm run build`**

---

### Task 7: Frontend Unit Tests, Playwright E2E Verification, and Milestone Wrap-Up

**Files:**
- Create: `frontend/tests/InterviewsFeatures.test.tsx`
- Create: `scratch/verify_interviews.py`

**Interfaces:**
- Consumes: Completed frontend and backend
- Produces: Test results, Playwright execution screenshots, and verified git commit

- [ ] **Step 1: Write frontend component tests in `frontend/tests/InterviewsFeatures.test.tsx`** testing:
  - `PrepChecklistCard` checkbox toggle and progress calculation
  - `QuestionsLogCard` question rendering with category badge and difficulty
  - `DebriefCard` star rating and thank-you toggle
- [ ] **Step 2: Run all unit tests** (`dotnet test backend/Pipeline.sln` and `npm test` in `frontend/`)
- [ ] **Step 3: Run production build** (`npm run build` in `frontend/`)
- [ ] **Step 4: Execute live Playwright browser verification** (`verify_interviews.py`) scheduling an interview, toggling checklist items, logging questions, filling debrief, and verifying .ics export
- [ ] **Step 5: Commit changes with git and update progress ledger**
