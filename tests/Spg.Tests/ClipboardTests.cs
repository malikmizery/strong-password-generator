namespace Spg.Tests;

public class ClipboardTests
{
    [Fact]
    public void StartInfo_PicksThePlatformTool()
    {
        var info = Clipboard.StartInfo();

        var expected = OperatingSystem.IsWindows() ? "clip" : OperatingSystem.IsMacOS() ? "pbcopy" : null;
        if (expected is not null)
            Assert.Equal(expected, info.FileName);
        else
            Assert.Contains(info.FileName, new[] { "wl-copy", "xclip", "xsel" });
        Assert.True(info.RedirectStandardInput);
        Assert.False(info.UseShellExecute);
    }
}
