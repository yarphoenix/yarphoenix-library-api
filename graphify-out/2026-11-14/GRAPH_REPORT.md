# Graph Report - yarphoenix-library-api  (2026-11-14)

## Corpus Check
- Corpus is ~21,921 words - fits in a single context window. You may not need a graph.

## Summary
- 891 nodes · 2125 edges · 42 communities (32 shown, 10 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 133 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

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
- Auth Refresh Tests
- MinIO Options
- ApplicationUser Profile
- Token Service
- EPUB Processing Worker
- JWT Options
- AI Infra Resilience
- Dev Seed Options
- Result Pattern

## God Nodes (most connected - your core abstractions)
1. `Result` - 41 edges
2. `AuthService` - 38 edges
3. `ApplicationUser` - 25 edges
4. `Book` - 25 edges
5. `LibraryService` - 23 edges
6. `UserBook` - 23 edges
7. `YarPhoenix.Catalog.Application` - 21 edges
8. `ReaderProfile` - 21 edges
9. `CatalogDbContext` - 21 edges
10. `RefreshToken` - 19 edges

## Surprising Connections (you probably didn't know these)
- `AuthServiceRefreshTests` --references--> `AuthService`  [EXTRACTED]
  tests/YarPhoenix.Auth.Tests/AuthServiceRefreshTests.cs → src/YarPhoenix.Auth/Application/AuthService.cs
- `AuthServiceRefreshTests` --references--> `TokenService`  [EXTRACTED]
  tests/YarPhoenix.Auth.Tests/AuthServiceRefreshTests.cs → src/YarPhoenix.Auth/Application/TokenService.cs
- `DevUserSeederTests` --references--> `ApplicationUser`  [EXTRACTED]
  tests/YarPhoenix.Auth.Tests/DevUserSeederTests.cs → src/YarPhoenix.Auth/Domain/ApplicationUser.cs
- `AuthServiceRefreshTests` --references--> `AuthDbContext`  [EXTRACTED]
  tests/YarPhoenix.Auth.Tests/AuthServiceRefreshTests.cs → src/YarPhoenix.Auth/Infrastructure/AuthDbContext.cs
- `RefreshTokens` --references--> `RefreshToken`  [EXTRACTED]
  src/YarPhoenix.Auth/Infrastructure/AuthDbContext.cs → src/YarPhoenix.Auth/Domain/RefreshToken.cs

## Import Cycles
- None detected.

## Communities (42 total, 10 thin omitted)

### Community 0 - "Core Services & Endpoints"
Cohesion: 0.06
Nodes (23): AuthService, IAuthEmailSender, RegisterResponse, TokenResponse, UserResponse, AuthEndpoints, BookProcessingQueue, LibraryService (+15 more)

### Community 1 - "AI Prompt Templates & Tests"
Cohesion: 0.07
Nodes (14): AssistantTemplates, TemplatePlan, CfiParser, StreakLogic, ReaderProfile, CurrentStreak, LastActiveDate, LongestStreak (+6 more)

### Community 2 - "NuGet Package Manifest"
Cohesion: 0.06
Nodes (44): Microsoft.AspNetCore.Authentication.JwtBearer (10.0.9), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9), Microsoft.AspNetCore.OpenApi (10.0.15), Microsoft.EntityFrameworkCore.Design (10.0.9), Microsoft.EntityFrameworkCore.Sqlite (10.0.2), Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore (10.0.9), Minio (7.0.0), Microsoft.NET.Sdk.Web (+36 more)

### Community 3 - "Docker Compose Config"
Cohesion: 0.06
Nodes (39): depends_on, environment, image, ports, profiles, restart, volumes, API_DOMAIN (+31 more)

### Community 4 - "CI/CD Workflow"
Cohesion: 0.06
Nodes (38): runs-on, steps, environment, if, needs, runs-on, steps, DEPLOY_HOST (+30 more)

### Community 5 - "Open Library Client"
Cohesion: 0.07
Nodes (27): PublicDomainCandidate, OpenLibraryAuthor, Name, OpenLibraryDoc, AuthorNames, CoverId, EbookAccess, FirstPublishYear (+19 more)

