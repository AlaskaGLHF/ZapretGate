using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Controls;
using ZapretGate.Services;
using ZapretGate.Services.Interfaces;
using ZapretGate.Views;
using Application = System.Windows.Application;

namespace ZapretGate.ViewModels;

public partial class ServiceViewModel : ObservableObject
{
    private const string DefaultNoStrategyText = "Лучшая стратегия не выявлена";

    private readonly ZapretServiceTools _tools;
    private readonly IConfigurationService _configService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IToastService _toastService;
    private bool _isInitializing = true;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;
    
    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(ApplyBestStrategyCommand))]
    private string _bestStrategyFound = DefaultNoStrategyText;
    
    [ObservableProperty] private string _diagnosticsResult = string.Empty;
    
    [ObservableProperty] private bool _isAutoUpdateEnabled;
    
    [ObservableProperty] private string _customGamePorts = string.Empty;
    
    [ObservableProperty] private string _currentDiscordFake = string.Empty;
    [ObservableProperty] private string _currentGameFake = string.Empty;
    [ObservableProperty] private string? _selectedDiscordFakeFile;
    [ObservableProperty] private string? _selectedGameFakeFile;
    public ObservableCollection<string> AvailableFakeFiles { get; } = [];

    public ServiceViewModel(
        ZapretServiceTools tools,
        IConfigurationService configService,
        IServiceProvider serviceProvider,
        IToastService toastService)
    {
        _tools = tools;
        _configService = configService;
        _serviceProvider = serviceProvider;
        _toastService = toastService;

        UpdateBestStrategyText();
        LoadCurrentFeaturesState();
    }

    private void UpdateBestStrategyText()
    {
        var recommended = _configService.Settings?.RecommendedStrategyName;
        BestStrategyFound = string.IsNullOrWhiteSpace(recommended) 
            ? DefaultNoStrategyText 
            : recommended;
    }

    private bool CanApplyBestStrategy()
    {
        return !string.IsNullOrWhiteSpace(BestStrategyFound) && BestStrategyFound != DefaultNoStrategyText;
    }

    public void LoadCurrentFeaturesState()
    {
        _isInitializing = true;

        var state = _tools.GetCurrentFeaturesState();

        IsAutoUpdateEnabled = state.CheckUpdatesEnabled;
        CustomGamePorts = _tools.GetCustomGameFilterPorts();

        CurrentDiscordFake = state.ActiveDiscordFake;
        CurrentGameFake = state.ActiveGameFake;

        AvailableFakeFiles.Clear();
        foreach (var file in state.AvailableFakeFiles)
        {
            AvailableFakeFiles.Add(file);
        }

        SelectedDiscordFakeFile = AvailableFakeFiles.FirstOrDefault(f => f == state.ActiveDiscordFake) 
                                  ?? AvailableFakeFiles.FirstOrDefault();
        SelectedGameFakeFile = AvailableFakeFiles.FirstOrDefault(f => f == state.ActiveGameFake) 
                               ?? AvailableFakeFiles.FirstOrDefault();

        _isInitializing = false;
    }

    partial void OnIsAutoUpdateEnabledChanged(bool value)
    {
        if (_isInitializing) return;
        _tools.SetAutoUpdateCheck(value);

        _toastService.Show(
            "Автопроверка zapret",
            value ? "Автоматическая проверка обновлений включена." : "Автоматическая проверка отключена.",
            new SymbolIcon(SymbolRegular.ArrowSync24),
            timeoutSeconds: 3
        );
    }

    [RelayCommand]
    private void SaveCustomPorts()
    {
        _tools.SetCustomGameFilterPorts(CustomGamePorts);
        _toastService.Show(
            "Игровой фильтр",
            $"Кастомные порты сохранены: {CustomGamePorts}",
            new SymbolIcon(SymbolRegular.Games24),
            timeoutSeconds: 3
        );
    }

    [RelayCommand]
    private void ApplyDiscordFake()
    {
        if (string.IsNullOrWhiteSpace(SelectedDiscordFakeFile)) return;
        if (_tools.ReplaceActiveFake("discord", SelectedDiscordFakeFile))
        {
            CurrentDiscordFake = SelectedDiscordFakeFile;
            _toastService.Show(
                "Фейк Discord",
                $"Активный фейк заменён на {SelectedDiscordFakeFile}",
                new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                timeoutSeconds: 3
            );
        }
        else
        {
            _toastService.Show(
                "Ошибка подмены фейка",
                "Не удалось скопировать бинарный фейк.",
                new SymbolIcon(SymbolRegular.DismissCircle24),
                timeoutSeconds: 4
            );
        }
    }

    [RelayCommand]
    private void ApplyGameFake()
    {
        if (string.IsNullOrWhiteSpace(SelectedGameFakeFile)) return;
        if (_tools.ReplaceActiveFake("game", SelectedGameFakeFile))
        {
            CurrentGameFake = SelectedGameFakeFile;
            _toastService.Show(
                "Фейк сетевых игр",
                $"Активный фейк заменён на {SelectedGameFakeFile}",
                new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                timeoutSeconds: 3
            );
        }
        else
        {
            _toastService.Show(
                "Ошибка подмены фейка",
                "Не удалось скопировать бинарный фейк.",
                new SymbolIcon(SymbolRegular.DismissCircle24),
                timeoutSeconds: 4
            );
        }
    }

    [RelayCommand]
    private async Task UpdateHostsFileAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            string result = await _tools.UpdateHostsFileAsync();
            _toastService.Show(
                "Файл Hosts",
                result,
                new SymbolIcon(SymbolRegular.DocumentSync24),
                timeoutSeconds: 3
            );
        }
        catch (Exception ex)
        {
            _toastService.Show(
                "Ошибка обновления Hosts",
                ex.Message,
                new SymbolIcon(SymbolRegular.DismissCircle24),
                timeoutSeconds: 4
            );
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenUserList(string listName)
    {
        _tools.OpenUserListFile(listName);
        _toastService.Show(
            "Редактирование списка",
            $"Открыт файл: {listName}",
            new SymbolIcon(SymbolRegular.DocumentEdit24),
            timeoutSeconds: 3
        );
    }

    [RelayCommand]
    private void RunStandardTest() => LaunchTest(isDpiChecker: false);

    [RelayCommand]
    private void RunDpiChecker() => LaunchTest(isDpiChecker: true);

    private void LaunchTest(bool isDpiChecker)
    {
        if (IsBusy) return;
        IsBusy = true;

        var benchmarkVm = _serviceProvider.GetRequiredService<TestViewModel>();
        benchmarkVm.IsDpiCheckerMode = isDpiChecker;

        var benchmarkWin = new TestWindow(benchmarkVm);
        benchmarkWin.Owner = Application.Current?.MainWindow;
        benchmarkWin.ShowDialog();

        UpdateBestStrategyText();

        if (CanApplyBestStrategy())
        {
            _toastService.Show(
                "Подбор стратегии",
                $"Тестирование окончено. Рекомендован: {BestStrategyFound}",
                new SymbolIcon(SymbolRegular.Trophy24),
                timeoutSeconds: 4
            );
        }

        IsBusy = false;
    }

    [RelayCommand(CanExecute = nameof(CanApplyBestStrategy))]
    private async Task ApplyBestStrategyAsync()
    {
        if (!CanApplyBestStrategy() || _configService.Settings == null) return;
        _configService.Settings.SelectedStrategyName = BestStrategyFound;
        await _configService.SaveSettingsAsync();

        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        mainViewModel.RefreshStrategies();

        _toastService.Show(
            "Стратегия активирована",
            $"{BestStrategyFound} выбрана в качестве основной.",
            new SymbolIcon(SymbolRegular.CheckmarkCircle24),
            timeoutSeconds: 3
        );
    }

    [RelayCommand]
    private async Task RunDiagnosticsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        DiagnosticsResult = string.Empty;

        try
        {
            var results = await _tools.RunDiagnosticsAsync();
            DiagnosticsResult = string.Join(Environment.NewLine, results);

            _toastService.Show(
                "Диагностика",
                "Системная проверка успешно завершена.",
                new SymbolIcon(SymbolRegular.ShieldCheckmark24),
                timeoutSeconds: 3
            );
        }
        catch (Exception ex)
        {
            _toastService.Show(
                "Диагностика",
                $"Ошибка проверки: {ex.Message}",
                new SymbolIcon(SymbolRegular.DismissCircle24),
                timeoutSeconds: 4
            );
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ClearDiscordCacheAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        bool ok = await _tools.ClearDiscordCacheAsync();

        _toastService.Show(
            "Кэш Discord",
            ok ? "Кэш приложения и шлюзы RTC успешно очищены." : "Кэш уже пуст или Discord не найден.",
            ok ? new SymbolIcon(SymbolRegular.Delete24) : new SymbolIcon(SymbolRegular.Warning24),
            timeoutSeconds: 3
        );

        IsBusy = false;
    }

    [RelayCommand]
    private async Task UpdateIpSetListAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        bool ok = await _tools.UpdateIpSetListAsync();

        _toastService.Show(
            "Списки IPSet",
            ok ? "Свежий список диапазонов ipset-all.txt загружен." : "Не удалось скачать список IPSet.",
            ok ? new SymbolIcon(SymbolRegular.ArrowDownload24) : new SymbolIcon(SymbolRegular.DismissCircle24),
            timeoutSeconds: 3
        );

        IsBusy = false;
    }
}