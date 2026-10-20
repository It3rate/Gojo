using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Active finite-library inference. Query decisions inspect only the
/// receiver's public candidate predictions, never the concealed supplier. A
/// supplier publishes opaque event-qualified answers; its certifying source
/// witnesses remain in a separate audit root. This is a fixed query policy,
/// not model training or unrestricted concept discovery.</summary>
public static class OrnamentReconstructionDialogue
{
    public sealed class Request
    {
        public R.Query Query { get; }
        public Relation Target { get; }
        public Relation Surface { get; }
        public bool IsValid => Surface.Left is Relation before && ReferenceEquals(before.Right, Target) &&
            ReferenceEquals(Surface.Right, Query.Surface);
        public Request(Relation receiverState, Relation target, R.Query query)
            : this(query, target, new(new Relation(receiverState, target), query.Surface)) { }
        public Request(R.Query query, Relation target, Relation surface)
        { Query = query; Target = target; Surface = surface; }
    }

    public sealed class Response
    {
        // response = actual request -> ((opaque source -> opaque revision) ->
        //   (actual query -> (yes -> (no -> (open -> blocked)))))
        public Request Request { get; }
        public R.Query Query => Request.Query;
        public Relation Surface { get; }
        public Relation AuditSurface { get; }
        public Relation Yes { get; } = S();
        public Relation No { get; } = S();
        public Relation Open { get; } = S();
        public Relation Blocked { get; } = S();
        public bool IsValid { get; }
        public string Answer => !IsValid || E(Blocked).Length != 0 ? "Blocked" : E(Open).Length != 0 ? "Open" :
            E(Yes).Length == 1 ? "Yes" : E(No).Length == 1 ? "No" : "Blocked";

        public Response(Request request, Relation surface, Relation? auditSurface = null)
        {
            Request = request; Surface = surface; AuditSurface = auditSurface ?? S();
            if (!request.IsValid || request.Target.Right is not Relation advertisedEvent ||
                surface is not { Left: var actualRequest, Right: Relation { Left: Relation actualEvent,
                    Right: Relation { Left: var actualQuery, Right: Relation { Left: Relation yes,
                        Right: Relation { Left: Relation no, Right: Relation { Left: Relation open, Right: Relation blocked } } } } } } ||
                !ReferenceEquals(actualRequest, request.Surface) || !ReferenceEquals(actualQuery, request.Query.Surface) ||
                !ReferenceEquals(actualEvent.Left, advertisedEvent.Left) || !ReferenceEquals(actualEvent.Right, advertisedEvent.Right) ||
                !Closed(yes, out var yesRows) || !Closed(no, out var noRows) || !Closed(blocked, out var blockedRows) ||
                !StructuralConstraint.TryReadScope(open, out var openRows, out var seam, out var tail) ||
                yesRows.Concat(noRows).Concat(openRows).Concat(blockedRows).Any(item => !ReferenceEquals(item, request.Query.Surface)) ||
                yesRows.Length + noRows.Length + openRows.Length + blockedRows.Length != 1 ||
                (openRows.Length == 1 ? seam.Equals(tail) : !seam.Equals(tail))) return;
            Yes = yes; No = no; Open = open; Blocked = blocked; IsValid = true;
        }
    }

    /// <summary>The receiver sees this advertised query service and opaque
    /// identity only. The source bank and observation are not exposed through
    /// Surface, requests, replies or generated messages.</summary>
    public sealed class Supplier
    {
        private readonly R.Bank bank;
        private readonly R.Input source;
        public IElement Source { get; } = new Placeholder();
        public IElement Revision { get; } = new Placeholder();
        public Relation Surface { get; }
        public Relation Port => Surface;
        public Relation AuditSurface => new(Surface, new Relation(bank.Surface, source.Surface));
        public Supplier(R.Bank bank, R.Input source)
        {
            this.bank = bank; this.source = source;
            Surface = new(S(bank.Queries.Select(query => (IElement)query.Surface).ToArray()), new Relation(Source, Revision));
        }
        public Response Reply(Request request)
        {
            var query = request.Query;
            R.Receipt? receipt = null;
            var answer = "Blocked";
            if (request.IsValid && ReferenceEquals(request.Target, Surface) &&
                bank.Queries.Any(actual => ReferenceEquals(actual.Surface, query.Surface)))
            { receipt = bank.Read(source, query); answer = receipt.Answer; }
            var yes = answer == "Yes" ? S(query.Surface) : S();
            var no = answer == "No" ? S(query.Surface) : S();
            var open = answer == "Open" ? StructuralConstraint.Open(query.Surface) : S();
            var blocked = answer == "Blocked" ? S(query.Surface) : S();
            var surface = new Relation(request.Surface, new Relation(new Relation(Source, Revision),
                new Relation(query.Surface, new Relation(yes, new Relation(no, new Relation(open, blocked))))));
            var audit = new Relation(surface, receipt?.Surface ?? new Relation(AuditSurface, request.Surface));
            return new(request, surface, audit);
        }
    }

