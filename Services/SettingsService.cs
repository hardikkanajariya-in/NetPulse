namespace SpeedoMeter.Services;

public sealed class SettingsService
{
    public const string WidgetModeTotal = "total";
    public const string WidgetModeTopAdapter = "top-adapter";
    public const string WidgetModeSelectedAdapter = "selected-adapter";

    private const string WidgetModeKey = "widget.mode";
    private const string SelectedAdapterKey = "widget.selectedAdapterId";

    private readonly DatabaseService _databaseService;

    public SettingsService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Reload();
    }

    public string WidgetMode { get; private set; } = WidgetModeTotal;

    public string SelectedAdapterId { get; private set; } = string.Empty;

    public void Reload()
    {
        WidgetMode = NormalizeWidgetMode(_databaseService.GetSettingString(WidgetModeKey, WidgetModeTotal));
        SelectedAdapterId = _databaseService.GetSettingString(SelectedAdapterKey, string.Empty);
    }

    public void SetWidgetMode(string? mode)
    {
        WidgetMode = NormalizeWidgetMode(mode);
        _databaseService.SetSettingString(WidgetModeKey, WidgetMode);
    }

    public void SetSelectedAdapterId(string? adapterId)
    {
        SelectedAdapterId = adapterId ?? string.Empty;
        _databaseService.SetSettingString(SelectedAdapterKey, SelectedAdapterId);
    }

    private static string NormalizeWidgetMode(string? mode)
    {
        return mode switch
        {
            WidgetModeTopAdapter => WidgetModeTopAdapter,
            WidgetModeSelectedAdapter => WidgetModeSelectedAdapter,
            _ => WidgetModeTotal
        };
    }
}