using Avalonia;
using Avalonia.Browser;
using System.Runtime.InteropServices.JavaScript;
namespace Gojo.PrimitiveExplorer;
internal static partial class BrowserProgram
{
    private static Task Main(string[] args) => AppBuilder.Configure<ExplorerApp>().WithInterFont().StartBrowserAppAsync("out");
    [JSImport("globalThis.openEncodingNotes")]
    internal static partial void OpenNotes(string file);
    [JSImport("globalThis.initialEncodingLayer")]
    internal static partial int InitialLayer();
    [JSImport("globalThis.initialOrnamentPlate")]
    internal static partial int InitialOrnamentPlate();
    [JSImport("globalThis.initialHintConstruction")]
    internal static partial int InitialHintConstruction();
}
