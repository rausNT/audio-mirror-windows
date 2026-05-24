using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Win32;

namespace AudioMirrorSetup;

internal static class Program
{
    private const string AppName = "AudioMirror";
    private const string DisplayVersion = "0.9.14";
    private const string RepositoryUrl = "https://github.com/rausNT/audio-mirror-windows";
    private const string ReleaseSummary =
        "Changes in this update:\n" +
        "- The setup package and installed app are now self-contained and do not require a separate .NET install.\n" +
        "- Settings are stored in the user profile instead of beside the app binaries.\n" +
        "- AudioMirror keeps retrying after display sleep until the selected audio devices wake up.\n" +
        "- If AudioMirror is running, audio from AudioMirror will briefly stop during the update.\n" +
        "- After the update, AudioMirror will be started again with mirroring enabled so sound can continue.";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var installDir = GetInstallDir();
            var installedVersion = GetInstalledVersion(installDir);
            var runningBeforeInstall = IsAudioMirrorRunning(installDir);

            var result = MessageBox.Show(
                BuildInstallPrompt(installedVersion, runningBeforeInstall),
                "AudioMirror Setup",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (result != DialogResult.OK)
            {
                return;
            }

            var stoppedRunningApp = StopRunningAudioMirror(installDir);
            Directory.CreateDirectory(installDir);
            ExtractPayload(installDir);
            WriteUninstaller(installDir);
            CreateStartMenuShortcut(installDir);
            RegisterUninstallEntry(installDir);

            if (runningBeforeInstall || stoppedRunningApp)
            {
                LaunchAudioMirror(installDir, "--start");
                MessageBox.Show(
                    $"AudioMirror {DisplayVersion} was installed successfully.\n\nAudioMirror was restarted with mirroring enabled.",
                    "AudioMirror Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var launch = MessageBox.Show(
                $"AudioMirror {DisplayVersion} was installed successfully.\n\nLaunch it now?",
                "AudioMirror Setup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (launch == DialogResult.Yes)
            {
                LaunchAudioMirror(installDir, "");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "AudioMirror Setup failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static string GetInstallDir()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Programs", "AudioMirror");
    }

    private static string BuildInstallPrompt(string? installedVersion, bool runningBeforeInstall)
    {
        var versionLine = string.IsNullOrWhiteSpace(installedVersion)
            ? $"Install AudioMirror {DisplayVersion} for the current user?"
            : $"AudioMirror {DisplayVersion} will replace AudioMirror {installedVersion}.";
        var runningLine = runningBeforeInstall
            ? "\n\nAudioMirror is currently running. Sound produced through AudioMirror will briefly stop during installation."
            : "";

        return $"{versionLine}\n\n{ReleaseSummary}{runningLine}\n\nContinue?";
    }

    private static string? GetInstalledVersion(string installDir)
    {
        var exePath = Path.Combine(installDir, "AudioMirrorApp.exe");
        if (File.Exists(exePath))
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
            if (!string.IsNullOrWhiteSpace(versionInfo.ProductVersion))
            {
                return versionInfo.ProductVersion;
            }

            if (!string.IsNullOrWhiteSpace(versionInfo.FileVersion))
            {
                return versionInfo.FileVersion;
            }
        }

        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\AudioMirror");
        return key?.GetValue("DisplayVersion") as string;
    }

    private static bool IsAudioMirrorRunning(string installDir)
    {
        return Process.GetProcessesByName("AudioMirrorApp").Any(process =>
        {
            using (process)
            {
                try
                {
                    var processPath = process.MainModule?.FileName ?? string.Empty;
                    return string.IsNullOrEmpty(processPath) ||
                        processPath.StartsWith(installDir, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return true;
                }
            }
        });
    }

    private static bool StopRunningAudioMirror(string installDir)
    {
        var stoppedAny = false;
        var processes = Process.GetProcessesByName("AudioMirrorApp");
        foreach (var process in processes)
        {
            using (process)
            {
                try
                {
                    var processPath = process.MainModule?.FileName ?? string.Empty;
                    var sameInstall = processPath.StartsWith(installDir, StringComparison.OrdinalIgnoreCase);
                    if (!sameInstall && !string.IsNullOrEmpty(processPath))
                    {
                        continue;
                    }

                    stoppedAny = true;
                    process.CloseMainWindow();
                    if (!process.WaitForExit(2500))
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(5000);
                    }
                }
                catch
                {
                    try
                    {
                        stoppedAny = true;
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(5000);
                    }
                    catch
                    {
                    }
                }
            }
        }

        WaitForFileRelease(Path.Combine(installDir, "AudioMirrorApp.dll"));
        WaitForFileRelease(Path.Combine(installDir, "AudioMirrorApp.exe"));
        return stoppedAny;
    }

    private static void LaunchAudioMirror(string installDir, string arguments)
    {
        Process.Start(new ProcessStartInfo(Path.Combine(installDir, "AudioMirrorApp.exe"))
        {
            Arguments = arguments,
            UseShellExecute = true
        });
    }

    private static void WaitForFileRelease(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        for (var i = 0; i < 20; i++)
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(250);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(250);
            }
        }
    }

    private static void ExtractPayload(string installDir)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip")
            ?? throw new InvalidOperationException("Installer payload was not found.");
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var installRoot = Path.GetFullPath(installDir);
        if (!installRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            installRoot += Path.DirectorySeparatorChar;
        }

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var path = Path.GetFullPath(Path.Combine(installDir, entry.FullName));
            if (!path.StartsWith(installRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unsafe installer payload path: {entry.FullName}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? installDir);
            entry.ExtractToFile(path, overwrite: true);
        }
    }

    private static void WriteUninstaller(string installDir)
    {
        var scriptPath = Path.Combine(installDir, "Uninstall-AudioMirror.ps1");
        var script = $$"""
        $ErrorActionPreference = 'Stop'
        $installDir = '{{installDir.Replace("'", "''")}}'
        $startMenuDir = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\AudioMirror'
        $runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
        $uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AudioMirror'

        Get-Process -Name 'AudioMirrorApp' -ErrorAction SilentlyContinue | Stop-Process -Force
        Remove-ItemProperty -Path $runKey -Name 'AudioMirror' -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $startMenuDir -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $uninstallKey -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $installDir -Recurse -Force -ErrorAction SilentlyContinue
        """;
        File.WriteAllText(scriptPath, script);
    }

    private static void CreateStartMenuShortcut(string installDir)
    {
        var startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            "AudioMirror");
        Directory.CreateDirectory(startMenuDir);

        var exePath = Path.Combine(installDir, "AudioMirrorApp.exe");
        CreateShortcut(
            Path.Combine(startMenuDir, "AudioMirror.lnk"),
            exePath,
            installDir,
            exePath,
            "AudioMirror");

        var uninstallScript = Path.Combine(installDir, "Uninstall-AudioMirror.ps1");
        CreateShortcut(
            Path.Combine(startMenuDir, "Uninstall AudioMirror.lnk"),
            "powershell.exe",
            installDir,
            exePath,
            "Uninstall AudioMirror",
            $"-NoProfile -ExecutionPolicy Bypass -File \"{uninstallScript}\"");
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string iconPath, string description, string arguments = "")
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is not available.");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Could not create WScript.Shell.");
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.Arguments = arguments;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.IconLocation = iconPath;
        shortcut.Description = description;
        shortcut.Save();
    }

    private static void RegisterUninstallEntry(string installDir)
    {
        var exePath = Path.Combine(installDir, "AudioMirrorApp.exe");
        var uninstallScript = Path.Combine(installDir, "Uninstall-AudioMirror.ps1");
        var uninstallCommand = $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{uninstallScript}\"";

        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\AudioMirror", writable: true)
            ?? throw new InvalidOperationException("Could not create uninstall registry key.");
        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", DisplayVersion);
        key.SetValue("Publisher", "AudioMirror contributors");
        key.SetValue("URLInfoAbout", RepositoryUrl);
        key.SetValue("InstallLocation", installDir);
        key.SetValue("DisplayIcon", exePath);
        key.SetValue("UninstallString", uninstallCommand);
        key.SetValue("QuietUninstallString", uninstallCommand);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key.SetValue("EstimatedSize", EstimateInstallSizeKb(installDir), RegistryValueKind.DWord);
    }

    private static int EstimateInstallSizeKb(string installDir)
    {
        return Directory.EnumerateFiles(installDir, "*", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path).Length)
            .Sum(length => (int)Math.Ceiling(length / 1024.0));
    }
}
