using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZapretGate.Services;

public static class WindowHider
{
    private const int SW_HIDE = 0;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static void HideProcessWindows(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var proc in processes)
            {
                if (proc.MainWindowHandle != IntPtr.Zero)
                {
                    
                    ShowWindow(proc.MainWindowHandle, SW_HIDE);
                    
                }
            }
        }
        catch
        {
            // ignored
        }
    }
}