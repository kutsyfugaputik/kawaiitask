using KawaiiTasks.Models;
using KawaiiTasks.Services;

namespace KawaiiTasks.Tests;

public class TaskQueryTests
{
    private static Project Demo() => DemoData.Create(1);

    [Fact]
    public void EmptyFilter_ReturnsAllTasks()
    {
        var project = Demo();

        var result = TaskQuery.Apply(project, new TaskFilter());

        Assert.Equal(project.Tasks.Count, result.Count);
    }

    [Fact]
    public void Text_IsCaseInsensitive_AndSearchesTitle()
    {
        var result = TaskQuery.Apply(Demo(), new TaskFilter { Text = "БАГ" });

        var task = Assert.Single(result);
        Assert.Contains("Баг", task.Title);
    }

    [Fact]
    public void Text_AlsoMatchesAssigneeName()
    {
        var project = Demo();

        var result = TaskQuery.Apply(project, new TaskFilter { Text = "миша" });

        Assert.Equal(3, result.Count);
        Assert.All(result, t => Assert.Equal("Миша", project.AssigneeName(t)));
    }

    [Fact]
    public void Text_AlsoMatchesDescription()
    {
        var result = TaskQuery.Apply(Demo(), new TaskFilter { Text = "пастельной" });   // есть только в описании макетов

        Assert.Single(result);
    }

    [Fact]
    public void Text_NothingFound_ReturnsEmptyList()
    {
        var result = TaskQuery.Apply(Demo(), new TaskFilter { Text = "такого-нет-нигде" });

        Assert.Empty(result);
    }

    [Fact]
    public void States_FilterKeepsOnlySelected()
    {
        var done = TaskQuery.Apply(Demo(), new TaskFilter { States = { TaskState.Done } });
        var todoAndReview = TaskQuery.Apply(Demo(), new TaskFilter { States = { TaskState.Todo, TaskState.Review } });

        Assert.Equal(2, done.Count);
        Assert.Equal(5, todoAndReview.Count);
    }

    [Fact]
    public void OnlyPriority_FilterWorks()
    {
        var result = TaskQuery.Apply(Demo(), new TaskFilter { OnlyPriority = Priority.Critical });

        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(Priority.Critical, t.Priority));
    }

    [Fact]
    public void Assignee_FilterWorks()
    {
        var project = Demo();
        var misha = project.Team.Single(m => m.Name == "Миша");

        var result = TaskQuery.Apply(project, new TaskFilter { AssigneeId = misha.Id });

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void OnlyOverdue_ReturnsOnlyOverdue()
    {
        var result = TaskQuery.Apply(Demo(), new TaskFilter { OnlyOverdue = true });

        var task = Assert.Single(result);
        Assert.True(task.IsOverdue);
    }

    [Fact]
    public void Filters_AreCombinedWithAnd()
    {
        var project = Demo();
        var misha = project.Team.Single(m => m.Name == "Миша");

        var result = TaskQuery.Apply(project, new TaskFilter
        {
            AssigneeId = misha.Id,
            States = { TaskState.InProgress }
        });

        Assert.Equal(2, result.Count);   // каталог и корзина
        Assert.All(result, t => Assert.Equal(TaskState.InProgress, t.State));
    }

    // ---------- сортировка ----------

    [Fact]
    public void Sort_ByPriority_MostImportantFirst_TiesByIdAscending()
    {
        var result = TaskQuery.Apply(Demo(), new TaskFilter { Sort = SortKey.Priority });

        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(result[i - 1].Priority >= result[i].Priority);
            if (result[i - 1].Priority == result[i].Priority)
                Assert.True(result[i - 1].Id < result[i].Id);
        }
    }

    [Fact]
    public void Sort_ByDeadline_NearestFirst_WithoutDeadlineLast()
    {
        var project = Make.NewProject();
        var noDeadline = Make.NewTask(project, "Без срока");
        var far = Make.NewTask(project, "Далеко", deadline: DateTime.Today.AddDays(10));
        var near = Make.NewTask(project, "Близко", deadline: DateTime.Today.AddDays(1));

        var result = TaskQuery.Apply(project, new TaskFilter { Sort = SortKey.Deadline });

        Assert.Equal(new[] { near.Id, far.Id, noDeadline.Id }, result.Select(t => t.Id).ToArray());
    }

    [Fact]
    public void Sort_ByTitle_IgnoresCase()
    {
        var project = Make.NewProject();
        Make.NewTask(project, "бета");
        Make.NewTask(project, "Альфа");
        Make.NewTask(project, "Гамма");

        var result = TaskQuery.Apply(project, new TaskFilter { Sort = SortKey.Title });

        Assert.Equal(new[] { "Альфа", "бета", "Гамма" }, result.Select(t => t.Title).ToArray());
    }

    [Fact]
    public void Sort_ByTime_MostTimeFirst()
    {
        var result = TaskQuery.Apply(Demo(), new TaskFilter { Sort = SortKey.Time });

        Assert.Equal(420, result[0].SpentMinutes);
        for (var i = 1; i < result.Count; i++)
            Assert.True(result[i - 1].SpentMinutes >= result[i].SpentMinutes);
    }

    [Fact]
    public void Sort_ByState_FollowsBoardOrder()
    {
        var result = TaskQuery.Apply(Demo(), new TaskFilter { Sort = SortKey.State });

        for (var i = 1; i < result.Count; i++)
            Assert.True(result[i - 1].State <= result[i].State);
    }

    [Fact]
    public void Apply_DoesNotChangeProjectTaskOrder()
    {
        var project = Demo();
        var idsBefore = project.Tasks.Select(t => t.Id).ToArray();

        TaskQuery.Apply(project, new TaskFilter { Sort = SortKey.Title });

        Assert.Equal(idsBefore, project.Tasks.Select(t => t.Id).ToArray());
    }
}
