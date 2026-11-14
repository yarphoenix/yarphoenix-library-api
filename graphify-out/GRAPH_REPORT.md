# Graph Report - yarphoenix-library-api  (2026-11-14)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 948 nodes · 2211 edges · 34 communities (28 shown, 6 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 139 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9d878c8b`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Core Services & Endpoints
- AI Prompt Templates & Tests
- NuGet Package Manifest
- Docker Compose Config
- CI/CD Workflow
- Open Library Client
- EPUB Text Extraction
- Backend Docker Services
- Internet Archive Client
- Auth Contracts
- Project References & Packages
- Auth Migrations
- Auth Project Dependencies
- Catalog Project Dependencies
- EF Core Migrations Infra
- Endpoint Mapping
- Claude API Client
- Book Search Contracts
- Book Entity
- Host Package Dependencies
- Token & Public Domain Whitelist
- Public Domain Source
- UserBook Entity
- Reading Assistant Contracts
- Catalog DbContext & Achievements
- AI Module Registration
- Email & Dev Seeder
- DevUserSeeder Tests
- Refresh Token Entity
- Launch Settings
- Auth DbContext
- Claude Reading Assistant
- Reading Context Builder

## God Nodes (most connected - your core abstractions)
1. `Result` - 41 edges
2. `AuthService` - 38 edges
3. `Book` - 25 edges
4. `ApplicationUser` - 25 edges
5. `LibraryService` - 23 edges
6. `UserBook` - 23 edges
7. `CatalogDbContext` - 21 edges
8. `ReaderProfile` - 21 edges
9. `YarPhoenix.Catalog.Application` - 21 edges
10. `RefreshToken` - 19 edges

## Surprising Connections (you probably didn't know these)
- `AuthServiceRefreshTests` --references--> `AuthService`  [EXTRACTED]
  tests/YarPhoenix.Auth.Tests/AuthServiceRefreshTests.cs → src/YarPhoenix.Auth/Application/AuthService.cs
- `AuthServiceRefreshTests` --references--> `AuthDbContext`  [EXTRACTED]
  tests/YarPhoenix.Auth.Tests/AuthServiceRefreshTests.cs → src/YarPhoenix.Auth/Infrastructure/AuthDbContext.cs
- `DevUserSeederTests` --references--> `ApplicationUser`  [EXTRACTED]
  tests/YarPhoenix.Auth.Tests/DevUserSeederTests.cs → src/YarPhoenix.Auth/Domain/ApplicationUser.cs
- `CI/CD workflow (backend.yml)` --references--> `CI deploy job (GHCR + SSH)`  [EXTRACTED]
  CLAUDE.md → docs/DEPLOY.md
- `Modular monolith (README)` --semantically_similar_to--> `Modular Monolith`  [INFERRED] [semantically similar]
  README.md → CLAUDE.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Modular monolith modules** — claude_catalog, claude_auth, claude_storage, claude_ai, claude_shared [EXTRACTED 1.00]
- **CI to VPS release flow** — docs_deploy_cideploy, docs_deploy_ghcr, docs_deploy_secrets, docs_deploy_backendvps, docs_deploy_rollback [EXTRACTED 1.00]
- **External dependencies protected by Polly pipeline** — claude_resilience, claude_openlibrary, claude_claudeapi, claude_email, claude_minio [INFERRED 0.85]

## Communities (34 total, 6 thin omitted)

### Community 0 - "Core Services & Endpoints"
Cohesion: 0.06
Nodes (21): AuthService, IAuthEmailSender, ConfirmEmailRequest, RegisterResponse, UserResponse, AuthEndpoints, LoggingAuthEmailSender, ResendEmailSender (+13 more)

### Community 1 - "AI Prompt Templates & Tests"
Cohesion: 0.06
Nodes (21): YarPhoenix.Catalog.Infrastructure.InternetArchive, AssistantTemplates, TemplatePlan, CfiParser, IaFile, Format, Name, Private (+13 more)

