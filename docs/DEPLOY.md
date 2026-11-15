# Деплой YarPhoenix Library

Два раздельных таргета (ТЗ §11.1):

- **Бэкенд + PostgreSQL + MinIO + Caddy** → Docker на VPS.
- **Фронтенд** → GitHub Pages (статическая сборка через GitHub Actions).

Они на разных origin, поэтому CORS и HTTPS — обязательны (§11.4).

---

## 1. Бэкенд на VPS

### 1.1. Предпосылки

- VPS (например Beget) с Ubuntu, установленными Docker и Docker Compose.
- Порты **80** и **443** открыты (Caddy получает TLS по ACME на 80/443).
- Домен для API. Варианты:
  - своя **A-запись** на IP VPS (`api.example.com` → `1.2.3.4`);
  - без покупки домена — **sslip.io**: для IP `1.2.3.4` используйте `1-2-3-4.sslip.io`,
    Let's Encrypt такой домен подписывает.

### 1.2. Клонирование и конфигурация

```bash
git clone <backend-repo> yarphoenix-api && cd yarphoenix-api
cp .env.example .env
```

Заполните `.env` (см. комментарии в `.env.example`):

- `POSTGRES_PASSWORD`, `MINIO_ROOT_PASSWORD` — сильные пароли.
- `JWT_SECRET` — `openssl rand -hex 48`.
- `API_DOMAIN` — домен API (или `1-2-3-4.sslip.io`).
- `MINIO_PUBLIC_URL=https://<API_DOMAIN>` — аватары идут через Caddy на том же домене.
- `FRONTEND_ORIGIN=https://<username>.github.io` — для CORS.
- `ANTHROPIC_API_KEY`, `RESEND_API_KEY`, `EMAIL_FROM` — если нужны AI и письма
  (иначе обе фичи мягко деградируют).
- `EGRESS_PROXY` — если провайдер VPS блокирует openlibrary.org, gutenberg.org,
  standardebooks.org или api.anthropic.com, укажите egress-прокси
  `http://user:pass@host:port` (§8). Без доступа к первым трём не соберётся читаемая коллекция.
- `BACKEND_IMAGE` — оставьте пустым, если собираете образ на VPS; для деплоя из CI
  укажите образ из GHCR (раздел 5).

### 1.3. Запуск

```bash
docker compose --profile prod up -d --build
```

Профиль `prod` добавляет к backend/postgres/minio сервис **caddy** (авто-TLS).
Миграции EF применяются на старте бэкенда. Проверка:

```bash
curl -fsS https://<API_DOMAIN>/health          # 200
curl -fsS https://<API_DOMAIN>/api/catalog/books?pageSize=1   # список книг
```

Сразу после первого запуска список пуст: читаемую коллекцию собирает фоновый импорт
(несколько минут, около 1 900 книг). Прогресс виден в логах —
`docker compose logs -f backend | grep "Readable collection"`; книги появляются в каталоге по
мере импорта. Импорт повторяется раз в неделю и после перезапуска продолжает с места остановки.

Первый запрос по HTTPS может занять секунды — Caddy получает сертификат.

> **Volumes критичны (§11.2):** `pg-data` (пользователи/полки/прогресс) и
> `minio-data` (файлы книг). Не удаляйте их при пересоздании контейнеров.

---

## 2. Фронтенд на GitHub Pages

Пайплайн уже описан в `.github/workflows/frontend.yml` (prettier → тесты → build → deploy).

1. **Settings → Pages → Build and deployment → Source: GitHub Actions.**
2. **Settings → Secrets and variables → Actions → Variables** → создайте переменную
   `API_URL` = `https://<API_DOMAIN>/api`. Она зашивается в сборку как `VITE_API_URL`.
3. Запушьте в `main`/`master` — workflow соберёт с `VITE_BASE=/yarphoenix-library/`,
   создаст `404.html` (SPA-фолбэк, §11.3) и задеплоит на Pages.
4. Сайт: `https://<username>.github.io/yarphoenix-library/`.

---

## 3. Проверка связки (§11.4)

