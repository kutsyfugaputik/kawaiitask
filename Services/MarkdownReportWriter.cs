using System.Text;
using KawaiiTasks.Models;

namespace KawaiiTasks.Services;

/// <summary>
/// Сохраняет отчёт о проекте в Markdown-файл (таблицы, которые красиво открываются на GitHub и в VS Code).
/// В консоль ничего не пишет: путь к файлу возвращается вызывающему коду, а он уже показывает его пользователю.
/// </summary>
public class MarkdownReportWriter : IReportWriter
{
    private readonly string _folder;

    public MarkdownReportWriter(string folder) => _folder = folder;

    public string Name => "Markdown-файл";

    public string? Write(ProjectReport r)
    {
        // Весь документ собираем в памяти и записываем на диск одним вызовом в конце.
        var sb = new StringBuilder();
        var p = r.Project;

        sb.AppendLine($"# ОТЧЁТ О ПРОЕКТЕ: {p.Name}");
        sb.AppendLine();
        sb.AppendLine($"_Сформирован: {r.GeneratedAt:dd.MM.yyyy HH:mm}_");
        if (!string.IsNullOrWhiteSpace(p.Description))
        {
            sb.AppendLine();
            sb.AppendLine($"> {Cell(p.Description)}");
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
        sb.AppendLine($"| Баллы | {r.CandyPoints} (уровень {level}: {levelName}) |");

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
            sb.AppendLine("## ! Просроченные задачи");
            sb.AppendLine();
            foreach (var t in r.Overdue)
                sb.AppendLine($"- #{t.Id} {Cell(t.Title)} - срок {t.Deadline:dd.MM.yyyy}, исполнитель: {Cell(p.AssigneeName(t))}");
        }

        sb.AppendLine();
        sb.AppendLine("## Все задачи");
        sb.AppendLine();
        sb.AppendLine("| # | Задача | Тип | Приоритет | Статус | Исполнитель | Срок | Время |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var t in p.Tasks.OrderBy(t => t.Id))
        {
            var due = t.Deadline.HasValue ? t.Deadline.Value.ToString("dd.MM.yyyy") : "-";
            sb.AppendLine($"| {t.Id} | {Cell(t.Title)} | {t.Kind.Text()} | {t.Priority.Text()} | {t.State.Text()} | {Cell(p.AssigneeName(t))} | {due} | {FormatMinutes(t.SpentMinutes)} |");
        }

        Directory.CreateDirectory(_folder);   // ничего не делает, если папка уже есть

        var baseName = $"report_{SafeName(p.Name)}_{r.GeneratedAt:yyyyMMdd_HHmmss}";
        var path = Path.Combine(_folder, baseName + ".md");
        // Если файл с таким именем уже есть (два отчёта в одну секунду) - добавляем _1, _2... и не затираем старый.
        var suffix = 1;
        while (File.Exists(path))
        {
            path = Path.Combine(_folder, $"{baseName}_{suffix}.md");
            suffix++;
        }

        // UTF-8 с BOM (true): без него стандартный Блокнот Windows может показать кириллицу «кракозябрами».
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        return path;
    }

    /// <summary>
    /// Экранирует текст для ячейки Markdown-таблицы: символ | иначе ломает колонки,
    /// а перевод строки обрывает строку таблицы.
    /// </summary>
    private static string Cell(string text) => (text ?? "")
        .Replace("\\", "\\\\")
        .Replace("|", "\\|")
        .Replace("\r", " ")
        .Replace("\n", " ");

    /// <summary>135 минут -> "2ч 15м"; ноль и меньше -> "-".</summary>
    private static string FormatMinutes(int minutes) =>
        minutes <= 0 ? "-" : $"{minutes / 60}ч {minutes % 60:00}м";

    /// <summary>Превращает название проекта в безопасную часть имени файла (буквы/цифры, остальное - "_").</summary>
    private static string SafeName(string name)
    {
        var safe = string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
        safe = safe.Trim('_');
        if (safe.Length == 0) safe = "project";

        // Windows reserves names like CON and AUX even when they have an extension.
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };
        if (reserved.Contains(safe)) safe += "_project";

        return safe.Length > 30 ? safe[..30] : safe;
    }
}
