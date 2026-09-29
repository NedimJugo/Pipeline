# Pipeline Implementation Plan — Milestone 2: Applications & Companies

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (native execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full Applications and Companies capabilities for Pipeline: Companies search/CRUD, Applications CRUD with multi-field filtering, automatic status history tracking on stage transitions, unified application timeline, Kanban board (`@dnd-kit`) with status columns and collapsed closed lane, sortable Table view, and Application Detail view with tabbed navigation (Overview, Timeline, Job Description with keyword highlighting, Contacts, Interviews, Documents, Offer, Closing).

**Architecture:** ASP.NET Core 8 Web API implementing application commands/queries in `Pipeline.Application`, EF Core repository operations with multi-tenant filtering in `Pipeline.Infrastructure`, and REST controllers in `Pipeline.Api`. Frontend built with React 18, `@dnd-kit/core` & `@dnd-kit/sortable` for Kanban, TanStack Query for caching and optimistic updates, React Hook Form + Zod for application creation/editing modals.

**Tech Stack:**
- Backend: C# 12 / .NET 8 Web API, EF Core 8, FluentValidation, xUnit, FluentAssertions, Moq.
- Frontend: React 18, TypeScript, Tailwind CSS, `@dnd-kit/core`, `@dnd-kit/sortable`, Lucide icons, TanStack Query, React Router v6.

**Spec:** [`docs/superpowers/specs/2026-09-29-pipeline-design.md`](file:///c:/Users/nedim/Desktop/Pipeline/docs/superpowers/specs/2026-09-29-pipeline-design.md) & [`README_to_do.md`](file:///c:/Users/nedim/Desktop/Pipeline/README_to_do.md) sections 7.3, 7.4, 10.

## Global Constraints

- Every application and company must belong to `currentUserId` and be automatically filtered by global EF query filters.
- Updating an application's `Status` must create an `ApplicationStatusHistory` record capturing `FromStatus`, `ToStatus`, `ChangedAt`, and optional `Note`.
- Status transitions to `Rejected`, `Withdrawn`, `Ghosted`, or `Declined` must require/allow capturing `RejectionStage`, `ClosedReason`, and `LessonsLearned`.
- The Kanban board must support moving cards between columns via drag-and-drop and reflect status changes optimistically.
- Full-text search endpoint `GET /api/search?q=` must search across applications (RoleTitle, Notes, JobDescription) and companies (Name).

---

## Tasks Breakdown

- **Task 1: Companies & Applications DTOs, Service Layer & Unit Tests**
  - Implement `ICompanyService` and `IApplicationService`.
  - Implement Status change logic recording `ApplicationStatusHistory`.
  - Implement timeline aggregator for `GET /api/applications/{id}/timeline`.
  - TDD tests verifying CRUD, status history recording, and user isolation.

- **Task 2: Applications & Companies REST Controllers & Search API**
  - Implement `CompaniesController` (`GET /api/companies/search`, `POST`, `GET /{id}`, `PUT`).
  - Implement `ApplicationsController` (`GET`, `POST`, `GET /{id}`, `PUT /{id}`, `DELETE /{id}`, `PATCH /{id}/status`, `GET /{id}/timeline`, `POST /{id}/duplicate`).
  - Implement `SearchController` (`GET /api/search?q=`).
  - Integration tests verifying all endpoints and RFC 7807 error responses.

- **Task 3: Frontend API Client & Application Services**
  - Define TypeScript types (`Application`, `Company`, `ApplicationStatus`, `TimelineEvent`, etc.).
  - Implement TanStack Query hooks: `useApplications`, `useApplication`, `useUpdateApplicationStatus`, `useCreateApplication`, `useDeleteApplication`, `useSearchCompanies`.

- **Task 4: Kanban Board View with Drag & Drop (`@dnd-kit`)**
  - Build `KanbanBoard` component with columns for `Wishlist`, `Applied`, `Screening`, `Interview`, `Assignment`, `Offer`, `Accepted`.
  - Build `KanbanCard` with company initials, role, days in stage, priority badge, and excitement stars.
  - Build Collapsed "Closed" lane for terminal statuses (`Rejected`, `Withdrawn`, `Ghosted`, `Declined`).
  - Implement smooth drag-and-drop with optimistic updates.

- **Task 5: Table View & Multi-Factor Filtering Bar**
  - Build `ApplicationsTable` view with sortable columns: Role, Company, Status, Work Mode, Salary, Applied Date, Priority, Days in Stage.
  - Build filter controls: Status filter, Work mode filter, Priority filter, Source filter, and text search input.
  - Add view switcher toggle (Kanban vs Table).

- **Task 6: Application Detail View & Tabbed Workspace**
  - Build `ApplicationDetailPage` (`/applications/:id`).
  - Header with Status stepper and quick action buttons (Duplicate, Delete, Edit).
  - Tabs:
    1. **Overview:** Company details, role info, salary range, excitement, pros/cons, notes.
    2. **Timeline:** Chronological activity feed showing status changes and timestamped notes.
    3. **Job Description:** Formatted description with auto-highlighted technical keyword chips.
    4. **Closing Details:** Modal and tab section for rejected/declined applications.
- **Task 7: Create & Edit Application Modal**
  - Modal form using React Hook Form + Zod.
  - Company autocomplete / inline creation.
  - Role title, job URL, work mode, salary expectation, priority, notes.

- **Task 8: End-to-End Verification & Browser Testing**
  - Run full backend tests.
  - Run frontend test suite.
  - Verify live Kanban drag-and-drop, creation, filtering, and tab navigation.
