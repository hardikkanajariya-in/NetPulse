using System;
using System.Windows;
using Microsoft.Win32;

namespace SpeedoMeter.Services;

public sealed class ThemeManager
{
    public const string ThemeDark = "dark";
    public const string ThemeLight = "light";
    public const string ThemeSystem = "system";

    private const string ThemeSettingKey = "app.theme";
    private readonly DatabaseService _db;
    private string _currentTheme = ThemeDark;
    private ResourceDictionary? _activeThemeDict;

    public ThemeManager(DatabaseService db)
    {
        _db = db;
    }

    public string CurrentTheme => _currentTheme;

    public void Initialize()
    {
        string saved = _db.GetSettingString(ThemeSettingKey, ThemeSystem);
        ApplyTheme(saved, save: false);
    }

    public void SetTheme(string theme)
    {
        ApplyTheme(theme, save: true);
    }

    public event Action? ThemeChanged;

    private void ApplyTheme(string theme, bool save)
    {
        _currentTheme = NormalizeTheme(theme);
        if (save)
            _db.SetSettingString(ThemeSettingKey, _currentTheme);

        string resolved = _currentTheme == ThemeSystem ? DetectSystemTheme() : _currentTheme;
        string uri = resolved == ThemeLight
            ? "Themes/LightTheme.xaml"
            : "Themes/DarkTheme.xaml";

        var dict = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };
        var app = Application.Current;

        if (_activeThemeDict != null)
            app.Resources.MergedDictionaries.Remove(_activeThemeDict);

        // Insert at position 0 so theme brushes are found first
        app.Resources.MergedDictionaries.Insert(0, dict);
        _activeThemeDict = dict;

        ThemeChanged?.Invoke();
    }

    private static string DetectSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            if (val is int i)
                return i == 1 ? ThemeLight : ThemeDark;
        }
        catch { }
        return ThemeDark;
    }

    private static string NormalizeTheme(string? theme)
    {
        return theme switch
        {
            ThemeLight => ThemeLight,
            ThemeDark => ThemeDark,
            ThemeSystem => ThemeSystem,
            _ => ThemeSystem
        };
    }
}
