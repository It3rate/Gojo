using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using SkiaSharp;

namespace Gojo.Experiments.PrimitiveRuntime.Viewer;

internal static class Program
{
    internal static PrimitiveMotion Motion { get; private set; } = null!;
    internal static bool ReverseObserver { get; private set; }
    internal static int? Quantum { get; private set; }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            string? Read(string name) => args.FirstOrDefault(arg => arg.StartsWith(name + "=", StringComparison.Ordinal))?[(name.Length + 1)..];
            Motion = new PrimitiveMotion();
            ReverseObserver = args.Contains("--reverse-observer");
            Quantum = Read("--quantum") is { } q ? int.Parse(q) : null;
            if (Quantum is not null and not (1 or 2 or 4)) throw new ArgumentException("Quantum must be 1, 2, or 4.");
            var steps = Read("--steps") is { } s ? int.Parse(s) : 0;
            if (steps < 0 || steps > 1_000_000) throw new ArgumentException("Steps must be between 0 and 1000000.");
            for (var i = 0; i < steps; i++) Motion.Step();
            if (args.Contains("--unknown")) Motion.SetUnknown();
            if (args.Contains("--alternatives")) Motion.SetAlternatives();
            if (args.Contains("--block")) Motion.Block();
            if (Read("--render") is { Length: > 0 } destination)
            {
                using var bitmap = new SKBitmap(1560, 940);
                using var canvas = new SKCanvas(bitmap);
                PrimitiveRenderer.Draw(canvas, bitmap.Width, bitmap.Height, Presentation.Read(Motion, ReverseObserver, Quantum));
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                var path = Path.GetFullPath(destination);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = File.Create(path);
                data.SaveTo(stream);
                Console.WriteLine($"Rendered current primitive state: {path}");
                return 0;
            }
            AppBuilder.Configure<PrimitiveApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
            return 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or FormatException or OverflowException)
        {
            Console.Error.WriteLine("Cannot open primitive viewer: " + error.Message);
            return 1;
        }
    }
}

internal sealed class PrimitiveApp : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new PrimitiveWindow(Program.Motion, Program.ReverseObserver, Program.Quantum);
        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class PrimitiveWindow : Window
{
    public PrimitiveWindow(PrimitiveMotion motion, bool reverseObserver, int? quantum)
    {
        Title = "Gojo · Primitive runtime · A connection before numbers";
        Width = 1440;
        Height = 1020;
        MinWidth = 1120;
        MinHeight = 800;
        Background = new SolidColorBrush(Color.Parse("#F2F5F4"));
        var canvas = new PrimitiveCanvas(Presentation.Read(motion, reverseObserver, quantum));
        var play = Button("Play");
        var step = Button("Single step");
        var reset = Button("Reset");
        var reverse = Button("Reverse travel");
        var block = Button("Blocked input");
        var open = Button("Open binding");
        var alternatives = Button("Alternatives");
        var observer = new CheckBox { Content = "Reverse observer", IsChecked = reverseObserver, VerticalAlignment = VerticalAlignment.Center };
        var resolution = new ComboBox
        {
            ItemsSource = new[] { "Unmeasured", "Calibrated · tick 1", "Calibrated · tick 2", "Calibrated · tick 4" },
            SelectedIndex = quantum switch { 1 => 1, 2 => 2, 4 => 3, _ => 0 }, MinWidth = 182
        };
        var playing = false;
        void Refresh()
        {
            reverseObserver = observer.IsChecked == true;
            quantum = resolution.SelectedIndex switch { 1 => 1, 2 => 2, 3 => 4, _ => null };
            canvas.View = Presentation.Read(motion, reverseObserver, quantum);
            canvas.InvalidateVisual();
        }
        void Pause() { playing = false; play.Content = "Play"; }
        void Input(Action action) { Pause(); action(); Refresh(); }
        play.Click += (_, _) => { playing = !playing; play.Content = playing ? "Pause" : "Play"; };
        step.Click += (_, _) => Input(motion.Step);
        reset.Click += (_, _) => Input(motion.Reset);
        reverse.Click += (_, _) => Input(motion.ReverseTravel);
        block.Click += (_, _) => Input(motion.Block);
        open.Click += (_, _) => Input(motion.SetUnknown);
        alternatives.Click += (_, _) => Input(motion.SetAlternatives);
        observer.IsCheckedChanged += (_, _) => Refresh();
        resolution.SelectionChanged += (_, _) => Refresh();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        // The timer offers an event. The connected transition law determines motion.
        timer.Tick += (_, _) => { if (playing) { motion.Step(); Refresh(); } };
        Opened += (_, _) => timer.Start();
        Closed += (_, _) => timer.Stop();

        var controls = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(32, 0, 32, 18) };
        foreach (var control in new Control[] { play, step, reset, reverse, block, open, alternatives, observer, resolution })
        {
            control.Margin = new Thickness(0, 4, 9, 4);
            controls.Children.Add(control);
        }
        var content = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        content.Children.Add(canvas);
        Grid.SetRow(controls, 1);
        content.Children.Add(controls);
        Content = content;
    }

    private static Button Button(string text) => new() { Content = text, Padding = new Thickness(12, 8) };
}

