using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Spg;

/// <summary>Stores a secret with <c>dotnet user-secrets</c>, feeding it through stdin so it never appears on a command line.</summary>
public static partial class UserSecrets
{
    // Configuration keys are colon-separated paths; anything printable except whitespace and quotes is fine.
    [GeneratedRegex("^[^\\s\"=]+$")]
    public static partial Regex KeyPattern { get; }

    /// <summary>The <c>{"key":"value"}</c> document <c>dotnet user-secrets set</c> accepts on stdin.</summary>
    public static string Json(string key, string value) => $"{{{JsonString(key)}:{JsonString(value)}}}";

    // Hand-rolled so the trimmed single-file build needs no reflection-based serializer.
    private static string JsonString(string text)
    {
        var builder = new StringBuilder(text.Length + 2).Append('"');
        foreach (var c in text)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case < ' ': builder.Append(CultureInfo.InvariantCulture, $"\\u{(int)c:x4}"); break;
                default: builder.Append(c); break;
            }
        }
        return builder.Append('"').ToString();
    }

    public static ProcessStartInfo StartInfo(string? project, string? id)
    {
        var info = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        info.ArgumentList.Add("user-secrets");
        info.ArgumentList.Add("set");
        if (project is not null)
        {
            info.ArgumentList.Add("--project");
            info.ArgumentList.Add(project);
        }
        if (id is not null)
        {
            info.ArgumentList.Add("--id");
            info.ArgumentList.Add(id);
        }
        return info;
    }

    /// <exception cref="InvalidOperationException">The dotnet CLI is missing or reported an error.</exception>
    public static void Set(string key, string value, string? project, string? id)
    {
        if (!KeyPattern.IsMatch(key))
            throw new ArgumentException($"'{key}' is not a valid configuration key.", nameof(key));

        Process process;
        try
        {
            process = Process.Start(StartInfo(project, id))
                ?? throw new InvalidOperationException("Could not start 'dotnet user-secrets'.");
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            throw new InvalidOperationException($"Could not start 'dotnet user-secrets': {e.Message}", e);
        }

        using (process)
        {
            process.StandardInput.Write(Json(key, value));
            process.StandardInput.Close();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"'dotnet user-secrets set' failed: {(stderr + stdout).Trim()}");
        }
    }
}
