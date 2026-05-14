# Current Task

## Status: complete

## Task

Fix import-layout persistence — target amount/currency columns and currency→account routing weren't surviving a save.

## Branch

fix/import-layout-persistence (ready to merge to main)

## Plan

- [x] Forward TargetAmountColumn/TargetCurrencyColumn through Web API (`Dtos.cs`, `ImportLayoutsEndpoints.cs`); remove dead duplicate `ImportLayoutDto`
- [x] Persist `CurrencyAccountMappings` on `ImportLayoutDefinition` (jsonb) + EF migration
- [x] Wire new field through `ImportLayoutDto`, GET/PUT handlers, Transactions API record, Web API DTO + payload, UI type + saveLayout
- [x] UI hydrates `currencyToAccount` from saved layout and syncs changes back before save
- [x] Tests: 3 new UpsertImportLayoutHandler tests covering create/update/null-map

## Progress

3 commits on branch. 295 tests passing (was 292, +3 new). UI typechecks clean.
