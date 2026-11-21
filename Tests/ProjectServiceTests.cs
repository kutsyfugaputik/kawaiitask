using KawaiiTasks.Models;
using KawaiiTasks.Services;

namespace KawaiiTasks.Tests;

public class ProjectServiceTests
{
    private static (ProjectService Service, InMemoryStorage Storage) NewService()
    {
        var storage = new InMemoryStorage();
        return (new ProjectService(storage), storage);
    }

    // ---------- проекты ----------

    [Fact]
    public void CreateProject_TrimsName_AssignsId_AndSaves()
    {
        var (service, storage) = NewService();

        var project = service.CreateProject("  Мой проект ", "  описание ");

        Assert.Equal("Мой проект", project.Name);
        Assert.Equal("описание", project.Description);
        Assert.Equal(1, project.Id);
        Assert.Single(service.Projects);
        Assert.Equal(1, storage.SaveCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProject_EmptyName_Throws_AndDoesNotSave(string? name)
    {
        var (service, storage) = NewService();

        Assert.Throws<KawaiiException>(() => service.CreateProject(name, null));

        Assert.Empty(service.Projects);
        Assert.Equal(0, storage.SaveCount);
    }

    [Fact]
    public void CreateProject_TooLongNameOrDescription_Throws()
    {
        var (service, _) = NewService();

        Assert.Throws<KawaiiException>(() =>
            service.CreateProject(new string('п', ProjectService.MaxProjectName + 1), null));
        Assert.Throws<KawaiiException>(() =>
            service.CreateProject("Ок", new string('о', ProjectService.MaxProjectDescription + 1)));

        Assert.Empty(service.Projects);
    }

    [Fact]
    public void CreateProject_DuplicateNameIgnoringCase_Throws()
    {
        var (service, _) = NewService();
        service.CreateProject("Сайт", null);

        Assert.Throws<KawaiiException>(() => service.CreateProject("сАЙТ", null));
        Assert.Single(service.Projects);
    }

    [Fact]
    public void AddDemoProject_CreatesFullProject_AndSecondCallThrows()
    {
        var (service, _) = NewService();

        var demo = service.AddDemoProject();

        Assert.NotEmpty(demo.Team);
        Assert.NotEmpty(demo.Tasks);
        Assert.Throws<KawaiiException>(() => service.AddDemoProject());
        Assert.Single(service.Projects);
    }

    [Fact]
    public void DeleteProject_RemovesIt_AndSaves()
    {
        var (service, storage) = NewService();
        var project = service.CreateProject("Удаляемый", null);
        var savesBefore = storage.SaveCount;

        service.DeleteProject(project);

        Assert.Empty(service.Projects);
        Assert.Equal(savesBefore + 1, storage.SaveCount);
    }

    [Fact]
    public void ProjectIds_AreNotReusedAfterDelete()
    {
        var (service, _) = NewService();
        var first = service.CreateProject("Первый", null);
        service.DeleteProject(first);

        var second = service.CreateProject("Второй", null);

        Assert.Equal(2, second.Id);
    }

    // ---------- команда и задачи ----------

    [Fact]
    public void EveryMutation_SavesData()
    {
        var (service, storage) = NewService();
        var project = service.CreateProject("Проект", null);

        var member = service.AddMember(project, "Аня", "Тимлид");
        var task = service.AddTask(project, "Задача", "", Priority.High, TaskKind.Bug, member.Id, null);
        service.ChangeState(task, TaskState.InProgress);
        service.SetDeadline(task, DateTime.Today.AddDays(1));
        service.LogTime(task, 30, "работа");
        service.Assign(project, task, null);
        service.Update(() => task.Priority = Priority.Low);
        service.DeleteTask(project, task);
        service.RemoveMember(project, member);

        // 1 (проект) + 9 операций выше
        Assert.Equal(10, storage.SaveCount);
    }

    [Fact]
    public void FailedOperation_DoesNotSave()
    {
        var (service, storage) = NewService();
        var project = service.CreateProject("Проект", null);
        var savesBefore = storage.SaveCount;

        Assert.Throws<KawaiiException>(() =>
            service.AddTask(project, "", "", Priority.Low, TaskKind.Chore, null, null));
        Assert.Throws<KawaiiException>(() => service.AddMember(project, "", null));

        Assert.Equal(savesBefore, storage.SaveCount);
    }

    [Fact]
    public void Assign_MemberFromAnotherProject_Throws()
    {
        var (service, _) = NewService();
        var one = service.CreateProject("Один", null);
        var two = service.CreateProject("Два", null);
        var stranger = service.AddMember(two, "Чужой", null);
        var task = service.AddTask(one, "Задача", "", Priority.Low, TaskKind.Chore, null, null);

        Assert.Throws<KawaiiException>(() => service.Assign(one, task, stranger));
        Assert.Null(task.AssigneeId);
    }

    [Fact]
    public void Assign_Null_RemovesAssignee()
    {
        var (service, _) = NewService();
        var project = service.CreateProject("Проект", null);
        var member = service.AddMember(project, "Аня", null);
        var task = service.AddTask(project, "Задача", "", Priority.Low, TaskKind.Chore, member.Id, null);

        service.Assign(project, task, null);

        Assert.Null(task.AssigneeId);
    }

    [Fact]
    public void Update_WhenChangeThrows_DoesNotSave()
    {
        var (service, storage) = NewService();
        var project = service.CreateProject("Проект", null);
        var task = service.AddTask(project, "Задача", "", Priority.Low, TaskKind.Chore, null, null);
        var savesBefore = storage.SaveCount;

        Assert.Throws<KawaiiException>(() => service.Update(() => task.Rename("")));

        Assert.Equal(savesBefore, storage.SaveCount);
        Assert.Equal("Задача", task.Title);
    }

    // ---------- данные из файла ----------

    [Fact]
    public void Constructor_NormalizesBrokenData()
    {
        var storage = new InMemoryStorage();
        var project = new Project { Id = 7, Name = "Из файла" };
        project.Tasks.Add(new TaskItem { Id = 10, Title = "Старая", AssigneeId = 99 });   // исполнителя 99 нет
        storage.Data.Projects.Add(project);
        storage.Data.NextProjectId = 1;   // счётчик «отстал»

        var service = new ProjectService(storage);

        var created = service.CreateProject("Новый", null);
        Assert.Equal(8, created.Id);                       // не 1 и не 7
        Assert.Null(project.Tasks[0].AssigneeId);          // «висячая» ссылка убрана
        Assert.Equal(11, project.NextTaskId);
    }
}