    public sealed record Decision(R.Query Query, BigInteger IfYes, BigInteger IfNo,
        int Gain, int NativeQueryCost, Relation Surface);
    public sealed record Step(R.Query Query, Response Reply, BigInteger BeforePossible,
        BigInteger AfterPossible, BigInteger BeforeAdmitted, BigInteger AfterAdmitted,
        Decision Decision, Request Request, Relation Surface);
    public sealed record PruneStep(R.Claim Removed, Relation Before, Relation After,
        BigInteger Possible, BigInteger Admitted, Relation Surface);

    public sealed class Result
    {
        public R.Message OriginalMessage { get; }
        public R.Message FinalMessage { get; }
        public BigInteger Possible { get; }
        public BigInteger Admitted { get; }
        public bool Uniform { get; }
        public string StopReason { get; }
        public Relation Blocked { get; }
        public Relation Open { get; }
        public IReadOnlyList<Step> Steps { get; }
        public IReadOnlyList<PruneStep> PruneSteps { get; }
        public Relation Surface { get; }
        public Relation AuditSurface { get; }
        internal Result(Policy policy, R.Message original, R.Message final, BigInteger possible, BigInteger admitted,
            bool uniform, string stopReason, IReadOnlyList<Step> steps, IReadOnlyList<PruneStep> prunes)
        {
            OriginalMessage = original; FinalMessage = final; Possible = possible; Admitted = admitted;
            Uniform = uniform; StopReason = stopReason; Steps = steps; PruneSteps = prunes;
            Blocked = policy.Blocked; Open = policy.Open;
            Surface = new(policy.Surface, new Relation(S(steps.Select(step => (IElement)step.Surface).ToArray()),
                new Relation(original.Surface, new Relation(S(prunes.Select(step => (IElement)step.Surface).ToArray()),
                    policy.State(final, possible, admitted)))));
            AuditSurface = new(Surface, S(steps.Select(step => (IElement)step.Reply.AuditSurface).ToArray()));
        }
    }

    public sealed class Policy
    {
        public long DecisionEvaluations { get; private set; }
        public long CachedDecisions { get; private set; }
        public OrnamentReconstructionProjection Projection { get; }
        public R.Focus Focus { get; }
        public BigInteger InitialUniverse { get; }
        public Relation Surface { get; }
        public Relation Blocked { get; }
        public Relation Open { get; }
        private readonly R.Query[] focus;
        private readonly string[] profiles;
        private readonly int[] queryCosts;
        private readonly BigInteger[] positive;
        private readonly BigInteger[] negative;
        private readonly Dictionary<(BigInteger Possible, BigInteger Asked), Decision?> decisions = [];
        public Policy(OrnamentReconstructionProjection projection, int[] focusQueryIndices, BigInteger? initialUniverse = null)
            : this(projection, new R.Focus(projection.Queries, focusQueryIndices.Select(index => projection.Queries[index]).ToArray()), initialUniverse) { }
        public Policy(OrnamentReconstructionProjection projection, R.Focus focus, BigInteger? initialUniverse = null)
        {
            Projection = projection; Focus = focus; InitialUniverse = (initialUniverse ?? projection.Universe) & projection.Universe;
            var selected = E(focus.Selected);
            this.focus = projection.Queries.Where(query => selected.Any(actual => ReferenceEquals(actual, query.Surface))).ToArray();
            var blocked = E(focus.Blocked).Concat(E(projection.Context.Blocked)).ToList();
            var open = E(focus.Open).Concat(E(projection.Context.Open)).ToList();
            if (selected.Length != this.focus.Length || E(focus.Basis).Any(item => !projection.Queries.Any(query => ReferenceEquals(query.Surface, item)))) blocked.Add(focus.Surface);
            if (selected.Length == 0) open.Add(StructuralConstraint.Open(focus.Selected));
            Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
            if (blocked.Count != 0 || open.Count != 0) InitialUniverse = BigInteger.Zero;
            profiles = projection.Inputs.Select((_, index) => string.Join("|", this.focus.Select(query => projection.ReadAnswer(index, query)))).ToArray();
            queryCosts = projection.Queries.Select(query => Math.Max(1, R.Participants(query.Surface).Count)).ToArray();
            positive = projection.Queries.Select(query => projection.PossibleMask(new(query, true))).ToArray();
            negative = projection.Queries.Select(query => projection.PossibleMask(new(query, false))).ToArray();
            Surface = new(new Relation(projection.Surface, focus.Surface), new Relation(CandidateScope(InitialUniverse), new Relation(Blocked, Open)));
        }