С открытого сайта на Pages:

- **CORS** — зарегистрируйтесь и войдите (демо-учётка `reader@example.com` создаётся только
  в окружении Development, в проде её нет). Ошибок CORS в консоли быть не должно — бэкенд
  разрешает `FRONTEND_ORIGIN`.
- **Mixed content** — все запросы идут на `https://<API_DOMAIN>` (не http),
  иначе браузер их заблокирует.
- **Аватар** — загрузите в настройках; картинка должна открываться по
  `https://<API_DOMAIN>/avatars/...` (Caddy → MinIO).
- **Книга коллекции** — откройте любую книгу с главной (напр. «Pride and Prejudice»), нажмите
  «Читать»: бэкенд за несколько секунд скачает EPUB из Standard Ebooks или Project Gutenberg
  в MinIO (нужен доступ к этим сайтам — при блокировке задайте `EGRESS_PROXY`). Если источник
  недоступен, ответ — 503, и повторное «Читать» попробует снова.

---

## 4. Обновление

```bash
git pull
docker compose --profile prod up -d --build
```

Это ручной путь со сборкой образа прямо на VPS. Фронтенд деплоится автоматически на каждый
push в main/master.

---

## 5. Деплой бэкенда из CI

`.github/workflows/backend.yml` на каждый push в `main`/`master` публикует образ в GHCR:
`ghcr.io/<owner>/<repo>:latest` и `:<sha коммита>`. Сам деплой запускается только вручную:
**Actions → backend → Run workflow** (из основной ветки). Job `deploy` заходит на VPS по SSH и
выполняет `git pull --ff-only`, `docker compose --profile prod pull backend`,
`docker compose --profile prod up -d`.

### 5.1. Разовая настройка VPS

1. В `.env` на VPS задайте образ: `BACKEND_IMAGE=ghcr.io/<owner>/<repo>:latest`
   (в нижнем регистре). Без этой переменной compose собирает образ локально, а `pull` не сработает.
2. Доступ к образу. Если пакет в GHCR приватный — один раз выполните на VPS
   `docker login ghcr.io` с Personal Access Token (право `read:packages`). Либо сделайте
   пакет публичным: GitHub → Packages → пакет → Package settings → Change visibility.
3. Заведите пользователя для деплоя с правом запускать Docker и SSH-ключ только для этой
   задачи: `ssh-keygen -t ed25519 -f deploy_key -N ""`; публичную часть добавьте в
   `~/.ssh/authorized_keys` этого пользователя на VPS.
4. Если репозиторий приватный, `git pull` на VPS должен работать без пароля (deploy key
   репозитория с правом чтения).

### 5.2. Секреты и переменные GitHub

**Settings → Environments → New environment → `production`** (там же можно включить
обязательное подтверждение деплоя — Required reviewers). В этом окружении задайте:

| Тип | Имя | Значение |
|---|---|---|
| Secret | `DEPLOY_HOST` | IP или домен VPS |
| Secret | `DEPLOY_USER` | пользователь для деплоя |
| Secret | `DEPLOY_SSH_KEY` | приватный ключ `deploy_key` целиком |
| Secret | `DEPLOY_KNOWN_HOSTS` | вывод `ssh-keyscan -t ed25519 <DEPLOY_HOST>` |
| Variable | `DEPLOY_PATH` | каталог с клоном репозитория на VPS, например `/opt/yarphoenix-api` |

`DEPLOY_KNOWN_HOSTS` обязателен: workflow подключается с `StrictHostKeyChecking=yes` и не
доверяет первому ответу сервера. Сверьте отпечаток с тем, что показывает сам VPS
(`ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub`).

### 5.3. Откат

Каждый образ помечен SHA коммита. Чтобы вернуться на предыдущую версию, укажите в `.env` на
VPS `BACKEND_IMAGE=ghcr.io/<owner>/<repo>:<sha>` и выполните
`docker compose --profile prod up -d`. Миграции EF назад не откатываются — если релиз менял
схему, сначала проверьте совместимость старого кода с новой схемой.
