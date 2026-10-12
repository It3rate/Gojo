using Avalonia;
using Avalonia.Browser;
using Avalonia.Controls.ApplicationLifetimes;
using System.Runtime.InteropServices.JavaScript;

namespace Phototaxis.Browser;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        await JSHost.ImportAsync("bath-ui", "../bath-ui.js");
        await AppBuilder.Configure<BathApp>().WithInterFont().StartBrowserAppAsync("out");
    }

    [JSImport("downloadGraph", "bath-ui")]
    internal static partial void DownloadGraph(string name, string base64);

    [JSImport("setStatus", "bath-ui")]
    internal static partial void SetStatus(string status);

    [JSImport("takeStopRequest", "bath-ui")]
    internal static partial bool TakeStopRequest();

    [JSImport("jevStatus", "bath-ui")]
    internal static partial Task<string> JevStatus();

    [JSImport("askJev", "bath-ui")]
    internal static partial Task<string> AskJev(string request);
}

public sealed class BathApp : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime single)
            single.MainView = new BathCanvas();
        base.OnFrameworkInitializationCompleted();
    }
}
