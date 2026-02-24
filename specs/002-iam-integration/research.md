# Research: IAM Integration

**Feature**: Permission-Based Authorization Migration
**Branch**: `002-iam-integration`

## Decision Log

### 1. IAM Registration Pattern

**Decision**: Use `Maliev.Aspire.ServiceDefaults.IAM.IAMRegistrationService` base class.

**Rationale**:
- **Consistency**: This is the established pattern in the `Maliev.Aspire.ServiceDefaults` library.
- **Resilience**: The base class handles HTTP client creation, error logging, and standard resilience (though it fails open to prevent startup crash, our feature spec says *authorization checks* fail closed, which is consistent—if registration fails, we just don't have *new* perms, but the service runs. Wait, spec said "Fail-Closed (Deny All) if the underlying identity or permission service is unavailable". This usually refers to the runtime check (Policy evaluation). Registration failure just means IAM doesn't know about our roles yet).
- **Simplicity**: Provides abstract methods `GetPermissions()` and `GetPredefinedRoles()` which perfectly match our requirements.

**Alternatives Considered**:
- *Manual HttpClient*: Too much boilerplate.
- *BackgroundService*: The base class implements `IHostedService` already.

### 2. Permission Storage

**Decision**: Define permissions and roles in static code artifacts (`ReceiptPermissions.cs`, `ReceiptPredefinedRoles.cs`).

**Rationale**:
- **Version Control**: Permissions are code. They should be tracked in git.
- **Type Safety**: Using `const` strings prevents typos in `[Authorize]` attributes.

### 3. Authorization Enforcement

**Decision**: Use `RequirePermissionAttribute` from `Maliev.Aspire.ServiceDefaults`.

**Rationale**:
- **Platform Standard**: Matches the pattern used in EmployeeService and others (`[RequirePermission("service.resource.action")]`).
- **Feature Flag**: Supports `Features:PermissionBasedAuthEnabled` for safe rollout.
- **Enhanced Audit**: Native support for `IsCritical = true` on sensitive actions.

## Technical Details

### IAM Service Base URL
- Requires configuration `ExternalServices:IAM:BaseUrl` in `appsettings.json`.
- Uses `Maliev.Aspire.ServiceDefaults.IAMExtensions.AddIAMClient`.

### Feature Flag
- Implement `Features:PermissionBasedAuthEnabled` check in `IAMRegistrationService` and `RequirePermissionAttribute` logic.

### Permission Format
- Must follow `service.resource.action` pattern (e.g., `receipt.receipts.create`).
- Validated by `IAMRegistrationService`.