        /// <summary>Pure selection before a supplier reply exists. Predicted
        /// unfinished candidates remain on both answer branches. The score is
        /// worst-case settled-answer elimination per native query participant.</summary>
        public Decision? Choose(BigInteger possible, IReadOnlyList<R.Query> alreadyAsked)
        {
            var current = possible & InitialUniverse; var count = Count(current);
            var askedMask = BigInteger.Zero;
            for (var index = 0; index < Projection.Queries.Count; index++)
                if (alreadyAsked.Any(query => ReferenceEquals(query.Surface, Projection.Queries[index].Surface))) askedMask |= BigInteger.One << index;
            var key = (current, askedMask);
            if (decisions.TryGetValue(key, out var remembered)) { CachedDecisions++; return remembered; }
            DecisionEvaluations++;
            var bestIndex = -1; var bestGain = 0; var bestYes = BigInteger.Zero; var bestNo = BigInteger.Zero;
            for (var index = 0; index < Projection.Queries.Count; index++)
            {
                var query = Projection.Queries[index];
                if (!Projection.Context.Available.Any(actual => ReferenceEquals(actual.Surface, query.Surface)) ||
                    (askedMask & (BigInteger.One << index)) != BigInteger.Zero) continue;
                var ifYes = current & positive[index]; var ifNo = current & negative[index];
                var gain = count - Math.Max(Count(ifYes), Count(ifNo));
                if (gain <= 0) continue;
                if (bestIndex >= 0 && (long)gain * queryCosts[bestIndex] <= (long)bestGain * queryCosts[index]) continue;
                bestIndex = index; bestGain = gain; bestYes = ifYes; bestNo = ifNo;
            }
            Decision? best = null;
            if (bestIndex >= 0)
            {
                var query = Projection.Queries[bestIndex];
                var witness = new Relation(new Relation(CandidateScope(current), query.Surface),
                    new Relation(CandidateScope(bestYes), CandidateScope(bestNo)));
                best = new(query, bestYes, bestNo, bestGain, queryCosts[bestIndex], witness);
            }
            decisions[key] = best; return best;
        }

        public Result Run(Supplier supplier, int maxRequests = 60, bool exhaustivePrune = false)
        {
            if (maxRequests < 0) throw new ArgumentOutOfRangeException(nameof(maxRequests));
            var claims = new List<R.Claim>(); var asked = new List<R.Query>(); var steps = new List<Step>();
            var possible = InitialUniverse; var admitted = InitialUniverse; var reason = "request budget";
            for (var turn = 0; turn < maxRequests; turn++)
            {
                if (possible == BigInteger.Zero) { reason = "empty possible set"; break; }
                if (IsUniform(possible)) { reason = "known focus consensus"; break; }
                var decision = Choose(possible, asked);
                if (decision is null) { reason = "no informative advertised query"; break; }
                var beforeMessage = new R.Message(claims.ToArray()); var before = State(beforeMessage, possible, admitted);
                var request = new Request(before, supplier.Surface, decision.Query);
                var reply = supplier.Reply(request); asked.Add(decision.Query);
                var previousPossible = possible; var previousAdmitted = admitted;
                if (reply.IsValid && reply.Answer is "Yes" or "No")
                {
                    var claim = new R.Claim(decision.Query, reply.Answer); claims.Add(claim);
                    possible &= Projection.PossibleMask(claim); admitted &= Projection.Mask(claim);
                }
                // An opaque Open/refusal is not evidence that a candidate is
                // false. Its request remains tried and its response survives.
                var after = State(new(claims.ToArray()), possible, admitted);
                steps.Add(new(decision.Query, reply, previousPossible, possible, previousAdmitted, admitted, decision, request,
                    new Relation(before, new Relation(decision.Surface, new Relation(request.Surface, new Relation(reply.Surface, after))))));
            }
            if (possible == BigInteger.Zero) reason = "empty possible set";
            else if (IsUniform(possible)) reason = "known focus consensus";
            if (E(Blocked).Length != 0) reason = "blocked dialogue contract";
            else if (E(Open).Length != 0) reason = "open dialogue contract";
            var original = new R.Message(claims.ToArray()); var prunes = new List<PruneStep>();
            if (IsUniform(possible) && admitted != BigInteger.Zero)
                Prune(claims, possible, exhaustivePrune, prunes);
            var final = new R.Message(claims.ToArray());
            possible = Projection.Possible(claims) & InitialUniverse; admitted = Projection.Admitted(claims) & InitialUniverse;
            return new(this, original, final, possible, admitted, IsUniform(possible), reason, steps, prunes);
        }

