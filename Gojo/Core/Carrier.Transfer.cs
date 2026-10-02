namespace Gojo.Core;

public sealed partial class Carrier
{
    /// <summary>
    /// Injects the two validated, calibrated transfer deltas before settlement.
    /// Called only by a structural material operation, never by projection.
    /// </summary>
    internal void TransferTo(Carrier target, long sourceTicks, long targetTicks)
    {
        _ = ReadReceivedState(-(Int128)sourceTicks);
        _ = target.ReadReceivedState(targetTicks);
        Receive(-sourceTicks);
        target.Receive(targetTicks);
        SettleWorld();
        target.SettleWorld();
    }

    /// <summary>Captures local state as values, tension and carrier identity for a material witness.</summary>
    internal static Relation CaptureTransferState(Carrier source, Carrier target) =>
        Relation.OrderedGroup(new Carrier(), [.. ConnectedCarriers([source, target])
            .Select(c => Relation.OrderedGroup(new Carrier(),
                new CarrierLocation(c, c.Value), new CarrierLocation(c, c.Tension)))]);
}