### Community 6 - "EPUB Text Extraction"
Cohesion: 0.11
Nodes (5): EpubTextExtractor, EpubMetadata, EpubMetadataParser, EpubMetadataParserTests, EpubTextExtractorTests

### Community 7 - "Backend Docker Services"
Cohesion: 0.07
Nodes (28): build, depends_on, environment, image, ports, restart, context, dockerfile (+20 more)

### Community 8 - "Internet Archive Client"
Cohesion: 0.14
Nodes (14): IaFile, Format, Name, Private, Size, IaFileSelector, IaLendingFilter, IaMetadata (+6 more)

### Community 9 - "Auth Contracts"
Cohesion: 0.08
Nodes (13): YarPhoenix.Auth.Contracts, ChangeEmailRequest, ChangePasswordRequest, ConfirmEmailChangeRequest, ConfirmEmailRequest, DeleteAccountRequest, ForgotPasswordRequest, LoginRequest (+5 more)

### Community 10 - "Project References & Packages"
Cohesion: 0.13
Nodes (6): YarPhoenix.Ai.Application, YarPhoenix.Catalog.Infrastructure.InternetArchive, YarPhoenix.Catalog.Infrastructure.OpenLibrary, YarPhoenix.Ai.Infrastructure.Epub, YarPhoenix.Catalog.Tests, OpenLibraryQuery

### Community 11 - "Auth Migrations"
Cohesion: 0.17
Nodes (5): InitialAuth, AddUserAvatarKey, AuthDbContextModelSnapshot, InitialCatalog, CatalogDbContextModelSnapshot

### Community 12 - "Auth Project Dependencies"
Cohesion: 0.22
Nodes (5): YarPhoenix.Auth.Application, YarPhoenix.Auth.Domain, YarPhoenix.Auth.Infrastructure, YarPhoenix.Auth.Tests, YarPhoenix.Shared

### Community 13 - "Catalog Project Dependencies"
Cohesion: 0.15
Nodes (6): YarPhoenix.Catalog.Contracts, YarPhoenix.Catalog.Infrastructure, YarPhoenix.Catalog.Application, YarPhoenix.Catalog.Domain, YarPhoenix.Storage, IBookMetadataProvider

### Community 15 - "Endpoint Mapping"
Cohesion: 0.15
Nodes (8): YarPhoenix.Auth.Endpoints, YarPhoenix.Catalog, YarPhoenix.Auth, YarPhoenix.Ai.Endpoints, YarPhoenix.Catalog.Endpoints, AssistantEndpoints, CatalogEndpoints, ClaimsPrincipalExtensions

### Community 16 - "Claude API Client"
Cohesion: 0.18
Nodes (11): ClaudeCacheControl, ClaudeClient, ClaudeContentBlock, Text, Type, ClaudeMessage, ClaudeMessageRequest, ClaudeMessageResponse (+3 more)

### Community 17 - "Book Search Contracts"
Cohesion: 0.24
Nodes (4): BookSearchQuery, BookListResponse, BookSummaryResponse, OpenLibraryQueryTests

### Community 18 - "Book Entity"
Cohesion: 0.12
Nodes (17): Book, Author, CoverId, CreatedAtUtc, Description, ExternalId, FileId, FirstPublishYear (+9 more)

### Community 21 - "Public Domain Source"
Cohesion: 0.18
Nodes (6): IPublicDomainSource, PublicDomainFile, UserDataRemover, CatalogModule, InternetArchiveSource, IUserDataRemover

### Community 22 - "UserBook Entity"
Cohesion: 0.14
Nodes (14): BookStatus, Read, Reading, WantToRead, UserBook, AddedAtUtc, Book, BookId (+6 more)

### Community 23 - "Reading Assistant Contracts"
Cohesion: 0.22
Nodes (4): YarPhoenix.Ai.Contracts, IReadingAssistant, AskAssistantRequest, AssistantResponse

