using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace GojoCAD.SketchViewer;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--smoke-scenes", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var scene in SketchSamples.CreateInitialScenes())
            {
                _ = scene.Structure;
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
        Title = "GojoCAD SketchViewer";
        Width = 1180;
        Height = 760;
        MinWidth = 860;
        MinHeight = 560;

        var document = SketchDocument.CreateInitial();
        var canvas = new SketchCanvas
        {
            Document = document
        };
        var sceneList = new ListBox
        {
            ItemsSource = document.Scenes.Select(scene => scene.Label).ToArray(),
            SelectedIndex = document.SelectedSceneIndex,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        };
        var refreshButton = CreateButton("Refresh");
        var nextButton = CreateButton("Next");

        var isUpdatingSceneList = false;
        void SetDocument(SketchDocument next)
        {
            document = next;
            canvas.Document = next;
            isUpdatingSceneList = true;
            try
            {
                sceneList.ItemsSource = next.Scenes.Select(scene => scene.Label).ToArray();
                sceneList.SelectedIndex = next.SelectedSceneIndex;
            }
            finally
            {
                isUpdatingSceneList = false;
            }
        }

        sceneList.SelectionChanged += (_, _) =>
        {
            if (isUpdatingSceneList)
            {
                return;
            }

            if (sceneList.SelectedIndex >= 0 && sceneList.SelectedIndex < document.Scenes.Count)
            {
                SetDocument(document.Select(sceneList.SelectedIndex));
            }
        };
        refreshButton.Click += (_, _) => SetDocument(document.Resample());
        nextButton.Click += (_, _) => SetDocument(document.Select((document.SelectedSceneIndex + 1) % document.Scenes.Count));

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 14, 0, 0)
        };
        buttonRow.Children.Add(refreshButton);
        buttonRow.Children.Add(nextButton);

        var side = new Border
        {
            Width = 260,
            Background = new SolidColorBrush(Color.FromRgb(246, 245, 240)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(65, 91, 97, 107)),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(16, 16, 12, 16),
            Child = new DockPanel()
        };
        var header = new TextBlock
        {
            Text = "Sketch Scenes",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(31, 37, 45)),
            Margin = new Thickness(0, 0, 0, 12)
        };
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(buttonRow, Dock.Bottom);
        ((DockPanel)side.Child).Children.Add(header);
        ((DockPanel)side.Child).Children.Add(buttonRow);
        ((DockPanel)side.Child).Children.Add(sceneList);

        var root = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        root.Children.Add(side);
        Grid.SetColumn(canvas, 1);
        root.Children.Add(canvas);
        Content = root;
    }

    private static Button CreateButton(string text) =>
        new()
        {
            Content = text,
            Padding = new Thickness(12, 5),
            MinWidth = 76,
            MinHeight = 30,
            Background = new SolidColorBrush(Color.FromArgb(235, 255, 254, 250)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 82, 88, 96)),
            BorderThickness = new Thickness(1)
        };
}
