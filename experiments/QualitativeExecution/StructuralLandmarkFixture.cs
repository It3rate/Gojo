using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// Experimental ordered landmark and glyph-like incidence patterns. There is
/// no midpoint, length, pixel geometry or font classifier in this surface.
/// </summary>
public sealed class StructuralLandmarkFixture
{
    private readonly Placeholder instanceSlot = new();
    private readonly Placeholder landmarkSlot = new();
    private readonly Placeholder beginningSlot = new();
    private readonly Placeholder interiorSlot = new();
    private readonly Placeholder endSlot = new();
    private readonly Placeholder farBeginningSlot = new();
    private readonly Placeholder farInteriorSlot = new();
    private readonly Placeholder farEndSlot = new();
    private readonly Placeholder strokesSlot = new();
    private readonly Placeholder frameEvidenceSlot = new();
    private readonly Relation rolePattern;
    private readonly Relation roleResult;

    public IElement Beginning { get; }
    public IElement Interior { get; }
    public IElement End { get; }
    public Relation Frame { get; }
    public Relation LandmarkBinding { get; }
    public Relation FrameQualification { get; }
    public Relation LocalRoleConstraints { get; }
    public Relation Variables { get; }
    public Relation ETemplate { get; }
    public Relation HTemplate { get; }
    public Relation BTemplate { get; }
    public Relation Surface { get; }

    public StructuralLandmarkFixture(IElement? beginning = null,
        IElement? interior = null, IElement? end = null)
    {
        Beginning = beginning ?? new Placeholder();
        Interior = interior ?? new Placeholder();
        End = end ?? new Placeholder();
        // Frame = (beginning -> interior) -> (interior -> end).
        // This ordered subdivision selects an interior landmark, not halfway.
        Frame = new(new Relation(Beginning, Interior), new Relation(Interior, End));
        LandmarkBinding = new(landmarkSlot, Interior);
        // Three explicit identity comparisons qualify the strict role path.
        // A collapsed or looped path remains structure, but cannot promise
        // that its chosen landmark lies strictly between distinct endpoints.
        FrameQualification = new(Frame, StructuralConstraint.Closed(
            Compare(Beginning, Interior), Compare(Interior, End), Compare(Beginning, End)));
        Variables = StructuralConstraint.Closed(instanceSlot, landmarkSlot,
            beginningSlot, interiorSlot, endSlot, farBeginningSlot,
            farInteriorSlot, farEndSlot, strokesSlot, frameEvidenceSlot);
        IElement[] localRoles = [beginningSlot, interiorSlot, endSlot,
            farBeginningSlot, farInteriorSlot, farEndSlot];
        var separationObligations = new List<IElement>();
        for (var i = 0; i < localRoles.Length; i++)
            for (var j = 0; j < i; j++)
                separationObligations.Add(new Relation(StructuralConstraint.Request(localRoles[i],
                    StructuralConstraint.Closed(), StructuralConstraint.Closed(localRoles[j])),
                    StructuralConstraint.Closed()));
        // Each native identity query is required to have empty candidate support.
        LocalRoleConstraints = StructuralConstraint.Closed(separationObligations.ToArray());
        var axis = new Relation(new Relation(beginningSlot, interiorSlot),
            new Relation(interiorSlot, endSlot));
        var roleMaps = new Relation(new Relation(Beginning, beginningSlot),
            new Relation(new Relation(landmarkSlot, interiorSlot), new Relation(End, endSlot)));
        // placement = shared frame -> (role correspondences -> local axis).
        // The same local axis also participates in each stroke surface.
        var placement = new Relation(Frame, new Relation(roleMaps, axis));
        var firstBar = new Relation(beginningSlot, farBeginningSlot);
        var interiorBar = new Relation(interiorSlot, farInteriorSlot);
        var lastBar = new Relation(endSlot, farEndSlot);
        var farAxis = new Relation(new Relation(farBeginningSlot, farInteriorSlot),
            new Relation(farInteriorSlot, farEndSlot));
        ETemplate = Glyph(new Relation(axis, new Relation(firstBar, new Relation(interiorBar, lastBar))));
        HTemplate = Glyph(new Relation(axis, new Relation(farAxis, interiorBar)));
        var lowerLoop = new Relation(firstBar, new Relation(
            new Relation(farBeginningSlot, farInteriorSlot), new Relation(farInteriorSlot, interiorSlot)));
        var upperLoop = new Relation(interiorBar, new Relation(
            new Relation(farInteriorSlot, farEndSlot), new Relation(farEndSlot, endSlot)));
        BTemplate = Glyph(new Relation(axis, new Relation(lowerLoop, upperLoop)));
        rolePattern = Glyph(strokesSlot);
        roleResult = new(new Relation(beginningSlot, new Relation(landmarkSlot, interiorSlot)), endSlot);
        Surface = new(new Relation(FrameQualification, LandmarkBinding), new Relation(Variables,
            new Relation(LocalRoleConstraints, new Relation(ETemplate, new Relation(HTemplate, BTemplate)))));

        Relation Glyph(IElement strokes) => new(instanceSlot, new Relation(placement, strokes));
    }

