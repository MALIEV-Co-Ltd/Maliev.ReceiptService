# ReceiptService Specification - Permission-Based Authorization Migration

## Permissions to Define

### Receipt Operations
```
receipt.receipts.create          - Create new receipts
receipt.receipts.read            - Read receipt details
receipt.receipts.update          - Update receipt information
receipt.receipts.void            - Void receipts (critical)
receipt.receipts.query           - Query receipt history
receipt.receipts.export          - Export receipt data
```

### Partial Payment Operations
```
receipt.partial-payments.create  - Create partial payment records
receipt.partial-payments.read    - Read partial payment details
receipt.partial-payments.manage  - Manage partial payments
```

### Audit Operations
```
receipt.audit.read               - Read receipt audit logs
receipt.audit.export             - Export audit data
```

## Predefined Roles

### receipt-admin
**Permissions**: All receipt.* permissions

### receipt-manager
**Permissions**: create, read, void, query, export, partial-payments.*, audit.read

### receipt-creator
**Permissions**: create, read, partial-payments.create, partial-payments.read

### receipt-viewer
**Permissions**: read, partial-payments.read

### receipt-auditor
**Permissions**: read, audit.read, audit.export, query, export

## Success Criteria
- [ ] ~11 permissions registered
- [ ] 5 predefined roles registered
- [ ] 1 critical permission (void)
