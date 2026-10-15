using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Controls;
using ZapretGate.Models;
using ZapretGate.Services;
using ZapretGate.Services.Interfaces;

namespace ZapretGate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;
    private readonly IStrategyDiscoveryService _discoveryService;
    private readonly IProcessManagerService _processManager;
    private readonly ZapretServiceTools _tools;
    private readonly DispatcherTimer _timer;
    private readonly IToastService _toastService;
    private DateTime _startTime;
    private bool _isInitializingFeatures = true;
    private bool _isRefreshingStrategies = false;

    [ObservableProperty]
    private ObservableCollection<StrategyModel> _strategies = new();

    [ObservableProperty]
    private StrategyModel? _selectedStrategy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusDescription))]
    [NotifyPropertyChangedFor(nameof(CanChangeSettings))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusDescription))]
    [NotifyPropertyChangedFor(nameof(CanChangeSettings))]
    private bool _isStarting;

    [ObservableProperty]
    private string _elapsedTime = "00:00:00";

    [ObservableProperty]
    private string _errorMessage = string.Empty;
    
    [ObservableProperty]
    private int _selectedGameFilterIndex; // 0 - выкл, 1 - TCP+UDP, 2 - TCP, 3 - UDP, 4 - Кастомные порты

    [ObservableProperty]
    private int _selectedIpSetModeIndex; // 0 - Loaded, 1 - None, 2 - Any

    [ObservableProperty]
    private string _filterStatusMessage = string.Empty;

    public bool CanChangeSettings => !IsRunning && !IsStarting;

    public string StatusDescription
    {
        get
        {
            if (IsStarting) return "Подключение...";
            return IsRunning ? "Подключено (нажмите для остановки)" : "Нажмите для подключения";
        }
    }

    public MainViewModel(
        IConfigurationService configService,
        IStrategyDiscoveryService discoveryService,
        IProcessManagerService processManager,
        ZapretServiceTools tools,
        IToastService toastService)
    {
        _configService = configService;
        _discoveryService = discoveryService;
        _processManager = processManager;
        _tools = tools;
        _toastService = toastService;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            var span = DateTime.Now - _startTime;
            ElapsedTime = span.ToString(@"hh\:mm\:ss");
        };
    }

    public async Task InitializeAsync()
    {
        await _configService.LoadSettingsAsync();
        RefreshStrategies();
        LoadFeaturesState();
        
        if (_configService.Settings?.AutoConnectOnStartup == true && SelectedStrategy != null && !IsRunning)
        {
            await Task.Delay(2500);
            await ToggleServiceAsync();
        }
    }

    public void LoadFeaturesState()
    {
        _isInitializingFeatures = true;
        
        var state = _tools.GetCurrentFeaturesState();

        SelectedGameFilterIndex = state.GameFilterMode switch
        {
            "all" => 1,
            "tcp" => 2,
            "udp" => 3,
            "custom" => 4,
            _ => 0
        };

        SelectedIpSetModeIndex = state.IpsetStatus switch
        {
            "any" => 2,
            "none" => 1,
            _ => 0
        };

        _isInitializingFeatures = false;
    }

    partial void OnSelectedGameFilterIndexChanged(int value)
    {
        if (_isInitializingFeatures) return;

        string mode = value switch
        {
            1 => "all",
            2 => "tcp",
            3 => "udp",
            4 => "custom",
            _ => "disabled"
        };

        _tools.SetGameFilterMode(mode);

        string modeTitle = value switch
        {
            1 => "TCP и UDP (Полный)",
            2 => "Только TCP",
            3 => "Только UDP",
            4 => $"Кастомные порты ({_tools.GetCustomGameFilterPorts()})",
            _ => "Отключен"
        };

        FilterStatusMessage = IsRunning 
            ? "Игровой фильтр сохранен. Нажмите на замок для перезапуска обхода." 
            : "Игровой фильтр сохранен.";

        _toastService.Show(
            "Игровой фильтр",
            IsRunning 
                ? $"Режим: {modeTitle}. Нажмите на замок для перезапуска обхода." 
                : $"Режим изменён на: {modeTitle}.",
            new SymbolIcon(SymbolRegular.Games24),
            timeoutSeconds: 3
        );
    }

    partial void OnSelectedIpSetModeIndexChanged(int value)
    {
        if (_isInitializingFeatures) return;

        string mode = value switch
        {
            1 => "none",
            2 => "any",
            _ => "loaded"
        };

        _tools.ToggleIpSetMode(mode);

        string modeTitle = value switch
        {
            1 => "NONE (Игнорировать)",
            2 => "ANY (Весь интернет)",
            _ => "LOADED (Активен)"
        };

        FilterStatusMessage = IsRunning 
            ? "IPSet сохранен. Нажмите на замок для перезапуска обхода." 
            : $"IPSet переключен в: {mode.ToUpperInvariant()}";

        _toastService.Show(
            "Фильтрация IPSet",
            IsRunning 
                ? $"Режим: {modeTitle}. Нажмите на замок для перезапуска обхода." 
                : $"Режим переключен на: {modeTitle}.",
            new SymbolIcon(SymbolRegular.Filter24),
            timeoutSeconds: 3
        );
    }

    async partial void OnSelectedStrategyChanged(StrategyModel? value)
    {
        if (_isRefreshingStrategies || value == null || _configService.Settings == null)
            return;

        _configService.Settings.SelectedStrategyName = value.Name;
        await _configService.SaveSettingsAsync();
    }

    public void RefreshStrategies()
    {
        _isRefreshingStrategies = true;
        try
        {
            Strategies.Clear();
            var path = _configService.Settings?.ZapretDirectoryPath;

            if (string.IsNullOrWhiteSpace(path) || !_discoveryService.ValidateZapretDirectory(path))
            {
                ErrorMessage = "Укажите верный путь к zapret в настройках.";
                return;
            }

            ErrorMessage = string.Empty;
            var list = _discoveryService.DiscoverStrategies(path)
                .OrderBy(s => s.DisplayTitle)
                .ToList();
            var recommended = _configService.Settings?.RecommendedStrategyName;

            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(recommended) && item.Name.Contains(recommended, StringComparison.OrdinalIgnoreCase))
                {
                    item.IsRecommended = true;
                }
                Strategies.Add(item);
            }

            var savedName = _configService.Settings?.SelectedStrategyName;

            SelectedStrategy = Strategies.FirstOrDefault(s => 
                                   string.Equals(s.Name, savedName, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(Path.GetFileNameWithoutExtension(s.Name), savedName, StringComparison.OrdinalIgnoreCase))
                               ?? Strategies.FirstOrDefault(s => s.IsRecommended)
                               ?? Strategies.FirstOrDefault();
        }
        finally
        {
            _isRefreshingStrategies = false;
        }
    }

    [RelayCommand]
    private async Task ToggleServiceAsync()
    {
        if (IsStarting) return;

        ErrorMessage = string.Empty;

        if (IsRunning || _processManager.IsRunning)
        {
            IsRunning = false;
            IsStarting = false;
            _timer.Stop();
            ElapsedTime = "00:00:00";

            await _processManager.StopAsync();
            return;
        }

        if (SelectedStrategy == null)
        {
            ErrorMessage = "Выберите стратегию перед запуском.";
            return;
        }

        try
        {
            IsStarting = true;

            await _processManager.StartAsync(SelectedStrategy.FilePath);

            IsStarting = false;
            IsRunning = true;
            _startTime = DateTime.Now;
            ElapsedTime = "00:00:00";
            _timer.Start();

            if (_configService.Settings != null)
            {
                _configService.Settings.SelectedStrategyName = SelectedStrategy.Name;
                await _configService.SaveSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            IsStarting = false;
            IsRunning = false;
            _timer.Stop();
            ElapsedTime = "00:00:00";
            ErrorMessage = ex.Message;
        }
    }
}