using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// A selected finite categorical probability law over native participation.
/// Inputs are unmeasured, equal-weight partitions, not physical measurements.
/// The calculation stays a connected prior/factor expression; exact fractions
/// below are derived reads, never the canonical storage of a belief.
/// </summary>
public static class StatisticalEvidence
{
    // frame = (one selector -> Closed(alternatives)) -> (count unit -> basis)
    // partition = (unit -> justification) -> (selected repetitions -> whole)
    // state = (frame -> Closed(model -> prior partition)) -> Closed(factors)
    // question = ((claim -> contrary) -> Closed(claim, contrary)) -> context
    // observation = (event -> question) -> (orientation -> response direction)
    // factor = (observation -> (conditioned events -> independent events)) ->
    //          (Closed(model -> positive-claim partition) -> premise)
    // expression = Closed(selected products) -> Closed(all products)
    // product = Closed(prior partition, observation -> likelihood partition...)
    // transition = (before -> offered factor) -> (after ->
    //              (accepted -> (duplicate -> (blocked -> (open -> proofs)))))
    // A categorical choice and all conditional/independence premises are
    // explicitly selected model contracts, not facts discovered by this helper.

    public sealed class Frame
    {
        public IElement Unit { get; }
        public IElement Selector { get; }
        public IElement[] Models { get; }
        public IElement Basis { get; }
        public Relation Surface { get; }
        public Frame(IElement selectionBasis, params IElement[] models)
        {
            if (models.Length == 0 || models.Distinct(Ref).Count() != models.Length)
                throw new ArgumentException("A selected categorical frame needs distinct alternatives.");
            Unit = new Placeholder(); Selector = new Placeholder(); Models = models.ToArray(); Basis = selectionBasis;
            Surface = new(new Relation(Selector, S(Models)), new Relation(Unit, selectionBasis));
        }
        private Frame(Relation surface, IElement selector, IElement unit, IElement basis, IElement[] models)
        { Surface = surface; Selector = selector; Unit = unit; Basis = basis; Models = models; }
        public static bool TryRead(Relation surface, out Frame frame)
        {
            frame = null!;
            if (surface is not { Left: Relation { Left: Placeholder selector, Right: Relation scope },
                    Right: Relation { Left: Placeholder unit, Right: var basis } } ||
                !Closed(scope) || E(scope).Length == 0 || E(scope).Distinct(Ref).Count() != E(scope).Length ||
                Same(selector, unit)) return false;
            frame = new(surface, selector, unit, basis, E(scope)); return true;
        }
        /// <summary>Declares a probability/relative-weight partition. The whole
        /// describes the selected measure, not confidence or acquisition count.</summary>
        public Relation Weight(int numerator, int denominator, IElement? reason = null)
        {
            if (numerator < 0 || numerator > denominator || denominator is < 1 or > 65536)
                throw new ArgumentOutOfRangeException(nameof(denominator));
            return new(new Relation(Unit, reason ?? Basis), new Relation(
                S(Enumerable.Repeat(Unit, numerator).ToArray()), S(Enumerable.Repeat(Unit, denominator).ToArray())));
        }
        public Relation Prior(params Relation[] bindings) => new(new Relation(Surface, S(bindings)), S());
    }

    public sealed class Question
    {
        public Relation Order { get; }
        public Relation Surface { get; }
        public Question(IElement claim, IElement contrary, IElement context)
        {
            if (Same(claim, contrary)) throw new ArgumentException("A binary question needs two distinct scoped alternatives.");
            Order = new(claim, contrary); Surface = new(new Relation(Order, S(claim, contrary)), context);
        }
        public Relation Observe(IElement eventId, bool askOpposed = false, bool affirm = true)
        {
            var orientation = askOpposed ? Order.Reverse() : Order;
            return new(new Relation(eventId, Surface), new Relation(orientation, affirm ? orientation : orientation.Reverse()));
        }
    }
    public sealed record Observation(IElement Event, Relation Question, Relation Orientation,
        Relation Response, IElement Outcome, IElement Context, bool Positive, Relation Surface);
    public static bool TryReadObservation(Relation surface, out Observation observation)
    {
        observation = null!;
        if (surface is not { Left: Relation { Left: var eventId,
                    Right: Relation { Left: Relation { Left: Relation order, Right: Relation domain }, Right: var context } question },
                Right: Relation { Left: Relation orientation, Right: Relation response } } ||
            Same(order.Left, order.Right) || !Closed(domain) || E(domain).Length != 2 ||
            !Same(E(domain)[0], order.Left) || !Same(E(domain)[1], order.Right) ||
            !EitherOrder(order, orientation) || !EitherOrder(orientation, response)) return false;
        observation = new(eventId, question, orientation, response, response.Left, context,
            Same(response.Left, order.Left), surface); return true;
    }
    public static Relation Offer(Relation observation, Relation likelihoodBindings,
        Relation conditionedEvents, Relation independentEvents, IElement premise) =>
        new(new Relation(observation, new Relation(conditionedEvents, independentEvents)), new Relation(likelihoodBindings, premise));

