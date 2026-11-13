namespace KawaiiTasks.Services;

/// <summary>Способ вывести отчёт (в консоль, в файл…). Возвращает путь к файлу, если он создавался.</summary>
public interface IReportWriter
{
    string Name { get; }
    string? Write(ProjectReport report);
}
