# Current Task

## Status: in progress

## Task

Parent bank accounts — a Wise "shell" bank account can have currency sub-accounts (Wise GBP, Wise EUR, Wise USD) that inherit metadata (nickname, import layout, group access) from the parent.

## Branch

feature/parent-bank-accounts

## Plan (originally 8 commits, ended up 7 — 6 and 7 merged)

- [x] 1. Domain: BankAccount.ParentBankAccountId + CurrencyCode + factories + tests
- [x] 2. EF config + migration
- [x] 3. BankAccountDto + GetUserBankAccounts/GroupBankAccounts plumbing
- [x] 4. Identity API: create-parent + add-child endpoints; delete guards
- [x] 5. Group access inheritance (child sees parent's grants)
- [x] 6+7. Web API resolves metadata-owner for import layout CRUD and infers currency routing from parent's children
- [x] 8. UI: grouped AccountsView + multi-currency add flow; ImportBuilder defaults routing to matching currency child

## Progress

7 commits on branch, all tests green (308 total, +13 new). UI typechecks. Ready to merge.
