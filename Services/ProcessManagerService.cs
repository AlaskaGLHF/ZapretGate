using System.Diagnostics;
using System.IO;
using ZapretGate.Services.Interfaces;

namespace ZapretGate.Services;

public partial class ProcessManagerService : IProcessManagerService
{
    private Process? _hostProcess;
    private readonly object _lock = new();

    public bool IsRunning
    {
        
        get
        {
            
            var list = Process.GetProcessesByName("winws");
            var running = list.Length > 0;
            foreach (var p in list)
            {
                p.Dispose();
            }
            return running;
            
        }
        
    }

    public event Action<string>? OutputDataReceived;
    public event Action<string>? ErrorDataReceived;
    public event Action? ProcessExited;

    public async Task StartAsync(string batFilePath)
    {
        
        if (IsRunning)
        {
            
            throw new InvalidOperationException("Обход уже запущен.");
            
        }

        if (!File.Exists(batFilePath))
        {
            
            throw new FileNotFoundException($"Файл не найден: {batFilePath}");
            
        }

        var workDir = Path.GetDirectoryName(batFilePath)
            ?? throw new DirectoryNotFoundException("Рабочая папка не найдена.");

        KillAllWinwsProcesses();

        var startInfo = new ProcessStartInfo
        {
            
            FileName = "cmd.exe",
            Arguments = $"/c chcp 65001 >nul && call \"{batFilePath}\"",
            WorkingDirectory = workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
            
        };

        var outputLog = new System.Text.StringBuilder();

        lock (_lock)
        {
            _hostProcess = new Process
            {
                
                StartInfo = startInfo,
                EnableRaisingEvents = true
                
            };

            _hostProcess.OutputDataReceived += (_, e) =>
            {
                
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    
                    outputLog.AppendLine(e.Data);
                    OutputDataReceived?.Invoke(e.Data);
                    
                }
                
            };

            _hostProcess.ErrorDataReceived += (_, e) =>
            {
                
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    
                    outputLog.AppendLine(e.Data);
                    ErrorDataReceived?.Invoke(e.Data);
                    
                }
                
            };

            _hostProcess.Start();
            _hostProcess.BeginOutputReadLine();
            _hostProcess.BeginErrorReadLine();
            
        }

        // Ждем старта и сразу прячем окно из таскбара
        bool started = false;
        for (int i = 0; i < 30; i++)
        {
            
            await Task.Delay(200);
            WindowHider.HideProcessWindows("winws");
            WindowHider.HideProcessWindows("cmd");

            if (IsRunning)
            {
                
                started = true;
                break;
                
            }
            
        }

        // Повторный сброс навсякий случай
        WindowHider.HideProcessWindows("winws");

        if (!started)
        {
            
            await StopAsync();
            var logText = outputLog.ToString().Trim();
            if (!string.IsNullOrEmpty(logText))
            {
                
                throw new Exception($"winws не запустился:\n{logText}");
                
            }
            throw new Exception("Не удалось инициализировать winws.exe. Проверьте права администратора или выберите другую стратегию.");
            
        }

        _ = MonitorProcessAsync();
        
    }

    private async Task MonitorProcessAsync()
    {
        
        while (IsRunning)
        {
            
            WindowHider.HideProcessWindows("winws");
            await Task.Delay(1000);
            
        }

        App.Current?.Dispatcher.Invoke(() =>
        {
            
            ProcessExited?.Invoke();
            
        });
        
    }

    public async Task StopAsync()
    {
        
        await Task.Run(() =>
        {
            
            lock (_lock)
            {
                try
                {
                    
                    if (_hostProcess is { HasExited: false })
                    {
                        
                        _hostProcess.Kill(entireProcessTree: true);
                        
                    }
                    
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    
                    _hostProcess?.Dispose();
                    _hostProcess = null;
                    
                }
            }

            KillAllWinwsProcesses();
            
        });

        await Task.Delay(200);
        App.Current?.Dispatcher.Invoke(() =>
        {
            
            ProcessExited?.Invoke();
            
        });
    }

    private static void KillAllWinwsProcesses()
    {
        foreach (var proc in Process.GetProcessesByName("winws"))
        {
            try
            {
                
                proc.Kill();
                proc.WaitForExit(500);
                
            }
            catch
            {
                
                // ignored
                
            }
            finally
            {
                
                proc.Dispose();
                
            }
        }
    }

    public void Dispose()
    {
        
        _ = StopAsync();
        GC.SuppressFinalize(this);
        
    }
}