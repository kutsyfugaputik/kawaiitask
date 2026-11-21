using System.Globalization;
using Spectre.Console;

namespace KawaiiTasks.UI;

/// <summary>
/// Событие навигации: пользователь попросил вернуться в предыдущее меню.
/// Это исключение используется как "аварийный выход" из глубоко вложенных вызовов ввода:
/// оно пролетает через все методы и ловится в циклах меню (см. App.ProjectLoop и др.).
/// </summary>
public sealed class NavigationBackException : Exception
{
    public NavigationBackException() : base("Возврат в предыдущее меню.") { }
}

/// <summary>
/// Ввод с проверкой данных и единым способом вернуться на предыдущий экран.
///
/// Как работает ввод в терминале: у терминала два режима. В "построчном" (его использует TextPrompt)
/// терминал сам собирает набираемые символы и отдаёт программе строку целиком после Enter.
/// В "посимвольном" (его использует SelectionPrompt для стрелок) программа через Console.ReadKey получает
/// каждое нажатие отдельно, а затем сама перерисовывает меню - поэтому стрелки двигают подсветку, а не печатают символы.
/// Обо всём этом заботится Spectre.Console; здесь мы описываем только то, ЧТО спрашивать и КАК проверять ответ.
/// </summary>
public static class Input
{
    private sealed record Choice<T>(T Value, string Label, bool IsBack = false);

