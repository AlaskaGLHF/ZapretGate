using Microsoft.Win32;
using ZapretGate.Services.Interfaces;

namespace ZapretGate.Services;

public class AutoStartService : IAutoStartService
{
    
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ZapretGate";

    public bool IsAutoStartEnabled()
    {
        
        try
        {
            
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
            return key?.GetValue(AppName) != null;
            
        }
        catch
        {
            
            return false;
            
        }
        
    }

    public void SetAutoStart(bool enable)
    {
        
        try
        {
            
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    
                    key.SetValue(AppName, $"\"{exePath}\"");
                    
                }
                
            }
            else
            {
                
                key.DeleteValue(AppName, false);
                
            }
        }
        catch
        {
            
            // ignored
            
        }
        
    }
}