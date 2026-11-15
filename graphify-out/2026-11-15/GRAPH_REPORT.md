# Graph Report - yarphoenix-library-api  (2026-11-15)

## Corpus Check
- 125 files · ~30,490 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: (none) 6, .example 1, .props 1)

## Summary
- 1223 nodes · 2916 edges · 59 communities (43 shown, 16 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 141 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9194e8eb`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- AuthService
- AssistantTemplates.TryBuildPlan
- NuGet Package Manifest
- AuthModule.AddAuthModule
- CI/CD Workflow
- Open Library Client
- EPUB Text Extraction
- caddy
- LibraryContracts.cs
- Auth Contracts
- Project References & Packages
- GutenbergCatalog
- Auth Project Dependencies
- Catalog Project Dependencies
- EpubTextExtractor.cs
- Endpoint Mapping
- Claude API Client
- Book Search Contracts
- Book Entity
- Host Package Dependencies
- YarPhoenix.Catalog.Application
- CatalogDbContext
- UserBook Entity
- Fact
- ApplicationUser
- AI Module Registration
- Email & Dev Seeder
- DevUserSeeder Tests
- Refresh Token Entity
- Task
- microsoft_entityframeworkcore
- CollectionOptions
- Reading Context Builder
- AssistantEndpoints.MapAssistantEndpoints
- Guid
- ReadingContextBuilder.BuildAsync
- BookFileLocks
- AuthService.ChangeEmailAsync
- YarPhoenix.Catalog.Infrastructure.OpenLibrary
- BookSearchQuery
- CatalogService
- CatalogModule.AddCatalogModule
- backend
- OpenLibraryProvider
- AuthDbContext
- ReadableCollectionImporter
- AuthEndpoints
- IReadOnlyList
- minio
- postgres
- MinioOptions
- services
- EpubProcessingWorker.CopyCatalogMetadata
- OpenLibraryProviderTests
- BookProcessingQueue
- healthcheck
- CatalogService.SearchWords
- CatalogDbContextFactory.cs

## God Nodes (most connected - your core abstractions)
1. `Book` - 40 edges
2. `AuthService` - 38 edges
3. `Result` - 31 edges
4. `YarPhoenix.Catalog.Application` - 29 edges
5. `CatalogDbContext` - 29 edges
6. `LibraryService` - 27 edges
7. `ApplicationUser` - 25 edges
8. `GutenbergCatalog` - 23 edges
9. `RefreshToken` - 19 edges
10. `YarPhoenix.Catalog.Domain` - 18 edges

## Surprising Connections (you probably didn't know these)
- `CollectionEntryFactoryTests` --references--> `GutenbergCatalog`  [EXTRACTED]
  tests/YarPhoenix.Catalog.Tests/CollectionEntryFactoryTests.cs → src/YarPhoenix.Catalog/Infrastructure/Collection/GutenbergCatalog.cs
- `AuthServiceRefreshTests` --references--> `AuthService`  [EXTRACTED]
  tests/YarPhoenix.Auth.Tests/AuthServiceRefreshTests.cs → src/YarPhoenix.Auth/Application/AuthService.cs
- `AuthServiceRefreshTests` --references--> `AuthDbContext`  [EXTRACTED]
  tests/YarPhoenix.Auth.Tests/AuthServiceRefreshTests.cs → src/YarPhoenix.Auth/Infrastructure/AuthDbContext.cs
- `DevUserSeederTests` --references--> `ApplicationUser`  [EXTRACTED]
  tests/YarPhoenix.Auth.Tests/DevUserSeederTests.cs → src/YarPhoenix.Auth/Domain/ApplicationUser.cs
- `CI/CD workflow (backend.yml)` --references--> `CI deploy job (GHCR + SSH)`  [EXTRACTED]
  CLAUDE.md → docs/DEPLOY.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Modular monolith modules** — claude_catalog, claude_auth, claude_storage, claude_ai, claude_shared [EXTRACTED 1.00]
- **CI to VPS release flow** — docs_deploy_cideploy, docs_deploy_ghcr, docs_deploy_secrets, docs_deploy_backendvps, docs_deploy_rollback [EXTRACTED 1.00]
- **External dependencies protected by Polly pipeline** — claude_resilience, claude_openlibrary, claude_claudeapi, claude_email, claude_minio [INFERRED 0.85]

## Communities (59 total, 16 thin omitted)

### Community 0 - "AuthService"
Cohesion: 0.18
Nodes (8): AuthService, TokenResponse, UserResponse, Error, Result, Error, IsFailure, IsSuccess

### Community 1 - "AssistantTemplates.TryBuildPlan"
Cohesion: 0.19
Nodes (3): AssistantTemplates, TemplatePlan, AssistantTemplatesTests

### Community 2 - "NuGet Package Manifest"
Cohesion: 0.05
Nodes (57): YarPhoenix.Ai (IReadingAssistant), YarPhoenix.Api (host), YarPhoenix.Auth, Book entity, YarPhoenix.Catalog (domain core), YarPhoenix.Catalog.Tests (xUnit), CI/CD workflow (backend.yml), Claude API (+49 more)

### Community 3 - "AuthModule.AddAuthModule"
Cohesion: 0.11
Nodes (8): AiModule, EmailOptions, ApiKey, From, FrontendOptions, BaseUrl, AuthModule, StorageModule

### Community 4 - "CI/CD Workflow"
Cohesion: 0.06
Nodes (44): Microsoft.AspNetCore.Authentication.JwtBearer (10.0.9), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9), Microsoft.AspNetCore.OpenApi (10.0.15), Microsoft.EntityFrameworkCore.Design (10.0.9), Microsoft.EntityFrameworkCore.Sqlite (10.0.2), Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore (10.0.9), Minio (7.0.0), Microsoft.NET.Sdk.Web (+36 more)

### Community 5 - "Open Library Client"
Cohesion: 0.06
Nodes (23): YarPhoenix.Ai.Infrastructure, YarPhoenix.Ai.Contracts, IReadingAssistant, AskAssistantRequest, AssistantResponse, ClaudeCacheControl, ClaudeClient, ClaudeContentBlock (+15 more)

### Community 6 - "EPUB Text Extraction"
Cohesion: 0.09
Nodes (12): YarPhoenix.Catalog.Migrations, YarPhoenix.Auth.Migrations, InitialAuth, AddUserAvatarKey, AuthDbContextModelSnapshot, UserAchievement, Threshold, UnlockedAtUtc (+4 more)

### Community 7 - "caddy"
Cohesion: 0.22
Nodes (9): depends_on, environment, image, ports, profiles, restart, volumes, API_DOMAIN (+1 more)

### Community 8 - "LibraryContracts.cs"
Cohesion: 0.15
Nodes (6): AddToLibraryRequest, LibraryBookResponse, LibraryStatsResponse, ProgressRequest, StartReadingRequest, UpdateShelfRequest

### Community 9 - "Auth Contracts"
Cohesion: 0.06
Nodes (38): runs-on, steps, environment, if, needs, runs-on, steps, DEPLOY_HOST (+30 more)

### Community 10 - "Project References & Packages"
Cohesion: 0.08
Nodes (26): OpenLibraryAuthor, Name, OpenLibraryDoc, AuthorNames, CoverId, EditionCount, FirstPublishYear, GutenbergIds (+18 more)

### Community 11 - "GutenbergCatalog"
Cohesion: 0.05
Nodes (14): EpubTextExtractor, CollectionEntryFactory, Columns, Last, GutenbergCatalog, Count, GutenbergEdition, GutenbergPerson (+6 more)

### Community 12 - "Auth Project Dependencies"
Cohesion: 0.12
Nodes (16): environment, Anthropic__ApiKey, ASPNETCORE_ENVIRONMENT, ConnectionStrings__Postgres, Cors__AllowedOrigins__0, Email__ApiKey, Email__From, Frontend__BaseUrl (+8 more)

### Community 13 - "Catalog Project Dependencies"
Cohesion: 0.07
Nodes (13): ChangeEmailRequest, ChangePasswordRequest, ConfirmEmailChangeRequest, ConfirmEmailRequest, DeleteAccountRequest, ForgotPasswordRequest, LoginRequest, LogoutRequest (+5 more)

### Community 15 - "Endpoint Mapping"
Cohesion: 0.20
Nodes (4): YarPhoenix.Auth.Application, YarPhoenix.Auth.Domain, YarPhoenix.Auth.Infrastructure, YarPhoenix.Auth.Tests

### Community 16 - "Claude API Client"
Cohesion: 0.14
Nodes (6): YarPhoenix.Catalog.Contracts, YarPhoenix.Auth.Endpoints, YarPhoenix.Auth.Contracts, YarPhoenix.Ai.Endpoints, YarPhoenix.Catalog.Endpoints, YarPhoenix.Shared

### Community 17 - "Book Search Contracts"
Cohesion: 0.17
Nodes (11): RefreshToken, CreatedAtUtc, ExpiresAtUtc, Id, IsActive, ReplacedByTokenHash, RevokedAtUtc, TokenHash (+3 more)

### Community 18 - "Book Entity"
Cohesion: 0.08
Nodes (24): Book, Author, CoverId, CreatedAtUtc, Description, ExternalId, FallbackGutenbergId, FileId (+16 more)

### Community 20 - "YarPhoenix.Catalog.Application"
Cohesion: 0.22
Nodes (4): YarPhoenix.Catalog.Infrastructure.Collection, YarPhoenix.Catalog.Application, YarPhoenix.Catalog.Domain, YarPhoenix.Catalog.Tests

### Community 21 - "CatalogDbContext"
Cohesion: 0.17
Nodes (10): CollectionImportState, Id, LastCompletedAtUtc, NextPage, CatalogDbContext, Books, CollectionImportStates, ReaderProfiles (+2 more)

### Community 22 - "UserBook Entity"
Cohesion: 0.14
Nodes (14): BookStatus, Read, Reading, WantToRead, UserBook, AddedAtUtc, Book, BookId (+6 more)

### Community 23 - "Fact"
Cohesion: 0.06
Nodes (15): PublicDomainRule, PublicDomainWhitelist, StreakLogic, ReaderProfile, CurrentStreak, LastActiveDate, LongestStreak, TimeZoneId (+7 more)

### Community 24 - "ApplicationUser"
Cohesion: 0.25
Nodes (6): ApplicationUser, AvatarKey, CreatedAtUtc, FavoriteGenres, Nickname, UiLanguage

### Community 26 - "Email & Dev Seeder"
Cohesion: 0.15
Nodes (6): DevSeedOptions, Email, Nickname, Password, DevUserSeeder, DevUserSeederTests

### Community 27 - "DevUserSeeder Tests"
Cohesion: 0.29
Nodes (6): JwtOptions, AccessTokenMinutes, Audience, Issuer, RefreshTokenDays, Secret

### Community 28 - "Refresh Token Entity"
Cohesion: 0.20
Nodes (9): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, profiles, http (+1 more)

### Community 29 - "Task"
Cohesion: 0.13
Nodes (5): IPublicDomainSource, UserDataRemover, IUserDataRemover, IFileStorage, MinioFileStorage

### Community 30 - "microsoft_entityframeworkcore"
Cohesion: 0.15
Nodes (3): YarPhoenix.Ai, YarPhoenix.Catalog.Infrastructure, YarPhoenix.Storage

### Community 31 - "CollectionOptions"
Cohesion: 0.07
Nodes (18): ReadableSource, Gutenberg, None, StandardEbooks, Upload, CollectionOptions, GutenbergBaseUrl, ImportEnabled (+10 more)

### Community 33 - "AssistantEndpoints.MapAssistantEndpoints"
Cohesion: 0.09
Nodes (5): YarPhoenix.Api, AssistantEndpoints, RequiredFieldsFilter, RequiredFields, RequestValidationTests

### Community 35 - "ReadingContextBuilder.BuildAsync"
Cohesion: 0.13
Nodes (4): YarPhoenix.Ai.Application, CfiParser, ReadingContextBuilder, CfiParserTests

### Community 36 - "BookFileLocks"
Cohesion: 0.17
Nodes (6): BookFileLocks, Entry, Semaphore, Users, Releaser, BookFileLocksTests

### Community 37 - "AuthService.ChangeEmailAsync"
Cohesion: 0.18
Nodes (3): IAuthEmailSender, LoggingAuthEmailSender, ResendEmailSender

### Community 40 - "BookSearchQuery"
Cohesion: 0.34
Nodes (5): BookSearchQuery, CatalogScope, All, Collection, OpenLibraryQueryTests

### Community 41 - "CatalogService"
Cohesion: 0.28
Nodes (4): CatalogService, IBookMetadataProvider, BookListResponse, CatalogEndpoints

### Community 43 - "backend"
Cohesion: 0.17
Nodes (12): build, depends_on, image, ports, restart, context, dockerfile, minio (+4 more)

### Community 47 - "AuthEndpoints"
Cohesion: 0.20
Nodes (3): AuthEndpoints, LibraryEndpoints, ClaimsPrincipalExtensions

### Community 48 - "IReadOnlyList"
Cohesion: 0.31
Nodes (3): ReadableWork, ReadableWorksPage, BookDetailsResponse

### Community 49 - "minio"
Cohesion: 0.22
Nodes (9): MINIO_ROOT_PASSWORD, MINIO_ROOT_USER, command, environment, image, ports, restart, volumes (+1 more)

### Community 50 - "postgres"
Cohesion: 0.22
Nodes (9): POSTGRES_DB, POSTGRES_PASSWORD, POSTGRES_USER, environment, image, ports, restart, volumes (+1 more)

### Community 51 - "MinioOptions"
Cohesion: 0.22
Nodes (8): MinioOptions, AccessKey, AvatarsBucket, BooksBucket, Endpoint, PublicBaseUrl, SecretKey, UseSsl

### Community 52 - "services"
Cohesion: 0.29
Nodes (6): services, volumes, caddy-config, caddy-data, minio-data, pg-data

### Community 56 - "healthcheck"
Cohesion: 0.53
Nodes (6): interval, retries, test, timeout, healthcheck, healthcheck

## Knowledge Gaps
- **173 isolated node(s):** `Semaphore`, `Users`, `Collection`, `All`, `Id` (+168 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 344 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **16 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Book` connect `Book Entity` to `Guid`, `EPUB Text Extraction`, `CatalogModule.AddCatalogModule`, `ReadableCollectionImporter`, `CatalogDbContext`, `UserBook Entity`, `EpubProcessingWorker.CopyCatalogMetadata`, `Task`, `CollectionOptions`?**
  _High betweenness centrality (0.050) - this node is a cross-community bridge._
- **Are the 3 inferred relationships involving `Book` (e.g. with `LibraryService.AddExternalAsync()` and `LibraryService.CreateUploadedBookAsync()`) actually correct?**
  _`Book` has 3 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Semaphore`, `Users`, `Collection` to the rest of the system?**
  _173 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `NuGet Package Manifest` be split into smaller, more focused modules?**
  _Cohesion score 0.05388471177944862 - nodes in this community are weakly interconnected._
- **Why does `YarPhoenix.Catalog.Application` connect `YarPhoenix.Catalog.Application` to `BookFileLocks`, `Open Library Client`, `YarPhoenix.Catalog.Infrastructure.OpenLibrary`, `BookSearchQuery`, `Claude API Client`, `IReadOnlyList`, `Host Package Dependencies`, `Fact`, `BookProcessingQueue`, `CatalogService.SearchWords`, `microsoft_entityframeworkcore`?**
  _High betweenness centrality (0.042) - this node is a cross-community bridge._
- **Should `AuthModule.AddAuthModule` be split into smaller, more focused modules?**
  _Cohesion score 0.11255411255411256 - nodes in this community are weakly interconnected._
- **Why does `ApplicationUser` connect `ApplicationUser` to `AuthService`, `Guid`, `AuthModule.AddAuthModule`, `AuthService.ChangeEmailAsync`, `EPUB Text Extraction`, `AuthDbContext`, `Endpoint Mapping`, `Book Search Contracts`, `AI Module Registration`, `Email & Dev Seeder`, `Task`?**
  _High betweenness centrality (0.024) - this node is a cross-community bridge._