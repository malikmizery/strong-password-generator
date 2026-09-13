namespace Spg.Tests;

public class PasswordOptionsTests
{
    [Fact]
    public void Defaults_AreValid() => Assert.Null(new PasswordOptions().Validate());

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
