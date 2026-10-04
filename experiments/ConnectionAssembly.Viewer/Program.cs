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

namespace ConnectionAssembly.Viewer;

internal static class Program
{
    internal static Replay Document { get; private set; } = null!;
    internal static int InitialFrame { get; private set; }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            string? Read(string name) => args.FirstOrDefault(arg => arg.StartsWith(name + "=", StringComparison.Ordinal))?[(name.Length + 1)..];
            var path = Read("--replay") ?? "experiments/ConnectionAssembly/results-dynamic/changing-patterns.json";
            Document = Replay.Load(Path.GetFullPath(path));
            InitialFrame = int.TryParse(Read("--frame"), out var frame) ? Math.Clamp(frame, 0, Document.Frames.Length - 1) : 0;
            if (Read("--render") is { Length: > 0 } destination)
            {
                using var bitmap = new SKBitmap(1480, 1000);
                using var canvas = new SKCanvas(bitmap);
                ReplayRenderer.Draw(canvas, bitmap.Width, bitmap.Height, Document, InitialFrame);
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                var output = Path.GetFullPath(destination);
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                using var stream = File.Create(output);
                data.SaveTo(stream);
                Console.WriteLine($"Rendered frame {InitialFrame}, step {Document.Frames[InitialFrame].Step}: {output}");
                return 0;
            }
            AppBuilder.Configure<ReplayApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
            return 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException)
        {
            Console.Error.WriteLine("Cannot open replay: " + error.Message);
            return 1;
        }
    }
}

internal sealed class ReplayApp : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new ReplayWindow(Program.Document, Program.InitialFrame);
        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class ReplayWindow : Window
{
    public ReplayWindow(Replay replay, int initial)
    {
        Title = $"Gojo · Recorded replay · {Path.GetFileName(Path.GetDirectoryName(replay.SourcePath))} · {replay.ContentFingerprint}";
        Width = 1480;
        Height = 1060;
        MinWidth = 1060;
        MinHeight = 840;
        Background = new SolidColorBrush(Color.Parse("#F2F4F0"));
        var canvas = new ReplayCanvas(replay) { Frame = initial };
        var scrubber = new Slider
        {
            Minimum = 0, Maximum = replay.Frames.Length - 1, Value = initial,
            TickFrequency = 1, IsSnapToTickEnabled = true,
            HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center
        };
        var play = Button("Play");
        var back = Button("−1");
        var forward = Button("+1");
        var nextPhase = Button("Next phase");
        var previousPhase = Button("Previous phase");
        var position = new TextBlock { VerticalAlignment = VerticalAlignment.Center, MinWidth = 160 };
        var playing = false;
        var stopwatch = Stopwatch.StartNew();
        double accumulated = 0;
        void Refresh()
        {
            canvas.Frame = (int)Math.Round(scrubber.Value);
            position.Text = $"Frame {canvas.Frame + 1} / {replay.Frames.Length}";
        }
        void Pause() { playing = false; play.Content = "Play"; accumulated = 0; }
        void Step(int direction) { Pause(); scrubber.Value = Math.Clamp(canvas.Frame + direction, 0, replay.Frames.Length - 1); }
        void Phase(int direction)
        {
            Pause();
            var index = canvas.Frame;
            if (direction > 0)
            {
                var phase = replay.Frames[index].Phase;
                while (index + 1 < replay.Frames.Length && replay.Frames[index + 1].Phase == phase) index++;
                if (index + 1 < replay.Frames.Length) index++;
            }
            else
            {
                var phase = replay.Frames[index].Phase;
                while (index > 0 && replay.Frames[index - 1].Phase == phase) index--;
                if (index > 0)
                {
                    index--;
                    phase = replay.Frames[index].Phase;
                    while (index > 0 && replay.Frames[index - 1].Phase == phase) index--;
                }
            }
            scrubber.Value = index;
        }
        play.Click += (_, _) =>
        {
            playing = !playing;
            if (playing && canvas.Frame == replay.Frames.Length - 1) scrubber.Value = 0;
            play.Content = playing ? "Pause" : "Play";
            stopwatch.Restart();
        };
        back.Click += (_, _) => Step(-1);
        forward.Click += (_, _) => Step(1);
        previousPhase.Click += (_, _) => Phase(-1);
        nextPhase.Click += (_, _) => Phase(1);
        scrubber.ValueChanged += (_, _) => Refresh();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000d / 30) };
        timer.Tick += (_, _) =>
        {
            var elapsed = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();
            if (!playing) return;
            accumulated += elapsed * 12;
            var ticks = (int)accumulated;
            if (ticks == 0) return;
            accumulated -= ticks;
            scrubber.Value = Math.Min(replay.Frames.Length - 1, canvas.Frame + ticks);
            if (canvas.Frame == replay.Frames.Length - 1) Pause();
        };
        Opened += (_, _) => { stopwatch.Restart(); timer.Start(); };
        Closed += (_, _) => timer.Stop();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var button in new[] { play, back, forward, previousPhase, nextPhase }) buttons.Children.Add(button);
        var controls = new Grid { Margin = new Thickness(30, 0, 30, 18), ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        controls.Children.Add(buttons);
        scrubber.Margin = new Thickness(18, 0);
        Grid.SetColumn(scrubber, 1);
        controls.Children.Add(scrubber);
        Grid.SetColumn(position, 2);
        controls.Children.Add(position);
        var content = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        content.Children.Add(canvas);
        Grid.SetRow(controls, 1);
        content.Children.Add(controls);
        Content = content;
        Refresh();
    }

    private static Button Button(string label) => new() { Content = label, Padding = new Thickness(12, 7) };
}
