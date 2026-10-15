namespace ZapretGate.Models;

public class AppSettings
{
    
    public string ZapretDirectoryPath { get; set; } = string.Empty;
    public string? SelectedStrategyName { get; set; }
    public string? RecommendedStrategyName { get; set; } 
    public bool AutoConnectOnStartup { get; set; } = false;
    public string ThemeMode { get; set; } = "Dark";
    public bool IsFirstRunCompleted { get; set; } = false;
}