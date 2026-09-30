# Milestone 6: Tasks and Automation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the complete Next-Action Engine, Tasks and Reminders system, Dashboard "Do today" command center, and Email Template placeholder rendering engine for Pipeline.

**Architecture:** ASP.NET Core 8 Web API backend services (`TaskService`, `EmailTemplateService`, `AutomationRuleEngine`, `DashboardService`) with Hangfire recurring rule evaluation and PostgreSQL/SQLite support, coupled with React 18 + TypeScript frontend features (`TasksPage`, `TemplatesPage`, upgraded `DashboardPage`, `EmailTemplatePickerModal`, `TaskModal`).

**Tech Stack:** ASP.NET Core 8, Entity Framework Core 8, Hangfire, MailKit, React 18, TypeScript, Tailwind CSS, Lucide icons, TanStack Query, Vitest, xUnit, Playwright.

**Spec:** `README_to_do.md` (Sections 5, 7.2, 7.11, 7.12, 8, 10, 16).

---

## Global Constraints

- **Strict Multi-Tenant Isolation:** Every task, reminder, template, and dashboard query must enforce `UserId == currentUserId && DeletedAt == null`.
- **Idempotent Automation Rules:** Each automated task must generate a deterministic `AutoRuleKey` (e.g., `follow_up_after_apply:{appId}`) to guarantee no duplicate tasks are spawned.
- **Offline / Test Resilient Hangfire:** Background automation jobs must run seamlessly in production via Hangfire while gracefully allowing unit tests, integration tests, and local setups to execute synchronously or in-memory without crashing if Postgres is not present.
- **Frontend Design System:** Clean, modern, responsive UI adhering to Tailwind CSS, dark mode compatibility, accessible modals (`Escape` key, backdrop dismissal, `aria-label`), and toast feedback.

---

## Review Focus

1. **Automation rule idempotency under concurrent or repeated runs:** Running the rule engine multiple times must never generate duplicate tasks for the same entity event.
2. **Template placeholder substitution edge cases:** If application company name, contact name, or role title contains special characters, spaces, or is partially missing (null), placeholders like `{{contactName}}` and `{{company}}` must fall back cleanly without throwing or showing raw broken strings.
3. **Task snooze calculation across time zones:** Snoozing a task by 1, 3, or 7 days must set `DueAt` relative to UTC midnight / end-of-day so tasks appear accurately in the "Today" vs "Upcoming" buckets.
4. **Dashboard "Do Today" aggregation performance:** As applications and tasks grow, the dashboard query must execute efficient indexed queries rather than loading all historical records into memory.
5. **Cross-entity navigation and action flow:** Tapping "Open" or "Follow up" on a dashboard or task item must seamlessly route to the corresponding application, contact, or interview with the template picker or relevant drawer open.

---

## Task Decomposition

### Task 1: Backend DTOs & Domain Services for Tasks & Email Templates
**Files:**
- Create: `backend/src/Pipeline.Application/Features/Tasks/DTOs/TaskDTOs.cs`
- Create: `backend/src/Pipeline.Application/Features/Tasks/Services/ITaskService.cs`
- Create: `backend/src/Pipeline.Application/Features/Templates/DTOs/EmailTemplateDTOs.cs`
- Create: `backend/src/Pipeline.Application/Features/Templates/Services/IEmailTemplateService.cs`
- Create: `backend/src/Pipeline.Infrastructure/Services/TaskService.cs`
- Create: `backend/src/Pipeline.Infrastructure/Services/EmailTemplateService.cs`
- Test: `backend/tests/Pipeline.UnitTests/Services/TaskServiceTests.cs`
- Test: `backend/tests/Pipeline.UnitTests/Services/EmailTemplateServiceTests.cs`

