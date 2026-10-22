using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// An explicitly selected empirical Bernoulli/Beta read, above the kernel. It
/// does not turn arbitrary confidence, resolution or model compatibility into
/// a sample count. Its state is native structure; integer counts and fractions
/// below are derived diagnostic reads over repeated shared participation.
/// </summary>
public static class EvidenceSupport
{
    // frame = (claim -> contrary) -> (common-rate -> support-token).
    // prior = (claim -> Closed(token...)) -> (contrary -> Closed(token...)).
    // acquisition = (frame -> common-rate) -> (Closed(events) -> Closed(factors)),
    // factor = event -> (common-rate -> Closed(claim, contrary)).
    // The acquisition surface selects the conditional product of these finite
    // Bernoulli factors. Conditional independence/common rate are assumptions
    // of this selected law, not inferred from separate event identities.
    // state = ((frame -> prior) -> selected-acquisition) -> report-scope.
    // report = (event -> (acquisition-basis -> route)) ->
    //          (question-orientation -> answer-scope).
    // A response repeats the orientation if affirmative and reverses it if
    // negative; its left participant is therefore the canonical answer.

    public sealed class Frame
    {
        public IElement Claim { get; }
        public IElement Contrary { get; }
        public IElement Rate { get; }
        public IElement Token { get; }
        public Relation Surface { get; }
        public Relation Orientation => (Relation)Surface.Left;

        public Frame(IElement? claim = null, IElement? contrary = null)
        {
            Claim = claim ?? new Placeholder(); Contrary = contrary ?? new Placeholder();
            if (Same(Claim, Contrary)) throw new ArgumentException("The two answer participants must be distinct.");
            Rate = new Placeholder(); Token = new Placeholder();
            Surface = new Relation(new Relation(Claim, Contrary), new Relation(Rate, Token));
        }

        private Frame(Relation surface, Relation answers, Relation basis)
        { Surface = surface; Claim = answers.Left; Contrary = answers.Right; Rate = basis.Left; Token = basis.Right; }

        public static bool TryRead(Relation surface, out Frame frame)
        {
            frame = null!;
            if (surface.Left is not Relation answers || surface.Right is not Relation basis ||
                Same(answers.Left, answers.Right) || Same(basis.Left, basis.Right)) return false;
            frame = new Frame(surface, answers, basis); return true;
        }

        /// <summary>Declares a finite common-rate conditional-IID acquisition
        /// contract. These premises are selected, not established by the helper.
        /// Its event list is a domain, never an observation or prior count.</summary>
        public Relation Acquisition(params IElement[] events)
        {
            if (events.Where((item, i) => events.Take(i).Any(previous => Same(previous, item))).Any())
                throw new ArgumentException("An acquisition domain cannot repeat an event identity.", nameof(events));
            var answers = StructuralConstraint.Closed(Claim, Contrary);
            var factors = events.Select(item => (IElement)new Relation(item, new Relation(Rate, answers))).ToArray();
            return new Relation(new Relation(Surface, Rate),
                new Relation(StructuralConstraint.Closed(events), StructuralConstraint.Closed(factors)));
        }
    }

    /// <summary>An immutable builder view. Only Surface is retained state.</summary>
    public sealed class State
    {
        public Relation Surface { get; }
        public State(Relation surface) => Surface = surface;

        public State Observe(IElement eventId, bool askOpposed, bool affirm,
            IElement acquisitionBasis, IElement? route = null)
        {
            if (!TryState(Surface, out var frame, out _, out _, out var reports))
                throw new ArgumentException("Expected an empirical support state.");
            var orientation = askOpposed
                ? new Relation(frame.Contrary, frame.Claim) : frame.Orientation;
            var response = affirm ? orientation : new Relation(orientation.Right, orientation.Left);
            return Append(Report(eventId, acquisitionBasis, route ?? eventId, orientation,
                StructuralConstraint.Closed(response)), reports);
        }

