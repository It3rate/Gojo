using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace Gojo.PrimitiveExplorer;

internal static class Program
{
    internal static string? CaptureDirectory;
    [STAThread]
    public static void Main(string[] args)
    {
        CaptureDirectory = args.FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        AppBuilder.Configure<ExplorerApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
    }
}

internal sealed class ExplorerApp : Application
{
    public override void Initialize() { RequestedThemeVariant = ThemeVariant.Light; Styles.Add(new FluentTheme()); }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new ExplorerWindow();
        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class ExplorerWindow : Window
{
    private readonly ExplorerSession session = new();
    private readonly FocusLink focus = new();
    private readonly StackPanel controls = new() { Orientation = Orientation.Horizontal, Spacing = 12 };
    private readonly StackPanel nav = new() { Spacing = 4 };
    private readonly TextBlock title = Text("", 30, true), intro = Text("", 14), result = Text("", 15, true), boundary = Text("", 12),
        selection = Text("", 12), events = Text("", 12), status = Text("", 11), number = Text("", 12);
    private readonly EncodingCanvas encoding;
    private readonly StructureCanvas structure;
    private readonly ScrollViewer numericScroll;
    private ExplorerPage page;
    private readonly List<Button> chapters = [];
    private bool rebuild;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(650) };
    private readonly Button play = Button("Play");
    private string? lastPinned;

