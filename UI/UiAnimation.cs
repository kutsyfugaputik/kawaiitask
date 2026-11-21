using Spectre.Console;

namespace KawaiiTasks.UI;

/// <summary>
/// Небольшие ASCII-анимации. При перенаправлении ввода/вывода отключаются.
///
/// Как работает анимация в терминале: у терминала нет "кадров". Программа печатает текст, затем отправляет
/// управляющие последовательности ANSI ("подними курсор на N строк", "сотри строку") и печатает следующий кадр
/// поверх предыдущего. Всю эту работу выполняет Spectre.Console (AnsiConsole.Live и AnsiConsole.Status),
/// нам остаётся лишь подготовить содержимое каждого кадра и подождать между кадрами (Thread.Sleep).
/// </summary>
public static class UiAnimation
{
    // Свой спиннер: стандартные используют Unicode-символы, которые могут не отобразиться в старых консолях.
    private sealed class StarSpinner : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(140);
        public override bool IsUnicode => false;
        public override IReadOnlyList<string> Frames => new[] { "-", "\\", "|", "/" };
    }

    // Живая анимация имеет смысл только в настоящем терминале. Если вывод идёт в файл или канал,
    // управляющие последовательности превратились бы в мусор в тексте, а Thread.Sleep лишь замедлил бы программу.
    private static bool Interactive => !Console.IsInputRedirected && !Console.IsOutputRedirected;

    /// <summary>Короткий спиннер с подписью, пока "выполняется" действие (чисто для приятного ощущения).</summary>
    public static void Loading(string message, int durationMs = 350)
    {
        if (durationMs <= 0 || !Interactive) return;

        try
        {
            // Status показывает вращающийся символ на отдельной строке и перерисовывает его по таймеру.
            AnsiConsole.Status()
                .Spinner(new StarSpinner())
                .SpinnerStyle(new Style(Theme.PinkC))
                .Start($"[{Theme.Lavender}]{Theme.E(message)}...[/]", _ => Thread.Sleep(durationMs));
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or NotSupportedException)
        {
            // Операция не зависит от декоративной анимации.
        }
    }

    /// <summary>Заставка при запуске: прогресс-бар и котик, 25 кадров по 60 мс (около полутора секунд).</summary>
    public static void Intro()
    {
        if (!Interactive) return;

        try
        {
            var steps = new[]
            {
                "достаем дневничок",
                "готовим рабочее место",
                "проверяем задачи",
                "открываем приложение"
            };
            const int total = 24;

            // Live: область экрана, которую можно целиком заменять (UpdateTarget) и перерисовывать (Refresh).
            // AutoClear(true) стирает область после окончания, и заставка не остаётся в истории терминала.
            AnsiConsole.Live(new Markup(" ")).AutoClear(true).Start(ctx =>
            {
                for (var i = 0; i <= total; i++)
                {
                    var step = steps[Math.Min(steps.Length - 1, i * steps.Length / (total + 1))];
                    var bar = $"[{Theme.Cocoa}]|[/][{Theme.Pink}]{new string('#', i)}[/][grey]{new string('-', total - i)}[/][{Theme.Cocoa}]|[/]";
                    var cat = i % 6 < 3 ? "( ^.^ )" : "( -.- )";
                    var body = new Markup(
                        $"[{Theme.Pink}]*  *  *  *  *  *  *[/]\n\n" +
                        $"[{Theme.Pink}]  /\\_/\\\n {Theme.E(cat)}\n  >   <[/]\n\n" +
                        $"[{Theme.Lavender}]{Theme.E(step)}...[/]\n{bar}\n\n" +
                        $"[{Theme.Pink}]*  *  *  *  *  *  *[/]");

                    ctx.UpdateTarget(Theme.Page(body, "KAWAII TASKS", Theme.Pink, true, new Padding(3, 0)));
                    ctx.Refresh();
                    Thread.Sleep(60);
                }
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or NotSupportedException)
        {
            // Заставка необязательна.
        }
    }

    /// <summary>Салют из звёздочек при получении баллов; итоговая строка печатается всегда, даже без анимации.</summary>
    public static void Reward(int points)
    {
        if (points <= 0) return;

        if (Interactive)
        {
            try
            {
                var frames = new[]
                {
                    "          *          ",
                    "       *  +  *       ",
                    "    +  *  +  *  +    ",
                    "  *  +  *  +  *  +  *",
                    " .  *  +  *  +  *  . ",
                    "  +  .  *  +  .  +  "
                };
                var colors = new[] { Theme.Pink, Theme.Lemon, Theme.Mint, Theme.Sky, Theme.Lavender, Theme.Peach };

                AnsiConsole.Live(new Markup(" ")).AutoClear(true).Start(ctx =>
                {
                    for (var i = 0; i < frames.Length * 2; i++)
                    {
                        var frame = frames[i % frames.Length];
                        var color = colors[i % colors.Length];
                        ctx.UpdateTarget(new Align(new Markup(
                            $"[{color}]{Theme.E(frame)}[/]\n[{Theme.Lemon}](^_^)/  +{points}[/]"),
                            HorizontalAlignment.Center));
                        ctx.Refresh();
                        Thread.Sleep(90);
                    }
                });
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or NotSupportedException)
            {
                // Эффект декоративный.
            }
        }

        AnsiConsole.MarkupLine($"[{Theme.Lemon}]  * +{points} очков прогресса![/]");
    }
}
