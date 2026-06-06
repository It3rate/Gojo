namespace GojoCAD.GCode;

/// <summary>
/// Parser-side resolution knobs. The returned Gojo structure carries the
/// chosen basis ticks, so changing these values changes the encoded basis
/// rather than adding a render-only scale.
/// </summary>
public sealed record GCodeParseOptions(
    long CoordinateTicksPerUnit = 1000,
    long ArcTicksPerTurn = 3600,
    double PenDownZ = 0d)
{
    public long SafeCoordinateTicksPerUnit => Math.Max(1, CoordinateTicksPerUnit);

    public long SafeArcTicksPerTurn => Math.Max(4, ArcTicksPerTurn);
}
