namespace KawaiiTasks.Models;

/// <summary>
/// Понятная пользователю ошибка предметной области (неверные данные, нарушение правил).
/// Интерфейс (App.Guard) ловит именно этот тип и показывает текст сообщения в консоли
/// как есть, а остальные исключения считает неожиданными и оборачивает общей фразой.
/// </summary>
public class KawaiiException : Exception
{
    public KawaiiException(string message) : base(message) { }
}
