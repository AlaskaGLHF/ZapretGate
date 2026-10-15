using ZapretGate.Models;

namespace ZapretGate.Services.Interfaces;

public interface IConfigurationService
{
    
    AppSettings Settings { get; }
    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
    
}