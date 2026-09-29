# Milestone 5: Documents & Version Management Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build complete document and version management for Pipeline, including S3/MinIO compatible file storage abstraction with local disk fallback, document uploads with magic byte and MIME type validation, version tagging (`v1`, `v2`, `tailored-fintech`), default version designations, presigned temporary download URLs, in-app PDF previewing, per-version performance analytics (sent count, response rate, interview conversion rate, offer conversion rate), side-by-side version comparison, and application CV/cover letter linking in the Application Detail screen.

**Architecture:** .NET 8 Clean Architecture backend (`Pipeline.Application` + `Pipeline.Infrastructure` + `Pipeline.Api`) utilizing `IFileStorage` with `AWSSDK.S3` and `LocalFileStorage` fallback, EF Core 8 with multi-tenant isolation, coupled with a React 18 + TypeScript + Vite frontend styled with Tailwind CSS, Lucide icons, and TanStack Query.

**Tech Stack:** C# .NET 8, EF Core 8, SQLite / PostgreSQL, AWSSDK.S3, MinIO / S3, React 18, TypeScript, Tailwind CSS, TanStack Query, Vitest, Playwright.

**Spec:** `docs/superpowers/specs/2026-09-29-pipeline-design.md` and `README_to_do.md` Sections 5, 6, 7.4, 7.8, and 10.

---

## Global Constraints

- **Multi-tenant data isolation:** All queries and mutations must respect EF Core query filters (`UserId == currentUserId && DeletedAt == null`).
- **File Upload Security:** Max file size 10 MB. Allowed MIME types: PDF, DOCX, PNG, JPG. Content verified by header magic bytes (`%PDF`, `PK\x03\x04`, `\x89PNG`, `\xff\xd8\xff`). Keys generated with random GUIDs to prevent path traversal or enumeration.
- **Presigned URLs:** Downloads served via short-lived signed URLs (never public buckets) or secure authorized streaming endpoints.
- **RFC 7807 ProblemDetails** for all error responses.
- **Frontend Design:** Clean command-center aesthetic following `frontend-design` and `theme-factory` (slate/zinc borders, consistent typography, accessible contrast, responsive).
- **Verification Evidence:** Fresh `dotnet test`, `npm test`, `npm run build`, and browser Playwright verification tests required before completion (`verification-before-completion`).

---

## Review Focus

1. **Storage Provider Flexibility:** Support both MinIO/S3 via `AWSSDK.S3` when configured and `LocalFileStorage` fallback for offline / test environments so tests never fail due to missing cloud buckets.
2. **File Validation:** Reject files with mismatched extensions/magic bytes (e.g. executable disguised as PDF) with explicit 400 Bad Request error details.
3. **Per-Version Conversion Analytics:** Accurately compute conversion funnel metrics per document version from linked applications:
   - `SentCount`: total applications where `DocumentVersionCvId == versionId || DocumentVersionCoverId == versionId`.
   - `ReplyCount`: applications with status in `Screening`, `Interview`, `Assignment`, `Offer`, `Accepted` or with at least one interaction.
   - `InterviewCount`: applications with status in `Interview`, `Assignment`, `Offer`, `Accepted` or having at least one interview.
   - `OfferCount`: applications with status in `Offer`, `Accepted` or having offer compensation recorded.
   - Percentages: `ResponseRate` (`ReplyCount / SentCount`), `InterviewRate` (`InterviewCount / SentCount`), `OfferRate` (`OfferCount / SentCount`).
4. **Default Version Management:** When marking a document version as default (`IsDefault = true`), ensure previous default versions of that same document have `IsDefault = false`.
5. **Application Documents Tab:** Users can link existing CV and Cover Letter versions to an application or upload new ones directly from the application detail screen.

---

### Task 1: Storage Abstraction (`IFileStorage`), S3 & Local Implementations, and File Validator

**Files:**
- Create: `backend/src/Pipeline.Application/Common/Interfaces/IFileStorage.cs`
- Create: `backend/src/Pipeline.Infrastructure/Services/Storage/FileValidationService.cs`
- Create: `backend/src/Pipeline.Infrastructure/Services/Storage/LocalFileStorage.cs`
- Create: `backend/src/Pipeline.Infrastructure/Services/Storage/S3FileStorage.cs`
- Create: `backend/src/Pipeline.Infrastructure/Services/Storage/FileStorageFactory.cs`
- Modify: `backend/src/Pipeline.Infrastructure/Pipeline.Infrastructure.csproj`
- Create: `backend/tests/Pipeline.Tests/Unit/FileStorageTests.cs`

**Interfaces:**
- Consumes: AWS S3 SDK, file streams, content types
- Produces: `IFileStorage` interface with methods:
  - `Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)`
  - `Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(string fileKey, CancellationToken ct = default)`
  - `Task<string> GetPresignedDownloadUrlAsync(string fileKey, string fileName, TimeSpan expiry, CancellationToken ct = default)`
  - `Task DeleteAsync(string fileKey, CancellationToken ct = default)`

