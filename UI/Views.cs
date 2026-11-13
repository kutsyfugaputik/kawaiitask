using KawaiiTasks.Models;
using KawaiiTasks.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace KawaiiTasks.UI;

/// <summary>Отрисовка данных: шапка проекта, доска, таблицы, карточка задачи.</summary>
public static class Views
{
    private static readonly string[] Avatars =
        { "(=^･ω･^=)", "ʕ•ᴥ•ʔ", "(•ᴗ•)", "(｡•ᴗ•｡)", "(⁎˃ᴗ˂⁎)" };

    public static string Fmt(int minutes) =>
        minutes <= 0 ? "—" : $"{minutes / 60}ч {minutes % 60:00}м";

    public static string Date(DateTime? date) =>
        date.HasValue ? date.Value.ToString("dd.MM.yyyy") : "—";

    public static string Short(string text, int max) =>
        text.Length <= max ? text : text.Substring(0, max - 1) + "…";

    private static Table NewTable(string title, string caption)
    {
        var table = new Table
        {
            Border = TableBorder.DoubleEdge,
            BorderStyle = new Style(Theme.LavenderC),
            ShowRowSeparators = true,
            Title = new TableTitle($"[{Theme.Pink}]✿ {title} ✿[/]"),
            Caption = new TableTitle($"[{Theme.Cocoa}]{caption}[/]")
        };
        return table;
    }