    /// <summary>An exact diagnostic projection. All durable input and output
    /// expressions remain relation-shaped. No floating-point update is used.</summary>
    public readonly record struct Ratio
    {
        public BigInteger Numerator { get; }
        public BigInteger Denominator { get; }
        public Ratio(BigInteger numerator, BigInteger denominator)
        {
            if (denominator <= 0 || numerator < 0) throw new ArgumentOutOfRangeException(nameof(denominator));
            var gcd = BigInteger.GreatestCommonDivisor(numerator, denominator);
            Numerator = numerator / gcd; Denominator = denominator / gcd;
        }
        public static Ratio Zero => new(0, 1);
        public static Ratio One => new(1, 1);
        public static Ratio operator +(Ratio a, Ratio b) => new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);
        public static Ratio operator *(Ratio a, Ratio b) => new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
        public static Ratio operator /(Ratio a, Ratio b) => new(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
        public Ratio Complement() => new(Denominator - Numerator, Denominator);
        public double ToDouble() => (double)Numerator / (double)Denominator;
        public override string ToString() => Numerator + "/" + Denominator;
    }
    public sealed record Row(IElement Model, Ratio? Probability);
    public sealed class Distribution
    {
        public bool Complete { get; }
        public bool Inconsistent { get; }
        public IReadOnlyList<Row> Rows { get; }
        public IElement[] Events { get; }
        public Relation Surface { get; }
        public Relation Expression { get; }
        public Ratio? EvidenceProbability { get; }
        public Relation EvidenceExpression { get; }
        internal Distribution(bool complete, bool inconsistent, Row[] rows, IElement[] events, Relation expression, Relation surface,
            Ratio? evidenceProbability = null, Relation? evidenceExpression = null)
        {
            Complete = complete; Inconsistent = inconsistent; Rows = rows; Events = events; Expression = expression; Surface = surface;
            EvidenceProbability = evidenceProbability; EvidenceExpression = evidenceExpression ?? StructuralConstraint.Open();
        }
    }
    public sealed class Transition
    {
        public Relation State { get; }
        public Relation Surface { get; }
        public bool Accepted { get; }
        public bool Duplicate { get; }
        public bool Contradicted { get; }
        public bool Open { get; }
        internal Transition(Relation before, Relation offered, Relation after, Relation accepted, Relation duplicate,
            Relation blocked, Relation open, Relation proofs)
        {
            State = after; Accepted = E(accepted).Length != 0; Duplicate = E(duplicate).Length != 0;
            Contradicted = E(blocked).Length != 0; Open = E(open).Length != 0;
            Surface = new(new Relation(before, offered), new Relation(after,
                new Relation(accepted, new Relation(duplicate, new Relation(blocked, new Relation(open, proofs))))));
        }
    }
    public sealed record ResolutionRead(bool Complete, Ratio Lower, Ratio Upper, Relation Surface);

    public static bool TryReadPartition(Relation partition, IElement unit, out Ratio ratio)
    {
        ratio = default;
        if (partition is not { Left: Relation basis, Right: Relation { Left: Relation selected, Right: Relation whole } } ||
            !Same(basis.Left, unit) || !Closed(selected) || !Closed(whole)) return false;
        var n = E(selected); var d = E(whole);
        if (d.Length == 0 || n.Length > d.Length || n.Concat(d).Any(item => !Same(item, unit))) return false;
        ratio = new(n.Length, d.Length); return true;
    }

