using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GojoDraw;
using SkiaSharp;

namespace Gojo.Blocks;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (TryExportGlossaryPage(args))
        {
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();

    private static bool TryExportGlossaryPage(string[] args)
    {
        var pageId = ValueAfter(args, "--export-glossary-page");
        if (string.IsNullOrWhiteSpace(pageId))
        {
            return false;
        }

        var output = ValueAfter(args, "--output") ??
            Path.Combine(Environment.CurrentDirectory, $"{pageId}.png");
        var width = IntValueAfter(args, "--width") ?? 1180;
        var height = IntValueAfter(args, "--height") ?? 760;
        var themeIndex = IntValueAfter(args, "--theme") ?? 0;
        var document = BlocksDocument.CreateDefault();
        var pageIndex = document.GlossaryPages
            .Select((page, index) => (page, index))
            .FirstOrDefault(candidate => candidate.page.Id.Equals(pageId, StringComparison.OrdinalIgnoreCase))
            .index;
        if (!document.GlossaryPages.Any(page => page.Id.Equals(pageId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Unknown glossary page '{pageId}'.", nameof(args));
        }

        document = document.SelectPage(pageIndex) with { GlossaryVisible = true, ThemeIndex = themeIndex };
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        BlocksDrawOperation.DrawToCanvas(
            canvas,
            new SKSize(width, height),
            document,
            GojoDrawAnimationClock.FrameFor(TimeSpan.Zero, document.CurrentPage.TutorialScene));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 95);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)) ?? Environment.CurrentDirectory);
        using var stream = File.Open(output, FileMode.Create, FileAccess.Write);
        data.SaveTo(stream);
        return true;
    }

    private static string? ValueAfter(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (args[index].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static int? IntValueAfter(IReadOnlyList<string> args, string name) =>
        int.TryParse(ValueAfter(args, name), out var value) && value > 0
            ? value
            : null;
}

internal sealed class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class MainWindow : Window
{
    private readonly BlocksCanvas canvas;
    private readonly Border notesDrawer;
    private readonly TextBox notesText;
    private readonly TextBlock notesTitle;

    public MainWindow()
    {
        Title = "Gojo Blocks";
        Width = 1180;
        Height = 760;
        MinWidth = 760;
        MinHeight = 520;

        canvas = new BlocksCanvas();
        canvas.DocumentChanged += (_, document) => UpdateNotesDrawer(document);

        notesText = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13.5
        };

        var notesPanel = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Margin = new Thickness(18, 12, 18, 12)
        };
        notesTitle = new TextBlock
        {
            Text = "Narration notes",
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11.5,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(105, 112, 120)),
            Margin = new Thickness(0, 0, 0, 8)
        };
        notesPanel.Children.Add(notesTitle);
        Grid.SetRow(notesText, 1);
        notesPanel.Children.Add(notesText);

        notesDrawer = new Border
        {
            Height = 160,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush(Color.FromArgb(246, 255, 255, 252)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 195, 190)),
            BorderThickness = new Thickness(1.1),
            CornerRadius = new CornerRadius(8),
            Child = notesPanel,
            IsVisible = false
        };

        var root = new Grid();
        root.Children.Add(canvas);
        root.Children.Add(notesDrawer);
        Content = root;

        SizeChanged += (_, _) => UpdateNotesDrawer(canvas.Document);
        canvas.Document = BlocksDocument.CreateDefault();
    }

    private void UpdateNotesDrawer(BlocksDocument? document)
    {
        if (document is null)
        {
            notesDrawer.IsVisible = false;
            return;
        }

        notesDrawer.IsVisible = document.NotesVisible;
        notesText.Text = document.CurrentPage.Body;
        var theme = document.CurrentTheme;
        notesDrawer.Background = new SolidColorBrush(ToAvalonia(theme.PageFill, 246));
        notesDrawer.BorderBrush = new SolidColorBrush(ToAvalonia(theme.PageStroke));
        notesTitle.Foreground = new SolidColorBrush(ToAvalonia(theme.Muted));
        notesText.Foreground = new SolidColorBrush(ToAvalonia(theme.Ink));
        notesDrawer.Margin = new Thickness(
            BlocksLayout.ContentLeft(document.SidePanelVisible),
            0,
            BlocksLayout.PageMargin,
            88);
    }

    private static Color ToAvalonia(SkiaSharp.SKColor color, byte? alpha = null) =>
        Color.FromArgb(alpha ?? color.Alpha, color.Red, color.Green, color.Blue);
}
