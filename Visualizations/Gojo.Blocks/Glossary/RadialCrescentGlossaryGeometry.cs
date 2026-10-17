namespace Gojo.Blocks;

internal static class RadialCrescentGlossaryGeometry
{
    public static (long X, long Y)[][] InteriorCuspPointGroups() =>
    [
        // The crescent body hole is cut by the two arc-band supports. The
        // central opening contributes four cusp joins, while each side slit is
        // lens-like and contributes only its two terminal cusp joins.
        [(-110, 55), (-30, 55), (-30, -55), (-110, -55)],
        [(-125, 40), (-125, -40)],
        [(-15, 40), (-15, -40)],

        // The bent-arm diamond hole splits into two central openings and two
        // side islands after the diagonal supports are subtracted.
        [(265, 68), (234, 30), (265, 6), (296, 30)],
        [(265, -6), (300, -26), (265, -68), (230, -26)],
        [(229, 16), (245, 0), (229, -16), (213, 0)],
        [(301, 16), (317, 0), (301, -16), (285, 0)]
    ];
}
