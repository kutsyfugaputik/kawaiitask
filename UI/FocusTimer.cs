using System.Diagnostics;
using Spectre.Console;

namespace KawaiiTasks.UI;

/// <summary>Живой таймер: считает время работы над задачей, пока не нажмёшь любую клавишу.</summary>
public static class FocusTimer
{
    private static readonly string[] Faces = { "( ˘ω˘ )", "( ˘▽˘ )", "( ˘ᴗ˘ )", "( ˘▽˘ )" };

    /// <summary>Возвращает прошедшие минуты (0, если прошло меньше 30 секунд).</summary>
    public static int Run(string taskTitle)
    {
        var watch = Stopwatch.StartNew();

        AnsiConsole.Live(new Markup(" ")).AutoClear(true).Start(ctx =>
        {
            var tick = 0;
            while (!KeyPressed())
            {
                var face = Faces[tick % Faces.Length];
                var dots = new string('.', tick % 4);
                var elapsed = watch.Elapsed.ToString(@"hh\:mm\:ss");
                var content = new Markup(
                    $"[{Theme.Pink}]{Theme.E(face)}[/]  [bold {Theme.Lemon}]{elapsed}[/]\n" +
                    $"[grey]{Theme.E(Views.Short(taskTitle, 50))}[/]\n" +
                    $"[{Theme.Lavender}]работаем{dots}[/]  [grey](любая клавиша — стоп)[/]");

                ctx.UpdateTarget(new Panel(content)
                {
                    Header = new PanelHeader($"[{Theme.Pink}]◷ Фокус-таймер[/]"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Theme.PinkC),
                    Padding = new Padding(2, 0)
                });
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
        catch (InvalidOperationException)
        {
            return true; // ввод перенаправлен — останавливаемся сразу
        }
    }
}
