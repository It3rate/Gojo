using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A small authored grammar of unmeasured, fourfold motif contours.
/// Every revision retains its actual path words, exposed boundary roles and
/// word-transform evidence. The family string selects construction only; the
/// resulting native structure carries every drawing and boundary choice.</summary>
public sealed class AdaptiveMotifGrammar
{
    // paths = Closed(path...), path = start-word -> Closed(actual chord words)
    // ports = Closed(role -> (actual path -> actual chord word))
    // A boundary publication belongs BEFORE that actual chord occurrence.
    // template = frame.Surface -> ((paths -> ports) -> construction)
    // correspondence = (plus.Paths -> minus.Paths) ->
    //                  ((path-map -> chord-map) -> (role-map -> transform-reads))
    // Chords declare simultaneous displacement. Their containing closed path
    // declares drawing order. Its last site is joined to its start explicitly
    // by the receiver of this grammar, never by coordinate coincidence.
    public QualitativeMotionFrame Frame { get; }
    public IElement Reach { get; }
    public IElement East { get; } = new Placeholder();
    public IElement North { get; } = new Placeholder();
    public IElement West { get; } = new Placeholder();
    public IElement South { get; } = new Placeholder();
    public Template Plus { get; }
    public Template Minus { get; }
    public QualitativePatternTransform TurnTransform { get; }
    public QualitativePatternTransform MirrorTransform { get; }
    public Relation Turn { get; }
    public Relation Mirror { get; }
    public Relation Correspondence { get; }
    public Relation ClosureChecks { get; }
    public Relation Surface { get; }

    public sealed record Template(string Name, Relation Paths, Relation Ports, Relation Surface);

