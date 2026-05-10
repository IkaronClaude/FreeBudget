# FreeBudget

A multi-user accounting and budgeting application built with .NET 10 microservices and a Vue 3 web UI.

## Prerequisites

| Tool | Version | Used by |
|------|---------|---------|
| .NET SDK | 10 (preview) | All backend services + BFF |
| Docker / Docker Desktop | any current | Postgres, pgAdmin, full-stack run |
| Node.js | 20+ (LTS recommended) | Vue dev server + build |

Install Node on Windows: `winget install OpenJS.NodeJS.LTS`

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

## pgAdmin

`docker compose up -d pgadmin` (or just `docker compose up -d`) starts pgAdmin on `http://localhost:5050`.

- Login: `admin@freebudget.local` / `admin`
- The "FreeBudget (local)" server is pre-configured. Password when prompted: `freebudget_dev`

## Services

| Service | Port | Description |
|---------|------|-------------|
| Transactions | 5100 | Bank import, transaction storage, deduplication |
| Categorization | 5200 | Rules engine, categories, pattern matching |
| Ledger | 5300 | Transaction splitting, shared expense tracking |
| Identity | 5400 | Users, groups, bank account linking |
| Web BFF | 5500 | API gateway for the Vue UI (proxies the four backend services) |
| Vue UI (dev) | 5173 | Vite dev server (proxies `/api` to BFF on 5500) |
| PostgreSQL | 5432 | Shared database (separate DB per service) |
| pgAdmin | 5050 | Database browser |

## Web UI

```bash
# In one terminal: backend services + BFF
docker compose up -d postgres
dotnet run --project src/Services/Identity/FreeBudget.Identity.Api
dotnet run --project src/Services/Transactions/FreeBudget.Transactions.Api
dotnet run --project src/Services/Ledger/FreeBudget.Ledger.Api
dotnet run --project src/Web/FreeBudget.Web.Api

# In another terminal: Vue dev server
cd src/Web/FreeBudget.Web.Ui
npm install
npm run dev
```

Open `http://localhost:5173`. The seeded admin user is the implicit current user — no login.

> On Windows PowerShell you may need to run npm via `npm.cmd` (or relax execution policy: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`).

## Architecture

Each microservice follows clean architecture: Domain → Application → Infrastructure → API.

- **CQRS** via MediatR
- **Code-First EF Core** with PostgreSQL
- **Central Package Management** for NuGet version consistency
