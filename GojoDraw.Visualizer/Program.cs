using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace GojoDraw.Visualizer;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--smoke-demo", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var demo in VisualizerLadderDemo.CreateAll())
            {
                _ = demo.Structure;
            }

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

        var canvas = new VisualizerCanvas { Document = VisualizerDocument.CreateEmpty() };
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

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        buttons.Children.Add(refreshButton);
        buttons.Children.Add(randomButton);
        buttons.Children.Add(clearButton);
        var buttonChrome = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(14),
            Padding = new Thickness(6),
            CornerRadius = new CornerRadius(7),
            Background = new SolidColorBrush(Color.FromArgb(205, 255, 254, 250)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(88, 82, 88, 96)),
            BorderThickness = new Thickness(1),
            Child = buttons
        };

        void SetDocument(VisualizerDocument next)
        {
            canvas.Document = next;
        }

        void UpdateButtonVisibility()
        {
            buttonChrome.IsVisible = canvas.Document?.DemoPage is null;
        }

        refreshButton.Click += (_, _) =>
        {
            SetDocument(canvas.Document?.Resample() ?? VisualizerDocument.CreateEmpty());
        };
        randomButton.Click += (_, _) =>
        {
            SetDocument(VisualizerDocument.CreateRandom());
        };
        clearButton.Click += (_, _) =>
        {
            SetDocument(VisualizerDocument.CreateEmpty());
        };
        canvas.DocumentChanged += (_, _) => UpdateButtonVisibility();

        var root = new Grid();
        root.Children.Add(canvas);
        root.Children.Add(buttonChrome);
        UpdateButtonVisibility();
        Content = root;
    }
}
