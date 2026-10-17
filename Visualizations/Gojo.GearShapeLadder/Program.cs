using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using SkiaSharp;

namespace Gojo.GearShapeLadder;

internal static class Program
{
#if GEAR_SHAPE_LADDER_DESKTOP
    [STAThread]
    public static void Main(string[] args)
    {
        if (ReadStringArg(args, "--smoke-render=") is { Length: > 0 } path)
        {
            var stage = Math.Clamp(
                ReadIntArg(args, "--stage=") ?? GearShapeGeometry.InvoluteToothStage,
                0,
                GearShapeGeometry.StageCount - 1);
            var driver = Math.Clamp(ReadIntArg(args, "--driver=") ?? 24, GearShapeLadderModel.MinimumTeeth, GearShapeLadderModel.MaximumTeeth);
            var driven = Math.Clamp(ReadIntArg(args, "--driven=") ?? 16, GearShapeLadderModel.MinimumTeeth, GearShapeLadderModel.MaximumTeeth);
            SmokeRender(path, stage, driver, driven);
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
#endif

    public static AppBuilder BuildAvaloniaApp() =>
#if GEAR_SHAPE_LADDER_DESKTOP
        AppBuilder.Configure<App>().UsePlatformDetect();
#else
        AppBuilder.Configure<App>();
#endif

#if GEAR_SHAPE_LADDER_DESKTOP
    private static void SmokeRender(string path, int stage, int driverTeeth, int drivenTeeth)
    {
        var model = new GearShapeLadderModel(driverTeeth, drivenTeeth, 37);
        using var bitmap = new SKBitmap(1280, 820);
        using var canvas = new SKCanvas(bitmap);
        GearShapeRenderer.Draw(canvas, new SKRect(0, 0, bitmap.Width, bitmap.Height), model, stage);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 96);
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var stream = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        data.SaveTo(stream);
        Console.WriteLine($"Rendered gear ladder rung {stage + 1} to {fullPath}");
    }

    private static string? ReadStringArg(IEnumerable<string> args, string prefix) =>
        args.FirstOrDefault(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?[prefix.Length..];

    private static int? ReadIntArg(IEnumerable<string> args, string prefix) =>
        int.TryParse(ReadStringArg(args, prefix), out var value) ? value : null;
#endif
}

internal sealed class App : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = MainWindow.CreateContent();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class MainWindow : Window
{
    private static readonly string[] StageNames =
    [
        "One sized circle",
        "Two sized circles",
        "Coupled pitch circles",
        "Triangle teeth",
        "Flat-tipped teeth",
        "Involute vector teeth"
    ];

    public MainWindow()
    {
        Title = "Gojo Gear Shape Ladder";
        Width = 1220;
        Height = 790;
        MinWidth = 920;
        MinHeight = 650;
        Background = new SolidColorBrush(Color.FromRgb(248, 247, 243));
        Content = CreateContent();
    }

    internal static Control CreateContent()
    {
        var currentStage = 0;
        var model = new GearShapeLadderModel(maximumStage: currentStage);
        var paused = false;
        var tickRate = 24d;
        var tickRemainder = 0d;
        var stopwatch = Stopwatch.StartNew();

        var canvas = new GearShapeCanvas { Model = model, Stage = currentStage };
        var rungLabel = Text("1/6: One sized circle", 13, FontWeight.SemiBold);
        rungLabel.TextWrapping = TextWrapping.Wrap;
        var motionLabel = Text(string.Empty, 12);
        motionLabel.TextWrapping = TextWrapping.Wrap;
        var ratioLabel = Text(string.Empty, 12);
        ratioLabel.TextWrapping = TextWrapping.Wrap;

        var ladder = new GearLadderSlider
        {
            Maximum = GearShapeGeometry.StageCount - 1,
            Value = currentStage,
            Margin = new Thickness(4, 0, 0, 2)
        };

        var pauseButton = Button("Pause");
        var resetButton = Button("Reset motion");
        var driverSlider = Slider(GearShapeLadderModel.MinimumTeeth, GearShapeLadderModel.MaximumTeeth, model.DriverTeeth);
        var drivenSlider = Slider(GearShapeLadderModel.MinimumTeeth, GearShapeLadderModel.MaximumTeeth, model.DrivenTeeth);
        var speedSlider = Slider(0, 80, tickRate);
        var driverLabel = Text(string.Empty, 12.5, FontWeight.SemiBold);
        var drivenLabel = Text(string.Empty, 12.5, FontWeight.SemiBold);
        var speedLabel = Text(string.Empty, 12.5, FontWeight.SemiBold);

        void RefreshLabels()
        {
            var motion = model.ReadMotion();
            var hasSecondCircle = currentStage >= GearShapeGeometry.TwoCircleStage;
            var hasMotion = currentStage >= GearShapeGeometry.CoupledCircleStage;
            var hasTeeth = currentStage >= GearShapeGeometry.TriangleToothStage;
            rungLabel.Text = $"{currentStage + 1}/{GearShapeGeometry.StageCount}: {StageNames[currentStage]}";
            driverLabel.Text = hasTeeth
                ? $"Driver teeth  {model.DriverTeeth}"
                : currentStage == GearShapeGeometry.SingleCircleStage
                    ? $"Circle size  {model.DriverTeeth}"
                    : $"Left circle size  {model.DriverTeeth}";
            drivenLabel.Text = hasTeeth
                ? $"Driven teeth  {model.DrivenTeeth}"
                : $"Right circle size  {model.DrivenTeeth}";
            speedLabel.Text = $"Turning rate  {tickRate:0} steps/s";
            motionLabel.Text = $"motion {motion.SourceTicks}  ->  driver {motion.DriverTicks}  ->  driven {motion.DrivenTicks}";
            ratioLabel.Text = hasTeeth
                ? $"8 ticks/tooth · angular ratio {model.DrivenTeeth}:{model.DriverTeeth} · opposed perimeter read"
                : $"size ratio {model.DrivenTeeth}:{model.DriverTeeth} · opposed circular projection";

            drivenLabel.IsVisible = hasSecondCircle;
            drivenSlider.IsVisible = hasSecondCircle;
            speedLabel.IsVisible = hasMotion;
            speedSlider.IsVisible = hasMotion;
            pauseButton.IsVisible = hasMotion;
            resetButton.IsVisible = hasMotion;
            motionLabel.IsVisible = hasMotion;
            ratioLabel.IsVisible = hasMotion;
        }

        void RebuildModel()
        {
            var driverSize = (int)Math.Round(driverSlider.Value);
            var drivenSize = (int)Math.Round(drivenSlider.Value);
            if (driverSize == model.DriverTeeth &&
                drivenSize == model.DrivenTeeth &&
                currentStage == model.MaximumStage)
            {
                return;
            }

            model = new GearShapeLadderModel(
                driverSize,
                drivenSize,
                model.Motion.Value,
                currentStage);
            canvas.Model = model;
            RefreshLabels();
        }

        ladder.ValueChanged += value =>
        {
            currentStage = value;
            RebuildModel();
            canvas.Stage = value;
            RefreshLabels();
        };
        pauseButton.Click += (_, _) =>
        {
            paused = !paused;
            pauseButton.Content = paused ? "Resume" : "Pause";
        };
        resetButton.Click += (_, _) =>
        {
            model.ResetMotion();
            tickRemainder = 0;
            canvas.InvalidateVisual();
            RefreshLabels();
        };
        driverSlider.ValueChanged += (_, _) => RebuildModel();
        drivenSlider.ValueChanged += (_, _) => RebuildModel();
        speedSlider.ValueChanged += (_, _) =>
        {
            tickRate = speedSlider.Value;
            RefreshLabels();
        };

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000d / 30d) };
        timer.Tick += (_, _) =>
        {
            var elapsed = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();
            if (paused || tickRate <= 0 || currentStage < GearShapeGeometry.CoupledCircleStage)
            {
                return;
            }

            tickRemainder += elapsed * tickRate;
            var wholeTicks = (ulong)Math.Floor(tickRemainder);
            if (wholeTicks == 0)
            {
                return;
            }

            tickRemainder -= wholeTicks;
            model.Advance(wholeTicks);
            canvas.InvalidateVisual();
            RefreshLabels();
        };
        timer.Start();

        var controls = new StackPanel { Spacing = 8 };
        controls.Children.Add(Text("Gear Shape Ladder", 20, FontWeight.SemiBold));
        var intro = Text("One sized circle gains a second perimeter, coupled motion, and then three increasingly detailed tooth profiles.", 12);
        intro.TextWrapping = TextWrapping.Wrap;
        intro.Foreground = new SolidColorBrush(Color.FromRgb(83, 88, 94));
        controls.Children.Add(intro);
        controls.Children.Add(new Border { Height = 8 });
        controls.Children.Add(Text("Shape ladder", 13, FontWeight.SemiBold));
        controls.Children.Add(rungLabel);
        controls.Children.Add(ladder);
        controls.Children.Add(driverLabel);
        controls.Children.Add(driverSlider);
        controls.Children.Add(drivenLabel);
        controls.Children.Add(drivenSlider);
        controls.Children.Add(speedLabel);
        controls.Children.Add(speedSlider);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7, Margin = new Thickness(0, 4, 0, 4) };
        buttons.Children.Add(pauseButton);
        buttons.Children.Add(resetButton);
        controls.Children.Add(buttons);
        controls.Children.Add(motionLabel);
        controls.Children.Add(ratioLabel);

        var sidebar = new Border
        {
            Width = 292,
            Padding = new Thickness(22, 22, 18, 18),
            BorderBrush = new SolidColorBrush(Color.FromRgb(218, 216, 210)),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = new ScrollViewer { Content = controls }
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        Grid.SetColumn(sidebar, 0);
        Grid.SetColumn(canvas, 1);
        grid.Children.Add(sidebar);
        grid.Children.Add(canvas);
        RefreshLabels();
        return grid;
    }

    private static TextBlock Text(string value, double size, FontWeight? weight = null) => new()
    {
        Text = value,
        FontSize = size,
        FontWeight = weight ?? FontWeight.Normal,
        Foreground = new SolidColorBrush(Color.FromRgb(40, 45, 52))
    };

    private static Button Button(string content) => new()
    {
        Content = content,
        Padding = new Thickness(12, 6),
        HorizontalAlignment = HorizontalAlignment.Left
    };

    private static Avalonia.Controls.Slider Slider(double minimum, double maximum, double value) => new()
    {
        Minimum = minimum,
        Maximum = maximum,
        Value = value,
        TickFrequency = 1,
        IsSnapToTickEnabled = true,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };
}
