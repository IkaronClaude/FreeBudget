# FreeBudget

A multi-user accounting and budgeting application built with .NET 10 microservices.

## Quick Start

```bash
# Build
dotnet build

# Test
dotnet test

# Run with Docker
docker compose up -d
```

## Services

| Service | Port | Description |
|---------|------|-------------|
| Transactions | 5100 | Bank import, transaction storage, deduplication |
| Categorization | 5200 | Rules engine, categories, pattern matching |
| Ledger | 5300 | Transaction splitting, shared expense tracking |
| Identity | 5400 | Users, groups, bank account linking |
| PostgreSQL | 5432 | Shared database (separate DB per service) |

## Architecture

Each microservice follows clean architecture: Domain → Application → Infrastructure → API.

- **CQRS** via MediatR
- **Code-First EF Core** with PostgreSQL
- **Central Package Management** for NuGet version consistency
