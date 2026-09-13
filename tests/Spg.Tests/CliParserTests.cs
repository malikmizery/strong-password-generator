namespace Spg.Tests;

public class CliParserTests
{
    private static CliOptions ParseOk(params string[] args) =>
        Assert.IsType<ParseResult.Run>(CliParser.Parse(args)).Options;

    private static string ParseError(params string[] args) =>
        Assert.IsType<ParseResult.Error>(CliParser.Parse(args)).Message;

    [Fact]
    public void NoArguments_StartsInteractiveMode() =>
        Assert.IsType<ParseResult.Interactive>(CliParser.Parse([]));

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    [InlineData("-l", "32", "--help")]
    public void HelpFlag_WinsAnywhere(params string[] args) =>
        Assert.IsType<ParseResult.Help>(CliParser.Parse(args));

    [Fact]
    public void PasswordFlags_AreApplied()
    {
        var options = ParseOk("--length", "32", "--no-upper", "--no-lower", "--no-ambiguous", "-n", "5", "-q");

        Assert.Equal(GenerationMode.Password, options.Mode);
        Assert.Equal(32, options.Password.Length);
        Assert.False(options.Password.Upper);
        Assert.False(options.Password.Lower);
        Assert.True(options.Password.Digits);
        Assert.True(options.Password.Symbols);
        Assert.True(options.Password.ExcludeAmbiguous);
        Assert.Equal(5, options.Count);
        Assert.True(options.Quiet);
    }

    [Fact]
    public void ShortLengthAndSetFlags_AreApplied()
    {
        var options = ParseOk("-l", "12", "--no-digits", "--no-symbols");

        Assert.Equal(12, options.Password.Length);
        Assert.False(options.Password.Digits);
        Assert.False(options.Password.Symbols);
        Assert.False(options.Quiet);
        Assert.Equal(1, options.Count);
    }

    [Fact]
    public void ShortExclusionFlags_AreApplied()
    {
        var options = ParseOk("-e", "-a", "-x", "&*", "-A", "-S");

        Assert.True(options.Password.EnvSafe);
        Assert.True(options.Password.ExcludeAmbiguous);
        Assert.Equal("&*", options.Password.Exclude);
        Assert.True(options.Password.AllSymbols);
        Assert.False(options.Password.Symbols);
    }

    [Fact]
    public void LongExclusionFlags_AreApplied()
    {
        var options = ParseOk("--env-safe", "--exclude", "xyz", "--all-symbols");

        Assert.True(options.Password.EnvSafe);
        Assert.Equal("xyz", options.Password.Exclude);
        Assert.True(options.Password.AllSymbols);
    }

    [Fact]
    public void KeyFlag_WritesToTheDefaultEnvFile()
    {
        var options = ParseOk("-k", "DB_PASSWORD");

        Assert.Equal("DB_PASSWORD", options.EnvKey);
        Assert.Equal(".env", options.EnvFile);
        Assert.False(options.Force);
        Assert.True(options.Password.EnvSafe);
    }

    [Fact]
    public void KeyFlag_AcceptsFileAndForce()
    {
        var options = ParseOk("--key", "API_TOKEN", "--file", "deploy/.env.prod", "--force");

        Assert.Equal("API_TOKEN", options.EnvKey);
        Assert.Equal("deploy/.env.prod", options.EnvFile);
        Assert.True(options.Force);
    }

    [Fact]
    public void KeyFlag_WithUserSecrets_TakesProjectAndId()
    {
        var options = ParseOk("-k", "Db:Password", "-u", "--project", "src/Web", "--id", "abc");

        Assert.Equal("Db:Password", options.EnvKey);
        Assert.True(options.UserSecrets);
        Assert.Equal("src/Web", options.Project);
        Assert.Equal("abc", options.SecretsId);
        Assert.True(options.Password.EnvSafe);
    }

    [Fact]
    public void KeyFlag_WithDashFile_MeansStdout() =>
        Assert.Equal("-", ParseOk("-k", "KEY", "-f", "-").EnvFile);

    [Fact]
    public void StdinFlag_ReadsTheSecretInsteadOfGenerating()
    {
        var options = ParseOk("--stdin", "-k", "KEY");

        Assert.True(options.FromStdin);
        Assert.Equal("KEY", options.EnvKey);
    }

    [Fact]
    public void CopyFlag_IsApplied()
    {
        var options = ParseOk("-c", "-l", "32");

        Assert.True(options.Copy);
        Assert.Equal(32, options.Password.Length);
    }

    [Fact]
    public void KeyFlag_WorksWithPassphrases() =>
        Assert.Equal(GenerationMode.Passphrase, ParseOk("-p", "-k", "SECRET").Mode);

