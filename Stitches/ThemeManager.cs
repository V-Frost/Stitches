// ThemeManager.cs
using System.Windows.Media;

public static class ThemeManager
{
    // Default theme colors
    public static Brush BackgroundColor { get; set; } = Brushes.White;
    public static Brush ForegroundColor { get; set; } = Brushes.Black;
    public static Brush CanvasBackgroundColor { get; set; } = Brushes.White;
    public static Brush BorderColor { get; set; } = Brushes.Black;
    public static Brush HintColor { get; set; } = Brushes.Black;
    public static Brush TimerColor { get; set; } = (Brush)new BrushConverter().ConvertFromString("#3c99f1"); // Default timer color

    // Night mode colors
    private static readonly Brush NightBackgroundColor = (Brush)new BrushConverter().ConvertFromString("#171212");
    private static readonly Brush NightForegroundColor = Brushes.White;
    private static readonly Brush NightCanvasBackgroundColor = (Brush)new BrushConverter().ConvertFromString("#292424");
    private static readonly Brush NightBorderColor = (Brush)new BrushConverter().ConvertFromString("#113aac");
    private static readonly Brush NightHintColor = Brushes.White;
    private static readonly Brush NightTimerColor = (Brush)new BrushConverter().ConvertFromString("#ababb0"); // Night mode timer color

    public static bool IsNightMode { get; private set; }

    public static void SetNightMode(bool enable)
    {
        IsNightMode = enable;

        BackgroundColor = enable ? NightBackgroundColor : Brushes.White;
        ForegroundColor = enable ? NightForegroundColor : Brushes.Black;
        CanvasBackgroundColor = enable ? NightCanvasBackgroundColor : Brushes.White;
        BorderColor = enable ? NightBorderColor : Brushes.Black;
        HintColor = enable ? NightHintColor : Brushes.Black;
        TimerColor = enable ? NightTimerColor : (Brush)new BrushConverter().ConvertFromString("#3c99f1");
    }
}
