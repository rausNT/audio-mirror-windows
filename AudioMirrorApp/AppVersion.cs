namespace AudioMirrorApp;

using System.Reflection;

internal static class AppVersion
{
    public static string Display { get; } = "v" + (
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(AppVersion).Assembly.GetName().Version?.ToString(3)
        ?? "0.0.0");
}
