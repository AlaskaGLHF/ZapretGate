using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui;
using ZapretGate.Services;
using ZapretGate.Services.Interfaces;
using ZapretGate.ViewModels;
using ZapretGate.Views.Pages;
using IThemeService = ZapretGate.Services.Interfaces.IThemeService;
using ThemeService = ZapretGate.Services.ThemeService;
using Application = System.Windows.Application;

namespace ZapretGate;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Сервисы
        services.AddSingleton<IConfigurationService, JsonConfigurationService>();
        services.AddSingleton<IStrategyDiscoveryService, StrategyDiscoveryService>();
        services.AddSingleton<IProcessManagerService, ProcessManagerService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IAutoStartService, AutoStartService>();
        services.AddSingleton<ZapretServiceTools>();
        services.AddSingleton<IToastService, ToastService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ServiceViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddTransient<TestViewModel>();

        // Страницы
        services.AddTransient<DashboardPage>();
        services.AddTransient<ServicePage>();
        services.AddTransient<SettingsPage>();
        
        services.AddSingleton<ISnackbarService, SnackbarService>();

        // Главное окно
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!AdminHelper.IsRunAsAdmin())
        {
            var adminWarningWindow = new Views.AdminRequiredWindow();
            adminWarningWindow.Show();
            return;
        }
        
        var configService = Services.GetRequiredService<IConfigurationService>();
        await configService.LoadSettingsAsync();
        
        var themeService = Services.GetRequiredService<IThemeService>();
        themeService.ApplyTheme(configService.Settings.ThemeMode);

        var discoveryService = Services.GetRequiredService<IStrategyDiscoveryService>();
        var currentPath = configService.Settings.ZapretDirectoryPath;
        var isPathValid = !string.IsNullOrWhiteSpace(currentPath) && discoveryService.ValidateZapretDirectory(currentPath);
        
        if (!configService.Settings.IsFirstRunCompleted || !isPathValid)
        {
            var firstRunWindow = new Views.FirstRunWindow();
            
            Current.MainWindow = firstRunWindow;
            firstRunWindow.Show();
        }
        else
        {
            var mainVm = Services.GetRequiredService<MainViewModel>();
            await mainVm.InitializeAsync();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            Current.MainWindow = mainWindow;
            mainWindow.Show();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (Services != null)
        {
            var processManager = Services.GetService<IProcessManagerService>();
            if (processManager != null)
            {
                await processManager.StopAsync();
            }
        }

        base.OnExit(e);
    }
}