        public State ObserveUnknown(IElement eventId, bool askOpposed,
            IElement acquisitionBasis, IElement? route = null)
        {
            if (!TryState(Surface, out var frame, out _, out _, out var reports))
                throw new ArgumentException("Expected an empirical support state.");
            var orientation = askOpposed
                ? new Relation(frame.Contrary, frame.Claim) : frame.Orientation;
            return Append(Report(eventId, acquisitionBasis, route ?? eventId, orientation,
                StructuralConstraint.Open()), reports);
        }

        /// <summary>Selects a joint acquisition premise explicitly. Existing
        /// reports retain the basis they actually advertised.</summary>
        public State WithAcquisition(IElement acquisition)
        {
            if (Surface.Left is not Relation head) throw new ArgumentException("Expected an empirical support state.");
            return new State(new Relation(new Relation(head.Left, acquisition), Surface.Right));
        }

        private State Append(Relation report, Relation reports)
        {
            StructuralConstraint.TryReadScope(reports, out var entries, out var seam, out var tail);
            var scope = Same(seam, tail)
                ? StructuralConstraint.Closed([.. entries, report])
                : StructuralConstraint.Open([.. entries, report]);
            return new State(new Relation(Surface.Left, scope));
        }
    }

    public sealed record Fraction(BigInteger Numerator, BigInteger Denominator)
    {
        public string Reduced
        {
            get
            {
                if (Denominator.IsZero) return "open";
                var gcd = BigInteger.GreatestCommonDivisor(Numerator, Denominator);
                return $"{Numerator / gcd}/{Denominator / gcd}";
            }
        }
    }

    /// <summary>All numbers are recomputed from the native state. Mean and
    /// dispersion are conditional on the selected Beta acquisition law and only
    /// the reports this read admits. An incomplete read still exposes that
    /// conditional calculation, alongside its conflicts and unresolved reports;
    /// it is not a posterior incorporating every submitted answer. These are
    /// not physical bounds or proof of observation reliability.</summary>
    public sealed record ReadResult(int A, int B, int AcceptedEvents, int DuplicateReports,
        int ConflictingEvents, int UnresolvedReports, int Partitions, int SelectedPartition,
        Relation Positive, Relation Negative, Relation Accepted, Relation Duplicates,
        Relation Conflicts, Relation Unresolved, Relation Resolution, Relation Surface)
    {
        public int Concentration => checked(A + B);
        /// <summary>All submitted reports have been accounted for without an
        /// unresolved or conflicting answer. This does not prove the premises.</summary>
        public bool Complete => ConflictingEvents == 0 && UnresolvedReports == 0;
        public Fraction Mean => new(A, Concentration);
        /// <summary>Posterior dispersion of the latent common Bernoulli rate.</summary>
        public Fraction Variance => new((BigInteger)A * B,
            (BigInteger)Concentration * Concentration * ((BigInteger)Concentration + 1));
        /// <summary>Variance of one future Bernoulli outcome under this selected
        /// law, distinct from uncertainty about the common rate.</summary>
        public Fraction PredictiveVariance => new((BigInteger)A * B,
            (BigInteger)Concentration * Concentration);
    }

    public static State Create(Frame frame, int positivePrior, int opposedPrior,
        IElement? acquisition = null)
    {
        if (positivePrior <= 0) throw new ArgumentOutOfRangeException(nameof(positivePrior), "This fixture selects a proper positive Beta prior.");
        if (opposedPrior <= 0) throw new ArgumentOutOfRangeException(nameof(opposedPrior), "This fixture selects a proper positive Beta prior.");
        var prior = new Relation(new Relation(frame.Claim, Repeat(frame.Token, positivePrior)),
            new Relation(frame.Contrary, Repeat(frame.Token, opposedPrior)));
        return new State(new Relation(new Relation(new Relation(frame.Surface, prior),
            acquisition ?? StructuralConstraint.Open()), StructuralConstraint.Closed()));
    }

