# Onboarding Wizard, Historical Date Logging & Mobile Responsiveness Design Spec

**Date:** 2026-10-01  
**Status:** Approved  
**Author:** Antigravity & User  

---

## 1. Overview & Goals

This specification details three interconnected enhancements to the **Pipeline** personal job search command center:

1. **Multi-Step Onboarding & App Tour:** Replace the barebones registration redirect with a structured 3-step setup wizard (Job Search Preferences → Interactive 4-slide Command Center Tour → Fast-Start Application). Introduce an in-app replayable tour button in the header.
2. **Historical Application Date Logging & Future Date Restriction:** Allow job seekers to backfill previous applications with arbitrary past dates while strictly prohibiting future dates both in the UI and backend validation.
3. **Full Mobile Responsive Shell & Views:** Overhaul the layout for mobile viewports (375px–430px) using a sliding drawer sidebar, mobile hamburger menu, touch-friendly horizontal scrolling for Kanban, and responsive modal viewports.

---

## 2. Multi-Step Onboarding Wizard & Guided Tour

### 2.1 User State & Backend Persistence
* **Entity Update:** Add `HasCompletedOnboarding` (`bool`, default `false`) to `User` entity (`Pipeline.Domain/Entities/User.cs`).
* **DTO Updates:**
  * Return `HasCompletedOnboarding` in `AuthResponseDto` and `UserProfileDto`.
  * Add endpoint `POST /api/profile/complete-onboarding` (or include `HasCompletedOnboarding` in `UpdateProfileDto`) to persist completion.
* **Frontend Routing & Trigger:**
  * In `AppLayout.tsx`, if `user?.hasCompletedOnboarding === false`, mount `OnboardingWizardModal`.
  * In `Header.tsx`, add a `HelpCircle` icon button labeled "App Tour" to re-trigger the tour modal at any time.

### 2.2 The 3 Wizard Steps
1. **Step 1: Job Search Profile & Preferences**
   * Target Role Title (e.g., "Senior Software Engineer")
   * Preferred Work Mode (`Remote`, `Hybrid`, `Onsite`, `Flexible`)
   * Target Minimum Salary & Currency (e.g., `140000`, `USD`)
   * Current Search Status (`Actively Looking`, `Interviewing`, `Casually Looking`)
   * Action: Saves preferences to `/api/profile` via `useProfile` mutation.
2. **Step 2: Interactive App Introduction (4 Slides)**
   * Slide 1: **Today Dashboard** — Prioritized "Do Today" queue, stale alerts, and interview prep.
   * Slide 2: **Applications Pipeline** — Drag-and-drop Kanban & tabular views from Wishlist to Offer.
   * Slide 3: **Contacts CRM & Documents** — Referees, recruiters, and resume versioning with conversion stats.
   * Slide 4: **Discovery & Command Palette** — Job search feed, ICS calendar sync, and `Ctrl+K` instant search.
3. **Step 3: Fast Start / First Application**
   * Quick form to add their first application (Company, Role, Status, and Date Applied).
   * Alternative primary button: "Jump straight to Dashboard" with pre-configured profile.
   * Action: Marks `HasCompletedOnboarding = true`.

---

## 3. Historical Date Logging (`appliedAt`)

### 3.1 Frontend (`CreateApplicationModal.tsx` & `EditApplicationModal.tsx`)
* **Input Field:** Add `Application Date` (`appliedAt`, type `date`).
* **Default:** Current local date `new Date().toISOString().split('T')[0]`.
* **Attributes:** `max={todayDateString}` to restrict datepickers from selecting future dates.
* **Client Validation:** Validate `appliedAt <= todayDateString`. Display inline error if a future date is manually typed.
* **Payload:** Send `appliedAt` in `CreateApplicationDto` and `UpdateApplicationDto`.

### 3.2 Backend Validation (`ApplicationService.cs` & `ApplicationDTOs.cs`)
* Enforce:
  ```csharp
  if (dto.AppliedAt.HasValue && dto.AppliedAt.Value > DateTime.UtcNow.AddDays(1))
  {
      throw new ValidationException("Application date cannot be in the future.");
  }
  ```
* If `AppliedAt` is not provided and application status is `Applied` or beyond, default to `DateTime.UtcNow`.
* If status is `Wishlist`, `AppliedAt` remains null unless specified.

---

## 4. Mobile Responsive Layout & Drawer Navigation

### 4.1 Navigation Shell Architecture
* **Breakpoints:** Tailwind `md` (`768px`).
* **Desktop (`≥ 768px`):** Fixed `w-64` sticky sidebar, as currently rendered.
* **Mobile (`< 768px`):**
  * Fixed sidebar hidden (`hidden md:flex`).
  * Sliding drawer (`fixed inset-y-0 left-0 z-50 w-72 bg-card border-r border-border transform transition-transform duration-200`).
  * Darkened backdrop (`fixed inset-0 bg-black/60 z-40 backdrop-blur-xs`).
  * Tapping any navigation link or backdrop triggers drawer close.
* **Header Updates (`Header.tsx`):**
  * Add hamburger button (`Menu` icon) on `md:hidden` at the left.
  * Search button collapses to icon-only on narrow screens (`< 640px`).
  * User profile details collapse into avatar badge only on small screens.

### 4.2 Views & Modals Adaptation
* **Kanban Board (`KanbanBoard.tsx`):** Add `overflow-x-auto flex gap-4 pb-4 snap-x` with min-width column wrappers so columns swipe smoothly on touch screens.
* **Applications Table (`ApplicationsTable.tsx`):** Add `overflow-x-auto min-w-[700px]` scroll container.
* **Modals:** Update all modals to `w-full max-w-2xl max-h-[90vh] mx-3 sm:mx-auto` so they fit inside 375px screens with safe bottom padding.
* **Today Dashboard (`TodayDashboardPage.tsx`):** Grid collapses from `grid-cols-3` down to `grid-cols-1` on mobile.

---

## 5. Testing & Verification Plan

### 5.1 Automated Tests
* **Backend Unit Tests:**
  * `ApplicationServiceTests`: Verify that creating or updating an application with a past `AppliedAt` succeeds.
  * `ApplicationServiceTests`: Verify that creating or updating an application with a future `AppliedAt` throws `ValidationException`.
  * `ProfileTests`: Verify `HasCompletedOnboarding` toggles and persists.
* **Frontend Tests:**
  * `OnboardingWizard.test.tsx`: Test step progression (Preferences → Tour → First Application / Skip) and completion callback.
  * `CreateApplicationModal.test.tsx`: Test that date picker defaults to today, has `max` attribute set to today, and sends `appliedAt`.
  * `MobileLayout.test.tsx`: Test hamburger button toggling drawer and auto-closing on nav item click.

### 5.2 Playwright E2E Verification
* Verify mobile drawer opens, navigates, and closes at 375px mobile viewport.
* Verify onboarding wizard renders on new account registration, completes steps, and redirects to dashboard.
* Verify date picker restricts future dates and successfully logs a past application date.
