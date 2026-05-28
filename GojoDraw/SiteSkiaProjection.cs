using Gojo.Core;
using SkiaSharp;

namespace GojoDraw;

/// <summary>Simple projection for visualizer-style shared Gojo sites.</summary>
public sealed class SiteSkiaProjection : IGojoCadSkiaProjection
{
    private readonly Dictionary<TickRef, SKPoint> sites = [];
    private readonly Dictionary<LocationKey, SKPoint> locations = [];

    public SiteSkiaProjection Map(TickRef site, float x, float y) =>
        Map(site, new SKPoint(x, y));

    public SiteSkiaProjection Map(TickRef site, SKPoint point)
    {
        sites[site ?? throw new ArgumentNullException(nameof(site))] = point;
        return this;
    }

    public SiteSkiaProjection Map(Carrier carrier, long ticks, float x, float y) =>
        Map(carrier, ticks, new SKPoint(x, y));

    public SiteSkiaProjection Map(Carrier carrier, long ticks, SKPoint point)
    {
        locations[LocationKey.Of(carrier ?? throw new ArgumentNullException(nameof(carrier)), ticks)] = point;
        return this;
    }

    public bool TryProject(CarrierLocation location, out SKPoint point)
    {
        if (location.Tick is not null && sites.TryGetValue(location.Tick, out point))
        {
            return true;
        }

        if (locations.TryGetValue(LocationKey.Of(location.Carrier, location.Ticks), out point))
        {
            return true;
        }

        point = new SKPoint(location.Ticks, 0);
        return true;
    }

    private readonly record struct LocationKey(ulong CarrierId, long Ticks)
    {
        public static LocationKey Of(Carrier carrier, long ticks) => new(carrier.Id, ticks);
    }
}
