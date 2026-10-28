using System.Text;
using CinemaAdoNet.Configuration;
using CinemaAdoNet.UI;

namespace CinemaAdoNet;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Устанавливаем UTF-8 для корректного отображения кириллицы в консоли
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        // Загрузка конфигурации
        var config = AppConfig.Load();

        // Возможность передать строку подключения аргументом командной строки:
        // dotnet run -- "Host=localhost;Database=cinema_training;..."
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            config.ConnectionString = args[0].Trim();
        }

        var menu = new MenuManager(config.ConnectionString);
        await menu.RunAsync();
    }
}
