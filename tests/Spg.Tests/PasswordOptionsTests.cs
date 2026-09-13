namespace Spg.Tests;

public class PasswordOptionsTests
{
    private const string CommonSymbols = "!@#$%^&*";
    private const string AllSymbols = "!@#$%^&*()-_=+[]{}<>?/~;:,.|";

    private static string OnlySymbols(PasswordOptions options) =>
        Assert.Single((options with { Upper = false, Lower = false, Digits = false }).EnabledSets());

    [Fact]
    public void Defaults_AreValid() => Assert.Null(new PasswordOptions().Validate());

    [Fact]
    public void DefaultSymbols_AreTheEightMostWidelyAccepted() =>
        Assert.Equal(CommonSymbols, OnlySymbols(new PasswordOptions()));

    [Fact]
    public void AllSymbols_UsesTheFullPunctuationSet() =>
        Assert.Equal(AllSymbols, OnlySymbols(new PasswordOptions { AllSymbols = true }));

    [Fact]
    public void EnvSafe_LeavesOutDollarAndHash() =>
        Assert.Equal("!@%^&*", OnlySymbols(new PasswordOptions { EnvSafe = true }));

    [Fact]
    public void EnvSafe_WithAllSymbols_StillLeavesOutDollarAndHash() =>
        Assert.Equal("!@%^&*()-_=+[]{}<>?/~;:,.|", OnlySymbols(new PasswordOptions { AllSymbols = true, EnvSafe = true }));

    [Fact]
    public void Exclude_RemovesTheListedCharactersFromEverySet()
    {
        var sets = new PasswordOptions { Exclude = "aB3!" }.EnabledSets();

        Assert.Equal(4, sets.Count);
        Assert.All(sets, set => Assert.DoesNotContain(set, c => "aB3!".Contains(c)));
        Assert.Equal(25, sets[0].Length);
    }

    [Fact]
    public void Exclude_ThatEmptiesASet_DropsTheSet()
    {
        var options = new PasswordOptions { Exclude = "0123456789", Length = 3 };

        Assert.Equal(3, options.EnabledSets().Count);
        Assert.Null(options.Validate());
    }

    [Fact]
    public void Exclude_ThatEmptiesEverySet_IsInvalid()
    {
        var options = new PasswordOptions { Upper = false, Lower = false, Symbols = false, Exclude = "0123456789" };

        Assert.NotNull(options.Validate());
    }

    [Fact]
    public void AllSetsDisabled_IsInvalid()
    {
        var options = new PasswordOptions { Upper = false, Lower = false, Digits = false, Symbols = false };

        Assert.NotNull(options.Validate());
    }

    [Theory]
    [InlineData(3, true, false)]  // 4 sets need at least 4 characters
    [InlineData(4, true, true)]
    [InlineData(3, false, true)]  // 3 sets fit in 3 characters
    [InlineData(2, false, false)]
    public void Length_MustCoverEveryEnabledSet(int length, bool symbols, bool valid)
    {
        var options = new PasswordOptions { Length = length, Symbols = symbols };

        Assert.Equal(valid, options.Validate() is null);
    }

    [Theory]
    [InlineData(1024, true)]
    [InlineData(1025, false)]
    public void Length_IsCapped(int length, bool valid) =>
        Assert.Equal(valid, new PasswordOptions { Length = length }.Validate() is null);
}
