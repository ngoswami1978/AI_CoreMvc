namespace ManpowerMonitoringTool.Services;

public sealed class BrowserAutomationOptions
{
    public string Url { get; init; } = string.Empty;
    public string UnitSelector { get; init; } = string.Empty;
    public string YearSelector { get; init; } = string.Empty;
    public string MonthSelector { get; init; } = string.Empty;
    public string SearchButtonSelector { get; init; } = string.Empty;
    public string SaveButtonSelector { get; init; } = "#convert_table_newid";
    public string TableSelector { get; init; } = "#MANPOWERCOST_FUNCTIONWISE_TAB2";
    public int ActionDelayMilliseconds { get; init; } = 2000;
    public int DropdownTypingDelayMilliseconds { get; init; } = 1000;
    public int CostTypingDelayMilliseconds { get; init; } = 150;
    public bool KeepBrowserOpen { get; init; } = true;
}