    public ExplorerWindow()
    {
        Title = "Gojo · Primitive Explorer"; Width = 1530; Height = 980; MinWidth = 1220; MinHeight = 820;
        Background = Palette.Paper; FontFamily = new FontFamily("Segoe UI"); WindowStartupLocation = WindowStartupLocation.CenterScreen;
        page = session.Read(); encoding = new(page, focus); structure = new(page, focus);
        numericScroll = new ScrollViewer { Content = encoding, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        var shell = new Grid { RowDefinitions = new("78,*"), Background = Palette.Paper };
        var mast = new Grid { ColumnDefinitions = new("220,*,Auto"), Margin = new Thickness(24, 14) };
        mast.Children.Add(Text("gojo / atlas", 25, true));
        var subtitle = new StackPanel { Spacing = 3 }; subtitle.Children.Add(Text("PRIMITIVE EXPLORER", 11, true)); subtitle.Children.Add(Text("From one bit of participation to connected patterns", 14));
        Grid.SetColumn(subtitle, 1); mast.Children.Add(subtitle);
        var help = Text("Hover to trace  ·  Click to pin  ·  Esc to clear", 12); help.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(help, 2); mast.Children.Add(help);
        shell.Children.Add(mast);
        var body = new Grid { ColumnDefinitions = new("238,*") }; Grid.SetRow(body, 1); shell.Children.Add(body);
        nav.Children.Add(Text("EXPLORE THE LAYERS", 10, true)); nav.Children.Add(new Border { Height = 8 });
        for (var i = 0; i < ExplorerSession.Chapters.Length; i++)
        {
            var chapter = i; var b = Button($"{i + 1:00}   {ExplorerSession.Chapters[i]}"); b.HorizontalContentAlignment = HorizontalAlignment.Left; b.FontSize = 12; b.Padding = new Thickness(10, 12); b.HorizontalAlignment = HorizontalAlignment.Stretch;
            b.Click += (_, _) => { Stop(); focus.Clear(); numericScroll.Offset = default; session.Navigate(chapter); }; chapters.Add(b); nav.Children.Add(b);
        }
        nav.Children.Add(new Border { Height = 20 });
        nav.Children.Add(Text("A living encoding", 14, true));
        nav.Children.Add(Text("The diagrams read the actual experiments. Screen distances are illustrations; the bits retain what is known.", 12));
        var side = new Border { Padding = new Thickness(16, 12, 12, 20), BorderBrush = Palette.Line, BorderThickness = new Thickness(0, 1, 1, 0), Child = new ScrollViewer { Content = nav } }; body.Children.Add(side);
        var main = new Grid { RowDefinitions = new("Auto,Auto,*,Auto"), Margin = new Thickness(24, 12, 24, 16) }; Grid.SetColumn(main, 1); body.Children.Add(main);
        var heading = new StackPanel { Spacing = 7 }; heading.Children.Add(number); heading.Children.Add(title); heading.Children.Add(intro); heading.Children.Add(status); main.Children.Add(heading);
        var toolbar = new ScrollViewer { Content = controls, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled, Margin = new Thickness(0, 16, 0, 14) }; Grid.SetRow(toolbar, 1); main.Children.Add(toolbar);
        var inspectors = new Grid { ColumnDefinitions = new("364,18,*") }; Grid.SetRow(inspectors, 2); main.Children.Add(inspectors);
        var numericPanel = new Grid { RowDefinitions = new("42,*") };
        numericPanel.Children.Add(new Border { Background = Palette.Dark, Padding = new Thickness(18, 12), Child = Text("01  /  NUMERIC ENCODING", 11, true, Palette.White) });
        Grid.SetRow(numericScroll, 1); numericPanel.Children.Add(numericScroll); inspectors.Children.Add(numericPanel);
        var picturePanel = new Grid { RowDefinitions = new("42,*,Auto,Auto") }; Grid.SetColumn(picturePanel, 2); inspectors.Children.Add(picturePanel);
        picturePanel.Children.Add(new Border { Background = Palette.Pale, Padding = new Thickness(16, 12), Child = Text("02  /  STRUCTURE & EFFECT", 11, true) });
        Grid.SetRow(structure, 1); picturePanel.Children.Add(structure);
        var focusPanel = new Border { MinHeight = 86, Background = Palette.White, BorderBrush = Palette.Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(16), Child = selection };
        Grid.SetRow(focusPanel, 2); picturePanel.Children.Add(focusPanel);
        events.FontFamily = new FontFamily("Cascadia Mono,Consolas"); events.Margin = new Thickness(8, 12, 0, 8); events.MinHeight = 54; Grid.SetRow(events, 3); picturePanel.Children.Add(events);
        var foot = new Grid { ColumnDefinitions = new("*,Auto"), Margin = new Thickness(0, 16, 0, 0) }; Grid.SetRow(foot, 3); main.Children.Add(foot);
        var reading = new StackPanel { Spacing = 7 }; reading.Children.Add(result); reading.Children.Add(boundary); foot.Children.Add(reading);
        var links = new StackPanel { Spacing = 6, Margin = new Thickness(18, 0, 0, 0) }; var source = Button("Encoding notes ↗"); source.Click += (_, _) => OpenNotes(); links.Children.Add(source);
        var next = Button("Next layer →"); next.Click += (_, _) => { Stop(); focus.Clear(); numericScroll.Offset = default; session.Navigate((session.Chapter + 1) % ExplorerSession.Chapters.Length); }; links.Children.Add(next); Grid.SetColumn(links, 1); foot.Children.Add(links);
        Content = shell;
        session.Changed += Refresh;
        focus.Changed += () =>
        {
            selection.Text = focus.Detail ?? (focus.Key is string key && page.Details.TryGetValue(key, out var detail) ? detail : "Move over a shape, site, connection or numeric bit. Its corresponding encoding lights up. Click to keep a selection and locate its bits in the left panel.");
            if (focus.Pinned != lastPinned)
            {
                lastPinned = focus.Pinned;
                if (focus.Pinned is string pin && encoding.OffsetOf(pin) is double y && (y < numericScroll.Offset.Y || y > numericScroll.Offset.Y + numericScroll.Viewport.Height - 60))
                    numericScroll.Offset = new Vector(0, Math.Max(0, y - 50));
            }
        };
        KeyDown += (_, e) => { if (e.Key == Key.Escape) focus.Clear(); };
        play.Click += (_, _) => { if (timer.IsEnabled) Stop(); else { timer.Start(); play.Content = "Pause"; } };
        timer.Tick += (_, _) => { if (session.Steps >= 48) Stop(); else page.Actions.FirstOrDefault(a => a.Label.StartsWith("Advance"))?.Run(); };
        Closed += (_, _) => timer.Stop();
        Refresh(); focus.Clear();
        if (Program.CaptureDirectory is not null) Opened += async (_, _) => await CaptureAll(Program.CaptureDirectory);
    }

    private void Stop() { timer.Stop(); play.Content = "Play"; }
    private void Refresh()
    {
        rebuild = true; page = session.Read();
        title.Text = page.Title; intro.Text = page.Intro; result.Text = page.Result; boundary.Text = page.Boundary;
        number.Text = $"LAYER {session.Chapter + 1:00} / {ExplorerSession.Chapters.Length:00}"; number.Foreground = Palette.Teal;
        status.Text = page.Status; status.Foreground = Palette.Muted;
        events.Text = page.Events.Count == 0 ? "Change a control to inspect the cause and effect.\nThe diagrams do not write back into the kernel." : string.Join('\n', page.Events.TakeLast(3));
        controls.Children.Clear();
        foreach (var choice in page.Choices)
        {
            var stack = new StackPanel { Spacing = 5 }; stack.Children.Add(Text(choice.Label, 11, true));
            var select = new ComboBox { ItemsSource = choice.Values, SelectedIndex = choice.Selected, MinWidth = 140, MaxWidth = 270, FontSize = 12, Background = Palette.White, Foreground = Palette.Ink };
            select.SelectionChanged += (_, _) => { if (!rebuild && select.SelectedIndex >= 0 && select.SelectedIndex != choice.Selected) { Stop(); choice.Change(select.SelectedIndex); } };
            stack.Children.Add(select); controls.Children.Add(stack);
        }
        foreach (var action in page.Actions)
        {
            var b = Button(action.Label); b.VerticalAlignment = VerticalAlignment.Bottom; b.Click += (_, _) => { Stop(); action.Run(); }; controls.Children.Add(b);
        }
        if (page.Actions.Any(a => a.Label.StartsWith("Advance"))) { play.VerticalAlignment = VerticalAlignment.Bottom; controls.Children.Add(play); }
        encoding.SetPage(page); structure.SetPage(page);
        for (var i = 0; i < chapters.Count; i++) { chapters[i].Background = i == session.Chapter ? Palette.Pale : Brushes.Transparent; chapters[i].Foreground = i == session.Chapter ? Palette.Teal : Palette.Ink; }
        rebuild = false;
        focus.Point(null);
    }

    private void OpenNotes()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Gojo.slnx"))) dir = dir.Parent;
        if (dir is null) { selection.Text = "Open the matching experiment document in the Gojo repository: " + page.Source; return; }
        var path = Path.Combine(dir.FullName, "experiments", "PrimitiveMeasure", page.Source);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    // Visual QA uses the same live control tree, layout and render backend as
    // the interactive window. No screenshot-specific diagram implementation.
    private async Task CaptureAll(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            // Exercise the actual event handlers, not only session methods.
            session.Navigate(0);
            var reverse = controls.Children.OfType<Button>().Single(b => b.Content?.ToString()?.StartsWith("Reverse direction") == true);
            reverse.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            if (session.A != 3) throw new InvalidOperationException("Direction button did not update the encoded word.");
            session.Navigate(9);
            var combo = controls.Children.OfType<StackPanel>().First().Children.OfType<ComboBox>().Single();
            combo.SelectedIndex = 2;
            if (session.Control != 2) throw new InvalidOperationException("Uncertain-control selector did not update the scenario.");
            var advance = controls.Children.OfType<Button>().First();
            advance.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            if (session.Steps != 1 || !page.Result.Contains("refused")) throw new InvalidOperationException("Step button did not preserve the uncertain branch.");
            for (var i = 0; i < ExplorerSession.Chapters.Length; i++)
            {
                session.Navigate(i);
                if (i == 9) { session.Control = 2; session.Steps = 1; session.Refresh(); }
                if (i == 11) { session.Mode = 0; session.Refresh(); }
                if (i == 12) { session.Mode = 3; session.Refresh(); }
                if (i == 13) { session.Mode = 1; session.Steps = 5; session.Refresh(); }
                if (i == 14) { session.Steps = 18; session.Refresh(); }
                numericScroll.Offset = default; focus.Clear();
                await Task.Delay(350);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var content = (Control)Content!;
                    using var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(content.Bounds.Width), (int)Math.Ceiling(content.Bounds.Height)), new Vector(96, 96));
                    bitmap.Render(content); bitmap.Save(Path.Combine(directory, $"{i + 1:00}.png"));
                }, DispatcherPriority.Render);
            }
            // Exercise a real hit on the rectangle canvas and its linked bit strip.
            session.Navigate(11); await Task.Delay(350);
            var scale = Math.Min(structure.Bounds.Width / 720, structure.Bounds.Height / 480);
            var hit = structure.Hit(new Point((structure.Bounds.Width - 720 * scale) / 2 + 350 * scale, (structure.Bounds.Height - 480 * scale) / 2 + 90 * scale));
            if (hit?.Key != "reach4") throw new InvalidOperationException("Diagram hit did not resolve the shared A reach.");
            focus.Pin(hit.Key);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var content = (Control)Content!; using var bitmap = new RenderTargetBitmap(new PixelSize((int)content.Bounds.Width, (int)content.Bounds.Height), new Vector(96, 96));
                bitmap.Render(content); bitmap.Save(Path.Combine(directory, "linked-selection.png"));
            }, DispatcherPriority.Render);
            File.WriteAllText(Path.Combine(directory, "capture-complete.txt"), "15 live Avalonia chapters and linked-selection capture rendered successfully. Native direction button, uncertainty selector, step button and diagram hit all verified.");
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(directory, "capture-error.txt"), ex.ToString()); Environment.ExitCode = 1; }
        finally { Close(); }
    }

    private static TextBlock Text(string value, double size, bool bold = false, IBrush? ink = null) => new()
    { Text = value, FontSize = size, FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal, Foreground = ink ?? Palette.Ink, TextWrapping = TextWrapping.Wrap };
    private static Button Button(string label) => new() { Content = label, FontSize = 12, Padding = new Thickness(12, 9), CornerRadius = new CornerRadius(7), Background = Palette.White, Foreground = Palette.Ink, BorderBrush = Palette.Line, BorderThickness = new Thickness(1) };
}
