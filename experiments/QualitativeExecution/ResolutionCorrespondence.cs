using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// An explicit observational correspondence between two finite resolutions.
/// Its native rows, rather than category codes, determine forward images and
/// inverse preimages. An inverse read never chooses one lost alternative.
/// </summary>
public sealed class ResolutionCorrespondence
{
    // Surface = ((source-basis -> source-domain) ->
    //            (target-basis -> target-domain)) -> mapping-scope
    // row = source-participant -> target-participant
    // read = ((Surface -> ((from-basis -> from-domain) ->
    //                     (to-basis -> to-domain))) -> input-support) ->
    //        ((result-support -> Closed(blocked)) ->
    //         (Closed(unresolved) -> Closed(provenance)))
    // Provenance contains actual consumed row identities, plus optional input
    // evidence. Mapping is observation; no source or receiver is mutated.

    public IElement SourceBasis { get; }
    public IElement TargetBasis { get; }
    public Relation SourceDomain { get; }
    public Relation TargetDomain { get; }
    public Relation SourcePort { get; }
    public Relation TargetPort { get; }
    public Relation Mappings { get; }
    public Relation Surface { get; }

    public static ResolutionCorrespondence FromRows(IElement sourceBasis, IElement targetBasis,
        Relation sourceDomain, Relation targetDomain, params Relation[] rows) =>
        new(sourceBasis, targetBasis, sourceDomain, targetDomain, StructuralConstraint.Closed(rows));

    /// <summary>
    /// Domains must be closed identity sets; the mapping scope may remain open.
    /// </summary>
    public ResolutionCorrespondence(IElement sourceBasis, IElement targetBasis,
        Relation sourceDomain, Relation targetDomain, Relation mappings)
    {
        if (!FiniteLaw.DomainEntries(sourceDomain, out _) ||
            !FiniteLaw.DomainEntries(targetDomain, out _))
            throw new ArgumentException("Correspondence domains must be closed identity sets.");
        SourceBasis = sourceBasis; TargetBasis = targetBasis;
        SourceDomain = sourceDomain; TargetDomain = targetDomain; Mappings = mappings;
        SourcePort = new Relation(sourceBasis, sourceDomain);
        TargetPort = new Relation(targetBasis, targetDomain);
        Surface = new Relation(new Relation(SourcePort, TargetPort), mappings);
    }

    public Relation ReadForward(Relation support, IElement? evidence = null) =>
        Read(support, SourcePort, TargetPort, evidence);

    public Relation ReadBackward(Relation support, IElement? evidence = null) =>
        Read(support, TargetPort, SourcePort, evidence);

    /// <summary>
    /// Reads only a witness owned by this correspondence, including its ordered
    /// basis and domain identities. Similar domains or equal outcomes do not
    /// authorize a read in another correspondence.
    /// </summary>
    public bool TryReadResult(Relation read, out Relation result, out Relation blocked,
        out Relation unresolved, out Relation provenance)
    {
        result = blocked = unresolved = provenance = null!;
        if (read.Left is not Relation { Left: Relation header, Right: Relation input } ||
            !ReferenceEquals(header.Left, Surface) || header.Right is not Relation direction ||
            direction.Left is not Relation from || direction.Right is not Relation to ||
            !(ReferenceEquals(from, SourcePort) && ReferenceEquals(to, TargetPort) ||
              ReferenceEquals(from, TargetPort) && ReferenceEquals(to, SourcePort)) ||
            read.Right is not Relation { Left: Relation outcome, Right: Relation remainder } ||
            outcome.Left is not Relation output || outcome.Right is not Relation denied ||
            remainder.Left is not Relation pending || remainder.Right is not Relation evidence ||
            !StructuralConstraint.TryReadScope(output, out _, out _, out _) ||
            !FiniteLaw.ClosedEntries(denied, out _) || !FiniteLaw.ClosedEntries(pending, out _) ||
            !FiniteLaw.ClosedEntries(evidence, out _)) return false;
        result = output; blocked = denied; unresolved = pending; provenance = evidence;
        return true;
    }

    // report = (Surface -> source-domain) ->
    //          ((Closed(totality-passed) -> Closed(totality-failed)) ->
    //           ((Closed(functionality-passed) -> Closed(functionality-failed)) ->
    //            Closed(unresolved)))
    // obligation witness = (source-participant -> target-domain) -> forward-read
    // Totality and functionality are distinct: a missing image violates totality
    // while still being a partial function; several images violate functionality.

    /// <summary>
    /// Checks all explicitly admitted source participants. This certifies only
    /// the current finite graph, not a discovered universal resolution law.
    /// Open or malformed maps cannot receive a completeness certificate.
    /// </summary>
    public Relation CheckForward()
    {
        var total = new List<IElement>(); var missing = new List<IElement>();
        var functional = new List<IElement>(); var multiple = new List<IElement>();
        var unresolved = new List<IElement>();
        if (!Definition(out var sources, out var targets) ||
            !FiniteLaw.ClosedEntries(Mappings, out var mappings) ||
            mappings.Any(entry => entry is not Relation row ||
                FiniteLaw.IndexOf(sources, row.Left) < 0 || FiniteLaw.IndexOf(targets, row.Right) < 0))
            unresolved.Add(new Relation(Surface, new Relation(SourceDomain, TargetDomain)));
        else
            foreach (var source in sources)
            {
                var read = ReadForward(StructuralConstraint.Closed(source));
                var obligation = new Relation(new Relation(source, TargetDomain), read);
                TryReadResult(read, out var result, out _, out var pending, out _);
                if (!FiniteLaw.ClosedEntries(result, out var values) || FiniteLaw.Entries(pending).Length > 0)
                { unresolved.Add(obligation); continue; }
                (values.Length > 0 ? total : missing).Add(obligation);
                (values.Length <= 1 ? functional : multiple).Add(obligation);
            }
        return new Relation(new Relation(Surface, SourceDomain),
            new Relation(new Relation(Closed(total), Closed(missing)),
                new Relation(new Relation(Closed(functional), Closed(multiple)), Closed(unresolved))));
    }

