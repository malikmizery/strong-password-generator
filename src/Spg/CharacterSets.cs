namespace Spg;

public static class CharacterSets
{
    public const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string Lower = "abcdefghijklmnopqrstuvwxyz";
    public const string Digits = "0123456789";

    // Quotes, backslash and backtick are left out: they break shell quoting and are often rejected by sites.
    public const string Symbols = "!@#$%^&*()-_=+[]{}<>?/~;:,.|";

    public const string Ambiguous = "0O1lI|";
}
