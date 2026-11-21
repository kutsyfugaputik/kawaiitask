using KawaiiTasks.Models;

namespace KawaiiTasks.Tests;

public class TaskItemTests
{
    [Fact]
    public void Rename_TrimsSpaces()
    {
        var task = new TaskItem();
        task.Rename("  Сделать отчёт  ");
        Assert.Equal("Сделать отчёт", task.Title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Rename_EmptyTitle_Throws(string? title)
    {
        var task = new TaskItem();
        Assert.Throws<KawaiiException>(() => task.Rename(title));
    }

    [Fact]
    public void Rename_MaxLengthIsAllowed_OneMoreIsNot()
    {
        var task = new TaskItem();
        task.Rename(new string('а', TaskItem.MaxTitle));
        Assert.Equal(TaskItem.MaxTitle, task.Title.Length);

        Assert.Throws<KawaiiException>(() => task.Rename(new string('а', TaskItem.MaxTitle + 1)));
    }

    [Fact]
    public void Rename_Failure_KeepsOldTitle()
    {
        var task = new TaskItem();
        task.Rename("Старое");
        Assert.Throws<KawaiiException>(() => task.Rename(""));
        Assert.Equal("Старое", task.Title);
    }

    [Fact]
    public void Describe_NullBecomesEmpty_TooLongThrows()
    {
        var task = new TaskItem();
        task.Describe(null);
        Assert.Equal("", task.Description);

        Assert.Throws<KawaiiException>(() => task.Describe(new string('x', TaskItem.MaxDescription + 1)));
    }

    [Fact]
    public void SetDeadline_PastDate_Throws()
    {
        var task = new TaskItem();
        Assert.Throws<KawaiiException>(() => task.SetDeadline(DateTime.Today.AddDays(-1)));
    }

    [Fact]
    public void SetDeadline_TodayAndFuture_AreAllowed_AndTimeIsDropped()
    {
        var task = new TaskItem();

        task.SetDeadline(DateTime.Today);
        Assert.Equal(DateTime.Today, task.Deadline);

        task.SetDeadline(DateTime.Today.AddDays(3).AddHours(15));
        Assert.Equal(DateTime.Today.AddDays(3), task.Deadline);   // время суток отброшено
    }

    [Fact]
    public void SetDeadline_Null_ClearsDeadline()
    {
        var task = new TaskItem();
        task.SetDeadline(DateTime.Today.AddDays(1));
        task.SetDeadline(null);
        Assert.Null(task.Deadline);
    }

    [Fact]
    public void ChangeState_ToDone_SetsCompletedAt_AndBackClearsIt()
    {
        var task = new TaskItem();

        task.ChangeState(TaskState.Done);
        Assert.True(task.IsDone);
        Assert.NotNull(task.CompletedAt);

        task.ChangeState(TaskState.InProgress);
        Assert.False(task.IsDone);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void ChangeState_SameStateAgain_DoesNotOverwriteCompletedAt()
    {
        var task = new TaskItem();
        task.ChangeState(TaskState.Done);
        var first = task.CompletedAt;

        task.ChangeState(TaskState.Done);

        Assert.Equal(first, task.CompletedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1441)]
    public void LogTime_OutOfRange_Throws(int minutes)
    {
        var task = new TaskItem();
        Assert.Throws<KawaiiException>(() => task.LogTime(minutes));
        Assert.Empty(task.TimeLog);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(90)]
    [InlineData(1440)]
    public void LogTime_BoundaryValues_AreAccepted(int minutes)
    {
        var task = new TaskItem();
        task.LogTime(minutes, "  заметка ");

        Assert.Single(task.TimeLog);
        Assert.Equal(minutes, task.SpentMinutes);
        Assert.Equal("заметка", task.TimeLog[0].Note);
    }

    [Fact]
    public void LogTime_TooLongNote_Throws()
    {
        var task = new TaskItem();
        Assert.Throws<KawaiiException>(() => task.LogTime(10, new string('x', TaskItem.MaxNote + 1)));
    }

    [Fact]
    public void SpentMinutes_IsSumOfAllEntries()
    {
        var task = new TaskItem();
        task.LogTime(30);
        task.LogTime(45);
        Assert.Equal(75, task.SpentMinutes);
    }

    [Fact]
    public void IsOverdue_OnlyWhenNotDoneAndDeadlineIsBeforeToday()
    {
        var task = new TaskItem { Deadline = DateTime.Today.AddDays(-1) };
        Assert.True(task.IsOverdue);

        task.ChangeState(TaskState.Done);
        Assert.False(task.IsOverdue);          // готовая задача не просрочена

        var today = new TaskItem { Deadline = DateTime.Today };
        Assert.False(today.IsOverdue);          // срок "сегодня" ещё не просрочен

        var noDeadline = new TaskItem();
        Assert.False(noDeadline.IsOverdue);     // без срока просрочки нет
    }
}