### Community 2 - "NuGet Package Manifest"
Cohesion: 0.05
Nodes (57): YarPhoenix.Ai (IReadingAssistant), YarPhoenix.Api (host), YarPhoenix.Auth, Book entity, YarPhoenix.Catalog (domain core), YarPhoenix.Catalog.Tests (xUnit), CI/CD workflow (backend.yml), Claude API (+49 more)

### Community 3 - "Docker Compose Config"
Cohesion: 0.06
Nodes (23): AiModule, ReadingContextBuilder, EmailOptions, ApiKey, From, FrontendOptions, BaseUrl, AuthModule (+15 more)

### Community 4 - "CI/CD Workflow"
Cohesion: 0.06
Nodes (44): Microsoft.AspNetCore.Authentication.JwtBearer (10.0.9), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9), Microsoft.AspNetCore.OpenApi (10.0.15), Microsoft.EntityFrameworkCore.Design (10.0.9), Microsoft.EntityFrameworkCore.Sqlite (10.0.2), Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore (10.0.9), Minio (7.0.0), Microsoft.NET.Sdk.Web (+36 more)

### Community 5 - "Open Library Client"
Cohesion: 0.07
Nodes (22): YarPhoenix.Ai.Contracts, IReadingAssistant, AskAssistantRequest, AssistantResponse, AssistantEndpoints, ClaudeCacheControl, ClaudeClient, ClaudeContentBlock (+14 more)

### Community 6 - "EPUB Text Extraction"
Cohesion: 0.12
Nodes (8): YarPhoenix.Catalog.Migrations, YarPhoenix.Catalog.Infrastructure, YarPhoenix.Auth.Migrations, InitialAuth, AddUserAvatarKey, AuthDbContextModelSnapshot, InitialCatalog, CatalogDbContextModelSnapshot

### Community 7 - "Backend Docker Services"
Cohesion: 0.06
Nodes (39): depends_on, environment, image, ports, profiles, restart, volumes, API_DOMAIN (+31 more)

### Community 8 - "Internet Archive Client"
Cohesion: 0.08
Nodes (14): BookSearchQuery, PublicDomainCandidate, PublicDomainFile, BookDetailsResponse, BookListResponse, BookSummaryResponse, AddToLibraryRequest, LibraryStatsResponse (+6 more)

### Community 9 - "Auth Contracts"
Cohesion: 0.06
Nodes (38): runs-on, steps, environment, if, needs, runs-on, steps, DEPLOY_HOST (+30 more)

### Community 10 - "Project References & Packages"
Cohesion: 0.07
Nodes (26): OpenLibraryAuthor, Name, OpenLibraryDoc, AuthorNames, CoverId, EbookAccess, FirstPublishYear, Ia (+18 more)

### Community 11 - "Auth Migrations"
Cohesion: 0.11
Nodes (5): EpubTextExtractor, EpubMetadata, EpubMetadataParser, EpubMetadataParserTests, EpubTextExtractorTests

### Community 12 - "Auth Project Dependencies"
Cohesion: 0.07
Nodes (28): build, depends_on, environment, image, ports, restart, context, dockerfile (+20 more)

### Community 13 - "Catalog Project Dependencies"
Cohesion: 0.08
Nodes (12): YarPhoenix.Auth.Contracts, ChangeEmailRequest, ChangePasswordRequest, ConfirmEmailChangeRequest, DeleteAccountRequest, ForgotPasswordRequest, LoginRequest, LogoutRequest (+4 more)

### Community 14 - "EF Core Migrations Infra"
Cohesion: 0.13
Nodes (5): YarPhoenix.Ai.Application, YarPhoenix.Ai.Infrastructure.Epub, YarPhoenix.Catalog.Infrastructure.Epub, YarPhoenix.Catalog.Tests, PublicDomainWhitelist

### Community 15 - "Endpoint Mapping"
Cohesion: 0.21
Nodes (4): YarPhoenix.Auth.Application, YarPhoenix.Auth.Domain, YarPhoenix.Auth.Infrastructure, YarPhoenix.Auth.Tests

### Community 16 - "Claude API Client"
Cohesion: 0.19
Nodes (6): YarPhoenix.Catalog.Contracts, YarPhoenix.Auth.Endpoints, YarPhoenix.Ai.Endpoints, YarPhoenix.Catalog.Endpoints, YarPhoenix.Shared, ClaimsPrincipalExtensions

