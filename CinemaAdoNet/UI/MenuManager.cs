using CinemaAdoNet.Data;
using CinemaAdoNet.Models;
using Npgsql;

namespace CinemaAdoNet.UI;

public class MenuManager
{
    private CinemaRepository _repository;
    private string _connectionString;

    public MenuManager(string connectionString)
    {
        _connectionString = connectionString;
        _repository = new CinemaRepository(connectionString);
    }

    public async Task RunAsync()
    {
        // Первоначальная проверка подключения при старте (Задание 1)
        await CheckInitialConnectionAsync();

        var exit = false;
        while (!exit)
        {
            ShowMainMenu();
            Console.Write("Выберите пункт меню (0-9): ");
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        await TestConnectionAsync();
                        break;
                    case "2":
                        await ViewFilmsMenuAsync();
                        break;
                    case "3":
                        await ViewScheduleMenuAsync();
                        break;
                    case "4":
                        await SearchCustomerAndTicketsAsync();
                        break;
                    case "5":
                        await RegisterCustomerAsync();
                        break;
                    case "6":
                        await PurchaseTicketAsync();
                        break;
                    case "7":
                        await CancelTicketAsync();
                        break;
                    case "8":
                        await ShowReportsAsync();
                        break;
                    case "9":
                        ChangeConnectionString();
                        break;
                    case "0":
                        exit = true;
                        ConsoleHelper.PrintSuccess("Завершение работы программы. До свидания!");
                        break;
                    default:
                        ConsoleHelper.PrintWarning("Неверный выбор. Пожалуйста, укажите цифру от 0 до 9.");
                        break;
                }
            }
            catch (PostgresException pex)
            {
                ConsoleHelper.PrintError($"Ошибка базы данных PostgreSQL (код {pex.SqlState}): {pex.MessageText}");
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintError($"Произошла ошибка: {ex.Message}");
            }