    public static Transition Update(Relation state, Relation factor)
    {
        if (!TryState(state, out var frame, out _, out var factors) || !TryFactor(factor, out var observation, out var rows, out var conditioned, out var independent))
            return Receipt(open: S(factor));
        foreach (var existing in factors)
        {
            if (!TryFactor(existing, out var priorObservation, out var priorRows, out _, out _)) return Receipt(open: S(existing));
            if (!Same(priorObservation.Event, observation.Event)) continue;
            // Inverse question readings of the same actual report are one
            // event. Changed outcome/calibration is a conflict to revise, not
            // another independent contribution from the same event identity.
            if (SameReport(priorObservation, observation) && SameLikelihoods(frame, priorRows, rows))
                return Receipt(duplicate: S(factor), proofs: S(new Relation(existing, factor)));
            return Receipt(blocked: S(new Relation(existing, factor)));
        }
        var events = factors.Select(item => { TryFactor(item, out var read, out _, out _, out _); return read.Event; }).ToArray();
        if (conditioned.Any(item => !events.Contains(item, Ref)) ||
            conditioned.Concat(independent).Any(item => Same(item, observation.Event)) ||
            conditioned.Intersect(independent, Ref).Any() ||
            events.Any(item => !conditioned.Contains(item, Ref) && !independent.Contains(item, Ref)))
            return Receipt(open: S(new Relation(S(events), new Relation(S(conditioned), S(independent)))));
        var next = new Relation(state.Left, S(factors.Cast<IElement>().Append(factor).ToArray()));
        var readNext = Read(next);
        if (!readNext.Complete)
            return Receipt(blocked: readNext.Inconsistent ? S(next) : S(), open: readNext.Inconsistent ? S() : S(next), proofs: S(readNext.Surface));
        return Receipt(after: next, accepted: S(factor), proofs: S(readNext.Surface));

        Transition Receipt(Relation? after = null, Relation? accepted = null, Relation? duplicate = null,
            Relation? blocked = null, Relation? open = null, Relation? proofs = null) =>
            new(state, factor, after ?? state, accepted ?? S(), duplicate ?? S(), blocked ?? S(), open ?? S(), proofs ?? S());
    }

    /// <summary>Reads the entire declared finite choice. Missing likelihoods
    /// remain open; an all-zero normalizer is a failed scope, not an even tie.</summary>
    public static Distribution Read(Relation state)
    {
        if (!TryState(state, out var frame, out var priors, out var factors)) return Failed(false, S(state));
        var products = new List<Relation>(); var weights = new List<Ratio>(); var events = new List<IElement>();
        var open = new List<IElement>(); var priorMap = Rows(priors);
        if (!CompleteRows(frame, priorMap)) return Failed(false, S(priors));
        var previous = new List<IElement>();
        foreach (var factor in factors)
        {
            if (!TryFactor(factor, out var observation, out var likelihoods, out var conditioned, out var independent) ||
                previous.Contains(observation.Event, Ref) || conditioned.Any(item => !previous.Contains(item, Ref)) ||
                conditioned.Intersect(independent, Ref).Any() ||
                conditioned.Concat(independent).Any(item => Same(item, observation.Event)) ||
                previous.Any(item => !conditioned.Contains(item, Ref) && !independent.Contains(item, Ref)) ||
                !CompleteRows(frame, Rows(likelihoods))) return Failed(false, S(factor));
            previous.Add(observation.Event); events.Add(observation.Event);
        }
        foreach (var model in frame.Models)
        {
            var terms = new List<IElement> { priorMap.Single(row => Same(row.Left, model)).Right };
            foreach (var factor in factors)
            {
                TryFactor(factor, out var observation, out var bindings, out _, out _);
                var likelihood = Rows(bindings).Single(row => Same(row.Left, model)).Right;
                terms.Add(new Relation(observation.Surface, likelihood));
            }
            var product = S(terms.ToArray()); products.Add(product);
            if (!TryProduct(product, frame.Unit, out var weight)) open.Add(product);
            weights.Add(weight);
        }
        var whole = S(products.Cast<IElement>().ToArray()); var expression = new Relation(whole, whole);
        if (open.Count != 0) return new(false, false, frame.Models.Select(model => new Row(model, null)).ToArray(), events.ToArray(),
            expression, new Relation(state, new Relation(expression, StructuralConstraint.Open(open.ToArray()))));
        var total = weights.Aggregate(Ratio.Zero, (sum, weight) => sum + weight);
        var priorTotal = priorMap.Aggregate(Ratio.Zero, (sum, row) => {
            TryReadPartition((Relation)row.Right, frame.Unit, out var weight); return sum + weight;
        });
        var priorProducts = S(priorMap.Select(row => (IElement)S(row.Right)).ToArray());
        var evidenceExpression = new Relation(priorProducts, whole);
        Ratio? evidenceProbability = priorTotal.Numerator == 0 ? null : total / priorTotal;
        if (total.Numerator == 0) return new(false, true, frame.Models.Select(model => new Row(model, null)).ToArray(), events.ToArray(),
            expression, new Relation(new Relation(state, evidenceExpression), new Relation(expression, S())), evidenceProbability, evidenceExpression);
        var normalized = frame.Models.Select((model, index) => new Row(model, weights[index] / total)).ToArray();
        // Each row's numerator points at its actual product participation.
        var queries = S(frame.Models.Select((model, index) => (IElement)new Relation(model, new Relation(S(products[index]), whole))).ToArray());
        return new(true, false, normalized, events.ToArray(), expression, new Relation(new Relation(state, evidenceExpression), queries),
            evidenceProbability, evidenceExpression);

        Distribution Failed(bool inconsistent, Relation why) => new(false, inconsistent, [], [], new(why, why),
            new Relation(state, StructuralConstraint.Open(why)));
    }