        public bool IsUniform(BigInteger possible)
        {
            string? first = null;
            foreach (var index in Projection.Indices(possible & InitialUniverse))
            {
                if (focus.Length == 0 || focus.Any(query => Projection.ReadAnswer(index, query) is not ("Yes" or "No"))) return false;
                first ??= profiles[index]; if (first != profiles[index]) return false;
            }
            return first is not null;
        }

        private void Prune(List<R.Claim> claims, BigInteger inferred, bool exhaustive, List<PruneStep> steps)
        {
            var first = Projection.Indices(inferred)[0]; var profile = profiles[first];
            var sameProfile = BigInteger.Zero;
            foreach (var index in Projection.Indices(InitialUniverse))
                if (profiles[index] == profile && focus.All(query => Projection.ReadAnswer(index, query) is "Yes" or "No")) sameProfile |= BigInteger.One << index;
            if (exhaustive && claims.Count <= 12)
            {
                var all = claims.ToArray(); var best = new R.Message(all); var chosen = all;
                for (var subset = 0; subset < 1 << all.Length; subset++)
                {
                    var selected = all.Where((_, index) => (subset & 1 << index) != 0).ToArray();
                    if (!Preserves(selected)) continue;
                    var candidate = new R.Message(selected);
                    if (candidate.NativeCost < best.NativeCost || candidate.NativeCost == best.NativeCost && candidate.HintCount < best.HintCount)
                    { best = candidate; chosen = selected; }
                }
                foreach (var claim in all.Where(claim => !chosen.Contains(claim))) Remove(claim);
            }
            else
            {
                // Prefer withdrawing expensive descriptive queries first. This
                // is a backwards greedy reduction, not global minimality.
                foreach (var claim in claims.OrderByDescending(claim => R.Participants(claim.Query.Surface).Count).ToArray())
                    if (Preserves(claims.Where(actual => !ReferenceEquals(actual, claim)).ToArray())) Remove(claim);
            }

            bool Preserves(IReadOnlyList<R.Claim> proposed)
            {
                var remaining = Projection.Possible(proposed) & InitialUniverse;
                return (Projection.Admitted(proposed) & InitialUniverse) != BigInteger.Zero && remaining != BigInteger.Zero &&
                    (remaining & ~sameProfile) == BigInteger.Zero;
            }
            void Remove(R.Claim claim)
            {
                var before = new R.Message(claims.ToArray()); claims.Remove(claim); var after = new R.Message(claims.ToArray());
                var remaining = Projection.Possible(claims) & InitialUniverse; var settled = Projection.Admitted(claims) & InitialUniverse;
                var surface = new Relation(before.Surface, new Relation(claim.Surface, new Relation(after.Surface,
                    new Relation(CandidateScope(remaining), CandidateScope(settled)))));
                steps.Add(new(claim, before.Surface, after.Surface, remaining, settled, surface));
            }
        }

        internal Relation State(R.Message message, BigInteger possible, BigInteger admitted) =>
            new(message.Surface, new Relation(CandidateScope(possible), CandidateScope(admitted)));
        private Relation CandidateScope(BigInteger mask) => S(Projection.Indices(mask).Select(index => (IElement)Projection.Inputs[index].Surface).ToArray());
        private static int Count(BigInteger mask) { var count = 0; while (mask > 0) { mask &= mask - 1; count++; } return count; }
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => QualitativeRecognitionObservation.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] entries) => QualitativeRecognitionObservation.Closed(scope, out entries);
}
