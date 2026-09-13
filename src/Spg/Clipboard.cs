using System.Diagnostics;

namespace Spg;

/// <summary>Copies text with the platform's clipboard tool, piped through stdin.</summary>
public static class Clipboard
{
    public static ProcessStartInfo StartInfo()
    {
        var (file, args) = OperatingSystem.IsWindows() ? ("clip", Array.Empty<string>())
            : OperatingSystem.IsMacOS() ? ("pbcopy", Array.Empty<string>())
            : Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") is { Length: > 0 } ? ("wl-copy", Array.Empty<string>())
            : ("xclip", ["-selection", "clipboard"]);

        var info = new ProcessStartInfo(file) { RedirectStandardInput = true, UseShellExecute = false };
        foreach (var arg in args)
            info.ArgumentList.Add(arg);
        return info;
    }

    /// <exception cref="InvalidOperationException">No clipboard tool could be run.</exception>
    public static void Copy(string text)
    {
        var info = StartInfo();
        Process process;
        try
        {
            process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start '{info.FileName}'.");
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            throw new InvalidOperationException($"No clipboard tool: could not start '{info.FileName}' ({e.Message}).", e);
        }

        using (process)
        {
            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"'{info.FileName}' exited with code {process.ExitCode}.");
        }
    }
}
