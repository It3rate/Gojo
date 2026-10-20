using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Authored native observations for local inference. Captions and IDs
/// are presentation only. Position words expose declared support, never hidden
/// authoring paths or raster geometry.</summary>
public sealed class JunctionInterpretationStudy
{
    public sealed record Case(string Id, string Name, string Description, string ComparisonGroup,
        QualitativeMotionFrame Frame, QualitativeStrokeGraph Graph, Relation Bindings, QualitativeJunctionEvidence Evidence)
    {
        public Relation Read => Evidence.Evaluate();
        public Relation Surface => new(Evidence.Surface, Read);
    }
    public IReadOnlyList<Case> Cases { get; }
    public Relation Surface { get; }
    private JunctionInterpretationStudy(IReadOnlyList<Case> cases)
    { Cases = cases; Surface = S(cases.Select(c => (IElement)c.Surface).ToArray()); }

    public static JunctionInterpretationStudy Build()
    {
        var f = new Fixture();
        return new([
            f.Build("J01", "Crossing without a junction", "Two complete strokes cross in the declared affine support. They have four endpoint sites and two physical components. Both proposed depth orders remain unresolved; neither creates a shared site.", "cross",
                [(-2, 0), (2, 0), (0, -2), (0, 2)], [(0, 1), (2, 3)]),
            f.Build("J02", "One actual four-arm junction", "The same visible support is split into four strokes meeting at one actual shared site. A connection query sees one component and six incident stroke pairs. The projected-crossing reader need not invent an extra intersection.", "cross",
                [(0, 0), (-2, 0), (2, 0), (0, -2), (0, 2)], [(0, 1), (0, 2), (0, 3), (0, 4)]),
            f.Build("J03", "Actual branching Y", "Three strokes share a site with no opposed ray pair. This is actual branching topology; its Y-like read does not claim historical growth, botanical identity, or ownership by one original object.", "branch",
                [(0, 0), (0, 2), (-2, -2), (2, -2)], [(0, 1), (0, 2), (0, 3)]),
            f.Build("J04", "Actual T junction", "One shared site has three incident rays, including one opposed pair. The branch is known; an absorbing or transmitting flow law would still need a separate connection contract.", "tee",
                [(0, 0), (-2, 0), (2, 0), (0, -2)], [(0, 1), (0, 2), (0, 3)]),
            f.Build("J05", "A stem merely reaches the rail", "The picture agrees with the T, but the rail has no middle site and the stem endpoint remains independent. Formal contact support is evidence for a possible reading, not authority to split the rail or weld sites.", "tee",
                [(-2, 0), (2, 0), (0, -2), (0, 0)], [(0, 1), (2, 3)]),
            f.Build("J06", "Gap with an apparent overpass", "Facing collinear endpoints support a proposed continuation. A separate transverse strand occupies the gap, supporting an occlusion hypothesis. Deliberate cut endpoints remain a competing candidate; no hidden line is asserted.", "gap",
                [(-3, 0), (-1, 0), (1, 0), (3, 0), (0, -2), (0, 2)], [(0, 1), (2, 3), (4, 5)]),
            f.Build("J07", "Offset gap refuses collinear completion", "The separated horizontal strands are offset in the other axis. The blocked witness retains that displacement; a same-direction impression cannot establish a straight hidden continuation.", "gap",
                [(-3, 0), (-1, 0), (1, 1), (3, 1), (0, -2), (0, 2)], [(0, 1), (2, 3), (4, 5)]),
            f.Build("J08", "Angular crossing remains open", "Both strokes carry two-axis displacement. This narrow reader deliberately supplies no general angular-intersection solver. The source is complete while that selected question remains open.", "open",
                [(-2, -2), (2, 2), (-2, 2), (2, -2)], [(0, 1), (2, 3)])
        ]);
    }

    private sealed class Fixture
    {
        private readonly QualitativeMotionFrame frame = new();
        private readonly IElement horizontal = new Placeholder();
        private readonly IElement vertical = new Placeholder();
        public Case Build(string id, string name, string description, string comparison,
            (int X, int Y)[] points, (int From, int To)[] lines)
        {
            // Integers here author repetitions of shared unmeasured reaches.
            // No integer coordinate or carrier survives in the native surface.
            var sites = points.Select(_ => (IElement)new Placeholder()).ToArray();
            var bindings = S(points.Select((p, i) => (IElement)new Relation(sites[i], Word(p.X, p.Y))).ToArray());
            var strokes = S(lines.Select(line => (IElement)QualitativeStrokeGraph.Stroke(new Placeholder(), sites[line.From], sites[line.To],
                Word(points[line.To].X - points[line.From].X, points[line.To].Y - points[line.From].Y))).ToArray());
            var graph = new QualitativeStrokeGraph(frame, S(sites), strokes);
            return new(id, name, description, comparison, frame, graph, bindings, new(graph, bindings));
        }
        private Relation Word(int x, int y) => S([
            .. Enumerable.Range(0, Math.Abs(x)).Select(_ => (IElement)QualitativeMotionFrame.Move(x < 0 ? frame.Left : frame.Right, horizontal)),
            .. Enumerable.Range(0, Math.Abs(y)).Select(_ => (IElement)QualitativeMotionFrame.Move(y < 0 ? frame.Down : frame.Up, vertical))]);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
