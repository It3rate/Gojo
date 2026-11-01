using Avalonia;
using Avalonia.Browser;
using System.Runtime.InteropServices.JavaScript;
namespace Gojo.PrimitiveExplorer;
internal static partial class BrowserProgram
{
    private static Task Main(string[] args) => AppBuilder.Configure<ExplorerApp>().WithInterFont().StartBrowserAppAsync("out");
    [JSImport("globalThis.openEncodingNotes")]
    internal static partial void OpenNotes(string file);
}
