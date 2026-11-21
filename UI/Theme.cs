using System.Globalization;
using System.Text;
using KawaiiTasks.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace KawaiiTasks.UI;

/// <summary>
/// Ретро-дневник: пастельная палитра, рамки, наклейки и маленькие помощники для красивого вывода.
///
/// Как цвета попадают в терминал: терминал понимает "управляющие последовательности ANSI" - особые строки,
/// начинающиеся с символа ESC (код 27, в C# '\u001b'). Например, ESC[38;2;255;158;203m включает розовый цвет
/// текста (RGB), а ESC[0m сбрасывает оформление. Spectre.Console превращает наши метки вида [#ff9ecb]текст[/]
/// в такие последовательности сам; если терминал не поддерживает 24-битный цвет, Spectre подбирает ближайший
/// из 256 или 16 цветов. Поэтому ниже цвета задаются обычными hex-строками.
/// </summary>
public static class Theme
{
    // ------------------------------------------------------------ палитра
    // Цвета в формате #RRGGBB. Их можно вставлять прямо в разметку Spectre: $"[{Pink}]текст[/]".
    public const string Pink = "#ff9ecb";
    public const string Lavender = "#c3a6ff";
    public const string Mint = "#8fe3c0";
    public const string Peach = "#ffc09f";
    public const string Sky = "#8fd3f4";
    public const string Lemon = "#ffe680";
    public const string Berry = "#ff6f91";

    // ретро-оттенки 'старой бумаги'
    public const string Cocoa = "#d4b595";   // тесьма, пунктир, даты
    public const string Rose = "#e8a0bf";    // пыльная роза

    // Чёрный фон и светлый текст терминала (устанавливаются через OSC-команды, см. ApplyPaper).
    public const string PaperBg = "#000000";
    public const string PaperFg = "#f3e6d8";

    /// <summary>Выключи (false), если терминал не должен менять цвета окна.</summary>
    public static bool UseCustomBackground = true;

    // Те же цвета в виде объектов Color - для API Spectre, которым нужен Color, а не строка разметки.
    public static readonly Color PinkC = FromHex(Pink);
    public static readonly Color LavenderC = FromHex(Lavender);
    public static readonly Color MintC = FromHex(Mint);
    public static readonly Color CocoaC = FromHex(Cocoa);

    /// <summary>"#ff9ecb" -> Color(255, 158, 203). Каждая пара шестнадцатеричных цифр - это яркость одного канала (R, G, B) от 0 до 255.</summary>
    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        return new Color(
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
    }

    /// <summary>
    /// Экранирует текст пользователя, чтобы квадратные скобки не ломали разметку.
    /// В разметке Spectre "[" открывает тег, поэтому название задачи "Баг [срочно]" без экранирования
    /// вызвало бы исключение. Markup.Escape заменяет "[" на "[[" и "]" на "]]".
    /// ВАЖНО: любой текст, который ввёл пользователь, перед выводом пропускаем через E().
    /// </summary>
    public static string E(string? text) => Markup.Escape(text ?? "");

    // ------------------------------------------------------------ цвета окна (OSC 10/11)

    private static bool _paperApplied;          // ApplyPaper уже отработал - второй раз его выполнять нельзя
    private static bool _paperOn;
    private static bool _consoleColorsCaptured;
    private static ConsoleColor _oldBackground;
    private static ConsoleColor _oldForeground;

    /// <summary>
    /// Красит фон и текст терминала. Терминалы без поддержки просто игнорируют команду.
    ///
    /// Два способа покрасить окно:
    ///  1) Windows Console API (Console.BackgroundColor): работает в классической консоли, но красит только
    ///     те ячейки, куда мы что-то напечатали, поэтому после смены цвета вызывается Console.Clear().
    ///  2) OSC-последовательности (Operating System Command): "ESC ] 11 ; цвет BEL" меняет цвет фона по умолчанию
    ///     всего окна, "ESC ] 10 ; цвет BEL" - цвет текста по умолчанию. Их понимают современные терминалы.
    ///     BEL (код 7, '\u0007') здесь служит "точкой" в конце команды.
    /// </summary>
    public static void ApplyPaper()
    {
        // _paperApplied защищает от повторного вызова: иначе мы бы запомнили уже изменённые цвета как "исходные"
        // и повесили на выход из программы лишние обработчики.
        if (!UseCustomBackground || _paperApplied || Console.IsOutputRedirected) return;
        _paperApplied = true;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                // Запоминаем текущие цвета, чтобы вернуть их при выходе (ResetPaper).
                _oldBackground = Console.BackgroundColor;
                _oldForeground = Console.ForegroundColor;
                _consoleColorsCaptured = true;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Clear();
            }

