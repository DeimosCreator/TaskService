# Task Service API

Микросервис для управления задачами, написанный на **.NET 9** (Minimal API) с использованием **PostgreSQL**, **EF Core** и **RabbitMQ**.

## Особенности реализации (Trade-offs & Architecture)

В рамках ограничения по времени (1–1.5 часа) были приняты следующие прагматичные решения (KISS):

1. **Архитектура**: Выбран один проект с логическим разделением по папкам (`Models`, `Endpoints`, `Services`, `Data`) вместо Clean Architecture/CQRS, чтобы избежать оверинжиниринга для простого CRUD-прототипа.

2. **Конкурентность (Concurrency)**: Обработка одновременных вызовов `PUT /tasks/{id}/complete` реализована через механизм **Optimistic Locking** с использованием `uint Version` (`xmin` в PostgreSQL, маппится через `.IsRowVersion()`). В случае конфликта второй запрос корректно получает `409 Conflict`.

3. **Паттерн работы с брокером**:
   - Сохранение в БД происходит **строго до** публикации в RabbitMQ.
   - Отправка события завернута в `try-catch`. Если брокер недоступен, ошибка логируется, но клиент получает успешный ответ (Fail Silently), как и требовалось по заданию.
   - *Примечание: в реальном production-коде здесь был бы использован паттерн Transactional Outbox (через MassTransit или Quartz), чтобы гарантировать доставку события в случае падения брокера.*

4. **Graceful Shutdown**: Сервис публикации сообщений реализует `IAsyncDisposable` для корректного закрытия каналов и соединений к RabbitMQ при получении `SIGTERM`.

## Как запустить проект

### Шаг 1. Настройка окружения

Убедитесь, что у вас есть файл `.env` в корне проекта (или переименуйте `.env.example`, если он есть) со следующими значениями:

```env
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
```

> Убедитесь, что строка подключения `ConnectionStrings` в файле `TaskService/appsettings.json` совпадает с учетными данными из вашего файла `.env`.

### Шаг 2. Поднятие инфраструктуры

Поднимите PostgreSQL и RabbitMQ через Docker:

```bash
docker compose up -d db rabbitmq
```

База данных и RabbitMQ будут доступны на стандартных портах.

Панель управления RabbitMQ: http://localhost:15672

### Шаг 3. Запуск API

Миграции применяются автоматически при старте приложения:

```bash
dotnet run --project TaskService/TaskService.csproj
```

Swagger UI будет доступен по адресу: http://localhost:8080/swagger

## Как запустить тесты

В проекте реализован интеграционный xUnit-тест с использованием `WebApplicationFactory` и заменой реального брокера на `Moq`.

Для прохождения теста необходимо, чтобы контейнер с БД был запущен. RabbitMQ не требуется, так как он заменён мок-объектом.

```bash
dotnet test
```

## Что реализовано

- [x] CRUD через Minimal API.
- [x] Валидация входных данных (`Title`).
- [x] Отправка события `TaskCompletedEvent` в RabbitMQ в формате JSON (Exchange `task.events`).
- [x] Защита от race condition (`409 Conflict`).
- [x] Интеграционный тест основного бизнес-процесса.
- [x] Graceful shutdown для соединений с брокером.
