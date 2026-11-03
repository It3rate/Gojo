using static Gojo.Experiments.PrimitiveMeasure.PenHeading;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>The names address authored experiments. Readers receive only the connected ink.</summary>
public static class CurvedCapitals
{
    private static PenSpan Line(int a, int b, PenHeading along) => new(a, b, along, along, 0);
    private static PenSpan Clockwise(int a, int b, PenHeading enter, PenHeading leave) => new(a, b, enter, leave, 1);
    private static PenSpan Counter(int a, int b, PenHeading enter, PenHeading leave) => new(a, b, enter, leave, 3);
    private static PenSpan Arc(int a, int b, PenHeading enter, PenHeading leave, int part) => new(a, b, enter, leave, null, part);
    private static readonly PenSpan Stem = Line(1, 0, Up), Bowl = Clockwise(0, 1, Right, Left);
    private static readonly PenSpan OpenBow = Counter(0, 1, UpLeft, UpRight);
    private static readonly ContourPattern Whole = RepetitionExamples.Contour(1, true, false);
    private static readonly ContourPattern Halves = RepetitionExamples.Contour(2, true, false);
    private static readonly ContourPattern Cut = RepetitionExamples.Contour(2, false, false);
    private static readonly (string Name, CurveInk Ink)[] Examples =
    [
        ("D", new([Stem, Bowl], 2, (1,2))),
        ("D · corners", new([Stem, Line(0,2,Right), Line(2,3,Down), Line(3,1,Left)], 4, (1,4))),
        ("D · half of O", new([Stem, Arc(0,1,Right,Left,0), Arc(1,0,Left,Right,1)], 2, (1,2), Halves, Up, sourceFirst:1)),
        ("D · ends only", new([Stem, new(0,1,Right,Left,null)], 2, (1,2))),
        ("C", new([OpenBow], 1, (0,1))),
        ("C · gap in O", new([Arc(0,1,UpLeft,UpRight,0), Arc(1,0,UpRight,UpLeft,1)], 1, (0,1), Cut, UpRight, sourceFirst:0)),
        ("C · closure sought", new([OpenBow], 1, (0,1), closureGoal:0)),
        ("O", new([Arc(0,0,Right,Right,0)], 1, (0,1), Whole, Up)),
        ("B", new([Line(2,1,Up), Line(1,0,Up), Bowl, Clockwise(1,2,Right,Left)], 4, (2,3))),
        ("G", new([OpenBow, Line(1,2,Up), Line(2,3,Left)], 3, (0,1))),
        ("J", new([Line(0,1,Down), Clockwise(1,2,Down,Up)], 2, (0,2))),
        ("P", new([Line(2,1,Up), Line(1,0,Up), Bowl], 3, (2,3))),
        ("Q", new([Arc(0,0,DownLeft,DownLeft,0), Line(0,1,DownRight)], 2, (0,1), Whole, DownRight)),
        ("R", new([Line(2,1,Up), Line(1,0,Up), Bowl, Line(1,3,DownRight)], 4, (2,3))),
        ("S", new([Counter(0,1,UpLeft,DownRight), Clockwise(1,2,DownRight,UpLeft)], 2, (0,2))),
        ("U", new([Line(0,1,Down), Counter(1,2,Down,Up), Line(2,3,Up)], 3, (0,3))),
        ("Straight · closure sought", new([Line(0,1,Right)], 1, (0,1), closureGoal:0))
    ];
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(Examples.Select(e => e.Name).ToArray());
    // These catalogue models are fixed. Compute their read surfaces once.
    private static readonly (string Name, PenRead Ink, PenOrigin Origin)[] Models = Examples
        .Select(e => (e.Name, Ink:e.Ink.Read(), Origin:e.Ink.ReadOrigin()))
        .Where(e => e.Ink.BendSupport != 3).ToArray();
    public static CurveInk Ink(int index) => Examples[index].Ink;
    public static CurveInk Ink(string name) => Examples.Single(e => e.Name == name).Ink;
    public static string[] Compatible(PenRead observation, PenOrigin? origin = null) => Models
        .Where(e => observation.Fits(e.Ink) && (!origin.HasValue || origin == e.Origin))
        .Select(e => e.Name).ToArray();
}
