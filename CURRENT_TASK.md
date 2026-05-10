# Current Task

## Status: complete

## Task

Transaction split workflow — split one transaction into multiple ledger entries.

## Branch

feature/transaction-split (merged to main)

## Plan

- [x] Add AddRangeAsync and GetByTransactionIdAsync to ledger repository
- [x] Add SplitTransactionCommand + handler + tests
- [x] Add POST /api/ledger/splits endpoint
- [x] Update tracker

## Progress

All 3 commits complete. 292 tests passing. Ready for merge to main.
