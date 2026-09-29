# Pipeline Implementation Plan — Milestone 1: Foundation & Project Core

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (native execution) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish the complete project foundation for Pipeline: solution structure, docker-compose development environment, PostgreSQL EF Core context with multi-tenant global query filters, ASP.NET Identity with JWT access + rotating refresh tokens in httpOnly cookies, health check endpoint, test infrastructure with user-isolation tests, and the React 18 + TypeScript + Vite frontend shell with routing, layout, and theme toggling.

**Architecture:** ASP.NET Core 8 Web API structured across Clean Architecture layers (`Pipeline.Domain`, `Pipeline.Application`, `Pipeline.Infrastructure`, `Pipeline.Api`, and `Pipeline.Tests`). The frontend is a React 18 + Vite SPA using Tailwind CSS, shadcn/ui components, TanStack Query, and Zustand. All user-owned data is strictly partitioned via EF Core global query filters.

**Tech Stack:** 
- Backend: C# 12 / .NET 8 / ASP.NET Core 8 Web API, EF Core 8, PostgreSQL (Npgsql), ASP.NET Identity, System.IdentityModel.Tokens.Jwt, FluentValidation, Serilog.
- Frontend: React 18, TypeScript, Vite, Tailwind CSS, Lucide React, React Router v6, TanStack Query, Zustand, Axios.
- DevOps / Tools: Docker Compose (PostgreSQL 16, MinIO, Mailpit), xUnit, FluentAssertions, Vitest.

**Spec:** [`docs/superpowers/specs/2026-09-29-pipeline-design.md`](file:///c:/Users/nedim/Desktop/Pipeline/docs/superpowers/specs/2026-09-29-pipeline-design.md) & [`README_to_do.md`](file:///c:/Users/nedim/Desktop/Pipeline/README_to_do.md)

## Global Constraints

- Backend must target .NET 8 (`net8.0`) with nullable reference types enabled (`<Nullable>enable</Nullable>`).
- Database must be PostgreSQL with automatic migrations on startup when `AUTO_MIGRATE=true`.
- Every user-owned entity must implement `IUserOwnedEntity` and have `UserId` filtered automatically by EF Core global query filter `UserId == currentUserId && DeletedAt == null`.
- Passwords require minimum 10 characters as specified in section 6 of `README_to_do.md`.
- Refresh tokens must be rotating, stored hashed in database, and transmitted via httpOnly secure cookie.
- JWT access tokens have 15-minute expiration; refresh tokens have 30-day expiration.
- Frontend must support dark mode and light mode cleanly without visual glitching or unstyled flashes.
- Endpoints must follow REST conventions under `/api/` and return RFC 7807 problem details on error.

## Review Focus

1. **User Isolation Breach:** Querying user entities when `currentUserId` is different must return empty result or 404, never another user's records.
2. **Expired or Tampered JWT:** API endpoints must return 401 Unauthorized with standard problem details when bearer token is invalid or expired.
3. **Refresh Token Reuse:** Attempting to reuse an already-used or revoked refresh token must revoke the active token family and reject authentication.
4. **Missing Environment Variables:** Backend must gracefully report clear startup configuration errors instead of unhandled null reference exceptions if required JWT or ConnectionStrings are missing.
5. **Database Migration Readiness:** The application startup must cleanly apply EF Core migrations against PostgreSQL if `AUTO_MIGRATE=true`.

---

## Task 1: Repository Structure, Solution & Docker Compose Environment

**Files:**
- Create: `docker-compose.yml`
- Create: `.env.example`
- Create: `.env`
- Create: `backend/Pipeline.sln`
- Create: `backend/src/Pipeline.Domain/Pipeline.Domain.csproj`
- Create: `backend/src/Pipeline.Application/Pipeline.Application.csproj`
- Create: `backend/src/Pipeline.Infrastructure/Pipeline.Infrastructure.csproj`
- Create: `backend/src/Pipeline.Api/Pipeline.Api.csproj`
- Create: `backend/tests/Pipeline.Tests/Pipeline.Tests.csproj`

**Interfaces:**
- Produces: Compilable .NET 8 solution with clean project references (`Api` -> `Infrastructure` -> `Application` -> `Domain`).
- Produces: Docker Compose configuration running PostgreSQL 16, MinIO, and Mailpit.

- [ ] **Step 1: Create `.env.example` and `.env`**
Copy exact settings from `README_to_do.md` section 13: `DOMAIN`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `ConnectionStrings__Default`, `Jwt__Secret`, etc.

- [ ] **Step 2: Create `docker-compose.yml`**
Define services:
- `db`: `postgres:16-alpine`, ports `5432:5432`, volume `postgres_data`.
- `minio`: `minio/minio:latest`, ports `9000:9000`, `9001:9001`, volume `minio_data`.
- `mailpit`: `axllent/mailpit:latest`, ports `1025:1025`, `8025:8025`.

- [ ] **Step 3: Scaffold .NET Solution and Projects**
Run `dotnet new sln -n Pipeline -o backend` and create class libraries for `Domain`, `Application`, `Infrastructure`, webapi for `Api`, and xunit for `Tests`. Wire project references.

- [ ] **Step 4: Verify build of .NET solution**
Run: `dotnet build backend/Pipeline.sln`
Expected: 0 errors, 0 warnings.

- [ ] **Step 5: Commit**
`git add . && git commit -m "feat: setup solution structure, projects, and docker-compose"`

---

## Task 2: Core Domain Entities & EF Core DbContext with Global Query Filter

**Files:**
- Create: `backend/src/Pipeline.Domain/Common/BaseEntity.cs`
- Create: `backend/src/Pipeline.Domain/Common/IUserOwnedEntity.cs`
- Create: `backend/src/Pipeline.Domain/Entities/User.cs`
- Create: `backend/src/Pipeline.Domain/Entities/RefreshToken.cs`
- Create: `backend/src/Pipeline.Domain/Entities/Company.cs`
- Create: `backend/src/Pipeline.Domain/Entities/Application.cs`
- Create: `backend/src/Pipeline.Domain/Entities/Contact.cs`
- Create: `backend/src/Pipeline.Domain/Enums/ApplicationEnums.cs`
- Create: `backend/src/Pipeline.Application/Common/Interfaces/ICurrentUserService.cs`
- Create: `backend/src/Pipeline.Infrastructure/Persistence/PipelineDbContext.cs`
- Create: `backend/tests/Pipeline.Tests/Unit/UserIsolationTests.cs`

**Interfaces:**
- Consumes: `ICurrentUserService.UserId` to evaluate global query filters.
- Produces: `PipelineDbContext` with ASP.NET Identity tables and user-owned domain tables.

- [ ] **Step 1: Write failing user-isolation unit test**
Write a test in `Pipeline.Tests/Unit/UserIsolationTests.cs` that mocks `ICurrentUserService` with User A, adds records for User A and User B to `PipelineDbContext` (using EF Core In-Memory or SQLite), queries the DbSet, and asserts User B's records are excluded.

- [ ] **Step 2: Run test to verify it fails**
Run: `dotnet test backend/tests/Pipeline.Tests/Pipeline.Tests.csproj`
Expected: Compilation failure or FAIL.

- [ ] **Step 3: Implement domain models and `PipelineDbContext`**
Implement `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`), `IUserOwnedEntity` (`UserId`, `DeletedAt`), `User` (inheriting `IdentityUser<Guid>`), domain entities, and configure global query filter `builder.Entity<T>().HasQueryFilter(e => e.UserId == _currentUserService.UserId && e.DeletedAt == null)` in `PipelineDbContext.OnModelCreating`.

