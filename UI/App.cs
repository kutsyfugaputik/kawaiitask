using KawaiiTasks.Models;
using KawaiiTasks.Services;
using Spectre.Console;

namespace KawaiiTasks.UI;

/// <summary>Пункт меню: подпись, действие (null = выход/назад) и нужна ли пауза после действия.</summary>
public sealed record MenuItem(string Label, Action? Run, bool Pause = true);

/// <summary>Весь интерактивный сценарий: меню проектов и работа внутри проекта.</summary>
public class App
{
    private readonly ProjectService _svc;
    private readonly string? _warning;

    public App(ProjectService service, string? warning)
    {
        _svc = service;
        _warning = warning;
    }

    public void Run()
    {
        if (_warning is not null)
        {
            Theme.Banner();
            Theme.Fail(_warning);
            Theme.Pause();
        }

        MainLoop();
        Goodbye();
    }

    // ------------------------------------------------------------ общие помощники

    /// <summary>Выполняет действие и превращает любые ошибки в понятные сообщения — программа не падает.</summary>
    private static void Guard(Action action, bool pause = true)
    {
        try
        {
            action();
        }
        catch (KawaiiException ex)
        {
            Theme.Fail(ex.Message);
            pause = true;
        }
        catch (IOException ex)
        {
            Theme.Fail($"Не получилось поработать с файлом: {ex.Message}");
            pause = true;
        }
        catch (UnauthorizedAccessException ex)
        {
            Theme.Fail($"Нет доступа к файлу: {ex.Message}");
            pause = true;
        }
        catch (Exception ex)
        {
            Theme.Fail($"Что-то пошло не так: {ex.Message}");
            pause = true;
        }

        if (pause) Theme.Pause();
    }

    private static TaskItem? PickTask(Project project, string title)
    {
        if (project.Tasks.Count == 0)
        {
            Theme.Hint("Задач пока нет — сначала добавь первую ✿");
            return null;
        }

        var choices = new List<(TaskItem? Task, string Label)> { (null, "← Отмена") };
        foreach (var t in project.Tasks.OrderBy(t => t.Id))
            choices.Add((t, $"#{t.Id} {Views.Short(t.Title, 45)} — {t.State.Text()}"));

        return Input.Pick(title, choices, c => c.Label).Task;
    }

    private static Member? PickMember(Project project, string noneLabel)
    {
        var choices = new List<(Member? Member, string Label)> { (null, noneLabel) };
        foreach (var m in project.Team)
            choices.Add((m, $"♙ {m.Name} — {m.Role}"));

        return Input.Pick("Кого выберем?", choices, c => c.Label).Member;
    }

    // ------------------------------------------------------------ главное меню

    private void MainLoop()
    {
        while (true)
        {
            Theme.Banner();
            AnsiConsole.Write(Mascot.Greeting());
            AnsiConsole.WriteLine();

            var items = new List<MenuItem>();
            foreach (var project in _svc.Projects.ToList())
            {
                var p = project;
                var done = p.Tasks.Count(t => t.IsDone);
                items.Add(new MenuItem($"✿ {p.Name}  ({done}/{p.Tasks.Count})", () => ProjectLoop(p), false));
            }

            items.Add(new MenuItem("✧ Создать проект", CreateProject));
            items.Add(new MenuItem("୨୧ Добавить демо-проект", AddDemo));
            if (_svc.Projects.Count > 0)
                items.Add(new MenuItem("× Удалить проект", DeleteProject));
            items.Add(new MenuItem("(｡･ω･｡)ﾉ Выйти", null));

            var choice = Input.Pick("Что будем делать?", items, i => i.Label);
            if (choice.Run is null) return;
            Guard(choice.Run, choice.Pause);
        }
    }

    private void CreateProject()
    {
        var name = Input.Ask("Название проекта", 60);
        var description = Input.AskOptional("Короткое описание (Enter — пропустить)", 200);
        var project = _svc.CreateProject(name, description);
        Theme.Ok($"Проект «{project.Name}» создан!");
    }

    private void AddDemo()
    {
        _svc.AddDemoProject();
        Theme.Ok("Демо-проект добавлен — загляни внутрь, там уже есть команда и задачи!");
    }

