using KawaiiTasks.Models;

namespace KawaiiTasks.Services;

/// <summary>Готовый пример проекта — чтобы сразу было что показать на защите.</summary>
public static class DemoData
{
    public static Project Create(int id)
    {
        var p = new Project
        {
            Id = id,
            Name = "Демо: приложение «Зефирка»",
            Description = "Мобильное приложение для заказа сладостей — пример проекта с командой и задачами.",
            CreatedAt = DateTime.Now.AddDays(-14)
        };

        var anya = p.AddMember("Аня", "Тимлид");
        var misha = p.AddMember("Миша", "Разработчик");
        var sasha = p.AddMember("Саша", "Тестировщик");
        var lina = p.AddMember("Лина", "Дизайнер");
        var today = DateTime.Today;

        var t1 = p.AddTask("Нарисовать макеты экранов", "Главный экран, корзина и профиль в пастельной гамме.",
            Priority.High, TaskKind.Feature, lina.Id, today.AddDays(3));
        t1.LogTime(240, "Первые наброски", today.AddDays(-5));
        t1.LogTime(180, "Правки после ревью", today.AddDays(-3));
        Finish(t1, today.AddDays(-2));

        var t2 = p.AddTask("Авторизация по e-mail", "Регистрация, вход и восстановление пароля.",
            Priority.High, TaskKind.Feature, misha.Id, today.AddDays(2));
        t2.LogTime(300, "Бэкенд и экран входа", today.AddDays(-4));
        Finish(t2, today.AddDays(-1));

        var t3 = p.AddTask("Каталог товаров с поиском", "Список сладостей, карточка товара, поиск по названию.",
            Priority.High, TaskKind.Feature, misha.Id, today.AddDays(5));
        t3.LogTime(150, "Список и карточка", today.AddDays(-1));
        t3.ChangeState(TaskState.InProgress);

        var t4 = p.AddTask("Корзина и оформление заказа", "Добавление товаров, подсчёт суммы, подтверждение заказа.",
            Priority.Critical, TaskKind.Feature, misha.Id, today.AddDays(1));
        t4.LogTime(90, "Модель корзины", today);
        t4.ChangeState(TaskState.InProgress);

        var t5 = p.AddTask("Баг: падение при пустой корзине", "Приложение закрывается при нажатии «Оформить».",
            Priority.Critical, TaskKind.Bug, sasha.Id, today);
        t5.LogTime(60, "Воспроизведение и анализ", today);
        t5.ChangeState(TaskState.Review);

        p.AddTask("Написать тест-кейсы", "Чек-лист для основных сценариев.",
            Priority.Medium, TaskKind.Chore, sasha.Id, today.AddDays(7));

        p.AddTask("Иконка и сплэш-экран", "Милая зефирка с глазками.",
            Priority.Low, TaskKind.Feature, lina.Id, today.AddDays(10));

        p.AddTask("Документация API", "Описание эндпоинтов для мобильной команды.",
            Priority.Medium, TaskKind.Docs, null, today.AddDays(14));

        var t9 = p.AddTask("Настроить CI", "Автосборка и запуск тестов при каждом коммите.",
            Priority.High, TaskKind.Chore, anya.Id, today.AddDays(1));
        t9.Deadline = today.AddDays(-2); // намеренно просрочена, чтобы показать предупреждения

        return p;
    }

    private static void Finish(TaskItem task, DateTime when)
    {
        task.ChangeState(TaskState.Done);
        task.CompletedAt = when;
    }
}
