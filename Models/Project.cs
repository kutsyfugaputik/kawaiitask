using System.Text.Json.Serialization;

namespace KawaiiTasks.Models;

/// <summary>IT-проект: команда + задачи. Все правила предметной области проверяются здесь, а не в консоли.</summary>
public class Project
{
    public const int MaxMemberName = 40;
    public const int MaxRole = 30;

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<Member> Team { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();

    // Счётчики хранятся в JSON, чтобы ID не повторялись даже после удаления задач/участников.
    public int NextTaskId { get; set; } = 1;
    public int NextMemberId { get; set; } = 1;

    /// <summary>Процент выполненных задач. [JsonIgnore] - значение вычисляемое, в файл его писать не нужно.</summary>
    [JsonIgnore]
    public double Progress => Tasks.Count == 0 ? 0 : Tasks.Count(t => t.IsDone) * 100.0 / Tasks.Count;

    /// <summary>Добавляет участника. Имена сравниваются без учёта регистра, дубликаты запрещены.</summary>
    public Member AddMember(string? name, string? role)
    {
        var value = (name ?? "").Trim();
        if (value.Length == 0)
            throw new KawaiiException("Имя участника не может быть пустым.");
        if (value.Length > MaxMemberName)
            throw new KawaiiException($"Имя слишком длинное (максимум {MaxMemberName} символов).");
        if (Team.Any(m => string.Equals(m.Name, value, StringComparison.OrdinalIgnoreCase)))
            throw new KawaiiException($"Участник '{value}' уже есть в команде.");

        var roleValue = string.IsNullOrWhiteSpace(role) ? "Участник" : role.Trim();
        if (roleValue.Length > MaxRole)
            throw new KawaiiException($"Название роли слишком длинное (максимум {MaxRole} символов).");

        var member = new Member
        {
            Id = NextMemberId++,
            Name = value,
            Role = roleValue
        };
        Team.Add(member);
        return member;
    }

    /// <summary>Удаляет участника и снимает его с задач, чтобы не остались «висячие» ссылки на несуществующий ID.</summary>
    public void RemoveMember(Member member)
    {
        foreach (var task in Tasks.Where(t => t.AssigneeId == member.Id))
            task.AssigneeId = null;
        Team.Remove(member);
    }

    /// <summary>
    /// Создаёт задачу. Счётчик NextTaskId увеличивается только после успешной проверки всех полей,
    /// поэтому неудачная попытка не «сжигает» номер.
    /// </summary>
    public TaskItem AddTask(string? title, string? description, Priority priority, TaskKind kind,
                            int? assigneeId, DateTime? deadline)
    {
        if (assigneeId.HasValue && FindMember(assigneeId) is null)
            throw new KawaiiException("Такого исполнителя нет в команде.");

        var task = new TaskItem { Id = NextTaskId, Priority = priority, Kind = kind };
        task.Rename(title);          // каждый из этих методов сам бросает KawaiiException при ошибке
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

    /// <summary>Имя исполнителя для вывода в таблицах; "-" если задача никому не назначена.</summary>
    public string AssigneeName(TaskItem task) => FindMember(task.AssigneeId)?.Name ?? "-";

    /// <summary>
    /// Чинит возможные несогласованности после чтения из JSON:
    /// null-списки, отстающие счётчики ID и ссылки на исполнителей, которых уже нет в команде.
    /// </summary>
    public void Normalize()
    {
        Team ??= new();
        Tasks ??= new();

        NextMemberId = Math.Max(NextMemberId, Team.Select(m => m.Id).DefaultIfEmpty(0).Max() + 1);
        NextTaskId = Math.Max(NextTaskId, Tasks.Select(t => t.Id).DefaultIfEmpty(0).Max() + 1);

        foreach (var task in Tasks)
        {
            task.TimeLog ??= new();
            if (task.AssigneeId.HasValue && FindMember(task.AssigneeId) is null)
                task.AssigneeId = null;
        }
    }
}
