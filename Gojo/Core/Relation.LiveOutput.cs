namespace Gojo.Core;

public sealed partial class Relation
{
    #region Live Output Ports

    /// <summary>Applies an input-to-port relation as a live output read into the port's concrete target carrier.</summary>
    private bool TryApplyLiveOutput()
    {
        if (Right is not Relation port)
        {
            return false;
        }

        return TryApplyLiveProjectionOutput(Left, port) ||
            TryApplyLiveOperationOutput(Left, port);
    }

    /// <summary>Applies a live single-slot projection shaped as input -> (source surface -> target surface).</summary>
    private static bool TryApplyLiveProjectionOutput(IElement input, Relation port)
    {
        if (input is not CarrierLocation location)
        {
            return false;
        }

        if (TryProjectToTargetSurface(port.Left, port.Right, location, out var projection) ||
            TryProjectToTargetSurface(port.Right, port.Left, location, out projection))
        {
            var targets = new List<LiveOutputTarget>
            {
                new(projection.Target.Carrier, projection.Target.Ticks)
            };
            return ApplyLiveOutputTargets(targets);
        }

        return false;
    }

    /// <summary>Applies a live operation output shaped as input group -> operation surface.</summary>
    private static bool TryApplyLiveOperationOutput(IElement input, Relation operation)
    {
        if (!TryReadFrame(operation.Right, out var targetFrame) ||
            targetFrame.Carrier.IsUnknown ||
            !operation.TryReadOperationFrom(input, targetFrame.Carrier, out var read) ||
            read.Left is not CarrierLocation target ||
            !target.Carrier.SameIdentityAs(targetFrame.Carrier))
        {
            return false;
        }

        var targets = new List<LiveOutputTarget>
        {
            new(target.Carrier, target.Ticks)
        };
        return ApplyLiveOutputTargets(targets);
    }

    /// <summary>Feeds every validated live output target into its carrier by delta.</summary>
    private static bool ApplyLiveOutputTargets(List<LiveOutputTarget> targets)
    {
        var changed = false;
        foreach (var target in targets
            .GroupBy(target => target.Carrier)
            .Select(group => new LiveOutputTarget(group.Key, group.Sum(target => target.Ticks))))
        {
            changed |= target.Carrier.ReceiveTo(target.Ticks);
        }

        return changed;
    }

    #endregion
}
