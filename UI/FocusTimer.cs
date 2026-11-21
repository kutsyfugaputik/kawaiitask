using System.Diagnostics;
using Spectre.Console;

namespace KawaiiTasks.UI;

/// <summary>
/// Таймер работы над задачей. Любая клавиша останавливает отсчет.
///
/// Как это работает в терминале: цикл раз в 250 мс перерисовывает экран таймера (AnsiConsole.Live), а между
/// перерисовками без блокировки проверяет, не нажата ли клавиша (Console.KeyAvailable). Если бы мы просто вызвали
/// Console.ReadKey, программа замерла бы в ожидании нажатия и не могла бы обновлять секундомер.
/// </summary>
public static class FocusTimer
{
    private const int CycleMinutes = 25;   // длина одного "помидора" (метод Pomodoro) - полоска прогресса идёт по кругу

    private static readonly string[] Cat =
    {
        "  /\\_/\\\n ( -.- )\n  >   <",
        "  /\\_/\\\n ( o.o )\n  >   <",
        "  /\\_/\\\n ( -.- )\n  >   <",
        "  /\\_/\\\n ( o.o )\n  >   <"
    };

    /// <summary>Возвращает прошедшие минуты (0, если прошло меньше 30 секунд).</summary>
    public static int Run(string taskTitle)
    {
        // Stopwatch считает реальное время, поэтому не зависит от того, как долго рисуется кадр.
        var watch = Stopwatch.StartNew();

        // Без настоящего терминала (ввод/вывод перенаправлены) ждать нажатия клавиши невозможно,
        // поэтому анимацию пропускаем, и метод сразу возвращает 0 минут.
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            AnsiConsole.Live(new Markup(" ")).AutoClear(true).Start(ctx =>
            {
                var tick = 0;
                while (!KeyPressed())
                {
                    // Кадр котика меняется каждые 2 тика (= 0,5 секунды), точки "работаем..." - каждый тик.
                    var cat = Cat[(tick / 2) % Cat.Length];
                    var dots = new string('.', tick % 4);
                    var elapsed = watch.Elapsed.ToString(@"hh\:mm\:ss");

                    // Полоска прогресса: доля прошедшего времени внутри текущих 25 минут, умноженная на ширину полоски.
                    const int barWidth = 28;
                    var cycle = watch.Elapsed.TotalMinutes % CycleMinutes / CycleMinutes;
                    var filled = Math.Clamp((int)Math.Round(cycle * barWidth), 0, barWidth);
                    var rounds = (int)(watch.Elapsed.TotalMinutes / CycleMinutes);
                    var bar = $"[{Theme.Cocoa}]|[/][{Theme.Lemon}]{new string('#', filled)}[/][grey]{new string('-', barWidth - filled)}[/][{Theme.Cocoa}]|[/]";
                    var stars = rounds > 0 ? new string('*', Math.Min(rounds, 10)) : "-";

                    var art = new Markup($"[{Theme.Pink}]{Theme.E(cat)}[/]   ");
                    var info = new Markup(
                        $"[bold {Theme.Lemon}]{elapsed}[/]\n" +
                        $"[grey]{Theme.E(Views.Short(taskTitle, 50))}[/]\n" +
                        $"{bar}  [{Theme.Cocoa}]{stars}[/]\n" +
                        $"[{Theme.Lavender}]работаем{dots}[/]  [grey](любая клавиша - стоп)[/]");

                    var grid = new Grid();
                    grid.AddColumn(new GridColumn { NoWrap = true });
                    grid.AddColumn();
                    grid.AddRow(art, info);
                    var content = new Rows(
                        new Align(new Markup($"[{Theme.Pink}]* * * * * * * * * *[/]"), HorizontalAlignment.Center),
                        grid,
                        new Align(new Markup($"[{Theme.Pink}]* * * * * * * * * *[/]"), HorizontalAlignment.Center));

                    ctx.UpdateTarget(Theme.Page(content, "ФОКУС-ТАЙМЕР", Theme.Pink, true, new Padding(2, 0)));
                    ctx.Refresh();        // реально отправляет новый кадр в терминал
                    Thread.Sleep(250);    // 4 кадра в секунду: плавно для глаза и почти не нагружает процессор
                    tick++;
                }
            });
        }

        watch.Stop();
        // Случайные короткие запуски (меньше 30 секунд) не записываем, чтобы не засорять журнал времени.
        if (watch.Elapsed.TotalSeconds < 30) return 0;
        return Math.Max(1, (int)Math.Round(watch.Elapsed.TotalMinutes, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Неблокирующая проверка клавиатуры. KeyAvailable отвечает "есть ли нажатие в буфере ввода" мгновенно.
    /// ReadKey(true) забирает нажатие из буфера; true = intercept, символ не печатается на экране.
    /// </summary>
    private static bool KeyPressed()
    {
        try
        {
            if (!Console.KeyAvailable) return false;
            Console.ReadKey(true);
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or NotSupportedException)
        {
            // Клавиатура недоступна (например, ввод перенаправили): лучше остановить таймер, чем крутить его вечно.
            return true;
        }
    }
}
