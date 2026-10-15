using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ZapretGate.Services.Interfaces;

namespace ZapretGate.Services;

public record TestProgressReport(
    string CurrentConfig,
    int CurrentIndex,
    int TotalConfigs,
    double ProgressPercentage
);

public record ZapretFeaturesState(
    string GameFilterMode,
    bool CheckUpdatesEnabled,
    string IpsetStatus,
    string ActiveDiscordFake,
    string ActiveGameFake,
    List<string> AvailableFakeFiles
);

public class ZapretServiceTools
{
    private readonly IConfigurationService _configService;
    private readonly IStrategyDiscoveryService _discoveryService;
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private Process? _activeProcess;

    public ZapretServiceTools(IConfigurationService configService, IStrategyDiscoveryService discoveryService)
    {
        _configService = configService;
        _discoveryService = discoveryService;
    }

    public List<string> GetAvailableStrategyNames()
    {
        var zapretDir = _configService.Settings.ZapretDirectoryPath;
        return _discoveryService.DiscoverStrategies(zapretDir)
                                .Select(s => s.Name)
                                .Distinct()
                                .ToList();
    }


    // ТЕСТИРОВАНИЕ СТРАТЕГИЙ

    public async Task<string?> RunTestAsync(
        bool isDpiChecker,
        IProgress<TestProgressReport> progress,
        CancellationToken ct)
    {
        var zapretDir = _configService.Settings.ZapretDirectoryPath;
        var psScript = Path.Combine(zapretDir, "utils", "test zapret.ps1");
        var strategies = GetAvailableStrategyNames();

        if (strategies.Count == 0) return null;

        if (!File.Exists(psScript))
        {
            return strategies.FirstOrDefault();
        }

        // Завершение ранее запущенных процессов winws
        await Task.Run(() =>
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("winws"))
                {
                    try { p.Kill(); p.WaitForExit(400); } catch { }
                    finally { p.Dispose(); }
                }
            }
            catch { }
        }, ct);

        string testTypeChoice = isDpiChecker ? "2" : "1";
        string psBootstrap = 
            $"$script:inputs = [System.Collections.Generic.Queue[string]]@('{testTypeChoice}', '1'); " +
            "function Read-Host($prompt) { if ($script:inputs.Count -gt 0) { return $script:inputs.Dequeue() } return '1' }; " +
            "function Pause { }; " +
            "& '" + psScript + "' *>&1";

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{psBootstrap}\"",
            WorkingDirectory = zapretDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        _activeProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        
        string? winnerConfig = null;
        int totalFound = strategies.Count;

        _activeProcess.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data)) return;
            string line = Regex.Replace(e.Data, @"\x1B\[[^@-~]*[@-~]", string.Empty).Trim();

            // Ловим текущий шаг
            var stepMatch = Regex.Match(line, @"\[(\d+)/(\d+)\]\s*([a-zA-Z0-9_\-\(\)\s\.]+)", RegexOptions.IgnoreCase);
            if (stepMatch.Success)
            {
                int current = int.Parse(stepMatch.Groups[1].Value);
                totalFound = int.Parse(stepMatch.Groups[2].Value);
                string strat = Path.GetFileNameWithoutExtension(stepMatch.Groups[3].Value.Trim());
                
                double pct = Math.Round(((double)(current - 1) / totalFound) * 100, 1);
                progress.Report(new TestProgressReport(strat, current, totalFound, pct));
                return;
            }

            // Слушаем финал теста
            var bestMatch = Regex.Match(line, @"(?:Best\s*config|Best\s*strategy|Победитель)[:\s]+([a-zA-Z0-9_\-\(\)\s\.]+)", RegexOptions.IgnoreCase);
            if (bestMatch.Success)
            {
                string raw = bestMatch.Groups[1].Value.Trim();
                winnerConfig = Path.GetFileNameWithoutExtension(raw);
            }
        };

        _activeProcess.Start();
        _activeProcess.BeginOutputReadLine();
        _activeProcess.BeginErrorReadLine();

        try
        {
            await _activeProcess.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            StopBenchmark();
            return null;
        }

        progress.Report(new TestProgressReport("Тестирование завершено", totalFound, totalFound, 100.0));
        return winnerConfig ?? strategies.FirstOrDefault();
    }

    public void StopBenchmark()
    {
        try
        {
            if (_activeProcess is { HasExited: false })
            {
                _activeProcess.Kill(entireProcessTree: true);
            }
        }
        catch { }
        finally
        {
            _activeProcess?.Dispose();
            _activeProcess = null;

            try
            {
                foreach (var p in Process.GetProcessesByName("winws"))
                {
                    try { p.Kill(); } catch { }
                    finally { p.Dispose(); }
                }
            }
            catch { }
        }
    }
    
    // СОСТОЯНИЕ И УПРАВЛЕНИЕ ФУНКЦИЯМИ SERVICE.BAT
    
    public ZapretFeaturesState GetCurrentFeaturesState()
    {
        var zapretDir = _configService.Settings?.ZapretDirectoryPath;
        if (string.IsNullOrWhiteSpace(zapretDir) || !Directory.Exists(zapretDir))
        {
            return new ZapretFeaturesState("disabled", false, "none", "(не найден)", "(не найден)", []);
        }

        try
        {
            // Игровой фильтр
            string gameFlagFile = Path.Combine(zapretDir, "utils", "game_filter.enabled");
            string gameMode = "disabled";
            if (File.Exists(gameFlagFile))
            {
                var content = File.ReadAllText(gameFlagFile).Trim().ToLowerInvariant();
                gameMode = content switch
                {
                    "all" => "all",
                    "tcp" => "tcp",
                    "udp" => "udp",
                    "custom" => "custom",
                    _ => "udp"
                };
            }

            // Флаг проверки обновлений
            string updateFlagFile = Path.Combine(zapretDir, "utils", "check_updates.enabled");
            bool checkUpdates = File.Exists(updateFlagFile);

            // Статус IPSet
            string ipsetFile = Path.Combine(zapretDir, "lists", "ipset-all.txt");
            string ipsetStatus = "none";
            if (File.Exists(ipsetFile))
            {
                var text = File.ReadAllText(ipsetFile).Trim();
                if (string.IsNullOrEmpty(text))
                {
                    ipsetStatus = "any";
                }
                else if (text.Contains("203.0.113.113/32"))
                {
                    ipsetStatus = "none";
                }
                else
                {
                    ipsetStatus = "loaded";
                }
            }

            // Активные фейки
            var binPath = Path.Combine(zapretDir, "bin");
            var availableFakes = new List<string>();
            string currentDiscordFake = "(не найден)";
            string currentGameFake = "(не найден)";

            if (Directory.Exists(binPath))
            {
                var binFiles = Directory.GetFiles(binPath, "*.bin")
                    .Select(Path.GetFileName)
                    .OfType<string>()
                    .Where(f => !string.IsNullOrEmpty(f) && !f.StartsWith("ACTIVE_", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                availableFakes.AddRange(binFiles);

                currentDiscordFake = GetMatchingFakeName(binPath, "ACTIVE_DISCORD_UDP.bin", availableFakes);
                currentGameFake = GetMatchingFakeName(binPath, "ACTIVE_GAME_UDP.bin", availableFakes);
            }

            return new ZapretFeaturesState(
                gameMode,
                checkUpdates,
                ipsetStatus,
                currentDiscordFake,
                currentGameFake,
                availableFakes
            );
        }
        catch
        {
            return new ZapretFeaturesState("disabled", false, "none", "(ошибка)", "(ошибка)", []);
        }
    }

    public void SetGameFilterMode(string mode)
    {
        var zapretDir = _configService.Settings?.ZapretDirectoryPath;
        if (string.IsNullOrWhiteSpace(zapretDir) || !Directory.Exists(zapretDir)) return;

        var utilsDir = Path.Combine(zapretDir, "utils");
        Directory.CreateDirectory(utilsDir);
        var gameFlagFile = Path.Combine(utilsDir, "game_filter.enabled");
        
        if (string.Equals(mode, "disabled", StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(gameFlagFile))
            {
                File.Delete(gameFlagFile);
            }
        }
        else
        {
            // Записываем 'all', 'tcp', 'udp' или 'custom'
            File.WriteAllText(gameFlagFile, mode.Trim().ToLowerInvariant());
        }
    }

    // Методы для работы с кастомными портами GameFilter
    public string GetCustomGameFilterPorts()
    {
        var zapretDir = _configService.Settings?.ZapretDirectoryPath;
        if (string.IsNullOrWhiteSpace(zapretDir)) return "1024-65535";

        var portsFile = Path.Combine(zapretDir, "utils", "game_filter.ports");
        if (File.Exists(portsFile))
        {
            var content = File.ReadAllText(portsFile).Trim();
            if (!string.IsNullOrEmpty(content)) return content;
        }
        return "1024-65535";
    }

    public void SetCustomGameFilterPorts(string ports)
    {
        var zapretDir = _configService.Settings?.ZapretDirectoryPath;
        if (string.IsNullOrWhiteSpace(zapretDir)) return;

        var utilsDir = Path.Combine(zapretDir, "utils");
        Directory.CreateDirectory(utilsDir);
        
        var portsFile = Path.Combine(utilsDir, "game_filter.ports");
        File.WriteAllText(portsFile, string.IsNullOrWhiteSpace(ports) ? "1024-65535" : ports.Trim());
    }

    public void SetAutoUpdateCheck(bool enable)
    {
        var zapretDir = _configService.Settings?.ZapretDirectoryPath;
        if (string.IsNullOrWhiteSpace(zapretDir) || !Directory.Exists(zapretDir)) return;

        var utilsDir = Path.Combine(zapretDir, "utils");
        Directory.CreateDirectory(utilsDir);
        var updateFlagFile = Path.Combine(utilsDir, "check_updates.enabled");
        
        if (enable)
        {
            File.WriteAllText(updateFlagFile, "ENABLED");
        }
        else if (File.Exists(updateFlagFile))
        {
            File.Delete(updateFlagFile);
        }
    }

    public void ToggleIpSetMode(string targetMode)
    {
        var zapretDir = _configService.Settings?.ZapretDirectoryPath;
        if (string.IsNullOrWhiteSpace(zapretDir) || !Directory.Exists(zapretDir)) return;

        var listDir = Path.Combine(zapretDir, "lists");
        Directory.CreateDirectory(listDir);

        var listFile = Path.Combine(listDir, "ipset-all.txt");
        var backupFile = Path.Combine(listDir, "ipset-all.txt.backup");
        
        if (targetMode == "none")
        {
            if (File.Exists(listFile) && !File.Exists(backupFile))
            {
                File.Copy(listFile, backupFile, true);
            }
            File.WriteAllText(listFile, "203.0.113.113/32\r\n");
        }
        else if (targetMode == "any")
        {
            if (File.Exists(listFile) && !File.Exists(backupFile))
            {
                File.Copy(listFile, backupFile, true);
            }
            File.WriteAllText(listFile, string.Empty);
        }
        else if (targetMode == "loaded")
        {
            if (File.Exists(backupFile))
            {
                File.Copy(backupFile, listFile, true);
            }
            else
            {
                _ = UpdateIpSetListAsync();
            }
        }
    }

    public async Task<string> UpdateHostsFileAsync()
    {
        try
        {
            const string hostsUrl = "https://raw.githubusercontent.com/Flowseal/zapret-discord-youtube/refs/heads/main/.service/hosts";
            var content = await HttpClient.GetStringAsync(hostsUrl);
            var hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");

            if (!File.Exists(hostsPath)) return "Файл hosts не найден.";

            var currentContent = await File.ReadAllTextAsync(hostsPath);
            var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0 && currentContent.Contains(lines[0]))
            {
                return "Файл hosts уже содержит актуальные записи.";
            }

            await File.AppendAllTextAsync(hostsPath, "\n# Zapret hosts fix\n" + content);
            return "Файл hosts успешно дополнен необходимыми записями!";
        }
        catch (Exception ex)
        {
            return $"Ошибка обновления hosts (нужны права администратора): {ex.Message}";
        }
    }

    public bool ReplaceActiveFake(string fakeType, string sourceFileName)
    {
        var zapretDir = _configService.Settings?.ZapretDirectoryPath;
        if (string.IsNullOrWhiteSpace(zapretDir)) return false;

        var binPath = Path.Combine(zapretDir, "bin");
        var sourceFile = Path.Combine(binPath, sourceFileName);

        string targetFileName = fakeType == "discord" ? "ACTIVE_DISCORD_UDP.bin" : "ACTIVE_GAME_UDP.bin";
        string targetFile = Path.Combine(binPath, targetFileName);

        if (!File.Exists(sourceFile)) return false;

        try
        {
            File.Copy(sourceFile, targetFile, true);
            return true;
        }
        catch { return false; }
    }

    public void OpenUserListFile(string listFileName)
    {
        var zapretDir = _configService.Settings?.ZapretDirectoryPath;
        if (string.IsNullOrWhiteSpace(zapretDir)) return;

        var filePath = Path.Combine(zapretDir, "lists", listFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "# Внесите ваши домены или IP по одному на строку\r\n");
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "notepad.exe",
            Arguments = $"\"{filePath}\"",
            UseShellExecute = true
        });
    }

    private static string GetMatchingFakeName(string binPath, string activeFileName, List<string> availableFakes)
    {
        var activePath = Path.Combine(binPath, activeFileName);
        if (!File.Exists(activePath)) return "(не установлен)";

        try
        {
            using var sha256 = SHA256.Create();
            using var activeStream = File.OpenRead(activePath);
            var activeHash = Convert.ToHexString(sha256.ComputeHash(activeStream));

            foreach (var fake in availableFakes)
            {
                var fakePath = Path.Combine(binPath, fake);
                if (!File.Exists(fakePath)) continue;

                using var fakeStream = File.OpenRead(fakePath);
                var fakeHash = Convert.ToHexString(sha256.ComputeHash(fakeStream));

                if (string.Equals(activeHash, fakeHash, StringComparison.OrdinalIgnoreCase))
                {
                    return fake;
                }
            }
        }
        catch
        {
            // ignored
        }

        return "(пользовательский)";
    }
    
    // СИСТЕМНЫЕ СЛУЖБЫ, ДИАГНОСТИКА И КЭШ
    
    public async Task<List<string>> RunDiagnosticsAsync()
    {
        var report = new List<string>();
        await Task.Run(() =>
        {
            bool bfeRunning = CheckServiceRunning("BFE");
            report.Add(bfeRunning ? "[OK] Служба фильтрации Windows (BFE) активна" : "[!] BFE выключена! Zapret не сможет работать");

            string[] conflictingProcesses = ["AdguardSvc", "GoodbyeDPI"];
            foreach (var proc in conflictingProcesses)
            {
                var processes = Process.GetProcessesByName(proc);
                if (processes.Length > 0)
                {
                    report.Add($"[Внимание] Обнаружен конфликтующий процесс: {proc}");
                    foreach (var p in processes) p.Dispose();
                }
            }

            var zapretDir = _configService.Settings?.ZapretDirectoryPath;
            if (!string.IsNullOrWhiteSpace(zapretDir))
            {
                var driverPath = Path.Combine(zapretDir, "bin", "WinDivert64.sys");
                report.Add(File.Exists(driverPath)
                    ? "[OK] Файлы перехватчика WinDivert на месте"
                    : "[Ошибка] Файл WinDivert64.sys не найден в папке bin/");
            }
        });
        return report;
    }

    public async Task<bool> ClearDiscordCacheAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                foreach (var name in new[] { "Discord", "DiscordPTB" })
                {
                    foreach (var proc in Process.GetProcessesByName(name))
                    {
                        try { proc.Kill(); proc.WaitForExit(800); } catch { }
                        finally { proc.Dispose(); }
                    }
                }

                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string[] discordFolders = [Path.Combine(appData, "discord"), Path.Combine(appData, "discordptb")];
                string[] cacheSubFolders = ["Cache", "Code Cache", "GPUCache"];

                bool cleaned = false;
                foreach (var dFolder in discordFolders)
                {
                    if (!Directory.Exists(dFolder)) continue;
                    foreach (var sub in cacheSubFolders)
                    {
                        var target = Path.Combine(dFolder, sub);
                        if (Directory.Exists(target))
                        {
                            Directory.Delete(target, true);
                            cleaned = true;
                        }
                    }
                }
                return cleaned;
            }
            catch { return false; }
        });
    }

    public async Task<bool> UpdateIpSetListAsync()
    {
        try
        {
            var zapretDir = _configService.Settings?.ZapretDirectoryPath;
            if (string.IsNullOrWhiteSpace(zapretDir)) return false;

            var ipsetPath = Path.Combine(zapretDir, "lists", "ipset-all.txt");
            const string url = "https://raw.githubusercontent.com/Flowseal/zapret-discord-youtube/refs/heads/main/.service/ipset-service.txt";

            var data = await HttpClient.GetStringAsync(url);
            if (!string.IsNullOrWhiteSpace(data))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ipsetPath)!);
                await File.WriteAllTextAsync(ipsetPath, data);
                return true;
            }
        }
        catch { return false; }

        return false;
    }

    private static bool CheckServiceRunning(string serviceName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = "query \"" + serviceName + "\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(startInfo);
            if (proc == null) return false;

            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase);
        }
        catch 
        { 
            return false; 
        }
    }
    
