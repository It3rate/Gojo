using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Reapplies previously induced native role graphs at their retained
/// bindings. This is a local truth read, not fresh induction or a global claim
/// that another occurrence cannot exist. No position or diagnostic name enters
/// the match. Replacement segment rows need an actual correspondence witness.</summary>
public sealed class QualitativeRecognitionRevalidation
{
    // correspondence = Closed(actual prior segment row -> actual current row)
    // definition = (prior induction read -> current observation) -> correspondence
    // check = (actual prior family -> actual prior occurrence) ->
    //         (current observation -> (supported obligations ->
    //          (refuted obligations -> (open obligations -> dependencies))))
    // An obligation connects its actual pattern role and retained binding to
    // current participation, a closed-inventory absence, or an open promise.
    // result = definition -> (supported checks -> (refuted checks ->
    //          (unresolved checks -> (input unresolved -> all checks))))
    // A check with any refutation belongs to Refuted; otherwise open obligations
    // place it in Unresolved. Complete witnessed obligations belong to Supported.
    public QualitativeMotifInduction Prior { get; }
    public QualitativeRecognitionObservation Current { get; }
    public Relation SegmentCorrespondence { get; }
    public Relation Definition { get; }
    public Relation Supported { get; }
    public Relation Refuted { get; }
    public Relation Unresolved { get; }
    public Relation InputUnresolved { get; }
    public Relation Surface { get; }
    public IReadOnlyList<QualitativeRecognitionCheck> Checks { get; }
    public bool IsComplete => N.E(Unresolved).Length == 0 && N.E(InputUnresolved).Length == 0;