- [ ] **Step 1: Define `IFileStorage`** in `Pipeline.Application/Common/Interfaces/IFileStorage.cs`
- [ ] **Step 2: Implement `FileValidationService`** with magic byte checking for PDF (`%PDF`), DOCX (`PK\x03\x04`), PNG (`\x89PNG`), JPG (`\xff\xd8\xff`) and 10MB limit
- [ ] **Step 3: Implement `LocalFileStorage`** for test and local execution fallback
- [ ] **Step 4: Implement `S3FileStorage`** using `AmazonS3Client` supporting MinIO (`ForcePathStyle = true`, custom ServiceURL)
- [ ] **Step 5: Write unit tests in `FileStorageTests.cs`** verifying upload, download, deletion, and invalid file rejection
- [ ] **Step 6: Verify build with `dotnet test backend/Pipeline.sln`**

---

### Task 2: Document DTOs, Statistics Calculations, and IDocumentService Interface

**Files:**
- Create: `backend/src/Pipeline.Application/Features/Documents/DTOs/DocumentDTOs.cs`
- Create: `backend/src/Pipeline.Application/Features/Documents/Services/IDocumentService.cs`
- Modify: `backend/src/Pipeline.Application/Features/Applications/DTOs/ApplicationDTOs.cs` (add document version link inputs)

**Interfaces:**
- Consumes: `DocumentType` from `Pipeline.Domain.Enums`, `Document`, `DocumentVersion`
- Produces: `IDocumentService`, `DocumentDto`, `DocumentVersionDto`, `DocumentVersionStatsDto`, `DocumentStatsSummaryDto`, `CreateDocumentRequest`, `UpdateDocumentRequest`, `UploadDocumentVersionRequest`

- [ ] **Step 1: Define DTOs in `DocumentDTOs.cs`**:
  - `DocumentDto`: Id, Title, Type, Description, CreatedAt, UpdatedAt, Versions list
  - `DocumentVersionDto`: Id, DocumentId, VersionLabel, FileName, ContentType, SizeBytes, Notes, IsDefault, CreatedAt, Stats
  - `DocumentVersionStatsDto`: SentCount, ReplyCount, InterviewCount, OfferCount, ResponseRate, InterviewRate, OfferRate
  - `DocumentStatsSummaryDto`: TotalDocuments, TotalVersions, TopPerformingCvVersion, ComparisonList
  - `CreateDocumentRequest`, `UpdateDocumentRequest`, `UploadDocumentVersionRequest`
- [ ] **Step 2: Update `UpdateApplicationRequest` in `ApplicationDTOs.cs`** to accept optional `Guid? DocumentVersionCvId` and `Guid? DocumentVersionCoverId`
- [ ] **Step 3: Define `IDocumentService.cs`**
- [ ] **Step 4: Verify build with `dotnet build backend/Pipeline.sln`**

---

### Task 3: DocumentService Implementation and Unit Tests

**Files:**
- Create: `backend/src/Pipeline.Infrastructure/Services/DocumentService.cs`
- Modify: `backend/src/Pipeline.Infrastructure/Services/ApplicationService.cs` (handle document version links in updates)
- Create: `backend/tests/Pipeline.Tests/Unit/DocumentServiceTests.cs`
- Modify: `backend/src/Pipeline.Api/Program.cs` (register `IFileStorage` and `IDocumentService`)

**Interfaces:**
- Consumes: `IDocumentService`, `IFileStorage`, `PipelineDbContext`, `ICurrentUserService`
- Produces: Full document management, version upload, default version switching, download URL generation, and per-version funnel stats

- [ ] **Step 1: Write unit tests in `DocumentServiceTests.cs`** testing:
  - Document creation and listing by type
  - Uploading new version and setting as default unsets previous default
  - Calculating conversion statistics across linked applications (sent, replies, interviews, offers, conversion percentages)
  - Deleting version and document cascade
  - Getting presigned download URL
- [ ] **Step 2: Implement `DocumentService.cs`**
- [ ] **Step 3: Update `ApplicationService.cs`** to update `DocumentVersionCvId` and `DocumentVersionCoverId` when provided in `UpdateApplicationAsync`
- [ ] **Step 4: Register DI services in `Program.cs`**
- [ ] **Step 5: Run `dotnet test backend/Pipeline.sln`** and ensure all tests pass

---

### Task 4: Documents REST API Controllers & Integration Tests

**Files:**
- Create: `backend/src/Pipeline.Api/Controllers/DocumentsController.cs`
- Create: `backend/src/Pipeline.Api/Controllers/DocumentVersionsController.cs`
- Create: `backend/tests/Pipeline.Tests/Integration/DocumentsApiTests.cs`

**Interfaces:**
- Consumes: `IDocumentService`, `IFileStorage`
- Produces: REST endpoints:
  - `GET /api/documents` (supports `?type=CV`)
  - `GET /api/documents/{id}`
  - `POST /api/documents`
  - `PUT /api/documents/{id}`
  - `DELETE /api/documents/{id}`
  - `POST /api/documents/{id}/versions` (multipart/form-data)
  - `GET /api/document-versions/{id}/download` (returns download URL and stream)
  - `PUT /api/document-versions/{id}/default`
  - `DELETE /api/document-versions/{id}`
  - `GET /api/documents/stats`

