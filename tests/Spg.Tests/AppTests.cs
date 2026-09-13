namespace Spg.Tests;

public class AppTests
{
    private static (int Exit, string Out, string Err) Run(string args, string input = "")
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var argv = args.Length == 0 ? [] : args.Split(' ');
        var exit = App.Run(argv, new StringReader(input), output, error);
        return (exit, output.ToString(), error.ToString());
    }

    private static string[] Lines(string text) => text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    public void Help_PrintsUsageAndSucceeds()
    {
        var (exit, stdout, _) = Run("--help");

        Assert.Equal(0, exit);
        Assert.Contains("--passphrase", stdout);
        Assert.Contains("--length", stdout);
        Assert.Contains("--env-safe", stdout);
        Assert.Contains("--exclude", stdout);
    }

    [Fact]
    public void BadArgument_FailsWithUsageErrorOnStderr()
    {
        var (exit, stdout, stderr) = Run("--bogus");

        Assert.Equal(2, exit);
        Assert.Empty(stdout);
        Assert.Contains("--bogus", stderr);
    }

    [Fact]
    public void Passwords_GoToStdoutOnePerLine()
    {
        var (exit, stdout, stderr) = Run("-l 16 -n 3 -q");

        Assert.Equal(0, exit);
        var lines = Lines(stdout);
        Assert.Equal(3, lines.Length);
        Assert.All(lines, line => Assert.Equal(16, line.Length));
        Assert.Empty(stderr);
    }

    [Fact]
    public void Passphrases_UseTheEffList()
    {
        var (exit, stdout, _) = Run("-p -w 4 -s _ --no-capitalize --no-digit -n 2 -q");
        var words = Wordlist.LoadEff();

        Assert.Equal(0, exit);
        var lines = Lines(stdout);
        Assert.Equal(2, lines.Length);
        Assert.All(lines, line =>
        {
            var parts = line.Split('_');
            Assert.Equal(4, parts.Length);
            Assert.All(parts, part => Assert.Contains(part, words));
        });
    }

    [Fact]
    public void Entropy_IsReportedOnStderrOnly()
    {
        var (_, stdout, stderr) = Run("-l 16");

        Assert.Equal(16, Assert.Single(Lines(stdout)).Length);
        Assert.Contains("bits", stderr);
        Assert.DoesNotContain("weak", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LowEntropy_IsFlaggedAsWeak()
    {
        // 26^8 ≈ 37.6 bits
        var (_, _, stderr) = Run("-l 8 --no-upper --no-digits --no-symbols");

        Assert.Contains("weak", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoArguments_PromptsOnStderrAndPrintsOnlyThePasswordOnStdout()
    {
        var (exit, stdout, stderr) = Run("", input: "");

        Assert.Equal(0, exit);
        Assert.Equal(20, Assert.Single(Lines(stdout)).Length);
        Assert.Contains("Length", stderr);
    }
}
