namespace Spg;

/// <summary>
/// The Agent Skill (a <c>SKILL.md</c> with YAML front matter) that teaches an AI coding agent to generate secrets
/// with spg without the secret ever entering its transcript.
/// </summary>
public static class SkillInstaller
{
    private const string Resource = "Spg.SKILL.md";
    private const string FileName = "SKILL.md";

    private static readonly Lazy<string> TextValue = new(Load);

    /// <summary>Harness name → (folder under the project root, folder under the home directory).</summary>
    private static readonly Dictionary<string, (string Project, string Global)> Harnesses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["claude"] = (".claude", ".claude"),
        ["codex"] = (".codex", ".codex"),
        ["gemini"] = (".gemini", ".gemini"),
        ["cursor"] = (".cursor", ".cursor"),
        ["copilot"] = (".github", ".copilot"),
    };

    public static string Text => TextValue.Value;

    public static string HarnessNames => string.Join(", ", Harnesses.Keys);

    /// <summary>
    /// Where <c>SKILL.md</c> ends up for <paramref name="target"/>: a harness name (under the current folder, or
    /// the home folder when <paramref name="global"/>), or any skills directory.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="global"/> was combined with a directory.</exception>
    public static string ResolvePath(string target, bool global)
    {
        string directory;
        if (Harnesses.TryGetValue(target, out var harness))
        {
            directory = global
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), harness.Global, "skills")
                : Path.Combine(harness.Project, "skills");
        }
        else if (global)
        {
            throw new ArgumentException($"--global only applies to a harness name ({HarnessNames}), not a directory.", nameof(target));
        }
        else
        {
            directory = target;
        }

        return Path.GetFullPath(Path.Combine(directory, "spg", FileName));
    }

    /// <returns>The path that was written.</returns>
    public static string Install(string target, bool global)
    {
        var path = ResolvePath(target, global);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Text);
        return path;
    }

    private static string Load()
    {
        using var stream = typeof(SkillInstaller).Assembly.GetManifestResourceStream(Resource)
            ?? throw new InvalidOperationException($"Embedded resource '{Resource}' not found.");
        using var reader = new StreamReader(stream);
        // Normalise so the front matter parses the same whichever OS the repo was checked out on.
        return reader.ReadToEnd().ReplaceLineEndings("\n");
    }
}
