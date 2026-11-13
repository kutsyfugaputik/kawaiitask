using KawaiiTasks.Models;
using KawaiiTasks.Services;
using Spectre.Console;

namespace KawaiiTasks.UI;

/// <summary>Красивый отчёт о проекте прямо в консоли — как разворот дневника.</summary>
public class ConsoleReportWriter : IReportWriter
{
    public string Name => "Консоль";

    public string? Write(ProjectReport r)
    {
        var project = r.Project;

        Theme.Spiral();
        Theme.Sparkles();
        Theme.Ribbon($"ОТЧЁТ: {project.Name}", Theme.Pink);
        AnsiConsole.Write(new Align(
            new Markup($"[{Theme.Cocoa}]✎ запись от {Theme.E(Theme.Today())} ✎[/]"), HorizontalAlignment.Center));
        AnsiConsole.WriteLine();

        var (level, levelName) = ReportService.Level(r.CandyPoints);
        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();

        void Row(string key, string value) =>
            grid.AddRow(new Markup($"[{Theme.Lavender}]{key}[/]   "), new Markup(value));

        const int barWidth = 24;
        var filled = Math.Clamp((int)Math.Round(r.Percent / 100.0 * barWidth), 0, barWidth);
        var bar = $"[{Theme.Cocoa}]「[/][{Theme.Mint}]{new string('█', filled)}[/][grey]{new string('░', barWidth - filled)}[/][{Theme.Cocoa}]」[/]";

        Row("✎ Всего задач", $"{r.Total}");
        Row("✿ Выполнено", $"{r.Done} ({r.Percent:0}%)");
        Row("↻ Прогресс", bar);
        Row("! Просрочено", r.Overdue.Count > 0 ? $"[{Theme.Berry}]{r.Overdue.Count}[/]" : "0");
        Row("♙ Без исполнителя", $"{r.Unassigned}");
        Row("◷ Затрачено времени", Views.Fmt(r.TotalMinutes));
        Row("◇ Конфетки", $"{r.CandyPoints} · уровень {level}: {Theme.E(levelName)}");
        Row("▦ Сформирован", r.GeneratedAt.ToString("dd.MM.yyyy HH:mm"));

        Theme.Tape(Theme.Sky);
        AnsiConsole.Write(Theme.Page(grid, "~ СВОДКА ~", Theme.Pink));
        AnsiConsole.WriteLine();

        if (r.Total > 0)
        {
            var states = new BarChart()
                .Width(60)
                .Label($"[{Theme.Pink}]✧ Задачи по статусам ✧[/]")
                .CenterLabel();
            foreach (var pair in r.ByState)
                states.AddItem(pair.Key.Text(), pair.Value, Theme.FromHex(Theme.StateHex(pair.Key)));
            AnsiConsole.Write(states);
            AnsiConsole.WriteLine();

            var priorities = new BarChart()
                .Width(60)
                .Label($"[{Theme.Pink}]★ Задачи по приоритетам ★[/]")
                .CenterLabel();
            foreach (var pair in r.ByPriority)
                priorities.AddItem(pair.Key.Text(), pair.Value, Theme.FromHex(Theme.PriorityHex(pair.Key)));
            AnsiConsole.Write(priorities);
            AnsiConsole.WriteLine();
        }

        if (r.Members.Count > 0)
        {
            var table = new Table
            {
                Border = TableBorder.DoubleEdge,
                BorderStyle = new Style(Theme.LavenderC),
                ShowRowSeparators = true
            };
            foreach (var header in new[] { "Участник", "Роль", "Задач", "Готово", "Время" })
                table.AddColumn(new TableColumn($"[bold {Theme.Pink}]{header}[/]"));
            foreach (var m in r.Members)
                table.AddRow(Theme.E(m.Name), Theme.E(m.Role), $"{m.Assigned}", $"{m.Done}", Views.Fmt(m.Minutes));
            AnsiConsole.Write(Theme.Page(table, "♧ КОМАНДА ♧", Theme.Lavender, false));
        }

        if (r.Overdue.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[{Theme.Berry}]╭─ ! Просроченные задачи ─╮[/]");
            foreach (var t in r.Overdue)
                AnsiConsole.MarkupLine($"[{Theme.Berry}]│[/] ✗ #{t.Id} {Theme.E(t.Title)} — срок {Views.Date(t.Deadline)}, {Theme.E(project.AssigneeName(t))}");
            AnsiConsole.MarkupLine($"[{Theme.Berry}]╰┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈╯[/]");
        }

        if (r.TopByTime.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[{Theme.Lavender}]╭─ ◷ Больше всего времени ушло на ─╮[/]");
            foreach (var t in r.TopByTime)
                AnsiConsole.MarkupLine($"[{Theme.Lavender}]│[/] ✧ #{t.Id} {Theme.E(t.Title)} — {Views.Fmt(t.SpentMinutes)}");
            AnsiConsole.MarkupLine($"[{Theme.Lavender}]╰┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈┈╯[/]");
        }

        AnsiConsole.WriteLine();
        Theme.Sparkles();
        AnsiConsole.Write(new Align(
            new Markup($"[{Theme.Pink}](\\_/)  конец странички  ✿\n(^ᴗ^)/\n(\")_(\")[/]"),
            HorizontalAlignment.Center));

        return null;
    }
}
