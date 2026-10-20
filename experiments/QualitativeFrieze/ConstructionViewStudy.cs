using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Three observation contexts consume the same actual source faces
/// and become three independently executed construction networks. Membership,
/// contact and overlap remain native relations; display colour is optional.</summary>
public sealed class ConstructionViewStudy
{
    public sealed record Variant(string Id, string Name, string Description,
        OrnamentView View, OrnamentViewComposition? Composition,
        OrnamentViewNetwork Network, Relation Bindings, Relation LandmarkReads,
        OrnamentNetworkConstruction Execution, UnitArrangementNode[] Decorations,
        Relation DecorationInputs, Relation Surface);

    public OrnamentSourcePatch Source { get; }
    public UnitArrangementProgram MotifProgram { get; }
    public Variant[] Variants { get; }
    public OrnamentViewOverlap[] Comparisons { get; }
    public Relation Surface { get; }

    public ConstructionViewStudy()
    {
        Source = OrnamentSourcePatch.SquareFans(4, 3);
        MotifProgram = MarkerProgram(Source.Frame, Source.Reach);
        var courts = new List<IElement>();
        for (var y = 0; y < Source.Rows; y++)
        for (var x = 0; x < Source.Columns; x++)
            courts.Add(OrnamentView.Group(new Placeholder(), Source.FaceRecords
                .Where(face => face.CellX == x && face.CellY == y).Select(face => face.Face).ToArray()));
        var courtView = new OrnamentView(Source.Scaffold, S(courts.ToArray()), new Placeholder());

        var diamonds = new List<IElement>();
        for (var y = 1; y < Source.Rows; y++)
        for (var x = 1; x < Source.Columns; x++)
            diamonds.Add(OrnamentView.Group(new Placeholder(), Source.FaceRecords
                .Where(face => face.LogicalVertices.Contains((x * Source.FineScale, y * Source.FineScale)))
                .Select(face => face.Face).ToArray()));
        var diamondView = new OrnamentView(Source.Scaffold, S(diamonds.ToArray()), new Placeholder());

        // Row parents consume actual accepted court READS. They are not a
        // second ownership partition inferred from the final picture.
        var childReads = E(courtView.Groups).Cast<Relation>().ToArray();
        var rowDefinitions = S(Enumerable.Range(0, Source.Rows).Select(y => (IElement)new Relation(
            new Placeholder(), S(childReads.Skip(y * Source.Columns).Take(Source.Columns).ToArray()))).ToArray());
        var rows = new OrnamentViewComposition(courtView, rowDefinitions, new Placeholder());

        Variants = [
            Build("V01", "Courts become a mesh",
                "Four actual triangular chambers form each court. Court boundary contacts become construction passages.", courtView),
            Build("V02", "Vertex diamonds become a shifted mesh",
                "Eight actual chambers meet around each interior source vertex. Neighbouring diamonds share chambers; that overlap is retained separately from boundary contact.", diamondView),
            Build("V03", "Courts become broad courses",
                "Each row consumes four actual court reads. Its contact network becomes a three-node course rather than another copy of the court mesh.", rows.View, rows)
        ];
        Comparisons = [new(courtView, diamondView, new Placeholder()),
            new(courtView, rows.View, new Placeholder()), new(diamondView, rows.View, new Placeholder())];
        Surface = new(Source.Surface, new Relation(S(Variants.Select(variant => (IElement)variant.Surface).ToArray()),
            S(Comparisons.Select(comparison => (IElement)comparison.Evaluate()).ToArray())));
    }

    private Variant Build(string id, string name, string description, OrnamentView view,
        OrnamentViewComposition? composition = null)
    {
        var network = new OrnamentViewNetwork(view, new Placeholder());
        var bindings = new List<IElement>();
        var landmarks = new List<IElement>();
        foreach (var representative in E(network.Representatives).Cast<Relation>())
        {
            if (!view.TryReadGroup((Relation)representative.Left, out var group))
                throw new InvalidOperationException("A construction representative must retain its actual source group.");
            var actualBindings = S(E(group.Sites).Select(site => (IElement)E(Source.Bindings).Cast<Relation>()
                .Single(binding => ReferenceEquals(binding.Left, site))).ToArray());
            var shares = S(E(group.Sites).Select(_ => (IElement)new Placeholder()).ToArray());
            var read = OrnamentWordLandmarks.Share(Source.Frame, actualBindings, shares);
            if (!OrnamentWordLandmarks.TryReadComplete(Source.Frame, read, out var word))
                throw new InvalidOperationException("This authored construction uses exactly representable equal-share landmarks.");
            bindings.Add(new Relation(representative.Right, word));
            landmarks.Add(new Relation(representative, read));
        }
        var positions = S(bindings.ToArray());
        var landmarkReads = S(landmarks.ToArray());
        // The actual selected read is consumed through its exposed topology;
        // the network surface remains the execution's explicit cause.
        var execution = new OrnamentNetworkConstruction(Source.Frame, network.Topology, positions, network.Surface);
        execution.Run();
        if (!execution.IsComplete)
            throw new InvalidOperationException("The authored view network must complete its acknowledged construction.");
        // One actual program is reused in all contexts. Placement consumes the
        // chosen group's published word, not a rendered or recomputed position.
        var decorations = E(positions).Cast<Relation>().Select(binding => new UnitArrangementNode(
            "shared ornament at selected representative", [MotifProgram], MotifProgram, (Relation)binding.Right)).ToArray();
        var decorationInputs = S(E(positions).Cast<Relation>().Zip(decorations).Select(pair => (IElement)new Relation(
            pair.First, new Relation(pair.Second.PlacementExecution.InputPort,
                new Relation(pair.Second.PlacementExecution.Capture(), pair.Second.Current.Surface)))).ToArray());
        var surface = new Relation(view.Evaluate(), new Relation(composition?.Evaluate() ?? S(),
            new Relation(network.Surface, new Relation(positions, new Relation(landmarkReads,
                new Relation(execution.Capture(), decorationInputs))))));
        return new(id, name, description, view, composition, network, positions, landmarkReads, execution,
            decorations, decorationInputs, surface);
    }

    private static UnitArrangementProgram MarkerProgram(QualitativeMotionFrame frame, IElement reach)
    {
        Relation W(params IElement[] directions) => S(directions.Select(direction =>
            (IElement)QualitativeMotionFrame.Move(direction, reach)).ToArray());
        var diamond = new Relation(W(frame.Right), S(W(frame.Left, frame.Up), W(frame.Left, frame.Down),
            W(frame.Right, frame.Down), W(frame.Right, frame.Up)));
        var rays = new[] { frame.Right, frame.Up, frame.Left, frame.Down }
            .Select(direction => (IElement)new Relation(S(), S(W(direction)))).ToArray();
        return new(frame, "shared diamond and radial spokes", S([diamond, .. rays]), S(), S(diamond));
    }

    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
