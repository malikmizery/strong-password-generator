namespace Spg.Tests;

public class SkillInstallerTests
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [Theory]
    [InlineData("claude", ".claude")]
    [InlineData("codex", ".codex")]
    [InlineData("gemini", ".gemini")]
    [InlineData("cursor", ".cursor")]
    [InlineData("copilot", ".github")]
    public void HarnessNames_ResolveToTheProjectSkillsFolderByDefault(string target, string dotDir) =>
        Assert.Equal(Path.GetFullPath(Path.Combine(dotDir, "skills", "spg", "SKILL.md")), SkillInstaller.ResolvePath(target, global: false));

    [Theory]
    [InlineData("claude", ".claude")]
    [InlineData("Codex", ".codex")]
    [InlineData("gemini", ".gemini")]
    [InlineData("cursor", ".cursor")]
    [InlineData("copilot", ".copilot")]
    public void HarnessNames_WithGlobal_ResolveToTheHomeSkillsFolder(string target, string dotDir) =>
        Assert.Equal(Path.Combine(Home, dotDir, "skills", "spg", "SKILL.md"), SkillInstaller.ResolvePath(target, global: true));

    [Fact]
    public void AnythingElse_IsTreatedAsADirectory() =>
        Assert.Equal(Path.GetFullPath(Path.Combine("my", "skills", "spg", "SKILL.md")), SkillInstaller.ResolvePath(Path.Combine("my", "skills"), global: false));

    [Fact]
    public void ADirectory_WithGlobal_IsRejected() =>
        Assert.Throws<ArgumentException>(() => SkillInstaller.ResolvePath("my/skills", global: true));

    [Fact]
    public void Text_IsAValidAgentSkill()
    {
        var text = SkillInstaller.Text;

        Assert.StartsWith("---\nname: spg\ndescription: ", text);
        Assert.Contains("\n---\n", text);
        Assert.Contains("spg -k", text);
        Assert.Contains("--force", text);
        Assert.Contains("-u", text);
        Assert.DoesNotContain("spg -q", text);  // agents get the two storing forms only, never stdout
        Assert.DoesNotMatch(@"spg[^\n]* (\||>>?) ", text);  // no `spg ... | tool` or `spg ... > file`
    }
}
