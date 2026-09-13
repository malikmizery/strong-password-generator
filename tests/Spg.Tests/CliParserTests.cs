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
    public void BadArguments_ReportTheOffendingToken(string args, string token) =>
        Assert.Contains(token, ParseError(args.Split(' ')));

    [Theory]
    [InlineData("--no-upper --no-lower --no-digits --no-symbols")]
    [InlineData("-l 3")]
    [InlineData("-l 1025")]
    [InlineData("-n 0")]
    [InlineData("-n 1001")]
    [InlineData("-p -w 0")]
    [InlineData("-p -w 65")]
    public void OutOfRangeOptions_AreRejected(string args) =>
        ParseError(args.Split(' '));
}
