using KawaiiTasks.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace KawaiiTasks.UI;

public enum Mood { Sleepy, Happy, Worried, Party }

/// <summary>
/// ASCII-талисман с короткими сообщениями о состоянии проекта.
/// Рисунки состоят только из обычных ASCII-символов: они гарантированно есть в любом шрифте терминала.
/// </summary>
public static class Mascot
{
    private static readonly string[] Sleepy =
    {
        "Тут пока пусто. Давай добавим первую задачу?",
        "Я немного отдохну, пока ты придумываешь план."
    };

    private static readonly string[] Happy =
    {
        "Ты отлично справляешься!",
        "Еще немного - и проект будет готов.",
        "Маленькие шаги тоже приближают к цели.",
        "Задача за задачей - и все получится!"
    };

    private static readonly string[] Worried =
    {
        "Есть просроченные задачи. Проверь сроки.",
        "Дедлайны близко. Давай расставим приоритеты."
    };

    private static readonly string[] Party =
    {
        "Ура! Все задачи готовы! Отличная работа!",
        "Проект завершен. Можно праздновать результат!"
    };

    public static string Face(Mood mood) => mood switch
    {
        Mood.Sleepy => "(-.-) zZ",
        Mood.Worried => "(T_T)",
        Mood.Party => "(^_^)/",
        _ => "(^.^)"
    };

    public static string Art(Mood mood) => mood switch
    {
        Mood.Sleepy => "  /\\_/\\\n ( -.- )\n  >   <",
        Mood.Worried => "  /\\_/\\\n ( T_T )\n  >   <",
        Mood.Party => "  /\\_/\\\n ( ^.^ )\n  > * <",
        _ => "  /\\_/\\\n ( o.o )\n  >   <"
    };

    public static string ColorFor(Mood mood) => mood switch
    {
        Mood.Sleepy => Theme.Lavender,
        Mood.Worried => Theme.Berry,
        Mood.Party => Theme.Lemon,
        _ => Theme.Pink
    };

    /// <summary>Настроение зависит от состояния проекта. Порядок проверок важен: пустой -> всё готово -> есть просрочка.</summary>
    public static Mood MoodFor(Project project)
    {
        if (project.Tasks.Count == 0) return Mood.Sleepy;
        if (project.Tasks.All(task => task.IsDone)) return Mood.Party;
        if (project.Tasks.Any(task => task.IsOverdue)) return Mood.Worried;
        return Mood.Happy;
    }

    /// <summary>Случайная фраза из набора для настроения (Random.Shared безопасен для вызова из любых потоков).</summary>
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
        var art = new Markup($"[{Theme.Pink}]{Theme.E(Art(Mood.Happy))}[/]");
        var text = new Markup(
            $"[bold {Theme.Pink}]Привет! Я Нори-нори[/]\n" +
            "[grey]Твой помощник в мире больших проектов.\n" +
            "Следим за задачами, сроками и временем вместе![/]\n" +
            $"[{Theme.Lavender}]Сегодня все получится. Начнем?[/]\n" +
            $"[{Theme.Cocoa}]------------------------------[/]\n" +
            $"[{Theme.Cocoa}]Это твой личный дневник проектов.[/]");

        // Grid - невидимая таблица без рамок: слева картинка (колонка не переносится, NoWrap),
        // справа текст. Spectre.Console сам считает ширину колонок по ширине окна терминала.
        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();
        grid.AddRow(art, text);

        return Theme.Page(grid, "ДОБРО ПОЖАЛОВАТЬ В ДНЕВНИК", Theme.Pink, true, new Padding(3, 1));
    }
}