    /// <summary>Instantiates a selected native pattern through explicit bindings.
    /// The return value is the generation witness, not an opaque glyph object.</summary>
    public Relation Generate(Relation template, IElement? localInterior = null,
        params Relation[] additionalBindings)
    {
        if (!DistinctFrameRoles()) return Unavailable(template, StructuralConstraint.Closed());
        var instance = new Placeholder();
        var bindings = new[]
        {
            new Relation(instanceSlot, instance), LandmarkBinding,
            new Relation(frameEvidenceSlot, FrameQualification),
            new Relation(beginningSlot, new Placeholder()),
            new Relation(interiorSlot, localInterior ?? new Placeholder()),
            new Relation(endSlot, new Placeholder()),
            new Relation(farBeginningSlot, new Placeholder()),
            new Relation(farInteriorSlot, new Placeholder()),
            new Relation(farEndSlot, new Placeholder())
        };
        var bound = StructuralConstraint.Match(instanceSlot, Variables,
            StructuralConstraint.Closed(instance), [.. bindings, .. additionalBindings]);
        return QualifyLocalRoles(StructuralConstraint.Project(bound, template));
    }

    /// <summary>Recognizes fresh native instances with the generation pattern.
    /// The selected shared landmark is fixed; local sites remain variables.</summary>
    public Relation Recognize(Relation template, Relation support) => DistinctFrameRoles()
        ? QualifyLocalRoles(StructuralConstraint.Match(template, Variables, support,
            LandmarkBinding, new Relation(frameEvidenceSlot, FrameQualification)))
        : Unavailable(template, support);

    /// <summary>Projects (local beginning -> (shared landmark -> local interior))
    /// -> local end, retaining recognition and correspondence provenance.</summary>
    public Relation ReadRoles(Relation glyph) => StructuralConstraint.Project(
        Recognize(rolePattern, StructuralConstraint.Closed(glyph)), roleResult);

    private static Relation Compare(IElement first, IElement second) =>
        StructuralConstraint.Match(first, StructuralConstraint.Closed(), StructuralConstraint.Closed(second));

    private bool DistinctFrameRoles() => FiniteLaw.Entries((Relation)FrameQualification.Right).All(comparison =>
        comparison is Relation read && StructuralConstraint.TryRead(read, out _, out var same, out _, out var open) &&
        FiniteLaw.Entries(same).Length == 0 && FiniteLaw.Entries(open).Length == 0);

    private Relation Unavailable(Relation template, Relation support) =>
        StructuralConstraint.Match(template, Variables,
            StructuralConstraint.Open(new Relation(support, FrameQualification)), LandmarkBinding);

    // General matching intentionally permits two variables to bind one site.
    // These particular incidence templates additionally promise distinct local
    // sites. Qualify that promise with native comparisons, retaining failures.
    private Relation QualifyLocalRoles(Relation read)
    {
        if (!StructuralConstraint.TryRead(read, out _, out var candidates, out var blocked, out var unresolved))
            return read;
        var accepted = new List<IElement>();
        var denied = FiniteLaw.Entries(blocked).ToList();
        var pending = FiniteLaw.Entries(unresolved).ToList();
        foreach (var entry in FiniteLaw.Entries(candidates))
        {
            if (entry is not Relation candidate ||
                !StructuralConstraint.TryReadCandidate(candidate, out _, out _, out var bindings))
            { pending.Add(entry); continue; }
            var supplied = FiniteLaw.Entries(bindings).OfType<Relation>().ToArray();
            var comparisons = new List<IElement>();
            var distinct = true;
            var incomplete = false;
            foreach (var obligation in FiniteLaw.Entries(LocalRoleConstraints))
            {
                if (obligation is not Relation { Left: Relation query, Right: Relation expected } ||
                    query.Left is not Relation { Left: Relation definition } ||
                    query.Right is not Relation offers || !FiniteLaw.ClosedEntries(offers, out var other) ||
                    other.Length != 1 || !FiniteLaw.ClosedEntries(expected, out var expectedCandidates) ||
                    expectedCandidates.Length != 0)
                { comparisons.Add(obligation); incomplete = true; continue; }
                var first = supplied.SingleOrDefault(binding => FiniteLaw.SameParticipant(binding.Left, definition.Left));
                var second = supplied.SingleOrDefault(binding => FiniteLaw.SameParticipant(binding.Left, other[0]));
                // A coarser role projection does not claim unexposed far sites.
                if (first is null || second is null) continue;
                var comparison = Compare(first.Right, second.Right);
                comparisons.Add(new Relation(obligation, comparison));
                if (!StructuralConstraint.TryRead(comparison, out _, out var same, out _, out var open) ||
                    FiniteLaw.Entries(open).Length != 0) incomplete = true;
                else if (FiniteLaw.Entries(same).Length != 0) distinct = false;
            }
            var qualification = new Relation(new Relation(candidate, LocalRoleConstraints),
                StructuralConstraint.Closed(comparisons.ToArray()));
            if (incomplete) pending.Add(qualification);
            else if (distinct) accepted.Add(new Relation(qualification, candidate.Right));
            else denied.Add(qualification);
        }
        return new Relation(read.Left, new Relation(StructuralConstraint.Closed(accepted.ToArray()),
            new Relation(StructuralConstraint.Closed(denied.ToArray()), StructuralConstraint.Closed(pending.ToArray()))));
    }
}
