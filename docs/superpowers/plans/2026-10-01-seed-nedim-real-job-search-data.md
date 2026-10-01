# Implementation Plan: Seed Real Job Search Data (Nedim Jugo)

Populate the database and web application with Nedim Jugo's complete real job application records, including 24 companies/applications, 27 contacts, 62 interactions (verbatim messages and notes), historical dates, status transitions, interview records, job offer, and candidate profile.

## Proposed Architecture & Design

1. **`NedimDataSeeder.cs`** (`backend/src/Pipeline.Infrastructure/Persistence/NedimDataSeeder.cs`):
   - Fully standalone, idempotent C# seeder.
   - Checks if user `nedim.jugoo@gmail.com` exists. If not, creates the user with profile data:
     - `DisplayName`: "Nedim Jugo"
     - `Email`: "nedim.jugoo@gmail.com"
     - `PhoneNumber`: "+387 60 318 5869"
     - `Location`: "Mostar, BiH"
     - `TargetRole`: "Junior Software Developer / Junior Project Manager"
     - `Seniority`: "Junior"
     - `SalaryExpectationMin`: 1750
     - `Currency`: "BAM"
     - `SearchStatus`: Active
     - `OnboardingCompleted`: true
   - Seeds 24 Companies with websites, notes, locations.
   - Seeds 27 Contacts linked to companies and user.
   - Seeds 24 Applications with correct `AppliedAt` dates, work modes, and statuses:
     - Ghosted (9): Raiffeisen, HTEC, Softray, NLB, ASA, Ziraat, Salt Square, Infobip, Lidl
     - Rejected (11): ITO, Galeyo, SaaS Solutions, BH Telecom, Endava, Port8, Ministry of Programming, XSoft, ConfigPOS, Bloomteq, Evona
     - Applied (3): ZIRA Group, Manpower, Popcorn Recruiters
     - Withdrawn (1): UniCredit Bank (with offer: Call centar, 1450 BAM, declined)
   - Seeds 62 Interactions with exact verbatim message texts, directions, channels, and timestamps.
   - Seeds `ApplicationStatusHistory` entries representing realistic status lifecycles.
   - Seeds `Interview` for Ministry of Programming screening call.
   - Seeds `TaskItem`s for follow-up actions (ZIRA Dev ZTA, HTEC web tracking, etc.).

2. **Integration into Backend Lifecycle**:
   - `Program.cs`: Run `await NedimDataSeeder.SeedAsync(db, userManager)` during relational database startup.
   - `MeController.cs` & `IUserService`: Endpoint `POST /api/me/seed-nedim-data` to allow any active session to seed/re-sync this exact data.
   - `AuthService.cs`: Ensure seamless registration/login for `nedim.jugoo@gmail.com`.

3. **Frontend Integration**:
   - `frontend/src/features/settings/settings-api.ts`: Add `seedNedimData` API call.
   - `frontend/src/features/settings/DataManagementTab.tsx`: Add dedicated section "Import Nedim's Real Applications & Interactions".

4. **Testing & Verification**:
   - Backend unit/integration tests confirming 24 applications, 27 contacts, and 62 interactions.
   - Frontend tests confirming settings seed trigger.
   - E2E Playwright test verifying Kanban board, application detail page with interactions, and contacts table.

## Tasks

### Task 1: Create `NedimDataSeeder.cs` with Full Data Dictionary
- File: `backend/src/Pipeline.Infrastructure/Persistence/NedimDataSeeder.cs`
- Contains all 24 firms, 27 contacts, 62 verbatim interactions, offer, status histories, and candidate profile.
- Verification: Compile and unit test in `Pipeline.Tests`.

### Task 2: Wire Seeder into Backend API & User Service
- Files:
  - `backend/src/Pipeline.Api/Program.cs`
  - `backend/src/Pipeline.Application/Features/Users/Services/IUserService.cs`
  - `backend/src/Pipeline.Infrastructure/Services/UserService.cs`
  - `backend/src/Pipeline.Api/Controllers/MeController.cs`
  - `backend/src/Pipeline.Infrastructure/Services/AuthService.cs`
- Verification: `dotnet test backend/Pipeline.sln` passes 100%.

### Task 3: Add Frontend Seed Trigger in Data Management
- Files:
  - `frontend/src/features/settings/types.ts`
  - `frontend/src/features/settings/settings-api.ts`
  - `frontend/src/features/settings/useSettings.ts`
  - `frontend/src/features/settings/DataManagementTab.tsx`
- Verification: `npm test` and `npm run build` pass.

### Task 4: E2E Playwright Verification & Visual Evidence
- File: `frontend/verify-nedim-data.cjs`
- Verifies:
  - Login as `nedim.jugoo@gmail.com`
  - Applications list/Kanban shows 24 applications with statuses (Ghosted, Rejected, Applied, Withdrawn)
  - Opening an application (e.g. Ministry of Programming or SaaS Solutions) displays the verbatim interactions and contacts
  - Contacts page lists the recruiters and referrers
  - Captures visual proof artifact `nedim_real_data_verified.png`.
