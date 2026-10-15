using ZapretGate.Models;

namespace ZapretGate.Services.Interfaces;

public interface IStrategyDiscoveryService
{
    
    bool ValidateZapretDirectory(string directoryPath);
    IReadOnlyList<StrategyModel> DiscoverStrategies(string directoryPath);
    
}