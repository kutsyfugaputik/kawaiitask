using System.Diagnostics;
using Spectre.Console;

namespace KawaiiTasks.UI;

/// <summary>Живой таймер: считает время работы над задачей, пока не нажмёшь любую клавишу.</summary>
public static class FocusTimer
{
    private const int CycleMinutes = 25;

    // Котик «дышит» и моргает.
    private static readonly string[] Cat =
    {
        "  /\\_/\\\n ( ˘ω˘ )\n  > ♡ <",
        "  /\\_/\\\n ( ˘▽˘ )\n  > ♡ <",
        "  /\\_/\\\n ( -.- )\n  > ♡ <",
        "  /\\_/\\\n ( ˘▽˘ )\n  > ♡ <"
    };

    /// <summary>Возвращает прошедшие минуты (0, если прошло меньше 30 секунд).</summary>
    public static int Run(string taskTitle)
    {
        var watch = Stopwatch.StartNew();

        AnsiConsole.Live(new Markup(" ")).AutoClear(true).Start(ctx =>
        {
            var tick = 0;
            while (!KeyPressed())
            {
                var cat = Cat[(tick / 2) % Cat.Length];
                var dots = new string('.', tick % 4);
                var elapsed = watch.Elapsed.ToString(@"hh\:mm\:ss");

                // кружок-помидорка на 25 минут
                const int barWidth = 28;
                var cycle = watch.Elapsed.TotalMinutes % CycleMinutes / CycleMinutes;
                var filled = Math.Clamp((int)Math.Round(cycle * barWidth), 0, barWidth);
                var rounds = (int)(watch.Elapsed.TotalMinutes / CycleMinutes);
                var bar = $"[{Theme.Cocoa}]「[/][{Theme.Lemon}]{new string('█', filled)}[/][grey]{new string('░', barWidth - filled)}[/][{Theme.Cocoa}]」[/]";
                var stars = rounds > 0 ? new string('★', Math.Min(rounds, 10)) : "☆";

                var art = new Markup($"[{Theme.Pink}]{Theme.E(cat)}[/]   ");
                var info = new Markup(
                    $"[bold {Theme.Lemon}]{elapsed}[/]\n" +
                    $"[grey]{Theme.E(Views.Short(taskTitle, 50))}[/]\n" +
                    $"{bar}  [{Theme.Cocoa}]{stars}[/]\n" +
                    $"[{Theme.Lavender}]работаем{dots}[/]  [grey](любая клавиша — стоп)[/]");

                var grid = new Grid();
                grid.AddColumn(new GridColumn { NoWrap = true });
                grid.AddColumn();
                grid.AddRow(art, info);

                var content = new Rows(
                    new Align(new Markup(Theme.SparkleMarkup(46, tick)), HorizontalAlignment.Center),
                    grid,
                    new Align(new Markup(Theme.SparkleMarkup(46, tick + 7)), HorizontalAlignment.Center));

                ctx.UpdateTarget(Theme.Page(content, "ФОКУС-ТАЙМЕР", Theme.Pink, true, new Padding(2, 0)));
                ctx.Refresh();
                Thread.Sleep(250);
                tick++;
            }
        });

        watch.Stop();
        if (watch.Elapsed.TotalSeconds < 30) return 0;
        return Math.Max(1, (int)Math.Round(watch.Elapsed.TotalMinutes, MidpointRounding.AwayFromZero));
    }

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
            return true; // при недоступности клавиатуры корректно завершаем таймер
        }
    }
}
