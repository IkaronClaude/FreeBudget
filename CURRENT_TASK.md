# Current Task

## Status: in-progress

## Task

Add categorisation rules: Category field on Transaction, rule engine for auto-categorisation, CRUD endpoints.

## Branch

feature/categorization-rules

## Plan

- [ ] Commit 1: Add Category to Transaction entity + EF config
- [ ] Commit 2: Add CategoryColumn to ImportLayout + parser + Wise layout
- [ ] Commit 3: CategorizationRule entity + MatchType enum + tests
- [ ] Commit 4: ICategorizer + rule matcher implementation + tests
- [ ] Commit 5: Wire categorizer into import handler
- [ ] Commit 6: CRUD endpoints for rules + category query
- [ ] Commit 7: Migration + update tracker, merge to main

## Design Notes

- Category lives as a string on Transaction (simple, queryable)
- Rules live in Transactions service for now (avoid cross-service coupling)
- Categorization microservice stays empty for future features (ML, shared categories)
- Wise CSV Category column imported directly; Barclays uses rule matching

## Progress

- [x] Commit 1+2: Category field on Transaction + CategoryColumn in import pipeline
