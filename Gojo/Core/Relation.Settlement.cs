namespace Gojo.Core;

public sealed partial class Relation
{
    #region Structural Perturbation and Settlement

    /// <summary>Moves or scales carrier locations participating in this relation.</summary>
    public bool Perturb(IElement perturbation)
    {
        if (perturbation is CarrierLocation delta)
        {
            return TryTransformLocations(location =>
                location.Carrier.SameIdentityAs(delta.Carrier)
                    ? LocationTransform.To(new CarrierLocation(location.Carrier, checked(location.Ticks + delta.Ticks)))
                    : LocationTransform.To(location));
        }

        if (perturbation is Relation relation &&
            TryReadBasis(relation.Left, out var source) &&
            TryReadBasis(relation.Right, out var target) &&
            source.Ticks != 0 &&
            target.Ticks != 0)
        {
            return TryTransformLocations(location =>
                TryScale(location, source.Ticks, target.Ticks, out var scaled)
                    ? LocationTransform.To(scaled)
                    : LocationTransform.Blocked);
        }

        return false;
    }

    /// <summary>Applies this relation as settlement structure in an attached world.</summary>
    internal bool Apply()
    {
        if (TryApplyLiveOutput())
        {
            return true;
        }

        var left = CarrierSitesOf(Left);
        var right = CarrierSitesOf(Right);
        if (!TryReadOneCarrier(left, out var leftSites) ||
            !TryReadOneCarrier(right, out var rightSites))
        {
            return false;
        }

        if (leftSites.Carrier.SameIdentityAs(rightSites.Carrier))
        {
            return AccountLocal(leftSites.Carrier, leftSites.Max, rightSites.Min);
        }

        if (leftSites.Carrier.IsInverse != rightSites.Carrier.IsInverse &&
            rightSites.IsOrigin)
        {
            return leftSites.Carrier.BalanceOpposedValueWith(rightSites.Carrier) |
                RouteNegativeTension(leftSites, rightSites);
        }

        return rightSites.IsOrigin && RoutePositiveTension(leftSites, rightSites);
    }

    #endregion

    #region Settlement Helpers

    /// <summary>Applies same-carrier accounting for reset, wrap, and clamp shapes.</summary>
    private static bool AccountLocal(
        Carrier carrier,
        long source,
        long target)
    {
        if (source <= 0 || target < 0 || source < target)
        {
            return false;
        }

        var sourceTicks = checked((ulong)source);
        var targetTicks = checked((ulong)target);
        if (carrier.Value <= targetTicks)
        {
            return false;
        }

        var local = sourceTicks == targetTicks
            ? Math.Min(carrier.Value, sourceTicks)
            : checked(targetTicks + ((carrier.Value - targetTicks) % (sourceTicks - targetTicks)));
        return local != carrier.Value &&
            carrier.LeaveTension(local, checked((long)(carrier.Value - local)));
    }

    /// <summary>Routes positive source tension into an origin receiver relation.</summary>
    private static bool RoutePositiveTension(CarrierSites source, CarrierSites receiver)
    {
        var unit = source.Width == 0 ? source.Max : source.Width;
        if (unit <= 0)
        {
            return false;
        }

        var target = source.Width == 0 ? 0 : source.Min;
        var changed = AccountLocal(source.Carrier, source.Max, target);
        if (source.Carrier.Tension <= 0)
        {
            return changed;
        }

        var tension = checked((ulong)source.Carrier.Tension);
        var unitTicks = checked((ulong)unit);
        var received = tension / unitTicks;
        if (received == 0)
        {
            return changed;
        }

        changed |= source.Carrier.KeepTensionRemainder(checked((long)(tension % unitTicks)));
        receiver.Carrier.Receive(checked((long)received));
        return true;
    }

    /// <summary>Routes negative source tension into an inverse-origin receiver relation.</summary>
    private static bool RouteNegativeTension(CarrierSites source, CarrierSites receiver)
    {
        var unit = source.Width == 0 ? source.Max : source.Width;
        if (unit <= 0)
        {
            return false;
        }

        if (source.Carrier.Tension >= 0)
        {
            return false;
        }

        var tension = checked((ulong)-source.Carrier.Tension);
        var unitTicks = checked((ulong)unit);
        var received = tension / unitTicks;
        if (received == 0)
        {
            return false;
        }

        var changed = source.Carrier.KeepTensionRemainder(-checked((long)(tension % unitTicks)));
        receiver.Carrier.Receive(checked((long)received));
        return true;
    }

    /// <summary>Collects all carrier sites participating in settlement application.</summary>
    private static CarrierLocation[] CarrierSitesOf(IElement element) => element switch
    {
        CarrierLocation location => [location],
        Relation relation => [.. CarrierSitesOf(relation.Left), .. CarrierSitesOf(relation.Right)],
        _ => []
    };

    /// <summary>Reads a carrier-site set only when all sites share one carrier.</summary>
    private static bool TryReadOneCarrier(
        CarrierLocation[] sites,
        out CarrierSites carrierSites)
    {
        if (sites.Length == 0)
        {
            carrierSites = default;
            return false;
        }

        var carrier = sites[0].Carrier;
        if (sites.Any(site => !site.Carrier.SameIdentityAs(carrier)))
        {
            carrierSites = default;
            return false;
        }

        carrierSites = new(
            carrier,
            sites.Min(site => site.Ticks),
            sites.Max(site => site.Ticks));
        return true;
    }

    #endregion
}
