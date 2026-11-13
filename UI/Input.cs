using System.Globalization;
using Spectre.Console;

namespace KawaiiTasks.UI;

/// <summary>Безопасный ввод: каждое значение проверяется до того, как попадёт в программу.</summary>
public static class Input
{
    private static string Prompt(string text) => $"[{Theme.Pink}]{Theme.E(text)}[/]";

    /// <summary>Обязательная строка до max символов.</summary>
    public static string Ask(string text, int max = 80)
    {
        var prompt = new TextPrompt<string>(Prompt(text))
            .Validate(s =>
            {
                if (string.IsNullOrWhiteSpace(s))
                    return ValidationResult.Error($"[{Theme.Berry}]Поле не может быть пустым[/]");
                if (s.Trim().Length > max)
                    return ValidationResult.Error($"[{Theme.Berry}]Максимум {max} символов[/]");
                return ValidationResult.Success();
            });
        return AnsiConsole.Prompt(prompt).Trim();
    }

    /// <summary>Необязательная строка (можно просто нажать Enter).</summary>
    public static string AskOptional(string text, int max = 500)
    {
        var prompt = new TextPrompt<string>(Prompt(text))
            .AllowEmpty()
            .Validate(s => (s ?? "").Trim().Length > max
                ? ValidationResult.Error($"[{Theme.Berry}]Максимум {max} символов[/]")
                : ValidationResult.Success());
        return AnsiConsole.Prompt(prompt).Trim();
    }

    public static bool TryParseDate(string? text, out DateTime date) =>
        DateTime.TryParseExact((text ?? "").Trim(), new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>Дата в формате дд.мм.гггг; пустой ввод = «без срока».</summary>
    public static DateTime? AskDate(string text)
    {
        var prompt = new TextPrompt<string>(Prompt($"{text} (дд.мм.гггг, Enter — без срока)"))
            .AllowEmpty()
            .Validate(s =>
            {
                if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success();
                if (!TryParseDate(s, out var date))
                    return ValidationResult.Error($"[{Theme.Berry}]Формат: дд.мм.гггг, например 25.12.2026[/]");
                if (date.Date < DateTime.Today)
                    return ValidationResult.Error($"[{Theme.Berry}]Эта дата уже прошла[/]");
                return ValidationResult.Success();
            });

        var raw = AnsiConsole.Prompt(prompt);
        return string.IsNullOrWhiteSpace(raw) ? null : DateTime.ParseExact(raw.Trim(),
            new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy" }, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    /// <summary>Часы как «1.5» или «1:30» → минуты (от 1 минуты до 24 часов).</summary>
    public static bool TryParseDuration(string? text, out int minutes)
    {
        minutes = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var value = text.Trim().Replace(',', '.');
        if (value.Contains(':'))
        {
            var parts = value.Split(':');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out var h) ||
                !int.TryParse(parts[1], out var m) ||
                h < 0 || m < 0 || m > 59)
                return false;
            minutes = h * 60 + m;
        }
        else
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hours))
                return false;
            minutes = (int)Math.Round(hours * 60);
        }

        return minutes is >= 1 and <= 1440;
    }

    public static int AskDuration(string text)
    {
        var prompt = new TextPrompt<string>(Prompt(text))
            .Validate(s => TryParseDuration(s, out _)
                ? ValidationResult.Success()
                : ValidationResult.Error($"[{Theme.Berry}]Введи часы: 1.5 или 1:30 (от 1 минуты до 24 часов)[/]"));

        TryParseDuration(AnsiConsole.Prompt(prompt), out var minutes);
        return minutes;
    }

    /// <summary>Выбор одного варианта стрелочками.</summary>
    public static T Pick<T>(string title, IEnumerable<T> items, Func<T, string> label) where T : notnull
    {
        var prompt = new SelectionPrompt<T>()
            .Title($"[{Theme.Lavender}]{Theme.E(title)}[/]")
            .PageSize(12)
            .MoreChoicesText("[grey](листай вверх/вниз ↑↓)[/]")
            .HighlightStyle(new Style(Theme.PinkC))
            .UseConverter(x => Theme.E(label(x)))
            .AddChoices(items);
        return AnsiConsole.Prompt(prompt);
    }

    /// <summary>Выбор значения перечисления или «любое» (тогда вернётся null).</summary>
    public static T? PickOrAny<T>(string title, IEnumerable<T> items, Func<T, string> label, string anyLabel)
        where T : struct
    {
        var choices = new List<(T? Value, string Label)> { (null, anyLabel) };
        foreach (var item in items)
            choices.Add((item, label(item)));
        return Pick(title, choices, c => c.Label).Value;
    }

    public static bool Confirm(string text) => AnsiConsole.Confirm($"[{Theme.Pink}]{Theme.E(text)}[/] [grey](y/n)[/]", false);
}
