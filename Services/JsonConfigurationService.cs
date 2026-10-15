using System.IO;
using System.Text.Json;
using ZapretGate.Models;
using ZapretGate.Services.Interfaces;

namespace ZapretGate.Services;

public class JsonConfigurationService : IConfigurationService
{
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
        
    };

    private readonly string _configFilePath;
    public AppSettings Settings { get; private set; } = new();

    public JsonConfigurationService()
    {
        
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var projectFolder = Path.Combine(appDataFolder, "ZapretGate");

        if (!Directory.Exists(projectFolder))
        {
            
            Directory.CreateDirectory(projectFolder);
            
        }

        _configFilePath = Path.Combine(projectFolder, "settings.json");
        
    }

    public async Task LoadSettingsAsync()
    {
        
        if (!File.Exists(_configFilePath))
        {
            
            Settings = new AppSettings();
            await SaveSettingsAsync();
            return;
            
        }

        try
        {
            
            var json = await File.ReadAllTextAsync(_configFilePath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            
        }
        catch
        {
            
            Settings = new AppSettings();
            
        }
        
    }

    public async Task SaveSettingsAsync()
    {
        
        var json = JsonSerializer.Serialize(Settings, JsonOptions);
        await File.WriteAllTextAsync(_configFilePath, json);
        
    }
    
}