    public AdaptiveMotifGrammar(QualitativeMotionFrame frame, IElement reach, string family)
    {
        Frame = frame; Reach = reach;
        Turn = S(new Relation(Frame.Right, Frame.Up), new Relation(Frame.Up, Frame.Left),
            new Relation(Frame.Left, Frame.Down), new Relation(Frame.Down, Frame.Right));
        // Reflection in the horizontal reference axis: the positive horizontal
        // face is retained while the two vertical faces exchange.
        Mirror = S(new Relation(Frame.Right, Frame.Right), new Relation(Frame.Up, Frame.Down),
            new Relation(Frame.Left, Frame.Left), new Relation(Frame.Down, Frame.Up));
        TurnTransform = Transform(Turn); MirrorTransform = Transform(Mirror);
        var orbitEvidence = new List<IElement>(); var closed = new List<IElement>();
        var directions = new QualitativeActionAlgebra(S(Frame.Right, Frame.Up, Frame.Left, Frame.Down));
        var turningLaw = directions.Compare(S(Turn, Turn, Turn, Turn), S());
        var mirrorLaw = directions.Compare(S(Mirror, Mirror), S());
        var reverseTurnLaw = directions.Compare(S(Mirror, Turn, Mirror), S(Turn, Turn, Turn));

        // Each quadrant starts at an actual tangentially offset boundary port.
        // The same shared reach participates repeatedly; no measured position
        // or magnitude is supplied. Rotating the entire quadrant four times
        // creates one continuous contour.
        Relation[] arc = family switch
        {
            "cushion" => [W(Frame.Left), W(Frame.Up), C(W(Frame.Left), W(Frame.Up)),
                W(Frame.Left, 2), W(Frame.Up), W(Frame.Left)],
            "key" => [W(Frame.Left), W(Frame.Up, 2), W(Frame.Left, 2), W(Frame.Down),
                W(Frame.Right), W(Frame.Down), W(Frame.Left, 2), W(Frame.Up, 3), W(Frame.Left)],
            "rosette" => [W(Frame.Left), C(W(Frame.Left), W(Frame.Up)), C(W(Frame.Right), W(Frame.Up)),
                W(Frame.Left, 2), C(W(Frame.Left), W(Frame.Down)), W(Frame.Up, 2), W(Frame.Left)],
            _ => throw new ArgumentException("Choose an authored cushion, key or rosette grammar.", nameof(family))
        };
        var outerStart = C(W(Frame.Right, 4), W(Frame.Up));
        var outer = Orbit(outerStart, arc, out var outerStarts);
        var plusPaths = new List<IElement> { outer };
        if (family != "key")
        {
            var innerReach = family == "cushion" ? 2 : 1;
            var innerArc = C(W(Frame.Left, innerReach), W(Frame.Up, innerReach));
            plusPaths.Add(Orbit(W(Frame.Right, innerReach), [innerArc], out _));
        }
        var plusScope = S(plusPaths.ToArray());
        var plusRoles = new[] { East, North, West, South };
        var plusPorts = S(outerStarts.Select((chord, i) => (IElement)new Relation(plusRoles[i], new Relation(outer, chord))).ToArray());
        var orbitProof = new Relation(new Relation(TurnTransform.Surface, directions.Surface),
            new Relation(S(turningLaw, mirrorLaw, reverseTurnLaw), S(orbitEvidence.ToArray())));
        Plus = TemplateOf(family + " / positive handedness", plusScope, plusPorts, orbitProof);

        var pathMap = new List<IElement>(); var chordMap = new List<IElement>(); var mirrorReads = new List<IElement>();
        var minusPaths = new List<IElement>(); var mirrored = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        foreach (var path in plusPaths.Cast<Relation>())
        {
            var startRead = MirrorTransform.Apply((Relation)path.Left);
            if (!MirrorTransform.TryReadComplete(startRead, out var start)) throw new InvalidOperationException("An authored mirror must map the entire start word.");
            mirrorReads.Add(startRead);
            var words = new List<IElement>();
            foreach (var source in E((Relation)path.Right).Cast<Relation>())
            {
                var read = MirrorTransform.Apply(source);
                if (!MirrorTransform.TryReadComplete(read, out var image)) throw new InvalidOperationException("An authored mirror must map the entire chord word.");
                words.Add(image); mirrored.Add(source, image); chordMap.Add(new Relation(source, image)); mirrorReads.Add(read);
            }
            var target = new Relation(start, S(words.ToArray()));
            minusPaths.Add(target); pathMap.Add(new Relation(path, target)); Closure(target);
        }
        var minusScope = S(minusPaths.ToArray()); var minusOuter = (Relation)minusPaths[0];
        var minusRoles = new[] { East, South, West, North };
        var minusPorts = S(outerStarts.Select((chord, i) => (IElement)new Relation(minusRoles[i], new Relation(minusOuter, mirrored[chord]))).ToArray());
        var roleMap = S(new Relation(East, East), new Relation(North, South), new Relation(West, West), new Relation(South, North));
        Correspondence = new Relation(new Relation(plusScope, minusScope),
            new Relation(new Relation(S(pathMap.ToArray()), S(chordMap.ToArray())), new Relation(roleMap, S(mirrorReads.ToArray()))));
        Minus = TemplateOf(family + " / reflected handedness", minusScope, minusPorts,
            new Relation(MirrorTransform.Surface, Correspondence));
        ClosureChecks = S(closed.ToArray());
        Surface = new Relation(new Relation(Frame.Surface, Reach),
            new Relation(S(East, North, West, South),
                new Relation(new Relation(Plus.Surface, Minus.Surface),
                    new Relation(new Relation(orbitProof, Correspondence), ClosureChecks))));

        Template TemplateOf(string name, Relation paths, Relation ports, IElement proof) =>
            new(name, paths, ports, new Relation(Frame.Surface, new Relation(new Relation(paths, ports), proof)));

        Relation Orbit(Relation start, Relation[] firstArc, out Relation[] starts)
        {
            var chords = new List<IElement>(); var arcStarts = new List<Relation>(); var selected = firstArc;
            for (var quadrant = 0; quadrant < 4; quadrant++)
            {
                arcStarts.Add(selected[0]); chords.AddRange(selected);
                var next = new List<Relation>();
                foreach (var word in selected)
                {
                    var read = TurnTransform.Apply(word);
                    if (!TurnTransform.TryReadComplete(read, out var output)) throw new InvalidOperationException("An authored quarter turn must map the entire chord word.");
                    next.Add(output); orbitEvidence.Add(read);
                }
                selected = next.ToArray();
            }
            // The fourth transform read is retained above. Compare its image
            // with the original arc instead of replacing the original words.
            for (var i = 0; i < firstArc.Length; i++)
                orbitEvidence.Add(new Relation(new Relation(selected[i], firstArc[i]), Frame.Reduce(selected[i], Inverse(firstArc[i]))));
            var path = new Relation(start, S(chords.ToArray()));
            Closure(path); starts = arcStarts.ToArray(); return path;
        }

        void Closure(Relation path)
        {
            var all = S(E((Relation)path.Right).Cast<Relation>().SelectMany(E).ToArray());
            var read = Frame.Reduce(S(), all);
            if (!Frame.TryReadResult(read, out var rest, out _, out var pending) || E(rest).Length != 0 || E(pending).Length != 0)
                throw new InvalidOperationException("An authored motif contour must have formal shared-reach closure.");
            closed.Add(new Relation(path, read));
        }
    }

    private QualitativePatternTransform Transform(Relation directionMap) => new(Frame, Frame,
        S(E(directionMap).Cast<Relation>().Select(row => (IElement)new Relation(
            QualitativeMotionFrame.Move(row.Left, Reach), S(QualitativeMotionFrame.Move(row.Right, Reach)))).ToArray()), directionMap);
    private Relation W(IElement direction, int count = 1) =>
        S(Enumerable.Range(0, count).Select(_ => (IElement)QualitativeMotionFrame.Move(direction, Reach)).ToArray());
    private static Relation C(params Relation[] words) => S(words.SelectMany(E).ToArray());
    private Relation Inverse(Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(Frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