    public static Relation Report(IElement eventId, IElement acquisitionBasis, IElement route,
        Relation orientation, Relation answers) =>
        new(new Relation(eventId, new Relation(acquisitionBasis, route)), new Relation(orientation, answers));

    public static ReadResult Read(State state, int partitions = 2) => Read(state.Surface, partitions);

    public static ReadResult Read(Relation state, int partitions = 2)
    {
        if (partitions <= 0) throw new ArgumentOutOfRangeException(nameof(partitions));
        if (!TryState(state, out var frame, out var prior, out var acquisition, out var reports))
            throw new ArgumentException("Expected an empirical support state.", nameof(state));
        var positive = Entries((Relation)((Relation)prior.Left).Right).ToList();
        var negative = Entries((Relation)((Relation)prior.Right).Right).ToList();
        var accepted = new List<IElement>(); var duplicates = new List<IElement>();
        var conflicts = new List<IElement>(); var unresolved = new List<IElement>();
        var eventGroups = new List<(IElement Event, List<(Relation Report, IElement Answer)> Reports)>();
        var validAcquisition = TryAcquisition(acquisition, frame, out var admitted);
        StructuralConstraint.TryReadScope(reports, out var rows, out var seam, out var tail);
        if (!Same(seam, tail)) unresolved.Add(new Relation(reports, tail));
        foreach (var item in rows)
        {
            if (item is not Relation report || !TryReport(report, frame, out var eventId,
                out var basis, out var answer) || !validAcquisition || !Same(basis, acquisition) ||
                !admitted.Any(candidate => Same(candidate, eventId)))
            { unresolved.Add(item); continue; }
            var at = eventGroups.FindIndex(group => Same(group.Event, eventId));
            if (at < 0) eventGroups.Add((eventId, [(report, answer)]));
            else eventGroups[at].Reports.Add((report, answer));
        }
        foreach (var group in eventGroups)
        {
            var first = group.Reports[0];
            if (group.Reports.Any(row => !Same(row.Answer, first.Answer)))
            {
                // A contradicted replay is not a second acquisition, and this
                // read does not choose which report to trust. Withhold both.
                conflicts.Add(new Relation(group.Event,
                    StructuralConstraint.Closed(group.Reports.Select(row => (IElement)row.Report).ToArray())));
                continue;
            }
            var contribution = new Relation(group.Event,
                new Relation(new Relation(acquisition, first.Answer), frame.Token));
            accepted.Add(new Relation(contribution,
                StructuralConstraint.Closed(group.Reports.Select(row => (IElement)row.Report).ToArray())));
            if (Same(first.Answer, frame.Claim)) positive.Add(frame.Token); else negative.Add(frame.Token);
            foreach (var repeated in group.Reports.Skip(1))
                duplicates.Add(new Relation(first.Report, repeated.Report));
        }
        var p = StructuralConstraint.Closed(positive.ToArray());
        var n = StructuralConstraint.Closed(negative.ToArray());
        var total = StructuralConstraint.Closed([.. positive, .. negative]);
        var partitionToken = new Placeholder(); var partition = Repeat(partitionToken, partitions);
        var numerator = (BigInteger)positive.Count * partitions;
        var concentration = checked(positive.Count + negative.Count);
        var q = (int)(numerator / concentration);
        var r = (int)(numerator % concentration);
        // Exact finite projection accounting: A * partitions = q * (A+B) + r.
        // Changing partitions never manufactures supporting acquisition events.
        var resolution = new Relation(new Relation(p, total), new Relation(partition,
            new Relation(Repeat(partitionToken, q), Repeat(frame.Token, r))));
        var yes = StructuralConstraint.Closed(accepted.ToArray());
        var duplicate = StructuralConstraint.Closed(duplicates.ToArray());
        var conflict = StructuralConstraint.Closed(conflicts.ToArray());
        var open = StructuralConstraint.Closed(unresolved.ToArray());
        var witness = new Relation(new Relation(state, resolution), new Relation(new Relation(p, n),
            new Relation(yes, new Relation(duplicate, new Relation(conflict, open)))));
        return new ReadResult(positive.Count, negative.Count, accepted.Count, duplicates.Count,
            conflicts.Count, unresolved.Count, partitions, q, p, n, yes, duplicate, conflict, open,
            resolution, witness);
    }

