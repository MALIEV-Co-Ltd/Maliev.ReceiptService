# Quickstart: IAM Integration

## Prerequisites
- **IAM Service**: Ensure `ExternalServices:IAM:BaseUrl` is configured in `appsettings.json` (or `appsettings.Development.json`).
- **Identity Provider**: A running Identity Provider (e.g., Keycloak, IdentityServer) that issues JWTs with `permissions` claim.

## Configuration
Add the following to `appsettings.json`:

```json
{
  "ExternalServices": {
    "IAM": {
      "BaseUrl": "http://localhost:5000" // Replace with actual IAM service URL
    }
  }
}
```

## Running the Service
1. Start the service: `dotnet run --project Maliev.ReceiptService.Api`
2. Observe logs for "Starting IAM registration...".
3. Verify successful registration: "Successfully registered permissions and roles...".

## Testing Authorization
1. Obtain a JWT token with the desired role/permissions.
2. Call an endpoint, e.g., `POST /api/v1/receipts`.
   - With `receipt-creator` role: Should succeed (201 Created).
   - With `receipt-viewer` role: Should fail (403 Forbidden).

## Troubleshooting
- **401 Unauthorized**: Token is missing, invalid, or expired. Check `Authorization: Bearer <token>` header.
- **403 Forbidden**: Token is valid but lacks the required permission. Check token claims.
- **Registration Failed**: Check `IAMService` connectivity and logs. The service will still start (fail-open registration), but new permissions won't be available in IAM.
