using System.Windows;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;
using XamlAnimatedGif;
using ZapretGate.ViewModels;

namespace ZapretGate.Views;

public partial class TestWindow : FluentWindow
{
    private readonly TestViewModel _viewModel;

    public TestWindow(TestViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.RequestClose += () => Dispatcher.Invoke(Close);
        Loaded += TestWindow_Loaded;
    }

    private async void TestWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var uri = new Uri("pack://application:,,,/ZapretGate;component/Resources/loading.gif", UriKind.Absolute);
            AnimationBehavior.SetRepeatBehavior(LoadingGifImage, RepeatBehavior.Forever);
            AnimationBehavior.SetSourceUri(LoadingGifImage, uri);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки GIF: {ex.Message}");
        }

        await _viewModel.StartTestAsync();
    }
}