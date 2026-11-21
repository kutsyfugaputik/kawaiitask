using KawaiiTasks.Models;
using KawaiiTasks.Services;

namespace KawaiiTasks.Tests;

public class MarkdownReportWriterTests
{
    private static string WriteReport(TempDir dir, Project project, out string text)
    {
        var writer = new MarkdownReportWriter(dir.File("reports"));
        var path = writer.Write(ReportService.Build(project));
        Assert.NotNull(path);
        text = File.ReadAllText(path!);
        return path!;
    }

    [Fact]
    public void Write_CreatesFolderAndFile_WithExpectedName()
    {
        using var dir = new TempDir();

        var path = WriteReport(dir, DemoData.Create(1), out _);

        Assert.True(File.Exists(path));
        Assert.Equal(dir.File("reports"), Path.GetDirectoryName(path));
        var name = Path.GetFileName(path);
        Assert.StartsWith("report_", name);
        Assert.EndsWith(".md", name);
    }

    [Fact]
    public void Write_UsesUtf8WithBom_ForWindowsNotepad()
    {
        using var dir = new TempDir();

        var path = WriteReport(dir, DemoData.Create(1), out _);

        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 3);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
    }

    [Fact]
    public void Write_ContainsSummaryTeamAndTaskTables()
    {
        using var dir = new TempDir();

        WriteReport(dir, DemoData.Create(1), out var text);

        Assert.Contains("## Сводка", text);
        Assert.Contains("| Всего задач | 9 |", text);
        Assert.Contains("| Просрочено | 1 |", text);
        Assert.Contains("## Команда", text);
        Assert.Contains("## Все задачи", text);
        Assert.Contains("## ! Просроченные задачи", text);
        Assert.Contains("Настроить CI", text);
    }

    [Fact]
    public void Write_EmptyProject_StillProducesValidReport_WithoutOptionalSections()
    {
        using var dir = new TempDir();

        WriteReport(dir, new Project { Name = "Пустой" }, out var text);

        Assert.Contains("| Всего задач | 0 |", text);
        Assert.DoesNotContain("## Команда", text);
        Assert.DoesNotContain("Просроченные задачи", text);
    }

    [Fact]
    public void Write_EscapesPipeCharacters_SoTablesDoNotBreak()
    {
        using var dir = new TempDir();
        var project = Make.NewProject();
        Make.NewTask(project, "A|B");

        WriteReport(dir, project, out var text);

        Assert.Contains("A\\|B", text);
    }

    [Fact]
    public void Write_SameReportTwice_DoesNotOverwrite_AddsNumericSuffix()
    {
        using var dir = new TempDir();
        var writer = new MarkdownReportWriter(dir.File("reports"));
        var report = ReportService.Build(DemoData.Create(1));

        var first = writer.Write(report);
        var second = writer.Write(report);

        Assert.NotEqual(first, second);
        Assert.EndsWith("_1.md", second);
        Assert.True(File.Exists(first));
        Assert.True(File.Exists(second));
    }

    [Theory]
    [InlineData("Проект: <Тест>/1?")]
    [InlineData("CON")]
    [InlineData("***")]
    public void Write_ProjectNameIsMadeSafeForFileSystem(string projectName)
    {
        using var dir = new TempDir();

        var path = WriteReport(dir, new Project { Name = projectName }, out _);

        var name = Path.GetFileName(path);
        Assert.True(name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0, name);
        Assert.True(File.Exists(path));
    }
}
