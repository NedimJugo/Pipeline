# User Integration Settings & Project README Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow users to safely configure external services (SMTP email delivery, Google Calendar client credentials, and file storage preferences) directly through an "Integrations" tab in Settings with fallback to `.env`, and deliver a comprehensive, modern `README.md` documenting zero-dependency local runs, full feature capabilities, and production deployment.

**Architecture:** Extend backend `User` entity with an encrypted or structured `UserIntegrationSettings` relationship, expose REST endpoints with test-connection actions, integrate a new "Integrations" tab in the frontend Settings module with sensitive field toggles and fallback status badges, and overhaul `README.md` with complete architecture maps, local/docker guides, and feature walk-throughs.

**Tech Stack:** ASP.NET Core 8, EF Core (SQLite / PostgreSQL), MailKit / SmtpClient, React 18, TypeScript, Tailwind CSS, Lucide React, Vitest, Playwright.

**Spec / Scope Reference:**
- Infrastructure settings (`ConnectionStrings`, `POSTGRES_*`, `Jwt__Secret`, `DOMAIN`, `Frontend__Origin`) remain in `.env` / environment variables for server bootstrap stability and cryptographic security.
- Service settings (`Smtp__*`, `Google__*`, `Storage__*`) can be set per user in the database via the Settings UI. When unset in the database, the backend gracefully falls back to the server environment variables (`.env`).

## Global Constraints

- Preserve all existing 101 backend tests and 52 frontend tests in green state.
- Maintain multi-tenant data isolation via `IUserOwnedEntity` and `ICurrentUserService`.
- Sensitive fields (SMTP password, Google client secret) must never be returned in plain text on `GET` requests (masked with `••••••••`).
- Mobile layout compliance: the new Integrations tab and forms must fit 375px screens with responsive wrapping.
- All new and existing documentation in `README.md` must accurately reflect both the zero-dependency SQLite local workflow and the Docker stack.

## Review Focus

1. **Fallback Priority:** If a user has not entered custom SMTP settings in the UI, outbound emails and test pings must use the host `.env` configuration.
2. **Password Masking:** Fetching integration settings must return whether a password/secret is set (`hasPassword: true`) without exposing raw secrets in JSON responses.
3. **SMTP Test Action:** Testing SMTP credentials from the UI must return immediate success or meaningful error messages without crashing the backend background runner.
4. **Local Developer Experience in README:** A developer cloning the repo should be able to run `dotnet run` + `npm run dev` with SQLite without installing or booting Docker.
5. **Mobile Viewport on Integrations Tab:** Forms, input groups, and action buttons must stack neatly on mobile screens without horizontal clipping.

---

### Task 1: Backend Domain, DTOs & API for User Integration Settings

**Files:**
- Create: `backend/src/Pipeline.Domain/Entities/UserIntegrationSetting.cs`
- Modify: `backend/src/Pipeline.Domain/Entities/User.cs`
- Modify: `backend/src/Pipeline.Infrastructure/Persistence/PipelineDbContext.cs`
- Create: `backend/src/Pipeline.Application/Features/Settings/DTOs/IntegrationDTOs.cs`
- Create: `backend/src/Pipeline.Application/Features/Settings/Services/IIntegrationService.cs`
- Create: `backend/src/Pipeline.Application/Features/Settings/Services/IntegrationService.cs`
- Create: `backend/src/Pipeline.Api/Controllers/IntegrationsController.cs`
- Modify: `backend/src/Pipeline.Infrastructure/Services/EmailNotificationService.cs`
- Test: `backend/tests/Pipeline.Tests/Unit/IntegrationServiceTests.cs`
- Test: `backend/tests/Pipeline.Tests/Integration/IntegrationsApiTests.cs`

**Interfaces:**
- `IntegrationSettingsDto`:
  - `smtpHost?: string`, `smtpPort?: int`, `smtpUser?: string`, `hasSmtpPassword: bool`, `smtpFrom?: string`, `useCustomSmtp: bool`, `isEnvFallbackSmtp: bool`
  - `googleClientId?: string`, `hasGoogleClientSecret: bool`, `useCustomGoogle: bool`
  - `storageProvider?: string`
- `UpdateIntegrationSettingsRequest`:
  - `smtpHost?: string`, `smtpPort?: int`, `smtpUser?: string`, `smtpPassword?: string`, `smtpFrom?: string`, `useCustomSmtp: bool`
  - `googleClientId?: string`, `googleClientSecret?: string`, `useCustomGoogle: bool`
  - `storageProvider?: string`
- `TestEmailRequest`: `{ targetEmail: string }`
- Endpoints:
  - `GET /api/settings/integrations`
  - `PUT /api/settings/integrations`
  - `POST /api/settings/integrations/test-email`

- [ ] **Step 1: Write failing unit test for `IntegrationService`**
  Verify that when user settings are empty, fallback values from configuration are reported, and updating integration settings persists customized values.
- [ ] **Step 2: Run test to verify it fails**
  Run: `dotnet test backend/Pipeline.sln --filter IntegrationServiceTests`
  Expected: FAIL.
- [ ] **Step 3: Implement domain model, service, controller, and email dispatcher fallback**
  Implement `UserIntegrationSetting`, `IntegrationService`, and `IntegrationsController`. Ensure `EmailNotificationService` resolves custom user SMTP settings before falling back to system configuration.
