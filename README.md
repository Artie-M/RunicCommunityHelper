# RunicCommunityHelper

Web-приложение и Discord-бот для управления доступом к Google-таблицам и Google-документам на основе ролей Discord-сервера.

## Как это работает

1. Администратор сервера командой `/create-email-link-form channel:#канал` публикует сообщение с кнопкой **«Обновить доступ»**.
<p align="center"><img width="504" height="164" alt="communityHelperMessageButton" src="https://github.com/user-attachments/assets/c71b7f7a-d942-4e36-8f20-0fb1fe417b5f" /></p>

2. Пользователь нажимает кнопку и в открывшейся форме указывает почту своего Google-аккаунта.
<p align="center"><img width="465" height="312" alt="communityHelperMenu" src="https://github.com/user-attachments/assets/6405c4e4-2e88-44bd-8b4f-05fd3247a493" /></p>

3. Бот сохраняет почту и проверяет роли пользователя. Для каждого документа из таблицы `GoogleDocuments`, у которого `RequiredRole` совпадает с одной из ролей пользователя, сервисный аккаунт Google выдаёт пользователю доступ к файлу.
4. Когда у пользователя снимают роль, бот отзывает доступ к документам, на которые у него не осталось других подходящих ролей.

После получения новой роли пользователю нужно снова нажать кнопку, чтобы получить доступ к новым документам.

## Стек

- **Платформа:** .NET 8.0 (.NET SDK 8.0)
- **Web API:** ASP.NET Core 8.0 Minimal API
- **Аутентификация:** JWT Bearer 8.0.26
- **ORM:** Entity Framework Core 9.0.15
- **База данных:** SQLite (Microsoft.EntityFrameworkCore.Sqlite 9.0.15)
- **Discord-бот:** Discord.Net 3.19.1, Discord.Addons.Hosting 6.1.0, Fergun.Interactive 1.7.7
- **Google API:** Google.Apis.Drive.v3 1.73.0
- **Логирование:** Serilog 4.3.1
- **Панель администратора:** React 18.3, React Router 6.28, TypeScript 5.6, Vite 5.4 (Node.js 22)
- **Контейнеризация:** Docker, Docker Compose

## Структура

```
src/
├── RunicCommunityHelper.App              # точка входа, REST API (EndpointManagers)
├── RunicCommunityHelper.Web              # панель администратора
├── RunicCommunityHelper.Discord          # бот: команды, модальные окна, обработчик событий
├── RunicCommunityHelper.Implementations  # БД, Google Drive, JWT, обработка ошибок
└── RunicCommunityHelper.Common           # сущности, интерфейсы сервисов, константы
```

## Развёртывание и запуск

Для развёртывания потребуются Git и Docker с плагином Docker Compose. Приложение собирается внутри контейнера, устанавливать .NET SDK не нужно.

### 1. Клонирование репозитория

```bash
git clone https://github.com/Artie-M/RunicCommunityHelper.git
cd RunicCommunityHelper
```

### 2. Настройка Discord-бота

