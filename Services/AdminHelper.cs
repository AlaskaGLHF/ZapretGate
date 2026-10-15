using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace ZapretGate.Services;

public static class AdminHelper
{

    // Проверяет, запущен ли текущий процесс с правами администратора
    public static bool IsRunAsAdmin()
    {
        
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
        
    }

    // Перезапускает текущее приложение с запросом на повышение прав
    public static void RestartAsAdmin()
    {
        
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            
            MessageBox.Show("Не удалось определить путь к исполняемому файлу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
            
        }

        var startInfo = new ProcessStartInfo
        {
            
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas"
            
        };

        try
        {
            
            Process.Start(startInfo);
            Application.Current.Shutdown();
            
        }
        catch (System.ComponentModel.Win32Exception)
        {
            
            MessageBox.Show(
                "Приложению необходимы права администратора для работы Zapret",
                "Отказано в доступе",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            
        }
    }
}