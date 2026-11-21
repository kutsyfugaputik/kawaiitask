using KawaiiTasks.Models;
using KawaiiTasks.Services;

namespace KawaiiTasks.Tests;

/// <summary>Хранилище в памяти: вместо файла просто считает, сколько раз вызвали Save.</summary>
public sealed class InMemoryStorage : IStorage
{
    public AppData Data { get; set; } = new();
    public int SaveCount { get; private set; }

    public string Location => "memory";
    public string? LoadWarning => null;

    public AppData Load() => Data;

    public void Save(AppData data)
    {
        Data = data;
        SaveCount++;
    }
}

/// <summary>Временная папка, которая удаляется после теста (using var dir = new TempDir()).</summary>
public sealed class TempDir : IDisposable
{
    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KawaiiTasksTests_" + Guid.NewGuid().ToString("N"));

    public TempDir() => Directory.CreateDirectory(Path);

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try { Directory.Delete(Path, true); }
        catch (IOException) { /* временный мусор не должен ронять тесты */ }
        catch (UnauthorizedAccessException) { }
    }
}

/// <summary>Готовые объекты для тестов.</summary>
public static class Make
{
    /// <summary>Проект с двумя участниками (Аня id=1, Миша id=2) и без задач.</summary>
    public static Project NewProject()
    {
        var project = new Project { Id = 1, Name = "Тестовый проект" };
        project.AddMember("Аня", "Тимлид");
        project.AddMember("Миша", "Разработчик");
        return project;
    }

    public static TaskItem NewTask(Project project, string title = "Задача", Priority priority = Priority.Medium,
                                int? assigneeId = null, DateTime? deadline = null)
        => project.AddTask(title, "", priority, TaskKind.Feature, assigneeId, deadline);
}
