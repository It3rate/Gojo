using Avalonia;
using Avalonia.Browser;

namespace Gojo.GearShapeLadder;

internal sealed class BrowserProgram
{
    private static Task Main(string[] args) =>
        Program.BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
}
