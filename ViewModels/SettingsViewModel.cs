using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Wpf.Ui.Controls;
using ZapretGate.Services;
using ZapretGate.Services.Interfaces;

namespace ZapretGate.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;
    private readonly IStrategyDiscoveryService _discoveryService;
    private readonly IThemeService _themeService;
    private readonly IAutoStartService _autoStartService;
    private readonly ZapretServiceTools _tools;
    private readonly IToastService _toastService;
    private readonly MainViewModel _mainViewModel;
    private bool _isInitializing = true;

    [ObservableProperty]
    private string _zapretPath = string.Empty;

    // Отдельное свойство только для статуса валидности папки
    [ObservableProperty]
    private string _pathStatusMessage = string.Empty;

    [ObservableProperty]
    private string _zapretVersionInfo = "Определение версии...";

    [ObservableProperty]
    private bool _isPathValid;

    [ObservableProperty]
    private int _foundStrategiesCount;

    [ObservableProperty]
    private bool _isAppAutoStart;

    [ObservableProperty]
    private bool _isAutoConnect;

    [ObservableProperty]
    private List<string> _availableThemes = new()
    {
        "Dark",
        "Light",
        "Coffee",
        "Dark Coffee",
        "Emerald",
        "Purple",
        "Crimson",
        "Cyberpunk"
    };

    [ObservableProperty]
    private string _selectedTheme = "Dark";

    public SettingsViewModel(
        IConfigurationService configService,
        IStrategyDiscoveryService discoveryService,
        IThemeService themeService,
        IAutoStartService autoStartService,
        ZapretServiceTools tools,
        IToastService toastService,
        MainViewModel mainViewModel)
    {
        _configService = configService;
        _discoveryService = discoveryService;
        _themeService = themeService;
        _autoStartService = autoStartService;
        _tools = tools;
        _toastService = toastService;
        _mainViewModel = mainViewModel;

        _mainViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CanChangeSettings))
            {
                UpdateZapretFilesCommand.NotifyCanExecuteChanged();
            }
        };

        LoadSettings();
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        ZapretPath = _configService.Settings.ZapretDirectoryPath ?? string.Empty;
        SelectedTheme = string.IsNullOrWhiteSpace(_configService.Settings.ThemeMode) 
            ? "Dark" 
            : _configService.Settings.ThemeMode;
        IsAutoConnect = _configService.Settings.AutoConnectOnStartup;
        IsAppAutoStart = _autoStartService.IsAutoStartEnabled();

        ValidatePath();
        UpdateVersionInfo();

        _isInitializing = false;
    }

    private void UpdateVersionInfo()
    {
        ZapretVersionInfo = _tools.GetZapretVersion();
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        SelectFolderDialog();
    }

    public bool SelectFolderDialog()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Выберите корневую папку zapret-discord-youtube"
        };

        if (dialog.ShowDialog() == true)
        {
            ZapretPath = dialog.FolderName;
            return IsPathValid;
        }
        return false;
    }

    private bool CanUpdateZapret()
    {
        return _mainViewModel.CanChangeSettings;
    }

    [RelayCommand(CanExecute = nameof(CanUpdateZapret))]
    private async Task UpdateZapretFilesAsync()
    {
        _toastService.Show(
            "Обновление Zapret",
            "Загрузка актуальной версии с GitHub...",
            new SymbolIcon(SymbolRegular.ArrowDownload24),
            timeoutSeconds: 3
        );

        var progress = new Progress<string>(msg =>
        { });

        bool success = await _tools.UpdateZapretRepositoryAsync(progress);

        _toastService.Show(
            "Обновление Zapret",
            success ? "Репозиторий успешно обновлен до актуальной версии." : "Не удалось обновить файлы zapret. Проверьте подключение к интернету или отключите VPN.",
            success ? new SymbolIcon(SymbolRegular.CheckmarkCircle24) : new SymbolIcon(SymbolRegular.DismissCircle24),
            timeoutSeconds: 4
        );

        UpdateVersionInfo();
        ValidatePath();
    }

    async partial void OnZapretPathChanged(string value)
    {
        ValidatePath();
        UpdateVersionInfo();

        if (_isInitializing) return;

        if (IsPathValid)
        {
            _configService.Settings.ZapretDirectoryPath = value;
            await AutoSaveAsync();
        }
    }

    async partial void OnIsAutoConnectChanged(bool value)
    {
        if (value && !IsAppAutoStart)
        {
            IsAppAutoStart = true;
        }

        if (_isInitializing) return;

        _configService.Settings.AutoConnectOnStartup = value;
        await AutoSaveAsync();

        _toastService.Show(
            "Настройки",
            value ? "Ультра-автозапуск включен." : "Ультра-автозапуск отключен.",
            new SymbolIcon(SymbolRegular.Play24),
            timeoutSeconds: 2
        );
    }

    async partial void OnIsAppAutoStartChanged(bool value)
    {
        if (!value && IsAutoConnect)
        {
            IsAutoConnect = false;
        }

        if (_isInitializing) return;

        _autoStartService.SetAutoStart(value);
        await AutoSaveAsync();

        _toastService.Show(
            "Настройки",
            value ? "Автозапуск приложения включен." : "Автозапуск приложения отключен.",
            new SymbolIcon(SymbolRegular.Rocket24),
            timeoutSeconds: 2
        );
    }

    async partial void OnSelectedThemeChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        ApplyCurrentTheme();

        if (_isInitializing) return;

        _configService.Settings.ThemeMode = value;
        await AutoSaveAsync();
    }

    public void ApplyCurrentTheme()
    {
        _themeService.ApplyTheme(SelectedTheme);
    }

    private async Task AutoSaveAsync()
    {
        try
        {
            await _configService.SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            PathStatusMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }

    private void ValidatePath()
    {
        IsPathValid = _discoveryService.ValidateZapretDirectory(ZapretPath);
        if (IsPathValid)
        {
            FoundStrategiesCount = _discoveryService.DiscoverStrategies(ZapretPath).Count;
            PathStatusMessage = $"Папка валидна. Обнаружено стратегий: {FoundStrategiesCount}";
        }
        else
        {
            FoundStrategiesCount = 0;
            PathStatusMessage = "В папке отсутствуют bin/winws.exe или bin/WinDivert.dll";
        }
    }
}