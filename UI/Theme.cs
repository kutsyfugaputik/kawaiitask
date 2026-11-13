using System.Globalization;
using System.Text;
using KawaiiTasks.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace KawaiiTasks.UI;

/// <summary>Ретро-дневник: пастельная палитра, рамки, наклейки и маленькие помощники для красивого вывода.</summary>
public static class Theme
{
    // ------------------------------------------------------------ палитра
    public const string Pink = "#ff9ecb";
    public const string Lavender = "#c3a6ff";
    public const string Mint = "#8fe3c0";
    public const string Peach = "#ffc09f";
    public const string Sky = "#8fd3f4";
    public const string Lemon = "#ffe680";
    public const string Berry = "#ff6f91";

    // ретро-оттенки «старой бумаги»
    public const string Cocoa = "#d4b595";   // тесьма, пунктир, даты
    public const string Rose = "#e8a0bf";    // пыльная роза

    // Фон и цвет текста терминала («тёмная слива» + «сливочный» текст).
    public const string PaperBg = "#2b2038";
    public const string PaperFg = "#f3e6d8";

    /// <summary>Выключи (false), если терминал не должен менять цвета окна.</summary>
    public static bool UseCustomBackground = true;

    public static readonly Color PinkC = FromHex(Pink);
    public static readonly Color LavenderC = FromHex(Lavender);
    public static readonly Color MintC = FromHex(Mint);
    public static readonly Color CocoaC = FromHex(Cocoa);

    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        return new Color(
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
    }

    /// <summary>Экранирует текст пользователя, чтобы квадратные скобки не ломали разметку.</summary>
    public static string E(string? text) => Markup.Escape(text ?? "");

    // ------------------------------------------------------------ цвета окна (OSC 10/11)

    private static bool _paperOn;

    /// <summary>Красит фон и текст терминала. Терминалы без поддержки просто игнорируют команду.</summary>
    public static void ApplyPaper()
    {
        if (!UseCustomBackground || _paperOn || Console.IsOutputRedirected) return;
        try
        {
            Console.Write($"\u001b]11;{PaperBg}\u0007\u001b]10;{PaperFg}\u0007");
            _paperOn = true;
            AppDomain.CurrentDomain.ProcessExit += (_, _) => ResetPaper();
            Console.CancelKeyPress += (_, _) => ResetPaper();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException) { }
    }

