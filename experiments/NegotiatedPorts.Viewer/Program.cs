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

namespace NegotiatedPorts.Viewer;

internal static class Program
{
    internal static Replay Document { get; private set; } = null!;
    internal static int InitialFrame { get; private set; }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            string? Read(string name) => args.FirstOrDefault(a => a.StartsWith(name + "=", StringComparison.Ordinal))?[(name.Length + 1)..];
            Document = Replay.Load(Path.GetFullPath(Read("--replay") ?? DefaultReplay()));
            InitialFrame = int.TryParse(Read("--frame"), out var index) ? Math.Clamp(index, 0, Document.Frames.Length - 1) : 0;
            if (Read("--render") is { Length: > 0 } destination)
            {
                using var bitmap = new SKBitmap(1600, 1120);
                using var canvas = new SKCanvas(bitmap);
                ReplayRenderer.Draw(canvas, bitmap.Width, bitmap.Height, Document, InitialFrame);
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                var path = Path.GetFullPath(destination);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = File.Create(path);
                data.SaveTo(stream);
                Console.WriteLine($"Rendered frame {InitialFrame}, step {Document.Frames[InitialFrame].Step}: {path}");
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

    private static string DefaultReplay()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
        {
            var candidate = Path.Combine(folder.FullName, "experiments", "NegotiatedPorts", "results", "changing-input.json");
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("Cannot find the recorded experiment. Use --replay=<path>.");
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
        Title = $"Gojo · Negotiated ports · Recorded replay · {replay.Title} · {replay.Fingerprint}";
        Width = 1440;
        Height = 1070;
        MinWidth = 1000;
        MinHeight = 790;
        Background = new SolidColorBrush(Color.Parse("#F0F3F5"));
        var canvas = new ReplayCanvas(replay) { Frame = initial };
        var scrubber = new Slider { Minimum = 0, Maximum = replay.Frames.Length - 1, Value = initial,
            TickFrequency = 1, IsSnapToTickEnabled = true, HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center };
        var play = Button("Play");
        var back = Button("−1");
        var forward = Button("+1");
        var previousPhase = Button("Previous phase");
        var nextPhase = Button("Next phase");
        var position = new TextBlock { VerticalAlignment = VerticalAlignment.Center, MinWidth = 125 };
        var playing = false;
        var clock = Stopwatch.StartNew();
        double accumulated = 0;
        void Refresh()
        {
            canvas.Frame = (int)Math.Round(scrubber.Value);
            position.Text = $"{canvas.Frame + 1} / {replay.Frames.Length} frames";
        }
        void Pause() { playing = false; play.Content = "Play"; accumulated = 0; }
        void Step(int direction) { Pause(); scrubber.Value = Math.Clamp(canvas.Frame + direction, 0, replay.Frames.Length - 1); }
        void Phase(int direction)
        {
            Pause();
            var frame = canvas.Frame;
            if (direction > 0)
            {
                var phase = replay.Frames[frame].Phase;
                while (frame + 1 < replay.Frames.Length && replay.Frames[frame + 1].Phase == phase) frame++;
                if (frame + 1 < replay.Frames.Length) frame++;
            }
            else
            {
                var phase = replay.Frames[frame].Phase;
                while (frame > 0 && replay.Frames[frame - 1].Phase == phase) frame--;
                if (frame > 0)
                {
                    frame--;
                    phase = replay.Frames[frame].Phase;
                    while (frame > 0 && replay.Frames[frame - 1].Phase == phase) frame--;
                }
            }
            scrubber.Value = frame;
        }
        play.Click += (_, _) =>
        {
            playing = !playing;
            if (playing && canvas.Frame == replay.Frames.Length - 1) scrubber.Value = 0;
            play.Content = playing ? "Pause" : "Play";
            clock.Restart();
        };
        back.Click += (_, _) => Step(-1);
        forward.Click += (_, _) => Step(1);
        previousPhase.Click += (_, _) => Phase(-1);
        nextPhase.Click += (_, _) => Phase(1);
        scrubber.ValueChanged += (_, _) => Refresh();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000d / 30) };
        timer.Tick += (_, _) =>
        {
            var elapsed = clock.Elapsed.TotalSeconds;
            clock.Restart();
            if (!playing) return;
            accumulated += elapsed * 3;
            var ticks = (int)accumulated;
            if (ticks == 0) return;
            accumulated -= ticks;
            scrubber.Value = Math.Min(replay.Frames.Length - 1, canvas.Frame + ticks);
            if (canvas.Frame == replay.Frames.Length - 1) Pause();
        };
        Opened += (_, _) => { clock.Restart(); timer.Start(); };
        Closed += (_, _) => timer.Stop();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
        foreach (var button in new[] { play, back, forward, previousPhase, nextPhase }) buttons.Children.Add(button);
        var controls = new Grid { Margin = new Thickness(24, 0, 24, 16), ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        controls.Children.Add(buttons);
        Grid.SetColumn(scrubber, 1);
        scrubber.Margin = new Thickness(16, 0);
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

    private static Button Button(string text) => new() { Content = text, Padding = new Thickness(11, 6) };
}
