using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// Question-directed observation over a native binary law. Advertised supports
/// are read before any supplier is called. A unique answer over every admitted
/// pair makes finer suppliers unnecessary; this never executes an action.
/// </summary>
public static class DemandLaw
{
    // port = identity -> advertised-publication
    // application = (law -> (left-port -> right-port)) -> response-routes
    // receipt = (application -> (current-left-port -> current-right-port)) ->
    //           (answers -> (requested-ports -> (blocked -> proofs)))
    // Requested ports follow the application's visible left/right order. There
    // is no hidden cost heuristic, private source read or execution dispatcher.
    public static Relation Port(IElement identity, Relation advertisedPublication) =>
        new(identity, advertisedPublication);

    public static Relation Create(Relation law, Relation leftPort, Relation rightPort,
        Relation responseRoutes) =>
        new(new Relation(law, new Relation(leftPort, rightPort)), responseRoutes);

    public static bool TryReadPort(Relation port, out IElement identity, out Relation publication)
    {
        identity = null!; publication = null!;
        if (port.Right is not Relation read ||
            !QualitativePublication.TryRead(read, out _, out _, out _, out _, out _, out _, out _)) return false;
        identity = port.Left; publication = read;
        return true;
    }

    public static bool TryReadApplication(Relation application, out Relation law,
        out Relation leftPort, out Relation rightPort, out Relation routes)
    {
        law = leftPort = rightPort = routes = null!;
        if (application.Left is not Relation { Left: Relation selected, Right: Relation ports } ||
            ports.Left is not Relation left || ports.Right is not Relation right ||
            application.Right is not Relation responses) return false;
        law = selected; leftPort = left; rightPort = right; routes = responses;
        return true;
    }

    public static Relation Ask(Relation application)
    {
        if (!TryReadApplication(application, out _, out var left, out var right, out _))
            return Receipt(application, application, application, StructuralConstraint.Open(),
                StructuralConstraint.Closed(), StructuralConstraint.Closed(application),
                StructuralConstraint.Closed());
        return Evaluate(application, left, right, [], []);
    }

    /// <summary>Inspects retained witness fields. Use TryReadAnswer to verify
    /// that those fields are certified by the connected application and ports.</summary>
    public static bool TryRead(Relation receipt, out Relation application, out Relation answers,
        out Relation requests, out Relation blocked, out Relation proofs)
    {
        application = answers = requests = blocked = proofs = null!;
        if (!TryReadCurrent(receipt, out var selected, out _, out _) ||
            receipt.Right is not Relation { Left: Relation support, Right: Relation rest } ||
            rest.Left is not Relation ports || rest.Right is not Relation { Left: Relation rejected, Right: Relation evidence } ||
            !StructuralConstraint.TryReadScope(support, out _, out _, out _) ||
            !FiniteLaw.ClosedEntries(ports, out _) || !FiniteLaw.ClosedEntries(rejected, out _) ||
            !FiniteLaw.ClosedEntries(evidence, out _)) return false;
        application = selected; answers = support; requests = ports; blocked = rejected; proofs = evidence;
        return true;
    }

    /// <summary>Guards prospective use by rereading the connected native law,
    /// routes and retained ports. Copying an answer into another receipt does
    /// not certify it. Pure qualitative relation sides are immutable; this
    /// validates native claims rather than promising a live mutation scheduler.</summary>
    public static bool TryReadAnswer(Relation receipt, out IElement answer)
    {
        answer = null!;
        if (!TryReadStoredAnswer(receipt, out var retained) ||
            !TryReadCurrent(receipt, out var application, out var left, out var right)) return false;
        var current = Evaluate(application, left, right, [], []);
        if (!TryReadStoredAnswer(current, out var certified) ||
            !FiniteLaw.SameParticipant(retained, certified)) return false;
        answer = retained;
        return true;
    }

    private static bool TryReadStoredAnswer(Relation receipt, out IElement answer)
    {
        answer = null!;
        if (!TryRead(receipt, out _, out var answers, out var requests, out _, out _) ||
            !FiniteLaw.ClosedEntries(answers, out var values) || values.Length != 1 ||
            FiniteLaw.Entries(requests).Length != 0) return false;
        answer = values[0];
        return true;
    }

