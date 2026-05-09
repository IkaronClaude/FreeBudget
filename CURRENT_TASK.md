# Current Task

## Status: complete

## Task

Add EF Core Code-First persistence layer for Identity and Transactions services.

## Branch

feature/ef-core-persistence (merged to main)

## Progress

- [x] Commit 1: DomainEvents ignore in BaseDbContext + InMemory test package
- [x] Commit 2: Identity entity configurations (User, Group, GroupMembership)
- [x] Commit 3: Identity entity configurations (BankAccount, BankAccountAccess)
- [x] Commit 4: Transactions entity configurations + Money domain tweak
- [x] Commit 5: Identity repository implementations
- [x] Commit 6: Transactions repository implementations
- [x] Commit 7: Infrastructure test projects + repository tests
- [x] Commit 8: Migrations + dev connection strings + auto-migrate on startup
- [x] Commit 9: Seed default admin user + update TRACKER.md/CURRENT_TASK.md

## Next Steps

Pick next priority task from TRACKER.md (likely: Barclays CSV import parser or API endpoints)
