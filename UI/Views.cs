using KawaiiTasks.Models;
using KawaiiTasks.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace KawaiiTasks.UI;

/// <summary>
/// Отрисовка данных: шапка проекта, доска, таблицы, карточка задачи.
/// Здесь нет бизнес-логики, только превращение данных в красивый текст. Spectre.Console строит из объектов
/// (Table, Grid, Panel, BarChart) обычный текст с цветами и сам подгоняет его под ширину окна терминала.
/// </summary>
public static class Views
{
    private static readonly string[] Avatars =
        { "(=^.w.^=)", "(*u*)", "(o.o)", "(.*u*.)", "(*>u<*)" };

    /// <summary>135 минут -> "2ч 15м"; ноль и меньше -> "-".</summary>
    public static string Fmt(int minutes) =>
        minutes <= 0 ? "-" : $"{minutes / 60}ч {minutes % 60:00}м";

    public static string Date(DateTime? date) =>
        date.HasValue ? date.Value.ToString("dd.MM.yyyy") : "-";

    /// <summary>Обрезает текст до max символов с многоточием, чтобы длинные названия не ломали ширину таблицы.</summary>
    public static string Short(string text, int max) =>
        max <= 0 ? "" : text.Length <= max ? text : max <= 3 ? text[..max] : text.Substring(0, max - 3) + "...";

    /// <summary>Таблица в едином стиле: ASCII-рамка (работает в любом шрифте), линии между строками, заголовок и подпись.</summary>
    private static Table NewTable(string title, string caption)
    {
        var table = new Table
        {
            Border = TableBorder.Ascii,
            BorderStyle = new Style(Theme.LavenderC),
            ShowRowSeparators = true,
            Title = new TableTitle($"[{Theme.Pink}]* {title} *[/]"),
            Caption = new TableTitle($"[{Theme.Cocoa}]{caption}[/]")
        };
        return table;
    }

    /// <summary>Шапка проекта: талисман с настроением, прогресс-бар, счётчики по статусам и баллы.</summary>
    public static void ProjectHeader(Project project)
    {
        var report = ReportService.Build(project);
        var mood = Mascot.MoodFor(project);
        var moodColor = Mascot.ColorFor(mood);
        var (level, levelName) = ReportService.Level(report.CandyPoints);

        // Прогресс-бар из символов: сколько "#" закрашено, столько и процентов выполнено (ширина бара - 24 символа).
        // Math.Clamp не даёт выйти за границы, даже если из-за округления получилось число больше 24.
        const int barWidth = 24;
        var filled = Math.Clamp((int)Math.Round(report.Percent / 100.0 * barWidth), 0, barWidth);
        var end = report.Percent >= 100 ? $" [{Theme.Lemon}]*[/]" : "";
        var bar = $"[{Theme.Cocoa}]|[/][{Theme.Mint}]{new string('#', filled)}[/][grey]{new string('-', barWidth - filled)}[/][{Theme.Cocoa}]|[/] {report.Percent:0}%{end}";
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
            $"[{Theme.StateHex(TaskState.Todo)}]({waiting})[/] " +
            $"[{Theme.StateHex(TaskState.InProgress)}]({active})[/] " +
            $"[{Theme.StateHex(TaskState.Review)}]({review})[/] " +
            $"[{Theme.StateHex(TaskState.Done)}]({done})[/]  [grey]всего: {report.Total}[/]";

        var text = new Markup(
            $"[bold {Theme.Pink}]{Theme.E(project.Name)}[/]\n" +
            $"{description}\n" +
            $"[{Theme.Cocoa}]------------------------------[/]\n" +
            $"{bar}\n" +
            $"{chips}\n" +
            $"[{Theme.Lemon}]{report.CandyPoints}[/] баллов | уровень {level}: {Theme.E(levelName)}   [{Theme.Sky}]Время: {Fmt(report.TotalMinutes)}[/]{overdue}");

        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();
        grid.AddRow(new Markup($"[{moodColor}]{Theme.E(Mascot.Art(mood))}[/]   "), text);

        Theme.Tape(Theme.Sky, 10);
        AnsiConsole.Write(Theme.Page(grid, "~ СТРАНИЧКА ПРОЕКТА ~", Theme.Pink));

        var bubble = new Panel(new Markup($"[{Theme.Lavender}]~ {Theme.E(Mascot.Phrase(mood))}[/]"))
        {
            Border = BoxBorder.Ascii,
            BorderStyle = new Style(Theme.FromHex(moodColor)),
            Padding = new Padding(1, 0)
        };
        AnsiConsole.Write(new Align(bubble, HorizontalAlignment.Center));
        AnsiConsole.WriteLine();
    }

