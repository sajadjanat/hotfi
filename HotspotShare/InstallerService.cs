using System;
using System.Diagnostics;
using System.IO;

namespace HotspotShare;

public static class InstallerService
{
    private const string AppFolderName = "HOTFI";
    private const string ExeName = "HOTFI.exe";
    private const string ShortcutName = "HOTFI.lnk";

    public static string TargetExePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", AppFolderName, ExeName);

    /// <summary>
    /// On first run (when launched from anywhere other than the installed location),
    /// copies the running exe to a stable per-user location, creates Desktop and Start Menu
    /// shortcuts, then relaunches from there. Returns true if a relaunch was started
    /// (caller should shut down immediately).
    /// </summary>
    public static bool EnsureInstalledAndRelaunchIfNeeded(string[] args)
    {
        string? currentExe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentExe)) return false;

        string targetExe = TargetExePath;
        bool alreadyInstalled = string.Equals(
            Path.GetFullPath(currentExe),
            Path.GetFullPath(targetExe),
            StringComparison.OrdinalIgnoreCase);

        if (alreadyInstalled)
        {
            EnsureShortcuts(targetExe);
            return false;
        }

        try
        {
            string targetDir = Path.GetDirectoryName(targetExe)!;
            Directory.CreateDirectory(targetDir);
            File.Copy(currentExe, targetExe, overwrite: true);

            EnsureShortcuts(targetExe);

            Process.Start(new ProcessStartInfo(targetExe)
            {
                UseShellExecute = true,
                WorkingDirectory = targetDir
            });
            return true;
        }
        catch
        {
            // If install fails (e.g. running from a locked/read-only path), just continue
            // running from the current location instead of blocking the user.
            return false;
        }
    }

    private static void EnsureShortcuts(string targetExe)
    {
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), ShortcutName), targetExe);
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), ShortcutName), targetExe);
    }

    private static void CreateShortcut(string shortcutPath, string targetExe)
    {
        try
        {
            string workingDir = Path.GetDirectoryName(targetExe)!;
            string script =
                $"$ws = New-Object -ComObject WScript.Shell; " +
                $"$s = $ws.CreateShortcut('{shortcutPath}'); " +
                $"$s.TargetPath = '{targetExe}'; " +
                $"$s.WorkingDirectory = '{workingDir}'; " +
                $"$s.IconLocation = '{targetExe},0'; " +
                $"$s.Save()";

            var psi = new ProcessStartInfo("powershell.exe")
            {
                Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{script.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            if (process is null) return;
            process.StandardInput.Close();
            process.WaitForExit(8000);
            if (!process.HasExited) process.Kill();
        }
        catch
        {
            // Non-fatal: app still works without a shortcut.
        }
    }
}
