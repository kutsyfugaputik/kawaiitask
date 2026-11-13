using System.Text.Json.Serialization;

namespace KawaiiTasks.Models;

/// <summary>Задача IT-проекта. Все изменения с проверками идут через методы класса.</summary>
public class TaskItem
{
    public const int MaxTitle = 80;
    public const int MaxDescription = 500;

    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public Priority Priority { get; set; } = Priority.Medium;
    public TaskKind Kind { get; set; } = TaskKind.Feature;
    public TaskState State { get; set; } = TaskState.Todo;
    public int? AssigneeId { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public List<TimeEntry> TimeLog { get; set; } = new();

    [JsonIgnore] public int SpentMinutes => TimeLog.Sum(t => t.Minutes);
    [JsonIgnore] public bool IsDone => State == TaskState.Done;
    [JsonIgnore] public bool IsOverdue => !IsDone && Deadline.HasValue && Deadline.Value.Date < DateTime.Today;

    public void Rename(string? title)
    {
        var value = (title ?? "").Trim();
        if (value.Length == 0)
            throw new KawaiiException("Название задачи не может быть пустым.");
        if (value.Length > MaxTitle)
            throw new KawaiiException($"Название слишком длинное (максимум {MaxTitle} символов).");
        Title = value;
    }

    public void Describe(string? text)
    {
        var value = (text ?? "").Trim();
        if (value.Length > MaxDescription)
            throw new KawaiiException($"Описание слишком длинное (максимум {MaxDescription} символов).");
        Description = value;
    }

    public void SetDeadline(DateTime? deadline)
    {
        if (deadline.HasValue && deadline.Value.Date < DateTime.Today)
            throw new KawaiiException("Срок выполнения не может быть в прошлом.");
        Deadline = deadline?.Date;
    }

    public void ChangeState(TaskState state)
    {
        State = state;
        CompletedAt = state == TaskState.Done ? DateTime.Now : null;
    }

    public TimeEntry LogTime(int minutes, string? note = null, DateTime? date = null)
    {
        if (minutes < 1 || minutes > 24 * 60)
            throw new KawaiiException("За один раз можно записать от 1 минуты до 24 часов.");

        var entry = new TimeEntry
        {
            Date = date ?? DateTime.Now,
            Minutes = minutes,
            Note = (note ?? "").Trim()
        };
        TimeLog.Add(entry);
        return entry;
    }
}