    // Форматы дат, которые понимает программа: 05.03.2026, 5.3.2026 и 05.03.26.
    private static readonly string[] DateFormats = { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy" };

    // Слова, которые считаются ответом "да" / "нет" в подтверждениях.
    private static readonly string[] YesAnswers = { "да", "д", "y" };
    private static readonly string[] NoAnswers = { "нет", "н", "n" };

    // Цвет текста, который печатает пользователь (лавандовый), - чтобы ввод отличался от подсказок.
    private static readonly Style Typed = new(Theme.LavenderC);

    // Не меняем Console.CursorLeft перед интерактивными запросами Spectre.Console:
    // SelectionPrompt/TextPrompt сами управляют перерисовкой. Ручное позиционирование
    // оставляло курсор в середине строки и могло приводить к повторному выводу заголовка.
    // Строка в квадратных скобках - это "разметка" Spectre: [цвет]текст[/] включает и выключает цвет.
    private static string Prompt(string text) =>
        $"[{Theme.Cocoa}]|[/] [{Theme.Pink}]> {Theme.E(text)}[/] [grey](:back - назад)[/]";

    /// <summary>Если пользователь ввёл :back или /back - прерываем текущий ввод исключением навигации.</summary>
    private static void ThrowIfBack(string? value)
    {
        if (string.Equals(value?.Trim(), ":back", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value?.Trim(), "/back", StringComparison.OrdinalIgnoreCase))
            throw new NavigationBackException();
    }

    /// <summary>Обязательная строка до max символов. Команда :back возвращает в меню.</summary>
    public static string Ask(string text, int max = 80)
    {
        // Validate вызывается Spectre после каждого Enter: если вернуть Error, текст ошибки печатается,
        // а пользователю предлагается ввести значение заново - прямо в том же месте экрана.
        var prompt = new TextPrompt<string>(Prompt(text))
            .PromptStyle(Typed)
            .Validate(s =>
            {
                ThrowIfBack(s);
                if (string.IsNullOrWhiteSpace(s))
                    return ValidationResult.Error($"[{Theme.Berry}]Поле не может быть пустым[/]");
                if (s.Trim().Length > max)
                    return ValidationResult.Error($"[{Theme.Berry}]Максимум {max} символов[/]");
                return ValidationResult.Success();
            });
        return AnsiConsole.Prompt(prompt).Trim();
    }

    /// <summary>Необязательная строка (можно нажать Enter).</summary>
    public static string AskOptional(string text, int max = 500)
    {
        // AllowEmpty разрешает пустой ответ: без него Spectre не принял бы просто Enter.
        var prompt = new TextPrompt<string>(Prompt(text))
            .PromptStyle(Typed)
            .AllowEmpty()
            .Validate(s =>
            {
                ThrowIfBack(s);
                return (s ?? "").Trim().Length > max
                    ? ValidationResult.Error($"[{Theme.Berry}]Максимум {max} символов[/]")
                    : ValidationResult.Success();
            });
        return AnsiConsole.Prompt(prompt).Trim();
    }

    /// <summary>Разбирает дату строго по форматам <see cref="DateFormats"/>. Культура - инвариантная, чтобы не зависеть от настроек ОС.</summary>
    public static bool TryParseDate(string? text, out DateTime date) =>
        DateTime.TryParseExact((text ?? "").Trim(), DateFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>Дата в формате дд.мм.гггг; пустой ввод = без срока.</summary>
    public static DateTime? AskDate(string text)
    {
        var prompt = new TextPrompt<string>(Prompt($"{text} (дд.мм.гггг, Enter - без срока)"))
            .PromptStyle(Typed)
            .AllowEmpty()
            .Validate(s =>
            {
                ThrowIfBack(s);
                if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success();
                if (!TryParseDate(s, out var date))
                    return ValidationResult.Error($"[{Theme.Berry}]Формат: дд.мм.гггг, например 25.12.2026[/]");
                if (date.Date < DateTime.Today)
                    return ValidationResult.Error($"[{Theme.Berry}]Эта дата уже прошла[/]");
                return ValidationResult.Success();
            });

        var raw = AnsiConsole.Prompt(prompt);

        // Строка уже прошла проверку выше, поэтому повторный разбор должен удаться;
        // на всякий случай обрабатываем и неудачу (вернём "без срока"), а не падаем.
        return TryParseDate(raw, out var parsed) ? parsed : null;
    }

    /// <summary>Часы как 1.5 или 1:30 -> минуты (от 1 минуты до 24 часов).</summary>
    public static bool TryParseDuration(string? text, out int minutes)
    {
        minutes = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Русская запятая как десятичный разделитель ("1,5") приравнивается к точке.
        var value = text.Trim().Replace(',', '.');
        if (value.Contains(':'))
        {
            // Формат "часы:минуты". NumberStyles.None запрещает знаки, пробелы и точки внутри чисел.
            var parts = value.Split(':');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var h) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var m) ||
                h < 0 || m < 0 || m > 59)
                return false;
            if (h > 24 || (h == 24 && m > 0)) return false;
            minutes = h * 60 + m;
        }
        else
        {
            // Формат "часы десятичной дробью". AllowDecimalPoint - только цифры и одна точка:
            // экспоненту ("1e1"), знак и бесконечность разбор не пропустит.
            if (!double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var hours) ||
                !double.IsFinite(hours) || hours < 0 || hours > 24)
                return false;
            minutes = (int)Math.Round(hours * 60, MidpointRounding.AwayFromZero);
        }

        return minutes is >= 1 and <= 1440;
    }

    public static int AskDuration(string text)
    {
        var prompt = new TextPrompt<string>(Prompt(text))
            .PromptStyle(Typed)
            .Validate(s =>
            {
                ThrowIfBack(s);
                return TryParseDuration(s, out _)
                    ? ValidationResult.Success()
                    : ValidationResult.Error($"[{Theme.Berry}]Введи часы: 1.5 или 1:30 (от 1 минуты до 24 часов)[/]");
            });

        TryParseDuration(AnsiConsole.Prompt(prompt), out var minutes);
        return minutes;
    }

    /// <summary>Выбор одного варианта. Дополнительный пункт и команда :back возвращают назад.</summary>
    public static T Pick<T>(string title, IEnumerable<T> items, Func<T, string> label, bool allowBack = true)
        where T : notnull
    {
        var values = items.ToList();
        if (values.Count == 0)
            throw new InvalidOperationException("Для выбора не передано ни одного варианта.");

        // Оборачиваем каждый вариант в Choice: так у пункта "<- Назад" есть собственный признак IsBack,
        // и его не надо путать с настоящими значениями.
        var choices = values.Select(value => new Choice<T>(value, label(value))).ToList();
        if (allowBack)
            choices.Add(new Choice<T>(default!, "<- Назад", true));

        // SelectionPrompt рисует список и в посимвольном режиме реагирует на стрелки и Enter.
        // PageSize - сколько пунктов видно одновременно; длинные списки прокручиваются.
        var prompt = new SelectionPrompt<Choice<T>>()
            .Title($"[{Theme.Cocoa}]+-[/] [{Theme.Lavender}]* {Theme.E(title)} *[/]\n[{Theme.Cocoa}]+-[/]")
            .PageSize(Math.Clamp(Theme.Width / 7, 6, 14))
            .MoreChoicesText("[grey](^/v - перемещение | Enter - выбор)[/]")
            .HighlightStyle(new Style(Theme.PinkC, decoration: Decoration.Bold))
            // Converter превращает пункт в текст на экране. Текст пользователя (названия задач) обязательно
            // экранируем (E), иначе квадратные скобки в названии были бы приняты за разметку и вызвали ошибку.
            .UseConverter(choice => Theme.E(choice.Label))
            .AddChoices(choices);

        var selected = AnsiConsole.Prompt(prompt);
        if (selected.IsBack)
            throw new NavigationBackException();
        return selected.Value;
    }

    /// <summary>Выбор нескольких значений с отдельным пунктом возврата.</summary>
    public static List<T> PickMany<T>(string title, IEnumerable<T> items, Func<T, string> label)
        where T : notnull
    {
        var choices = items.Select(value => new Choice<T>(value, label(value))).ToList();
        choices.Add(new Choice<T>(default!, "<- Назад", true));

        // Пробел ставит/снимает галочку у пункта, Enter подтверждает выбор.
        // NotRequired - можно подтвердить, не отметив ничего (это значит "все").
        var selected = AnsiConsole.Prompt(new MultiSelectionPrompt<Choice<T>>()
            .Title($"[{Theme.Lavender}]{Theme.E(title)}[/]")
            .NotRequired()
            .InstructionsText("[grey](пробел - отметить | Enter - готово)[/]")
            .HighlightStyle(new Style(Theme.PinkC))
            .UseConverter(choice => Theme.E(choice.Label))
            .AddChoices(choices));

        if (selected.Any(choice => choice.IsBack))
            throw new NavigationBackException();
        return selected.Where(choice => !choice.IsBack).Select(choice => choice.Value).ToList();
    }

    /// <summary>Выбор значения перечисления или пункта "любой" (тогда возвращается null).</summary>
    public static T? PickOrAny<T>(string title, IEnumerable<T> items, Func<T, string> label, string anyLabel)
        where T : struct
    {
        var choices = new List<(T? Value, string Label)> { (null, anyLabel) };
        foreach (var item in items)
            choices.Add((item, label(item)));
        return Pick(title, choices, c => c.Label).Value;
    }

    /// <summary>
    /// Разбирает ответ на вопрос "да/нет": true = да, false = нет или пустой ввод (безопасное значение по умолчанию),
    /// null = непонятный ответ. Регистр не важен.
    /// </summary>
    public static bool? ParseYesNo(string? text)
    {
        var answer = (text ?? "").Trim();
        if (answer.Length == 0) return false;
        if (YesAnswers.Contains(answer, StringComparer.OrdinalIgnoreCase)) return true;
        if (NoAnswers.Contains(answer, StringComparer.OrdinalIgnoreCase)) return false;
        return null;
    }

    /// <summary>Подтверждение с безопасным значением по умолчанию (нет).</summary>
    public static bool Confirm(string text)
    {
        var prompt = new TextPrompt<string>(Prompt($"{text} (да/нет)"))
            .PromptStyle(Typed)
            .AllowEmpty()
            .Validate(value =>
            {
                ThrowIfBack(value);
                return ParseYesNo(value) is null
                    ? ValidationResult.Error($"[{Theme.Berry}]Введи да или нет[/]")
                    : ValidationResult.Success();
            });

        return ParseYesNo(AnsiConsole.Prompt(prompt)) == true;
    }
}
