using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A non-draining, native read of one construction step. Selected
/// paths are actual old participants affected by an accepted change; introduced
/// paths are actual changed result participants. A refused edit publishes its
/// intent through the retained rung, while its accepted change is empty.
/// Captions and Kind are diagnostics; the connected scopes carry the read.</summary>
public sealed class LatticeCatalogueStepRead
{
    public UnitArrangementProgram? Source { get; }
    public UnitArrangementProgram Result { get; }
    public Relation CarriedPaths { get; }
    public Relation CarriedResultPaths { get; }
    public Relation SelectedPaths { get; }
    public Relation IntroducedPaths { get; }
    public Relation WithdrawnPorts { get; }
    public Relation Correspondence { get; }
    public Relation PathCorrespondence { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }

    /// <summary>Derived from the exact native transition contract, never the
    /// rung/program name. It supplies no execution semantics.</summary>
    public string Kind { get; }

    private LatticeCatalogueStepRead(QualitativeLatticeRung rung, string kind,
        Relation carried, Relation carriedResult, Relation selected, Relation introduced,
        Relation withdrawn, Relation pairs, Relation unresolved)
    {
        Source = rung.Source; Result = rung.Program; Kind = kind;
        CarriedPaths = carried; CarriedResultPaths = carriedResult;
        SelectedPaths = selected; IntroducedPaths = introduced; WithdrawnPorts = withdrawn;
        Correspondence = rung.Correspondence; PathCorrespondence = pairs; Unresolved = unresolved;
        // ((actual-rung -> (old-program -> current-program)) ->
        //   ((carried-old -> carried-current) -> (selected-old ->
        //     (introduced-current -> (withdrawn-publications ->
        //       (original-correspondence -> (path-pairs -> unresolved))))))))
        Surface = new(new Relation(rung.Surface, new Relation(Source?.Surface ?? S(), Result.Surface)),
            new Relation(new Relation(carried, carriedResult), new Relation(selected,
                new Relation(introduced, new Relation(withdrawn,
                    new Relation(Correspondence, new Relation(pairs, unresolved)))))));
    }