### Community 17 - "Book Search Contracts"
Cohesion: 0.13
Nodes (13): RefreshToken, CreatedAtUtc, ExpiresAtUtc, Id, IsActive, ReplacedByTokenHash, RevokedAtUtc, TokenHash (+5 more)

### Community 18 - "Book Entity"
Cohesion: 0.12
Nodes (17): Book, Author, CoverId, CreatedAtUtc, Description, ExternalId, FileId, FirstPublishYear (+9 more)

### Community 19 - "Host Package Dependencies"
Cohesion: 0.20
Nodes (3): YarPhoenix.Ai, YarPhoenix.Catalog, YarPhoenix.Auth

### Community 20 - "Token & Public Domain Whitelist"
Cohesion: 0.15
Nodes (3): YarPhoenix.Catalog.Application, YarPhoenix.Catalog.Infrastructure.OpenLibrary, YarPhoenix.Ai.Infrastructure

### Community 21 - "Public Domain Source"
Cohesion: 0.15
Nodes (9): UserAchievement, Threshold, UnlockedAtUtc, UserId, CatalogDbContext, Books, ReaderProfiles, UserAchievements (+1 more)

### Community 22 - "UserBook Entity"
Cohesion: 0.13
Nodes (15): BookStatus, Read, Reading, WantToRead, UserBook, AddedAtUtc, Book, BookId (+7 more)

### Community 23 - "Reading Assistant Contracts"
Cohesion: 0.24
Nodes (7): ReaderProfile, CurrentStreak, LastActiveDate, LongestStreak, TimeZoneId, UserId, StreakLogicTests

### Community 24 - "Catalog DbContext & Achievements"
Cohesion: 0.16
Nodes (8): TokenResponse, ApplicationUser, AvatarKey, CreatedAtUtc, FavoriteGenres, Nickname, UiLanguage, DevUserSeeder

### Community 27 - "DevUserSeeder Tests"
Cohesion: 0.18
Nodes (6): JwtOptions, AccessTokenMinutes, Audience, Issuer, RefreshTokenDays, Secret

### Community 28 - "Refresh Token Entity"
Cohesion: 0.20
Nodes (9): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, profiles, http (+1 more)

### Community 31 - "Claude Reading Assistant"
Cohesion: 0.40
Nodes (4): DevSeedOptions, Email, Nickname, Password

## Knowledge Gaps
- **153 isolated node(s):** `Error`, `IsFailure`, `IsSuccess`, `Format`, `Name` (+148 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 295 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `AuthService` connect `Core Services & Endpoints` to `Docker Compose Config`, `Endpoint Mapping`, `Book Search Contracts`, `Catalog DbContext & Achievements`, `AI Module Registration`, `Email & Dev Seeder`, `DevUserSeeder Tests`?**
  _High betweenness centrality (0.039) - this node is a cross-community bridge._
- **What connects `Error`, `IsFailure`, `IsSuccess` to the rest of the system?**
  _153 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Core Services & Endpoints` be split into smaller, more focused modules?**
  _Cohesion score 0.058153676064123826 - nodes in this community are weakly interconnected._
- **Why does `ApplicationUser` connect `Catalog DbContext & Achievements` to `Core Services & Endpoints`, `Docker Compose Config`, `EPUB Text Extraction`, `Endpoint Mapping`, `Book Search Contracts`, `AI Module Registration`, `Email & Dev Seeder`?**
  _High betweenness centrality (0.034) - this node is a cross-community bridge._
- **Should `AI Prompt Templates & Tests` be split into smaller, more focused modules?**
  _Cohesion score 0.057902973395931145 - nodes in this community are weakly interconnected._
- **Why does `Book` connect `Book Entity` to `Core Services & Endpoints`, `Public Domain Source`, `EPUB Text Extraction`, `UserBook Entity`?**
  _High betweenness centrality (0.027) - this node is a cross-community bridge._
- **Should `NuGet Package Manifest` be split into smaller, more focused modules?**
  _Cohesion score 0.05388471177944862 - nodes in this community are weakly interconnected._