public async Task<bool> UpdateZapretRepositoryAsync(IProgress<string> progress)
{
    try
    {
        var zapretDir = _configService.Settings?.ZapretDirectoryPath;
        if (string.IsNullOrWhiteSpace(zapretDir) || !Directory.Exists(zapretDir))
        {
            progress.Report("Путь к пачке zapret не выбран или не существует.");
            return false;
        }

        progress.Report("Остановка активных процессов winws...");
        try
        {
            foreach (var process in Process.GetProcessesByName("winws"))
            {
                process.Kill();
                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ZapretUpdate] Warning while stopping winws: {ex.Message}");
        }

        progress.Report("Скачивание актуальной версии с GitHub...");
        const string url = "https://github.com/Flowseal/zapret-discord-youtube/archive/refs/heads/main.zip";
        
        var zipBytes = await HttpClient.GetByteArrayAsync(url);
        var tempZipPath = Path.Combine(Path.GetTempPath(), "zapret_update.zip");
        await File.WriteAllBytesAsync(tempZipPath, zipBytes);

        progress.Report("Распаковка загруженного архива...");
        var tempExtractDir = Path.Combine(Path.GetTempPath(), "zapret_extracted");
        if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
        
        System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, tempExtractDir);

        var extractedFolders = Directory.GetDirectories(tempExtractDir);
        if (extractedFolders.Length == 0)
        {
            progress.Report("Ошибка: архив с GitHub оказался пустым.");
            return false;
        }
        var sourceDir = extractedFolders[0];

        progress.Report("Обновление файлов в целевой папке...");
        CopyDirectoryRecursive(new DirectoryInfo(sourceDir), new DirectoryInfo(zapretDir));

        try { File.Delete(tempZipPath); } catch { }
        try { Directory.Delete(tempExtractDir, true); } catch { }

        progress.Report("Репозиторий успешно обновлен!");
        return true;
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[ZapretUpdate EXCEPTION]: {ex.Message}");
        progress.Report("Ошибка обновления: проверьте интернет-соединение или отключите VPN.");
        return false;
    }
}

    private static void CopyDirectoryRecursive(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);
        foreach (FileInfo fi in source.GetFiles())
        {
            string targetFilePath = Path.Combine(target.FullName, fi.Name);
            // Защита пользовательских списков
            if (fi.Name.Equals("list-general-user.txt", StringComparison.OrdinalIgnoreCase) ||
                fi.Name.Equals("list-exclude-user.txt", StringComparison.OrdinalIgnoreCase) ||
                fi.Name.Equals("ipset-exclude-user.txt", StringComparison.OrdinalIgnoreCase) ||
                fi.Name.Equals("game_filter.ports", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(targetFilePath)) continue;
            }

            fi.CopyTo(targetFilePath, true);
        }

        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyDirectoryRecursive(diSourceSubDir, nextTargetSubDir);
        }
    }
    
    public string GetZapretVersion()
    {
        var zapretDir = _configService.Settings?.ZapretDirectoryPath;
        if (string.IsNullOrWhiteSpace(zapretDir) || !Directory.Exists(zapretDir)) 
            return "Текущая версия: Не найдено";

        try
        {
            var versionFile = Path.Combine(zapretDir, ".service", "version.txt");
            if (File.Exists(versionFile))
            {
                var content = File.ReadAllText(versionFile).Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    // Если строка уже начинается с 'v', не добавляем лишнюю букву
                    string versionString = content.StartsWith("v", StringComparison.OrdinalIgnoreCase) 
                        ? content 
                        : $"v{content}";

                    return $"Текущая версия: {versionString}";
                }
            }
        }
        catch { }

        return "Текущая версия: Не определена";
    }
}