internal static class Presentation
{
    public static InspectionView Read(PrimitiveMotion motion, bool reverseObserver, int? quantum)
    {
        var read = motion.Observe(reverseObserver, quantum);
        var choices = read.Candidates.Select(candidate => new ChoiceView(candidate.Landmark, candidate.Forward,
            $"{Landmark(candidate.Landmark)} · {(candidate.Forward ? "forward" : "reverse")} phase")).ToArray();
        var metric = quantum is null
            ? new[] { "No unit or magnitude has been selected.", "Landmark addresses are not coordinates." }
            : read.Candidates.Length == 0
                ? new[] { $"Selected quantum: {quantum} source ticks.", "No unique numeric position is supplied." }
                : read.Candidates.Take(2).Select(candidate =>
                    $"{Landmark(candidate.Landmark)}: {candidate.CoarseTicks} ticks; residue {Fraction(candidate.ResidueNumerator, candidate.ResidueDenominator)}")
                    .Append($"Selected quantum: {quantum} source ticks.").ToArray();
        return new InspectionView(read.Step, reverseObserver, quantum is not null, read.Open, read.StateMask,
            ["A", "B", "C", "D"], read.CalibratedTicks?.Select(value => (double)value).ToArray(), choices,
            [$"anchor @{read.AnchorAddress}", $"moving site @{read.MovingAddress}", "terminal D", $"independent @{read.IndependentAddress}"],
            "Each alternative keeps landmark and phase together.", Status(read.Status),
            [read.Status == "Ready" ? "Explicit binding supplied; no transition offered yet." : read.Applied ? "Unique supported transition applied." : "Current state held; the offered read remains visible.",
                $"State mask: 0x{read.StateMask:X2}    Offered mask: 0x{read.OfferedMask:X2}",
                Clip(read.Receipt, 79), "Current evidence retained; no growing replay archive."],
            [$"Arena + compiled table payload: {read.DefinitionBytes:N0} bytes",
                $"Occurrence + receipt estimate: {read.OccurrenceBytes:N0} bytes",
                "Addresses preserve participant identity and ordered connections.",
                "Native correspondence graph and maps are additional shared cost.",
                "Display and native inspection allocate outside the step loop."], metric, 8);
    }

    private static string Landmark(int index) => index is >= 0 and < 4 ? ((char)('A' + index)).ToString() : "?";
    private static string Status(string status) => status switch
    {
        "NoTransition" => "No admitted transition", "StaleDefinition" => "Definition changed",
        "Ambiguous" => "Multiple supported responses", "Contradiction" => "Empty closed support",
        "Open" => "Open participation retained", _ => status
    };
    private static string Fraction(long? numerator, long? denominator) => numerator is null ? "open" : denominator is null or 1 ? numerator.ToString()! : $"{numerator}/{denominator}";
    private static string Clip(string value, int length) => value.Length <= length ? value : value[..(length - 1)] + "…";
}
