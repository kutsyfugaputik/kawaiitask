using KawaiiTasks.Models;
using Spectre.Console;

namespace KawaiiTasks.UI;

/// <summary>Пастельная палитра и маленькие помощники для красивого вывода.</summary>
public static class Theme
{
    public const string Pink = "#ff9ecb";
    public const string Lavender = "#c3a6ff";
    public const string Mint = "#8fe3c0";
    public const string Peach = "#ffc09f";
    public const string Sky = "#8fd3f4";
    public const string Lemon = "#ffe680";
    public const string Berry = "#ff6f91";

    public static readonly Color PinkC = FromHex(Pink);
    public static readonly Color LavenderC = FromHex(Lavender);
    public static readonly Color MintC = FromHex(Mint);

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

    public static string PriorityTag(Priority p) => $"[{PriorityHex(p)}]{E(p.Text())}[/]";
    public static string StateTag(TaskState s) => $"[{StateHex(s)}]{E(s.Text())}[/]";
    public static string PriorityDot(Priority p) => $"[{PriorityHex(p)}]●[/]";

    public static void Banner()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("KawaiiTasks").Centered().Color(PinkC));
        AnsiConsole.Write(new Rule(
            $"[{Lavender}]✿  М Е Н Е Д Ж Е Р   I T - П Р О Е К Т О В  ✿[/]")
            .RuleStyle(new Style(LavenderC)));
        AnsiConsole.Write(new Align(
            new Markup("[grey]~ уютное пространство для больших планов и маленьких шагов ~[/]"),
            HorizontalAlignment.Center));
        AnsiConsole.WriteLine();
    }

    public static void Ok(string message) => AnsiConsole.MarkupLine($"[{Mint}]✿ {E(message)}[/]");

    public static void Fail(string message) => AnsiConsole.MarkupLine($"[{Berry}](；ω；) {E(message)}[/]");

    public static void Hint(string message) => AnsiConsole.MarkupLine($"[{Lavender}]「 {E(message)} 」[/]");

    public static void Pause()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Markup("[grey]нажми любую клавишу, чтобы вернуться ୨୧[/]");
        try { Console.ReadKey(true); }
        catch (InvalidOperationException) { Console.ReadLine(); }
        AnsiConsole.WriteLine();
    }
}