    public QualitativeRecognitionRevalidation(QualitativeMotifInduction prior,
        QualitativeRecognitionObservation current, Relation? segmentCorrespondence = null)
    {
        Prior = prior; Current = current; SegmentCorrespondence = segmentCorrespondence ?? N.S();
        Definition = new(new Relation(prior.Surface, current.Surface), SegmentCorrespondence);
        var inputPending = new List<IElement>(N.E(current.Unresolved));
        var checks = new List<QualitativeRecognitionCheck>();
        var mapClosed = N.Read(SegmentCorrespondence, out var suppliedMaps);
        if (!mapClosed) inputPending.Add(SegmentCorrespondence);
        var priorRows = N.E(prior.Observation.Segments).OfType<Relation>().ToArray();
        var currentRows = N.E(current.Segments).OfType<Relation>().ToArray();
        var currentSites = N.E(current.Sites);
        var validMaps = new List<Relation>();
        foreach (var item in suppliedMaps)
        {
            if (item is not Relation { Left: Relation before, Right: Relation after } map ||
                !priorRows.Any(row => ReferenceEquals(row, before)) ||
                !currentRows.Any(row => ReferenceEquals(row, after)))
            { inputPending.Add(item); continue; }
            validMaps.Add(map);
        }
        var duplicateIdentities = currentRows.GroupBy(row => row.Left, N.Participants.Instance)
            .Where(group => group.Count() > 1).SelectMany(group => group).ToHashSet(ReferenceEqualityComparer.Instance);
        var currentSiteInventoryReady = N.Read(current.Sites, out _) && N.Distinct(currentSites);
        var currentSegmentInventoryReady = currentSiteInventoryReady && N.Read(current.Segments, out var inventory) &&
            inventory.Length == currentRows.Length && duplicateIdentities.Count == 0 &&
            currentRows.All(row => current.TryReadSegment(row, out _, out _, out _, out _));
        var compatibleFrame = ReferenceEquals(prior.Observation.Frame.Surface, current.Frame.Surface);

        foreach (var family in prior.Families)
            foreach (var occurrence in family.Occurrences)
            {
                var yes = new List<IElement>(); var no = new List<IElement>(); var open = new List<IElement>();
                var dependencies = new List<IElement>(); var matchedRows = new List<Relation>();
                dependencies.Add(family.Definition); dependencies.Add(occurrence.Read);
                if (!prior.TryReadPattern(family.Definition, out _, out _) ||
                    !TryPattern(family.Definition, out var patternSites, out var patternEdges, out var centres) ||
                    !N.Read(occurrence.SiteBindings, out var siteBindings) ||
                    !N.Read(occurrence.SegmentBindings, out var edgeBindings))
                { open.Add(new Relation(family.Definition, occurrence.Read)); Finish(); continue; }
                if (!compatibleFrame)
                    open.Add(new Relation(prior.Observation.Frame.Surface, current.Frame.Surface));

                var boundSites = new Dictionary<IElement, IElement>(N.Participants.Instance);
                foreach (var role in patternSites)
                {
                    var bindings = siteBindings.OfType<Relation>().Where(row => N.Same(row.Left, role)).ToArray();
                    if (bindings is not [var binding]) { open.Add(new Relation(role, occurrence.SiteBindings)); continue; }
                    boundSites.Add(role, binding.Right); dependencies.Add(binding);
                    if (currentSites.Any(site => N.Same(site, binding.Right)))
                        yes.Add(new Relation(binding, new Relation(current.Sites, binding.Right)));
                    else if (currentSiteInventoryReady)
                        no.Add(new Relation(binding, new Relation(current.Sites, N.S())));
                    else open.Add(new Relation(binding, new Relation(current.Sites, StructuralConstraint.Open())));
                }
                if (!N.Distinct(boundSites.Values.ToArray()))
                    open.Add(new Relation(occurrence.SiteBindings, family.Definition));

                foreach (var item in patternEdges)
                {
                    if (item is not Relation { Right: Relation { Left: Relation ends, Right: Relation expectedHeading } } patternEdge ||
                        !boundSites.TryGetValue(ends.Left, out var expectedFrom) || !boundSites.TryGetValue(ends.Right, out var expectedTo))
                    { open.Add(new Relation(item, occurrence.SiteBindings)); continue; }
                    dependencies.Add(patternEdge);
                    var bindings = edgeBindings.OfType<Relation>().Where(row => N.Same(row.Left, patternEdge.Left)).ToArray();
                    if (bindings is not [Relation { Right: Relation oldRow } oldBinding] ||
                        !priorRows.Any(row => ReferenceEquals(row, oldRow)))
                    { open.Add(new Relation(patternEdge, occurrence.SegmentBindings)); continue; }
                    dependencies.Add(oldBinding);
                    var mappings = validMaps.Where(map => ReferenceEquals(map.Left, oldRow)).ToArray();
                    var invalidMapping = suppliedMaps.OfType<Relation>().Any(map => ReferenceEquals(map.Left, oldRow) &&
                        !validMaps.Any(actual => ReferenceEquals(actual, map)));
                    Relation? actual = null;
                    if (invalidMapping || mappings.Length > 1)
                    { open.Add(new Relation(oldBinding, SegmentCorrespondence)); continue; }
                    if (mappings is [var mapping])
                    {
                        dependencies.Add(mapping);
                        if (!mapClosed) { open.Add(new Relation(oldBinding, SegmentCorrespondence)); continue; }
                        actual = (Relation)mapping.Right;
                    }
                    else if (currentRows.Any(row => ReferenceEquals(row, oldRow))) actual = oldRow;
                    else
                    {
                        if (!mapClosed)
                        { open.Add(new Relation(oldBinding, SegmentCorrespondence)); continue; }
                        var sameIdentity = currentRows.Where(row => N.Same(row.Left, oldRow.Left)).ToArray();
                        if (sameIdentity.Length != 0)
                            open.Add(new Relation(oldBinding, new Relation(N.S(sameIdentity), SegmentCorrespondence)));
                        else if (currentSegmentInventoryReady)
                            no.Add(new Relation(oldBinding, new Relation(current.Segments, N.S())));
                        else open.Add(new Relation(oldBinding, new Relation(current.Segments, StructuralConstraint.Open())));
                        continue;
                    }
                    dependencies.Add(actual);
                    if (duplicateIdentities.Contains(actual) || !current.TryReadSegment(actual, out _, out var actualFrom, out var actualTo, out var actualHeading))
                    { open.Add(new Relation(oldBinding, actual)); continue; }
                    if (matchedRows.Any(row => ReferenceEquals(row, actual)))
                    { no.Add(new Relation(oldBinding, new Relation(actual, N.S(matchedRows.ToArray())))); continue; }
                    matchedRows.Add(actual);
                    var expectedUse = new Relation(patternEdge, new Relation(expectedFrom, expectedTo));
                    var direct = N.Same(actualFrom, expectedFrom) && N.Same(actualTo, expectedTo);
                    var reverse = N.Same(actualFrom, expectedTo) && N.Same(actualTo, expectedFrom);
                    var actualUse = new Relation(actual, new Relation(actualFrom, actualTo));
                    if (!direct && !reverse)
                    { no.Add(new Relation(expectedUse, actualUse)); continue; }
                    yes.Add(new Relation(expectedUse, actualUse));
                    if (!compatibleFrame) continue;
                    var oriented = reverse ? Opposite(current, actualHeading) : actualHeading;
                    if (oriented is null || !StructuralConstraint.TryReadScope(oriented, out var actualDirections, out var seam, out var tail) ||
                        actualDirections.Length == 0)
                    { open.Add(new Relation(new Relation(patternEdge, expectedHeading), new Relation(actual, actualHeading))); continue; }
                    var expectedDirections = N.E(expectedHeading);
                    var headingProof = new Relation(new Relation(patternEdge, expectedHeading), new Relation(actual, oriented));
                    // A known direction outside the promised closed heading is
                    // already a counterexample, even when more detail may arrive.
                    // A compatible prefix cannot establish that no more exists.
                    if (actualDirections.Any(actualDirection => !expectedDirections.Any(direction => N.Same(actualDirection, direction))))
                        no.Add(headingProof);
                    else if (!seam.Equals(tail)) open.Add(headingProof);
                    else if (expectedDirections.Length == actualDirections.Length && expectedDirections.All(direction =>
                        actualDirections.Any(actualDirection => N.Same(actualDirection, direction)))) yes.Add(headingProof);
                    else no.Add(headingProof);
                }

                // An exhaustive incident star promises more than an existential
                // walk/cycle. An extra incident edge is a positive counterexample;
                // proving no extra edge requires a complete incident inventory.
                foreach (var centreRole in centres)
                {
                    if (!boundSites.TryGetValue(centreRole, out var centre))
                    { open.Add(new Relation(centreRole, occurrence.SiteBindings)); continue; }
                    var incident = currentRows.Where(row => !duplicateIdentities.Contains(row) && current.TryReadSegment(row, out _, out var from, out var to, out _) &&
                        (N.Same(centre, from) || N.Same(centre, to))).ToArray();
                    var extras = incident.Where(row => !matchedRows.Any(matched => ReferenceEquals(matched, row))).ToArray();
                    var retainedRows = edgeBindings.OfType<Relation>().Select(binding => binding.Right).OfType<Relation>().ToArray();
                    var definiteExtras = extras.Where(row => !retainedRows.Any(old => N.Same(old.Left, row.Left))).ToArray();
                    var incidenceQuestion = new Relation(new Relation(centreRole, centre), N.S(matchedRows.ToArray()));
                    dependencies.Add(current.Segments);
                    if (definiteExtras.Length != 0) no.Add(new Relation(incidenceQuestion, N.S(definiteExtras)));
                    else if (extras.Length != 0)
                        open.Add(new Relation(incidenceQuestion, new Relation(N.S(extras), SegmentCorrespondence)));
                    else if (currentSegmentInventoryReady)
                        yes.Add(new Relation(incidenceQuestion, new Relation(current.Segments, N.S(incident))));
                    else open.Add(new Relation(incidenceQuestion, new Relation(current.Segments, StructuralConstraint.Open())));
                }
                Finish();

                void Finish()
                {
                    var support = N.S(yes.ToArray()); var refute = N.S(no.ToArray()); var unsettled = N.S(open.ToArray());
                    var dependencyScope = N.S(dependencies.Distinct(N.Participants.Instance).ToArray());
                    var read = new Relation(new Relation(family.Read, occurrence.Read),
                        new Relation(current.Surface, new Relation(support, new Relation(refute, new Relation(unsettled, dependencyScope)))));
                    checks.Add(new(family, occurrence, read, support, refute, unsettled, dependencyScope));
                }
            }
        Checks = checks;
        Supported = N.S(checks.Where(check => check.IsSupported).Select(check => (IElement)check.Read).ToArray());
        Refuted = N.S(checks.Where(check => N.E(check.RefutedObligations).Length != 0).Select(check => (IElement)check.Read).ToArray());
        Unresolved = N.S(checks.Where(check => N.E(check.RefutedObligations).Length == 0 && N.E(check.UnresolvedObligations).Length != 0)
            .Select(check => (IElement)check.Read).ToArray());
        InputUnresolved = N.S(inputPending.Distinct(N.Participants.Instance).ToArray());
        Surface = new(Definition, new Relation(Supported, new Relation(Refuted, new Relation(Unresolved,
            new Relation(InputUnresolved, N.S(checks.Select(check => (IElement)check.Read).ToArray()))))));
    }

