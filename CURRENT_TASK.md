# Current Task

## Status: complete

## Task

Build CSV import feature: generic parser, predefined Barclays/Wise layouts, import command handler, API endpoint.

## Branch

feature/csv-import (merged to main)

## Progress

- [x] Commit 1: Add CsvHelper package reference
- [x] Commit 2: CsvTransactionParser tests + implementation (20 tests)
- [x] Commit 3: Predefined bank layouts (Barclays, Wise) + integration tests
- [x] Commit 4: Direction mappings for bank-specific normalization
- [x] Commit 5: ImportCsv command handler + application tests (6 tests)
- [x] Commit 6: Fix Wise layout to match real CSV format, add ExternalIdColumn
- [x] Commit 7: CSV upload API endpoint

## Next Steps

Pick next priority task from TRACKER.md (likely: rule engine for transaction categorization)
