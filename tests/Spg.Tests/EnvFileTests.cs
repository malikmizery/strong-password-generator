namespace Spg.Tests;

public class EnvFileTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"spg-test-{Guid.NewGuid():N}.env");

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Fact]
    public void Set_CreatesTheFileWhenMissing()
    {
        var result = EnvFile.Set(_path, "KEY", "value", force: false);

        Assert.Equal(EnvFile.Result.Added, result);
        Assert.Equal("KEY=value\n", File.ReadAllText(_path));
    }

    [Fact]
    public void Set_AppendsAfterTheLastLineEvenWithoutATrailingNewline()
    {
        File.WriteAllText(_path, "A=1\nB=2");

        EnvFile.Set(_path, "C", "3", force: false);

        Assert.Equal("A=1\nB=2\nC=3\n", File.ReadAllText(_path));
    }

    [Fact]
    public void Set_KeepsWindowsLineEndings()
    {
        File.WriteAllText(_path, "A=1\r\n");

        EnvFile.Set(_path, "B", "2", force: false);

        Assert.Equal("A=1\r\nB=2\r\n", File.ReadAllText(_path));
    }

    [Theory]
    [InlineData("KEY=old")]
    [InlineData("  KEY = old")]
    [InlineData("export KEY=old")]
    public void Set_RefusesToOverwriteAnExistingKey(string line)
    {
        File.WriteAllText(_path, $"A=1\n{line}\n");

        Assert.Throws<InvalidOperationException>(() => EnvFile.Set(_path, "KEY", "new", force: false));
        Assert.Equal($"A=1\n{line}\n", File.ReadAllText(_path));
    }

    [Fact]
    public void Set_WithForce_ReplacesTheLineInPlace()
    {
        File.WriteAllText(_path, "A=1\nKEY=old # note\nB=2\n");

        var result = EnvFile.Set(_path, "KEY", "new", force: true);

        Assert.Equal(EnvFile.Result.Replaced, result);
        Assert.Equal("A=1\nKEY=new\nB=2\n", File.ReadAllText(_path));
    }

    [Theory]
    [InlineData("k9Qv!zR2@mXe7*Lp_4w%", "k9Qv!zR2@mXe7*Lp_4w%")]     // nothing a loader reinterprets: bare
    [InlineData("pa$$w#rd", "'pa$$w#rd'")]                            // $ and # are literal inside single quotes
    [InlineData("has space", "'has space'")]
    [InlineData("tab\there", "'tab\there'")]
    [InlineData("say \"hi\"", "'say \"hi\"'")]
    [InlineData("back\\slash", "'back\\slash'")]
    [InlineData("tick`", "'tick`'")]
    [InlineData("it's", "\"it's\"")]                                  // a single quote forces double quotes
    [InlineData("it's \"quoted\" \\ #1", "\"it's \\\"quoted\\\" \\\\ #1\"")]
    [InlineData("", "''")]
    public void Quote_MakesTheValueSurviveAnyDotenvLoader(string value, string expected) =>
        Assert.Equal(expected, EnvFile.Quote(value));

    [Fact]
    public void Quote_RefusesAValueThatNeedsBothASingleQuoteAndADollar() =>
        // Inside double quotes loaders disagree on how to escape $ ($$, \$ or not at all), so no portable form exists.
        Assert.Throws<ArgumentException>(() => EnvFile.Quote("it's $5"));

    [Fact]
    public void Format_IsTheLineSetWrites() =>
        Assert.Equal("KEY='pa$$w#rd'", EnvFile.Format("KEY", "pa$$w#rd"));

    [Fact]
    public void Set_QuotesTheValueWhenNeeded()
    {
        EnvFile.Set(_path, "KEY", "pa$$ w#rd", force: false);

        Assert.Equal("KEY='pa$$ w#rd'\n", File.ReadAllText(_path));
    }

    [Fact]
    public void Set_DoesNotMatchKeysThatMerelyStartTheSameWay()
    {
        File.WriteAllText(_path, "KEY_ID=1\n#KEY=2\n");

        Assert.Equal(EnvFile.Result.Added, EnvFile.Set(_path, "KEY", "3", force: false));
        Assert.Equal("KEY_ID=1\n#KEY=2\nKEY=3\n", File.ReadAllText(_path));
    }
}
