using KawaiiTasks.Models;
using Spectre.Console.Rendering;
using KawaiiTasks.Services;
using Spectre.Console;

namespace KawaiiTasks.UI;

public enum Mood { Sleepy, Happy, Worried, Party }

/// <summary>Талисман: чиби-зверюшка, которая меняет настроение в зависимости от состояния проекта.</summary>
public static class Mascot
{
    private static readonly string[] Sleepy =
    {
        "Тут пока пусто… давай добавим первую задачку?",
        "Я немного отдохну, пока ты придумываешь план."
    };

    private static readonly string[] Happy =
    {
        "Ты отлично справляешься! Я в тебя верю ♡",
        "Ещё чуть-чуть — и конфетки твои!",
        "Маленькие шаги — тоже шаги ✿",
        "Задача за задачей — и проект готов!"
    };

    private static readonly string[] Worried =
    {
        "Ой, есть просроченные задачки… но мы всё успеем!",
        "Дедлайны горят, но ты сильнее! (ง •̀_•́)ง"
    };

    private static readonly string[] Party =
    {
        "УРА! Все задачи готовы! Время тортика ♡",
        "Проект завершён! Ты просто чудо ✧"
    };

    public static string Face(Mood mood) => mood switch
    {
        Mood.Sleepy => "(－ω－) zzZ",
        Mood.Worried => "(；ω；)",
        Mood.Party => "ヽ(>∀<☆)ノ",
        _ => "(◕‿◕✿)"
    };

    /// <summary>Рисунок символами (3 строки) для каждого настроения.</summary>
    public static string Art(Mood mood) => mood switch
    {
        Mood.Sleepy =>
            "  /\\_/\\    z\n" +
            " ( -.- )  z\n" +
            "  > ~ <  Z",
        Mood.Worried =>
            " (\\_/)  ;\n" +
            " (T_T)\n" +
            " (\")_(\")",
        Mood.Party =>
            " ✧ /\\_/\\ ✧\n" +
            "  ( ^▽^ )\n" +
            " ☆ > ♡ < ☆",
        _ =>
            "  /\\_/\\\n" +
            " ( ^.^ )\n" +
            "  > ♡ <"
    };

    public static string ColorFor(Mood mood) => mood switch
    {
        Mood.Sleepy => Theme.Lavender,
        Mood.Worried => Theme.Berry,
        Mood.Party => Theme.Lemon,
        _ => Theme.Pink
    };

    public static Mood MoodFor(Project project)
    {
        if (project.Tasks.Count == 0) return Mood.Sleepy;
        if (project.Tasks.All(t => t.IsDone)) return Mood.Party;
        if (project.Tasks.Any(t => t.IsOverdue)) return Mood.Worried;
        return Mood.Happy;
    }

    public static string Phrase(Mood mood)
    {
        var pool = mood switch
        {
            Mood.Sleepy => Sleepy,
            Mood.Worried => Worried,
            Mood.Party => Party,
            _ => Happy
        };
        return pool[Random.Shared.Next(pool.Length)];
    }

    /// <summary>Приветствие на главном экране: обложка дневника с котиком.</summary>
    public static IRenderable Greeting()
    {
        var art = new Markup(
            $"[{Theme.Pink}]  /\\_/\\  ✧\n ( o.o )\n  > ^ <  ♡\n [/][{Theme.Lavender}] ✿ ˚ ✦ ˚ ✿[/]");

        var text = new Markup(
            $"[bold {Theme.Pink}]Привет! Я Нори-нори ୨୧[/]\n" +
            "[grey]Твой маленький помощник в мире больших проектов.\n" +
            "Следим за задачами, дедлайнами и временем вместе![/]\n" +
            $"[{Theme.Lavender}]Сегодня всё получится. Начнём? ✧[/]\n" +
            $"[{Theme.Cocoa}]┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈[/]\n" +
            $"[{Theme.Cocoa}]✎ это твой личный дневник проектов[/]");

        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();
        grid.AddRow(art, text);

        return Theme.Page(grid, "~ ДОБРО ПОЖАЛОВАТЬ В ДНЕВНИК ~", Theme.Pink, true, new Padding(3, 1));
    }
}