    [Fact]
    public void Skill_PrintsTheSkill() =>
        Assert.IsType<ParseResult.Skill>(CliParser.Parse(["--skill"]));

    [Fact]
    public void InstallSkill_TakesATargetAndDefaultsToProjectScope()
    {
        var install = Assert.IsType<ParseResult.InstallSkill>(CliParser.Parse(["--install-skill", "some/dir"]));

        Assert.Equal("some/dir", install.Target);
        Assert.False(install.Global);
    }

    [Theory]
    [InlineData("--install-skill", "claude", "-g")]
    [InlineData("--global", "--install-skill", "claude")]
    public void InstallSkill_AcceptsGlobal(params string[] args)
    {
        var install = Assert.IsType<ParseResult.InstallSkill>(CliParser.Parse(args));

        Assert.Equal("claude", install.Target);
        Assert.True(install.Global);
    }

    [Theory]
    [InlineData("--skill -l 20", "--skill")]
    [InlineData("--skill -g", "-g")]
    [InlineData("--install-skill", "--install-skill")]
    [InlineData("--install-skill a b", "b")]
    [InlineData("--install-skill claude -l 20", "-l")]
    [InlineData("-g", "-g")]
    [InlineData("-g -l 20", "-g")]
    public void SkillFlags_StandAlone(string args, string token) =>
        Assert.Contains(token, ParseError(args.Split(' ')));

    [Fact]
    public void PassphraseFlags_AreApplied()
    {
        var options = ParseOk("-p", "-w", "8", "-s", ".", "--no-capitalize", "--no-digit");

        Assert.Equal(GenerationMode.Passphrase, options.Mode);
        Assert.Equal(8, options.Passphrase.Words);
        Assert.Equal(".", options.Passphrase.Separator);
        Assert.False(options.Passphrase.Capitalize);
        Assert.False(options.Passphrase.AddDigit);
    }

    [Fact]
    public void LongPassphraseFlags_AndEmptySeparator_AreAccepted()
    {
        var options = ParseOk("--passphrase", "--words", "7", "--separator", "");

        Assert.Equal(7, options.Passphrase.Words);
        Assert.Equal("", options.Passphrase.Separator);
        Assert.True(options.Passphrase.Capitalize);
        Assert.True(options.Passphrase.AddDigit);
    }

    [Theory]
    [InlineData("--bogus", "--bogus")]
    [InlineData("-l", "-l")]
    [InlineData("-l abc", "abc")]
    [InlineData("-l -5", "-5")]
    [InlineData("-n 1.5", "1.5")]
    [InlineData("-p -s", "-s")]
    [InlineData("-p -l 32", "-l")]
    [InlineData("-p --no-symbols", "--no-symbols")]
    [InlineData("-w 8", "-w")]
    [InlineData("--no-digit", "--no-digit")]
    [InlineData("-p -e", "-e")]
    [InlineData("-p -x abc", "-x")]
    [InlineData("-x", "-x")]
    [InlineData("-k", "-k")]
    [InlineData("-k 1BAD", "1BAD")]
    [InlineData("-k DB-PASS", "DB-PASS")]
    [InlineData("-k KEY -n 2", "-n")]
    [InlineData("-f .env", "-f")]
    [InlineData("--force", "--force")]
    [InlineData("-p -k KEY -s $", "-s")]
    [InlineData("-p -k KEY -s #", "-s")]
    [InlineData("-k Db:Password", "Db:Password")]
    [InlineData("-k KEY -u -f .env", "-f")]
    [InlineData("-k KEY -u --force", "--force")]
    [InlineData("-u", "-u")]
    [InlineData("--project x", "--project")]
    [InlineData("--id x", "--id")]
    [InlineData("-k KEY --project x", "--project")]
    [InlineData("--stdin", "--stdin")]
    [InlineData("--stdin -c", "--stdin")]
    [InlineData("--stdin -k KEY -l 20", "-l")]
    [InlineData("--stdin -k KEY -p", "-p")]
    [InlineData("--stdin -k KEY -q", "-q")]
    [InlineData("-c -n 2", "-n")]
    [InlineData("-c -k KEY", "-c")]
    public void BadArguments_ReportTheOffendingToken(string args, string token) =>
        Assert.Contains(token, ParseError(args.Split(' ')));

    [Theory]
    [InlineData("--no-upper --no-lower --no-digits --no-symbols")]
    [InlineData("-x 0123456789 --no-upper --no-lower --no-symbols")]
    [InlineData("-l 3")]
    [InlineData("-l 1025")]
    [InlineData("-n 0")]
    [InlineData("-n 1001")]
    [InlineData("-p -w 0")]
    [InlineData("-p -w 65")]
    public void OutOfRangeOptions_AreRejected(string args) =>
        ParseError(args.Split(' '));
}
