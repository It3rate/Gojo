using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Gojo.Blocks;

internal sealed class BlocksBrowserView : Grid
{
    private readonly BlocksCanvas canvas;
    private readonly Border notesDrawer;
    private readonly TextBox notesText;
    private readonly TextBlock notesTitle;

    public BlocksBrowserView(string? initialHref = null)
    {
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

        Children.Add(canvas);
        Children.Add(notesDrawer);

        SizeChanged += (_, _) => UpdateNotesDrawer(canvas.Document);
        canvas.Document = ApplyInitialPage(BlocksDocument.CreateDefault(), initialHref);
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

    private static BlocksDocument ApplyInitialPage(BlocksDocument document, string? initialHref)
    {
        if (string.IsNullOrWhiteSpace(initialHref) ||
            !Uri.TryCreate(initialHref, UriKind.Absolute, out var uri))
        {
            return document;
        }

        var pageValue = uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .FirstOrDefault(parts => string.Equals(Uri.UnescapeDataString(parts[0]), "page", StringComparison.OrdinalIgnoreCase))?[1];

        if (string.IsNullOrWhiteSpace(pageValue))
        {
            return document;
        }

        pageValue = Uri.UnescapeDataString(pageValue);
        if (int.TryParse(pageValue, out var pageNumber))
        {
            return document.SelectPage(pageNumber - 1);
        }

        var index = document.Pages
            .Select((page, pageIndex) => new { page.Id, Index = pageIndex })
            .FirstOrDefault(page => string.Equals(page.Id, pageValue, StringComparison.OrdinalIgnoreCase))
            ?.Index;

        return index is { } pageIndex ? document.SelectPage(pageIndex) : document;
    }
}
