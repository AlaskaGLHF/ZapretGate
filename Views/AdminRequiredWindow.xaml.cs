using System.Windows;
using ZapretGate.Services;
using Application = System.Windows.Application;

namespace ZapretGate.Views;

public partial class AdminRequiredWindow
{
    public AdminRequiredWindow()
    {
        InitializeComponent();
    }

    private void BtnRestart_Click(object sender, RoutedEventArgs e)
    {
        AdminHelper.RestartAsAdmin();
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}