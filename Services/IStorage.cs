using KawaiiTasks.Models;

namespace KawaiiTasks.Services;

/// <summary>Абстракция хранилища: сейчас JSON, при желании можно заменить на SQLite/XML.</summary>
public interface IStorage
{
    string Location { get; }
    string? LoadWarning { get; }
    AppData Load();
    void Save(AppData data);
}
