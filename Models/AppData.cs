namespace KawaiiTasks.Models;

/// <summary>
/// Корень данных приложения - именно этот объект целиком сериализуется в JSON-файл
/// (см. <see cref="Services.JsonStorage"/>). Всё остальное (проекты, задачи, команда)
/// лежит внутри него, поэтому одного вызова Save достаточно, чтобы сохранить всё.
/// </summary>
public class AppData
{
    /// <summary>Следующий свободный идентификатор проекта (счётчик растёт и никогда не переиспользуется).</summary>
    public int NextProjectId { get; set; } = 1;

    public List<Project> Projects { get; set; } = new();

    /// <summary>
    /// Приводит данные, прочитанные из JSON, в согласованное состояние.
    /// Файл могли отредактировать вручную или он мог частично повредиться, поэтому
    /// мы не доверяем ему слепо: чиним null-списки и счётчики идентификаторов,
    /// иначе новые записи получили бы уже занятые ID.
    /// </summary>
    public void Normalize()
    {
        Projects ??= new();

        var maxId = Projects.Select(p => p.Id).DefaultIfEmpty(0).Max();
        NextProjectId = Math.Max(NextProjectId, maxId + 1);

        foreach (var project in Projects)
            project.Normalize();
    }
}
