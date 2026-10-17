using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Gojo.Blocks;

internal sealed class BlocksBrowserApp : Application
{
    public static string? InitialHref { get; set; }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new BlocksBrowserView(InitialHref);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
