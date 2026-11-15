# Graph Report - yarphoenix-library-api  (2026-11-15)

## Corpus Check
- 125 files · ~31,078 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: (none) 6, .example 1, .props 1)

## Summary
- 1247 nodes · 2986 edges · 66 communities (45 shown, 21 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 145 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `3d445458`
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
- GeneratedRegex
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
- OpenLibrarySubjects.ToGenres
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
- InlineData
- BookSearchQuery
- EpubTextExtractor
- CatalogModule.AddCatalogModule
- backend
- IReadOnlyList
- AssistantEndpoints.MapAssistantEndpoints
- ReadableCollectionImporter
- LibraryEndpoints.ToErrorResult
- Fact
- minio
- postgres
- MinioOptions
- services
- EpubProcessingWorker.CopyCatalogMetadata
- GutenbergCatalog
- GutenbergEditionSelector.Pick
- healthcheck
- EpubMetadataParser.Parse
- PublicDomainRule.IsPublicDomain
- GutenbergCatalogTests
- ReadableSource
- GutenbergCatalog.Parse
- AiModule.AddAiModule
- ClaudeReadingAssistant.cs
- Dictionary
- ClaimsPrincipalExtensions.GetUserId

## God Nodes (most connected - your core abstractions)
1. `Book` - 40 edges
2. `AuthService` - 38 edges
3. `Result` - 31 edges
4. `CatalogDbContext` - 29 edges
5. `YarPhoenix.Catalog.Application` - 28 edges
6. `LibraryService` - 27 edges
7. `ApplicationUser` - 25 edges
8. `GutenbergCatalog` - 23 edges
9. `RefreshToken` - 19 edges
10. `CollectionOptions` - 18 edges

## Surprising Connections (you probably didn't know these)
- `AuthServiceRefreshTests` --references--> `AuthService`  [EXTRACTED]
  tests/YarPhoenix.Auth.Tests/AuthServiceRefreshTests.cs → src/YarPhoenix.Auth/Application/AuthService.cs
- `CollectionEntryFactoryTests` --references--> `GutenbergCatalog`  [EXTRACTED]
  tests/YarPhoenix.Catalog.Tests/CollectionEntryFactoryTests.cs → src/YarPhoenix.Catalog/Infrastructure/Collection/GutenbergCatalog.cs
- `GutenbergEditionSelectorTests` --references--> `GutenbergCatalog`  [EXTRACTED]
  tests/YarPhoenix.Catalog.Tests/GutenbergEditionSelectorTests.cs → src/YarPhoenix.Catalog/Infrastructure/Collection/GutenbergCatalog.cs
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

## Communities (66 total, 21 thin omitted)

### Community 0 - "AuthService"
Cohesion: 0.14
Nodes (10): AuthService, RegisterResponse, TokenResponse, UserResponse, AuthEndpoints, Error, Result, Error (+2 more)

### Community 1 - "AssistantTemplates.TryBuildPlan"
Cohesion: 0.21
Nodes (3): AssistantTemplates, TemplatePlan, AssistantTemplatesTests

### Community 2 - "NuGet Package Manifest"
Cohesion: 0.05
Nodes (57): YarPhoenix.Ai (IReadingAssistant), YarPhoenix.Api (host), YarPhoenix.Auth, Book entity, YarPhoenix.Catalog (domain core), YarPhoenix.Catalog.Tests (xUnit), CI/CD workflow (backend.yml), Claude API (+49 more)

### Community 3 - "AuthModule.AddAuthModule"
Cohesion: 0.17
Nodes (7): EmailOptions, ApiKey, From, FrontendOptions, BaseUrl, AuthModule, StorageModule

### Community 4 - "CI/CD Workflow"
Cohesion: 0.06
Nodes (44): Microsoft.AspNetCore.Authentication.JwtBearer (10.0.9), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9), Microsoft.AspNetCore.OpenApi (10.0.15), Microsoft.EntityFrameworkCore.Design (10.0.9), Microsoft.EntityFrameworkCore.Sqlite (10.0.2), Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore (10.0.9), Minio (7.0.0), Microsoft.NET.Sdk.Web (+36 more)

### Community 5 - "Open Library Client"
Cohesion: 0.07
Nodes (22): YarPhoenix.Ai.Infrastructure, YarPhoenix.Ai.Contracts, IReadingAssistant, AskAssistantRequest, AssistantResponse, ClaudeCacheControl, ClaudeClient, ClaudeContentBlock (+14 more)

