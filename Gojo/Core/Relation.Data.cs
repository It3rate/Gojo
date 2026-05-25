namespace Gojo.Core;

public sealed partial class Relation
{
    #region Local Data Shapes

    private readonly record struct CarrierSites(Carrier Carrier, long Min, long Max)
    {
        public long Width => Max - Min;

        public bool IsOrigin => Width == 0 && Min == 0;
    }

    private readonly record struct CarrierFrame(Carrier Carrier, long Start, long End)
    {
        public long Extent => End - Start;
    }

    private readonly record struct SurfaceProjection(CarrierLocation Target, CarrierLocation Residue);

    private readonly record struct LiveOutputTarget(Carrier Carrier, long Ticks);

    private readonly record struct OperationSignal(
        long Ticks,
        CarrierLocation Residue,
        bool InvertsFlow,
        CarrierLocation Source);

    private readonly record struct OperationSignalFactor(OperationSignal[] Components);

    private readonly record struct OperationSignalFlow(OperationSignalFactor[] Factors);

    private readonly record struct OperationBasis(long Extent, bool IsDominant);

    private readonly record struct SlotImport(Carrier Target, long TargetTicks, CarrierLocation Residue)
    {
        public void Apply()
        {
            Target.Set(TargetTicks);
            Residue.Carrier.HoldResidueTension(Residue.Ticks);
        }
    }

    private readonly record struct StructuralSlotImport(
        CarrierLocation Target,
        long TargetTicks,
        CarrierLocation Residue)
    {
        public void Apply()
        {
            Target.Tick.Set(TargetTicks);
            Residue.Carrier.HoldResidueTension(Residue.Ticks);
        }
    }

    private readonly record struct LocationUpdate(
        Relation Relation,
        bool IsLeft,
        CarrierLocation Current,
        CarrierLocation Next);

    private readonly record struct LocationTransform(bool CanTransform, CarrierLocation Location)
    {
        public static LocationTransform Blocked => new(false, default);

        public static LocationTransform To(CarrierLocation location) => new(true, location);
    }

    /// <summary>Allocates the next physical relation identity.</summary>
    private static ulong NextId() => idCounter++;

    #endregion
}
