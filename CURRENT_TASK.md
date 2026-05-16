# Current Task

## Status: complete (ready to merge)

## Task

Add user accounts: registration, login, logout. Provider-agnostic — local
password store with a pluggable JWT issuer so an OIDC provider (Keycloak,
Google, etc.) can be slotted in later without restructuring.

## Branch

feature/authentication

## Plan (5 commits)

- [x] 1. Domain: UserCredential entity + IPasswordHasher contract + tests
- [x] 2. Infrastructure: BCryptPasswordHasher, EF config, migration, repository + tests
- [x] 3. Identity.Application: RegisterUser/VerifyCredentials commands; Identity.Api auth endpoints; admin seed gains password "Admin123!"
- [x] 4. Web.Api: JwtBearer middleware, ITokenIssuer + LocalJwtTokenIssuer, /api/auth/login + /register, ClaimsCurrentUserResolver, fallback auth policy
- [x] 5. Web UI: Pinia auth store with localStorage token, axios interceptor, router guard, LoginView + RegisterView, sign-out button

## Progress

5 commits on branch, 387 tests passing (was 352). Vue typechecks. End-to-end
verified against running stack: /api/me 401 without token; admin login →
JWT → /api/me 200; register new user → JWT → /api/me 200 with empty
groups/accounts (scoped per-user); duplicate email and short password
each return 422 with the expected error.

## Out of scope (not yet)

- Inter-service JWT validation: Transactions/Ledger/Categorization still
  trust Web.Api and accept userId via params. JWT validation lives only
  at the Web.Api edge for now.
- External OIDC providers (Keycloak/Google). Architecture is ready — drop
  in an IExternalAuthProvider abstraction and an alternate ITokenIssuer
  when needed.
- Password reset / email verification.
