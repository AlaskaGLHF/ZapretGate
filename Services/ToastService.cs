using System.Collections.ObjectModel;
using Wpf.Ui.Controls;
using ZapretGate.Models;
using ZapretGate.Services.Interfaces;
using Application = System.Windows.Application;

namespace ZapretGate.Services;

public class ToastService : IToastService
{
    public ObservableCollection<ToastNotification> Notifications { get; } = [];

    public void Show(string title, string message, IconElement? icon = null, int timeoutSeconds = 3)
    {
        Application.Current?.Dispatcher.Invoke(async () =>
        {
            var item = new ToastNotification
            {
                
                Title = title,
                Message = message,
                Icon = icon
                
            };
            
            Notifications.Add(item);

            await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

            Notifications.Remove(item);
        });
    }
}