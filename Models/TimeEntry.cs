namespace KawaiiTasks.Models;

/// <summary>Запись о затраченном на задачу времени (одна сессия работы).</summary>
public class TimeEntry
{
    /// <summary>Когда работали (для записей из фокус-таймера - момент сохранения).</summary>
    public DateTime Date { get; set; } = DateTime.Now;

    /// <summary>Длительность в минутах, от 1 до 1440 (24 часа) - границы проверяет <see cref="TaskItem.LogTime"/>.</summary>
    public int Minutes { get; set; }

    public string Note { get; set; } = "";
}
