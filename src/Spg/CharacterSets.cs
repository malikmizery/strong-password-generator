namespace Spg;

public static class CharacterSets
{
    public const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string Lower = "abcdefghijklmnopqrstuvwxyz";
    public const string Digits = "0123456789";

    // The eight symbols that password rules accept almost everywhere; the same set Bitwarden and most
    // password managers generate by default. Fewer rejected sign-ups is worth the ~0.5 bit per character.
    public const string Symbols = "!@#$%^&*";

    // Every other US-keyboard punctuation mark except quotes, backslash and backtick, which break shell quoting.
    public const string AllSymbols = "!@#$%^&*()-_=+[]{}<>?/~;:,.|";

    public const string Ambiguous = "0O1lI|";

    // In a .env file an unquoted "$NAME" or "${NAME}" is expanded as a variable and "#" starts a comment, so a
    // password containing either is silently truncated or rewritten by the loader.
    public const string EnvUnsafe = "$#";
}
