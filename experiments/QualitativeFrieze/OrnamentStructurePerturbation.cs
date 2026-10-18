using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An explicit observational structural patch. Earlier topology,
/// bindings and claims remain available unchanged; this does not retract
/// material already delivered by the original drawing machines.</summary>
public sealed class OrnamentStructurePerturbation
{
    // state = topology -> binding scope
    // replacement = actual old participation -> actual new participation
    // patch = (source -> question) -> ((before -> instruction) ->
    //   (after -> (replacements -> (retained -> (removed ->
    //   (introduced -> (withdrawn faces -> unresolved)))))))
    // step = (earlier event -> later event) ->
    //   (before -> (instruction -> (after -> unresolved)))
    // Event order is qualitative. No measured duration or numeric clock is
    // asserted. A withdrawn face declaration does not invent a merged face.
    public IElement Source { get; }
    public IElement Question { get; }
    public Relation BeforeTopology { get; }
    public Relation BeforeBindings { get; }
    public Relation Topology { get; }
    public Relation Bindings { get; }
    public Relation Before { get; }
    public Relation After { get; }
    public Relation Instruction { get; }
    public Relation Replacements { get; }
    public Relation Retained { get; }
    public Relation Removed { get; }
    public Relation Introduced { get; }
    public Relation WithdrawnFaces { get; }
    public Relation Unresolved { get; }
    public Relation Timeline { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Unresolved).Length == 0;

    private OrnamentStructurePerturbation(IElement source, IElement question,
        Relation beforeTopology, Relation beforeBindings, Relation topology, Relation bindings,
        Relation instruction, IEnumerable<IElement> replacements, IEnumerable<IElement> removed,
        IEnumerable<IElement> introduced, IEnumerable<IElement> withdrawnFaces, IEnumerable<IElement> unresolved)
    {
        Source = source; Question = question; BeforeTopology = beforeTopology; BeforeBindings = beforeBindings;
        Topology = topology; Bindings = bindings; Before = new(beforeTopology, beforeBindings); After = new(topology, bindings);
        Instruction = instruction; Replacements = N.S(replacements.ToArray()); Removed = N.S(removed.ToArray());
        Introduced = N.S(introduced.ToArray()); WithdrawnFaces = N.S(withdrawnFaces.ToArray());
        Unresolved = N.S(unresolved.ToArray());
        var afterParts = Parts(topology, bindings).ToHashSet(N.Participants.Instance);
        Retained = N.S(Parts(beforeTopology, beforeBindings).Where(afterParts.Contains).ToArray());
        var step = new Relation(new Relation(new Placeholder(), new Placeholder()),
            new Relation(Before, new Relation(Instruction, new Relation(After, Unresolved))));
        Timeline = N.S(step);
        var patch = new Relation(new Relation(Before, Instruction), new Relation(After,
            new Relation(Replacements, new Relation(Retained, new Relation(Removed,
                new Relation(Introduced, new Relation(WithdrawnFaces, Unresolved)))))));
        Surface = new(new Relation(source, question), new Relation(patch, Timeline));
    }

    /// <summary>Applies a formal word delta through the existing reduction law.
    /// An unsettled reduction stays a failed patch proposal; ReplaceWord can
    /// explicitly publish an open word instead.</summary>
    public static OrnamentStructurePerturbation WordDelta(QualitativeMotionFrame frame, IElement source,
        Relation topology, Relation bindings, Relation actualBinding, Relation deltaWord, IElement question)
    {
        if (actualBinding.Right is not Relation oldWord)
            return Pending(source, topology, bindings, new(actualBinding, deltaWord), question);
        var reduction = frame.Reduce(oldWord, deltaWord);
        if (!frame.TryReadResult(reduction, out var remaining, out _, out var unknown) ||
            !N.Read(remaining, out _) || N.E(unknown).Length != 0)
            return Pending(source, topology, bindings, new(new Relation(actualBinding, deltaWord), reduction), question);
        return Replace(source, topology, bindings, actualBinding, remaining, question,
            new(new Relation(actualBinding, deltaWord), reduction));
    }

    /// <summary>Rebinds the same role to an explicitly supplied word, possibly
    /// open. This is a structural retargeting proposal, not carrier assignment.</summary>
    public static OrnamentStructurePerturbation ReplaceWord(IElement source, Relation topology, Relation bindings,
        Relation actualBinding, Relation replacementWord, IElement question) =>
        Replace(source, topology, bindings, actualBinding, replacementWord, question,
            new(actualBinding, replacementWord));

    private static OrnamentStructurePerturbation Replace(IElement source, Relation topology, Relation bindings,
        Relation actualBinding, Relation replacementWord, IElement question, Relation instruction)
    {
        if (!N.Syntax(bindings) || !N.E(bindings).Any(row => ReferenceEquals(row, actualBinding)) ||
            N.E(bindings).OfType<Relation>().Count(row => N.Same(row.Left, actualBinding.Left)) != 1)
            return Pending(source, topology, bindings, instruction, question);
        var next = new Relation(actualBinding.Left, replacementWord);
        var after = N.Scope(N.Read(bindings, out _), N.E(bindings).Select(row =>
            ReferenceEquals(row, actualBinding) ? next : row).ToArray());
        var unresolved = new List<IElement>();
        if (!N.Read(bindings, out _)) unresolved.Add(bindings);
        return new(source, question, topology, bindings, topology, after, instruction,
            [new Relation(actualBinding, next)], [actualBinding], [next], [], unresolved);
    }

