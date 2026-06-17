using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Gojo.Blocks;

internal sealed partial class Program
{
    private static Task Main(string[] args) =>
        AppBuilder.Configure<BlocksBrowserApp>()
            .WithInterFont()
            .StartBrowserAppAsync("out");
}