**Interfaces:**
- `ITaskService`:
  - `Task<PagedResult<TaskItemDto>> GetTasksAsync(TaskFilterParams filter, CancellationToken ct = default)`
  - `Task<TaskItemDto?> GetTaskByIdAsync(Guid id, CancellationToken ct = default)`
  - `Task<TaskItemDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken ct = default)`
  - `Task<TaskItemDto> UpdateTaskAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)`
  - `Task DeleteTaskAsync(Guid id, CancellationToken ct = default)`
  - `Task<TaskItemDto> CompleteTaskAsync(Guid id, bool isCompleted, CancellationToken ct = default)`
  - `Task<TaskItemDto> SnoozeTaskAsync(Guid id, int days, CancellationToken ct = default)`
- `IEmailTemplateService`:
  - `Task<IReadOnlyList<EmailTemplateDto>> GetTemplatesAsync(EmailTemplateCategory? category = null, CancellationToken ct = default)`
  - `Task<EmailTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken ct = default)`
  - `Task<EmailTemplateDto> CreateTemplateAsync(CreateEmailTemplateRequest request, CancellationToken ct = default)`
  - `Task<EmailTemplateDto> UpdateTemplateAsync(Guid id, UpdateEmailTemplateRequest request, CancellationToken ct = default)`
  - `Task DeleteTemplateAsync(Guid id, CancellationToken ct = default)`
  - `Task<RenderedEmailTemplateDto> RenderTemplateAsync(Guid id, RenderEmailTemplateRequest request, CancellationToken ct = default)`
  - `Task EnsureDefaultTemplatesSeededAsync(Guid userId, CancellationToken ct = default)`

- [ ] **Step 1: Write unit tests for `TaskService` and `EmailTemplateService`**
  - Verify task creation, completion toggling, snooze by 1/3/7 days, filtering (Today, Upcoming, Overdue, Done).
  - Verify template CRUD, 6 seeded default templates, and placeholder substitution (`{{contactName}}`, `{{company}}`, `{{role}}`, `{{myName}}`).
- [ ] **Step 2: Run tests to verify failure**
- [ ] **Step 3: Implement TaskDTOs, EmailTemplateDTOs, TaskService, and EmailTemplateService**
- [ ] **Step 4: Run tests to verify they pass**
- [ ] **Step 5: Commit backend services**

---

### Task 2: Automation Rule Engine & Dashboard Service
**Files:**
- Create: `backend/src/Pipeline.Application/Features/Automation/Services/IAutomationRuleEngine.cs`
- Create: `backend/src/Pipeline.Infrastructure/Services/AutomationRuleEngine.cs`
- Create: `backend/src/Pipeline.Application/Features/Dashboard/DTOs/DashboardDTOs.cs`
- Create: `backend/src/Pipeline.Application/Features/Dashboard/Services/IDashboardService.cs`
- Create: `backend/src/Pipeline.Infrastructure/Services/DashboardService.cs`
- Test: `backend/tests/Pipeline.UnitTests/Services/AutomationRuleEngineTests.cs`
- Test: `backend/tests/Pipeline.UnitTests/Services/DashboardServiceTests.cs`

**Interfaces:**
- `IAutomationRuleEngine`:
  - `Task<int> EvaluateRulesForUserAsync(Guid userId, CancellationToken ct = default)`
  - `Task<int> EvaluateAllActiveUsersAsync(CancellationToken ct = default)`
  - Evaluates the 8 spec rules:
    1. `follow_up_after_apply`: status Applied, applied >= 7 days ago.
    2. `stale_application`: no update for `StaleAfterDays` (default 14).
    3. `thank_you`: interview completed, thank-you not sent (due +24h).
    4. `post_interview_follow_up`: interview completed >= 5 days ago, application still active.
    5. `prep_reminder`: interview scheduled within 48h and checklist not completed.
    6. `offer_deadline`: application in Offer status, offer deadline within 3 days.
    7. `contact_follow_up`: interaction marked `FollowUpRequired`.
    8. `cold_contact`: contact linked to active app, no contact in 21 days.