    public static OrnamentStructurePerturbation WithdrawEdge(IElement source, Relation topology,
        Relation bindings, Relation actualEdgeRow, IElement question)
    {
        var instruction = new Relation(actualEdgeRow, N.S());
        if (!ReadParts(topology, out var sites, out var edges, out var faces) ||
            !ActualEdge(actualEdgeRow, edges)) return Pending(source, topology, bindings, instruction, question);
        var retired = N.E(faces).Where(face => Uses(face, actualEdgeRow.Left)).ToArray();
        var activeFaces = N.Scope(N.Read(faces, out _), N.E(faces).Where(face => !retired.Any(old => ReferenceEquals(old, face))).ToArray());
        var activeEdges = N.Scope(N.Read(edges, out _), N.E(edges).Where(row => !ReferenceEquals(row, actualEdgeRow)).ToArray());
        var after = new Relation(sites, new Relation(activeEdges, activeFaces));
        var unknown = OpenInputs(sites, edges, faces, bindings);
        return new(source, question, topology, bindings, after, bindings, instruction, [],
            [actualEdgeRow, .. retired], [], retired, unknown);
    }

    /// <summary>Splits one advertised endpoint into a fresh role carrying the
    /// same published word. Equal projected position supplies no joining law.</summary>
    public static OrnamentStructurePerturbation DetachEndpoint(IElement source, Relation topology,
        Relation bindings, Relation actualEdgeRow, IElement actualEndpointRole, IElement question)
    {
        var instruction = new Relation(actualEdgeRow, actualEndpointRole);
        if (!ReadParts(topology, out var sites, out var edges, out var faces) || !ActualEdge(actualEdgeRow, edges) ||
            actualEdgeRow.Right is not Relation ends ||
            !N.Same(ends.Left, actualEndpointRole) && !N.Same(ends.Right, actualEndpointRole))
            return Pending(source, topology, bindings, instruction, question);
        var fresh = new Placeholder();
        var nextEdge = new Relation(actualEdgeRow.Left, new Relation(
            N.Same(ends.Left, actualEndpointRole) ? fresh : ends.Left,
            N.Same(ends.Right, actualEndpointRole) ? fresh : ends.Right));
        var retired = N.E(faces).Where(face => Uses(face, actualEdgeRow.Left)).ToArray();
        var afterSites = N.Scope(N.Read(sites, out _), [.. N.E(sites), fresh]);
        var afterEdges = N.Scope(N.Read(edges, out _), N.E(edges).Select(row => ReferenceEquals(row, actualEdgeRow) ? nextEdge : row).ToArray());
        var afterFaces = N.Scope(N.Read(faces, out _), N.E(faces).Where(face => !retired.Any(old => ReferenceEquals(old, face))).ToArray());
        var after = new Relation(afterSites, new Relation(afterEdges, afterFaces));
        var introduced = new List<IElement> { fresh, nextEdge };
        var replacements = new List<IElement> { new Relation(actualEdgeRow, nextEdge) };
        var unknown = OpenInputs(sites, edges, faces, bindings).ToList();
        var offers = N.E(bindings).OfType<Relation>().Where(row => N.Same(row.Left, actualEndpointRole)).ToArray();
        var nextBindings = bindings;
        if (offers is [var binding])
        {
            var copy = new Relation(fresh, binding.Right);
            nextBindings = N.Scope(N.Read(bindings, out _), [.. N.E(bindings), copy]);
            introduced.Add(copy);
            // The old binding still serves its old role. This is an explicit
            // observed copy, not a replacement of the supplier participation.
            instruction = new(instruction, new Relation(binding, copy));
        }
        else unknown.Add(new Relation(actualEndpointRole, bindings));
        return new(source, question, topology, bindings, after, nextBindings, instruction,
            replacements, [actualEdgeRow, .. retired], introduced, retired, unknown);
    }

    public Relation Evaluate() => Surface;

    private static OrnamentStructurePerturbation Pending(IElement source, Relation topology, Relation bindings,
        Relation instruction, IElement question) => new(source, question, topology, bindings, topology, bindings,
            instruction, [], [], [], [], [new Relation(instruction, new Relation(topology, bindings))]);

    private static bool ActualEdge(Relation edge, Relation scope) =>
        N.E(scope).Any(row => ReferenceEquals(row, edge)) &&
        N.E(scope).OfType<Relation>().Count(row => N.Same(row.Left, edge.Left)) == 1;
    private static bool Uses(IElement item, IElement identity) => item is Relation { Right: Relation boundary } &&
        N.E(boundary).OfType<Relation>().Any(dart => N.Same(dart.Left, identity));
    private static IEnumerable<IElement> OpenInputs(params Relation[] inputs) => inputs.Where(scope => !N.Read(scope, out _));
    private static bool ReadParts(Relation topology, out Relation sites, out Relation edges, out Relation faces)
    {
        sites = edges = faces = null!;
        if (topology is not { Left: Relation s, Right: Relation { Left: Relation e, Right: Relation f } } ||
            !N.Syntax(s) || !N.Syntax(e) || !N.Syntax(f)) return false;
        sites = s; edges = e; faces = f; return true;
    }
    private static IEnumerable<IElement> Parts(Relation topology, Relation bindings) =>
        ReadParts(topology, out var sites, out var edges, out var faces)
            ? N.E(sites).Concat(N.E(edges)).Concat(N.E(faces)).Concat(N.E(bindings)) : N.E(bindings);
}