    /// <summary>Возвращает терминалу его обычные цвета.</summary>
    public static void ResetPaper()
    {
        if (!_paperOn) return;
        _paperOn = false;
        try { Console.Write("\u001b]111\u0007\u001b]110\u0007"); }
        catch (Exception ex) when (ex is IOException or InvalidOperationException) { }
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

    /// <summary>Значок статуса: пустой кружок → половинки → цветочек-стикер.</summary>
    public static string StateIcon(TaskState s) => s switch
    {
        TaskState.Todo => "○",
        TaskState.InProgress => "◐",
        TaskState.Review => "◑",
        _ => "✿"
    };

    /// <summary>Приоритет звёздочками.</summary>
    public static string PriorityStars(Priority p) => p switch
    {
        Priority.Low => "★☆☆☆",
        Priority.Medium => "★★☆☆",
        Priority.High => "★★★☆",
        _ => "★★★★"
    };

    public static string PriorityTag(Priority p) => $"[{PriorityHex(p)}]{E(p.Text())}[/]";
    public static string StateTag(TaskState s) => $"[{StateHex(s)}]{E(s.Text())}[/]";
    public static string PriorityDot(Priority p) => $"[{PriorityHex(p)}]●[/]";
    public static string PriorityLabel(Priority p) => $"[{PriorityHex(p)}]{PriorityStars(p)}[/] {E(p.Text())}";
    public static string StateLabel(TaskState s) => $"[{StateHex(s)}]{StateIcon(s)} {E(s.Text())}[/]";

    // ------------------------------------------------------------ украшения

    public static int Width => Math.Clamp(AnsiConsole.Profile.Width, 50, 110);

    private static readonly string[] SparkleTokens =
        { "✧", "·", "˚", "·", "✿", "·", "˚", "·", "☆", "·", "˚", "·", "✦", "·", "˚", "·" };

    private static readonly string[] SparkleColors = { Pink, Lavender, Sky, Mint, Peach, Lemon };

    /// <summary>Строка из мерцающих звёздочек и точек.</summary>
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

    /// <summary>Дырочки от переплёта блокнота.</summary>
    public static void Spiral()
    {
        var holes = string.Concat(Enumerable.Repeat("○   ", Math.Max(4, Width / 4))).TrimEnd();
        AnsiConsole.Write(new Align(new Markup($"[{Lavender}]{holes}[/]"), HorizontalAlignment.Center));
    }

    /// <summary>Полоска цветного скотча над карточкой.</summary>
    public static void Tape(string? color = null, int length = 14) =>
        AnsiConsole.Write(new Align(
            new Markup($"[{color ?? Peach}]{new string('▚', length)}[/]"), HorizontalAlignment.Center));

    /// <summary>Пунктирная «строчка» тетради.</summary>
    public static string Dots(int length) => $"[{Cocoa}]{new string('┈', length)}[/]";

    /// <summary>Закладка-лента с названием раздела.</summary>
    public static void Ribbon(string title, string? color = null)
    {
        var c = color ?? Lavender;
        var text = $" ✿ {title} ✿ ";
        var side = Math.Max(2, (Width - text.Length - 4) / 2);
        var bar = new string('═', side);
        AnsiConsole.Write(new Align(
            new Markup($"[{c}]╞{bar}╡{E(text)}╞{bar}╡[/]"), HorizontalAlignment.Center));
    }

    /// <summary>Страничка дневника: рамка (двойная или скруглённая) с названием по центру.</summary>
    public static Panel Page(IRenderable content, string? title = null, string? color = null,
        bool dbl = true, Padding? padding = null)
    {
        var c = color ?? Pink;
        var panel = new Panel(content)
        {
            Border = dbl ? BoxBorder.Double : BoxBorder.Rounded,
            BorderStyle = new Style(FromHex(c)),
            Padding = padding ?? new Padding(2, 0)
        };
        if (title is not null)
            panel.Header = new PanelHeader($"[{c}] {title} [/]", Justify.Center);
        return panel;
    }

    /// <summary>Сегодняшняя дата по-русски (если нет культуры — обычный формат).</summary>
    public static string Today()
    {
        try { return DateTime.Now.ToString("dddd, d MMMM", new CultureInfo("ru-RU")); }
        catch (Exception ex) when (ex is CultureNotFoundException or ArgumentException)
        {
            return DateTime.Now.ToString("dd.MM.yyyy");
        }
    }

    // ------------------------------------------------------------ экраны и сообщения

    public static void Banner()
    {
        AnsiConsole.Clear();
        Spiral();
        Sparkles();
        AnsiConsole.Write(new FigletText("kawaii tasks").Centered().Color(PinkC));
        Ribbon("М Е Н Е Д Ж Е Р   I T - П Р О Е К Т О В");
        AnsiConsole.Write(new Align(
            new Markup($"[{Cocoa}]✎ запись от {E(Today())} ✎[/]"), HorizontalAlignment.Center));
        AnsiConsole.Write(new Align(
            new Markup("[grey]~ уютное пространство для больших планов и маленьких шагов ~[/]"),
            HorizontalAlignment.Center));
        AnsiConsole.Write(new Align(
            new Markup("[grey]↑ ↓ перемещение   ·   Enter — выбрать   ·   Для возврата выбери «Назад»[/]"),
            HorizontalAlignment.Center));
        Sparkles();
        AnsiConsole.WriteLine();
    }

    public static void Ok(string message) =>
        AnsiConsole.MarkupLine($"[{Mint}]  ✧ ✿ {E(message)} ✿ ✧[/]");

    public static void Fail(string message) =>
        AnsiConsole.MarkupLine($"[{Berry}]  (；ω；) ✗ {E(message)}[/]");

    public static void Hint(string message) =>
        AnsiConsole.MarkupLine($"[{Lavender}]  ♪ 「 {E(message)} 」[/]");

    public static void Pause()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Dots(Math.Min(Width, 60)));
        AnsiConsole.Markup($"[grey]нажми любую клавишу, чтобы перевернуть страничку[/] [{Pink}]୨୧[/]");
        try { Console.ReadKey(true); }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or NotSupportedException)
        {
            try { Console.ReadLine(); }
            catch (IOException) { /* Нет интерактивного ввода — продолжаем без паузы. */ }
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

        var art = new Markup($"[{Pink}] (\\_/)  ✧\n (^ᴗ^)/\n (\")_(\")[/]");
        var text = new Markup(
            $"[bold {Pink}]До встречи, дневничок! ♡[/]\n" +
            "[grey]Все странички сохранены здесь:[/]\n" +
            $"[{Cocoa}]{E(storagePath)}[/]\n" +
            $"[{Lavender}]Возвращайся скорее — я буду ждать ✿[/]");

        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();
        grid.AddRow(art, text);

        Tape(Sky);
        AnsiConsole.Write(Page(grid, "~ ПОСЛЕДНЯЯ СТРАНИЧКА ~", Pink, true, new Padding(3, 1)));
        Sparkles();
        AnsiConsole.WriteLine();
        ResetPaper();
    }
}
