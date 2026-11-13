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
src/YarPhoenix.Catalog   ДОМЕННОЕ ЯДРО: Book, UserBook, полки, прогресс/CFI, стрик, ачивки, Open Library, матчинг
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

## Локальное окружение и сеть (важно)

- **Postgres контейнера — на 127.0.0.1:5433** (не 5432: на машине живёт нативный виндовый Postgres).
  Логин/пароль/БД — в `appsettings.Development.json`.
- MinIO: 127.0.0.1:9000 (консоль 9001). Секреты compose — в `.env` (gitignored, образец `.env.example`).
- VPN/прокси включён всегда (`HTTP(S)_PROXY=127.0.0.1:10809`); `HttpClient` подхватывает env-прокси сам.
  **openlibrary.org и archive.org заблокированы провайдером** — без прокси недоступны; на VPS в РФ
  понадобится egress-прокси (задать HTTPS_PROXY в compose).
- Для оффлайн-разработки каталога есть заглушка Open Library:
  `OpenLibrary__BaseUrl=http://localhost:5090 dotnet run ...` (стаб-сервер лежал в scratchpad сессии —
  при необходимости пересоздать: простой JSON-сервер с форматом /search.json).
- curl-диагностика локальных портов: всегда `curl --noproxy '*' http://127.0.0.1:...`.

## Домен: инварианты из ТЗ, которые нельзя ломать

- **Два мира (§1.1)**: каталог (мир A) = метаданные Open Library, витрина/вишлист; читалка и AI
  (мир B) работают **только при наличии файла** (`Book.FileId` в MinIO). «Текст открывает AI»,
  а не полка. Мосты между мирами: матчинг загруженного EPUB (§4.3, best-effort по ISBN→title+author)
  и `ocaid` у записей OL.
- **Book (§4.1)**: одна сущность; внешняя запись уникальна по паре `(Provider, ExternalId)` —
  не по ExternalId в одиночку. Provider: OpenLibrary | InternetArchive.
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
| Claude API | `Anthropic:ApiKey` (+ модели Haiku/Sonnet) | без ключа — мягкая деградация ассистента |
| Email | пока `LoggingAuthEmailSender` (ссылки в лог) | реальный провайдер — в планах |
| JWT | `Jwt:Secret` ≥ 32 символов (fail fast на старте) | dev-секрет в appsettings.Development.json |

## Код-стайл

`.claude/rules/code-style.md` обязателен: Allman, 4 пробела, primary constructors для DI-классов,
file-scoped namespaces, `_camelCase` поля, суффикс Async + CancellationToken всюду, Result-паттерн
вместо исключений в бизнес-логике. `.editorconfig` включён в сборку (`-warnaserror` в CI).

## CI/CD (ТЗ §11)

`.github/workflows/backend.yml`: restore → build -warnaserror → test → docker build. Деплой на VPS
(Beget) — вручную для MVP: `docker compose up -d` + TLS через Caddy/nginx (HTTPS обязателен: фронт
на gh-pages HTTPS-only, mixed content блокируется). CORS-origin прода задаётся `FRONTEND_ORIGIN` в `.env`.