- [ ] **Step 1: Write integration tests in `DocumentsApiTests.cs`** asserting:
  - Document creation and version upload with file content
  - Multi-tenant isolation: User A cannot see or download User B's documents
  - Setting default version updates document version flags
  - Statistics endpoint calculates response and interview rates correctly
- [ ] **Step 2: Implement `DocumentsController.cs` and `DocumentVersionsController.cs`**
- [ ] **Step 3: Run `dotnet test backend/Pipeline.sln`** and verify 100% pass

---

### Task 5: Frontend Document Types, API Client, and React Query Hooks

**Files:**
- Create: `frontend/src/features/documents/types.ts`
- Create: `frontend/src/features/documents/documents-api.ts`
- Create: `frontend/src/features/documents/useDocuments.ts`

**Interfaces:**
- Consumes: `/api/documents`, `/api/document-versions`
- Produces: TypeScript interfaces, TanStack Query hooks:
  - `useDocuments(type?)`
  - `useDocument(id)`
  - `useDocumentStats()`
  - `useCreateDocument()`
  - `useUpdateDocument()`
  - `useDeleteDocument()`
  - `useUploadVersion()`
  - `useSetDefaultVersion()`
  - `useDeleteVersion()`
  - `useDownloadVersion()`

- [ ] **Step 1: Define types in `types.ts`** matching backend DTOs
- [ ] **Step 2: Implement `documents-api.ts`** with `FormData` multipart upload, download URL fetcher, and stats API
- [ ] **Step 3: Implement `useDocuments.ts`** with TanStack Query hooks and cache invalidation
- [ ] **Step 4: Verify frontend builds with `npm run build`**

---

### Task 6: UI Components & Application Detail Integration

**Files:**
- Create: `frontend/src/features/documents/components/DocumentCard.tsx`
- Create: `frontend/src/features/documents/components/UploadDocumentModal.tsx`
- Create: `frontend/src/features/documents/components/UploadVersionModal.tsx`
- Create: `frontend/src/features/documents/components/PdfViewerModal.tsx`
- Create: `frontend/src/features/documents/components/DocumentVersionCompareModal.tsx`
- Create: `frontend/src/features/documents/DocumentsPage.tsx`
- Create: `frontend/src/features/applications/components/ApplicationDocumentsTab.tsx`
- Modify: `frontend/src/features/applications/ApplicationDetailPage.tsx` (add Documents tab)
- Modify: `frontend/src/app/router.tsx` (replace placeholder with `DocumentsPage`)

**Interfaces:**
- Consumes: `useDocuments`, `useApplications`
- Produces: Complete document command center UI:
  - Filter by document type tabs (`All`, `CV`, `Cover Letter`, `Portfolio`, `Certificate`, `Other`)
  - Document cards showing version histories, file sizes, default badges, and stats
  - Side-by-side version comparison modal with conversion bars
  - In-app PDF preview modal
  - Application detail Documents tab to link CV/Cover Letter and preview them

- [ ] **Step 1: Implement `PdfViewerModal.tsx`** providing in-app PDF previewing with fallback download
- [ ] **Step 2: Implement `DocumentVersionCompareModal.tsx`** for side-by-side stats comparison
- [ ] **Step 3: Implement `UploadDocumentModal.tsx` and `UploadVersionModal.tsx`** with drag-and-drop and validation
- [ ] **Step 4: Implement `DocumentCard.tsx`** with version accordion, default badge, stats chips, and action menus
- [ ] **Step 5: Implement `DocumentsPage.tsx`**
- [ ] **Step 6: Implement `ApplicationDocumentsTab.tsx`** and wire it into `ApplicationDetailPage.tsx`
- [ ] **Step 7: Update `router.tsx`**
- [ ] **Step 8: Verify build with `npm run build`**

---

### Task 7: Frontend Component Tests, Playwright Verification, and Final Evidence

**Files:**
- Create: `frontend/tests/DocumentsFeatures.test.tsx`
- Create: `scratch/verify_documents.py`

**Interfaces:**
- Consumes: Frontend components, Playwright, live API and Web servers
- Produces: Test results and screenshot verification artifacts

- [ ] **Step 1: Write Vitest tests in `DocumentsFeatures.test.tsx`** testing Document cards, version rendering, and stats calculation
- [ ] **Step 2: Run `npm test`** and ensure all tests pass
- [ ] **Step 3: Write and execute Playwright test `scratch/verify_documents.py`** to verify end-to-end:
  - Document creation & PDF upload
  - Version creation (`v1`, `v2`) and setting default
  - Version comparison modal showing response rates
  - PDF preview modal functioning
  - Linking CV version to an Application in `ApplicationDetailPage`
  - Verifying stats incremented for the linked CV version
- [ ] **Step 4: Save screenshot artifacts into brain directory**
- [ ] **Step 5: Run full test suites (`dotnet test`, `npm test`)**
