using System.Collections.ObjectModel;
using Wpf.Ui.Controls;
using ZapretGate.Models;

namespace ZapretGate.Services.Interfaces;

public interface IToastService
{
    
    ObservableCollection<ToastNotification> Notifications { get; }
    void Show(string title, string message, IconElement? icon = null, int timeoutSeconds = 3);
    
}