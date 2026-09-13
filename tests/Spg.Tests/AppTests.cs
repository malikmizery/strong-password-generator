namespace Spg.Tests;

public class AppTests
{
    private static (int Exit, string Out, string Err) Run(string args, string input = "") =>
        Run(args.Length == 0 ? [] : args.Split(' '), input);

    private static (int Exit, string Out, string Err) Run(string[] argv, string input = "", Action<string>? copy = null)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exit = App.Run(argv, new StringReader(input), output, error, copy ?? (_ => throw new InvalidOperationException("clipboard not expected")));
        return (exit, output.ToString(), error.ToString());
    }

    [Fact]
    public void Copy_SendsThePasswordToTheClipboardAndKeepsItOffStdout()
    {
        string? copied = null;

        var (exit, stdout, stderr) = Run(["-c", "-l", "24"], copy: text => copied = text);

        Assert.Equal(0, exit);
        Assert.Empty(stdout);
        Assert.Equal(24, copied!.Length);
        Assert.Contains("Copied", stderr);
        Assert.Contains("bits", stderr);
        Assert.DoesNotContain(copied, stderr);
    }

    [Fact]
    public void Copy_WhenTheClipboardToolFails_ReportsItWithoutPrintingTheSecret()
    {
        var (exit, stdout, stderr) = Run(["-c"], copy: _ => throw new InvalidOperationException("no clipboard tool"));

        Assert.Equal(1, exit);
        Assert.Empty(stdout);
        Assert.Contains("no clipboard tool", stderr);
    }

    [Fact]
    public void Key_WithDashFile_PrintsTheEnvLineToStdout()
    {
        var (exit, stdout, stderr) = Run("-k DB_PASSWORD -f - -l 24");

        Assert.Equal(0, exit);
        var line = Assert.Single(Lines(stdout));
        Assert.StartsWith("DB_PASSWORD=", line);
        Assert.Equal(24, line["DB_PASSWORD=".Length..].Length);
        Assert.DoesNotContain("Added", stderr);
        Assert.Contains("bits", stderr);
    }

    [Fact]
    public void Stdin_WritesAPastedPasswordQuotedForDotenv()
    {
        var path = TempPath(".env");
        try
        {
            var (exit, stdout, stderr) = Run(["--stdin", "-k", "DB_PASSWORD", "-f", path], input: "pa$$w#rd \"ok\"\r\n");

            Assert.Equal(0, exit);
            Assert.Empty(stdout);
            Assert.Contains("Added DB_PASSWORD", stderr);
            Assert.DoesNotContain("bits", stderr);  // unknown entropy for a password we didn't generate
            Assert.DoesNotContain("pa$$", stderr);
            Assert.Equal("DB_PASSWORD='pa$$w#rd \"ok\"'\n", File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Theory]
    [InlineData("pa$$w#rd\n", "DB_PASSWORD='pa$$w#rd'")]
    [InlineData("plain123\r\n", "DB_PASSWORD=plain123")]
    [InlineData("it's\n", "DB_PASSWORD=\"it's\"")]
    [InlineData("  spaced  \n", "DB_PASSWORD='  spaced  '")]
    public void Stdin_WithDashFile_PrintsTheQuotedLine(string input, string expected)
    {
        var (exit, stdout, _) = Run(["--stdin", "-k", "DB_PASSWORD", "-f", "-"], input: input);

        Assert.Equal(0, exit);
        Assert.Equal(expected, Assert.Single(Lines(stdout)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    public void Stdin_WithNothingToRead_Fails(string input)
    {
        var (exit, stdout, stderr) = Run(["--stdin", "-k", "KEY", "-f", "-"], input: input);

        Assert.Equal(1, exit);
        Assert.Empty(stdout);
        Assert.Contains("empty", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Stdin_WithAnUnportablePassword_ExplainsAndSuggestsRotating()
    {
        var (exit, stdout, stderr) = Run(["--stdin", "-k", "KEY", "-f", "-"], input: "it's $5\n");

        Assert.Equal(1, exit);
        Assert.Empty(stdout);
        Assert.Contains("--force", stderr);
        Assert.DoesNotContain("it's", stderr);
    }

    private static string[] Lines(string text) => text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    private static string TempPath(string name) => Path.Combine(Path.GetTempPath(), $"spg-test-{Guid.NewGuid():N}", name);

    [Fact]
    public void Help_PrintsUsageAndSucceeds()
    {
        var (exit, stdout, _) = Run("--help");

        Assert.Equal(0, exit);
        Assert.Contains("--passphrase", stdout);
        Assert.Contains("--length", stdout);
        Assert.Contains("--env-safe", stdout);
        Assert.Contains("--exclude", stdout);
        Assert.Contains("--key", stdout);
    }

    [Fact]
    public void Key_WritesThePasswordToTheEnvFileAndKeepsItOffStdout()
    {
        var path = TempPath(".env");
        try
        {
            var (exit, stdout, stderr) = Run(["-k", "DB_PASSWORD", "-f", path, "-l", "24"]);

            Assert.Equal(0, exit);
            Assert.Empty(stdout);
            Assert.Contains("Added DB_PASSWORD", stderr);
            Assert.Contains("bits", stderr);

            var line = Assert.Single(File.ReadAllText(path).Split('\n', StringSplitOptions.RemoveEmptyEntries));
            Assert.StartsWith("DB_PASSWORD=", line);
            var password = line["DB_PASSWORD=".Length..];
            Assert.Equal(24, password.Length);
            Assert.DoesNotContain(password, c => c is '$' or '#');
            Assert.DoesNotContain(password, stderr);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void Key_RefusesToOverwriteWithoutForce()
    {
        var path = TempPath(".env");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "DB_PASSWORD=old\n");

            var (exit, stdout, stderr) = Run(["-k", "DB_PASSWORD", "-f", path]);

            Assert.Equal(1, exit);
            Assert.Empty(stdout);
            Assert.Contains("--force", stderr);
            Assert.Equal("DB_PASSWORD=old\n", File.ReadAllText(path));

            var (forcedExit, _, forcedErr) = Run(["-k", "DB_PASSWORD", "-f", path, "--force"]);

            Assert.Equal(0, forcedExit);
            Assert.Contains("Replaced DB_PASSWORD", forcedErr);
            Assert.DoesNotContain("old", File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
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
