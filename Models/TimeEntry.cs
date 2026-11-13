namespace KawaiiTasks.Models;

/// <summary>Запись о затраченном на задачу времени.</summary>
public class TimeEntry
{
    public DateTime Date { get; set; } = DateTime.Now;
    public int Minutes { get; set; }
    public string Note { get; set; } = "";
}
