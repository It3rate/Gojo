using Avalonia.Browser;

namespace GojoCAD.SketchViewer;

internal sealed class BrowserProgram
{
    private static Task Main(string[] args) =>
        Program.BuildAvaloniaApp().StartBrowserAppAsync("out");
}
