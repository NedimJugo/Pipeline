# Pipeline Implementation Plan — Milestone 3: Contacts & Interactions

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (native execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full Contacts CRM and Interaction Logging for Pipeline: Contacts CRUD, Company & Application linking, Interaction logging with directional audit (Inbound/Outbound, channels), automated Warmth Indicator calculation (`Hot`, `Warm`, `Cooling`, `Cold`), Contacts directory view with filtering, Contact Detail view with interaction timeline, and Application Detail contacts tab integration.

**Architecture:** ASP.NET Core 8 Web API implementing application commands/queries in `Pipeline.Application`, EF Core repository operations with multi-tenant filtering in `Pipeline.Infrastructure`, and REST controllers in `Pipeline.Api`. Frontend built with React 18, TanStack Query, React Hook Form + Zod, and Tailwind CSS.

**Tech Stack:**
- Backend: C# 12 / .NET 8 Web API, EF Core 8, FluentValidation, xUnit, FluentAssertions, Moq.
- Frontend: React 18, TypeScript, Tailwind CSS, Lucide icons, TanStack Query, React Router v6.

**Spec:** [`docs/superpowers/specs/2026-09-29-pipeline-design.md`](file:///c:/Users/nedim/Desktop/Pipeline/docs/superpowers/specs/2026-09-29-pipeline-design.md) & [`README_to_do.md`](file:///c:/Users/nedim/Desktop/Pipeline/README_to_do.md) sections 7.4, 7.6, 10.

---

## Global Constraints

- Every contact and interaction must belong to `currentUserId` and be automatically filtered by global EF query filters.
- Logging an interaction must update `Contact.LastContactedAt` to `OccurredAt` (if newer), and if `FollowUpRequired` is true, set `Contact.NextFollowUpAt = FollowUpDueAt`.
- Interactions must be linked to both Contact (optional) and Application (optional) and appear in the unified Application Timeline (`GET /api/applications/{id}/timeline`).
- Warmth calculation rule:
  - `Hot`: Last contacted within 14 days.
  - `Warm`: Last contacted 14–30 days ago.
  - `Cooling`: Last contacted 31–60 days ago.
  - `Cold`: > 60 days ago or never contacted (`LastContactedAt == null`).

---

## Tasks Breakdown

### Task 1: Contacts & Interactions DTOs, Service Layer & Unit Tests
- Create `IContactService` and `IInteractionService` in `Pipeline.Application`.
- Implement `ContactService` and `InteractionService` in `Pipeline.Infrastructure`.
- Implement Warmth Indicator calculation and `LastContactedAt` automatic updating.
- Implement Application-Contact linking (`ApplicationContact`).
- Update `ApplicationService.GetTimelineAsync` to include `Interaction` events.
- Unit tests (`ContactServiceTests.cs`) verifying CRUD, warmth calculation, application associations, and user isolation.

### Task 2: Contacts & Interactions REST Controllers & Integration Tests
- Implement `ContactsController` (`GET`, `POST`, `GET /{id}`, `PUT /{id}`, `DELETE /{id}`, `GET /{id}/interactions`, `POST /{id}/link-application`, `DELETE /{id}/link-application/{appId}`).
- Implement `InteractionsController` (`POST`, `GET /{id}`, `DELETE /{id}`).
- Add contacts management endpoints to `ApplicationsController` (`GET /{id}/contacts`, `POST /{id}/contacts`, `DELETE /{id}/contacts/{contactId}`).
- Integration tests (`ContactsApiTests.cs`) covering the full contact and interaction lifecycle and RFC 7807 error responses.

### Task 3: Frontend API Client & TanStack Query Hooks
- Define TypeScript types (`ContactListItem`, `ContactDetail`, `ContactWarmth`, `ContactType`, `Interaction`, `InteractionChannel`, `InteractionDirection`, `LogInteractionPayload`, etc.).
- Implement API client in `frontend/src/features/contacts/contacts-api.ts`.
- Implement TanStack Query hooks: `useContacts`, `useContact`, `useCreateContact`, `useUpdateContact`, `useDeleteContact`, `useContactInteractions`, `useLogInteraction`, `useLinkApplicationContact`.

### Task 4: Contacts Directory Page with Warmth Indicators
- Build `frontend/src/features/contacts/ContactsPage.tsx`.
- Filter bar: Text search, Contact Type filter (Recruiter, Hiring Manager, Interviewer, Referrer, Peer, Other), Company filter, Warmth filter (All, Hot, Warm, Cooling, Cold).
- Contact cards/table with flame warmth badge, role, company, email/phone/LinkedIn quick links, and linked applications chips.
- "Quick Log Interaction" action trigger.

### Task 5: Contact Detail View & Interaction Timeline
- Build `frontend/src/features/contacts/ContactDetailPage.tsx` (`/contacts/:id`).
- Header with contact profile, warmth indicator, last contacted time, and quick actions.
- Tabs:
  1. **Interaction Timeline:** Chronological feed of interactions with channel icons, inbound/outbound badges, summary, sent message body, and follow-up flags.
  2. **Linked Applications:** Applications this contact is tied to, with role in process.
  3. **Notes:** Private notes, recruiter preferences, strategy.
- Register `/contacts/:id` in router.

### Task 6: Interaction Logging Modal & Application Integration
- Build `LogInteractionModal.tsx`:
  - Channel selector (Email, LinkedIn, Phone, Video, InPerson, Message, Other).
  - Direction toggle (Inbound vs Outbound).
  - Date & time picker.
  - Linked Application selector (auto-selected if opened from application detail).
  - Summary / What they said.
  - What I sent.
  - Follow-up required toggle + due date picker.
- Build `CreateContactModal.tsx` and `EditContactModal.tsx`.
- Update `ApplicationDetailPage.tsx`: Add **Contacts** tab showing linked contacts for this specific application, with inline contact creation and interaction logging.

### Task 7: Verification, Tests & Milestone Wrap-Up
- Run backend test suite (`dotnet test backend/Pipeline.sln`).
- Run frontend test suite (`npm test`).
- Run production build (`npm run build`).
- Verify live flow in local server.
- Commit all changes and summarize Milestone 3.