- [ ] **Step 4: Run test to verify it passes**
Run: `dotnet test backend/tests/Pipeline.Tests/Pipeline.Tests.csproj`
Expected: PASS with 100% isolation verified.

- [ ] **Step 5: Commit**
`git add backend/ && git commit -m "feat: implement domain entities and EF Core multi-tenant isolation"`

---

## Task 3: Authentication Service (Register, Login, Refresh, Password Hashing)

**Files:**
- Create: `backend/src/Pipeline.Application/Features/Auth/DTOs/AuthDTOs.cs`
- Create: `backend/src/Pipeline.Application/Features/Auth/Validators/RegisterRequestValidator.cs`
- Create: `backend/src/Pipeline.Application/Features/Auth/Services/IAuthService.cs`
- Create: `backend/src/Pipeline.Infrastructure/Services/AuthService.cs`
- Create: `backend/src/Pipeline.Infrastructure/Services/JwtTokenGenerator.cs`
- Create: `backend/src/Pipeline.Api/Controllers/AuthController.cs`
- Create: `backend/tests/Pipeline.Tests/Unit/AuthServiceTests.cs`

**Interfaces:**
- Consumes: `UserManager<User>`, `JwtOptions`.
- Produces: `AuthResponseDto(string AccessToken, string RefreshToken, UserProfileDto User)`.
- Produces: `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`.

- [ ] **Step 1: Write failing test for auth validation & token generation**
Write tests verifying:
- Password with < 10 characters fails validation.
- Valid registration creates user and returns JWT + Refresh Token.
- Refresh token rotation invalidates old refresh token.

- [ ] **Step 2: Run test to verify it fails**
Run: `dotnet test backend/tests/Pipeline.Tests/Pipeline.Tests.csproj`
Expected: FAIL.

- [ ] **Step 3: Implement Auth Service and Controller**
Implement FluentValidation validator, JWT generator with HS256, refresh token generator with cryptographically secure RNG and SHA-256 hash storage, and httpOnly cookie handling in `AuthController`.

- [ ] **Step 4: Run test to verify it passes**
Run: `dotnet test backend/tests/Pipeline.Tests/Pipeline.Tests.csproj`
Expected: PASS.

