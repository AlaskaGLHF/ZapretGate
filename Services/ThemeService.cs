using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using ZapretGate.Services.Interfaces;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Application = System.Windows.Application;

namespace ZapretGate.Services;

public class ThemeService : IThemeService
{
    private string _currentRequestedTheme = "Dark";

    public void ApplyTheme(string themeName)
    {
        // По умолчанию всегда используется Dark
        _currentRequestedTheme = string.IsNullOrWhiteSpace(themeName) ? "Dark" : themeName;

        string effectiveTheme = _currentRequestedTheme;
        bool isLight = effectiveTheme.Equals("Light", StringComparison.OrdinalIgnoreCase);
        
        ApplicationThemeManager.Apply(isLight ? ApplicationTheme.Light : ApplicationTheme.Dark);

        Color accentColor;
        Color accentHoverColor;
        Color windowBg;
        Color cardBg;
        Color cardStroke;
        Color textPrimary;
        Color textSecondary;

        Color checkBgChecked;
        Color checkBgHover;
        Color checkGlyph;
        Color checkBorder;

        switch (effectiveTheme)
        {
            case "Light":
                accentColor      = (Color)ColorConverter.ConvertFromString("#2563EB");
                accentHoverColor = (Color)ColorConverter.ConvertFromString("#1D4ED8");
                windowBg         = (Color)ColorConverter.ConvertFromString("#F1F5F9");
                cardBg           = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                cardStroke       = (Color)ColorConverter.ConvertFromString("#E2E8F0");
                textPrimary      = (Color)ColorConverter.ConvertFromString("#1E293B");
                textSecondary    = (Color)ColorConverter.ConvertFromString("#64748B");

                checkBgChecked   = accentColor;
                checkBgHover     = accentHoverColor;
                checkGlyph       = Colors.White;
                checkBorder      = cardStroke;
                break;

            case "Coffee":
                accentColor      = (Color)ColorConverter.ConvertFromString("#DDB892");
                accentHoverColor = (Color)ColorConverter.ConvertFromString("#E6CCB2");
                windowBg         = (Color)ColorConverter.ConvertFromString("#1A1412");
                cardBg           = (Color)ColorConverter.ConvertFromString("#261E1A");
                cardStroke       = (Color)ColorConverter.ConvertFromString("#3D302A");
                textPrimary      = (Color)ColorConverter.ConvertFromString("#EDE0D4");
                textSecondary    = (Color)ColorConverter.ConvertFromString("#B09E90");

                checkBgChecked   = Color.FromArgb(70, 221, 184, 146);
                checkBgHover     = Color.FromArgb(110, 221, 184, 146);
                checkGlyph       = (Color)ColorConverter.ConvertFromString("#EDE0D4");
                checkBorder      = (Color)ColorConverter.ConvertFromString("#7F5539");
                break;

            case "Dark Coffee":
                accentColor      = (Color)ColorConverter.ConvertFromString("#C68B59");
                accentHoverColor = (Color)ColorConverter.ConvertFromString("#D49A6A");
                windowBg         = (Color)ColorConverter.ConvertFromString("#110D0B");
                cardBg           = (Color)ColorConverter.ConvertFromString("#1A1411");
                cardStroke       = (Color)ColorConverter.ConvertFromString("#2E221C");
                textPrimary      = (Color)ColorConverter.ConvertFromString("#F5EBE1");
                textSecondary    = (Color)ColorConverter.ConvertFromString("#A38F81");

                checkBgChecked   = Color.FromArgb(45, 198, 139, 89);
                checkBgHover     = Color.FromArgb(85, 198, 139, 89);
                checkGlyph       = (Color)ColorConverter.ConvertFromString("#F5EBE1");
                checkBorder      = Color.FromArgb(160, 198, 139, 89);
                break;

            case "Cyberpunk":
                accentColor      = (Color)ColorConverter.ConvertFromString("#00F0FF");
                accentHoverColor = (Color)ColorConverter.ConvertFromString("#38F5FF");
                windowBg         = (Color)ColorConverter.ConvertFromString("#090A10");
                cardBg           = (Color)ColorConverter.ConvertFromString("#111320");
                cardStroke       = (Color)ColorConverter.ConvertFromString("#1D233A");
                textPrimary      = (Color)ColorConverter.ConvertFromString("#F0F6FC");
                textSecondary    = (Color)ColorConverter.ConvertFromString("#7E8B9B");

                checkBgChecked   = Color.FromArgb(50, 0, 240, 255);
                checkBgHover     = Color.FromArgb(90, 0, 240, 255);
                checkGlyph       = (Color)ColorConverter.ConvertFromString("#00F0FF");
                checkBorder      = (Color)ColorConverter.ConvertFromString("#00F0FF");
                break;

            case "Emerald":
                accentColor      = (Color)ColorConverter.ConvertFromString("#10B981");
                accentHoverColor = (Color)ColorConverter.ConvertFromString("#34D399");
                windowBg         = (Color)ColorConverter.ConvertFromString("#06100B");
                cardBg           = (Color)ColorConverter.ConvertFromString("#0B1B13");
                cardStroke       = (Color)ColorConverter.ConvertFromString("#143224");
                textPrimary      = (Color)ColorConverter.ConvertFromString("#ECFDF5");
                textSecondary    = (Color)ColorConverter.ConvertFromString("#6EE7B7");

                checkBgChecked   = Color.FromArgb(60, 16, 185, 129);
                checkBgHover     = Color.FromArgb(100, 16, 185, 129);
                checkGlyph       = (Color)ColorConverter.ConvertFromString("#A7F3D0");
                checkBorder      = (Color)ColorConverter.ConvertFromString("#10B981");
                break;

            case "Purple":
                accentColor      = (Color)ColorConverter.ConvertFromString("#A855F7");
                accentHoverColor = (Color)ColorConverter.ConvertFromString("#B975F9");
                windowBg         = (Color)ColorConverter.ConvertFromString("#0B0813");
                cardBg           = (Color)ColorConverter.ConvertFromString("#140F22");
                cardStroke       = (Color)ColorConverter.ConvertFromString("#261B40");
                textPrimary      = (Color)ColorConverter.ConvertFromString("#FAF5FF");
                textSecondary    = (Color)ColorConverter.ConvertFromString("#C084FC");

                checkBgChecked   = Color.FromArgb(55, 168, 85, 247);
                checkBgHover     = Color.FromArgb(95, 168, 85, 247);
                checkGlyph       = (Color)ColorConverter.ConvertFromString("#FAF5FF");
                checkBorder      = (Color)ColorConverter.ConvertFromString("#A855F7");
                break;

            case "Crimson":
                accentColor      = (Color)ColorConverter.ConvertFromString("#F43F5E");
                accentHoverColor = (Color)ColorConverter.ConvertFromString("#FB7185");
                windowBg         = (Color)ColorConverter.ConvertFromString("#120609");
                cardBg           = (Color)ColorConverter.ConvertFromString("#1E0A10");
                cardStroke       = (Color)ColorConverter.ConvertFromString("#39121E");
                textPrimary      = (Color)ColorConverter.ConvertFromString("#FFF1F2");
                textSecondary    = (Color)ColorConverter.ConvertFromString("#FDA4AF");

                checkBgChecked   = Color.FromArgb(55, 244, 63, 94);
                checkBgHover     = Color.FromArgb(95, 244, 63, 94);
                checkGlyph       = (Color)ColorConverter.ConvertFromString("#FFF1F2");
                checkBorder      = (Color)ColorConverter.ConvertFromString("#F43F5E");
                break;

            case "Dark":
            default:
                accentColor      = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                accentHoverColor = (Color)ColorConverter.ConvertFromString("#E4E4E7");
                windowBg         = (Color)ColorConverter.ConvertFromString("#0D0D0D");
                cardBg           = (Color)ColorConverter.ConvertFromString("#18181B");
                cardStroke       = (Color)ColorConverter.ConvertFromString("#27272A");
                textPrimary      = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                textSecondary    = (Color)ColorConverter.ConvertFromString("#A1A1AA");

                checkBgChecked   = accentColor;
                checkBgHover     = accentHoverColor;
                checkGlyph       = Colors.Black;
                checkBorder      = cardStroke;
                break;
        }
        
        ApplicationAccentColorManager.Apply(accentColor);
        
        ApplyBrushesInternal(
            accentColor, accentHoverColor, windowBg, cardBg, cardStroke, 
            textPrimary, textSecondary, checkBgChecked, checkBgHover, checkGlyph, checkBorder);
        
        Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            ApplyBrushesInternal(
                accentColor, accentHoverColor, windowBg, cardBg, cardStroke, 
                textPrimary, textSecondary, checkBgChecked, checkBgHover, checkGlyph, checkBorder);
        });
    }

    private static void ApplyBrushesInternal(
        Color accentColor,
        Color accentHoverColor,
        Color windowBg,
        Color cardBg,
        Color cardStroke,
        Color textPrimary,
        Color textSecondary,
        Color checkBgChecked,
        Color checkBgHover,
        Color checkGlyph,
        Color checkBorder)
    {
        SetBrush("AccentFillColorDefaultBrush", accentColor);
        SetBrush("AccentFillColorSecondaryBrush", accentHoverColor);
        SetBrush("AccentFillColorTertiaryBrush", accentHoverColor);
        SetBrush("AccentButtonBackground", accentColor);
        SetBrush("AccentButtonBackgroundPointerOver", accentHoverColor);
        SetBrush("AccentButtonBackgroundPressed", accentHoverColor);
        
        SetBrush("ApplicationBackgroundBrush", windowBg);
        SetBrush("WindowBackground", windowBg);
        SetBrush("SolidBackgroundFillColorBaseBrush", windowBg);
        SetBrush("SolidBackgroundFillColorSecondaryBrush", windowBg);
        SetBrush("NavigationViewDefaultPaneBackground", windowBg);

        SetBrush("CardBackgroundFillColorDefaultBrush", cardBg);
        SetBrush("CardBackgroundFillColorSecondaryBrush", cardBg);
        SetBrush("CardStrokeColorDefaultBrush", cardStroke);
        SetBrush("ControlFillColorDefaultBrush", cardBg);
        SetBrush("ControlStrokeColorDefaultBrush", cardStroke);
        SetBrush("ControlElevationBorderBrush", cardStroke);

        SetBrush("TextFillColorPrimaryBrush", textPrimary);
        SetBrush("TextFillColorSecondaryBrush", textSecondary);
        SetBrush("TextFillColorTertiaryBrush", Color.FromArgb(150, textSecondary.R, textSecondary.G, textSecondary.B));
        SetBrush("AccentTextFillColorPrimaryBrush", accentColor);

        SetBrush("AccentFillColorDefaultBrush", accentColor);
        SetBrush("AccentFillColorSecondaryBrush", accentHoverColor);
        SetBrush("AccentFillColorTertiaryBrush", accentHoverColor);
        SetBrush("ProgressBarForeground", accentColor);

        SetBrush("ControlStrongFillColorDefaultBrush", accentColor);
        SetBrush("ControlStrongFillColorPointerOverBrush", accentHoverColor);
        SetBrush("ControlStrongFillColorPressedBrush", accentHoverColor);
        SetBrush("NavigationViewItemSelectionIndicatorForeground", accentColor);
        
        SetRawColor("SystemAccentColor", accentColor);
        SetRawColor("SystemAccentColorPrimary", accentColor);
        SetRawColor("SystemAccentColorSecondary", accentHoverColor);
        SetRawColor("SystemAccentColorLight1", accentHoverColor);
        SetRawColor("SystemAccentColorLight2", accentHoverColor);
        SetRawColor("SystemAccentColorLight3", accentHoverColor);
        SetRawColor("SystemAccentColorDark1", accentColor);
        SetRawColor("SystemAccentColorDark2", accentColor);
        SetRawColor("SystemAccentColorDark3", accentColor);

        SetBrush("SystemAccentColorPrimaryBrush", accentColor);
        SetBrush("SystemAccentColorSecondaryBrush", accentHoverColor);
        SetBrush("SystemControlHighlightAccentBrush", accentColor);

        SetBrush("CheckBoxCheckBackgroundFillChecked", checkBgChecked);
        SetBrush("CheckBoxCheckBackgroundFillCheckedPointerOver", checkBgHover);
        SetBrush("CheckBoxCheckBackgroundFillCheckedPressed", checkBgHover);
        SetBrush("CheckBoxCheckBackgroundStrokeChecked", checkBorder);
        SetBrush("CheckBoxCheckBackgroundStrokeCheckedPointerOver", checkBorder);
        SetBrush("CheckBoxCheckGlyphForegroundChecked", checkGlyph);
        SetBrush("CheckBoxCheckGlyphForegroundCheckedPointerOver", checkGlyph);

        SetBrush("ToggleSwitchFillOn", checkBgChecked);
        SetBrush("ToggleSwitchFillOnPointerOver", checkBgHover);
        SetBrush("ToggleSwitchStrokeOn", checkBorder);
        SetBrush("ToggleSwitchKnobFillOn", checkGlyph);

        SetBrush("SnackbarBackground", cardBg);
        SetBrush("SnackbarBorderBrush", cardStroke);
        SetBrush("SnackbarForeground", textPrimary);
        SetBrush("SnackbarAccentBrush", accentColor);
        SetBrush("SnackbarCardBackground", cardBg);
        SetBrush("SystemControlBackgroundChromeMediumLowBrush", cardBg);
        
        if (Application.Current?.MainWindow != null)
        {
            var winBgBrush = new SolidColorBrush(windowBg);
            winBgBrush.Freeze();
            Application.Current.MainWindow.Background = winBgBrush;
            
        }
    }

    private static void SetBrush(string key, Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();

        if (Application.Current?.Resources != null)
            Application.Current.Resources[key] = brush;

        if (Application.Current?.MainWindow?.Resources != null)
            Application.Current.MainWindow.Resources[key] = brush;
    }

    private static void SetRawColor(string key, Color color)
    {
        if (Application.Current?.Resources != null)
            Application.Current.Resources[key] = color;

        if (Application.Current?.MainWindow?.Resources != null)
            Application.Current.MainWindow.Resources[key] = color;
    }
}