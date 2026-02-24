# Maliev Receipt Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/ORGANIZATION/Maliev.ReceiptService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%2018-blue)](https://www.postgresql.org/)

Financial compliance and payment documentation service for the Maliev manufacturing ecosystem.

**Role in MALIEV Architecture**: The authoritative record-keeper for all payment transactions. It ensures financial integrity by managing the issuance, voiding, and auditing of receipts, integrating with the PDF service for professional document generation and the Accounting service for ledger synchronization.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Distributed Cache**: Redis 7.x (High-speed sequence management)
- **Messaging**: RabbitMQ via MassTransit
- **API Documentation**: OpenAPI 3.1 + Scalar UI
- **Observability**: OpenTelemetry (Metrics, Traces, Logging)

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
To maintain high performance and low complexity, the following are **NOT** used:
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Standard Data Annotations (`[Required]`, `[EmailAddress]`) only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.
- ❌ **In-memory Test DB**: All integration tests use **Testcontainers** with real PostgreSQL 18.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **XML Documentation**: Required on all public methods and properties.
- ✅ **No Secrets in Code**: All sensitive configuration injected via environment variables.
- ✅ **No Test Config in Program.cs**: Test configuration in test fixtures only.
- ✅ **IAM Integration**: Self-registers permissions with the IAM Service using GCP-style naming: `{service}.{resource}.{action}`.

---

## ✨ Key Features

- **Immutable Receipt Registry**: Guaranteed financial integrity with write-once records for all issued receipts and sequential numbering.
- **Complex Void Operations**: Robust compliance tracking for voided documents with mandatory justification and audit logging.
- **Sequence Management Engine**: Distributed Redis-backed generator for gapless, zero-collision receipt number generation.
- **Tax-Ready Auditing**: Dedicated endpoints for compliance review, void logging, and specialized tax authority reporting.
- **Multi-Currency Support**: Native handling of various currencies with precise exchange rate tracking at the moment of issuance.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for infrastructure)
- PostgreSQL 18 (Alpine)

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/ORGANIZATION/Maliev.ReceiptService.git
cd Maliev.ReceiptService
```

2. **Spin up Infrastructure**
```bash
docker run --name receipt-db -e POSTGRES_PASSWORD=YOUR_PASSWORD -p 5432:5432 -d postgres:18-alpine
docker run --name receipt-redis -p 6379:6379 -d redis:7-alpine
```

3. **Configure Environment**
```powershell
# Windows PowerShell
$env:ConnectionStrings__ReceiptDbContext="YOUR_POSTGRES_CONNECTION_STRING"
$env:ConnectionStrings__Cache="YOUR_REDIS_CONNECTION_STRING"
```

4. **Apply Migrations & Run**
```bash
dotnet ef database update --project Maliev.ReceiptService.Data
dotnet run --project Maliev.ReceiptService.Api
```

The service will be available at `http://localhost:5000/receipts`. Access the interactive documentation at `http://localhost:5000/receipts/scalar`.

---

## 📡 API Endpoints

All endpoints are prefixed with `/receipts/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/receipts` | Issue a formal payment receipt |
| POST | `/receipts/{id}/void` | Void an existing receipt with mandatory reasoning |
| GET | `/receipts/{id}/pdf` | Retrieve a professional rendered PDF document |
| GET | `/receipts/audit-trail/{id}` | Access the complete modification history for a record |

---

## 🏥 Health & Monitoring

Standardized health probes for Kubernetes orchestration:
- **Liveness**: `GET /receipts/liveness`
- **Readiness**: `GET /receipts/readiness` (Checks DB and Redis connectivity)
- **Metrics**: `GET /receipts/metrics` (Prometheus format)

---

## 🧪 Testing

We prioritize reliable tests over mock-heavy unit tests.

```bash
# Run all tests using Testcontainers
dotnet test --verbosity normal
```

- **Integration Tests**: Use real PostgreSQL 18 containers.
- **Contract Tests**: Ensure API stability for consumers.

---

## 📦 Deployment

Infrastructure management is handled via GitOps patterns.

- **Docker Image**: `REGION-docker.pkg.dev/PROJECT_ID/REPOSITORY/maliev-receipt-service:{sha}`
- **Environments**: Development, Staging, Production

---

## 📄 License

Proprietary - © 2025 MALIEV Co., Ltd. All rights reserved.
