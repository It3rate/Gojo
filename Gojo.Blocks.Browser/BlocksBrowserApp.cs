using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Gojo.Blocks;

internal sealed class BlocksBrowserApp : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new BlocksBrowserView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
