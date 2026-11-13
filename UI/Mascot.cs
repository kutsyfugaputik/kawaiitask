using KawaiiTasks.Models;
using Spectre.Console.Rendering;
using KawaiiTasks.Services;
using Spectre.Console;

namespace KawaiiTasks.UI;

public enum Mood { Sleepy, Happy, Worried, Party }

/// <summary>Талисман Моти: меняет настроение в зависимости от состояния проекта.</summary>
public static class Mascot
{
    private static readonly string[] Sleepy =
    {
        "Тут пока пусто… давай добавим первую задачку?",
        "Я вздремну, пока ты придумываешь план ☁"
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
        "УРА! Все задачи готовы! Время тортика 🍰",
        "Проект завершён! Ты просто чудо ✨"
    };

    public static string Face(Mood mood) => mood switch
    {
        Mood.Sleepy => "(－ω－) zzZ",
        Mood.Worried => "(；ω；)",
        Mood.Party => "ヽ(>∀<☆)ノ",
        _ => "(◕‿◕✿)"
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

    /// <summary>Приветствие на главном экране.</summary>
    public static IRenderable Greeting()
    {
        var text = new Markup(
            $"[bold {Theme.Pink}]Привет! Я Моти ♡[/]\n" +
            "[grey]Помогу следить за задачами, дедлайнами и временем.\nВыбери проект или создай новый![/]");

        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();
        grid.AddRow(new Markup($"[{Theme.Pink}]{Theme.E(Face(Mood.Happy))}[/]   "), text);

        return new Panel(grid)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Theme.PinkC),
            Padding = new Padding(2, 0)
        };
    }
}
