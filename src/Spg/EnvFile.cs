using System.Text.RegularExpressions;

namespace Spg;

/// <summary>Adds or replaces one <c>KEY=value</c> line in a dotenv file, leaving everything else byte-for-byte intact.</summary>
public static partial class EnvFile
{
    public enum Result
    {
        Added,
        Replaced,
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    public static partial Regex KeyPattern { get; }

    // Anything outside this set is either reinterpreted by some dotenv loader ($ # ` \ quotes) or trimmed (whitespace).
    [GeneratedRegex(@"^[A-Za-z0-9!@%^&*()\-_=+\[\]{}<>?/~;:,.|]+$")]
    private static partial Regex BareSafe { get; }

    /// <summary>
    /// The value as it must appear after <c>=</c> so every common loader (docker compose, dotenv for Node, Python,
    /// Ruby, Go, …) reads it back exactly: bare when nothing needs escaping, otherwise single-quoted, where they all
    /// agree the content is literal.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The value contains both <c>'</c> and <c>$</c>: it would need double quotes, and inside those the loaders
    /// disagree on how a literal <c>$</c> is written, so no portable spelling exists.
    /// </exception>
    public static string Quote(string value)
    {
        if (value.Length > 0 && BareSafe.IsMatch(value))
            return value;
        if (!value.Contains('\''))
            return $"'{value}'";
        if (value.Contains('$'))
            throw new ArgumentException("This password contains both a single quote and a dollar sign, which no .env file can hold portably.");
        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    public static string Format(string key, string value) => $"{key}={Quote(value)}";

    /// <exception cref="InvalidOperationException">The key is already present and <paramref name="force"/> is false.</exception>
    /// <exception cref="ArgumentException">The key or value cannot be written; see <see cref="Quote"/>.</exception>
    public static Result Set(string path, string key, string value, bool force)
    {
        if (!KeyPattern.IsMatch(key))
            throw new ArgumentException($"'{key}' is not a valid variable name.", nameof(key));
        var line = Format(key, value);

        var newline = "\n";
        var lines = new List<string>();
        if (File.Exists(path))
        {
            var text = File.ReadAllText(path);
            if (text.Contains("\r\n"))
                newline = "\r\n";
            lines.AddRange(text.Split(newline));
            if (lines.Count > 0 && lines[^1].Length == 0)
                lines.RemoveAt(lines.Count - 1);  // the trailing newline; re-added on write
        }

        // Accept the spellings dotenv loaders accept: optional indentation and "export", spaces around "=".
        var existing = new Regex($@"^\s*(export\s+)?{Regex.Escape(key)}\s*=");
        var index = lines.FindIndex(existing.IsMatch);
        var result = Result.Added;
        if (index >= 0)
        {
            if (!force)
                throw new InvalidOperationException($"{key} is already set in {path}; use --force to replace it.");
            lines[index] = line;
            result = Result.Replaced;
        }
        else
        {
            lines.Add(line);
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        var isNew = !File.Exists(path);
        File.WriteAllText(path, string.Join(newline, lines) + newline);

        // A freshly created secrets file should not be world-readable.
        if (isNew && !OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        return result;
    }
}