    /// <summary>Publishes enclosing equal-partition cells from this exact
    /// finite fixture. More display partitions add no observation or support.
    /// This is not an assertion of finite-precision internal execution.</summary>
    public static ResolutionRead Quantize(Relation state, IElement model, int partitions)
    {
        if (partitions is < 1 or > 65536) throw new ArgumentOutOfRangeException(nameof(partitions));
        var read = Read(state);
        if (!read.Complete || !TryState(state, out var frame, out _, out _) ||
            read.Rows.SingleOrDefault(row => Same(row.Model, model))?.Probability is not Ratio p)
            return new(false, Ratio.Zero, Ratio.One, new Relation(new Relation(state, model), StructuralConstraint.Open(read.Surface)));
        var scaled = p.Numerator * partitions; var low = BigInteger.DivRem(scaled, p.Denominator, out var remainder);
        var high = remainder.IsZero ? low : low + 1;
        var lower = frame.Weight((int)low, partitions, read.Surface);
        var upper = frame.Weight((int)high, partitions, read.Surface);
        return new(true, new(low, partitions), new(high, partitions), new Relation(new Relation(state, model),
            new Relation(new Relation(lower, upper), read.Surface)));
    }

    /// <summary>An explicit prior replacement. Earlier state and justification
    /// remain in the receipt; accepted observation factors are retained and
    /// reevaluated. It is not an additional observation or automatic revival.</summary>
    public static Transition RevisePrior(Relation state, Relation newPriorBindings, IElement justification)
    {
        if (!TryState(state, out var frame, out _, out var factors))
            return new(state, newPriorBindings, state, S(), S(), S(), S(state), S(justification));
        var next = new Relation(new Relation(frame.Surface, newPriorBindings), S(factors.Cast<IElement>().ToArray()));
        var read = Read(next);
        return new(state, newPriorBindings, read.Complete ? next : state,
            read.Complete ? S(newPriorBindings) : S(), S(), read.Inconsistent ? S(next) : S(),
            !read.Complete && !read.Inconsistent ? S(next) : S(), S(justification, read.Surface));
    }

