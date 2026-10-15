using System.Windows.Media;
using Wpf.Ui.Controls;

namespace ZapretGate.Models;

public class ToastNotification
{
    
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IconElement? Icon { get; init; }
    
    
}