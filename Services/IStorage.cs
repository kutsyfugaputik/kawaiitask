using KawaiiTasks.Models;

namespace KawaiiTasks.Services;

/// <summary>
/// Абстракция хранилища: сейчас JSON, при желании можно заменить на SQLite/XML.
/// Благодаря интерфейсу <see cref="ProjectService"/> не знает, где лежат данные,
/// а в тестах вместо файла подставляется хранилище в памяти.
/// </summary>
public interface IStorage
{
    /// <summary>Где лежат данные (показывается пользователю при выходе из программы).</summary>
    string Location { get; }

    /// <summary>Сообщение о проблеме при чтении (например, повреждённый файл); null, если всё прошло гладко.</summary>
    string? LoadWarning { get; }

    AppData Load();
    void Save(AppData data);
}
