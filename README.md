# FreeBudget

A multi-user accounting and budgeting application built with .NET 10 microservices.

## Quick Start

```bash
# Build & test
dotnet build
dotnet test

# Start the database (Postgres) for local dev
docker compose up -d postgres

# Run a service against local Postgres
dotnet run --project src/Services/Identity/FreeBudget.Identity.Api

# Or run the full stack in Docker
docker compose up -d
```

## Local development database

`docker compose up -d postgres` starts Postgres 17 on port `5432` with:

- User: `freebudget`
- Password: `freebudget_dev`
- Databases: `freebudget_identity`, `freebudget_transactions`, `freebudget_categorization`, `freebudget_ledger` (auto-created by `init-databases.sql`)

The connection strings live in each service's `appsettings.Development.json` under `ConnectionStrings:<ServiceName>Db` and already point at this setup. EF migrations run automatically on first startup in dev mode.

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
