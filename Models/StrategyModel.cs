namespace ZapretGate.Models;

public class StrategyModel
{
    
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool IsRecommended { get; set; }

    public string DisplayTitle => IsRecommended ? $" {Name} (Лучшая)" : Name;
    
}