1. Создайте приложение и бота в [Discord Developer Portal](https://discord.com/developers/applications), скопируйте токен.
2. Включите Privileged Intents: **Server Members** и **Message Content**.
3. Пригласите бота на сервер с правами `bot` и `applications.commands`.
4. Укажите в `src/RunicCommunityHelper.App/appsettings.json` ID сервера (`Discord:DevGuild`), на котором регистрируются команды, и ID канала для отчётов об ошибках (`Discord:ExceptionChannel`).

### 3. Настройка сервисного аккаунта Google

1. Создайте сервисный аккаунт в Google Cloud Console и включите Google Drive API.
2. Скачайте JSON-ключ и сохраните его в корне репозитория как `google-key.json`.
3. Откройте каждый нужный документ для почты сервисного аккаунта (поле `client_email` в JSON-ключе, вида `...@...iam.gserviceaccount.com`) с ролью **«Редактор»**. Владелец документа не должен запрещать редакторам изменять настройки доступа.

### 4. Настройка переменных окружения

Создайте файл `.env` в корне репозитория:

```env
DISCORD_TOKEN=токен-бота
JWT_SECRET=длинная-случайная-строка-не-короче-32-символов
ADMIN_PASSWORD=пароль-администратора-API
```

| Переменная | Назначение                                                                              |
|---|-----------------------------------------------------------------------------------------|
| `DISCORD_TOKEN` | токен Discord-бота                                                                      |
| `JWT_SECRET` | ключ подписи JWT-токенов API                                                            |
| `ADMIN_PASSWORD` | пароль пользователя `admin`, создаваемого при первом запуске                            |
| `GOOGLE_CREDENTIALS_PATH` | путь к JSON-ключу Google (по умолчанию `./google-key.json`) |

### 5. Запуск

Выполните в корне репозитория:

```bash
docker compose up --build -d
```

Приложение будет доступно на `http://localhost:5024`, база SQLite сохраняется в `./data/core.db`.

## Панель администратора

Откройте `http://localhost:5024` и войдите под учётной записью администратора (при первом запуске создаётся пользователь `admin` с паролем из `ADMIN_PASSWORD`). Сессия действует 30 минут.
<p align="center"><img width="909" height="481" alt="communityHelperUI" src="https://github.com/user-attachments/assets/4e6adbaa-4f35-4e7c-b73d-b3f0e7309e03" /></p>

- **Документы** — какие роли Discord дают доступ к каким документам: добавление, изменение ID документа и ID роли, удаление. В поле ID документа можно вставить полную ссылку на документ.
- **Пользователи** — список пользователей, привязавших почту, поиск и изменение почты.

Изменения в панели меняют только записи в базе. Новый документ пользователи получат при следующем нажатии кнопки «Обновить доступ» в Discord; при удалении документа или смене почты в панели уже выданный доступ не отзывается.

## REST API

Все эндпоинты, кроме входа, требуют заголовок `Authorization: Bearer <token>` с ролью `Admin`. Токен действует 30 минут. Discord ID (`discordId`, `requiredRole`) передаются строками: они не помещаются в число JavaScript без потери точности (числа в запросах тоже принимаются).

| Метод | Путь | Описание |
|---|---|---|
| `POST` | `/api/auth/login` | вход, тело `{ "username": "...", "password": "..." }`, ответ `{ "token": "..." }` |
| `GET` | `/api/googledocuments` | список документов |
| `GET` | `/api/googledocuments/{id}` | документ по ID |
| `POST` | `/api/googledocuments` | добавить документ `{ "fileId": "...", "requiredRole": "936422104788791337" }` |
| `PUT` | `/api/googledocuments/{id}` | изменить документ |
| `DELETE` | `/api/googledocuments/{id}` | удалить документ |
| `GET` | `/api/discordusers` | список пользователей с привязанной почтой |
| `GET` | `/api/discordusers/{id}` | пользователь по ID |
| `POST` | `/api/discordusers` | добавить пользователя `{ "discordId": "283279185436000257", "email": "..." }` |
| `PUT` | `/api/discordusers/{id}` | изменить пользователя |
| `DELETE` | `/api/discordusers/{id}` | удалить пользователя |

`fileId` — идентификатор файла из его ссылки (`https://docs.google.com/spreadsheets/d/<fileId>/edit`), `requiredRole` — ID роли Discord.

## База данных

| Таблица | Содержимое |
|---|---|
| `GoogleDocuments` | `FileId` документа и `RequiredRole` — роль, дающая к нему доступ (один файл может быть привязан к нескольким ролям) |
| `DiscordUsers` | соответствие Discord ID и почты Google |
| `Staff` | учётные записи администраторов API |
| `Exceptions` | журнал внутренних ошибок бота |
