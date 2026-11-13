using KawaiiTasks.Models;

namespace KawaiiTasks.Services;

public record MemberStat(string Name, string Role, int Assigned, int Done, int Minutes);

public record ProjectReport(
    Project Project,
    DateTime GeneratedAt,
    int Total,
    int Done,
    double Percent,
    Dictionary<TaskState, int> ByState,
    Dictionary<Priority, int> ByPriority,
    int TotalMinutes,
    List<TaskItem> Overdue,
    List<MemberStat> Members,
    int Unassigned,
    int CandyPoints,
    List<TaskItem> TopByTime);

/// <summary>Сбор статистики по проекту + игровая механика «конфеток».</summary>
public static class ReportService
{
    private static readonly string[] LevelNames =
        { "Зефирка", "Маршмеллоу", "Пончик", "Единорожка", "Королева кода" };

    /// <summary>Конфетки за выполненную задачу: приоритет × 10, +5 за сдачу в срок.</summary>
    public static int TaskPoints(TaskItem task)
    {
        if (!task.IsDone) return 0;
        var onTime = task.CompletedAt.HasValue && task.Deadline.HasValue
                     && task.CompletedAt.Value.Date <= task.Deadline.Value.Date;
        return (int)task.Priority * 10 + (onTime ? 5 : 0);
    }

    public static int CandyPoints(Project project) => project.Tasks.Sum(TaskPoints);

    public static (int Level, string Name) Level(int points)
    {
        var index = Math.Max(0, points) / 50;
        return (index + 1, LevelNames[Math.Min(index, LevelNames.Length - 1)]);
    }

    public static ProjectReport Build(Project project)
    {
        var total = project.Tasks.Count;
        var done = project.Tasks.Count(t => t.IsDone);

        var byState = Enum.GetValues<TaskState>()
            .ToDictionary(s => s, s => project.Tasks.Count(t => t.State == s));
        var byPriority = Enum.GetValues<Priority>()
            .ToDictionary(p => p, p => project.Tasks.Count(t => t.Priority == p));

        var members = project.Team.Select(m =>
        {
            var mine = project.Tasks.Where(t => t.AssigneeId == m.Id).ToList();
            return new MemberStat(m.Name, m.Role, mine.Count, mine.Count(t => t.IsDone), mine.Sum(t => t.SpentMinutes));
        }).ToList();

        return new ProjectReport(
            project,
            DateTime.Now,
            total,
            done,
            total == 0 ? 0 : done * 100.0 / total,
            byState,
            byPriority,
            project.Tasks.Sum(t => t.SpentMinutes),
            project.Tasks.Where(t => t.IsOverdue).OrderBy(t => t.Deadline).ToList(),
            members,
            project.Tasks.Count(t => t.AssigneeId is null),
            CandyPoints(project),
            project.Tasks.Where(t => t.SpentMinutes > 0).OrderByDescending(t => t.SpentMinutes).Take(3).ToList());
    }
}
