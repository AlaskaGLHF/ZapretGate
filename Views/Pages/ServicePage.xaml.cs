using System.Windows;
using System.Windows.Controls;
using ZapretGate.ViewModels;

namespace ZapretGate.Views.Pages;

public partial class ServicePage : Page
{
    public ServicePage(ServiceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += ServicePage_Loaded;
    }

    private void ServicePage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ServiceViewModel vm)
            vm.LoadCurrentFeaturesState();
    }
}