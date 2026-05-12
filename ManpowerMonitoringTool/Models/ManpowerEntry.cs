namespace ManpowerMonitoringTool.Models;

public sealed class ManpowerEntry
{
    public string UnitCode { get; init; } = string.Empty;
    public string UnitName { get; init; } = string.Empty;
    public int CurrentYear { get; init; }
    public int CurrentMonth { get; init; }
    public string Function { get; init; } = string.Empty;
    public decimal ActualMpCost { get; init; }
    public decimal ActualMpCostLeasing { get; init; }
    public decimal TotalMpCost { get; init; }
    public string Section { get; init; } = string.Empty;
}
