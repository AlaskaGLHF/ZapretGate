using System.IO;
using ZapretGate.Models;
using ZapretGate.Services.Interfaces;

namespace ZapretGate.Services;

public class StrategyDiscoveryService : IStrategyDiscoveryService
{
    public bool ValidateZapretDirectory(string directoryPath)
    {
        
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            
            return false;
            
        }

        var winwsPath = Path.Combine(directoryPath, "bin", "winws.exe");
        var winDivertPath = Path.Combine(directoryPath, "bin", "WinDivert.dll");

        return File.Exists(winwsPath) && File.Exists(winDivertPath);
        
    }

    public IReadOnlyList<StrategyModel> DiscoverStrategies(string directoryPath)
    {
        if (!ValidateZapretDirectory(directoryPath))
        {
            
            return Array.Empty<StrategyModel>();
            
        }

        var batFiles = Directory.GetFiles(directoryPath, "general*.bat", SearchOption.TopDirectoryOnly);

        var strategies = batFiles
            .Select(filePath => new StrategyModel
            {
                
                Name = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath
                
            })
            
            .OrderBy(s => s.Name)
            .ToList();

        return strategies;
    }
}