- [ ] **Step 5: Commit**
`git add backend/ && git commit -m "feat: implement JWT and rotating refresh token authentication"`

---

## Task 4: API Host, Health Check, Middlewares & Swagger

**Files:**
- Create: `backend/src/Pipeline.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Create: `backend/src/Pipeline.Api/Services/CurrentUserService.cs`
- Modify: `backend/src/Pipeline.Api/Program.cs`
- Create: `backend/src/Pipeline.Api/Controllers/HealthController.cs`
- Create: `backend/tests/Pipeline.Tests/Integration/HealthEndpointTests.cs`

**Interfaces:**
- Produces: `GET /health` returning `{ status: "Healthy", database: "Healthy" }`.
- Produces: RFC 7807 problem details middleware for all unhandled exceptions.
- Produces: Swagger/OpenAPI documentation at `/swagger`.

- [ ] **Step 1: Write failing integration test for `/health`**
Use `WebApplicationFactory<Program>` to make a GET request to `/health` and assert 200 OK with expected JSON structure.

- [ ] **Step 2: Run test to verify it fails**
Run: `dotnet test backend/tests/Pipeline.Tests/Pipeline.Tests.csproj`
Expected: FAIL.

- [ ] **Step 3: Implement `Program.cs`, middleware, and `HealthController`**
Configure Serilog, CORS, JWT Bearer authentication, ExceptionHandlingMiddleware, Swagger with Bearer authorization, and EF Core migration check on startup.

- [ ] **Step 4: Run test to verify it passes**
Run: `dotnet test backend/tests/Pipeline.Tests/Pipeline.Tests.csproj`
Expected: PASS.

- [ ] **Step 5: Commit**
`git add backend/ && git commit -m "feat: configure API host, health endpoint, and problem details middleware"`

---

## Task 5: Frontend Shell, Routing, Tailwind Theme & Layout

**Files:**
- Create: `frontend/package.json`
- Create: `frontend/vite.config.ts`
- Create: `frontend/tsconfig.json`
- Create: `frontend/tailwind.config.js`
- Create: `frontend/src/index.css`
- Create: `frontend/src/main.tsx`
- Create: `frontend/src/app/App.tsx`
- Create: `frontend/src/app/router.tsx`
- Create: `frontend/src/components/layout/AppLayout.tsx`
- Create: `frontend/src/components/layout/Sidebar.tsx`
- Create: `frontend/src/components/layout/Header.tsx`
- Create: `frontend/src/lib/api-client.ts`
- Create: `frontend/src/features/auth/AuthContext.tsx`
- Create: `frontend/src/features/auth/LoginPage.tsx`
- Create: `frontend/src/features/auth/RegisterPage.tsx`
- Create: `frontend/src/features/dashboard/DashboardPage.tsx`
- Create: `frontend/tests/App.test.tsx`

**Interfaces:**
- Consumes: REST endpoints `/api/auth/*` and `/api/health`.
- Produces: Responsive frontend layout with theme toggle (dark/light), sidebar navigation for all core sections, and route protection.

- [ ] **Step 1: Scaffold Vite + React 18 + TS and install dependencies**
Initialize frontend with Tailwind CSS, Lucide React, React Router, TanStack Query, Zustand, and Axios.

- [ ] **Step 2: Configure Theme and Design Tokens**
In `tailwind.config.js` and `index.css`, establish semantic palette: slate/zinc base, indigo accent (`#4F46E5`), emerald/amber/red status colors, and dark mode class strategy.

- [ ] **Step 3: Implement API Client with 401 Refresh Interceptor**
Setup Axios instance in `src/lib/api-client.ts` with request interceptor attaching Bearer token and response interceptor calling `/api/auth/refresh` on 401.

- [ ] **Step 4: Build Layout, Navigation, and Auth Pages**
Create `AppLayout` with collapsible sidebar, header with dark mode toggle and user menu, `LoginPage`, `RegisterPage`, and `DashboardPage` placeholder.

- [ ] **Step 5: Write and run frontend test**
Run: `npm test` (or `vitest run`) in `frontend/`
Expected: App renders with navigation and theme switcher without error.

- [ ] **Step 6: Commit**
`git add frontend/ && git commit -m "feat: scaffold frontend shell, router, auth pages, and layout"`

---

## Task 6: Milestone 1 Verification & End-to-End Check

- [ ] **Step 1: Run all backend tests**
Run: `dotnet test backend/Pipeline.sln`
Expected: All tests pass.

- [ ] **Step 2: Run frontend build and linter**
Run: `npm run build` in `frontend/`
Expected: Clean build with zero errors.

- [ ] **Step 3: Run backend and frontend locally, test registration and login**
Launch backend API and frontend, register a new account through the UI, verify tokens and dashboard redirection.

- [ ] **Step 4: Verify milestone completion evidence**
Document working verification outputs before proceeding to Milestone 2.
