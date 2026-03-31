using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace SpeedoMeter.Services;

public sealed class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string SettingsKey = @"Software\NetPulse";
    private const string StartupInitializedValueName = "StartupInitialized";
    private const string AppName = "NetPulse";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            return key?.GetValue(AppName) != null;
        }
    }

    public void EnsureDefaultEnabled()
    {
        using var settingsKey = Registry.CurrentUser.CreateSubKey(SettingsKey);
        var initialized = settingsKey?.GetValue(StartupInitializedValueName, 0);

        if (initialized is int initializedFlag && initializedFlag == 1)
        {
            return;
        }

        if (Enable())
        {
            settingsKey?.SetValue(StartupInitializedValueName, 1, RegistryValueKind.DWord);
        }
    }

    public bool Enable()
    {
        string? exePath = ResolveExecutablePath();
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return false;
        }

        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        key?.SetValue(AppName, $"\"{exePath}\"", RegistryValueKind.String);
        return true;
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        key?.DeleteValue(AppName, false);
    }

    private static string? ResolveExecutablePath()
    {
        string? processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            string fileName = Path.GetFileName(processPath);
            if (!string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fileName, "dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                return processPath;
            }
        }

        string? entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
        if (string.IsNullOrWhiteSpace(entryAssemblyName))
        {
            return null;
        }

        string candidatePath = Path.Combine(AppContext.BaseDirectory, $"{entryAssemblyName}.exe");
        return File.Exists(candidatePath) ? candidatePath : null;
    }
}
