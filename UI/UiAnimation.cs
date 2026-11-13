using Spectre.Console;

namespace KawaiiTasks.UI;

/// <summary>Короткие декоративные эффекты. При перенаправленном вводе/выводе отключаются.</summary>
public static class UiAnimation
{
    /// <summary>Мерцающая звёздочка вместо обычного спиннера.</summary>
    private sealed class StarSpinner : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(110);
        public override bool IsUnicode => true;
        public override IReadOnlyList<string> Frames =>
            new[] { "✧", "✦", "✶", "✷", "✸", "✷", "✶", "✦" };
    }

    private static bool Interactive => !Console.IsInputRedirected && !Console.IsOutputRedirected;

    /// <summary>Показывает компактный индикатор во время короткой операции.</summary>
    public static void Loading(string message, int durationMs = 350)
    {
        if (durationMs <= 0 || !Interactive)
            return;

        try
        {
            AnsiConsole.Status()
                .Spinner(new StarSpinner())
                .SpinnerStyle(new Style(Theme.PinkC))
                .Start($"[{Theme.Lavender}]{Theme.E(message)} ✿[/]", _ => Thread.Sleep(durationMs));
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or NotSupportedException)
        {
            // Анимация декоративная: если терминал её не поддерживает, операция продолжается без неё.
        }
    }

    /// <summary>Заставка при запуске: дневник «открывается», блёстки бегут по строке.</summary>
    public static void Intro()
    {
        if (!Interactive) return;

        try
        {
            var steps = new[]
            {
                "достаём дневничок",
                "ищем любимую ручку",
                "рассыпаем блёстки",
                "открываем первую страничку"
            };
            const int total = 28;

            AnsiConsole.Live(new Markup(" ")).AutoClear(true).Start(ctx =>
            {
                for (var i = 0; i <= total; i++)
                {
                    var step = steps[Math.Min(steps.Length - 1, i * steps.Length / (total + 1))];
                    var bar = $"[{Theme.Cocoa}]「[/][{Theme.Pink}]{new string('█', i)}[/][grey]{new string('░', total - i)}[/][{Theme.Cocoa}]」[/]";
                    var cat = i % 6 < 3 ? "( ^.^ )" : "( -.- )";
                    var body = new Markup(
                        $"{Theme.SparkleMarkup(total + 2, i)}\n\n" +
                        $"[{Theme.Pink}]  /\\_/\\\n {Theme.E(cat)}\n  > ♡ <[/]\n\n" +
                        $"[{Theme.Lavender}]{Theme.E(step)}…[/]\n{bar}\n\n" +
                        $"{Theme.SparkleMarkup(total + 2, i + 5)}");

                    ctx.UpdateTarget(Theme.Page(body, "~ kawaii tasks ~", Theme.Pink, true, new Padding(3, 0)));
                    ctx.Refresh();
                    Thread.Sleep(55);
                }
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or NotSupportedException)
        {
            // Заставка необязательна.
        }
    }

    /// <summary>Конфетти из звёздочек после успешного выполнения задачи.</summary>
    public static void Reward(int points)
    {
        if (points <= 0) return;

        if (Interactive)
        {
            try
            {
                var frames = new[]
                {
                    "           ✧           ",
                    "        ✦  ✧  ✦        ",
                    "     ☆  ✦  ✧  ✦  ☆     ",
                    "  ✧  ☆  ✦  ✧  ✦  ☆  ✧  ",
                    " ·  ✧  ☆  ✧  ✧  ☆  ✧  · ",
                    "  ˚  ·  ✧  ☆  ✧  ·  ˚  "
                };
                var colors = new[] { Theme.Pink, Theme.Lemon, Theme.Mint, Theme.Sky, Theme.Lavender, Theme.Peach };

                AnsiConsole.Live(new Markup(" ")).AutoClear(true).Start(ctx =>
                {
                    for (var i = 0; i < frames.Length * 2; i++)
                    {
                        var f = frames[i % frames.Length];
                        var c = colors[i % colors.Length];
                        ctx.UpdateTarget(new Align(new Markup(
                            $"[{c}]{Theme.E(f)}[/]\n[{Theme.Lemon}]ヽ(>∀<☆)ノ  ◇ +{points}[/]"),
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

        AnsiConsole.MarkupLine($"[{Theme.Lemon}]  ✧ ◇ +{points} очков прогресса! (ﾉ◕ヮ◕)ﾉ ✧[/]");
    }
}
