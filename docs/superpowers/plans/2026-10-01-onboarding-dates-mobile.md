# Onboarding Wizard, Historical Date Logging & Mobile Responsiveness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver an interactive 3-step onboarding wizard and replayable app tour, historical application date logging with future-date blocking, and an adaptive mobile-responsive navigation drawer across the entire web application.

**Architecture:** Extend backend ASP.NET Identity `User` model with onboarding state persistence, enforce date validation in application domain services, build a modular React onboarding wizard with step-by-step preference configuration and tour carousel, and restructure the layout shell with Tailwind CSS responsive drawer mechanics and mobile touch wrappers.

**Tech Stack:** ASP.NET Core 8, EF Core (SQLite / PostgreSQL), React 18, TypeScript, Tailwind CSS, Lucide React, Vitest, Testing Library, Playwright.

**Spec:** [`docs/superpowers/specs/2026-10-01-onboarding-dates-mobile-design.md`](file:///c:/Users/nedim/Desktop/Pipeline/docs/superpowers/specs/2026-10-01-onboarding-dates-mobile-design.md)

## Global Constraints

- Preserve all existing 99 backend tests and 46 frontend tests in green state.
- Maintain multi-tenant data isolation via `IUserOwnedEntity` and `ICurrentUserService`.
- Date pickers for application creation must default to today's date, allow all past dates, and forbid future dates via HTML5 `max` attribute and backend validation.
- Mobile layout must fit 375px (iPhone SE) without horizontal viewport overflow.

## Review Focus

1. **Future Date Submission:** An API request with `AppliedAt` set to a future timestamp must be rejected with 400 Bad Request.
2. **Backfilled Past Date Submission:** An application created with an `AppliedAt` from 6 months ago must successfully save and reflect in application timeline and details.
3. **Onboarding Persistence:** A newly registered user triggers the onboarding wizard; once completed or skipped, refreshing the page never displays the wizard again.
4. **Tour Replay:** Existing users who have completed onboarding can click the "App Tour" button in the header at any time to review the tour slides.
5. **Mobile Drawer Navigation:** Tapping the hamburger menu opens the sliding drawer; tapping a nav link or backdrop immediately closes the drawer and navigates cleanly.

---

### Task 1: Backend Domain & DTOs for Onboarding & Date Validation

**Files:**
- Modify: `backend/src/Pipeline.Domain/Entities/User.cs`
- Modify: `backend/src/Pipeline.Application/Features/Auth/DTOs/AuthDTOs.cs`
- Modify: `backend/src/Pipeline.Application/Features/Auth/Services/AuthService.cs`
- Modify: `backend/src/Pipeline.Application/Features/Applications/Services/ApplicationService.cs`
- Modify: `backend/src/Pipeline.Api/Controllers/ProfileController.cs`
- Test: `backend/tests/Pipeline.Tests/Unit/ApplicationServiceTests.cs`
- Test: `backend/tests/Pipeline.Tests/Unit/AuthServiceTests.cs`

**Interfaces:**
- `User.HasCompletedOnboarding`: `bool`
- `AuthResponseDto`: includes `bool HasCompletedOnboarding`
- `UserProfileDto`: includes `bool HasCompletedOnboarding`
- `ApplicationService.CreateApplicationAsync`: enforces `AppliedAt <= DateTime.UtcNow.AddDays(1)`

- [ ] **Step 1: Write failing tests for future date rejection and past date acceptance**
  Add unit tests in `ApplicationServiceTests.cs` verifying that `AppliedAt` in the future throws `ValidationException`, while past dates succeed.

- [ ] **Step 2: Run tests to verify they fail**
  Run: `dotnet test backend/Pipeline.sln --filter ApplicationServiceTests`
  Expected: FAIL on validation rules.

- [ ] **Step 3: Implement validation in `ApplicationService.cs` and add `HasCompletedOnboarding` to `User.cs` and DTOs**
  Add date boundary check and profile completion endpoint `POST /api/profile/complete-onboarding`.

- [ ] **Step 4: Run tests to verify they pass**
  Run: `dotnet test backend/Pipeline.sln`
  Expected: PASS (all 99+ tests pass).

- [ ] **Step 5: Commit changes**
  Run: `git commit -m "feat(backend): add onboarding user state and application date validation"`

---

### Task 2: Frontend Historical Date Validation in Application Modals

**Files:**
- Modify: `frontend/src/features/applications/components/CreateApplicationModal.tsx`
- Modify: `frontend/src/features/applications/components/EditApplicationModal.tsx`
- Modify: `frontend/src/features/applications/types.ts`
- Test: `frontend/tests/ApplicationsFeatures.test.tsx`

**Interfaces:**
- `CreateApplicationPayload`: includes `appliedAt?: string`
- `UpdateApplicationPayload`: includes `appliedAt?: string`

- [ ] **Step 1: Write failing frontend test in `ApplicationsFeatures.test.tsx`**
  Verify that `CreateApplicationModal` renders an `Application Date` input defaulting to today with `max` set to today's date string, and correctly allows past dates.

- [ ] **Step 2: Run test to verify it fails**
  Run: `npm test -- tests/ApplicationsFeatures.test.tsx`
  Expected: FAIL (input not found).

- [ ] **Step 3: Implement `appliedAt` field with `max={todayString}` in `CreateApplicationModal.tsx` and `EditApplicationModal.tsx`**
  Add date input with calendar icon, client-side validation against future dates, and pass `appliedAt` to mutation payload.

- [ ] **Step 4: Run test to verify it passes**
  Run: `npm test -- tests/ApplicationsFeatures.test.tsx`
  Expected: PASS.

- [ ] **Step 5: Commit changes**
  Run: `git commit -m "feat(frontend): add historical datepicker with future date restriction"`

---

### Task 3: Multi-Step Onboarding Wizard & Guided App Tour

**Files:**
- Create: `frontend/src/features/onboarding/OnboardingWizardModal.tsx`
- Create: `frontend/src/features/onboarding/AppTourModal.tsx`
- Modify: `frontend/src/features/auth/types.ts`
- Modify: `frontend/src/features/auth/AuthContext.tsx`
- Modify: `frontend/src/components/layout/AppLayout.tsx`
- Modify: `frontend/src/components/layout/Header.tsx`
- Test: `frontend/tests/OnboardingFeatures.test.tsx`

**Interfaces:**
- `OnboardingWizardModal`: props `{ isOpen: boolean; onComplete: () => void }`
- `AppTourModal`: props `{ isOpen: boolean; onClose: () => void }`
- `Header`: emits/triggers tour modal via state or custom event `pipeline_open_app_tour`

- [ ] **Step 1: Write failing test in `frontend/tests/OnboardingFeatures.test.tsx`**
  Test wizard step progression (Job Preferences → Tour Slides → Fast Start / Dashboard) and completion callback.

- [ ] **Step 2: Run test to verify it fails**
  Run: `npm test -- tests/OnboardingFeatures.test.tsx`
  Expected: FAIL (component missing).

- [ ] **Step 3: Implement `OnboardingWizardModal.tsx` and `AppTourModal.tsx`**
  Implement step 1 (target role, work mode, salary goal), step 2 (4 interactive tour slides with visual highlights), step 3 (first application quick-add with past datepicker or jump to dashboard), and hook into `AppLayout` when `hasCompletedOnboarding === false`. Add "App Tour" button to `Header.tsx`.

- [ ] **Step 4: Run test to verify it passes**
  Run: `npm test -- tests/OnboardingFeatures.test.tsx`
  Expected: PASS.

- [ ] **Step 5: Commit changes**
  Run: `git commit -m "feat(frontend): implement multi-step onboarding wizard and replayable app tour"`

---

### Task 4: Mobile Responsive Navigation Drawer & Layout Overhaul

**Files:**
- Modify: `frontend/src/components/layout/Sidebar.tsx`
- Modify: `frontend/src/components/layout/AppLayout.tsx`
- Modify: `frontend/src/components/layout/Header.tsx`
- Modify: `frontend/src/features/applications/components/KanbanBoard.tsx`
- Modify: `frontend/src/features/applications/components/ApplicationsTable.tsx`
- Test: `frontend/tests/MobileLayoutFeatures.test.tsx`

**Interfaces:**
- `Sidebar`: props `{ isMobileOpen: boolean; onMobileClose: () => void }`
- `Header`: props `{ onToggleMobileMenu: () => void }`

- [ ] **Step 1: Write failing test for mobile navigation drawer in `MobileLayoutFeatures.test.tsx`**
  Verify hamburger button toggles drawer, and clicking a navigation link triggers `onMobileClose`.

- [ ] **Step 2: Run test to verify it fails**
  Run: `npm test -- tests/MobileLayoutFeatures.test.tsx`
  Expected: FAIL.

- [ ] **Step 3: Implement sliding drawer, backdrop, hamburger header, and touch-scroll containers**
  Update `Sidebar.tsx` with responsive drawer styling (`fixed inset-y-0 left-0 z-50 transform ... md:relative md:translate-x-0`). Add hamburger button to `Header.tsx`. Update `KanbanBoard.tsx` and `ApplicationsTable.tsx` with responsive touch containers.

- [ ] **Step 4: Run test to verify it passes**
  Run: `npm test -- tests/MobileLayoutFeatures.test.tsx`
  Expected: PASS.

- [ ] **Step 5: Commit changes**
  Run: `git commit -m "feat(frontend): implement mobile responsive navigation drawer and layout adaptations"`

---

### Task 5: End-to-End Playwright Verification & Visual Evidence

**Files:**
- Create: `frontend/verify-mobile-and-onboarding.cjs`

- [ ] **Step 1: Write verification script testing mobile viewport (375px), onboarding flow, and date picker**
  Automate browser verification testing:
  - Mobile hamburger menu opening drawer and selecting a section.
  - Onboarding wizard completing preferences and tour.
  - Logging an application with a backfilled historical date (e.g., 2026-08-15) and asserting it renders in the pipeline.

- [ ] **Step 2: Run verification script and capture visual artifacts**
  Capture `mobile_navigation_verified.png`, `onboarding_wizard_verified.png`, and `historical_date_application_verified.png`.

- [ ] **Step 3: Verify all test suites pass with 0 errors**
  Run: `dotnet test backend/Pipeline.sln` and `npm test -- --run` and `npm run build`.

- [ ] **Step 4: Commit changes**
  Run: `git commit -m "test(e2e): verify mobile drawer, onboarding wizard, and historical dates"`
