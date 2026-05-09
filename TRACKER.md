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
- [ ] EF Core persistence layer (Identity + Transactions)
- [ ] API endpoints for import workflow

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

## Bugs

(none)

## Notes

- 135 total tests across 9 test projects (Identity domain: 66, Transactions domain: 55, SharedKernel: 8, smoke tests: 6)
