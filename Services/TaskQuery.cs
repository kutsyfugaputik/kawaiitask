using KawaiiTasks.Models;

namespace KawaiiTasks.Services;

public enum SortKey { Priority, Deadline, State, Title, Time }

/// <summary>Параметры поиска и фильтрации задач (то, что пользователь выбрал в меню «Поиск и фильтры»).</summary>
public class TaskFilter
{
    public string? Text { get; set; }
    public List<TaskState> States { get; set; } = new();   // пусто = любые
    public Priority? OnlyPriority { get; set; }
    public int? AssigneeId { get; set; }
    public bool OnlyOverdue { get; set; }
    public SortKey Sort { get; set; } = SortKey.Priority;
}

/// <summary>Применение фильтра к задачам проекта. Чистая функция: ничего не меняет и не выводит.</summary>
public static class TaskQuery
{
    public static List<TaskItem> Apply(Project project, TaskFilter filter)
    {
        // LINQ строит запрос лениво: условия накапливаются, а выполняется он один раз в самом конце (ToList).
        IEnumerable<TaskItem> query = project.Tasks;

        // Текстовый поиск: в названии, описании и имени исполнителя, без учёта регистра.
        if (!string.IsNullOrWhiteSpace(filter.Text))
        {
            var needle = filter.Text.Trim();
            query = query.Where(t =>
                t.Title.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                project.AssigneeName(t).Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.States.Count > 0)
            query = query.Where(t => filter.States.Contains(t.State));

        if (filter.OnlyPriority.HasValue)
            query = query.Where(t => t.Priority == filter.OnlyPriority.Value);

        if (filter.AssigneeId.HasValue)
            query = query.Where(t => t.AssigneeId == filter.AssigneeId.Value);

        if (filter.OnlyOverdue)
            query = query.Where(t => t.IsOverdue);

        IOrderedEnumerable<TaskItem> ordered = filter.Sort switch
        {
            SortKey.Priority => query.OrderByDescending(t => t.Priority),
            SortKey.Deadline => query.OrderBy(t => t.Deadline ?? DateTime.MaxValue),   // без срока - в самый конец
            SortKey.State => query.OrderBy(t => t.State),
            SortKey.Title => query.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase),
            SortKey.Time => query.OrderByDescending(t => t.SpentMinutes),
            _ => query.OrderBy(t => t.Id)
        };

        // Вторичная сортировка по ID делает порядок стабильным и предсказуемым при равных значениях.
        return ordered.ThenBy(t => t.Id).ToList();
    }
}
