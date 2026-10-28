using System.Text.Json;

namespace CinemaAdoNet.Configuration;

public class AppConfig
{
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Database=cinema_training;Username=postgres;Password=postgres";

    public string ConnectionString { get; set; } = DefaultConnectionString;

    public static AppConfig Load()
    {
        var config = new AppConfig();

        // 1. Проверяем переменную окружения
        var envConn = Environment.GetEnvironmentVariable("CINEMA_DB_CONN");
        if (!string.IsNullOrWhiteSpace(envConn))
        {
            config.ConnectionString = envConn.Trim();
            return config;
        }

        // 2. Проверяем файл appsettings.json в нескольких стандартных локациях
        var searchPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "CinemaAdoNet", "appsettings.json")
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings) &&
                        connStrings.TryGetProperty("CinemaDatabase", out var cinemaDb))
                    {
                        var val = cinemaDb.GetString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            config.ConnectionString = val.Trim();
                            return config;
                        }
                    }
                }
                catch
                {
                    // Игнорируем ошибку чтения файла и используем значение по умолчанию
                }
            }
        }

        return config;
    }
}
