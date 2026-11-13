using KawaiiTasks.Models;
using KawaiiTasks.Services;
using Spectre.Console;

namespace KawaiiTasks.UI;

/// <summary>Красивый отчёт о проекте прямо в консоли.</summary>
public class ConsoleReportWriter : IReportWriter
{
    public string Name => "Консоль";

    public string? Write(ProjectReport r)
    {
        var project = r.Project;

        AnsiConsole.Write(new Rule($"[{Theme.Pink}]🎀 Отчёт о проекте: {Theme.E(project.Name)}[/]")
            .RuleStyle(new Style(Theme.LavenderC)));
        AnsiConsole.WriteLine();

        var (level, levelName) = ReportService.Level(r.CandyPoints);
        var grid = new Grid();
        grid.AddColumn(new GridColumn { NoWrap = true });
        grid.AddColumn();

        void Row(string key, string value) =>
            grid.AddRow(new Markup($"[{Theme.Lavender}]{key}[/]   "), new Markup(value));

        Row("Всего задач", $"{r.Total}");
        Row("Выполнено", $"{r.Done} ({r.Percent:0}%)");
        Row("Просрочено", r.Overdue.Count > 0 ? $"[{Theme.Berry}]{r.Overdue.Count}[/]" : "0");
        Row("Без исполнителя", $"{r.Unassigned}");
        Row("Затрачено времени", Views.Fmt(r.TotalMinutes));
        Row("Конфетки", $"🍬 {r.CandyPoints} · уровень {level}: {Theme.E(levelName)}");
        Row("Сформирован", r.GeneratedAt.ToString("dd.MM.yyyy HH:mm"));

        AnsiConsole.Write(new Panel(grid)
        {
            Header = new PanelHeader($"[{Theme.Pink}]Сводка[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Theme.PinkC),
            Padding = new Padding(2, 0)
        });

        if (r.Total > 0)
        {
            var states = new BarChart()
                .Width(60)
                .Label($"[{Theme.Pink}]Задачи по статусам[/]")
                .CenterLabel();
            foreach (var pair in r.ByState)
                states.AddItem(pair.Key.Text(), pair.Value, Theme.FromHex(Theme.StateHex(pair.Key)));
            AnsiConsole.Write(states);
            AnsiConsole.WriteLine();

            var priorities = new BarChart()
                .Width(60)
                .Label($"[{Theme.Pink}]Задачи по приоритетам[/]")
                .CenterLabel();
            foreach (var pair in r.ByPriority)
                priorities.AddItem(pair.Key.Text(), pair.Value, Theme.FromHex(Theme.PriorityHex(pair.Key)));
            AnsiConsole.Write(priorities);
            AnsiConsole.WriteLine();
        }

        if (r.Members.Count > 0)
        {
            var table = new Table { Border = TableBorder.Rounded, BorderStyle = new Style(Theme.LavenderC) };
            foreach (var header in new[] { "Участник", "Роль", "Задач", "Готово", "Время" })
                table.AddColumn(new TableColumn($"[bold {Theme.Pink}]{header}[/]"));
            foreach (var m in r.Members)
                table.AddRow(Theme.E(m.Name), Theme.E(m.Role), $"{m.Assigned}", $"{m.Done}", Views.Fmt(m.Minutes));
            AnsiConsole.Write(new Panel(table)
            {
                Header = new PanelHeader($"[{Theme.Pink}]👥 Команда[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Theme.LavenderC)
            });
        }

        if (r.Overdue.Count > 0)
        {
            AnsiConsole.MarkupLine($"[{Theme.Berry}]❗ Просроченные задачи:[/]");
            foreach (var t in r.Overdue)
                AnsiConsole.MarkupLine($"  • #{t.Id} {Theme.E(t.Title)} — срок {Views.Date(t.Deadline)}, {Theme.E(project.AssigneeName(t))}");
            AnsiConsole.WriteLine();
        }

        if (r.TopByTime.Count > 0)
        {
            AnsiConsole.MarkupLine($"[{Theme.Lavender}]⏰ Больше всего времени ушло на:[/]");
            foreach (var t in r.TopByTime)
                AnsiConsole.MarkupLine($"  • #{t.Id} {Theme.E(t.Title)} — {Views.Fmt(t.SpentMinutes)}");
        }

        return null;
    }
}