    public Relation Evaluate() => Surface;
    public bool TryReadCheck(Relation actual, out QualitativeRecognitionCheck check)
    { check = Checks.FirstOrDefault(candidate => ReferenceEquals(candidate.Read, actual))!; return check is not null; }

    private static bool TryPattern(Relation pattern, out IElement[] sites, out IElement[] edges, out IElement[] centres)
    {
        sites = edges = centres = [];
        return pattern is { Left: Relation { Right: Relation { Left: Relation siteScope, Right: Relation edgeScope } },
            Right: Relation { Left: Relation centreScope } } && N.Read(siteScope, out sites) && N.Read(edgeScope, out edges) && N.Read(centreScope, out centres);
    }
    private static Relation? Opposite(QualitativeRecognitionObservation observation, Relation heading)
    {
        if (!StructuralConstraint.TryReadScope(heading, out var directions, out var seam, out var tail)) return null;
        var inverse = new List<IElement>();
        foreach (var direction in directions)
        {
            var pair = N.E(observation.Frame.Opposites).OfType<Relation>().SingleOrDefault(row => N.Same(row.Left, direction));
            if (pair is null) return null; inverse.Add(pair.Right);
        }
        return seam.Equals(tail) ? N.S(inverse.ToArray()) : StructuralConstraint.Open(inverse.ToArray());
    }
}

/// <summary>Projection of a retained native check. Refutation is local to this
/// occurrence's declared binding; it does not remove the reusable pattern.</summary>
public sealed class QualitativeRecognitionCheck(QualitativeMotifFamily priorFamily,
    QualitativeMotifOccurrence priorOccurrence, Relation read, Relation supported,
    Relation refuted, Relation unresolved, Relation dependencies)
{
    public QualitativeMotifFamily PriorFamily { get; } = priorFamily;
    public QualitativeMotifOccurrence PriorOccurrence { get; } = priorOccurrence;
    public Relation Read { get; } = read;
    public Relation SupportedObligations { get; } = supported;
    public Relation RefutedObligations { get; } = refuted;
    public Relation UnresolvedObligations { get; } = unresolved;
    public Relation Dependencies { get; } = dependencies;
    public bool IsSupported => N.E(RefutedObligations).Length == 0 && N.E(UnresolvedObligations).Length == 0;
    /// <summary>A display read over the native obligation scopes.</summary>
    public string Status => N.E(RefutedObligations).Length != 0 ? "refuted" :
        N.E(UnresolvedObligations).Length != 0 ? "unresolved" : "supported";
}
