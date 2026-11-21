using KawaiiTasks.Models;
using KawaiiTasks.Services;

namespace KawaiiTasks.Tests;

public class StorageTests
{
    [Fact]
    public void Load_MissingFile_ReturnsEmptyData_WithoutWarning()
    {
        using var dir = new TempDir();
        var storage = new JsonStorage(dir.File("data.json"));

        var data = storage.Load();

        Assert.Empty(data.Projects);
        Assert.Equal(1, data.NextProjectId);
        Assert.Null(storage.LoadWarning);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsEverything()
    {
        using var dir = new TempDir();
        var path = dir.File("data.json");

        var data = new AppData();
        var project = new Project { Id = 1, Name = "Проект «Зефирка»", Description = "Описание" };
        var member = project.AddMember("Аня", "Тимлид");
        var deadline = DateTime.Today.AddDays(5);
        var task = project.AddTask("Сделать макеты", "Подробности", Priority.Critical, TaskKind.Bug, member.Id, deadline);
        task.ChangeState(TaskState.Review);
        task.LogTime(90, "первая сессия");
        data.Projects.Add(project);
        data.NextProjectId = 2;

        new JsonStorage(path).Save(data);
        var loaded = new JsonStorage(path).Load();

        var p = Assert.Single(loaded.Projects);
        Assert.Equal("Проект «Зефирка»", p.Name);
        Assert.Equal(2, loaded.NextProjectId);

        var m = Assert.Single(p.Team);
        Assert.Equal("Аня", m.Name);
        Assert.Equal("Тимлид", m.Role);

        var t = Assert.Single(p.Tasks);
        Assert.Equal("Сделать макеты", t.Title);
        Assert.Equal(Priority.Critical, t.Priority);
        Assert.Equal(TaskKind.Bug, t.Kind);
        Assert.Equal(TaskState.Review, t.State);
        Assert.Equal(member.Id, t.AssigneeId);
        Assert.Equal(deadline, t.Deadline);
        Assert.Equal(90, t.SpentMinutes);
        Assert.Equal("первая сессия", t.TimeLog[0].Note);
    }

    [Fact]
    public void Save_WritesReadableJson_EnumsAsNames_AndCyrillicAsIs()
    {
        using var dir = new TempDir();
        var path = dir.File("data.json");
        var data = new AppData();
        var project = new Project { Id = 1, Name = "Русское имя" };
        project.AddTask("Задача", "", Priority.High, TaskKind.Docs, null, null);
        data.Projects.Add(project);

        new JsonStorage(path).Save(data);
        var text = File.ReadAllText(path);

        Assert.Contains("Русское имя", text);          // без \u04xx-экранирования
        Assert.Contains("\"Priority\": \"High\"", text); // enum словом, а не числом
        Assert.Contains("\"Kind\": \"Docs\"", text);
    }

    [Fact]
    public void Save_CreatesMissingFolders_AndLeavesNoTempFile()
    {
        using var dir = new TempDir();
        var path = Path.Combine(dir.Path, "a", "b", "data.json");

        new JsonStorage(path).Save(new AppData());

        Assert.True(File.Exists(path));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Save_Twice_OverwritesPreviousFile()
    {
        using var dir = new TempDir();
        var path = dir.File("data.json");
        var storage = new JsonStorage(path);

        var data = new AppData();
        storage.Save(data);
        data.Projects.Add(new Project { Id = 1, Name = "Появился позже" });
        storage.Save(data);

        Assert.Single(new JsonStorage(path).Load().Projects);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsEmptyData_SetsWarning_AndKeepsBackup()
    {
        using var dir = new TempDir();
        var path = dir.File("data.json");
        File.WriteAllText(path, "{ это не json ");

        var storage = new JsonStorage(path);
        var data = storage.Load();

        Assert.Empty(data.Projects);
        Assert.NotNull(storage.LoadWarning);
        var backups = Directory.GetFiles(dir.Path, "data.json.broken-*");
        var backup = Assert.Single(backups);
        Assert.Equal("{ это не json ", File.ReadAllText(backup));   // содержимое спасено
    }

    [Fact]
    public void Load_EmptyFile_IsTreatedAsCorrupt()
    {
        using var dir = new TempDir();
        var path = dir.File("data.json");
        File.WriteAllText(path, "");

        var storage = new JsonStorage(path);
        var data = storage.Load();

        Assert.Empty(data.Projects);
        Assert.NotNull(storage.LoadWarning);
    }

    [Fact]
    public void FullCycle_ServiceDataSurvivesRestart()
    {
        using var dir = new TempDir();
        var path = dir.File("data.json");

        var first = new ProjectService(new JsonStorage(path));
        var project = first.CreateProject("Переживёт перезапуск", "тест");
        var member = first.AddMember(project, "Миша", "Разработчик");
        first.AddTask(project, "Задача", "", Priority.High, TaskKind.Feature, member.Id, DateTime.Today.AddDays(2));

        // «перезапуск программы» = новый сервис поверх того же файла
        var second = new ProjectService(new JsonStorage(path));

        var loaded = Assert.Single(second.Projects);
        Assert.Equal("Переживёт перезапуск", loaded.Name);
        Assert.Equal("Миша", Assert.Single(loaded.Team).Name);
        Assert.Equal("Задача", Assert.Single(loaded.Tasks).Title);
        Assert.Equal(2, loaded.NextTaskId);
    }
}
