using System.Text;
using KawaiiTasks.Models;

namespace KawaiiTasks.Services;

/// <summary>Сохраняет отчёт о проекте в Markdown-файл.</summary>
public class MarkdownReportWriter : IReportWriter
{
    private readonly string _folder;

    public MarkdownReportWriter(string folder) => _folder = folder;

    public string Name => "Markdown-файл";

    public string? Write(ProjectReport r)
    {
        var sb = new StringBuilder();
        var p = r.Project;

        sb.AppendLine($"# 🎀 Отчёт о проекте: {p.Name}");
        sb.AppendLine();
        sb.AppendLine($"_Сформирован: {r.GeneratedAt:dd.MM.yyyy HH:mm}_");
        if (!string.IsNullOrWhiteSpace(p.Description))
        {
            sb.AppendLine();
            sb.AppendLine($"> {p.Description}");
        }

        sb.AppendLine();
        sb.AppendLine("## Сводка");
        sb.AppendLine();
        sb.AppendLine("| Показатель | Значение |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| Всего задач | {r.Total} |");
        sb.AppendLine($"| Выполнено | {r.Done} ({r.Percent:0}%) |");
        sb.AppendLine($"| Просрочено | {r.Overdue.Count} |");
        sb.AppendLine($"| Без исполнителя | {r.Unassigned} |");
        sb.AppendLine($"| Затрачено времени | {FormatMinutes(r.TotalMinutes)} |");
        var (level, levelName) = ReportService.Level(r.CandyPoints);
        sb.AppendLine($"| Конфетки 🍬 | {r.CandyPoints} (уровень {level}: {levelName}) |");

        sb.AppendLine();
        sb.AppendLine("## Задачи по статусам");
        sb.AppendLine();
        sb.AppendLine("| Статус | Количество |");
        sb.AppendLine("|---|---|");
        foreach (var pair in r.ByState)
            sb.AppendLine($"| {pair.Key.Text()} | {pair.Value} |");

        if (r.Members.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Команда");
            sb.AppendLine();
            sb.AppendLine("| Участник | Роль | Задач | Готово | Время |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var m in r.Members)
                sb.AppendLine($"| {Cell(m.Name)} | {Cell(m.Role)} | {m.Assigned} | {m.Done} | {FormatMinutes(m.Minutes)} |");
        }

        if (r.Overdue.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## ❗ Просроченные задачи");
            sb.AppendLine();
            foreach (var t in r.Overdue)
                sb.AppendLine($"- #{t.Id} {t.Title} — срок {t.Deadline:dd.MM.yyyy}, исполнитель: {p.AssigneeName(t)}");
        }

        sb.AppendLine();
        sb.AppendLine("## Все задачи");
        sb.AppendLine();
        sb.AppendLine("| # | Задача | Тип | Приоритет | Статус | Исполнитель | Срок | Время |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var t in p.Tasks.OrderBy(t => t.Id))
        {
            var due = t.Deadline.HasValue ? t.Deadline.Value.ToString("dd.MM.yyyy") : "—";
            sb.AppendLine($"| {t.Id} | {Cell(t.Title)} | {t.Kind.Text()} | {t.Priority.Text()} | {t.State.Text()} | {Cell(p.AssigneeName(t))} | {due} | {FormatMinutes(t.SpentMinutes)} |");
        }

        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, $"report_{SafeName(p.Name)}_{r.GeneratedAt:yyyyMMdd_HHmm}.md");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        return path;
    }

    private static string Cell(string text) => text.Replace("|", "\\|").Replace("\n", " ");

    private static string FormatMinutes(int minutes) =>
        minutes <= 0 ? "—" : $"{minutes / 60}ч {minutes % 60:00}м";

    private static string SafeName(string name)
    {
        var safe = string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
        return safe.Length > 30 ? safe[..30] : safe;
    }
}
