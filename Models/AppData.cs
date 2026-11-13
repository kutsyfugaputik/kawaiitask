namespace KawaiiTasks.Models;

/// <summary>Корень данных приложения — именно он сохраняется в JSON.</summary>
public class AppData
{
    public int NextProjectId { get; set; } = 1;
    public List<Project> Projects { get; set; } = new();
}
