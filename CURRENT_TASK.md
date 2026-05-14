# Current Task

## Status: in progress

## Task

Parent bank accounts — a Wise "shell" bank account can have currency sub-accounts (Wise GBP, Wise EUR, Wise USD) that inherit metadata (nickname, import layout, group access) from the parent.

## Branch

feature/parent-bank-accounts

## Plan (8 commits)

- [ ] 1. Domain: BankAccount.ParentBankAccountId + CurrencyCode + factories + tests
- [ ] 2. EF config + migration
- [ ] 3. BankAccountDto + GetUserBankAccounts/GroupBankAccounts plumbing
- [ ] 4. Identity API: create-parent + add-child endpoints; delete guards
- [ ] 5. Group access inheritance (child sees parent's grants)
- [ ] 6. ImportLayout: resolve metadata-owner (parent if exists, else self)
- [ ] 7. ImportCsv: infer currency routing from parent's children
- [ ] 8. UI: grouped AccountsView + multi-currency add flow; ImportBuilder uses inferred routing

## Progress

Branch created. Starting commit 1.
