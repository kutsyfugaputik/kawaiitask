namespace KawaiiTasks.Models;

/// <summary>Понятная пользователю ошибка предметной области (неверные данные, нарушение правил).</summary>
public class KawaiiException : Exception
{
    public KawaiiException(string message) : base(message) { }
}