    /// <summary>Канбан-доска: по колонке на каждый статус.</summary>
    public static void Board(Project project, IReadOnlyCollection<TaskItem> tasks)
    {
        Theme.Ribbon("ДОСКА ЗАДАЧ", Theme.Lavender);

        var states = Enum.GetValues<TaskState>();
        var cards = new List<IRenderable>();
        foreach (var state in states)
        {
            var items = tasks.Where(t => t.State == state)
                             .OrderByDescending(t => t.Priority)
                             .ThenBy(t => t.Id)
                             .ToList();

            var lines = new List<IRenderable>();
            if (items.Count == 0)
            {
                lines.Add(new Markup("[grey]- пока пусто -[/]"));
                lines.Add(new Markup("[grey]  ( . . )  zzz[/]"));
            }

            for (var n = 0; n < items.Count; n++)
            {
                var task = items[n];
                var warn = task.IsOverdue ? $"[{Theme.Berry}]![/]" : "";
                var due = task.Deadline.HasValue ? $" | срок: {Date(task.Deadline)}" : "";
                lines.Add(new Markup($"{Theme.PriorityDot(task.Priority)} {warn}[bold]#{task.Id}[/] {Theme.E(Short(task.Title, 24))}"));
                lines.Add(new Markup($"[grey]  {Theme.E(project.AssigneeName(task))}{Theme.E(due)}[/]"));
                lines.Add(new Markup(n < items.Count - 1 ? Theme.Dots(22) : " "));
            }

            cards.Add(new Panel(new Rows(lines))
            {
                Header = new PanelHeader($" {Theme.StateLabel(state)} [grey]({items.Count})[/]", Justify.Center),
                Border = BoxBorder.Ascii,
                BorderStyle = new Style(Theme.FromHex(Theme.StateHex(state))),
                Expand = true
            });
        }

        // На небольшом окне четыре колонки становятся слишком узкими, поэтому
        // доска переключается в вертикальный вид без горизонтальной прокрутки.
        // Горизонтальной прокрутки в терминале нет: что не влезло в ширину, переносится или обрезается.
        if (Theme.Width >= 108)
        {
            var grid = new Grid { Expand = true };
            foreach (var _ in states) grid.AddColumn();
            grid.AddRow(cards.ToArray());
            AnsiConsole.Write(new Align(grid, HorizontalAlignment.Center));
        }
        else
        {
            foreach (var card in cards)
                AnsiConsole.Write(new Align(card, HorizontalAlignment.Center));
        }

        Theme.Sparkles(Math.Min(40, Theme.Width - 4));
    }

    /// <summary>Таблица задач. Expand = true растягивает таблицу на всю ширину окна.</summary>
    public static void TaskTable(Project project, IEnumerable<TaskItem> tasks)
    {
        var list = tasks.ToList();
        if (list.Count == 0)
        {
            Theme.Hint("Ничего не найдено. Попробуй изменить условия поиска.");
            return;
        }

        var table = NewTable("Список задач", $"| всего записей: {list.Count} |");
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

    /// <summary>Подробная карточка одной задачи + таблица записей времени (если они есть).</summary>
    public static void TaskCard(Project project, TaskItem task)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();

        // Локальная функция: добавляет строку "ключ - значение". Ключ экранируем (в нём есть квадратные скобки),
        // значение приходит уже готовой разметкой, поэтому его экранирует вызывающий код.
        void Row(string key, string value) =>
            grid.AddRow(new Markup($"[{Theme.Lavender}]{Theme.E(key)}[/]   "), new Markup(value));

        var due = task.IsOverdue ? $"[{Theme.Berry}]! {Date(task.Deadline)} (просрочена)[/]" : Date(task.Deadline);

        Row("> Задача", $"[bold]#{task.Id} {Theme.E(task.Title)}[/]");
        Row("[ ] Описание", string.IsNullOrWhiteSpace(task.Description) ? "[grey]-[/]" : Theme.E(task.Description));
        Row("* Тип", Theme.E(task.Kind.Text()));
        Row("* Приоритет", Theme.PriorityLabel(task.Priority));
        Row("Статус", Theme.StateLabel(task.State));
        Row("[участник] Исполнитель", Theme.E(project.AssigneeName(task)));
        Row("[#] Срок", due);
        Row("Затрачено времени", Fmt(task.SpentMinutes));

        Theme.Tape(Theme.Peach);
        AnsiConsole.Write(Theme.Page(grid, "[*] СТРАНИЧКА ЗАДАЧИ [*]", Theme.Pink));

        if (task.TimeLog.Count == 0) return;

        var log = NewTable("Записи времени", $"| записей: {task.TimeLog.Count} |");
        log.AddColumn($"[bold {Theme.Pink}]Дата[/]");
        log.AddColumn($"[bold {Theme.Pink}]Время[/]");
        log.AddColumn($"[bold {Theme.Pink}]Заметка[/]");
        foreach (var entry in task.TimeLog.OrderBy(e => e.Date))
            log.AddRow(entry.Date.ToString("dd.MM.yyyy"), Fmt(entry.Minutes), Theme.E(entry.Note));
        AnsiConsole.Write(log);
    }

    /// <summary>Таблица команды с аватарками-смайликами; аватар выбирается по ID участника, поэтому у человека он всегда один и тот же.</summary>
    public static void TeamTable(Project project)
    {
        if (project.Team.Count == 0)
        {
            Theme.Hint("Команда пока пустая. Добавь первого участника в меню 'Команда'.");
            return;
        }

        var table = NewTable("Наша команда", $"| участников: {project.Team.Count} |");
        foreach (var header in new[] { "", "ID", "Имя", "Роль", "Задач", "Готово", "Время" })
            table.AddColumn(new TableColumn($"[bold {Theme.Pink}]{header}[/]"));

        foreach (var m in project.Team)
        {
            var mine = project.Tasks.Where(t => t.AssigneeId == m.Id).ToList();
            var avatar = Avatars[(int)(Math.Abs((long)m.Id) % Avatars.Length)];
            table.AddRow($"[{Theme.Lavender}]{Theme.E(avatar)}[/]", $"{m.Id}", Theme.E(m.Name), Theme.E(m.Role),
                $"{mine.Count}", $"{mine.Count(t => t.IsDone)}", Fmt(mine.Sum(t => t.SpentMinutes)));
        }

        AnsiConsole.Write(new Align(table, HorizontalAlignment.Center));
    }
}