            if (!exit)
            {
                Console.WriteLine();
                Console.Write("Нажмите Enter для продолжения...");
                Console.ReadLine();
            }
        }
    }

    private void ShowMainMenu()
    {
        Console.Clear();
        ConsoleHelper.PrintHeader("Кинотеатр: Система продажи билетов (ADO.NET)");
        Console.WriteLine(" 1. Задание 1: Проверить подключение к базе данных");
        Console.WriteLine(" 2. Задание 2: Каталог фильмов (просмотр и поиск по названию)");
        Console.WriteLine(" 3. Задание 3: Расписание сеансов и свободные места (с фильтром)");
        Console.WriteLine(" 4. Задание 4: Поиск клиента и его билетов по Email");
        Console.WriteLine(" 5. Задание 5: Регистрация нового клиента");
        Console.WriteLine(" 6. Задание 6: Оформление покупки билета (транзакция)");
        Console.WriteLine(" 7. Задание 7: Отмена купленного билета");
        Console.WriteLine(" 8. Задание 8: Отчёт по сеансам и выручке + Топ-3 фильма");
        Console.WriteLine(" 9. Настройки: Изменить строку подключения к БД");
        Console.WriteLine(" 0. Выход");
        Console.WriteLine(new string('-', 70));
    }

    private async Task CheckInitialConnectionAsync()
    {
        ConsoleHelper.PrintInfo("Проверка подключения к PostgreSQL...");
        try
        {
            var serverTime = await _repository.TestConnectionAsync();
            ConsoleHelper.PrintSuccess($"Подключение к БД успешно установлено! Время на сервере: {serverTime:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError($"Не удалось подключиться к базе данных: {ex.Message}");
            ConsoleHelper.PrintWarning("Проверьте параметры в appsettings.json или настройте строку подключения через пункт 9.");
        }
        Console.WriteLine("Нажмите Enter для перехода в главное меню...");
        Console.ReadLine();
    }

    private async Task TestConnectionAsync()
    {
        ConsoleHelper.PrintHeader("Задание 1: Проверка подключения к PostgreSQL");
        try
        {
            var serverTime = await _repository.TestConnectionAsync();
            ConsoleHelper.PrintSuccess($"Соединение активно и работает корректно.");
            Console.WriteLine($"Тестовый запрос 'SELECT CURRENT_TIMESTAMP' вернул: {serverTime:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.PrintError($"Ошибка подключения: {ex.Message}");
        }
    }

    private async Task ViewFilmsMenuAsync()
    {
        ConsoleHelper.PrintHeader("Задание 2: Просмотр и поиск фильмов");
        Console.WriteLine("1. Показать все фильмы");
        Console.WriteLine("2. Найти фильм по части названия (ILIKE)");
        Console.Write("Выберите вариант (1-2): ");
        var opt = Console.ReadLine()?.Trim();

        List<Film> films;
        if (opt == "2")
        {
            var term = ConsoleHelper.ReadNonEmptyString("Введите строку для поиска");
            films = await _repository.SearchFilmsByTitleAsync(term);
            Console.WriteLine($"\nРезультаты поиска по запросу '{term}':");
        }
        else
        {
            films = await _repository.GetAllFilmsAsync();
            Console.WriteLine("\nПолный список фильмов в прокате:");
        }

        if (films.Count == 0)
        {
            ConsoleHelper.PrintWarning("Фильмы не найдены.");
            return;
        }

        PrintFilmsTable(films);
    }

    private void PrintFilmsTable(List<Film> films)
    {
        Console.WriteLine(new string('-', 95));
        Console.WriteLine($"| {"ID",-4} | {"Название",-28} | {"Жанр",-14} | {"Длит.",-7} | {"Рейт.",-5} | {"Год",-4} |");
        Console.WriteLine(new string('-', 95));
        foreach (var f in films)
        {
            Console.WriteLine($"| {f.FilmId,-4} | {Truncate(f.Title, 28),-28} | {Truncate(f.Genre, 14),-14} | {f.DurationMinutes + " мин",-7} | {f.AgeRating + "+",-5} | {f.ReleaseYear,-4} |");
            if (!string.IsNullOrEmpty(f.Description))
            {
                Console.WriteLine($"  └─ Описание: {f.Description}");
            }
        }
        Console.WriteLine(new string('-', 95));
        Console.WriteLine($"Всего фильмов: {films.Count}");
    }

    private async Task ViewScheduleMenuAsync()
    {
        ConsoleHelper.PrintHeader("Задание 3: Расписание сеансов и свободные места");
        Console.WriteLine("1. Показать все сеансы");
        Console.WriteLine("2. Фильтровать по конкретной дате");
        Console.WriteLine("3. Фильтровать по диапазону дат");
        Console.Write("Выберите вариант (1-3): ");
        var opt = Console.ReadLine()?.Trim();

        DateTime? start = null;
        DateTime? end = null;

        if (opt == "2")
        {
            var d = ConsoleHelper.ReadOptionalDate("Введите дату сеанса");
            if (d.HasValue)
            {
                start = d.Value;
                end = d.Value.AddDays(1).AddTicks(-1);
            }
        }
        else if (opt == "3")
        {
            start = ConsoleHelper.ReadOptionalDate("Введите начальную дату");
            var dEnd = ConsoleHelper.ReadOptionalDate("Введите конечную дату");
            if (dEnd.HasValue)
            {
                end = dEnd.Value.AddDays(1).AddTicks(-1);
            }
        }

        var screenings = await _repository.GetScreeningsAsync(start, end);

        if (screenings.Count == 0)
        {
            ConsoleHelper.PrintWarning("По выбранным критериям сеансов не найдено.");
            return;
        }

        Console.WriteLine(new string('-', 100));
        Console.WriteLine($"| {"ID",-4} | {"Фильм",-24} | {"Жанр",-12} | {"Начало сеанса",-16} | {"Зал",-7} | {"Цена",-8} | {"Мест",-5} | {"Свободно",-8} |");
        Console.WriteLine(new string('-', 100));

        foreach (var s in screenings)
        {
            Console.WriteLine($"| {s.ScreeningId,-4} | {Truncate(s.FilmTitle, 24),-24} | {Truncate(s.Genre, 12),-12} | {s.StartsAt:yyyy-MM-dd HH:mm} | {s.HallName,-7} | {s.TicketPrice,6:F2} ₽ | {s.TotalSeats,-5} | {s.AvailableSeats,-8} |");
        }
        Console.WriteLine(new string('-', 100));
        Console.WriteLine($"Всего сеансов в расписании: {screenings.Count}");
    }

    private async Task SearchCustomerAndTicketsAsync()
    {
        ConsoleHelper.PrintHeader("Задание 4: Поиск клиента и его билетов");
        var email = ConsoleHelper.ReadNonEmptyString("Введите адрес электронной почты (email)");

        var customer = await _repository.GetCustomerByEmailAsync(email);
        if (customer == null)
        {
            ConsoleHelper.PrintWarning($"Клиент с адресом электронной почты '{email}' не найден.");
            return;
        }

        ConsoleHelper.PrintSuccess($"Клиент найден! ID: {customer.CustomerId}");
        Console.WriteLine($"Имя: {customer.FirstName} {customer.LastName}");
        Console.WriteLine($"Email: {customer.Email}");
        Console.WriteLine($"Телефон: {customer.Phone ?? "не указан"}");
        Console.WriteLine($"Дата регистрации: {customer.RegisteredAt:yyyy-MM-dd HH:mm}");

        var tickets = await _repository.GetCustomerTicketsAsync(customer.CustomerId);
        if (tickets.Count == 0)
        {
            ConsoleHelper.PrintInfo("У данного клиента пока нет оформленных билетов.");
            return;
        }

        Console.WriteLine("\nИстория билетов клиента:");
        Console.WriteLine(new string('-', 95));
        Console.WriteLine($"| {"Билет",-6} | {"Фильм",-24} | {"Сеанс",-16} | {"Зал",-7} | {"Место",-5} | {"Цена",-8} | {"Статус",-11} |");
        Console.WriteLine(new string('-', 95));

        foreach (var t in tickets)
        {
            Console.WriteLine($"| {t.TicketId,-6} | {Truncate(t.FilmTitle, 24),-24} | {t.StartsAt:yyyy-MM-dd HH:mm} | {t.HallName,-7} | {t.SeatNumber,-5} | {t.Price,6:F2} ₽ | {t.Status,-11} |");
        }
        Console.WriteLine(new string('-', 95));
    }

    private async Task RegisterCustomerAsync()
    {
        ConsoleHelper.PrintHeader("Задание 5: Регистрация нового клиента");
        var firstName = ConsoleHelper.ReadNonEmptyString("Введите имя");
        var lastName = ConsoleHelper.ReadNonEmptyString("Введите фамилию");
        var email = ConsoleHelper.ReadNonEmptyString("Введите email");
        
        Console.Write("Введите номер телефона (или нажмите Enter): ");
        var phone = Console.ReadLine()?.Trim();

        try
        {
            var customerId = await _repository.RegisterCustomerAsync(firstName, lastName, email, phone);
            ConsoleHelper.PrintSuccess($"Клиент успешно зарегистрирован!");
            Console.WriteLine($"Назначен ID клиента: {customerId}");
            Console.WriteLine($"ФИО: {firstName} {lastName}");
            Console.WriteLine($"Email: {email}");
            if (!string.IsNullOrEmpty(phone))
            {
                Console.WriteLine($"Телефон: {phone}");
            }
        }
        catch (InvalidOperationException ex)
        {
            ConsoleHelper.PrintWarning(ex.Message);
        }
    }

    private async Task PurchaseTicketAsync()
    {
        ConsoleHelper.PrintHeader("Задание 6: Оформление покупки билета (Транзакция)");
        ConsoleHelper.PrintInfo("Операция выполняется в транзакции с проверкой сеанса, клиента, зала и занятости места.");

        var screeningId = ConsoleHelper.ReadInt("Введите ID сеанса");
        var customerId = ConsoleHelper.ReadInt("Введите ID клиента");
        var seatNumber = ConsoleHelper.ReadInt("Введите номер места");

        Console.WriteLine("\nВыполняется оформление билета...");
        var result = await _repository.PurchaseTicketAsync(screeningId, customerId, seatNumber);

        if (result.Success)
        {
            ConsoleHelper.PrintSuccess("Покупка успешно завершена!");
            Console.WriteLine(new string('-', 55));
            Console.WriteLine($" Номер билета:    №{result.TicketId}");
            Console.WriteLine($" Фильм:           {result.FilmTitle}");
            Console.WriteLine($" Зал:             {result.HallName}");
            Console.WriteLine($" Дата и время:    {result.StartsAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine($" Номер места:     {result.SeatNumber}");
            Console.WriteLine($" Стоимость:       {result.Price:F2} ₽");
            Console.WriteLine(new string('-', 55));
        }
        else
        {
            ConsoleHelper.PrintError(result.Message);
        }
    }

    private async Task CancelTicketAsync()
    {
        ConsoleHelper.PrintHeader("Задание 7: Отмена билета");
        var ticketId = ConsoleHelper.ReadInt("Введите номер билета для отмены");

        var (success, message) = await _repository.CancelTicketAsync(ticketId);
        if (success)
        {
            ConsoleHelper.PrintSuccess(message);
        }
        else
        {
            ConsoleHelper.PrintWarning(message);
        }
    }

    private async Task ShowReportsAsync()
    {
        ConsoleHelper.PrintHeader("Задание 8: Отчёты по сеансам и топ фильмов");

        Console.WriteLine("1. Сводный отчёт по каждому сеансу (продажи и выручка):");
        var reports = await _repository.GetScreeningsReportAsync();

        if (reports.Count == 0)
        {
            ConsoleHelper.PrintWarning("Нет данных о сеансах.");
        }
        else
        {
            Console.WriteLine(new string('-', 88));
            Console.WriteLine($"| {"ID",-4} | {"Фильм",-24} | {"Сеанс",-16} | {"Зал",-7} | {"Билетов",-8} | {"Выручка",-12} |");
            Console.WriteLine(new string('-', 88));

            int totalTickets = 0;
            decimal grandTotal = 0;

            foreach (var r in reports)
            {
                Console.WriteLine($"| {r.ScreeningId,-4} | {Truncate(r.FilmTitle, 24),-24} | {r.StartsAt:yyyy-MM-dd HH:mm} | {r.HallName,-7} | {r.SoldTicketsCount,-8} | {r.TotalRevenue,10:F2} ₽ |");
                totalTickets += r.SoldTicketsCount;
                grandTotal += r.TotalRevenue;
            }

            Console.WriteLine(new string('-', 88));
            Console.WriteLine($"ИТОГО по всем сеансам: {totalTickets} билетов на сумму {grandTotal:F2} ₽\n");
        }

        Console.WriteLine("2. Топ-3 фильма по количеству проданных билетов:");
        var topFilms = await _repository.GetTopFilmsReportAsync(3);

        if (topFilms.Count == 0)
        {
            ConsoleHelper.PrintWarning("Нет данных о продажах фильмов.");
        }
        else
        {
            Console.WriteLine(new string('-', 78));
            Console.WriteLine($"| {"#",-2} | {"Фильм",-28} | {"Жанр",-14} | {"Билетов",-8} | {"Выручка",-12} |");
            Console.WriteLine(new string('-', 78));

            int rank = 1;
            foreach (var top in topFilms)
            {
                Console.WriteLine($"| {rank++,-2} | {Truncate(top.Title, 28),-28} | {Truncate(top.Genre, 14),-14} | {top.SoldTicketsCount,-8} | {top.TotalRevenue,10:F2} ₽ |");
            }
            Console.WriteLine(new string('-', 78));
        }
    }

    private void ChangeConnectionString()
    {
        ConsoleHelper.PrintHeader("Настройки строки подключения к базе данных");
        Console.WriteLine($"Текущая строка подключения:\n{_connectionString}\n");
        Console.Write("Введите новую строку подключения (или Enter для отмены): ");
        var newConn = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(newConn))
        {
            _connectionString = newConn;
            _repository = new CinemaRepository(newConn);
            ConsoleHelper.PrintSuccess("Строка подключения обновлена!");
        }
        else
        {
            ConsoleHelper.PrintInfo("Изменения отменены.");
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
    }
}