    /// <summary>Reads source/focus/result identities and the native instruction
    /// spine. Refinement focus rows select words within paths, so their enclosing
    /// path is highlighted only when at least one actual word is selected.
    /// An unchanged refinement path may have a new path wrapper; its ordered
    /// start and chord identities, with native path correspondence, justify the
    /// carried read. Coordinate equality is never consulted.</summary>
    public static LatticeCatalogueStepRead Read(QualitativeLatticeRung rung)
    {
        ArgumentNullException.ThrowIfNull(rung);
        var result = rung.Program;
        if (rung.Source is not { } source)
            return New("initial", S(), S(), S(), result.Paths, S(), S());

        var old = E(source.Paths); var current = E(result.Paths);
        if (!rung.IsComplete && ReferenceEquals(source.Surface, result.Surface))
            return New("blocked edit", source.Paths, result.Paths, S(), S(), S(), IdentityPairs(old));

        if (Instruction(rung, out var instruction))
        {
            // Native transform surface: (source-frame -> target-frame) ->
            // (substitution-rows -> evidence). The actual result supplies the
            // path/word correspondences retained by the application.
            if (instruction.Left is Relation { Left: Relation frames, Right: Relation declaration } &&
                ReferenceEquals(frames.Left, source.Frame.Surface) &&
                ReferenceEquals(frames.Right, result.Frame.Surface) && declaration.Left is Relation &&
                ReferenceEquals(rung.Focus, source.Paths) && ReferenceEquals(rung.Replacement, result.Paths) &&
                rung.Correspondence.Left is Relation transformPairs && ExactPairs(transformPairs, old, current))
                return New("word substitution", S(), S(), source.Paths, result.Paths, Withdrawn(source, result), transformPairs);

            // Native focused edit keeps source selection and carried complement
            // together. Its replacement contains complete result contracts.
            if (instruction.Left is Relation patch && patch.Left is Relation selection &&
                ReferenceEquals(selection.Left, rung.Focus) && ReferenceEquals(selection.Right, rung.Carried) &&
                ReferenceEquals(patch.Right, rung.Replacement) && instruction.Right is Relation proof &&
                ReferenceEquals(proof.Left, rung.Correspondence) &&
                rung.Replacement.Left is Relation introduced && rung.Replacement.Right is Relation
                    { Left: Relation ports, Right: Relation closed } &&
                ReferenceEquals(result.Ports, ports) && ReferenceEquals(result.ClosedPaths, closed))
                return New("focused path edit", rung.Carried, rung.Carried, rung.Focus, introduced,
                    Withdrawn(source, result), (Relation)rung.Correspondence.Left);

            // Native refinement: source -> (replacement-rows -> endpoint proofs).
            // Every path pair is explicit, including paths with no selected word.
            if (ReferenceEquals(instruction.Left, rung.Replacement) &&
                rung.Correspondence.Left is Relation refinementPairs && ExactPairs(refinementPairs, old, current) &&
                UnitArrangementRead.ClosedEntries(rung.Focus, out var focus))
            {
                var carried = new List<IElement>(); var carriedResult = new List<IElement>();
                var selected = new List<IElement>(); var introducedResult = new List<IElement>();
                var pairs = E(refinementPairs).Cast<Relation>().ToArray();
                foreach (var path in old)
                {
                    var row = focus.OfType<Relation>().SingleOrDefault(entry => ReferenceEquals(entry.Left, path));
                    if (row?.Right is not Relation words || !UnitArrangementRead.ClosedEntries(words, out var selectedWords))
                        return Unknown();
                    var next = pairs.Single(pair => ReferenceEquals(pair.Left, path)).Right;
                    if (selectedWords.Length == 0 && SamePathWords((Relation)path, (Relation)next))
                    { carried.Add(path); carriedResult.Add(next); }
                    else
                    { selected.Add(path); introducedResult.Add(next); }
                }
                return New("endpoint refinement", S(carried.ToArray()), S(carriedResult.ToArray()),
                    S(selected.ToArray()), S(introducedResult.ToArray()), Withdrawn(source, result), refinementPairs);
            }
        }
        return Unknown();

        LatticeCatalogueStepRead Unknown()
        {
            // Identity alone establishes this conservative complement. We do
            // not infer a transform from a favourable-looking output graph.
            var carried = old.Where(path => current.Any(next => ReferenceEquals(path, next))).ToArray();
            return new(rung, "unresolved transition", S(carried), S(carried), S(), S(), S(),
                IdentityPairs(carried), S(rung.Surface));
        }
        LatticeCatalogueStepRead New(string kind, Relation carried, Relation carriedResult,
            Relation selected, Relation introduced, Relation withdrawn, Relation pairs) =>
            new(rung, kind, carried, carriedResult, selected, introduced, withdrawn, pairs, S());
    }

    private static bool Instruction(QualitativeLatticeRung rung, out Relation instruction)
    {
        instruction = null!;
        if (rung.Source is not { } source || rung.Provenance is not Relation { Right: Relation native } ||
            !ReferenceEquals(native.Left, source.Surface) || native.Right is not Relation actual) return false;
        instruction = actual; return true;
    }

    private static bool ExactPairs(Relation scope, IElement[] source, IElement[] result) =>
        source.Length == result.Length && UnitArrangementRead.ClosedEntries(scope, out var pairs) && pairs.Length == source.Length &&
        pairs.Zip(source.Zip(result)).All(pair => pair.First is Relation row &&
            ReferenceEquals(row.Left, pair.Second.First) && ReferenceEquals(row.Right, pair.Second.Second));

    private static bool SamePathWords(Relation source, Relation result) =>
        ReferenceEquals(source.Left, result.Left) && source.Right is Relation a && result.Right is Relation b &&
        UnitArrangementRead.ClosedEntries(a, out var aa) && UnitArrangementRead.ClosedEntries(b, out var bb) &&
        aa.Length == bb.Length && aa.Zip(bb).All(pair => ReferenceEquals(pair.First, pair.Second));

    private static Relation Withdrawn(UnitArrangementProgram source, UnitArrangementProgram result) => S(
        E(source.Ports).Cast<Relation>().Where(old => !E(result.Ports).Cast<Relation>()
            .Any(next => Same(old.Left, next.Left))).Cast<IElement>().ToArray());

    private static Relation IdentityPairs(IElement[] paths) => S(paths.Select(path => (IElement)new Relation(path, path)).ToArray());
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