    public static bool TryReadExpression(Relation expression, IElement unit, out Ratio result)
    {
        result = default;
        if (expression is not { Left: Relation numerator, Right: Relation denominator } || !Closed(numerator) || !Closed(denominator)) return false;
        var remaining = E(denominator).ToList();
        foreach (var item in E(numerator))
        {
            var at = remaining.FindIndex(other => Same(other, item));
            if (at < 0) return false;
            remaining.RemoveAt(at);
        }
        if (!Sum(numerator, out var n) || !Sum(denominator, out var d) || d.Numerator == 0) return false;
        result = n / d; return true;
        bool Sum(Relation scope, out Ratio value)
        {
            value = Ratio.Zero;
            foreach (var item in E(scope))
            { if (item is not Relation product || !TryProduct(product, unit, out var p)) return false; value += p; }
            return true;
        }
    }
    private static bool TryProduct(Relation product, IElement unit, out Ratio result)
    {
        result = Ratio.One;
        if (!Closed(product) || E(product).Length == 0) return false;
        var terms = E(product);
        if (terms[0] is not Relation prior || !TryReadPartition(prior, unit, out result)) return false;
        foreach (var item in terms.Skip(1))
        {
            if (item is not Relation { Left: Relation observation, Right: Relation likelihood } ||
                !TryReadObservation(observation, out var report) || !TryReadPartition(likelihood, unit, out var q)) return false;
            result *= report.Positive ? q : q.Complement();
        }
        return true;
    }
    private static bool TryState(Relation state, out Frame frame, out Relation priors, out Relation[] factors)
    {
        frame = null!; priors = null!; factors = [];
        if (state is not { Left: Relation { Left: Relation basis, Right: Relation bindings }, Right: Relation history } ||
            !Frame.TryRead(basis, out frame) || !Closed(bindings) || !Closed(history) || E(history).Any(item => item is not Relation)) return false;
        if (E(bindings).Any(item => item is not Relation)) return false;
        priors = bindings; factors = E(history).Cast<Relation>().ToArray(); return true;
    }
    private static bool TryFactor(Relation factor, out Observation observation, out Relation likelihoods,
        out IElement[] conditioned, out IElement[] independent)
    {
        observation = null!; likelihoods = null!; conditioned = independent = [];
        if (factor is not { Left: Relation { Left: Relation report, Right: Relation { Left: Relation parents, Right: Relation separate } },
                Right: Relation { Left: Relation rows } } || !TryReadObservation(report, out observation) ||
            !Closed(parents) || !Closed(separate) || !Closed(rows) || E(rows).Any(item => item is not Relation)) return false;
        conditioned = E(parents); independent = E(separate); likelihoods = rows;
        return conditioned.Distinct(Ref).Count() == conditioned.Length && independent.Distinct(Ref).Count() == independent.Length;
    }
    private static bool CompleteRows(Frame frame, Relation[] rows) => rows.Length == frame.Models.Length &&
        rows.Select(row => row.Left).Distinct(Ref).Count() == rows.Length && rows.All(row => frame.Models.Contains(row.Left, Ref));
    private static Relation[] Rows(Relation scope) => E(scope).OfType<Relation>().ToArray();
    private static bool SameReport(Observation a, Observation b) => Same(a.Outcome, b.Outcome) &&
        Same(a.Context, b.Context) && a.Question.Left is Relation { Left: Relation ao } && b.Question.Left is Relation { Left: Relation bo } &&
        Same(ao.Left, bo.Left) && Same(ao.Right, bo.Right);
    private static bool SameLikelihoods(Frame frame, Relation first, Relation second)
    {
        var a = Rows(first); var b = Rows(second);
        return CompleteRows(frame, a) && CompleteRows(frame, b) && frame.Models.All(model =>
            a.Single(row => Same(row.Left, model)).Right is Relation p &&
            b.Single(row => Same(row.Left, model)).Right is Relation q &&
            TryReadPartition(p, frame.Unit, out var x) && TryReadPartition(q, frame.Unit, out var y) && x == y);
    }
    private static bool EitherOrder(Relation order, Relation other) =>
        Same(order.Left, other.Left) && Same(order.Right, other.Right) ||
        Same(order.Left, other.Right) && Same(order.Right, other.Left);
    private static bool Same(IElement left, IElement right) =>
        ReferenceEquals(left, right) || left is Placeholder a && right is Placeholder b && a.Equals(b);
    private sealed class ParticipantComparer : IEqualityComparer<IElement>
    {
        public bool Equals(IElement? left, IElement? right) =>
            left is null ? right is null : right is not null && Same(left, right);
        public int GetHashCode(IElement item) => item is Placeholder placeholder
            ? placeholder.GetHashCode() : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(item);
    }
    private static readonly IEqualityComparer<IElement> Ref = new ParticipantComparer();
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var start, out var end) && Same(start, end);
}
