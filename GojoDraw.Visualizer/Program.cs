using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;

namespace GojoDraw.Visualizer;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();
}

internal sealed class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "GojoDraw Visualizer";
        Width = 820;
        Height = 520;
        MinWidth = 420;
        MinHeight = 320;

        var canvas = new VisualizerCanvas { Sample = VisualizerSample.Create() };
        var refreshButton = new Button
        {
            Content = "Refresh",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(12),
            Padding = new Thickness(12, 6),
        };

        refreshButton.Click += (_, _) =>
        {
            canvas.Sample = canvas.Sample?.Resample() ?? VisualizerSample.Create();
        };

        var root = new Grid();
        root.Children.Add(canvas);
        root.Children.Add(refreshButton);
        Content = root;
    }
}
