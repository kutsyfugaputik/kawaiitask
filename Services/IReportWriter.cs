namespace KawaiiTasks.Services;

/// <summary>
/// Способ вывести отчёт (в консоль, в файл...). Возвращает путь к файлу, если он создавался.
/// Паттерн «Стратегия»: <c>ConsoleReportWriter</c> рисует отчёт в терминале,
/// <see cref="MarkdownReportWriter"/> сохраняет его в .md - данные для обоих собирает <see cref="ReportService"/>.
/// </summary>
public interface IReportWriter
{
    string Name { get; }
    string? Write(ProjectReport report);
}
