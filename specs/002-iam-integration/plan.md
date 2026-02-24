# Implementation Plan: Permission-Based Authorization Migration

**Branch**: `002-iam-integration` | **Date**: 2025-12-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/002-iam-integration/spec.md`

## Summary

Migrate the ReceiptService to a permission-based authorization model by integrating with the centralized IAM service. This involves defining granular permissions and predefined roles in code, implementing a startup registration service to sync these definitions with IAM, and securing all API endpoints (receipts, partial payments, audit) with the appropriate permissions.

## Technical Context

**Language/Version**: C# / .NET 10.0
**Primary Dependencies**: ASP.NET Core, MassTransit, StackExchange.Redis, `Maliev.Aspire.ServiceDefaults`
**Storage**: PostgreSQL (Existing)
**Testing**: xUnit, Testcontainers
**Target Platform**: Linux (Docker)
**Project Type**: Microservice (API)
**Performance Goals**: Standard API responsiveness; minimal overhead from auth checks.
**Constraints**: Fail-closed authorization via `RequirePermissionAttribute`; `Features:PermissionBasedAuthEnabled` flag for safe rollout.
**Scale/Scope**: ~11 permissions, 5 roles.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Service Autonomy**: IAM integration uses a clean client interface; no direct DB access to IAM.
- [x] **Explicit Contracts**: Permissions are defined in code and registered explicitly via API.
- [x] **Test-First Development**: Integration tests will verify permission enforcement.
- [x] **Real Infrastructure Testing**: Tests will use real service instances (mocking IAM client only where necessary, or using a fake IAM stub).
- [x] **Security**: JWT usage with granular claims.
- [x] **Project Structure**: Follows standard layout.

## Project Structure

### Documentation (this feature)

```text
specs/002-iam-integration/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (permissions.json)
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
Maliev.ReceiptService.Api/
├── Services/
│   ├── IAM/
│   │   ├── ReceiptIAMRegistrationService.cs
│   │   ├── ReceiptPermissions.cs
│   │   └── ReceiptPredefinedRoles.cs
├── Controllers/
│   ├── ReceiptsController.cs (Update)
│   ├── AnalyticsController.cs (Update)
│   └── ...
```

**Structure Decision**: Add IAM-related services to `Services/IAM` namespace/folder to keep them organized.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A       |            |                                     |