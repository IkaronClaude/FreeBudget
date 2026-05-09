# FreeBudget — Tracker

## Priority Tasks

- [ ] Build Barclays CSV import parser (top priority bank)
- [ ] Build Wise import parser (2nd priority bank)
- [ ] Implement rule engine for transaction categorization
- [ ] Define Ledger domain entities (Split, LedgerEntry, MoneyOwedPot)
- [ ] Implement transaction split workflow
- [ ] Add auto-clear ledger debt via rules
- [ ] Add reporting/graphing queries (income vs. expense by category, trends)
- [ ] NatWest import parser (nice to have)
- [ ] API endpoints for import workflow
- [ ] Transaction sharing rules (auto-share with groups via user-defined rules)

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

## Bugs

(none)

## Notes

- 160 total tests across 11 test projects
- Default admin: admin@freebudget.local / "Admin" (seeded on first startup in dev mode)
- Architecture: event queue for inter-service comms (deferred until cross-service flow needed)
- Future: overnight bank feed auto-pull for connected accounts
