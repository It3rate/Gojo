using Avalonia;
using Avalonia.Browser;
using Gojo.Blocks;

internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        BlocksBrowserApp.InitialHref = args.Length > 0 ? args[0] : null;
        return AppBuilder.Configure<BlocksBrowserApp>()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }
}
