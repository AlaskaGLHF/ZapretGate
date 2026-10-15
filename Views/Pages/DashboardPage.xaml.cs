using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ZapretGate.ViewModels;

namespace ZapretGate.Views.Pages;

public partial class DashboardPage : Page
{
    private readonly MainViewModel _viewModel;

    public DashboardPage()
    {
        
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;
        
        Loaded += (_, _) =>
        {
            
            _viewModel.RefreshStrategies();
            
            _viewModel.LoadFeaturesState();
            
        };
        
    }
    
    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}