    /// <summary>
    /// Queries only explicitly requested native ports, at most once per port in
    /// this call. After each offered publication the question is read again; a
    /// sufficient first refinement skips the remaining supplier. The supplier
    /// returns a publication, never an imperative effect. Independent physical
    /// maintenance must be performed through its own execution structure.
    /// </summary>
    public static Relation Service(Relation receipt, Func<IElement, Relation> supplier)
    {
        // A copied receipt may claim an answer or suppress demand unsupported
        // by its application. Certify it before deciding no supplier must run.
        var current = Refresh(receipt);
        if (!TryRead(current, out var application, out _, out var initialRequests, out _, out _)) return current;
        foreach (var requested in FiniteLaw.Entries(initialRequests))
        {
            if (requested is not Relation requestedPort || !TryReadPort(requestedPort, out var identity, out _) ||
                !TryRead(current, out _, out _, out var requests, out var priorBlocked, out var priorProofs) ||
                !TryReadCurrent(current, out _, out var left, out var right)) continue;
            var active = FiniteLaw.Entries(requests).OfType<Relation>().FirstOrDefault(port =>
                TryReadPort(port, out var otherIdentity, out _) && FiniteLaw.SameParticipant(identity, otherIdentity));
            if (active is null || !TryReadPort(active, out _, out var earlier)) continue;
            var offered = supplier(active);
            var admitted = QualitativePublication.TryRefine(earlier, offered, out var accepted, out var witness);
            var evidence = new List<IElement>(FiniteLaw.Entries(priorProofs)) { current, witness };
            var blocked = new List<IElement>(FiniteLaw.Entries(priorBlocked));
            if (admitted)
            {
                var updated = Port(identity, accepted);
                if (ReferenceEquals(active, left)) left = updated;
                else if (ReferenceEquals(active, right)) right = updated;
            }
            else blocked.Add(new Relation(active, offered));
            current = Evaluate(application, left, right, blocked, evidence);
        }
        return current;
    }

    private static Relation Refresh(Relation receipt)
    {
        if (!TryRead(receipt, out var application, out var oldAnswers, out var oldRequests,
                out var oldBlocked, out _) ||
            !TryReadCurrent(receipt, out _, out var left, out var right)) return receipt;
        var fresh = Evaluate(application, left, right, [], []);
        if (!TryRead(fresh, out _, out var answers, out var requests, out var blocked, out var proofs)) return receipt;
        if (SameScope(oldAnswers, answers) && SameScope(oldRequests, requests) &&
            FiniteLaw.Entries(blocked).Length == 0) return receipt;
        return Receipt(application, left, right, answers, requests,
            StructuralConstraint.Closed([.. FiniteLaw.Entries(oldBlocked), .. FiniteLaw.Entries(blocked)]),
            StructuralConstraint.Closed([receipt, .. FiniteLaw.Entries(proofs)]));
    }

    private static bool SameScope(Relation left, Relation right) =>
        StructuralConstraint.TryReadScope(left, out var a, out var as0, out var at) &&
        StructuralConstraint.TryReadScope(right, out var b, out var bs0, out var bt) &&
        as0.Equals(at) == bs0.Equals(bt) && a.Length == b.Length &&
        a.Zip(b).All(pair => FiniteLaw.SameParticipant(pair.First, pair.Second));

    private static Relation Evaluate(Relation application, Relation left, Relation right,
        List<IElement> blocked, List<IElement> proofs)
    {
        var answers = StructuralConstraint.Open();
        var requests = StructuralConstraint.Closed();
        var valid = TryReadApplication(application, out var law, out var originalLeft, out var originalRight, out var routes) &&
            FiniteLaw.TryRead(law, out var leftDomain, out var rightDomain, out _, out _) &&
            TryReadPort(left, out var leftIdentity, out var leftPublication) &&
            TryReadPort(right, out var rightIdentity, out var rightPublication) &&
            !FiniteLaw.SameParticipant(leftIdentity, rightIdentity) &&
            CorrespondingPort(originalLeft, left, proofs) && CorrespondingPort(originalRight, right, proofs) &&
            QualitativePublication.TryRead(leftPublication, out _, out _, out _, out _, out var actualLeft, out _, out _) &&
            QualitativePublication.TryRead(rightPublication, out _, out _, out _, out _, out var actualRight, out _, out _) &&
            ReferenceEquals(actualLeft, leftDomain) && ReferenceEquals(actualRight, rightDomain);
        if (!valid)
        {
            blocked.Add(new Relation(left, right));
            return Receipt(application, left, right, answers, requests,
                StructuralConstraint.Closed(blocked.ToArray()), StructuralConstraint.Closed(proofs.ToArray()));
        }

        TryReadPort(left, out _, out var lp); TryReadPort(right, out _, out var rp);
        QualitativePublication.TryRead(lp, out _, out _, out _, out _, out _, out var ls, out _);
        QualitativePublication.TryRead(rp, out _, out _, out _, out _, out _, out var rs, out _);
        var read = CompleteRead(law!, ls, rs);
        var question = QualitativeQuestion.Ask(read, routes!, new Relation(left, right));
        proofs.Add(new Relation(lp, rp)); proofs.Add(question);
        QualitativeQuestion.TryRead(question, out answers, out var needed, out _);
        if (FiniteLaw.Entries(needed).Length != 0)
        {
            var ports = new List<IElement>();
            if (Refinable(ls)) ports.Add(left);
            if (Refinable(rs)) ports.Add(right);
            requests = StructuralConstraint.Closed(ports.ToArray());
        }
        return Receipt(application, left, right, answers, requests,
            StructuralConstraint.Closed(blocked.ToArray()), StructuralConstraint.Closed(proofs.ToArray()));
    }

