using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using KawaiiTasks.Models;

namespace KawaiiTasks.Services;

/// <summary>Хранение данных в JSON-файле (переживает перезапуск программы).</summary>
public class JsonStorage : IStorage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Location { get; }
    public string? LoadWarning { get; private set; }

    public JsonStorage(string? path = null)
    {
        if (path is not null)
        {
            Location = path;
            return;
        }

        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root))
            root = AppContext.BaseDirectory;
        Location = Path.Combine(root, "KawaiiTasks", "data.json");
    }

    public AppData Load()
    {
        if (!File.Exists(Location))
            return new AppData();

        try
        {
            var json = File.ReadAllText(Location);
            return JsonSerializer.Deserialize<AppData>(json, Options) ?? new AppData();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            var backup = $"{Location}.broken-{DateTime.Now:yyyyMMddHHmmss}";
            try { File.Copy(Location, backup, true); } catch { /* копия — приятный бонус, не обязанность */ }
            LoadWarning = $"Файл данных не удалось прочитать ({ex.Message}). Начинаем с чистого листа, старый файл сохранён как {Path.GetFileName(backup)}.";
            return new AppData();
        }
    }

    public void Save(AppData data)
    {
        var folder = Path.GetDirectoryName(Location);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);

        // Сначала пишем во временный файл — так данные не потеряются при сбое.
        var temp = Location + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
        File.Move(temp, Location, true);
    }
}
