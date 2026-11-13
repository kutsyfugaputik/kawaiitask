namespace KawaiiTasks.Models;

public enum Priority { Low = 1, Medium = 2, High = 3, Critical = 4 }

public enum TaskState { Todo, InProgress, Review, Done }

public enum TaskKind { Feature, Bug, Chore, Docs }

/// <summary>Читаемые подписи для перечислений.</summary>
public static class EnumText
{
    public static string Text(this Priority value) => value switch
    {
        Priority.Low => "♡ Низкий",
        Priority.Medium => "✿ Средний",
        Priority.High => "◆ Высокий",
        Priority.Critical => "!! Срочно!",
        _ => value.ToString()
    };

    public static string Text(this TaskState value) => value switch
    {
        TaskState.Todo => "○ Ждёт",
        TaskState.InProgress => "◇ В работе",
        TaskState.Review => "⌕ Проверка",
        TaskState.Done => "✓ Готово",
        _ => value.ToString()
    };

    public static string Text(this TaskKind value) => value switch
    {
        TaskKind.Feature => "✧ Фича",
        TaskKind.Bug => "× Баг",
        TaskKind.Chore => "✎ Рутина",
        TaskKind.Docs => "▤ Доки",
        _ => value.ToString()
    };
}
