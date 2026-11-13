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
- `EGRESS_PROXY` — если провайдер VPS блокирует archive.org / api.anthropic.com,
  укажите egress-прокси `http://user:pass@host:port` (§5.1.2, §8).

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

- **CORS** — залогиньтесь (`reader@example.com` / `password123`, если засеян dev-юзер,
  или зарегистрируйтесь). Ошибок CORS в консоли быть не должно — бэкенд разрешает
  `FRONTEND_ORIGIN`.
- **Mixed content** — все запросы идут на `https://<API_DOMAIN>` (не http),
  иначе браузер их заблокирует.
- **Аватар** — загрузите в настройках; картинка должна открываться по
  `https://<API_DOMAIN>/avatars/...` (Caddy → MinIO).
- **PD-книга** — откройте классику (напр. Достоевский), нажмите «Читать»: бэкенд
  скачает EPUB с Internet Archive в MinIO (нужен доступ к archive.org — при блокировке
  задайте `EGRESS_PROXY`).

---

## 4. Обновление

```bash
git pull
docker compose --profile prod up -d --build
```

Автодеплой бэкенда из CI (SSH) — сознательно отложен (§9.2): ручного деплоя для MVP
достаточно. Фронтенд деплоится автоматически на каждый push в main/master.
