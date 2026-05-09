# FreeBudget — Tracker

## Priority Tasks

- [ ] Define core domain entities for Transactions service (Transaction, Account, BankSource)
- [ ] Define core domain entities for Identity service (User, Group, GroupMembership, LinkedBankAccount)
- [ ] Build Barclays CSV import parser (top priority bank)
- [ ] Build Wise import parser (2nd priority bank)
- [ ] Implement rule engine for transaction categorization
- [ ] Define Ledger domain entities (Split, LedgerEntry, MoneyOwedPot)
- [ ] Implement transaction split workflow
- [ ] Add auto-clear ledger debt via rules
- [ ] Add reporting/graphing queries (income vs. expense by category, trends)
- [ ] NatWest import parser (nice to have)

## In Progress

(none)

## Completed

- [x] Scaffold .NET 10 solution structure (28 projects: 4 services x 4 layers + 3 shared libs + 9 test projects)
- [x] Set up MediatR and DI registration pattern
- [x] Set up Docker Compose with PostgreSQL
- [x] SharedKernel domain primitives (Entity, AggregateRoot, ValueObject, DomainEvent, Result)

## Bugs

(none)

## Notes

(none)
