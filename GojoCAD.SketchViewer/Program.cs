using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Gojo.Core;
using Gojo.Structures;

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
                ValidateGeneratedScene(scene);
            }

            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();

    private static void ValidateGeneratedScene(SketchScene scene)
    {
        if (scene.Structure is not Relation ladder ||
            !StructuralLadderSurface.TryReadRungPath(ladder, out var rungs) ||
            rungs.Length == 0)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not read as a structural ladder.");
        }

        if (!StructuralLadderSurface.TryReadTimeline(ladder, out var timeline) ||
            !TimeStepSequence.TryRead(timeline, out _, out _, out var steps))
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' did not expose a readable timeline.");
        }

        if (steps.Length != rungs.Length - 1)
        {
            throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a timeline/rung count mismatch.");
        }

        foreach (var step in steps)
        {
            if (!StructuralLadderSurface.TryReadTransformStep(
                    step,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var application,
                    out _,
                    out _) ||
                !TransformSurface.TryReadApplication(
                    application,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var patch,
                    out _,
                    out _,
                    out _))
            {
                throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a non-transform ladder step.");
            }

            if (!TransformSurface.TryReadPatch(
                    patch,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a transform step without a readable patch.");
            }

            if (!TransformSurface.TryCreateOperationOutputRead(
                    new Carrier(),
                    application,
                    Relation.Point(Carrier.Unknown, 0),
                    Relation.Point(Carrier.Unknown, 0),
                    out var operationOutput) ||
                !TransformSurface.TryReadOperationOutput(
                    operationOutput,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                throw new InvalidOperationException($"Sketch scene '{scene.Label}' has a transform step without a readable operation output.");
            }
        }
    }
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
        var nextButton = CreateButton("Next");
        var pointsButton = CreateButton(document.ShowConstructionPoints ? "Points On" : "Points Off");
        var rungLabel = new TextBlock
        {
            FontSize = 12.5,
            Foreground = new SolidColorBrush(Color.FromRgb(57, 64, 74)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 8)
        };
        var rungSlider = new LadderSliderControl
        {
            Maximum = Math.Max(0, document.SelectedScene.RungCount - 1),
            Value = document.CurrentRungIndex,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(6, 0, 0, 0)
        };

        var isUpdatingSceneList = false;
        var isUpdatingRungSlider = false;
        void UpdateRungControls(SketchDocument next)
        {
            isUpdatingRungSlider = true;
            try
            {
                rungSlider.SetRange(Math.Max(0, next.SelectedScene.RungCount - 1), next.CurrentRungIndex);
                rungLabel.Text = $"{next.CurrentRungIndex + 1}/{next.SelectedScene.RungCount}: {next.SelectedScene.ReadRungLabel(next.CurrentRungIndex)}";
            }
            finally
            {
                isUpdatingRungSlider = false;
            }
        }

        void SetDocument(SketchDocument next)
        {
            document = next;
            canvas.Document = next;
            pointsButton.Content = next.ShowConstructionPoints ? "Points On" : "Points Off";
            UpdateRungControls(next);
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
        nextButton.Click += (_, _) => SetDocument(document.Select((document.SelectedSceneIndex + 1) % document.Scenes.Count));
        pointsButton.Click += (_, _) => SetDocument(document.ToggleConstructionPoints());
        rungSlider.ValueChanged += nextRung =>
        {
            if (!isUpdatingRungSlider)
            {
                SetDocument(document.SelectRung(nextRung));
            }
        };

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 14, 0, 0)
        };
        buttonRow.Children.Add(nextButton);
        buttonRow.Children.Add(pointsButton);
        var rungPanel = new StackPanel
        {
            Margin = new Thickness(0, 20, 0, 14)
        };
        rungPanel.Children.Add(new TextBlock
        {
            Text = "Drawing Ladder",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(31, 37, 45))
        });
        rungPanel.Children.Add(rungLabel);
        rungPanel.Children.Add(rungSlider);
        UpdateRungControls(document);

        var header = new TextBlock
        {
            Text = "Sketch Scenes",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(31, 37, 45)),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var sideLayout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };
        Grid.SetRow(header, 0);
        Grid.SetRow(rungPanel, 1);
        Grid.SetRow(sceneList, 2);
        Grid.SetRow(buttonRow, 3);
        sideLayout.Children.Add(header);
        sideLayout.Children.Add(rungPanel);
        sideLayout.Children.Add(sceneList);
        sideLayout.Children.Add(buttonRow);

        var side = new Border
        {
            Width = 260,
            Background = new SolidColorBrush(Color.FromRgb(246, 245, 240)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(65, 91, 97, 107)),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(16, 16, 12, 16),
            Child = sideLayout
        };
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
