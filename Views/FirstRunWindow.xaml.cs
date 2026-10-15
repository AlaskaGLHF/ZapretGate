using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ZapretGate.Services.Interfaces;
using ZapretGate.ViewModels;

namespace ZapretGate.Views;

public partial class FirstRunWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly IConfigurationService _configService;
    private readonly IStrategyDiscoveryService _discoveryService;

    public FirstRunWindow()
    {
        InitializeComponent();
        _configService = App.Services.GetRequiredService<IConfigurationService>();
        _discoveryService = App.Services.GetRequiredService<IStrategyDiscoveryService>();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private async void BtnBrowseFirstRun_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Выберите каталог с zapret-discord-youtube"
        };

        if (dialog.ShowDialog(this) == true)
        {
            var selectedPath = dialog.FolderName;
            TxtFirstRunStatus.Visibility = Visibility.Visible;

            if (_discoveryService.ValidateZapretDirectory(selectedPath))
            {
                _configService.Settings.ZapretDirectoryPath = selectedPath;
                await _configService.SaveSettingsAsync();

                var foundCount = _discoveryService.DiscoverStrategies(selectedPath).Count;
                TxtFirstRunStatus.Text = $"✓ Папка успешно найдена! Обнаружено стратегий: {foundCount}";
                TxtFirstRunStatus.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#34D399"));
                
                BtnNextStep.Visibility = Visibility.Visible;
            }
            else
            {
                TxtFirstRunStatus.Text = "✕ Ошибка: в выбранной папке отсутствуют bin/winws.exe или bin/WinDivert.dll";
                TxtFirstRunStatus.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F87171"));
                
                BtnNextStep.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void BtnNextStep_Click(object sender, RoutedEventArgs e)
    {
        Step1Panel.Visibility = Visibility.Collapsed;
        Step2Panel.Visibility = Visibility.Visible;
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void BtnRunTestFirstRun_Click(object sender, RoutedEventArgs e)
    {
        BtnRunTestFirstRun.IsEnabled = false;
        BtnContinueFirstRun.IsEnabled = false;
        
        try
        {
            var testVm = App.Services.GetRequiredService<TestViewModel>();
            testVm.IsDpiCheckerMode = false;

            var testWin = new TestWindow(testVm);
            testWin.Owner = this;
            testWin.ShowDialog();

            var mainVm = App.Services.GetRequiredService<MainViewModel>();
            mainVm.RefreshStrategies();
            mainVm.LoadFeaturesState();
        }
        finally
        {
            BtnRunTestFirstRun.IsEnabled = true;
            BtnContinueFirstRun.IsEnabled = true;
        }
    }

    private async void BtnContinueFirstRun_Click(object sender, RoutedEventArgs e)
    {
        _configService.Settings.IsFirstRunCompleted = true;
        await _configService.SaveSettingsAsync();

        var mainVm = App.Services.GetRequiredService<MainViewModel>();
        await mainVm.InitializeAsync();

        var mainWindow = App.Services.GetRequiredService<MainWindow>();
        App.Current.MainWindow = mainWindow;
        mainWindow.Show();

        Close();
    }
}