    public bool TryReadCheck(Relation report, out Relation totalityPassed, out Relation totalityFailed,
        out Relation functionalityPassed, out Relation functionalityFailed, out Relation unresolved)
    {
        totalityPassed = totalityFailed = functionalityPassed = functionalityFailed = unresolved = null!;
        if (report.Left is not Relation header || !ReferenceEquals(header.Left, Surface) ||
            !ReferenceEquals(header.Right, SourceDomain) ||
            report.Right is not Relation { Left: Relation totality, Right: Relation rest } ||
            totality.Left is not Relation total || totality.Right is not Relation missing ||
            rest.Left is not Relation functionality || rest.Right is not Relation pending ||
            functionality.Left is not Relation functional || functionality.Right is not Relation multiple ||
            !FiniteLaw.ClosedEntries(total, out _) || !FiniteLaw.ClosedEntries(missing, out _) ||
            !FiniteLaw.ClosedEntries(functional, out _) || !FiniteLaw.ClosedEntries(multiple, out _) ||
            !FiniteLaw.ClosedEntries(pending, out _)) return false;
        totalityPassed = total; totalityFailed = missing;
        functionalityPassed = functional; functionalityFailed = multiple; unresolved = pending;
        return true;
    }

    private Relation Read(Relation input, Relation fromPort, Relation toPort, IElement? evidence)
    {
        var results = new List<IElement>(); var blocked = new List<IElement>();
        var unresolved = new List<IElement>(); var provenance = new List<IElement>();
        if (evidence is not null) provenance.Add(new Relation(input, evidence));
        var empty = FiniteLaw.ClosedEntries(input, out var entries) && entries.Length == 0;
        var fromDomain = ReferenceEquals(fromPort, SourcePort) ? SourceDomain : TargetDomain;
        if (!Definition(out var sourceStates, out var targetStates))
            unresolved.Add(new Relation(Surface, new Relation(SourceDomain, TargetDomain)));
        else if (!StructuralConstraint.TryReadScope(input, out entries, out var inputSeam, out var inputTail))
            unresolved.Add(new Relation(input, fromDomain));
        else
        {
            var forward = ReferenceEquals(fromPort, SourcePort);
            var fromStates = forward ? sourceStates : targetStates;
            var toStates = forward ? targetStates : sourceStates;
            if (!inputSeam.Equals(inputTail)) unresolved.Add(new Relation(input, inputTail));
            var mappingReadable = StructuralConstraint.TryReadScope(Mappings, out var mappings,
                out var seam, out var tail);
            var mappingOpen = !mappingReadable || !seam.Equals(tail);
            if (mappingOpen) unresolved.Add(mappingReadable ? new Relation(Mappings, tail) : Mappings);
            foreach (var entry in mappings)
                if (entry is not Relation) unresolved.Add(new Relation(entry, Mappings));
                else if (entry is Relation row &&
                    (FiniteLaw.IndexOf(sourceStates, row.Left) < 0 || FiniteLaw.IndexOf(targetStates, row.Right) < 0))
                    blocked.Add(new Relation(row, new Relation(SourceDomain, TargetDomain)));

            foreach (var participant in entries)
            {
                if (FiniteLaw.IndexOf(fromStates, participant) < 0)
                { blocked.Add(new Relation(participant, fromDomain)); continue; }
                var matched = false;
                foreach (var entry in mappings)
                {
                    if (entry is not Relation row) continue;
                    var from = forward ? row.Left : row.Right;
                    var to = forward ? row.Right : row.Left;
                    if (!FiniteLaw.SameParticipant(from, participant) ||
                        FiniteLaw.IndexOf(toStates, to) < 0) continue;
                    matched = true;
                    AddDistinct(results, to); AddDistinct(provenance, row);
                }
                if (!matched)
                {
                    var absent = new Relation(participant, Mappings);
                    if (mappingOpen || mappings.Any(entry => entry is not Relation)) unresolved.Add(absent);
                    else blocked.Add(absent);
                }
            }
        }
        var result = unresolved.Count > 0 && !empty ? StructuralConstraint.Open(results.ToArray()) : Closed(results);
        return new Relation(new Relation(new Relation(Surface,
                new Relation(fromPort, toPort)), input),
            new Relation(new Relation(result, Closed(blocked)), new Relation(Closed(unresolved), Closed(provenance))));
    }

    private bool Definition(out IElement[] sources, out IElement[] targets)
    {
        sources = targets = [];
        return Surface.Left is Relation { Left: Relation source, Right: Relation target } &&
            ReferenceEquals(source, SourcePort) && ReferenceEquals(target, TargetPort) &&
            Contract(source, SourceBasis, SourceDomain) && Contract(target, TargetBasis, TargetDomain) &&
            ReferenceEquals(Surface.Right, Mappings) && FiniteLaw.DomainEntries(SourceDomain, out sources) &&
            FiniteLaw.DomainEntries(TargetDomain, out targets);
    }

    private static bool Contract(Relation contract, IElement basis, Relation domain) =>
        FiniteLaw.SameParticipant(contract.Left, basis) && ReferenceEquals(contract.Right, domain);

    private static Relation Closed(List<IElement> entries) => StructuralConstraint.Closed(entries.ToArray());

    private static void AddDistinct(List<IElement> entries, IElement entry)
    {
        if (!entries.Any(existing => FiniteLaw.SameParticipant(existing, entry))) entries.Add(entry);
    }
}
