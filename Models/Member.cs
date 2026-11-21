namespace KawaiiTasks.Models;

/// <summary>Участник команды проекта (потенциальный исполнитель задач).</summary>
public class Member
{
    /// <summary>Идентификатор уникален только внутри одного проекта.</summary>
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Участник";
}