            // OSC-команды отправляем в современные ANSI-терминалы. В старой Windows Console
            // используем ConsoleColor, чтобы не выводить управляющие последовательности в текст.
            var supportsOsc = !OperatingSystem.IsWindows() ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WT_SESSION")) ||
                string.Equals(Environment.GetEnvironmentVariable("ConEmuANSI"), "ON", StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANSICON"));

            if (supportsOsc)
            {
                // ESC ] 11 ; #000000 BEL  -> фон окна чёрный;  ESC ] 10 ; #f3e6d8 BEL  -> текст по умолчанию светло-бежевый.
                Console.Write($"\u001b]11;{PaperBg}\u0007\u001b]10;{PaperFg}\u0007");
                _paperOn = true;
            }

            // Возвращаем исходные цвета и при обычном выходе, и при Ctrl+C - иначе чёрный фон "прилипнет" к терминалу.
            AppDomain.CurrentDomain.ProcessExit += (_, _) => ResetPaper();
            Console.CancelKeyPress += (_, _) => ResetPaper();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or PlatformNotSupportedException)
        {
            // Не каждый хост разрешает менять цвет окна или очищать экран.
        }
    }

    /// <summary>Возвращает терминалу исходные цвета при завершении программы.</summary>
    public static void ResetPaper()
    {
        if (!_paperOn && !_consoleColorsCaptured) return;
        try
        {
            if (_paperOn)
                // OSC 111 и OSC 110 - "сбросить фон и цвет текста к настройкам самого терминала".
                Console.Write("\u001b]111\u0007\u001b]110\u0007");
            _paperOn = false;

            if (_consoleColorsCaptured)
            {
                Console.BackgroundColor = _oldBackground;
                Console.ForegroundColor = _oldForeground;
                _consoleColorsCaptured = false;
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or PlatformNotSupportedException)
        {
            // Восстановление оформления не должно мешать завершению приложения.
        }
    }

    // ------------------------------------------------------------ приоритеты и статусы

    public static string PriorityHex(Priority p) => p switch
    {
        Priority.Low => Mint,
        Priority.Medium => Sky,
        Priority.High => Peach,
        _ => Berry
    };

    public static string StateHex(TaskState s) => s switch
    {
        TaskState.Todo => Lavender,
        TaskState.InProgress => Lemon,
        TaskState.Review => Sky,
        _ => Mint
    };

    /// <summary>Значок статуса: пустой кружок -> половинки -> цветочек-стикер.</summary>
    public static string StateIcon(TaskState s) => s switch
    {
        TaskState.Todo => "o",
        TaskState.InProgress => "~",
        TaskState.Review => "R",     // раньше здесь был "?", но он похож на символ, в который терминал превращает нечитаемые буквы
        _ => "OK"
    };

    /// <summary>Приоритет звёздочками.</summary>
    public static string PriorityStars(Priority p) => p switch
    {
        Priority.Low => "*...",
        Priority.Medium => "**..",
        Priority.High => "***.",
        _ => "****"
    };

    public static string PriorityTag(Priority p) => $"[{PriorityHex(p)}]{E(p.Text())}[/]";
    public static string StateTag(TaskState s) => $"[{StateHex(s)}]{E(s.Text())}[/]";
    public static string PriorityDot(Priority p) => $"[{PriorityHex(p)}]*[/]";
    public static string PriorityLabel(Priority p) => $"[{PriorityHex(p)}]{PriorityStars(p)}[/] {E(p.Text())}";
    public static string StateLabel(TaskState s) => $"[{StateHex(s)}]{StateIcon(s)} {E(s.Text())}[/]";

    // ------------------------------------------------------------ украшения

    /// <summary>
    /// Ширина окна терминала в символах. Spectre.Console узнаёт её у терминала (Console.WindowWidth);
    /// если вывод перенаправлен в файл - берёт 80. Ограничиваем 20..120, чтобы вёрстка не ломалась
    /// ни в крошечном окне, ни на очень широком мониторе.
    /// </summary>
    public static int Width => Math.Clamp(AnsiConsole.Profile.Width, 20, 120);

    private static readonly string[] SparkleTokens =
        { "*", "|", ".", "|", "*", "|", ".", "|", "*", "|", ".", "|", "*", "|", ".", "|" };

    private static readonly string[] SparkleColors = { Pink, Lavender, Sky, Mint, Peach, Lemon };

    /// <summary>
    /// Строка из мерцающих звёздочек и точек. Каждый символ занимает 2 позиции (символ и пробел),
    /// поэтому в строку шириной width влезает width / 2 значков. Возвращает разметку, а не готовый текст.
    /// </summary>
    public static string SparkleMarkup(int width, int offset = 0)
    {
        var sb = new StringBuilder();
        var used = 0;
        var i = offset;
        while (used + 2 <= width)
        {
            var token = SparkleTokens[i % SparkleTokens.Length];
            sb.Append($"[{SparkleColors[i % SparkleColors.Length]}]{token}[/] ");
            used += 2;
            i++;
        }
        return sb.ToString().TrimEnd();
    }

    public static void Sparkles(int? width = null) =>
        AnsiConsole.Write(new Align(new Markup(SparkleMarkup(Math.Min(width ?? Width, Width))), HorizontalAlignment.Center));

    // Align(..., Center) просит Spectre вычислить отступ слева так, чтобы блок оказался по центру ТЕКУЩЕЙ ширины окна.
    /// <summary>Дырочки от переплёта блокнота.</summary>
    public static void Spiral()
    {
        var holes = string.Concat(Enumerable.Repeat("o---", Math.Max(4, Width / 4))).TrimEnd();
        AnsiConsole.Write(new Align(new Markup($"[{Lavender}]{holes}[/]"), HorizontalAlignment.Center));
    }

    /// <summary>Полоска цветного скотча над карточкой.</summary>
    public static void Tape(string? color = null, int length = 14) =>
        AnsiConsole.Write(new Align(
            new Markup($"[{color ?? Peach}]{new string('/', length)}[/]"), HorizontalAlignment.Center));

    /// <summary>Пунктирная 'строчка' тетради.</summary>
    public static string Dots(int length) => $"[{Cocoa}]{new string('-', length)}[/]";

    /// <summary>Закладка-лента с названием раздела.</summary>
    public static void Ribbon(string title, string? color = null)
    {
        var c = color ?? Lavender;
        var text = $" * {title} * ";
        var side = Math.Max(2, (Width - text.Length - 4) / 2);
        var bar = new string('=', side);
        AnsiConsole.Write(new Align(
            new Markup($"[{c}]|{bar}|{E(text)}|{bar}|[/]"), HorizontalAlignment.Center));
    }

    /// <summary>
    /// Страничка дневника с ASCII-рамкой и центрированным заголовком.
    /// Panel - блок с рамкой вокруг любого содержимого; Spectre сам переносит длинные строки внутри рамки
    /// по ширине окна. Align центрирует рамку в окне.
    /// </summary>
    public static IRenderable Page(IRenderable content, string? title = null, string? color = null,
        bool dbl = true, Padding? padding = null)
    {
        var c = color ?? Pink;
        var panel = new Panel(content)
        {
            // Используем только ASCII-рамку: она поддерживается Spectre.Console 0.49.1
            // и корректно отображается в терминалах без Unicode-шрифта.
            Border = BoxBorder.Ascii,
            BorderStyle = new Style(FromHex(c)),
            Padding = padding ?? new Padding(2, 0)
        };
        if (title is not null)
            panel.Header = new PanelHeader($"[{c}] {E(title)} [/]", Justify.Center);
        return new Align(panel, HorizontalAlignment.Center);
    }

    /// <summary>Сегодняшняя дата по-русски (если нет культуры - обычный формат).</summary>
    public static string Today()
    {
        try { return DateTime.Now.ToString("dddd, d MMMM", new CultureInfo("ru-RU")); }
        catch (Exception ex) when (ex is ArgumentException)
        {
            return DateTime.Now.ToString("dd.MM.yyyy");
        }
    }

    // ------------------------------------------------------------ экраны и сообщения

    /// <summary>
    /// Шапка каждого экрана. AnsiConsole.Clear() очищает экран и возвращает курсор в левый верхний угол
    /// (последовательности ESC[2J и ESC[H), поэтому каждый экран начинается "с чистого листа".
    /// </summary>
    public static void Banner()
    {
        AnsiConsole.Clear();
        Spiral();
        Sparkles();
        // FigletText рисует крупные буквы из обычных символов (ASCII-арт шрифтом Standard).
        AnsiConsole.Write(new FigletText("kawaii tasks").Centered().Color(PinkC));
        Ribbon("М Е Н Е Д Ж Е Р   I T - П Р О Е К Т О В");
        AnsiConsole.Write(new Align(
            new Markup($"[{Cocoa}]> запись от {E(Today())} >[/]"), HorizontalAlignment.Center));
        AnsiConsole.Write(new Align(
            new Markup("[grey]~ уютное пространство для больших планов и маленьких шагов ~[/]"),
            HorizontalAlignment.Center));
        AnsiConsole.Write(new Align(
            new Markup("[grey]^/v - перемещение | Enter - выбор | :back - возврат[/]"),
            HorizontalAlignment.Center));
        Sparkles();
        AnsiConsole.WriteLine();
    }

    public static void Ok(string message) =>
        AnsiConsole.MarkupLine($"[{Mint}]  * * {E(message)} * *[/]");

    public static void Fail(string message) =>
        AnsiConsole.MarkupLine($"[{Berry}]  (;w;) x {E(message)}[/]");

    public static void Hint(string message) =>
        AnsiConsole.MarkupLine($"[{Lavender}]  ~ {E(message)} ~[/]");

    /// <summary>
    /// Ждёт нажатия любой клавиши. ReadKey(true) читает одно нажатие и НЕ печатает символ на экране (intercept).
    /// Если ввод перенаправлен (например, из файла в тестах), ждать некого - просто продолжаем.
    /// </summary>
    public static void Pause()
    {
        if (Console.IsInputRedirected) return;
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Dots(Math.Min(Width, 60)));
        AnsiConsole.Markup($"[grey]нажми любую клавишу, чтобы продолжить[/] [{Pink}]*[/]");
        try { Console.ReadKey(true); }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or NotSupportedException)
        {
            // ReadKey не работает в некоторых терминалах (например, в консоли отладки IDE) - ждём Enter через ReadLine.
            try { Console.ReadLine(); }
            catch (IOException) { /* Нет интерактивного ввода - продолжаем без паузы. */ }
        }
        AnsiConsole.WriteLine();
    }

    /// <summary>Прощальная страничка: кролик машет лапкой.</summary>
    public static void Goodbye(string storagePath)
    {
        AnsiConsole.Clear();
        Spiral();
        Sparkles();
        AnsiConsole.WriteLine();

        var art = new Markup($"[{Pink}] (\\_/)  *\n (^.^)/\n (\")_(\")[/]");
        var text = new Markup(
            $"[bold {Pink}]До встречи, дневничок! <3[/]\n" +
            "[grey]Данные сохранены здесь:[/]\n" +
            $"[{Cocoa}]{E(storagePath)}[/]\n" +
            $"[{Lavender}]До новых встреч! *[/]");

        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();
        grid.AddRow(art, text);

        Tape(Sky);
        AnsiConsole.Write(Page(grid, "~ ПОСЛЕДНЯЯ СТРАНИЧКА ~", Pink, true, new Padding(3, 1)));
        Sparkles();
        AnsiConsole.WriteLine();
        ResetPaper();   // перед выходом возвращаем терминалу его собственные цвета
    }
}
