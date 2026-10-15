namespace ZapretGate.Services.Interfaces;

public interface IAutoStartService
{
    
    bool IsAutoStartEnabled();
    void SetAutoStart(bool enable);
    
}