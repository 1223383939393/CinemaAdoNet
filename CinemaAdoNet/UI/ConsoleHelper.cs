namespace CinemaAdoNet.UI;

public static class ConsoleHelper
{
    public static void PrintHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"  {title.ToUpper()}");
        Console.WriteLine(new string('=', 70));
        Console.ResetColor();
    }

    public static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[+] {message}");
        Console.ResetColor();
    }

    public static void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[!] {message}");
        Console.ResetColor();
    }

    public static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[-] Ошибка: {message}");
        Console.ResetColor();
    }

    public static void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[*] {message}");
        Console.ResetColor();
    }

    public static string ReadNonEmptyString(string prompt)
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            var input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input))
            {
                return input;
            }
            PrintWarning("Поле не может быть пустым. Повторите ввод.");
        }
    }

    public static int ReadInt(string prompt, int min = 1, int max = int.MaxValue)
    {
        while (true)
        {
            Console.Write($"{prompt} ({min}..{(max == int.MaxValue ? "∞" : max.ToString())}): ");
            var input = Console.ReadLine()?.Trim();
            if (int.TryParse(input, out var result) && result >= min && result <= max)
            {
                return result;
            }
            PrintWarning($"Пожалуйста, введите корректное число от {min} до {(max == int.MaxValue ? "любого положительного" : max.ToString())}.");
        }
    }

    public static DateTime? ReadOptionalDate(string prompt)
    {
        while (true)
        {
            Console.Write($"{prompt} (ГГГГ-ММ-ДД или Enter для пропуска): ");
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            if (DateTime.TryParse(input, out var date))
            {
                return date.Date;
            }

            PrintWarning("Неверный формат даты. Используйте формат ГГГГ-ММ-ДД (например, 2026-10-30).");
        }
    }
}
