using System.Text.Json.Serialization;

namespace KawaiiTasks.Models;

/// <summary>IT-проект: команда + задачи.</summary>
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<Member> Team { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
    public int NextTaskId { get; set; } = 1;
    public int NextMemberId { get; set; } = 1;

    [JsonIgnore]
    public double Progress => Tasks.Count == 0 ? 0 : Tasks.Count(t => t.IsDone) * 100.0 / Tasks.Count;

    public Member AddMember(string? name, string? role)
    {
        var value = (name ?? "").Trim();
        if (value.Length == 0)
            throw new KawaiiException("Имя участника не может быть пустым.");
        if (value.Length > 40)
            throw new KawaiiException("Имя слишком длинное (максимум 40 символов).");
        if (Team.Any(m => string.Equals(m.Name, value, StringComparison.OrdinalIgnoreCase)))
            throw new KawaiiException($"Участник «{value}» уже есть в команде.");

        var member = new Member
        {
            Id = NextMemberId++,
            Name = value,
            Role = string.IsNullOrWhiteSpace(role) ? "Участник" : role.Trim()
        };
        Team.Add(member);
        return member;
    }

    public void RemoveMember(Member member)
    {
        foreach (var task in Tasks.Where(t => t.AssigneeId == member.Id))
            task.AssigneeId = null;
        Team.Remove(member);
    }

    public TaskItem AddTask(string? title, string? description, Priority priority, TaskKind kind,
                            int? assigneeId, DateTime? deadline)
    {
        if (assigneeId.HasValue && FindMember(assigneeId) is null)
            throw new KawaiiException("Такого исполнителя нет в команде.");

        var task = new TaskItem { Id = NextTaskId, Priority = priority, Kind = kind };
        task.Rename(title);
        task.Describe(description);
        task.SetDeadline(deadline);
        task.AssigneeId = assigneeId;

        NextTaskId++;
        Tasks.Add(task);
        return task;
    }

    public void RemoveTask(TaskItem task) => Tasks.Remove(task);

    public Member? FindMember(int? id) =>
        id.HasValue ? Team.FirstOrDefault(m => m.Id == id.Value) : null;

    public string AssigneeName(TaskItem task) => FindMember(task.AssigneeId)?.Name ?? "—";
}
