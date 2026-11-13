using KawaiiTasks.Models;

namespace KawaiiTasks.Services;

/// <summary>Вся работа с проектами и задачами. После каждого изменения данные сохраняются.</summary>
public class ProjectService
{
    private readonly IStorage _storage;
    private readonly AppData _data;

    public ProjectService(IStorage storage)
    {
        _storage = storage;
        _data = storage.Load();
    }

    public IReadOnlyList<Project> Projects => _data.Projects;

    public string StoragePath => _storage.Location;

    public string DataFolder => Path.GetDirectoryName(_storage.Location) ?? AppContext.BaseDirectory;

    // ---------- проекты ----------

    public Project CreateProject(string? name, string? description)
    {
        var value = (name ?? "").Trim();
        if (value.Length == 0)
            throw new KawaiiException("Название проекта не может быть пустым.");
        if (value.Length > 60)
            throw new KawaiiException("Название проекта слишком длинное (максимум 60 символов).");
        if (_data.Projects.Any(p => string.Equals(p.Name, value, StringComparison.OrdinalIgnoreCase)))
            throw new KawaiiException("Проект с таким названием уже существует.");

        var project = new Project
        {
            Id = _data.NextProjectId++,
            Name = value,
            Description = (description ?? "").Trim(),
            CreatedAt = DateTime.Now
        };
        _data.Projects.Add(project);
        Save();
        return project;
    }

    public Project AddDemoProject()
    {
        var demo = DemoData.Create(_data.NextProjectId);
        if (_data.Projects.Any(p => string.Equals(p.Name, demo.Name, StringComparison.OrdinalIgnoreCase)))
            throw new KawaiiException("Демо-проект уже создан.");

        _data.NextProjectId++;
        _data.Projects.Add(demo);
        Save();
        return demo;
    }

    public void DeleteProject(Project project)
    {
        _data.Projects.Remove(project);
        Save();
    }

    // ---------- команда ----------

    public Member AddMember(Project project, string? name, string? role)
    {
        var member = project.AddMember(name, role);
        Save();
        return member;
    }

    public void RemoveMember(Project project, Member member)
    {
        project.RemoveMember(member);
        Save();
    }

    // ---------- задачи ----------

    public TaskItem AddTask(Project project, string? title, string? description, Priority priority,
                            TaskKind kind, int? assigneeId, DateTime? deadline)
    {
        var task = project.AddTask(title, description, priority, kind, assigneeId, deadline);
        Save();
        return task;
    }

    public void DeleteTask(Project project, TaskItem task)
    {
        project.RemoveTask(task);
        Save();
    }

    public void ChangeState(TaskItem task, TaskState state)
    {
        task.ChangeState(state);
        Save();
    }

    public void Assign(Project project, TaskItem task, Member? member)
    {
        if (member is not null && project.FindMember(member.Id) is null)
            throw new KawaiiException("Этот участник не состоит в команде проекта.");
        task.AssigneeId = member?.Id;
        Save();
    }

    public void SetDeadline(TaskItem task, DateTime? deadline)
    {
        task.SetDeadline(deadline);
        Save();
    }

    public void LogTime(TaskItem task, int minutes, string? note)
    {
        task.LogTime(minutes, note);
        Save();
    }

    /// <summary>Применить произвольную правку (название, описание, приоритет…) и сохранить.</summary>
    public void Update(Action change)
    {
        change();
        Save();
    }

    private void Save() => _storage.Save(_data);
}
