using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;

namespace GojoDraw.Visualizer;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--smoke-demo", StringComparison.OrdinalIgnoreCase)))
        {
            _ = VisualizerLadderDemo.CreateFeatureReplay();
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

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
        Width = 1120;
        Height = 720;
        MinWidth = 720;
        MinHeight = 480;

        var canvas = new VisualizerCanvas { Document = VisualizerDocument.CreateInitial() };
        var refreshButton = new Button
        {
            Content = "Refresh",
            Padding = new Thickness(20, 12),
            MinWidth = 112,
            MinHeight = 44
        };
        var randomButton = new Button
        {
            Content = "Random",
            Padding = new Thickness(20, 12),
            MinWidth = 104,
            MinHeight = 44
        };
        var clearButton = new Button
        {
            Content = "Clear",
            Padding = new Thickness(20, 12),
            MinWidth = 96,
            MinHeight = 44
        };

        refreshButton.Click += (_, _) =>
        {
            canvas.Document = canvas.Document?.Resample() ?? VisualizerDocument.CreateInitial();
        };
        randomButton.Click += (_, _) =>
        {
            canvas.Document = VisualizerDocument.CreateRandom();
        };
        clearButton.Click += (_, _) =>
        {
            canvas.Document = VisualizerDocument.CreateEmpty();
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16),
            Spacing = 8
        };
        buttons.Children.Add(refreshButton);
        buttons.Children.Add(randomButton);
        buttons.Children.Add(clearButton);

        var root = new Grid();
        root.Children.Add(canvas);
        root.Children.Add(buttons);
        Content = root;
    }
}