### Community 6 - "EPUB Text Extraction"
Cohesion: 0.07
Nodes (18): InitialAuth, AddUserAvatarKey, AuthDbContextModelSnapshot, StreakLogic, ReaderProfile, CurrentStreak, LastActiveDate, LongestStreak (+10 more)

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
Cohesion: 0.06
Nodes (30): ReadableWork, ReadableWorksPage, OpenLibraryAuthor, Name, OpenLibraryDoc, AuthorNames, CoverId, EditionCount (+22 more)

### Community 12 - "Auth Project Dependencies"
Cohesion: 0.12
Nodes (16): environment, Anthropic__ApiKey, ASPNETCORE_ENVIRONMENT, ConnectionStrings__Postgres, Cors__AllowedOrigins__0, Email__ApiKey, Email__From, Frontend__BaseUrl (+8 more)

### Community 13 - "Catalog Project Dependencies"
Cohesion: 0.08
Nodes (12): ChangeEmailRequest, ChangePasswordRequest, ConfirmEmailChangeRequest, ConfirmEmailRequest, DeleteAccountRequest, ForgotPasswordRequest, LoginRequest, LogoutRequest (+4 more)

### Community 14 - "EpubTextExtractor.cs"
Cohesion: 0.17
Nodes (4): YarPhoenix.Ai.Application, YarPhoenix.Ai.Infrastructure.Epub, YarPhoenix.Catalog.Infrastructure.Epub, YarPhoenix.Catalog.Tests

### Community 15 - "Endpoint Mapping"
Cohesion: 0.16
Nodes (4): YarPhoenix.Auth.Application, YarPhoenix.Auth.Domain, YarPhoenix.Auth.Infrastructure, YarPhoenix.Auth.Tests

### Community 16 - "Claude API Client"
Cohesion: 0.15
Nodes (6): YarPhoenix.Auth.Endpoints, YarPhoenix.Auth.Contracts, YarPhoenix.Ai.Endpoints, YarPhoenix.Catalog.Endpoints, YarPhoenix.Shared, CatalogEndpoints

### Community 17 - "Book Search Contracts"
Cohesion: 0.17
Nodes (11): RefreshToken, CreatedAtUtc, ExpiresAtUtc, Id, IsActive, ReplacedByTokenHash, RevokedAtUtc, TokenHash (+3 more)

### Community 18 - "Book Entity"
Cohesion: 0.10
Nodes (21): Book, Author, CoverId, CreatedAtUtc, Description, ExternalId, FallbackGutenbergId, FileId (+13 more)

### Community 19 - "Host Package Dependencies"
Cohesion: 0.14
Nodes (3): YarPhoenix.Ai, YarPhoenix.Catalog, YarPhoenix.Auth

### Community 20 - "YarPhoenix.Catalog.Application"
Cohesion: 0.14
Nodes (5): YarPhoenix.Catalog.Contracts, YarPhoenix.Catalog.Application, YarPhoenix.Catalog.Domain, YarPhoenix.Storage, PublicDomainRule

### Community 21 - "CatalogDbContext"
Cohesion: 0.18
Nodes (10): CollectionImportState, Id, LastCompletedAtUtc, NextPage, CatalogDbContext, Books, CollectionImportStates, ReaderProfiles (+2 more)

### Community 22 - "UserBook Entity"
Cohesion: 0.14
Nodes (14): BookStatus, Read, Reading, WantToRead, UserBook, AddedAtUtc, Book, BookId (+6 more)

### Community 24 - "ApplicationUser"
Cohesion: 0.22
Nodes (6): ApplicationUser, AvatarKey, CreatedAtUtc, FavoriteGenres, Nickname, UiLanguage

### Community 25 - "AI Module Registration"
Cohesion: 0.15
Nodes (4): TokenService, AuthDbContext, AuthDbContextFactory, AuthServiceRefreshTests

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
Cohesion: 0.12
Nodes (4): UserDataRemover, IUserDataRemover, IFileStorage, MinioFileStorage

### Community 30 - "microsoft_entityframeworkcore"
Cohesion: 0.21
Nodes (4): YarPhoenix.Catalog.Migrations, YarPhoenix.Catalog.Infrastructure, YarPhoenix.Auth.Migrations, CatalogDbContextFactory

### Community 31 - "CollectionOptions"
Cohesion: 0.09
Nodes (13): CollectionOptions, GutenbergBaseUrl, GutenbergMirrorBaseUrl, ImportEnabled, MaxFileBytes, PageSize, RefreshDays, RequestDelayMilliseconds (+5 more)