    public static void ProjectHeader(Project project)
    {
        var report = ReportService.Build(project);
        var mood = Mascot.MoodFor(project);
        var moodColor = Mascot.ColorFor(mood);
        var (level, levelName) = ReportService.Level(report.CandyPoints);

        const int barWidth = 24;
        var filled = Math.Clamp((int)Math.Round(report.Percent / 100.0 * barWidth), 0, barWidth);
        var end = report.Percent >= 100 ? $" [{Theme.Lemon}]✿[/]" : "";
        var bar = $"[{Theme.Cocoa}]「[/][{Theme.Mint}]{new string('█', filled)}[/][grey]{new string('░', barWidth - filled)}[/][{Theme.Cocoa}]」[/] {report.Percent:0}%{end}";
        var waiting = report.ByState[TaskState.Todo];
        var active = report.ByState[TaskState.InProgress];
        var review = report.ByState[TaskState.Review];
        var done = report.ByState[TaskState.Done];

        var overdue = report.Overdue.Count > 0
            ? $"   [{Theme.Berry}]! Просрочено: {report.Overdue.Count}[/]"
            : "";

        var description = string.IsNullOrWhiteSpace(project.Description)
            ? "[grey]Описание проекта не добавлено[/]"
            : $"[grey]{Theme.E(project.Description)}[/]";

        var chips =
            $"[{Theme.StateHex(TaskState.Todo)}]「○ {waiting}」[/] " +
            $"[{Theme.StateHex(TaskState.InProgress)}]「◐ {active}」[/] " +
            $"[{Theme.StateHex(TaskState.Review)}]「◑ {review}」[/] " +
            $"[{Theme.StateHex(TaskState.Done)}]「✿ {done}」[/]  [grey]всего: {report.Total}[/]";

        var text = new Markup(
            $"[bold {Theme.Pink}]{Theme.E(project.Name)}[/]\n" +
            $"{description}\n" +
            $"[{Theme.Cocoa}]┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈[/]\n" +
            $"{bar}\n" +
            $"{chips}\n" +
            $"[{Theme.Lemon}]◇ {report.CandyPoints}[/] очков · уровень {level}: {Theme.E(levelName)}   [{Theme.Sky}]◷ {Fmt(report.TotalMinutes)}[/]{overdue}");

        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();
        grid.AddRow(new Markup($"[{moodColor}]{Theme.E(Mascot.Art(mood))}[/]   "), text);

        Theme.Tape(Theme.Sky, 10);
        AnsiConsole.Write(Theme.Page(grid, "~ СТРАНИЧКА ПРОЕКТА ~", Theme.Pink));

        var bubble = new Panel(new Markup($"[{Theme.Lavender}]♪ {Theme.E(Mascot.Phrase(mood))}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Theme.FromHex(moodColor)),
            Padding = new Padding(1, 0)
        };
        AnsiConsole.Write(new Padder(bubble, new Padding(4, 0, 0, 0)));
        AnsiConsole.WriteLine();
    }

    /// <summary>Канбан-доска: по колонке на каждый статус.</summary>
    public static void Board(Project project, IReadOnlyCollection<TaskItem> tasks)
    {
        Theme.Ribbon("ДОСКА ЗАДАЧ", Theme.Lavender);

        var grid = new Grid { Expand = true };
        var states = Enum.GetValues<TaskState>();
        for (var i = 0; i < states.Length; i++) grid.AddColumn();

        var cells = new List<IRenderable>();
        foreach (var state in states)
        {
            var items = tasks.Where(t => t.State == state)
                             .OrderByDescending(t => t.Priority)
                             .ThenBy(t => t.Id)
                             .ToList();

            var lines = new List<IRenderable>();
            if (items.Count == 0)
            {
                lines.Add(new Markup("[grey]— пока пусто —[/]"));
                lines.Add(new Markup("[grey] ( ˘ω˘ ) zzZ[/]"));
            }

            for (var n = 0; n < items.Count; n++)
            {
                var t = items[n];
                var warn = t.IsOverdue ? $"[{Theme.Berry}]![/]" : "";
                var due = t.Deadline.HasValue ? $" · ▦ {Date(t.Deadline)}" : "";
                lines.Add(new Markup($"{Theme.PriorityDot(t.Priority)} {warn}[bold]#{t.Id}[/] {Theme.E(Short(t.Title, 24))}"));
                lines.Add(new Markup($"[grey]  {Theme.E(project.AssigneeName(t))}{due}[/]"));
                lines.Add(new Markup(n < items.Count - 1 ? Theme.Dots(22) : " "));
            }

            cells.Add(new Panel(new Rows(lines))
            {
                Header = new PanelHeader($" {Theme.StateLabel(state)} [grey]({items.Count})[/] ", Justify.Center),
                Border = BoxBorder.Double,
                BorderStyle = new Style(Theme.FromHex(Theme.StateHex(state))),
                Expand = true
            });
        }

        grid.AddRow(cells.ToArray());
        AnsiConsole.Write(grid);
        Theme.Sparkles(40);
    }

    public static void TaskTable(Project project, IEnumerable<TaskItem> tasks)
    {
        var list = tasks.ToList();
        if (list.Count == 0)
        {
            Theme.Hint("Ничего не найдено. Попробуй изменить условия поиска.");
            return;
        }

        var table = NewTable("Список задач", $"· всего записей: {list.Count} ·");
        table.Expand = true;

        foreach (var header in new[] { "ID", "Задача", "Тип", "Приоритет", "Статус", "Исполнитель", "Срок", "Время" })
            table.AddColumn(new TableColumn($"[bold {Theme.Pink}]{header}[/]"));

        foreach (var t in list)
        {
            var due = t.IsOverdue ? $"[{Theme.Berry}]! {Date(t.Deadline)}[/]" : Date(t.Deadline);
            table.AddRow(
                $"[{Theme.Cocoa}]#{t.Id}[/]",
                Theme.E(Short(t.Title, 40)),
                Theme.E(t.Kind.Text()),
                Theme.PriorityLabel(t.Priority),
                Theme.StateLabel(t.State),
                Theme.E(project.AssigneeName(t)),
                due,
                Fmt(t.SpentMinutes));
        }

        AnsiConsole.Write(table);
    }

    public static void TaskCard(Project project, TaskItem task)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();

        void Row(string key, string value) =>
            grid.AddRow(new Markup($"[{Theme.Lavender}]{key}[/]   "), new Markup(value));

        var due = task.IsOverdue ? $"[{Theme.Berry}]! {Date(task.Deadline)} (просрочена)[/]" : Date(task.Deadline);

        Row("✎ Задача", $"[bold]#{task.Id} {Theme.E(task.Title)}[/]");
        Row("「 」 Описание", string.IsNullOrWhiteSpace(task.Description) ? "[grey]—[/]" : Theme.E(task.Description));
        Row("✧ Тип", Theme.E(task.Kind.Text()));
        Row("★ Приоритет", Theme.PriorityLabel(task.Priority));
        Row("↻ Статус", Theme.StateLabel(task.State));
        Row("♙ Исполнитель", Theme.E(project.AssigneeName(task)));
        Row("▦ Срок", due);
        Row("◷ Затрачено", Fmt(task.SpentMinutes));

        Theme.Tape(Theme.Peach);
        AnsiConsole.Write(Theme.Page(grid, "୨୧ СТРАНИЧКА ЗАДАЧИ ୨୧", Theme.Pink));

        if (task.TimeLog.Count == 0) return;

        var log = NewTable("Записи времени", $"· записей: {task.TimeLog.Count} ·");
        log.AddColumn($"[bold {Theme.Pink}]Дата[/]");
        log.AddColumn($"[bold {Theme.Pink}]Время[/]");
        log.AddColumn($"[bold {Theme.Pink}]Заметка[/]");
        foreach (var entry in task.TimeLog.OrderBy(e => e.Date))
            log.AddRow(entry.Date.ToString("dd.MM.yyyy"), Fmt(entry.Minutes), Theme.E(entry.Note));
        AnsiConsole.Write(log);
    }

    public static void TeamTable(Project project)
    {
        if (project.Team.Count == 0)
        {
            Theme.Hint("Команда пока пустая. Добавь первого участника в меню «Команда».");
            return;
        }

        var table = NewTable("Наша команда", $"· участников: {project.Team.Count} ·");
        foreach (var header in new[] { "", "ID", "Имя", "Роль", "Задач", "Готово", "Время" })
            table.AddColumn(new TableColumn($"[bold {Theme.Pink}]{header}[/]"));

        foreach (var m in project.Team)
        {
            var mine = project.Tasks.Where(t => t.AssigneeId == m.Id).ToList();
            var avatar = Avatars[Math.Abs(m.Id.GetHashCode()) % Avatars.Length];
            table.AddRow($"[{Theme.Lavender}]{Theme.E(avatar)}[/]", $"{m.Id}", Theme.E(m.Name), Theme.E(m.Role),
                $"{mine.Count}", $"{mine.Count(t => t.IsDone)}", Fmt(mine.Sum(t => t.SpentMinutes)));
        }

        AnsiConsole.Write(table);
    }
}
