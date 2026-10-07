using LogDeck.Core.Modules;

namespace LogDeck.Tests;

public class ToolCatalogTests
{
    [Fact]
    public void IdsAreUnique() =>
        Assert.Equal(ToolCatalog.All.Count, ToolCatalog.All.Select(m => m.Id).Distinct().Count());

    [Fact]
    public void SourceIdsAreUniqueWithinATool() =>
        Assert.All(ToolCatalog.All, m => Assert.Equal(m.Sources.Count, m.Sources.Select(s => s.Id).Distinct().Count()));

    [Fact]
    public void EveryToolHasDetectionAndSources() =>
        Assert.All(ToolCatalog.All, m =>
        {
            Assert.NotEmpty(m.DetectionPaths);
            Assert.NotEmpty(m.Sources);
            Assert.All(m.Sources, s => Assert.NotEmpty(s.Patterns));
        });

    [Theory]
    [InlineData("bootstrapmate", @"%ProgramData%\ManagedBootstrap\logs")]
    [InlineData("reportmate", @"%ProgramData%\ManagedReports\logs")]
    [InlineData("cimian", @"%ProgramData%\ManagedInstalls\logs")]
    [InlineData("startset", @"%ProgramData%\ManagedState\logs")]
    [InlineData("crypt", @"%ProgramData%\ManagedEncryption\logs")]
    [InlineData("csharpdialog", @"%ProgramData%\ManagedNotifications\logs")]
    [InlineData("manageusers", @"%ProgramData%\ManagedUsers\logs")]
    [InlineData("intune", @"%ProgramData%\Microsoft\IntuneManagementExtension\Logs")]
    public void FirstSourceIsTheToolsLogFolder(string id, string root) =>
        Assert.Equal(root, ToolCatalog.Find(id)!.Sources[0].Root);

    [Fact]
    public void FindIgnoresCase() => Assert.Equal("cimian", ToolCatalog.Find("Cimian")?.Id);
}
