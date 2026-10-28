using System.Data;
using System.Text;
using CinemaAdoNet.Models;
using Npgsql;

namespace CinemaAdoNet.Data;

public class CinemaRepository
{
    private readonly string _connectionString;

    public CinemaRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Задание 1: Проверка подключения к базе данных.
    /// Выполняет тестовый скалярный запрос SELECT CURRENT_TIMESTAMP.
    /// </summary>
    public async Task<DateTime> TestConnectionAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = "SELECT CURRENT_TIMESTAMP;";
        await using var command = new NpgsqlCommand(sql, connection);

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToDateTime(result) : DateTime.Now;
    }

    /// <summary>
    /// Задание 2: Получение полного списка фильмов.
    /// </summary>
    public async Task<List<Film>> GetAllFilmsAsync()
    {
        const string sql = @"
            SELECT film_id, title, genre, duration_minutes, age_rating, release_year, description
            FROM films
            ORDER BY film_id;";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var films = new List<Film>();
        while (await reader.ReadAsync())
        {
            films.Add(MapFilm(reader));
        }

        return films;
    }

    /// <summary>
    /// Задание 2: Поиск фильмов по части названия через оператор ILIKE с передачей параметра.
    /// </summary>
    public async Task<List<Film>> SearchFilmsByTitleAsync(string searchTerm)
    {
        const string sql = @"
            SELECT film_id, title, genre, duration_minutes, age_rating, release_year, description
            FROM films
            WHERE title ILIKE @SearchTerm
            ORDER BY film_id;";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm.Trim()}%");

        await using var reader = await command.ExecuteReaderAsync();

        var films = new List<Film>();
        while (await reader.ReadAsync())
        {
            films.Add(MapFilm(reader));
        }

        return films;
    }

    /// <summary>
    /// Задание 3: Получение расписания сеансов с расчётом свободных мест и фильтрацией по датам.
    /// Отменённые билеты исключаются из подсчёта занятых мест (status = 'Purchased').
    /// </summary>
    public async Task<List<ScreeningScheduleItem>> GetScreeningsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var sqlBuilder = new StringBuilder(@"
            SELECT
                s.screening_id,
                f.title AS film_title,
                f.genre AS film_genre,
                s.starts_at,
                h.hall_name,
                s.ticket_price,
                h.seat_count AS total_seats,
                (h.seat_count - COUNT(t.ticket_id)) AS available_seats
            FROM screenings s
            JOIN films f ON f.film_id = s.film_id
            JOIN halls h ON h.hall_id = s.hall_id
            LEFT JOIN tickets t ON t.screening_id = s.screening_id AND t.status = 'Purchased'
        ");

        var whereClauses = new List<string>();
        if (startDate.HasValue)
        {
            whereClauses.Add("s.starts_at >= @StartDate");
        }
        if (endDate.HasValue)
        {
            whereClauses.Add("s.starts_at <= @EndDate");
        }

        if (whereClauses.Count > 0)
        {
            sqlBuilder.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
        }

        sqlBuilder.Append(@"
            GROUP BY s.screening_id, f.title, f.genre, s.starts_at, h.hall_name, s.ticket_price, h.seat_count
            ORDER BY s.starts_at ASC, s.screening_id ASC;");

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sqlBuilder.ToString(), connection);
        if (startDate.HasValue)
        {
            command.Parameters.AddWithValue("@StartDate", startDate.Value);
        }
        if (endDate.HasValue)
        {
            command.Parameters.AddWithValue("@EndDate", endDate.Value);
        }

        await using var reader = await command.ExecuteReaderAsync();

        var items = new List<ScreeningScheduleItem>();
        while (await reader.ReadAsync())
        {
            items.Add(new ScreeningScheduleItem
            {
                ScreeningId = reader.GetInt32(reader.GetOrdinal("screening_id")),
                FilmTitle = reader.GetString(reader.GetOrdinal("film_title")),
                Genre = reader.GetString(reader.GetOrdinal("film_genre")),
                StartsAt = reader.GetDateTime(reader.GetOrdinal("starts_at")),
                HallName = reader.GetString(reader.GetOrdinal("hall_name")),
                TicketPrice = reader.GetDecimal(reader.GetOrdinal("ticket_price")),
                TotalSeats = reader.GetInt32(reader.GetOrdinal("total_seats")),
                AvailableSeats = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("available_seats")))
            });
        }

        return items;
    }

    /// <summary>
    /// Задание 4: Поиск клиента по адресу электронной почты.
    /// </summary>
    public async Task<Customer?> GetCustomerByEmailAsync(string email)
    {
        const string sql = @"
            SELECT customer_id, first_name, last_name, email, phone, registered_at
            FROM customers
            WHERE LOWER(email) = LOWER(@Email);";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", email.Trim());

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Customer
            {
                CustomerId = reader.GetInt32(reader.GetOrdinal("customer_id")),
                FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                LastName = reader.GetString(reader.GetOrdinal("last_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                RegisteredAt = reader.GetDateTime(reader.GetOrdinal("registered_at"))
            };
        }

        return null;
    }

    /// <summary>
    /// Задание 4: Получение списка всех билетов конкретного клиента.
    /// </summary>
    public async Task<List<CustomerTicketItem>> GetCustomerTicketsAsync(int customerId)
    {
        const string sql = @"
            SELECT
                t.ticket_id,
                f.title AS film_title,
                s.starts_at,
                h.hall_name,
                st.seat_number,
                s.ticket_price,
                t.status,
                t.purchased_at
            FROM tickets t
            JOIN screenings s ON s.screening_id = t.screening_id
            JOIN films f ON f.film_id = s.film_id
            JOIN halls h ON h.hall_id = s.hall_id
            JOIN seats st ON st.seat_id = t.seat_id
            WHERE t.customer_id = @CustomerId
            ORDER BY s.starts_at DESC, t.ticket_id DESC;";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CustomerId", customerId);

        await using var reader = await command.ExecuteReaderAsync();

        var tickets = new List<CustomerTicketItem>();
        while (await reader.ReadAsync())
        {
            tickets.Add(new CustomerTicketItem
            {
                TicketId = reader.GetInt32(reader.GetOrdinal("ticket_id")),
                FilmTitle = reader.GetString(reader.GetOrdinal("film_title")),
                StartsAt = reader.GetDateTime(reader.GetOrdinal("starts_at")),
                HallName = reader.GetString(reader.GetOrdinal("hall_name")),
                SeatNumber = reader.GetInt32(reader.GetOrdinal("seat_number")),
                Price = reader.GetDecimal(reader.GetOrdinal("ticket_price")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                PurchasedAt = reader.GetDateTime(reader.GetOrdinal("purchased_at"))
            });
        }

        return tickets;
    }

    /// <summary>
    /// Задание 5: Регистрация нового клиента. Возвращает ID созданного клиента (RETURNING customer_id).
    /// Выбрасывает исключение при дублировании уникального email (SqlState 23505).
    /// </summary>
    public async Task<int> RegisterCustomerAsync(string firstName, string lastName, string email, string? phone)
    {
        const string sql = @"
            INSERT INTO customers (first_name, last_name, email, phone)
            VALUES (@FirstName, @LastName, @Email, @Phone)
            RETURNING customer_id;";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FirstName", firstName.Trim());
        command.Parameters.AddWithValue("@LastName", lastName.Trim());
        command.Parameters.AddWithValue("@Email", email.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone.Trim());

        try
        {
            var idResult = await command.ExecuteScalarAsync();
            return Convert.ToInt32(idResult);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new InvalidOperationException($"Клиент с адресом электронной почты '{email.Trim()}' уже зарегистрирован.", ex);
        }
    }

    /// <summary>
    /// Задание 6: Покупка билета в транзакции с полной защитой от параллельных коллизий.
    /// </summary>
    public async Task<PurchaseResult> PurchaseTicketAsync(int screeningId, int customerId, int seatNumber)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Открываем транзакцию (BeginTransaction)
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Проверяем существование сеанса
            int hallId;
            decimal ticketPrice;
            DateTime startsAt;
            string filmTitle;
            string hallName;

            const string screeningSql = @"
                SELECT s.hall_id, s.ticket_price, s.starts_at, f.title, h.hall_name
                FROM screenings s
                JOIN films f ON f.film_id = s.film_id
                JOIN halls h ON h.hall_id = s.hall_id
                WHERE s.screening_id = @ScreeningId;";

            await using (var cmdScreening = new NpgsqlCommand(screeningSql, connection, transaction))
            {
                cmdScreening.Parameters.AddWithValue("@ScreeningId", screeningId);
                await using (var reader = await cmdScreening.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        await transaction.RollbackAsync();
                        return PurchaseResult.Fail($"Сеанс с идентификатором {screeningId} не существует.");
                    }

                    hallId = reader.GetInt32(0);
                    ticketPrice = reader.GetDecimal(1);
                    startsAt = reader.GetDateTime(2);
                    filmTitle = reader.GetString(3);
                    hallName = reader.GetString(4);
                }
            }

            // 2. Проверяем существование клиента
            const string customerSql = "SELECT customer_id FROM customers WHERE customer_id = @CustomerId;";
            await using (var cmdCustomer = new NpgsqlCommand(customerSql, connection, transaction))
            {
                cmdCustomer.Parameters.AddWithValue("@CustomerId", customerId);
                var customerExists = await cmdCustomer.ExecuteScalarAsync();
                if (customerExists == null)
                {
                    await transaction.RollbackAsync();
                    return PurchaseResult.Fail($"Клиент с идентификатором {customerId} не существует.");
                }
            }

            // 3. Проверяем, существует ли указанный номер места в данном зале
            int seatId;
            const string seatSql = "SELECT seat_id FROM seats WHERE hall_id = @HallId AND seat_number = @SeatNumber;";
            await using (var cmdSeat = new NpgsqlCommand(seatSql, connection, transaction))
            {
                cmdSeat.Parameters.AddWithValue("@HallId", hallId);
                cmdSeat.Parameters.AddWithValue("@SeatNumber", seatNumber);
                var seatResult = await cmdSeat.ExecuteScalarAsync();
                if (seatResult == null)
                {
                    await transaction.RollbackAsync();
                    return PurchaseResult.Fail($"Место №{seatNumber} не существует в зале '{hallName}'.");
                }
                seatId = Convert.ToInt32(seatResult);
            }

            // 4. Проверяем, свободно ли место (нет активного билета со статусом 'Purchased')
            const string checkSeatSql = @"
                SELECT ticket_id
                FROM tickets
                WHERE screening_id = @ScreeningId AND seat_id = @SeatId AND status = 'Purchased';";

            await using (var cmdCheckSeat = new NpgsqlCommand(checkSeatSql, connection, transaction))
            {
                cmdCheckSeat.Parameters.AddWithValue("@ScreeningId", screeningId);
                cmdCheckSeat.Parameters.AddWithValue("@SeatId", seatId);
                var occupiedTicket = await cmdCheckSeat.ExecuteScalarAsync();
                if (occupiedTicket != null)
                {
                    await transaction.RollbackAsync();
                    return PurchaseResult.Fail($"Место №{seatNumber} на выбранный сеанс уже занято!");
                }
            }

            // 5. Оформляем покупку: вставляем билет с получением ticket_id
            const string insertTicketSql = @"
                INSERT INTO tickets (screening_id, hall_id, seat_id, customer_id, status)
                VALUES (@ScreeningId, @HallId, @SeatId, @CustomerId, 'Purchased')
                RETURNING ticket_id;";

            int ticketId;
            await using (var cmdInsert = new NpgsqlCommand(insertTicketSql, connection, transaction))
            {
                cmdInsert.Parameters.AddWithValue("@ScreeningId", screeningId);
                cmdInsert.Parameters.AddWithValue("@HallId", hallId);
                cmdInsert.Parameters.AddWithValue("@SeatId", seatId);
                cmdInsert.Parameters.AddWithValue("@CustomerId", customerId);

                var ticketIdObj = await cmdInsert.ExecuteScalarAsync();
                if (ticketIdObj == null)
                {
                    await transaction.RollbackAsync();
                    return PurchaseResult.Fail("Не удалось сохранить запись о билете.");
                }
                ticketId = Convert.ToInt32(ticketIdObj);
            }

            // Фиксация транзакции (Commit)
            await transaction.CommitAsync();

            return PurchaseResult.Ok(ticketId, filmTitle, startsAt, seatNumber, ticketPrice, hallName);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            // Ошибка нарушения уникальности частичного индекса ux_tickets_active_seat
            await transaction.RollbackAsync();
            return PurchaseResult.Fail($"Ошибка конкуренции: место №{seatNumber} было приобретено другим покупателем.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return PurchaseResult.Fail($"Ошибка транзакции при покупке билета: {ex.Message}");
        }
    }

    /// <summary>
    /// Задание 7: Отмена билета по номеру. Физически строка не удаляется, статус меняется на 'Cancelled'.
    /// Проверяет ExecuteNonQuery() и сообщает статус.
    /// </summary>
    public async Task<(bool Success, string Message)> CancelTicketAsync(int ticketId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // 1. Проверяем существование билета и его текущий статус
        const string checkSql = "SELECT status FROM tickets WHERE ticket_id = @TicketId;";
        string? currentStatus = null;
        await using (var cmdCheck = new NpgsqlCommand(checkSql, connection))
        {
            cmdCheck.Parameters.AddWithValue("@TicketId", ticketId);
            var statusObj = await cmdCheck.ExecuteScalarAsync();

            if (statusObj == null)
            {
                return (false, $"Билет с номером {ticketId} не найден в базе данных.");
            }

            currentStatus = statusObj.ToString();
        }

        if (string.Equals(currentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"Билет №{ticketId} уже был отменён ранее.");
        }

        // 2. Обновляем статус билета на 'Cancelled'
        const string updateSql = @"
            UPDATE tickets
            SET status = 'Cancelled'
            WHERE ticket_id = @TicketId AND status = 'Purchased';";

        await using (var cmdUpdate = new NpgsqlCommand(updateSql, connection))
        {
            cmdUpdate.Parameters.AddWithValue("@TicketId", ticketId);
            int rowsAffected = await cmdUpdate.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                return (true, $"Билет №{ticketId} успешно отменён! Место освобождено для повторной покупки.");
            }

            return (false, $"Не удалось отменить билет №{ticketId}. Статус не был изменён.");
        }
    }

    /// <summary>
    /// Задание 8: Отчёт по каждому сеансу: название фильма, дата и время, зал, число проданных билетов и выручка.
    /// Учитываются только билеты со статусом 'Purchased', сеансы без продаж также выводятся.
    /// </summary>
    public async Task<List<ScreeningReportItem>> GetScreeningsReportAsync()
    {
        const string sql = @"
            SELECT
                s.screening_id,
                f.title AS film_title,
                s.starts_at,
                h.hall_name,
                s.ticket_price,
                COUNT(t.ticket_id) AS tickets_sold,
                COALESCE(SUM(s.ticket_price), 0.00) AS total_revenue
            FROM screenings s
            JOIN films f ON f.film_id = s.film_id
            JOIN halls h ON h.hall_id = s.hall_id
            LEFT JOIN tickets t ON t.screening_id = s.screening_id AND t.status = 'Purchased'
            GROUP BY s.screening_id, f.title, s.starts_at, h.hall_name, s.ticket_price
            ORDER BY s.starts_at ASC, s.screening_id ASC;";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var report = new List<ScreeningReportItem>();
        while (await reader.ReadAsync())
        {
            report.Add(new ScreeningReportItem
            {
                ScreeningId = reader.GetInt32(reader.GetOrdinal("screening_id")),
                FilmTitle = reader.GetString(reader.GetOrdinal("film_title")),
                StartsAt = reader.GetDateTime(reader.GetOrdinal("starts_at")),
                HallName = reader.GetString(reader.GetOrdinal("hall_name")),
                TicketPrice = reader.GetDecimal(reader.GetOrdinal("ticket_price")),
                SoldTicketsCount = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("tickets_sold"))),
                TotalRevenue = Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("total_revenue")))
            });
        }

        return report;
    }

    /// <summary>
    /// Задание 8 (дополнительно): Топ-3 фильма по количеству проданных билетов.
    /// </summary>
    public async Task<List<TopFilmReportItem>> GetTopFilmsReportAsync(int limit = 3)
    {
        const string sql = @"
            SELECT
                f.film_id,
                f.title,
                f.genre,
                COUNT(t.ticket_id) AS sold_tickets_count,
                COALESCE(SUM(s.ticket_price), 0.00) AS total_revenue
            FROM films f
            LEFT JOIN screenings s ON s.film_id = f.film_id
            LEFT JOIN tickets t ON t.screening_id = s.screening_id AND t.status = 'Purchased'
            GROUP BY f.film_id, f.title, f.genre
            ORDER BY sold_tickets_count DESC, total_revenue DESC, f.title ASC
            LIMIT @Limit;";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Limit", limit);

        await using var reader = await command.ExecuteReaderAsync();

        var list = new List<TopFilmReportItem>();
        while (await reader.ReadAsync())
        {
            list.Add(new TopFilmReportItem
            {
                FilmId = reader.GetInt32(reader.GetOrdinal("film_id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Genre = reader.GetString(reader.GetOrdinal("genre")),
                SoldTicketsCount = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("sold_tickets_count"))),
                TotalRevenue = Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("total_revenue")))
            });
        }

        return list;
    }

    private static Film MapFilm(NpgsqlDataReader reader)
    {
        return new Film
        {
            FilmId = reader.GetInt32(reader.GetOrdinal("film_id")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            Genre = reader.GetString(reader.GetOrdinal("genre")),
            DurationMinutes = reader.GetInt32(reader.GetOrdinal("duration_minutes")),
            AgeRating = reader.GetInt32(reader.GetOrdinal("age_rating")),
            ReleaseYear = reader.GetInt32(reader.GetOrdinal("release_year")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description"))
        };
    }
}