    private void DeleteProject()
    {
        var choices = new List<(Project? Project, string Label)> { (null, "← Отмена") };
        foreach (var p in _svc.Projects)
            choices.Add((p, $"✿ {p.Name}"));

        var picked = Input.Pick("Какой проект удалить?", choices, c => c.Label).Project;
        if (picked is null) return;

        if (!Input.Confirm($"Точно удалить «{picked.Name}» со всеми задачами?"))
        {
            Theme.Hint("Ничего не удалили ♡");
            return;
        }

        _svc.DeleteProject(picked);
        Theme.Ok("Проект удалён.");
    }

    private void Goodbye()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel(new Markup(
            $"[bold {Theme.Pink}]До встречи! (◕‿◕✿)[/]\n[grey]Все данные сохранены:\n{Theme.E(_svc.StoragePath)}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Theme.PinkC),
            Padding = new Padding(2, 0)
        });
    }

    // ------------------------------------------------------------ меню проекта

    private void ProjectLoop(Project p)
    {
        while (true)
        {
            Theme.Banner();
            Views.ProjectHeader(p);

            var items = new List<MenuItem>
            {
                new("▤ Доска задач", () => Views.Board(p, p.Tasks)),
                new("✎ Все задачи (таблица)", () => ShowTable(p)),
                new("＋ Добавить задачу", () => AddTask(p)),
                new("✐ Изменить задачу", () => EditTask(p)),
                new("↻ Сменить статус", () => ChangeState(p)),
                new("♙ Назначить исполнителя", () => Assign(p)),
                new("▦ Установить срок", () => SetDeadline(p)),
                new("◷ Учёт времени", () => TrackTime(p)),
                new("⌕ Поиск и фильтры", () => SearchTasks(p)),
                new("♧ Команда", () => TeamMenu(p), false),
                new("▥ Отчёт о проекте", () => ShowReport(p)),
                new("× Удалить задачу", () => DeleteTask(p)),
                new("← К списку проектов", null)
            };

            var choice = Input.Pick($"Проект «{p.Name}» — что делаем?", items, i => i.Label);
            if (choice.Run is null) return;
            Guard(choice.Run, choice.Pause);
        }
    }

    private static void ShowTable(Project p)
    {
        Views.TaskTable(p, p.Tasks.OrderBy(t => t.Id));
    }

    private void AddTask(Project p)
    {
        var title = Input.Ask("Название задачи", TaskItem.MaxTitle);
        var description = Input.AskOptional("Описание (Enter — пропустить)", TaskItem.MaxDescription);
        var priority = Input.Pick("Приоритет", Enum.GetValues<Priority>(), x => x.Text());
        var kind = Input.Pick("Тип задачи", Enum.GetValues<TaskKind>(), x => x.Text());

        Member? assignee = null;
        if (p.Team.Count > 0)
            assignee = PickMember(p, "(×_×) Пока никому");

        var deadline = Input.AskDate("Срок выполнения");

        var task = _svc.AddTask(p, title, description, priority, kind, assignee?.Id, deadline);
        Theme.Ok($"Задача #{task.Id} «{task.Title}» добавлена!");
    }

    private void EditTask(Project p)
    {
        var task = PickTask(p, "Какую задачу правим?");
        if (task is null) return;

        var fields = new[] { "✎ Название", "「 」 Описание", "◆ Приоритет", "✧ Тип" };
        var field = Input.Pick("Что поменяем?", fields, s => s);

        switch (Array.IndexOf(fields, field))
        {
            case 0:
            {
                var title = Input.Ask($"Новое название (сейчас: {task.Title})", TaskItem.MaxTitle);
                _svc.Update(() => task.Rename(title));
                break;
            }
            case 1:
            {
                var text = Input.AskOptional("Новое описание (Enter — очистить)", TaskItem.MaxDescription);
                _svc.Update(() => task.Describe(text));
                break;
            }
            case 2:
            {
                var priority = Input.Pick("Новый приоритет", Enum.GetValues<Priority>(), x => x.Text());
                _svc.Update(() => task.Priority = priority);
                break;
            }
            default:
            {
                var kind = Input.Pick("Новый тип", Enum.GetValues<TaskKind>(), x => x.Text());
                _svc.Update(() => task.Kind = kind);
                break;
            }
        }

        Theme.Ok($"Задача #{task.Id} обновлена!");
    }

    private void ChangeState(Project p)
    {
        var task = PickTask(p, "Какой задаче меняем статус?");
        if (task is null) return;

        var options = Enum.GetValues<TaskState>().Where(s => s != task.State).ToList();
        var state = Input.Pick($"Сейчас: {task.State.Text()}. Какой статус теперь?", options, s => s.Text());

        var before = ReportService.CandyPoints(p);
        _svc.ChangeState(task, state);
        var gained = ReportService.CandyPoints(p) - before;

        Theme.Ok($"Задача #{task.Id} теперь «{state.Text()}»");
        if (gained > 0)
            AnsiConsole.MarkupLine($"[{Theme.Lemon}]◇ +{gained} конфеток! Моти в восторге ヽ(>∀<☆)ノ[/]");
        else if (gained < 0)
            Theme.Hint("Конфетки вернулись обратно в баночку… но ты ещё заработаешь их ♡");
    }

    private void Assign(Project p)
    {
        if (p.Team.Count == 0)
        {
            Theme.Hint("В команде пока никого нет — добавь участников в разделе «Команда» ♧");
            return;
        }

        var task = PickTask(p, "Кому назначаем задачу?");
        if (task is null) return;

        var member = PickMember(p, "(×_×) Снять исполнителя");
        _svc.Assign(p, task, member);

        Theme.Ok(member is null
            ? $"Исполнитель снят с задачи #{task.Id}"
            : $"{member.Name} теперь отвечает за задачу #{task.Id}");
    }

    private void SetDeadline(Project p)
    {
        var task = PickTask(p, "Для какой задачи ставим срок?");
        if (task is null) return;

        Theme.Hint($"Текущий срок: {Views.Date(task.Deadline)}");
        var deadline = Input.AskDate("Новый срок");
        _svc.SetDeadline(task, deadline);

        Theme.Ok(deadline.HasValue
            ? $"Срок задачи #{task.Id}: {Views.Date(deadline)}"
            : $"Срок задачи #{task.Id} убран");
    }

    private void TrackTime(Project p)
    {
        var task = PickTask(p, "Для какой задачи считаем время?");
        if (task is null) return;

        Views.TaskCard(p, task);
        AnsiConsole.WriteLine();

        var options = new[] { "◷ Запустить таймер", "✎ Добавить время вручную", "← Назад" };
        var picked = Input.Pick("Что сделаем?", options, s => s);

        switch (Array.IndexOf(options, picked))
        {
            case 0:
            {
                var minutes = FocusTimer.Run(task.Title);
                if (minutes == 0)
                {
                    Theme.Hint("Прошло меньше 30 секунд — запись не создана.");
                    return;
                }

                var note = Input.AskOptional("Что делал(а)? (Enter — без заметки)", 100);
                _svc.LogTime(task, minutes, note);
                Theme.Ok($"Записано {Views.Fmt(minutes)} в задачу #{task.Id}");
                break;
            }
            case 1:
            {
                var minutes = Input.AskDuration("Сколько времени? (часы: 1.5 или 1:30)");
                var note = Input.AskOptional("Что делал(а)? (Enter — без заметки)", 100);
                _svc.LogTime(task, minutes, note);
                Theme.Ok($"Записано {Views.Fmt(minutes)} в задачу #{task.Id}");
                break;
            }
        }
    }

    private static string SortLabel(SortKey key) => key switch
    {
        SortKey.Priority => "◆ По приоритету (сначала важные)",
        SortKey.Deadline => "▦ По сроку (сначала ближайшие)",
        SortKey.State => "🍓 По статусу",
        SortKey.Title => "🔤 По названию (А–Я)",
        SortKey.Time => "◷ По затраченному времени",
        _ => key.ToString()
    };

    private void SearchTasks(Project p)
    {
        if (p.Tasks.Count == 0)
        {
            Theme.Hint("Пока нечего искать — задач нет.");
            return;
        }

        var filter = new TaskFilter
        {
            Text = Input.AskOptional("Что ищем? (слово из названия, описания или имя; Enter — пропустить)", 60)
        };

        filter.States = AnsiConsole.Prompt(new MultiSelectionPrompt<TaskState>()
            .Title($"[{Theme.Lavender}]Какие статусы показать? (пробел — выбрать, Enter — готово; ничего не выбрано = все)[/]")
            .NotRequired()
            .HighlightStyle(new Style(Theme.PinkC))
            .UseConverter(s => Theme.E(s.Text()))
            .AddChoices(Enum.GetValues<TaskState>()));

        filter.OnlyPriority = Input.PickOrAny("Приоритет", Enum.GetValues<Priority>(), x => x.Text(), "✧ Любой");

        if (p.Team.Count > 0)
            filter.AssigneeId = PickMember(p, "✧ Любой исполнитель")?.Id;

        filter.OnlyOverdue = Input.Confirm("Показать только просроченные?");
        filter.Sort = Input.Pick("Как отсортировать?", Enum.GetValues<SortKey>(), SortLabel);

        var result = TaskQuery.Apply(p, filter);
        AnsiConsole.WriteLine();
        Theme.Hint($"Найдено задач: {result.Count}");
        Views.TaskTable(p, result);
    }

    private void TeamMenu(Project p)
    {
        while (true)
        {
            Theme.Banner();
            AnsiConsole.MarkupLine($"[{Theme.Pink}]♧ Команда проекта «{Theme.E(p.Name)}»[/]");
            Views.TeamTable(p);
            AnsiConsole.WriteLine();

            var items = new List<MenuItem>
            {
                new("＋ Добавить участника", () => AddMember(p)),
                new("× Убрать участника", () => RemoveMember(p)),
                new("← Назад", null)
            };

            var choice = Input.Pick("Что делаем с командой?", items, i => i.Label);
            if (choice.Run is null) return;
            Guard(choice.Run, choice.Pause);
        }
    }

    private void AddMember(Project p)
    {
        var name = Input.Ask("Имя участника", 40);

        var roles = new[] { "Разработчик", "Тестировщик", "Аналитик", "Дизайнер", "Тимлид", "✎ Другая роль" };
        var role = Input.Pick("Роль в проекте", roles, s => s);
        if (role == roles[^1])
            role = Input.Ask("Какая роль?", 30);

        var member = _svc.AddMember(p, name, role);
        Theme.Ok($"{member.Name} ({member.Role}) теперь в команде!");
    }

    private void RemoveMember(Project p)
    {
        if (p.Team.Count == 0)
        {
            Theme.Hint("Убирать некого — команда пуста.");
            return;
        }

        var member = PickMember(p, "← Отмена");
        if (member is null) return;

        if (!Input.Confirm($"Убрать {member.Name} из команды? Его задачи останутся без исполнителя."))
        {
            Theme.Hint("Всё осталось как было ♡");
            return;
        }

        _svc.RemoveMember(p, member);
        Theme.Ok($"{member.Name} больше не в команде.");
    }

    private void ShowReport(Project p)
    {
        var report = ReportService.Build(p);

        IReportWriter console = new ConsoleReportWriter();
        console.Write(report);
        AnsiConsole.WriteLine();

        if (!Input.Confirm("Сохранить отчёт в Markdown-файл?")) return;

        IReportWriter markdown = new MarkdownReportWriter(Path.Combine(_svc.DataFolder, "reports"));
        var path = markdown.Write(report);
        Theme.Ok($"Отчёт сохранён: {path}");
    }

    private void DeleteTask(Project p)
    {
        var task = PickTask(p, "Какую задачу удаляем?");
        if (task is null) return;

        if (!Input.Confirm($"Удалить задачу #{task.Id} «{task.Title}»?"))
        {
            Theme.Hint("Задача осталась на месте ♡");
            return;
        }

        _svc.DeleteTask(p, task);
        Theme.Ok("Задача удалена.");
    }
}