    public static bool TryAcquisition(IElement acquisition, Frame frame, out IElement[] events)
    {
        events = [];
        if (acquisition is not Relation { Left: Relation basis, Right: Relation product } ||
            !Same(basis.Left, frame.Surface) || !Same(basis.Right, frame.Rate) ||
            product.Left is not Relation domain || product.Right is not Relation factors ||
            !Closed(domain, out var ids) || !Closed(factors, out var terms) || ids.Length != terms.Length)
            return false;
        for (var i = 0; i < ids.Length; i++)
        {
            if (ids.Take(i).Any(previous => Same(previous, ids[i])) || terms[i] is not Relation factor ||
                !Same(factor.Left, ids[i]) || factor.Right is not Relation condition ||
                !Same(condition.Left, frame.Rate) || condition.Right is not Relation answers ||
                !Closed(answers, out var outcomes) || outcomes.Length != 2 ||
                !Same(outcomes[0], frame.Claim) || !Same(outcomes[1], frame.Contrary)) return false;
        }
        events = ids; return true;
    }

    private static bool TryState(Relation state, out Frame frame, out Relation prior,
        out IElement acquisition, out Relation reports)
    {
        frame = null!; prior = reports = null!; acquisition = null!;
        if (state.Left is not Relation { Left: Relation basis } head ||
            basis.Left is not Relation frameSurface || !Frame.TryRead(frameSurface, out frame) ||
            basis.Right is not Relation p || p.Left is not Relation positive || p.Right is not Relation negative ||
            !Same(positive.Left, frame.Claim) || !Same(negative.Left, frame.Contrary) ||
            positive.Right is not Relation yes || negative.Right is not Relation no ||
            !Closed(yes, out var a) || !Closed(no, out var b) || a.Length == 0 || b.Length == 0 ||
            state.Right is not Relation observations ||
            !StructuralConstraint.TryReadScope(observations, out _, out _, out _)) return false;
        var tokenBasis = frame.Token;
        if (a.Concat(b).Any(token => !Same(token, tokenBasis))) return false;
        prior = p; acquisition = head.Right; reports = observations; return true;
    }

    private static bool TryReport(Relation report, Frame frame, out IElement eventId,
        out IElement acquisition, out IElement answer)
    {
        eventId = acquisition = answer = null!;
        if (report.Left is not Relation { Right: Relation context } source ||
            report.Right is not Relation { Left: Relation orientation, Right: Relation support } ||
            !OpposedPair(orientation, frame) || !Closed(support, out var replies) || replies.Length != 1 ||
            replies[0] is not Relation response || !OpposedPair(response, frame)) return false;
        eventId = source.Left; acquisition = context.Left; answer = response.Left; return true;
    }

    private static bool OpposedPair(Relation relation, Frame frame) =>
        Same(relation.Left, frame.Claim) && Same(relation.Right, frame.Contrary) ||
        Same(relation.Left, frame.Contrary) && Same(relation.Right, frame.Claim);

    private static Relation Repeat(IElement token, int count) =>
        StructuralConstraint.Closed(Enumerable.Repeat(token, count).ToArray());
    private static IElement[] Entries(Relation scope) =>
        StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && Same(seam, tail);
    private static bool Same(IElement left, IElement right) =>
        ReferenceEquals(left, right) || left is Placeholder a && right is Placeholder b && a.Equals(b);
}