### Community 33 - "AssistantEndpoints.MapAssistantEndpoints"
Cohesion: 0.14
Nodes (3): YarPhoenix.Api, RequiredFieldsFilter, RequiredFields

### Community 36 - "BookFileLocks"
Cohesion: 0.17
Nodes (6): BookFileLocks, Entry, Semaphore, Users, Releaser, BookFileLocksTests

### Community 37 - "AuthService.ChangeEmailAsync"
Cohesion: 0.17
Nodes (3): IAuthEmailSender, LoggingAuthEmailSender, ResendEmailSender

### Community 38 - "YarPhoenix.Catalog.Infrastructure.OpenLibrary"
Cohesion: 0.14
Nodes (4): YarPhoenix.Catalog.Infrastructure.Collection, YarPhoenix.Catalog.Infrastructure.OpenLibrary, PublicDomainWhitelist, CsvReader

### Community 40 - "BookSearchQuery"
Cohesion: 0.10
Nodes (11): ReadingContextBuilder, BookSearchQuery, CatalogScope, All, Collection, CatalogService, IBookMetadataProvider, BookDetailsResponse (+3 more)

### Community 43 - "backend"
Cohesion: 0.17
Nodes (12): build, depends_on, image, ports, restart, context, dockerfile, minio (+4 more)

### Community 46 - "ReadableCollectionImporter"
Cohesion: 0.33
Nodes (3): IPublicDomainSource, CollectionEntry, ReadableCollectionImporter

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

### Community 54 - "GutenbergCatalog"
Cohesion: 0.27
Nodes (4): GutenbergCatalog, Count, GutenbergEdition, GutenbergPerson

### Community 56 - "healthcheck"
Cohesion: 0.53
Nodes (6): interval, retries, test, timeout, healthcheck, healthcheck

### Community 57 - "EpubMetadataParser.Parse"
Cohesion: 0.24
Nodes (3): EpubMetadata, EpubMetadataParser, EpubMetadataParserTests

### Community 60 - "ReadableSource"
Cohesion: 0.22
Nodes (8): BookProvider, InternetArchive, OpenLibrary, ReadableSource, Gutenberg, None, StandardEbooks, Upload

## Knowledge Gaps
- **175 isolated node(s):** `ImportEnabled`, `StandardEbooksBaseUrl`, `GutenbergBaseUrl`, `GutenbergMirrorBaseUrl`, `PageSize` (+170 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 350 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **21 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Book` connect `Book Entity` to `Guid`, `EPUB Text Extraction`, `BookSearchQuery`, `ReadableCollectionImporter`, `CatalogDbContext`, `UserBook Entity`, `EpubProcessingWorker.CopyCatalogMetadata`, `ReadableSource`, `Task`?**
  _High betweenness centrality (0.058) - this node is a cross-community bridge._
- **Are the 3 inferred relationships involving `Book` (e.g. with `LibraryService.AddExternalAsync()` and `LibraryService.CreateUploadedBookAsync()`) actually correct?**
  _`Book` has 3 INFERRED edges - model-reasoned connections that need verification._
- **What connects `ImportEnabled`, `StandardEbooksBaseUrl`, `GutenbergBaseUrl` to the rest of the system?**
  _175 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `AuthService` be split into smaller, more focused modules?**
  _Cohesion score 0.14169570267131243 - nodes in this community are weakly interconnected._
- **Why does `CatalogDbContext` connect `CatalogDbContext` to `Guid`, `BookSearchQuery`, `CatalogModule.AddCatalogModule`, `ReadableCollectionImporter`, `Book Entity`, `AI Module Registration`, `Task`, `microsoft_entityframeworkcore`?**
  _High betweenness centrality (0.044) - this node is a cross-community bridge._
- **Should `NuGet Package Manifest` be split into smaller, more focused modules?**
  _Cohesion score 0.05388471177944862 - nodes in this community are weakly interconnected._
- **Why does `ApplicationUser` connect `ApplicationUser` to `AuthService`, `Guid`, `AuthModule.AddAuthModule`, `AuthService.ChangeEmailAsync`, `EPUB Text Extraction`, `Endpoint Mapping`, `Book Search Contracts`, `AI Module Registration`, `Email & Dev Seeder`, `Task`?**
  _High betweenness centrality (0.040) - this node is a cross-community bridge._