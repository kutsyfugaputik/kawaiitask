
using System.Text.Json.Serialization; // Подключает атрибут JsonIgnore, позволяющий исключать свойства из JSON-сериализации.

namespace KawaiiTasks.Models; // Указывает пространство имён, к которому относится класс TaskItem.

/// <summary>
/// Задача IT-проекта.
/// Все изменения с проверками идут через методы класса.
/// </summary>
public class TaskItem
{
    // Максимальная допустимая длина названия задачи.
    public const int MaxTitle = 80;

    // Максимальная допустимая длина описания задачи.
    public const int MaxDescription = 500;

    // Максимальная допустимая длина заметки к записи затраченного времени.
    public const int MaxNote = 100;

    // Уникальный идентификатор задачи внутри проекта.
    public int Id { get; set; }

    // Название задачи; по умолчанию используется пустая строка.
    public string Title { get; set; } = "";

    // Подробное описание задачи; по умолчанию пустое.
    public string Description { get; set; } = "";

    // Приоритет задачи; по умолчанию устанавливается средний приоритет.
    public Priority Priority { get; set; } = Priority.Medium;

    // Тип задачи; по умолчанию задача считается новой функциональностью.
    public TaskKind Kind { get; set; } = TaskKind.Feature;

    // Текущий статус задачи; по умолчанию задача ещё не начата.
    public TaskState State { get; set; } = TaskState.Todo;

    /// <summary>
    /// ID исполнителя из Project.Team.
    /// Значение null означает, что задача никому не назначена.
    /// </summary>
    public int? AssigneeId { get; set; }

    // Крайний срок выполнения задачи.
    // Nullable DateTime (?) позволяет не указывать срок.
    public DateTime? Deadline { get; set; }

    // Дата и время создания задачи.
    // DateTime.Now возвращает текущие локальные дату и время.
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Дата и время завершения задачи.
    // Если задача ещё не завершена, значение равно null.
    public DateTime? CompletedAt { get; set; }

    // Список записей о времени, затраченном на выполнение задачи.
    // new() создаёт пустой список TimeEntry.
    public List<TimeEntry> TimeLog { get; set; } = new();

    // JsonIgnore исключает вычисляемое свойство из JSON-файла.
    // Sum складывает количество минут во всех записях TimeLog.
    [JsonIgnore]
    public int SpentMinutes => TimeLog.Sum(t => t.Minutes);

    // Возвращает true, если статус задачи соответствует Done.
    // Свойство вычисляется на основании State, а не хранится отдельно.
    [JsonIgnore]
    public bool IsDone => State == TaskState.Done;

    /// <summary>
    /// Проверяет, просрочена ли задача.
    /// Задача считается просроченной, если она не завершена,
    /// срок указан и его календарная дата раньше сегодняшнего дня.
    /// </summary>
    [JsonIgnore]
    public bool IsOverdue =>
        !IsDone && // Проверяет, что задача ещё не завершена.
        Deadline.HasValue && // Проверяет, что срок выполнения указан.
        Deadline.Value.Date < DateTime.Today; // Сравнивает дату срока с сегодняшней датой.

    /// <summary>
    /// Изменяет название задачи с проверкой корректности.
    /// </summary>
    /// <param name="title">Новое название задачи.</param>
    public void Rename(string? title)
    {
        // Если передано null, вместо него используется пустая строка.
        // Trim() удаляет пробелы в начале и конце текста.
        var value = (title ?? "").Trim();

        // Запрещает задавать пустое название или строку из одних пробелов.
        if (value.Length == 0)
            throw new KawaiiException("Название задачи не может быть пустым.");

        // Проверяет, не превышает ли название установленное ограничение.
        if (value.Length > MaxTitle)
            throw new KawaiiException($"Название слишком длинное (максимум {MaxTitle} символов).");

        // Сохраняет новое название после успешного прохождения проверок.
        Title = value;
    }