### Community 24 - "Catalog DbContext & Achievements"
Cohesion: 0.18
Nodes (10): UserAchievement, Threshold, UnlockedAtUtc, UserId, CatalogDbContext, Books, ReaderProfiles, UserAchievements (+2 more)

### Community 25 - "AI Module Registration"
Cohesion: 0.22
Nodes (5): AiModule, FrontendOptions, BaseUrl, AuthModule, StorageModule

### Community 26 - "Email & Dev Seeder"
Cohesion: 0.17
Nodes (6): EmailOptions, ApiKey, From, DevUserSeeder, LoggingAuthEmailSender, ResendEmailSender

### Community 28 - "Refresh Token Entity"
Cohesion: 0.18
Nodes (10): RefreshToken, CreatedAtUtc, ExpiresAtUtc, Id, IsActive, ReplacedByTokenHash, RevokedAtUtc, TokenHash (+2 more)

### Community 29 - "Launch Settings"
Cohesion: 0.20
Nodes (9): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, profiles, http (+1 more)

### Community 30 - "Auth DbContext"
Cohesion: 0.25
Nodes (3): AuthDbContext, RefreshTokens, AuthDbContextFactory

### Community 31 - "Claude Reading Assistant"
Cohesion: 0.22
Nodes (6): ClaudeOptions, ApiKey, BaseUrl, HaikuModel, SonnetModel, ClaudeReadingAssistant

### Community 34 - "MinIO Options"
Cohesion: 0.22
Nodes (8): MinioOptions, AccessKey, AvatarsBucket, BooksBucket, Endpoint, PublicBaseUrl, SecretKey, UseSsl

### Community 35 - "ApplicationUser Profile"
Cohesion: 0.25
Nodes (6): ApplicationUser, AvatarKey, CreatedAtUtc, FavoriteGenres, Nickname, UiLanguage

### Community 38 - "JWT Options"
Cohesion: 0.29
Nodes (6): JwtOptions, AccessTokenMinutes, Audience, Issuer, RefreshTokenDays, Secret

### Community 40 - "Dev Seed Options"
Cohesion: 0.40
Nodes (4): DevSeedOptions, Email, Nickname, Password

## Knowledge Gaps
- **142 isolated node(s):** `Content`, `StopReason`, `Type`, `Text`, `BaseUrl` (+137 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 284 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `AuthService` connect `Core Services & Endpoints` to `Auth Refresh Tests`, `ApplicationUser Profile`, `Token Service`, `Auth Project Dependencies`, `Public Domain Source`, `AI Module Registration`, `Email & Dev Seeder`, `DevUserSeeder Tests`, `Auth DbContext`?**
  _High betweenness centrality (0.045) - this node is a cross-community bridge._
- **What connects `Content`, `StopReason`, `Type` to the rest of the system?**
  _142 weakly-connected nodes found - possible missing edges._
- **Should `Core Services & Endpoints` be split into smaller, more focused modules?**
  _Cohesion score 0.05835484232970486 - nodes in this community are weakly interconnected._
- **Why does `ApplicationUser` connect `ApplicationUser Profile` to `Core Services & Endpoints`, `Auth Refresh Tests`, `Token Service`, `Auth Migrations`, `Auth Project Dependencies`, `AI Module Registration`, `Email & Dev Seeder`, `DevUserSeeder Tests`, `Refresh Token Entity`, `Auth DbContext`?**
  _High betweenness centrality (0.039) - this node is a cross-community bridge._
- **Should `AI Prompt Templates & Tests` be split into smaller, more focused modules?**
  _Cohesion score 0.06885245901639345 - nodes in this community are weakly interconnected._
- **Why does `Book` connect `Book Entity` to `Core Services & Endpoints`, `EPUB Processing Worker`, `Auth Migrations`, `UserBook Entity`, `Catalog DbContext & Achievements`?**
  _High betweenness centrality (0.030) - this node is a cross-community bridge._
- **Should `NuGet Package Manifest` be split into smaller, more focused modules?**
  _Cohesion score 0.0595959595959596 - nodes in this community are weakly interconnected._