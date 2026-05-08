using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Win32;

namespace AudioMirrorSetup;

internal static class Program
{
    private const string AppName = "AudioMirror";
    private const string DisplayVersion = "0.9.1";
    private const string RepositoryUrl = "https://github.com/rausNT/audio-mirror-windows";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var result = MessageBox.Show(
                "Install AudioMirror for the current user?",
                "AudioMirror Setup",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (result != DialogResult.OK)
            {
                return;
            }

            var installDir = GetInstallDir();
            Directory.CreateDirectory(installDir);
            ExtractPayload(installDir);
            WriteUninstaller(installDir);
            CreateStartMenuShortcut(installDir);
            RegisterUninstallEntry(installDir);

            var launch = MessageBox.Show(
                "AudioMirror was installed successfully.\n\nLaunch it now?",
                "AudioMirror Setup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (launch == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo(Path.Combine(installDir, "AudioMirrorApp.exe"))
                {
                    UseShellExecute = true
                });
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

    private static void ExtractPayload(string installDir)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip")
            ?? throw new InvalidOperationException("Installer payload was not found.");
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var path = Path.Combine(installDir, entry.FullName);
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
