# FreeBudget — Tracker

## Priority Tasks

- [ ] Add auto-clear ledger debt via rules
- [ ] NatWest import parser (nice to have)

## In Progress

(none)

## Completed

- [x] Scaffold .NET 10 solution structure (28 projects: 4 services x 4 layers + 3 shared libs + 9 test projects)
- [x] Set up MediatR and DI registration pattern
- [x] Set up Docker Compose with PostgreSQL
- [x] SharedKernel domain primitives (Entity, AggregateRoot, ValueObject, DomainEvent, Result)
- [x] Identity domain entities (User, Group, GroupMembership, BankAccount, BankAccountAccess)
- [x] Identity value objects (Email, BankType)
- [x] Identity domain events (UserCreated, GroupCreated, BankAccountLinked, BankAccountAccessGranted)
- [x] Identity application repository interfaces (IUserRepository, IGroupRepository, IBankAccountRepository)
- [x] Transactions domain entities (Transaction, ImportBatch)
- [x] Transactions value objects (Money, TransactionDirection)
- [x] Transactions domain events (TransactionImported, ImportBatchCompleted) + ImportStatus enum
- [x] Transactions application interfaces + DTOs (ITransactionRepository, IImportBatchRepository, ICsvTransactionParser, ImportLayout, RawBankTransaction)
- [x] EF Core persistence layer (Identity + Transactions: entity configs, repositories, migrations, auto-migrate)
- [x] Infrastructure test projects (25 repository tests with InMemory provider)
- [x] Default admin user seed on startup
- [x] CSV import: generic CsvTransactionParser with CsvHelper
- [x] CSV import: predefined Barclays + Wise layouts (matched to real CSV formats)
- [x] CSV import: ImportCsv MediatR command handler with dedup support
- [x] CSV import: POST /api/transactions/import endpoint
- [x] Python anonymise_csv.py tool for test data
- [x] Categorization rules: Category field on Transaction, CategorizationRule entity, rule matcher, CRUD API, auto-categorize during import
- [x] Reporting queries: category breakdown and period breakdown (day/week/month) with API endpoints
- [x] Ledger domain: LedgerEntry entity, balance computation, commands/queries, EF Core persistence, API endpoints
- [x] Move Money value object from Transactions.Domain to SharedKernel for cross-service use
- [x] Transaction split workflow: SplitTransactionCommand creates multiple ledger entries from one transaction with duplicate-split prevention
- [x] Import layout persistence: forward TargetAmount/TargetCurrency columns through Web API; persist currency→account routing on the layout (jsonb) so it survives reloads
- [x] Skip zero-amount CSV rows (active card checks)
- [x] Parent bank accounts: Wise-style parent shell with one currency child per code; metadata (nickname, import layout, group access) inherits from parent; currency routing on import inferred from siblings
- [x] Transaction sharing rules (Expense/Settlement/Exclude/Any) — auto-share matching transactions to a group's members
- [x] ImportLayout DB persistence (ImportLayoutDefinition entity + per-bank-account CRUD)
- [x] Authentication: local email+password with BCrypt, JWT bearer at Web.Api edge, pluggable ITokenIssuer for future OIDC, login/register UI

## Bugs

(none)

## Notes

- 387 total tests across 11 test projects
- Default admin: admin@freebudget.local / "Admin123!" (seeded on first startup in dev mode)
- Architecture: event queue for inter-service comms (deferred until cross-service flow needed)
- Auth: JWT validated only at the Web.Api edge today; downstream services trust Web.Api. Swap LocalJwtTokenIssuer + JwtBearer issuer/key for an OIDC provider later without restructuring.
- Future: overnight bank feed auto-pull for connected accounts
- Future: importer marketplace — users upload/share CSV layout definitions
- Future: external OIDC providers (Keycloak, Google) via IExternalAuthProvider
