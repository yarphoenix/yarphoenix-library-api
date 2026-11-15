# YarPhoenix Library — Backend

Портфолио-проект (цель — стажировка C#/.NET): веб-сервис для чтения книг с каталогом, личной
библиотекой и AI-ассистентом. Это **бэкенд**; фронтенд (React + Vite) живёт в
`C:\Users\yarik\WebstormProjects\yarphoenix-library`.

Требования: ТЗ `C:\Users\yarik\Downloads\YarPhoenix-Library-TechSpec-EN.md`. Архитектурные решения
в нём **приняты и не пересматриваются** — разделы Rationale объясняют почему. При конфликте с
«общепринятой практикой» побеждает ТЗ.

## Архитектура: модульный монолит (ТЗ §3 — не микросервисы, осознанно)

```
src/YarPhoenix.Api       хост: DI-регистрация модулей, CORS, rate limiter, healthcheck, миграции на старте
src/YarPhoenix.Catalog   ДОМЕННОЕ ЯДРО: Book, UserBook, полки, прогресс/CFI, стрик, ачивки, Open Library, читаемая коллекция
src/YarPhoenix.Auth      ASP.NET Core Identity + JWT + refresh-ротация, профиль
src/YarPhoenix.Storage   MinIO: приватный бакет books (+ публичный avatars — в планах), загрузка EPUB
src/YarPhoenix.Ai        IReadingAssistant: роутинг моделей, сборка промпта, Claude Messages API
src/YarPhoenix.Shared    Result/Error паттерн
tests/YarPhoenix.Catalog.Tests   xUnit (все юниты пока здесь)
```

Каждый модуль: `AddXxxModule(IServiceCollection, IConfiguration)` + `MapXxxEndpoints(app)` +
своя схема БД (`auth`, `catalog`) и свой DbContext. Поддерживающие модули могут ссылаться на
Catalog (ядро); Catalog не ссылается ни на кого, кроме Shared.

## Команды

```bash
dotnet build yarphoenix-library-api.slnx        # style enforced (EnforceCodeStyleInBuild)
dotnet test yarphoenix-library-api.slnx
docker compose up -d postgres minio             # зависимости (бэкенд в компоузе только для прода)
dotnet run --project src/YarPhoenix.Api         # http://127.0.0.1:5080, миграции применятся сами

# новая миграция (пример для Auth; для Catalog заменить проект и контекст)
dotnet ef migrations add <Name> --project src/YarPhoenix.Auth --startup-project src/YarPhoenix.Api
```

## Контракт с фронтендом (OpenAPI)

- В Development API отдаёт документ `http://127.0.0.1:5080/openapi/v1.json` (в Production — нет).
  Из него фронтенд генерирует типы: `npm run gen:api` в репозитории фронтенда.
- **У каждого эндпоинта с телом ответа обязателен `.Produces<T>()`** (для 204/404 — `.Produces(код)`):
  лямбды возвращают `IResult`, и без метаданных тип ответа в схему не попадёт.
- Обязательные поля тел запросов — то, что объявлено в `*Request` без `?`: `RequiredFieldsFilter`
  (на корневой группе маршрутов в `Program.cs`) отвечает 400 с именем поля. Необязательное поле
  объявлять nullable, иначе запрос без него перестанет проходить.
- После изменения контрактов (`*Contracts.cs`, маршруты, параметры) — перегенерировать типы на фронтенде
  и закоммитить `schema.d.ts` вместе с изменением.
- Числа в JSON — строго числами (`JsonNumberHandling.Strict` в `Program.cs`): иначе схема описывает
  каждое числовое поле как «число или строка».

## Локальное окружение и сеть (важно)

- **Postgres контейнера — на 127.0.0.1:5433** (не 5432: на машине живёт нативный виндовый Postgres).
  Логин/пароль/БД, ключи MinIO и JWT-секрет — в user-secrets
  (`dotnet user-secrets list --project src/YarPhoenix.Api`); в `appsettings.Development.json` секретов нет.
- В Development при старте сидится демо-пользователь `reader@example.com` / `password123`
  (`DevUserSeeder`, секция `DevSeed`); в Production сидер не вызывается.
- MinIO: 127.0.0.1:9000 (консоль 9001). Секреты compose — в `.env` (gitignored, образец `.env.example`).
- VPN/прокси включён всегда (`HTTP(S)_PROXY=127.0.0.1:10809`); `HttpClient` подхватывает env-прокси сам.
  **openlibrary.org заблокирован провайдером** — без прокси недоступен; через тот же прокси ходят
  gutenberg.org и standardebooks.org. На VPS в РФ понадобится egress-прокси (задать HTTPS_PROXY в compose).
- Фоновый импорт коллекции стартует вместе с API и ходит в сеть (Open Library + каталог Gutenberg,
  несколько минут). Для офлайн-работы и отладки без сети: `Collection__ImportEnabled=false`.
- Для оффлайн-разработки каталога есть заглушка Open Library:
  `OpenLibrary__BaseUrl=http://localhost:5090 dotnet run ...` (стаб-сервер лежал в scratchpad сессии —
  при необходимости пересоздать: простой JSON-сервер с форматом /search.json).
- curl-диагностика локальных портов: всегда `curl --noproxy '*' http://127.0.0.1:...`.

## Домен: инварианты из ТЗ, которые нельзя ломать

- **Два мира (§1.1)**: каталог (мир A) = метаданные Open Library, витрина/вишлист; читалка и AI
  (мир B) работают **только при наличии файла** (`Book.FileId` в MinIO). «Текст открывает AI»,
  а не полка. Мост между мирами — читаемая коллекция (ниже).
- **Book (§4.1)**: одна сущность; внешняя запись уникальна по паре `(Provider, ExternalId)` —
  не по ExternalId в одиночку. Откуда у книги текст — `ReadableSource`
  (None | StandardEbooks | Gutenberg | Upload) + `SourceRef`.
- **Читаемая коллекция** (решение владельца, **сознательное отступление от ТЗ §5.1.2** — там слот
  файлов = Internet Archive; не возвращать): `Book.InCollection` — книги с вычитанным EPUB из
  Standard Ebooks / Project Gutenberg, найденные по `id_standard_ebooks` / `id_project_gutenberg`
  в Open Library. Собирает `ReadableCollectionImporter` (при пустой коллекции, далее раз в неделю;
  возобновляемый, upsert по `(Provider, ExternalId)`). В коллекцию попадает только: английский
  текст, название + автор + обложка, общественное достояние по `PublicDomainRule` (все создатели
  издания умерли ≥ 75 лет назад; год неизвестен → пропуск, кроме `PublicDomainWhitelist`).
  `GET /api/catalog/books` по умолчанию отдаёт коллекцию из Postgres, `scope=all` — живой Open
  Library; язык интерфейса выдачу **не** фильтрует. Файл качается лениво по «Читать»
  (`CuratedEbookSource`) под блокировкой по книге (`BookFileLocks`); сбой источника — 503, не 500.
- **Файл пользователя — только его (K1)**: загруженный EPUB никогда не прикрепляется к общей
  каталожной записи. Матчинг (§4.3, ISBN → title+author) копирует из каталога лишь метаданные
  (`EpubProcessingWorker.CopyCatalogMetadata`); `FileId` остаётся у личной записи.
- **Гигиена данных Open Library**: обложки `<= 0` → null, описания чистятся (`OpenLibraryText`),
  темы — без машинных тегов вроде `series:Harry_Potter` (`OpenLibrarySubjects`), пользовательский
  текст экранируется в Solr-запросе. Чистим на бэкенде — на фронтенде фильтры не дублировать.
- **UserBook (§4.2)**: Status (WantToRead|Reading|Read) + отдельный флаг IsFavorite (не enum!);
  CFI-позиция хранится тут; «Читать» делает upsert в Reading (§4.4) — отдельного «добавить» не нужно.
- **Стрик (§4.5)**: мягкий, O(1) (CurrentStreak/LongestStreak/LastActiveDate, день — по таймзоне
  пользователя), ленивое погашение при чтении значения, ачивки 10/30/50/100 выдаются идемпотентно
  и не отбираются.
- **Устойчивость (§8)**: у КАЖДОЙ внешней зависимости свой Polly-конвейер
  (Retry(2, без 429/4xx) → CircuitBreaker(0.5/10/30s/15s) → Timeout 20s/попытка). Фолбэк возвращает
  значение, не бросает: ассистент отдаёт 200 + `available:false`, каталог при падении OL — 503
  с понятным сообщением, письмо не ушло — мягкий ответ, не 500.

## Внешние зависимости и конфиги

| Зависимость | Конфиг | Локально |
|---|---|---|
| Postgres | `ConnectionStrings:Postgres` | контейнер, порт 5433 |
| MinIO | секция `Minio` | контейнер 9000/9001 |
| Open Library | `OpenLibrary:BaseUrl` | реальный или стаб :5090 |
| Standard Ebooks / Project Gutenberg | секция `Collection` (базовые URL, `ImportEnabled`, `MaxFileBytes`) | реальные; `ImportEnabled=false` — без фонового импорта |
| Claude API | `Anthropic:ApiKey` (+ модели Haiku/Sonnet) | без ключа — мягкая деградация ассистента |
| Email | пока `LoggingAuthEmailSender` (ссылки в лог) | реальный провайдер — в планах |
| JWT | `Jwt:Secret` ≥ 32 символов (fail fast на старте) | dev-секрет в user-secrets |

## Код-стайл

`.claude/rules/code-style.md` обязателен: Allman, 4 пробела, primary constructors для DI-классов,
file-scoped namespaces, `_camelCase` поля, суффикс Async + CancellationToken всюду, Result-паттерн
вместо исключений в бизнес-логике. `.editorconfig` включён в сборку (`-warnaserror` в CI).

## CI/CD (ТЗ §11)

`.github/workflows/backend.yml`: restore → build -warnaserror → test (на PR ещё docker build);
push в main/master публикует образ в GHCR (`ghcr.io/<owner>/<repo>:latest` и `:<sha>`). Деплой на VPS
(Beget) — job `deploy`, только вручную (Run workflow): SSH → `git pull` → `docker compose pull backend`
→ `up -d`; секреты и настройка — `docs/DEPLOY.md`, раздел 5. TLS через Caddy (HTTPS обязателен: фронт
на gh-pages HTTPS-only, mixed content блокируется). CORS-origin прода задаётся `FRONTEND_ORIGIN` в `.env`.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- When graphify-out/graph.json exists, run `graphify query "<question>"` before your first grep, search or source-file read for a codebase question, and put this rule in every subagent prompt that explores code. Read raw files only for lines the graph or an earlier read already located. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
- A committed graphify-out/graph.json is rewritten whole on every build, so a large diff to it in a pull request is expected. Do not flag it as churn or suggest skipping the graph rebuild or its hooks.