- `IDashboardService`:
  - `Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken ct = default)`
  - Aggregates greeting, user search status, "Do Today" prioritized items, upcoming interviews (7 days) with countdown and prep bar, weekly stats (sent, replies, interviews, offers), and stale applications with quick actions.

- [ ] **Step 1: Write unit tests for `AutomationRuleEngine` and `DashboardService`**
  - Verify all 8 rule triggers create auto tasks with proper `AutoRuleKey`.
  - Verify idempotency: running twice does not produce duplicates.
  - Verify dashboard summary calculations.
- [ ] **Step 2: Run tests to verify failure**
- [ ] **Step 3: Implement `AutomationRuleEngine` and `DashboardService`**
- [ ] **Step 4: Run tests to verify they pass**
- [ ] **Step 5: Commit automation engine and dashboard service**

---

### Task 3: REST API Controllers & Background Job Wiring
**Files:**
- Create: `backend/src/Pipeline.Api/Controllers/TasksController.cs`
- Create: `backend/src/Pipeline.Api/Controllers/TemplatesController.cs`
- Create: `backend/src/Pipeline.Api/Controllers/DashboardController.cs`
- Create: `backend/src/Pipeline.Api/Controllers/AutomationController.cs`
- Modify: `backend/src/Pipeline.Api/Program.cs` (Register DI services and Hangfire / hosted service)
- Test: `backend/tests/Pipeline.UnitTests/Api/TasksApiTests.cs`

**Interfaces:**
- `TasksController`: `GET /api/tasks`, `POST /api/tasks`, `GET /api/tasks/{id}`, `PUT /api/tasks/{id}`, `DELETE /api/tasks/{id}`, `POST /api/tasks/{id}/complete`, `POST /api/tasks/{id}/snooze`
- `TemplatesController`: `GET /api/templates`, `POST /api/templates`, `GET /api/templates/{id}`, `PUT /api/templates/{id}`, `DELETE /api/templates/{id}`, `POST /api/templates/{id}/render`
- `DashboardController`: `GET /api/dashboard`
- `AutomationController`: `POST /api/automation/evaluate`

- [ ] **Step 1: Write integration tests for API controllers verifying auth and user isolation**
- [ ] **Step 2: Implement controllers and wire services in `Program.cs`**
- [ ] **Step 3: Run full backend test suite (`dotnet test backend/Pipeline.sln`)**
- [ ] **Step 4: Commit API controllers and wiring**

---

### Task 4: Frontend API Clients, Types & State Hooks
**Files:**
- Create: `frontend/src/features/tasks/types.ts`
- Create: `frontend/src/features/tasks/tasks-api.ts`
- Create: `frontend/src/features/tasks/useTasks.ts`
- Create: `frontend/src/features/templates/types.ts`
- Create: `frontend/src/features/templates/templates-api.ts`
- Create: `frontend/src/features/templates/useTemplates.ts`
- Create: `frontend/src/features/dashboard/types.ts`
- Create: `frontend/src/features/dashboard/dashboard-api.ts`
- Create: `frontend/src/features/dashboard/useDashboard.ts`

- [ ] **Step 1: Define TypeScript models matching backend DTOs**
- [ ] **Step 2: Implement API clients using `api-client.ts` with error handling**
- [ ] **Step 3: Implement TanStack Query hooks with optimistic updates and invalidation**
- [ ] **Step 4: Verify types with `npx tsc --noEmit`**
- [ ] **Step 5: Commit frontend data layer**

---

### Task 5: Email Templates UI & Picker Modal
**Files:**
- Create: `frontend/src/features/templates/TemplateModal.tsx`
- Create: `frontend/src/features/templates/EmailTemplatePickerModal.tsx`
- Create: `frontend/src/features/templates/TemplatesPage.tsx`
- Modify: `frontend/src/app/router.tsx` (Route `/templates` to `TemplatesPage`)
- Modify: `frontend/src/features/applications/ApplicationDetailPage.tsx` ("Send follow-up" button opens picker)
- Modify: `frontend/src/features/interviews/InterviewDetailPage.tsx` ("Send thank-you" button opens picker)

