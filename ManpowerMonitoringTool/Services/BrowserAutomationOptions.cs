namespace ManpowerMonitoringTool.Services;

public sealed class BrowserAutomationOptions
{
    public string Url { get; init; } = string.Empty;
    public string UnitSelector { get; init; } = string.Empty;
    public string YearSelector { get; init; } = string.Empty;
    public string MonthSelector { get; init; } = string.Empty;
    public string SearchButtonSelector { get; init; } = string.Empty;
    public string TableSelector { get; init; } = "table";
    public bool KeepBrowserOpen { get; init; } = true;
}
