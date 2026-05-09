# FreeBudget — Project Plan

## Overview

FreeBudget is a multi-user accounting and budgeting application built with .NET 10 microservices. It imports bank transaction history, reconciles transactions, supports user-defined rules for categorization and pattern detection, and provides visual insights into income and expenses by category. It also provides a ledger system for splitting transactions and tracking shared expenses between users.

## Feature Pillars

### Feature 1: Grouping & Graphing

Categorize transactions via rules, visualize income/outgoing by category and over time.

- Rule engine evaluates user-defined rules against transactions (rules are domain entities, not hardcoded)
- Bank import pipeline: Import → Parse → Normalize → Deduplicate → Persist (each stage discrete and testable)
- Reporting queries for income vs. expense by category, trends over time, filterable by account/group

### Feature 2: Ledger

Split transactions between users and track shared expenses.

- **Split**: A transaction (e.g., JustEat on Barclays) can be split — user enters how much each person owes
- **Money Owed Pots**: Splits write into per-user "money owed" records in the database
- **Auto-clear via rules**: Rules can automatically settle ledger debt. Example: a Wise transfer where source=Wise, name matches "Anna%", target=Barclays, name matches "Anna%", title="ledger" → auto-assigned to pay off ledger debt

## Supported Banks

| Bank | Priority | Format |
|------|----------|--------|
| Barclays | Top | TBD |
| Wise | High | TBD |
| NatWest | Nice-to-have | TBD |

## Multi-User & Groups

- Users can create groups and link their bank accounts/feeds to groups
- Rules can be scoped per bank account per group, with a default group fallback
- In the future, every user can have rules per bank account on which group a transaction relates to

## Authentication

- Architecture is auth-ready: design interfaces and middleware slots so authentication can be added without restructuring
- Not yet implemented — future targets include Google login, Facebook login, etc.

## Deployment

- Docker — each microservice is its own container
- Docker Compose for local development
- Cloud-ready architecture but no cloud deployment yet
