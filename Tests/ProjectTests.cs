using KawaiiTasks.Models;

namespace KawaiiTasks.Tests;

public class ProjectTests
{
    // ---------- команда ----------

    [Fact]
    public void AddMember_AssignsIncreasingIds_AndDefaultRole()
    {
        var project = new Project();
        var a = project.AddMember("Аня", null);
        var b = project.AddMember("Миша", "   ");

        Assert.Equal(1, a.Id);
        Assert.Equal(2, b.Id);
        Assert.Equal("Участник", a.Role);
        Assert.Equal("Участник", b.Role);
    }

    [Fact]
    public void AddMember_DuplicateNameIgnoringCase_Throws()
    {
        var project = new Project();
        project.AddMember("Аня", "Тимлид");
        Assert.Throws<KawaiiException>(() => project.AddMember("  аНЯ ", "Дизайнер"));
        Assert.Single(project.Team);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddMember_EmptyName_Throws(string? name)
    {
        var project = new Project();
        Assert.Throws<KawaiiException>(() => project.AddMember(name, "Тимлид"));
    }

    [Fact]
    public void AddMember_TooLongNameOrRole_Throws_AndDoesNotConsumeId()
    {
        var project = new Project();

        Assert.Throws<KawaiiException>(() => project.AddMember(new string('я', Project.MaxMemberName + 1), null));
        Assert.Throws<KawaiiException>(() => project.AddMember("Аня", new string('р', Project.MaxRole + 1)));

        var ok = project.AddMember("Аня", null);
        Assert.Equal(1, ok.Id);   // неудачные попытки не «сжигают» номера
    }

    [Fact]
    public void RemoveMember_UnassignsTheirTasks_ButKeepsTasks()
    {
        var project = Make.NewProject();
        var anya = project.Team[0];
        var task = Make.NewTask(project, assigneeId: anya.Id);

        project.RemoveMember(anya);

        Assert.DoesNotContain(anya, project.Team);
        Assert.Null(task.AssigneeId);
        Assert.Contains(task, project.Tasks);
        Assert.Equal("-", project.AssigneeName(task));
    }

    // ---------- задачи ----------

    [Fact]
    public void AddTask_IncrementsIds_AndStoresFields()
    {
        var project = Make.NewProject();
        var deadline = DateTime.Today.AddDays(2);

        var first = project.AddTask("Первая", "Описание", Priority.High, TaskKind.Bug, 1, deadline);
        var second = Make.NewTask(project, "Вторая");

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
        Assert.Equal(Priority.High, first.Priority);
        Assert.Equal(TaskKind.Bug, first.Kind);
        Assert.Equal(1, first.AssigneeId);
        Assert.Equal(deadline, first.Deadline);
        Assert.Equal(TaskState.Todo, first.State);
    }

    [Fact]
    public void AddTask_UnknownAssignee_Throws_AndAddsNothing()
    {
        var project = Make.NewProject();

        Assert.Throws<KawaiiException>(() => Make.NewTask(project, assigneeId: 999));

        Assert.Empty(project.Tasks);
        Assert.Equal(1, project.NextTaskId);
    }

    [Fact]
    public void AddTask_InvalidTitle_DoesNotConsumeId()
    {
        var project = Make.NewProject();

        Assert.Throws<KawaiiException>(() => Make.NewTask(project, title: ""));
        var ok = Make.NewTask(project, "Нормальная");

        Assert.Equal(1, ok.Id);
    }

    [Fact]
    public void AddTask_PastDeadline_Throws()
    {
        var project = Make.NewProject();
        Assert.Throws<KawaiiException>(() => Make.NewTask(project, deadline: DateTime.Today.AddDays(-1)));
        Assert.Empty(project.Tasks);
    }

    [Fact]
    public void RemoveTask_DoesNotReuseIds()
    {
        var project = Make.NewProject();
        var first = Make.NewTask(project, "Первая");
        project.RemoveTask(first);

        var next = Make.NewTask(project, "Вторая");

        Assert.Equal(2, next.Id);
    }

    [Fact]
    public void Progress_IsPercentOfDoneTasks()
    {
        var project = Make.NewProject();
        Assert.Equal(0, project.Progress);   // пустой проект: нет деления на ноль

        var a = Make.NewTask(project, "A");
        Make.NewTask(project, "B");
        a.ChangeState(TaskState.Done);

        Assert.Equal(50, project.Progress);
    }

    [Fact]
    public void FindMember_NullOrUnknownId_ReturnsNull()
    {
        var project = Make.NewProject();
        Assert.Null(project.FindMember(null));
        Assert.Null(project.FindMember(42));
        Assert.Equal("Аня", project.FindMember(1)!.Name);
    }

    // ---------- нормализация данных после чтения из файла ----------

    [Fact]
    public void Normalize_RepairsCountersAndDanglingAssignees()
    {
        var project = Make.NewProject();
        var task = Make.NewTask(project, assigneeId: 1);

        // имитируем «испорченный» файл: счётчики сброшены, исполнитель не существует
        project.NextTaskId = 1;
        project.NextMemberId = 1;
        task.AssigneeId = 777;

        project.Normalize();

        Assert.Equal(2, project.NextTaskId);     // максимальный id задачи (1) + 1
        Assert.Equal(3, project.NextMemberId);   // максимальный id участника (2) + 1
        Assert.Null(task.AssigneeId);
    }

    [Fact]
    public void AppData_Normalize_RepairsProjectCounter()
    {
        var data = new AppData { NextProjectId = 1 };
        data.Projects.Add(new Project { Id = 5, Name = "Старый" });

        data.Normalize();

        Assert.Equal(6, data.NextProjectId);
    }
}
