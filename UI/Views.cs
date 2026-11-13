using KawaiiTasks.Models;
using KawaiiTasks.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace KawaiiTasks.UI;

/// <summary>Отрисовка данных: шапка проекта, доска, таблицы, карточка задачи.</summary>
public static class Views
{
    public static string Fmt(int minutes) =>
        minutes <= 0 ? "—" : $"{minutes / 60}ч {minutes % 60:00}м";

    public static string Date(DateTime? date) =>
        date.HasValue ? date.Value.ToString("dd.MM.yyyy") : "—";

    public static string Short(string text, int max) =>
        text.Length <= max ? text : text.Substring(0, max - 1) + "…";

    public static void ProjectHeader(Project project)
    {
        var report = ReportService.Build(project);
        var mood = Mascot.MoodFor(project);
        var (level, levelName) = ReportService.Level(report.CandyPoints);

        var filled = (int)Math.Round(report.Percent / 100.0 * 20);
        var bar = $"[{Theme.Mint}]{new string('█', filled)}[/][grey]{new string('░', 20 - filled)}[/] {report.Percent:0}%";

        var overdue = report.Overdue.Count > 0
            ? $"   [{Theme.Berry}]❗ просрочено: {report.Overdue.Count}[/]"
            : "";

        var text = new Markup(
            $"[bold {Theme.Pink}]{Theme.E(project.Name)}[/]\n" +
            $"[grey]{Theme.E(project.Description)}[/]\n" +
            $"{bar}\n" +
            $"🍬 {report.CandyPoints} конфеток · уровень {level}: {Theme.E(levelName)}   ⏰ {Fmt(report.TotalMinutes)}{overdue}");

        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();
        grid.AddRow(new Markup($"[{Theme.Pink}]{Theme.E(Mascot.Face(mood))}[/]   "), text);

        AnsiConsole.Write(new Panel(grid)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Theme.PinkC),
            Padding = new Padding(2, 0)
        });
        AnsiConsole.MarkupLine($"  [{Theme.Lavender}]💬 {Theme.E(Mascot.Phrase(mood))}[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>Канбан-доска: по колонке на каждый статус.</summary>
    public static void Board(Project project, IReadOnlyCollection<TaskItem> tasks)
    {
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
                lines.Add(new Markup("[grey]пусто (´･ω･`)[/]"));

            foreach (var t in items)
            {
                var warn = t.IsOverdue ? $"[{Theme.Berry}]❗[/]" : "";
                var due = t.Deadline.HasValue ? $" · 📅 {Date(t.Deadline)}" : "";
                lines.Add(new Markup($"{Theme.PriorityDot(t.Priority)} {warn}[bold]#{t.Id}[/] {Theme.E(Short(t.Title, 24))}"));
                lines.Add(new Markup($"[grey]  {Theme.E(project.AssigneeName(t))}{due}[/]"));
                lines.Add(new Markup(" "));
            }

            cells.Add(new Panel(new Rows(lines))
            {
                Header = new PanelHeader($"{Theme.StateTag(state)} ({items.Count})"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Theme.FromHex(Theme.StateHex(state))),
                Expand = true
            });
        }

        grid.AddRow(cells.ToArray());
        AnsiConsole.Write(grid);
    }

    public static void TaskTable(Project project, IEnumerable<TaskItem> tasks)
    {
        var list = tasks.ToList();
        if (list.Count == 0)
        {
            Theme.Hint("Ничего не найдено (´･ω･`)");
            return;
        }

        var table = new Table
        {
            Border = TableBorder.Rounded,
            BorderStyle = new Style(Theme.LavenderC),
            Expand = true
        };

        foreach (var header in new[] { "ID", "Задача", "Тип", "Приоритет", "Статус", "Исполнитель", "Срок", "Время" })
            table.AddColumn(new TableColumn($"[bold {Theme.Pink}]{header}[/]"));

        foreach (var t in list)
        {
            var due = t.IsOverdue ? $"[{Theme.Berry}]❗ {Date(t.Deadline)}[/]" : Date(t.Deadline);
            table.AddRow(
                $"#{t.Id}",
                Theme.E(Short(t.Title, 40)),
                Theme.E(t.Kind.Text()),
                Theme.PriorityTag(t.Priority),
                Theme.StateTag(t.State),
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

        var due = task.IsOverdue ? $"[{Theme.Berry}]❗ {Date(task.Deadline)} (просрочена)[/]" : Date(task.Deadline);

        Row("Задача", $"[bold]#{task.Id} {Theme.E(task.Title)}[/]");
        Row("Описание", string.IsNullOrWhiteSpace(task.Description) ? "[grey]—[/]" : Theme.E(task.Description));
        Row("Тип", Theme.E(task.Kind.Text()));
        Row("Приоритет", Theme.PriorityTag(task.Priority));
        Row("Статус", Theme.StateTag(task.State));
        Row("Исполнитель", Theme.E(project.AssigneeName(task)));
        Row("Срок", due);
        Row("Затрачено", Fmt(task.SpentMinutes));

        AnsiConsole.Write(new Panel(grid)
        {
            Header = new PanelHeader($"[{Theme.Pink}]🎀 Карточка задачи[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Theme.PinkC),
            Padding = new Padding(2, 0)
        });

        if (task.TimeLog.Count == 0) return;

        var log = new Table { Border = TableBorder.Rounded, BorderStyle = new Style(Theme.LavenderC) };
        log.AddColumn("Дата");
        log.AddColumn("Время");
        log.AddColumn("Заметка");
        foreach (var entry in task.TimeLog.OrderBy(e => e.Date))
            log.AddRow(entry.Date.ToString("dd.MM.yyyy"), Fmt(entry.Minutes), Theme.E(entry.Note));
        AnsiConsole.Write(log);
    }

    public static void TeamTable(Project project)
    {
        if (project.Team.Count == 0)
        {
            Theme.Hint("В команде пока никого нет (´･ω･`)");
            return;
        }

        var table = new Table { Border = TableBorder.Rounded, BorderStyle = new Style(Theme.LavenderC) };
        foreach (var header in new[] { "ID", "Имя", "Роль", "Задач", "Готово", "Время" })
            table.AddColumn(new TableColumn($"[bold {Theme.Pink}]{header}[/]"));

        foreach (var m in project.Team)
        {
            var mine = project.Tasks.Where(t => t.AssigneeId == m.Id).ToList();
            table.AddRow($"{m.Id}", Theme.E(m.Name), Theme.E(m.Role),
                $"{mine.Count}", $"{mine.Count(t => t.IsDone)}", Fmt(mine.Sum(t => t.SpentMinutes)));
        }

        AnsiConsole.Write(table);
    }
}
