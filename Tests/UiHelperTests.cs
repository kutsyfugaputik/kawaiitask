using System.Text.RegularExpressions;
using KawaiiTasks.Models;
using KawaiiTasks.UI;

namespace KawaiiTasks.Tests;

/// <summary>
/// Тесты "чистых" помощников интерфейса: разбор ввода, форматирование, настроение талисмана.
/// Сам вывод в терминал здесь не проверяется - для него нужен настоящий терминал.
/// </summary>
public class UiHelperTests
{
    // ---------- ввод длительности ----------

    [Theory]
    [InlineData("1.5", 90)]
    [InlineData("1,5", 90)]       // русская запятая
    [InlineData("0.5", 30)]
    [InlineData("2", 120)]
    [InlineData("1:30", 90)]
    [InlineData("0:01", 1)]
    [InlineData("24", 1440)]
    [InlineData("24:00", 1440)]
    [InlineData("0.01", 1)]       // 0,6 минуты округляется до 1
    [InlineData("  2:15  ", 135)] // пробелы по краям игнорируются
    public void TryParseDuration_ValidInput(string text, int expectedMinutes)
    {
        Assert.True(Input.TryParseDuration(text, out var minutes));
        Assert.Equal(expectedMinutes, minutes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("0:00")]
    [InlineData("0.008")]     // округляется до 0 минут
    [InlineData("-1")]
    [InlineData("25")]
    [InlineData("24:01")]
    [InlineData("1:60")]
    [InlineData("1:2:3")]
    [InlineData("1:-5")]
    [InlineData("1e1")]       // экспонента не допускается
    [InlineData("NaN")]
    [InlineData("1 : 30")]
    public void TryParseDuration_InvalidInput(string? text)
    {
        Assert.False(Input.TryParseDuration(text, out var minutes));
        Assert.Equal(0, minutes);
    }

    // ---------- ввод даты ----------

    [Theory]
    [InlineData("25.12.2026", 2026, 12, 25)]
    [InlineData("5.3.2027", 2027, 3, 5)]
    [InlineData("05.03.27", 2027, 3, 5)]
    [InlineData("  01.01.2030 ", 2030, 1, 1)]
    public void TryParseDate_ValidInput(string text, int year, int month, int day)
    {
        Assert.True(Input.TryParseDate(text, out var date));
        Assert.Equal(new DateTime(year, month, day), date);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026-12-25")]
    [InlineData("31.02.2026")]
    [InlineData("32.01.2026")]
    [InlineData("25/12/2026")]
    [InlineData("завтра")]
    public void TryParseDate_InvalidInput(string? text)
    {
        Assert.False(Input.TryParseDate(text, out _));
    }

    // ---------- да / нет ----------

    [Theory]
    [InlineData("да")]
    [InlineData("ДА")]
    [InlineData("д")]
    [InlineData("y")]
    [InlineData(" Y ")]
    public void ParseYesNo_Yes(string text) => Assert.True(Input.ParseYesNo(text));

    [Theory]
    [InlineData("нет")]
    [InlineData("Нет")]
    [InlineData("н")]
    [InlineData("n")]
    [InlineData("")]        // пустой ввод = безопасное "нет"
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseYesNo_No(string? text) => Assert.False(Input.ParseYesNo(text));

    [Theory]
    [InlineData("может быть")]
    [InlineData("yes")]
    [InlineData("1")]
    public void ParseYesNo_Unknown_ReturnsNull(string text) => Assert.Null(Input.ParseYesNo(text));

    // ---------- форматирование ----------

    [Theory]
    [InlineData(0, "-")]
    [InlineData(-5, "-")]
    [InlineData(5, "0ч 05м")]
    [InlineData(60, "1ч 00м")]
    [InlineData(135, "2ч 15м")]
    [InlineData(1440, "24ч 00м")]
    public void Views_Fmt(int minutes, string expected) => Assert.Equal(expected, Views.Fmt(minutes));

    [Fact]
    public void Views_Date_FormatsOrDash()
    {
        Assert.Equal("05.03.2027", Views.Date(new DateTime(2027, 3, 5)));
        Assert.Equal("-", Views.Date(null));
    }

    [Theory]
    [InlineData("коротко", 10, "коротко")]
    [InlineData("1234567890", 10, "1234567890")]       // ровно по длине - без многоточия
    [InlineData("12345678901", 10, "1234567...")]
    [InlineData("очень длинное название", 10, "очень д...")]
    [InlineData("abcdef", 3, "abc")]                   // слишком узко для многоточия
    [InlineData("abc", 0, "")]
    public void Views_Short(string text, int max, string expected) => Assert.Equal(expected, Views.Short(text, max));

    // ---------- тема ----------

    [Fact]
    public void Theme_FromHex_ParsesChannels()
    {
        var color = Theme.FromHex("#ff9ecb");

        Assert.Equal(0xff, color.R);
        Assert.Equal(0x9e, color.G);
        Assert.Equal(0xcb, color.B);
        Assert.Equal(Theme.FromHex("ff9ecb"), color);   // # необязательна
    }

    [Fact]
    public void Theme_AllPaletteColorsAreValidHex()
    {
        foreach (var hex in new[] { Theme.Pink, Theme.Lavender, Theme.Mint, Theme.Peach, Theme.Sky,
                                    Theme.Lemon, Theme.Berry, Theme.Cocoa, Theme.Rose, Theme.PaperBg, Theme.PaperFg })
            Assert.Matches("^#[0-9a-fA-F]{6}$", hex);
    }

    [Fact]
    public void Theme_E_EscapesSquareBrackets_AndHandlesNull()
    {
        Assert.Equal("[[red]]x[[/]]", Theme.E("[red]x[/]"));
        Assert.Equal("", Theme.E(null));
        Assert.Equal("без скобок", Theme.E("без скобок"));
    }

    [Fact]
    public void Theme_E_MakesUserTextSafeForSpectreMarkup()
    {
        var ex = Record.Exception(() => new Spectre.Console.Markup($"[bold]{Theme.E("Баг [срочно] и [/] ещё")}[/]"));

        Assert.Null(ex);
    }

    [Fact]
    public void Theme_StateIcons_AreDistinct_AndAvoidQuestionMark()
    {
        var icons = Enum.GetValues<TaskState>().Select(Theme.StateIcon).ToList();

        Assert.Equal(icons.Count, icons.Distinct().Count());
        Assert.DoesNotContain("?", icons);
    }

    [Fact]
    public void Theme_PriorityStars_GrowWithPriority()
    {
        Assert.Equal("*...", Theme.PriorityStars(Priority.Low));
        Assert.Equal("****", Theme.PriorityStars(Priority.Critical));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(41)]
    public void Theme_SparkleMarkup_HasOneMarkPerTwoColumns(int width)
    {
        var markup = Theme.SparkleMarkup(width);

        var marks = Regex.Matches(markup, @"\[/\]").Count;
        Assert.Equal(width / 2, marks);
    }

    // ---------- талисман ----------

    [Fact]
    public void Mascot_Mood_DependsOnProjectState()
    {
        var project = Make.NewProject();
        Assert.Equal(Mood.Sleepy, Mascot.MoodFor(project));               // нет задач

        var task = Make.NewTask(project, "A");
        Assert.Equal(Mood.Happy, Mascot.MoodFor(project));                // всё спокойно

        task.Deadline = DateTime.Today.AddDays(-1);
        Assert.Equal(Mood.Worried, Mascot.MoodFor(project));              // есть просрочка

        task.ChangeState(TaskState.Done);
        Assert.Equal(Mood.Party, Mascot.MoodFor(project));                // всё готово
    }

    [Fact]
    public void Mascot_HasFaceArtColorAndPhrase_ForEveryMood()
    {
        foreach (var mood in Enum.GetValues<Mood>())
        {
            Assert.False(string.IsNullOrWhiteSpace(Mascot.Face(mood)));
            Assert.False(string.IsNullOrWhiteSpace(Mascot.Art(mood)));
            Assert.False(string.IsNullOrWhiteSpace(Mascot.Phrase(mood)));
            Assert.Matches("^#[0-9a-fA-F]{6}$", Mascot.ColorFor(mood));
        }
    }

    // ---------- подписи перечислений ----------

    [Fact]
    public void EnumText_EveryValueHasUniqueNonEmptyLabel()
    {
        var priorities = Enum.GetValues<Priority>().Select(p => p.Text()).ToList();
        var states = Enum.GetValues<TaskState>().Select(s => s.Text()).ToList();
        var kinds = Enum.GetValues<TaskKind>().Select(k => k.Text()).ToList();

        foreach (var labels in new[] { priorities, states, kinds })
        {
            Assert.All(labels, label => Assert.False(string.IsNullOrWhiteSpace(label)));
            Assert.Equal(labels.Count, labels.Distinct().Count());
        }
    }
}
