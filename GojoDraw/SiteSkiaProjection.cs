using Gojo.Core;
using SkiaSharp;

namespace GojoDraw;

/// <summary>Simple projection for visualizer-style shared Gojo sites.</summary>
public sealed class SiteSkiaProjection : IGojoCadSkiaProjection
{
    private readonly Dictionary<TickRef, SKPoint> sites = [];

    public SiteSkiaProjection Map(TickRef site, float x, float y) =>
        Map(site, new SKPoint(x, y));

    public SiteSkiaProjection Map(TickRef site, SKPoint point)
    {
        sites[site ?? throw new ArgumentNullException(nameof(site))] = point;
        return this;
    }

    public bool TryProject(CarrierLocation location, out SKPoint point)
    {
        if (location.Tick is not null && sites.TryGetValue(location.Tick, out point))
        {
            return true;
        }

        point = new SKPoint(location.Ticks, 0);
        return true;
    }
}
