# Tasks: Permission-Based Authorization Migration

**Input**: Design documents from `/specs/002-iam-integration/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are included as per the constitution and project standards (Test-First Development).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- `Maliev.ReceiptService.Api/` (API project)
- `Maliev.ReceiptService.Tests/` (Test project)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create `IAM` namespace folder at `Maliev.ReceiptService.Api/Services/IAM/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T002 Define permissions in `Maliev.ReceiptService.Api/Services/IAM/ReceiptPermissions.cs`
- [x] T003 Define predefined roles in `Maliev.ReceiptService.Api/Services/IAM/ReceiptPredefinedRoles.cs`
- [x] T004 Implement `ReceiptIAMRegistrationService.cs` in `Maliev.ReceiptService.Api/Services/IAM/` inheriting from `IAMRegistrationService`
- [x] T005 Register `ReceiptIAMRegistrationService` as a hosted service in `Maliev.ReceiptService.Api/Program.cs`
- [x] T006 Configure IAM settings (BaseUrl, ServiceName, Timeout, RetryCount) in `Maliev.ReceiptService.Api/appsettings.json`
- [x] T007 Add `Features:PermissionBasedAuthEnabled` flag to `appsettings.json` (default: true)
- [x] T008 [P] Create `Maliev.ReceiptService.Tests/Integration/IAMRegistrationTests.cs` using `IAMTestHelpers`

**Checkpoint**: Foundation ready - IAM registration services are in place and permissions are defined.

---

## Phase 3: User Story 1 - Secure Receipt Operations (Priority: P1) 🎯 MVP

**Goal**: Protect receipt operations so only authorized roles can create or void receipts.

**Independent Test**: Verify `receipt-creator` can create, `receipt-viewer` cannot, and only `receipt-admin` can void.

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T009 [P] [US1] Create integration test for receipt creation authorization in `Maliev.ReceiptService.Tests/Integration/ReceiptCreationAuthTests.cs`
- [x] T010 [P] [US1] Create integration test for receipt voiding authorization in `Maliev.ReceiptService.Tests/Integration/ReceiptVoidAuthTests.cs`

### Implementation for User Story 1

- [x] T011 [US1] Apply `[RequirePermission(ReceiptPermissions.Receipts.Create)]` to `CreateReceipt` endpoint in `Maliev.ReceiptService.Api/Controllers/ReceiptsController.cs`
- [x] T012 [US1] Apply `[RequirePermission(ReceiptPermissions.Receipts.Void, IsCritical = true)]` to `VoidReceipt` endpoint in `Maliev.ReceiptService.Api/Controllers/ReceiptsController.cs`
- [x] T013 [US1] Apply `[RequirePermission(ReceiptPermissions.Receipts.Read)]` to `GetReceipt` endpoint in `Maliev.ReceiptService.Api/Controllers/ReceiptsController.cs`
- [x] T014 [US1] Verify Swagger documentation reflects authorization requirements (via `RequirePermission` filter)

**Checkpoint**: Receipt creation, voiding, and reading are now secured.

---

## Phase 4: User Story 2 - Partial Payment Management (Priority: P2)

**Goal**: Control who can record and manage partial payments.

**Independent Test**: Verify `receipt-manager` can manage payments, while `receipt-creator` cannot.

### Tests for User Story 2 ⚠️

- [x] T015 [P] [US2] Create integration test for partial payment authorization in `Maliev.ReceiptService.Tests/Integration/PartialPaymentAuthTests.cs`

### Implementation for User Story 2

- [x] T016 [US2] Apply `[RequirePermission(ReceiptPermissions.PartialPayments.Manage)]` (and others) to partial payment endpoints in `Maliev.ReceiptService.Api/Controllers/ReceiptsController.cs`

**Checkpoint**: Partial payment operations are secured.

---

## Phase 5: User Story 3 - Auditing and Compliance (Priority: P3)

**Goal**: Restrict audit log access and export to auditors.

**Independent Test**: Verify `receipt-auditor` can access audit logs, others cannot.

### Tests for User Story 3 ⚠️

- [x] T017 [P] [US3] Create integration test for audit endpoint authorization in `Maliev.ReceiptService.Tests/Integration/AuditAuthTests.cs`

### Implementation for User Story 3

- [x] T018 [US3] Apply `[RequirePermission(ReceiptPermissions.Audit.Read)]` to `GetAuditHistory` endpoint in `Maliev.ReceiptService.Api/Controllers/ReceiptsController.cs`
- [x] T019 [US3] Apply `[RequirePermission(ReceiptPermissions.Audit.Export)]` to export endpoints in `Maliev.ReceiptService.Api/Controllers/ReceiptsController.cs`

**Checkpoint**: Audit endpoints are secured.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T019 Verify all endpoints have appropriate authorization attributes
- [x] T020 Run full integration test suite to ensure no regressions
- [x] T021 [P] Update documentation/README.md with new IAM configuration details

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Phase 1. BLOCKS all user stories.
- **User Stories (Phase 3+)**: All depend on Foundational phase.
  - US1, US2, US3 can technically proceed in parallel after Phase 2, but P1 is MVP.
- **Polish (Phase 6)**: Depends on all user stories.

### Implementation Strategy

1. **Foundation First**: Complete T001-T007. This establishes the IAM registration and permission definitions.
2. **MVP (US1)**: Secure the core receipt operations (T008-T013). This delivers the most critical security value.
3. **Incremental**: Add US2 (Partial Payments) and US3 (Audit) subsequently.

### Parallel Opportunities

- Tests (T008, T009, T014, T016) can be written in parallel with each other.
- Controller updates for different user stories (T010-T012 vs T015 vs T017-T018) can be done in parallel once the foundation (permissions defined) is ready.