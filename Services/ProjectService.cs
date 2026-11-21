using KawaiiTasks.Models;

namespace KawaiiTasks.Services;

/// <summary>
/// Вся работа с проектами и задачами. После каждого изменения данные сохраняются.
/// Консольный интерфейс обращается только сюда и не трогает файл данных напрямую.
/// </summary>
public class ProjectService
{
    public const int MaxProjectName = 60;
    public const int MaxProjectDescription = 200;

    private readonly IStorage _storage;
    private readonly AppData _data;

    public ProjectService(IStorage storage)
    {
        _storage = storage;
        _data = storage.Load();
        _data.Normalize();   // страховка от ручной правки/повреждения файла (см. AppData.Normalize)
    }

    /// <summary>Только для чтения: менять список можно лишь через методы сервиса, чтобы не забыть сохранить.</summary>
    public IReadOnlyList<Project> Projects => _data.Projects;

    public string StoragePath => _storage.Location;

    /// <summary>Корневая папка исходного проекта (при публикации - папка приложения).</summary>
    public string ProjectFolder => FindProjectFolder();

    /// <summary>Отчёты всегда лежат отдельно в папке reports внутри проекта.</summary>
    public string ReportsFolder => Path.Combine(ProjectFolder, "reports");

    /// <summary>
    /// Ищет папку с KawaiiTasks.csproj, поднимаясь от папки exe вверх.
    /// При запуске через `dotnet run` exe лежит в bin/Debug/net8.0/, а отчёты должны попасть
    /// в папку исходников, а не в служебную bin.
    /// </summary>
    private static string FindProjectFolder()
    {
        var starts = new[]
        {
            AppContext.BaseDirectory,          // папка, где лежит запущенная сборка
            Environment.CurrentDirectory       // текущая папка терминала, из которой запустили программу
        };

        foreach (var start in starts)
        {
            DirectoryInfo? current;
            try { current = new DirectoryInfo(start); }
            catch (ArgumentException) { continue; }

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "KawaiiTasks.csproj")))
                    return current.FullName;
                current = current.Parent;
            }
        }

        // Для опубликованного приложения без исходников используем папку exe.
        return AppContext.BaseDirectory;
    }

    // ---------- проекты ----------

    public Project CreateProject(string? name, string? description)
    {
        var value = (name ?? "").Trim();
        if (value.Length == 0)
            throw new KawaiiException("Название проекта не может быть пустым.");
        if (value.Length > MaxProjectName)
            throw new KawaiiException($"Название проекта слишком длинное (максимум {MaxProjectName} символов).");
        if (_data.Projects.Any(p => string.Equals(p.Name, value, StringComparison.OrdinalIgnoreCase)))
            throw new KawaiiException("Проект с таким названием уже существует.");

        var text = (description ?? "").Trim();
        if (text.Length > MaxProjectDescription)
            throw new KawaiiException($"Описание проекта слишком длинное (максимум {MaxProjectDescription} символов).");

        var project = new Project
        {
            Id = _data.NextProjectId++,
            Name = value,
            Description = text,
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

    /// <summary>
    /// Применить произвольную правку (название, описание, приоритет...) и сохранить.
    /// Если правка бросит исключение, Save не вызовется - на диск попадут только корректные данные.
    /// </summary>
    public void Update(Action change)
    {
        change();
        Save();
    }

    private void Save() => _storage.Save(_data);
}