- [ ] **Step 1: Implement `EmailTemplatePickerModal`**
  - Select template, auto-render placeholders with selected application and contact.
  - "Copy to clipboard" button with visual confirmation.
  - "Open in mail app" (`mailto:`) button.
- [ ] **Step 2: Implement `TemplateModal` and `TemplatesPage`**
  - Category filters, system badge, placeholder cheat-sheet, template preview.
- [ ] **Step 3: Connect "Send follow-up" in `ApplicationDetailPage` and "Send thank-you" in `InterviewDetailPage`**
- [ ] **Step 4: Verify with React tests**
- [ ] **Step 5: Commit templates UI**

---

### Task 6: Tasks Management UI & Modal
**Files:**
- Create: `frontend/src/features/tasks/TaskModal.tsx`
- Create: `frontend/src/features/tasks/TaskCard.tsx`
- Create: `frontend/src/features/tasks/TasksPage.tsx`
- Modify: `frontend/src/app/router.tsx` (Route `/tasks` to `TasksPage`)

- [ ] **Step 1: Implement `TaskModal` for creating/editing tasks with due dates, notes, and entity links**
- [ ] **Step 2: Implement `TaskCard` with quick complete, snooze menu (1d, 3d, 1w), link chips, and auto rule badge**
- [ ] **Step 3: Implement `TasksPage` with Today / Upcoming / Overdue / Done / All tabs, search, and filters**
- [ ] **Step 4: Verify with React tests**
- [ ] **Step 5: Commit tasks UI**

---

### Task 7: Dashboard "Do Today" & Next-Action Command Center
**Files:**
- Modify: `frontend/src/features/dashboard/DashboardPage.tsx`
- Create: `frontend/src/features/dashboard/DoTodayList.tsx`
- Create: `frontend/src/features/dashboard/UpcomingInterviewsWidget.tsx`
- Create: `frontend/src/features/dashboard/StaleApplicationsWidget.tsx`
- Create: `frontend/src/features/dashboard/QuickAddMenu.tsx`

- [ ] **Step 1: Upgrade `DashboardPage` to consume `useDashboard`**
- [ ] **Step 2: Build `DoTodayList` with one-tap actions (Done, Snooze 1d/3d/1w, Open)**
- [ ] **Step 3: Build `UpcomingInterviewsWidget` with countdown and prep progress bar**
- [ ] **Step 4: Build `StaleApplicationsWidget` with "Follow up" and "Mark ghosted"**
- [ ] **Step 5: Build `QuickAddMenu` for instant creation of Application, Contact, Interaction, or Task**
- [ ] **Step 6: Verify with React unit and component tests**
- [ ] **Step 7: Commit dashboard upgrades**

---

### Task 8: End-to-End Verification & Playwright Automated Browser Test
**Files:**
- Create: `backend/tests/Pipeline.UnitTests/Automation/FullRuleEngineIntegrationTests.cs`
- Create: `frontend/src/features/tasks/TasksAndAutomation.test.tsx`
- Create: `playwright-milestone6-verify.js` (E2E browser test script)

- [ ] **Step 1: Run complete backend test suite (`dotnet test backend/Pipeline.sln`)**
- [ ] **Step 2: Run frontend test suite (`npm test`) and build check (`npm run build`)**
- [ ] **Step 3: Launch backend and frontend, run Playwright verification covering:**
  - Login → Dashboard "Do today" view
  - Create manual task → Complete task → Snooze task
  - Run automation rule evaluation → New auto-generated task appears in "Do today"
  - Open Email Template Picker modal → Substitute placeholders (`{{contactName}}`, `{{company}}`) → Copy / Mailto action
  - Stale application quick action ("Mark ghosted" / "Follow up")
- [ ] **Step 4: Save verified screenshots to brain directory**
- [ ] **Step 5: Update documentation and record git commit**
