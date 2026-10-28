# Практическая работа: Система продажи билетов в кинотеатр (ADO.NET)

Консольное приложение на C# (.NET 8) для работы с базой данных кинотеатра на PostgreSQL с использованием библиотеки **Npgsql** и чистого **ADO.NET** (без использования ORM).

---

## 📋 Реализованные задания

| № | Задание | Реализация в проекте |
|---|---|---|
| **1** | **Подключение к БД** | Проверка подключения методом `SELECT CURRENT_TIMESTAMP`, обработка ошибок, освобождение ресурсов через `await using` (`NpgsqlConnection`, `NpgsqlCommand`). |
| **2** | **Просмотр фильмов** | Вывод всех фильмов и параметризованный поиск по подстроке названия (`ILIKE @SearchTerm`) с `NpgsqlDataReader`. |
| **3** | **Расписание и свободные места** | Расчёт свободных мест на уровне SQL (`h.seat_count - COUNT(t.ticket_id)` с фильтром по `status = 'Purchased'`). Фильтрация по дате/диапазону дат через параметры. |
| **4** | **Поиск клиента и билетов** | Поиск по email, вывод детальной информации о клиенте и всех его билетах (фильм, дата, зал, место, цена, статус). |
| **5** | **Регистрация клиента** | Параметризованный `INSERT ... RETURNING customer_id`. Валидация полей, обработка дубликата email (код ошибки PostgreSQL `23505`). |
| **6** | **Покупка билета** | Выполнение в единой транзакции (`BeginTransactionAsync`). Проверка сеанса, клиента, корректности места в зале и занятости. Защита от race condition через уникальный индекс `ux_tickets_active_seat` (отлов ошибки `23505`), `Commit` / `Rollback`. |
| **7** | **Отмена билета** | Смена статуса на `'Cancelled'` без физического удаления. Проверка `ExecuteNonQueryAsync()` на количество изменённых строк. Освобождение места. |
| **8** | **Отчёт по сеансам и выручке** | Агрегация SQL (`COUNT`, `SUM`, `GROUP BY`) с учётом только `'Purchased'` билетов. Включение сеансов без продаж (`LEFT JOIN`, `COALESCE`). Тип `decimal` в C#. Топ-3 фильма по продажам. |

---

## 🛠️ Структура проекта

```text
CinemaAdoNet/
├── CinemaAdoNet.sln                 # Файл решения Visual Studio / Rider
├── cinema_training.sql             # SQL-скрипт инициализации схемы и тестовых данных
├── README.md                       # Документация и инструкция по запуску
├── .gitignore                      # Игнорирование bin, obj, .vs и временных файлов
└── CinemaAdoNet/
    ├── CinemaAdoNet.csproj          # Файл проекта (.NET 8, Npgsql)
    ├── Program.cs                   # Точка входа, настройка кодировки UTF-8
    ├── appsettings.json             # Конфигурация строки подключения к БД
    ├── Configuration/
    │   └── AppConfig.cs             # Чтение настроек (файл, переменные окружения, args)
    ├── Models/                      # Доменные модели и DTO
    │   ├── Film.cs
    │   ├── Customer.cs
    │   ├── ScreeningScheduleItem.cs
    │   ├── CustomerTicketItem.cs
    │   ├── PurchaseResult.cs
    │   ├── ScreeningReportItem.cs
    │   └── TopFilmReportItem.cs
    ├── Data/
    │   └── CinemaRepository.cs      # Слой доступа к данным: все SQL-запросы на ADO.NET
    └── UI/
        ├── ConsoleHelper.cs         # Вспомогательные методы вывода и валидации ввода
        └── MenuManager.cs           # Интерактивное консольное меню с циклом обработки
```

---

## 🚀 Инструкция по запуску

### 1. Подготовка базы данных в PostgreSQL
1. Убедитесь, что сервер PostgreSQL запущен.
2. Создайте пустую базу данных `cinema_training`:
   ```sql
   CREATE DATABASE cinema_training;
   ```
3. Выполните скрипт `cinema_training.sql` в созданной базе через pgAdmin, DBeaver или утилиту `psql`:
   ```bash
   psql -U postgres -d cinema_training -f cinema_training.sql
   ```

### 2. Настройка подключения
Строка подключения по умолчанию задана в файле `CinemaAdoNet/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "CinemaDatabase": "Host=localhost;Port=5432;Database=cinema_training;Username=postgres;Password=ВАШ_ПАРОЛЬ"
  }
}
```
*Укажите свой реальный пароль от пользователя `postgres`.*  
Также строку подключения можно изменить прямо в меню запущенного приложения (пункт 9) либо передать аргументом при запуске.

### 3. Сборка и запуск приложения
Перейдите в папку с проектом:
```bash
cd CinemaAdoNet/CinemaAdoNet
dotnet restore
dotnet run
```
Либо откройте решение `CinemaAdoNet.sln` в Visual Studio, JetBrains Rider или VS Code и запустите проект.

---

## 📦 Сдача работы на GitHub / GitLab
1. Инициализируйте репозиторий в корне проекта:
   ```bash
   git init
   git add .
   git commit -m "feat: complete cinema ticketing ADO.NET console application"
   ```
2. Создайте репозиторий на GitHub или GitLab и отправьте код:
   ```bash
   git remote add origin https://github.com/ВАШ_ЛОГИН/CinemaAdoNet.git
   git branch -M main
   git push -u origin main
   ```