    private static bool CorrespondingPort(Relation original, Relation current, List<IElement> proofs)
    {
        if (!TryReadPort(original, out var originalIdentity, out var originalPublication) ||
            !TryReadPort(current, out var currentIdentity, out var currentPublication) ||
            !FiniteLaw.SameParticipant(originalIdentity, currentIdentity)) return false;
        // An unchanged advertised read can be open or contradictory. A distinct
        // claimed refinement must meet the ordinary same-context narrowing law.
        if (ReferenceEquals(originalPublication, currentPublication)) return true;
        var admitted = QualitativePublication.TryRefine(originalPublication, currentPublication, out _, out var witness);
        proofs.Add(witness);
        return admitted;
    }

    // The ordinary finite-law read permits missing pairs to contribute no
    // output. A universal answer needs a stronger obligation: every admitted
    // input pair has at least one admitted output. Preserve each failure.
    private static Relation CompleteRead(Relation law, Relation left, Relation right)
    {
        var read = FiniteLaw.Evaluate(law, left, right);
        FiniteLaw.TryRead(law, out _, out _, out _, out var rows);
        FiniteLaw.TryReadResult(read, out var support, out var residue, out var provenance);
        var failures = new List<IElement>(FiniteLaw.Entries(residue));
        foreach (var a in FiniteLaw.Entries(left))
            foreach (var b in FiniteLaw.Entries(right))
            {
                var covered = rows.Any(row => FiniteLaw.TryReadRow(row, out var x, out var y, out var outputs) &&
                    FiniteLaw.SameParticipant(a, x) && FiniteLaw.SameParticipant(b, y) &&
                    StructuralConstraint.TryReadScope(outputs, out var values, out _, out _) && values.Length != 0);
                if (!covered) failures.Add(new Relation(new Relation(a, b), (Relation)law.Right));
            }
        if (failures.Count == 0 || FiniteLaw.ClosedEntries(left, out var l) && l.Length == 0 ||
            FiniteLaw.ClosedEntries(right, out var r) && r.Length == 0) return read;
        var outputDomain = ((Relation)read.Left).Right as Relation;
        return FiniteLaw.Witness(law, left, right, outputDomain!,
            StructuralConstraint.Open(FiniteLaw.Entries(support)), StructuralConstraint.Closed(failures.ToArray()), provenance);
    }

    private static bool Refinable(Relation support)
    {
        if (!StructuralConstraint.TryReadScope(support, out var entries, out var seam, out var tail)) return false;
        return !seam.Equals(tail) || entries.Any(entry => !FiniteLaw.SameParticipant(entries[0], entry));
    }

    private static bool TryReadCurrent(Relation receipt, out Relation application,
        out Relation left, out Relation right)
    {
        application = left = right = null!;
        if (receipt.Left is not Relation { Left: Relation selected, Right: Relation ports } ||
            ports.Left is not Relation a || ports.Right is not Relation b ||
            !TryReadApplication(selected, out _, out _, out _, out _)) return false;
        application = selected; left = a; right = b;
        return true;
    }

    private static Relation Receipt(Relation application, Relation left, Relation right,
        Relation answers, Relation requests, Relation blocked, Relation proofs) =>
        new(new Relation(application, new Relation(left, right)),
            new Relation(answers, new Relation(requests, new Relation(blocked, proofs))));
}
