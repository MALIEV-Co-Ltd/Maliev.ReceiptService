# Data Model: IAM Integration

**Feature**: Permission-Based Authorization Migration
**Branch**: `002-iam-integration`

## Static Entities (Code)

### 1. Permissions (`ReceiptPermissions.cs`)

Hierarchy: `Service` -> `Resource` -> `Action`

| Permission ID | Description |
|---|---|
| `receipt.receipts.create` | Create new receipts |
| `receipt.receipts.read` | Read receipt details |
| `receipt.receipts.update` | Update receipt information |
| `receipt.receipts.void` | Void receipts (Critical) |
| `receipt.receipts.query` | Query receipt history |
| `receipt.receipts.export` | Export receipt data |
| `receipt.partial-payments.create` | Create partial payment records |
| `receipt.partial-payments.read` | Read partial payment details |
| `receipt.partial-payments.manage` | Update/Delete partial payments |
| `receipt.audit.read` | Read receipt audit logs |
| `receipt.audit.export` | Export audit data |

### 2. Predefined Roles (`ReceiptPredefinedRoles.cs`)

| Role Name | Permissions Included |
|---|---|
| `receipt-admin` | `*` (All permissions) |
| `receipt-manager` | All EXCEPT `receipt.audit.export` |
| `receipt-creator` | `receipts.create`, `receipts.read`, `partial-payments.create`, `partial-payments.read` |
| `receipt-viewer` | `receipts.read`, `partial-payments.read` |
| `receipt-auditor` | `receipts.read`, `receipts.query`, `receipts.export`, `audit.read`, `audit.export` |

## Database Changes
*None*. Permissions are managed by the external IAM service and enforced via JWT claims.
