using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Controls;
using ZapretGate.Services.Interfaces;
using ZapretGate.ViewModels;
using ZapretGate.Views;
using Application = System.Windows.Application;

namespace ZapretGate;

public partial class MainWindow : FluentWindow
{
    private const int WmTrayCallback = 0x8000 + 100;
    private const int WmRbuttonup = 0x0205;
    private const int WmLbuttonup = 0x0202;
    private const int NimAdd = 0x00000000;
    private const int NimDelete = 0x00000002;
    private const int NifMessage = 0x00000001;
    private const int NifIcon = 0x00000002;
    private const int NifTip = 0x00000004;

    [DllImport("psapi.dll")]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NotifyIconData lpData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
    }

    private readonly IConfigurationService _configService;
    private readonly IStrategyDiscoveryService _discoveryService;
    private IntPtr _hIcon = IntPtr.Zero;
    private bool _isTrayAdded;
    private bool _isExplicitExit;

    public MainViewModel MainVm => App.Services.GetRequiredService<MainViewModel>();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = this;
        RootNavigation.SetServiceProvider(App.Services);

        _configService = App.Services.GetRequiredService<IConfigurationService>();
        _discoveryService = App.Services.GetRequiredService<IStrategyDiscoveryService>();
        var toastService = App.Services.GetRequiredService<IToastService>();
        ToastContainer.ItemsSource = toastService.Notifications;

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeNativeTray();
    
        var themeService = App.Services.GetRequiredService<IThemeService>();
        themeService.ApplyTheme(_configService.Settings.ThemeMode);

        RootNavigation.Visibility = Visibility.Visible;
        RootNavigation.Navigate(typeof(Views.Pages.DashboardPage));
    
        var mainVm = App.Services.GetRequiredService<MainViewModel>();
        mainVm.RefreshStrategies();
        mainVm.LoadFeaturesState();
    }

    #region Нативный трей и оптимизация памяти

    private void InitializeNativeTray()
    {
        var helper = new WindowInteropHelper(this);
        var source = HwndSource.FromHwnd(helper.Handle);
        source?.AddHook(WndProc);

        try
        {
            var iconUri = new Uri("pack://application:,,,/ZapretGate;component/Resources/app.ico", UriKind.Absolute);
            var res = Application.GetResourceStream(iconUri);
            if (res != null)
            {
                using var stream = res.Stream;
                using var originalIcon = new System.Drawing.Icon(stream, new System.Drawing.Size(16, 16));
                using var bmp = originalIcon.ToBitmap();
                _hIcon = bmp.GetHicon();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Tray] Ошибка создания иконки трея: {ex.Message}");
            _hIcon = IntPtr.Zero;
        }

        if (_hIcon == IntPtr.Zero)
        {
            try
            {
                using var procIcon = System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
                if (procIcon != null)
                {
                    using var bmp = procIcon.ToBitmap();
                    _hIcon = bmp.GetHicon();
                }
            }
            catch { }
        }

        var nid = new NotifyIconData
        {
            cbSize = Marshal.SizeOf<NotifyIconData>(),
            hWnd = helper.Handle,
            uID = 1,
            uFlags = NifMessage | NifTip | (_hIcon != IntPtr.Zero ? NifIcon : 0),
            uCallbackMessage = WmTrayCallback,
            hIcon = _hIcon,
            szTip = "ZapretGate"
        };

        _isTrayAdded = Shell_NotifyIcon(NimAdd, ref nid);
    }

    private void RemoveNativeTray()
    {
        if (_isTrayAdded)
        {
            var helper = new WindowInteropHelper(this);
            var nid = new NotifyIconData
            {
                cbSize = Marshal.SizeOf<NotifyIconData>(),
                hWnd = helper.Handle,
                uID = 1
            };
            Shell_NotifyIcon(NimDelete, ref nid);
            _isTrayAdded = false;
        }

        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmTrayCallback)
        {
            var mouseMsg = lParam.ToInt32();
            if (mouseMsg == WmLbuttonup)
            {
                ShowFromTray();
                handled = true;
            }
            else if (mouseMsg == WmRbuttonup)
            {
                ShowContextMenu();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private void ShowContextMenu()
    {
        var contextMenu = new ContextMenu();

        var openItem = new System.Windows.Controls.MenuItem 
        { 
            Header = "Открыть ZapretGate", 
            FontWeight = FontWeights.Bold 
        };
        openItem.Click += (_, _) => ShowFromTray();

        var exitItem = new System.Windows.Controls.MenuItem 
        { 
            Header = "Выйти" 
        };
        exitItem.Click += (_, _) =>
        {
            _isExplicitExit = true;
            Close();
        };

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);

        contextMenu.IsOpen = true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitExit)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        RemoveNativeTray();
        base.OnClosing(e);
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;

        try
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
        }
        catch { }
    }

    private void ShowFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    #endregion
}