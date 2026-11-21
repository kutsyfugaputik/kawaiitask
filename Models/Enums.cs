namespace KawaiiTasks.Models;

/// <summary>Приоритет задачи. Числа важны: они используются и для сортировки, и для начисления баллов.</summary>
public enum Priority { Low = 1, Medium = 2, High = 3, Critical = 4 }

/// <summary>Статус задачи. Порядок значений = порядок колонок на канбан-доске.</summary>
public enum TaskState { Todo, InProgress, Review, Done }

/// <summary>Тип задачи IT-проекта.</summary>
public enum TaskKind { Feature, Bug, Chore, Docs }

/// <summary>
/// Читаемые русские подписи для перечислений.
/// Подписи намеренно содержат только ASCII-префикс (LOW, WORK, BUG...) и кириллицу:
/// так они одинаково выглядят в любой консоли, без эмодзи и редких Unicode-символов.
/// </summary>
public static class EnumText
{
    public static string Text(this Priority value) => value switch
    {
        Priority.Low => "LOW - Низкий",
        Priority.Medium => "MED - Средний",
        Priority.High => "HIGH - Высокий",
        Priority.Critical => "URGENT - Срочно!",
        _ => value.ToString()
    };

    public static string Text(this TaskState value) => value switch
    {
        TaskState.Todo => "TODO - Ждет",
        TaskState.InProgress => "WORK - В работе",
        TaskState.Review => "REVIEW - Проверка",
        TaskState.Done => "DONE - Готово",
        _ => value.ToString()
    };

    public static string Text(this TaskKind value) => value switch
    {
        TaskKind.Feature => "FEATURE - Фича",
        TaskKind.Bug => "BUG - Баг",
        TaskKind.Chore => "CHORE - Рутина",
        TaskKind.Docs => "DOCS - Документация",
        _ => value.ToString()
    };
}
