using KawaiiTasks.Models;
using KawaiiTasks.Services;

namespace KawaiiTasks.Tests;

public class ReportServiceTests
{
    // ---------- баллы ----------

    [Fact]
    public void TaskPoints_NotDone_IsZero()
    {
        var project = Make.NewProject();
        var task = Make.NewTask(project, priority: Priority.Critical);

        Assert.Equal(0, ReportService.TaskPoints(task));
    }

    [Theory]
    [InlineData(Priority.Low, 10)]
    [InlineData(Priority.Medium, 20)]
    [InlineData(Priority.High, 30)]
    [InlineData(Priority.Critical, 40)]
    public void TaskPoints_DoneWithoutDeadline_IsPriorityTimesTen(Priority priority, int expected)
    {
        var project = Make.NewProject();
        var task = Make.NewTask(project, priority: priority);
        task.ChangeState(TaskState.Done);

        Assert.Equal(expected, ReportService.TaskPoints(task));
    }

    [Fact]
    public void TaskPoints_DoneOnTime_GetsFiveBonusPoints()
    {
        var project = Make.NewProject();
        var task = Make.NewTask(project, priority: Priority.High, deadline: DateTime.Today.AddDays(1));
        task.ChangeState(TaskState.Done);

        Assert.Equal(35, ReportService.TaskPoints(task));
    }

    [Fact]
    public void TaskPoints_DoneLate_NoBonus()
    {
        var project = Make.NewProject();
        var task = Make.NewTask(project, priority: Priority.High);
        task.Deadline = DateTime.Today.AddDays(-3);   // напрямую: SetDeadline не разрешает прошлое
        task.ChangeState(TaskState.Done);

        Assert.Equal(30, ReportService.TaskPoints(task));
    }

    [Fact]
    public void CandyPoints_SumsAllTasks()
    {
        var project = Make.NewProject();
        var a = Make.NewTask(project, priority: Priority.Low);
        var b = Make.NewTask(project, priority: Priority.High);
        Make.NewTask(project, priority: Priority.Critical);   // не выполнена
        a.ChangeState(TaskState.Done);
        b.ChangeState(TaskState.Done);

        Assert.Equal(40, ReportService.CandyPoints(project));
    }

    // ---------- уровни ----------

    [Theory]
    [InlineData(-10, 1, "Зефирка")]
    [InlineData(0, 1, "Зефирка")]
    [InlineData(49, 1, "Зефирка")]
    [InlineData(50, 2, "Маршмеллоу")]
    [InlineData(100, 3, "Пончик")]
    [InlineData(150, 4, "Единорожка")]
    [InlineData(200, 5, "Королева кода")]
    [InlineData(100000, 5, "Королева кода")]
    public void Level_ChangesEveryFiftyPoints_AndIsCapped(int points, int level, string name)
    {
        var result = ReportService.Level(points);

        Assert.Equal(level, result.Level);
        Assert.Equal(name, result.Name);
    }

    // ---------- отчёт ----------

    [Fact]
    public void Build_EmptyProject_HasZeroesAndNoDivisionByZero()
    {
        var report = ReportService.Build(new Project { Name = "Пустой" });

        Assert.Equal(0, report.Total);
        Assert.Equal(0, report.Done);
        Assert.Equal(0, report.Percent);
        Assert.Equal(0, report.TotalMinutes);
        Assert.Equal(0, report.CandyPoints);
        Assert.Empty(report.Overdue);
        Assert.Empty(report.Members);
        Assert.Empty(report.TopByTime);
        // нулевые колонки для диаграмм всё равно есть
        Assert.Equal(Enum.GetValues<TaskState>().Length, report.ByState.Count);
        Assert.Equal(Enum.GetValues<Priority>().Length, report.ByPriority.Count);
    }

    [Fact]
    public void Build_DemoProject_MatchesKnownNumbers()
    {
        var report = ReportService.Build(DemoData.Create(1));

        Assert.Equal(9, report.Total);
        Assert.Equal(2, report.Done);
        Assert.Equal(2 * 100.0 / 9, report.Percent, 6);
        Assert.Equal(1020, report.TotalMinutes);          // 240+180+300+150+90+60
        Assert.Equal(1, report.Unassigned);               // «Документация API»
        Assert.Equal(70, report.CandyPoints);             // две выполненные High-задачи в срок: 2 x 35

        Assert.Equal(4, report.ByState[TaskState.Todo]);
        Assert.Equal(2, report.ByState[TaskState.InProgress]);
        Assert.Equal(1, report.ByState[TaskState.Review]);
        Assert.Equal(2, report.ByState[TaskState.Done]);

        Assert.Equal(1, report.ByPriority[Priority.Low]);
        Assert.Equal(2, report.ByPriority[Priority.Medium]);
        Assert.Equal(4, report.ByPriority[Priority.High]);
        Assert.Equal(2, report.ByPriority[Priority.Critical]);

        var overdue = Assert.Single(report.Overdue);      // намеренно просроченная «Настроить CI»
        Assert.Equal("Настроить CI", overdue.Title);
    }

    [Fact]
    public void Build_DemoProject_TopByTimeIsSortedDescending_AndMaxThree()
    {
        var report = ReportService.Build(DemoData.Create(1));

        Assert.Equal(new[] { 420, 300, 150 }, report.TopByTime.Select(t => t.SpentMinutes).ToArray());
    }

    [Fact]
    public void Build_DemoProject_MemberStatsAreCorrect()
    {
        var report = ReportService.Build(DemoData.Create(1));

        var misha = report.Members.Single(m => m.Name == "Миша");
        Assert.Equal(3, misha.Assigned);
        Assert.Equal(1, misha.Done);
        Assert.Equal(540, misha.Minutes);                 // 300 + 150 + 90

        Assert.Equal(4, report.Members.Count);
        Assert.Equal(report.Total - report.Unassigned, report.Members.Sum(m => m.Assigned));
    }

    // ---------- демо-данные ----------

    [Fact]
    public void DemoData_UsesGivenId_AndHasTeamAndTasks()
    {
        var demo = DemoData.Create(42);

        Assert.Equal(42, demo.Id);
        Assert.Equal(4, demo.Team.Count);
        Assert.Equal(9, demo.Tasks.Count);
    }

    [Fact]
    public void DemoData_AllAssigneesExist_AndDeadlinesAreSensible()
    {
        var demo = DemoData.Create(1);

        foreach (var task in demo.Tasks)
        {
            if (task.AssigneeId.HasValue)
                Assert.NotNull(demo.FindMember(task.AssigneeId));
        }

        Assert.Single(demo.Tasks, t => t.IsOverdue);
    }
}