- [ ] **Step 4: Run backend tests to verify they pass**
  Run: `dotnet test backend/Pipeline.sln`
  Expected: PASS (all 101+ tests pass).
- [ ] **Step 5: Commit changes**
  Run: `git commit -m "feat(backend): add user integration settings with env fallback and test email endpoint"`

---

### Task 2: Frontend Integrations Tab in Settings

**Files:**
- Create: `frontend/src/features/settings/IntegrationsTab.tsx`
- Create: `frontend/src/features/settings/integrations-api.ts`
- Modify: `frontend/src/features/settings/types.ts`
- Modify: `frontend/src/features/settings/useSettings.ts`
- Modify: `frontend/src/features/settings/SettingsPage.tsx`
- Test: `frontend/tests/SettingsIntegrationsFeatures.test.tsx`

**Interfaces:**
- `IntegrationsTab`: renders 3 clear sections:
  1. **Email & Notifications (SMTP):** Custom SMTP toggle, Host, Port, User, Password (with show/hide), Sender From, and a "Send Test Email" modal/button. Displays an "Active (.env default)" or "Active (Custom)" badge.
  2. **Google & Calendar Sync:** Client ID and Secret with OAuth setup instructions and status.
  3. **File Storage Preference:** Toggle between Local disk and Cloud S3/MinIO.
- `SettingsPage`: includes new tab button `<Plug className="h-4 w-4" /> Integrations`.

- [ ] **Step 1: Write failing frontend test in `SettingsIntegrationsFeatures.test.tsx`**
  Verify `IntegrationsTab` renders SMTP and Google sections, supports saving changes, and triggers test email.
- [ ] **Step 2: Run test to verify it fails**
  Run: `npm test -- tests/SettingsIntegrationsFeatures.test.tsx`
  Expected: FAIL.
- [ ] **Step 3: Implement `IntegrationsTab.tsx` and wire into `SettingsPage.tsx`**
  Build clean Tailwind UI with sensitive field masking, status indicators, and test button.
- [ ] **Step 4: Run test to verify it passes**
  Run: `npm test -- tests/SettingsIntegrationsFeatures.test.tsx`
  Expected: PASS.
- [ ] **Step 5: Commit changes**
  Run: `git commit -m "feat(frontend): add integrations tab to settings with smtp and service configuration"`

---

### Task 3: Comprehensive, World-Class `README.md` Overhaul

**Files:**
- Modify: `README.md`

**Content Requirements:**
1. **Hero & Badges:** Title, value proposition, badges (.NET 8, React 18, TypeScript, Tailwind, Docker, PWA).
2. **Interactive Visual Showcase:** Links to key screenshots and walkthroughs (Mobile Drawer, Onboarding Wizard, Kanban Pipeline, STAR Interview Prep, Offer Comparison, Analytics).
3. **Fast Start Options:**
   - **Option A (Zero-Docker Lightweight Local Run):** Windows/macOS/Linux with SQLite (.NET 8 + Node 18+). Run in 2 commands.
   - **Option B (Full Docker Compose Stack):** Postgres 16, MinIO S3, Mailpit SMTP, API, Frontend, and Caddy reverse proxy.
4. **Complete Feature Catalog:**
   - Visual Kanban & Table with historical date backfilling & future date restrictions.
   - 3-step Onboarding Wizard & Replayable Command Center Tour.
   - Mobile-First Responsive Shell (sliding drawer, touch-scroll snapping, compact header).
   - In-app Integrations Management (SMTP, Google, S3 Storage with fallback to `.env`).
   - Daily "Do Today" prioritization & automated task generation.
   - Contact warmth scoring & touchpoint tracking.
   - STAR debriefing questions & ICS calendar subscription.
   - Resume tailoring & version conversion yield analytics.
   - Offer comparison matrix & GDPR data archive export.
5. **Configuration Architecture:** Clear explanation of `.env` (infrastructure bootstrap) vs Settings UI (per-user services).
6. **Project Structure Tree & Commands:** Solution anatomy, test execution (`dotnet test`, `npm test`), and production build instructions.
7. **Developer FAQ & Troubleshooting:** Windows DLL file locks, port selection, and SQLite database migration tips.

- [ ] **Step 1: Draft and review the full `README.md` document**
  Ensure all sections, commands, paths, and environment variables are 100% accurate.
- [ ] **Step 2: Update `README.md`**
  Write the completed documentation.
- [ ] **Step 3: Commit changes**
  Run: `git commit -m "docs: overhaul README with local sqlite guides, feature catalog, and integrations reference"`

---

### Task 4: Playwright E2E Verification & Visual Evidence

**Files:**
- Create: `frontend/verify-integrations-tab.cjs`

- [ ] **Step 1: Write Playwright verification script testing Integrations tab**
  Automate browser navigation to `/settings`, clicking "Integrations" tab, updating custom SMTP settings, verifying fallback status badges, and validating mobile responsiveness at 375px.
- [ ] **Step 2: Run verification script and capture visual artifact**
  Capture `settings_integrations_verified.png` in the artifact directory.
- [ ] **Step 3: Run all backend and frontend test suites and production build**
  Run: `dotnet test backend/Pipeline.sln`, `npm test`, `npm run build`.
- [ ] **Step 4: Commit changes**
  Run: `git commit -m "test(e2e): verify integrations settings tab and mobile layout"`
