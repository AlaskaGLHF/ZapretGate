namespace ZapretGate.Services.Interfaces;

public interface IProcessManagerService : IDisposable
{
    
    bool IsRunning { get; }
    
    event Action<string>? OutputDataReceived;
    event Action<string>? ErrorDataReceived;
    event Action? ProcessExited;

    Task StartAsync(string batFilePath);
    Task StopAsync();
    
}