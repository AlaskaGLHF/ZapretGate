using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZapretGate.Services;
using ZapretGate.Services.Interfaces;

namespace ZapretGate.ViewModels;

public partial class TestViewModel : ObservableObject
{
    private readonly ZapretServiceTools _tools;
    private readonly IConfigurationService _configService;
    private readonly MainViewModel _mainViewModel;
    private CancellationTokenSource? _cts;

    public bool IsDpiCheckerMode { get; set; }

    [ObservableProperty]
    private string _currentStrategyName = "Инициализация...";

    [ObservableProperty]
    private int _overallProgress;

    [ObservableProperty]
    private string _stepCounterText = "0 из 0";

    [ObservableProperty]
    private string _progressPercentageText = "0%";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private string _bestStrategyName = string.Empty;

    public event Action? RequestClose;

    public TestViewModel(
        ZapretServiceTools tools,
        IConfigurationService configService,
        MainViewModel mainViewModel)
    {
        _tools = tools;
        _configService = configService;
        _mainViewModel = mainViewModel;
    }

    public async Task StartTestAsync()
    {
        IsRunning = true;
        IsCompleted = false;
        OverallProgress = 0;
        ProgressPercentageText = "0%";
        BestStrategyName = string.Empty;
        _cts = new CancellationTokenSource();

        var totalStrategies = _tools.GetAvailableStrategyNames().Count;
        int totalCount = Math.Max(totalStrategies, 1);

        StepCounterText = $"0 из {totalCount}";
        CurrentStrategyName = IsDpiCheckerMode ? "DPI Checker: подготовка..." : "Стандартный тест: подготовка...";

        var progress = new Progress<TestProgressReport>(report =>
        {
            CurrentStrategyName = report.CurrentConfig;
            StepCounterText = $"{report.CurrentIndex} из {report.TotalConfigs}";
            OverallProgress = Math.Clamp((int)report.ProgressPercentage, 0, 100);
            ProgressPercentageText = $"{report.ProgressPercentage:0}%";
        });

        try
        {
            string? winner = await Task.Run(() => _tools.RunTestAsync(IsDpiCheckerMode, progress, _cts.Token));

            if (!string.IsNullOrWhiteSpace(winner))
            {
                BestStrategyName = winner;

                if (_configService.Settings != null)
                {
                    _configService.Settings.RecommendedStrategyName = winner;
                    _configService.Settings.SelectedStrategyName = winner;
                    await _configService.SaveSettingsAsync();

                    _mainViewModel.RefreshStrategies();
                }
            }
            else
            {
                BestStrategyName = "Не удалось определить стратегию";
            }
        }
        catch (OperationCanceledException)
        {
            CurrentStrategyName = "Тестирование прервано.";
            BestStrategyName = "Тестирование отменено";
        }
        catch (Exception ex)
        {
            CurrentStrategyName = "Произошла ошибка при тестировании.";
            BestStrategyName = ex.Message;
        }
        finally
        {
            IsRunning = false;
            OverallProgress = 100;
            ProgressPercentageText = "100%";
            IsCompleted = true;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
        _tools.StopBenchmark();
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void ApplyAndClose()
    {
        RequestClose?.Invoke();
    }
}