    /// <summary>
    /// Изменяет описание задачи с проверкой допустимой длины.
    /// </summary>
    /// <param name="text">Новое описание задачи.</param>
    public void Describe(string? text)
    {
        // Заменяет null пустой строкой и удаляет лишние пробелы по краям.
        var value = (text ?? "").Trim();

        // Проверяет, не превышает ли описание максимально допустимую длину.
        if (value.Length > MaxDescription)
            throw new KawaiiException($"Описание слишком длинное (максимум {MaxDescription} символов).");

        // Сохраняет описание, если проверка пройдена.
        Description = value;
    }

    /// <summary>
    /// Устанавливает срок выполнения задачи.
    /// Сохраняется только календарная дата, без времени.
    /// Передача null снимает установленный срок.
    /// Прошедшие даты запрещены.
    /// </summary>
    /// <param name="deadline">Новая дата выполнения или null.</param>
    public void SetDeadline(DateTime? deadline)
    {
        // Если дата передана, сравнивает её с сегодняшним днём.
        // Value получает значение DateTime из nullable-переменной.
        if (deadline.HasValue && deadline.Value.Date < DateTime.Today)
            throw new KawaiiException("Срок выполнения не может быть в прошлом.");

        // Сохраняет только дату, отбрасывая часы, минуты и секунды.
        // Оператор ?. безопасно обращается к Date только при наличии значения.
        Deadline = deadline?.Date;
    }

    /// <summary>
    /// Изменяет статус задачи.
    /// При переходе в Done сохраняет момент завершения.
    /// При выходе из Done сбрасывает дату завершения.
    /// Повторная установка того же статуса ничего не меняет.
    /// </summary>
    /// <param name="state">Новый статус задачи.</param>
    public void ChangeState(TaskState state)
    {
        // Если статус уже установлен, завершает метод без изменений.
        // Это позволяет не перезаписывать первоначальное время завершения.
        if (State == state) return;

        // Сохраняет новый статус задачи.
        State = state;

        // Если новый статус Done, записывает текущие дату и время.
        // Если статус любой другой, устанавливает CompletedAt в null.
        CompletedAt = state == TaskState.Done ? DateTime.Now : null;
    }

    /// <summary>
    /// Добавляет запись о затраченном времени.
    /// За один раз разрешено записать от 1 минуты до 24 часов.
    /// </summary>
    /// <param name="minutes">Количество затраченных минут.</param>
    /// <param name="note">Необязательная заметка.</param>
    /// <param name="date">Необязательная дата записи.</param>
    /// <returns>Созданная запись о времени.</returns>
    public TimeEntry LogTime(int minutes, string? note = null, DateTime? date = null)
    {
        // Проверяет допустимый диапазон затраченного времени.
        // 24 * 60 соответствует 1440 минутам, то есть 24 часам.
        if (minutes < 1 || minutes > 24 * 60)
            throw new KawaiiException("За один раз можно записать от 1 минуты до 24 часов.");

        // Если заметка не передана, использует пустую строку.
        // Trim() удаляет пробелы в начале и конце заметки.
        var text = (note ?? "").Trim();

        // Проверяет, не превышает ли заметка установленный лимит.
        if (text.Length > MaxNote)
            throw new KawaiiException($"Заметка слишком длинная (максимум {MaxNote} символов).");

        // Создаёт новый объект TimeEntry с помощью инициализатора свойств.
        var entry = new TimeEntry
        {
            // Использует переданную дату или текущие дату и время.
            Date = date ?? DateTime.Now,

            // Сохраняет количество затраченных минут.
            Minutes = minutes,

            // Сохраняет подготовленный текст заметки.
            Note = text
        };

        // Добавляет созданную запись в список учёта времени задачи.
        TimeLog.Add(entry);

        // Возвращает созданный объект, чтобы вызывающий код мог его использовать.
        return entry;
    }
}
