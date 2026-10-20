using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A small learned query policy. The numerical regressor is a derived
/// read of retained native acquisition/outcome witnesses; it cannot supply or
/// override a candidate verdict. Each reply enters through an advertised port.</summary>
public sealed class EvidenceSelectionStudy
{
    // receiver = (engine -> context) -> (hypotheses -> (ports -> publications))
    // port = actual role scope -> request seam
    // request = receiver-before -> actual port
    // response = request -> (published observations -> blocked)
    // features = port -> Closed(feature basis -> actual participating scopes)
    // training row = features -> (before assessment -> (response -> after assessment))
    // policy = feature basis -> Closed(training rows)
    // step = before -> (features -> (response -> after))
    public sealed record Options(int TrainingEpisodes = 48, int EvaluationEpisodes = 32,
        int Seed = 731, int MaxRequests = 4, int RetainedPerPolicy = 2);
    public sealed record Form(string Name, UnitArrangementProgram Program,
        ProvisionalEquivalence.Hypothesis Hypothesis, Relation RoleSelectors);
    public sealed record Metric(string Split, string Family, string Policy, int Ordinal,
        int Requests, int Compatible, int ConflictCount, int BlockedCount, bool Identified, bool Exhausted)
    { public bool UniqueCompatible => Compatible == 1; }
    public sealed record Summary(string Policy, int Episodes, int Identified, int Open,
        int Conflicted, int Blocked, int Requests)
    { public double MeanRequests => Episodes == 0 ? 0 : (double)Requests / Episodes; }

    public sealed class EvidencePort
    {
        public string Name { get; }
        public Relation Roles { get; }
        public Relation Surface { get; }
        public EvidencePort(string name, Relation roles)
        { Name = name; Roles = roles; Surface = new(roles, new Placeholder()); }
    }
    public sealed class Response
    {
        public EvidencePort Port { get; }
        public Relation Request { get; }
        public IReadOnlyList<ProvisionalEquivalence.Publication> Publications { get; }
        public Relation Blocked { get; }
        public Relation Surface { get; }
        internal Response(EvidencePort port, Relation request,
            IReadOnlyList<ProvisionalEquivalence.Publication> publications, Relation blocked, Relation surface)
        { Port = port; Request = request; Publications = publications; Blocked = blocked; Surface = surface; }
    }

    /// <summary>The receiver contains no supplier callback, concealed answer,
    /// family name, truth index, or reply body. Pure policy reads see only this.</summary>
    public sealed class Receiver
    {
        public ProvisionalEquivalence Engine { get; }
        public IReadOnlyList<Form> Forms { get; }
        public ProvisionalEquivalence.Context Context { get; }
        public IReadOnlyList<EvidencePort> Ports { get; }
        public IReadOnlyList<ProvisionalEquivalence.Publication> Publications => publications;
        public IReadOnlyList<EvidencePort> Requested => requested;
        public Relation Rejected { get; private set; } = S();
        public ProvisionalEquivalence.Selection Selection { get; private set; }
        public Relation Surface => new(new Relation(Engine.Surface, Context.Surface), new Relation(
            S(Forms.Select(form => (IElement)form.Hypothesis.Surface).ToArray()), new Relation(
                S(Ports.Select(port => (IElement)port.Surface).ToArray()), new Relation(
                    S(publications.Select(pub => (IElement)pub.Surface).ToArray()), new Relation(
                        S(requested.Select(port => (IElement)port.Surface).ToArray()),
                        new Relation(S(pending.Cast<IElement>().ToArray()), Rejected))))));
        private readonly List<ProvisionalEquivalence.Publication> publications = [];
        private readonly List<EvidencePort> requested = [];
        private readonly HashSet<Relation> pending = new(ReferenceEqualityComparer.Instance);
        public Receiver(ProvisionalEquivalence engine, IReadOnlyList<Form> forms,
            ProvisionalEquivalence.Context context, IReadOnlyList<EvidencePort> ports)
        { Engine = engine; Forms = forms; Context = context; Ports = ports; Selection = Assess(); }
        internal Relation IssueRequest(EvidencePort port)
        {
            var request = new Relation(Surface, port.Surface); pending.Add(request); return request;
        }
        public ProvisionalEquivalence.Selection Receive(Response response)
        {
            var permitted = pending.Remove(response.Request) && Ports.Any(port => ReferenceEquals(port, response.Port)) &&
                ReferenceEquals(response.Request.Right, response.Port.Surface) &&
                response.Publications.All(pub => E(pub.Values).All(item => item is Relation row &&
                    E(response.Port.Roles).Any(role => ReferenceEquals(role, row.Left))));
            if (!permitted || E(response.Blocked).Length != 0)
            { Rejected = S([.. E(Rejected), response.Surface]); return Selection; }
            if (!requested.Any(port => ReferenceEquals(port, response.Port))) requested.Add(response.Port);
            foreach (var pub in response.Publications)
                if (!publications.Any(old => ReferenceEquals(old, pub))) publications.Add(pub);
            Selection = Assess(); return Selection;
        }
        private ProvisionalEquivalence.Selection Assess() => Engine.Assess(
            Forms.Select(form => form.Hypothesis).ToArray(), publications, Context);
    }

    /// <summary>Supplier and receiver stay separate. Request is the only public
    /// access to the concealed role words. Names are report labels only.</summary>
    public sealed class Scenario
    {
        public string Family { get; }
        public Receiver Receiver { get; }
        public QualitativeMotionFrame Frame { get; }
        public IElement Reach { get; }
        public IElement[] Roles { get; }
        public Relation Domain => Receiver.Engine.Domain;
        private readonly ProvisionalEquivalence.Publication[] concealed;
        internal Scenario(string family, Receiver receiver, QualitativeMotionFrame frame,
            IElement reach, IElement[] roles, ProvisionalEquivalence.Publication[] concealed)
        { Family = family; Receiver = receiver; Frame = frame; Reach = reach; Roles = roles; this.concealed = concealed; }
        public Response Request(EvidencePort port)
        {
            var request = Receiver.IssueRequest(port);
            if (!Receiver.Ports.Any(actual => ReferenceEquals(actual, port)))
                return new(port, request, [], S(port.Surface), new Relation(request, new Relation(S(), S(port.Surface))));
            var pubs = concealed.Select(pub => new ProvisionalEquivalence.Publication(pub.Source, pub.Revision,
                S(E(pub.Values).OfType<Relation>().Where(row => E(port.Roles).Any(role => ReferenceEquals(role, row.Left)))
                    .Cast<IElement>().ToArray()))).ToArray();
            var blocked = S();
            return new(port, request, pubs, blocked,
                new Relation(request, new Relation(S(pubs.Select(pub => (IElement)pub.Surface).ToArray()), blocked)));
        }
        /// <summary>Post-run evaluator only: this observes the supplier's full
        /// publication. It is not a receiver input or a policy feature.</summary>
        public ProvisionalEquivalence.Selection Evaluate() => Receiver.Engine.Assess(
            Receiver.Forms.Select(form => form.Hypothesis).ToArray(), concealed, Receiver.Context);
    }

    public sealed record FeatureRead(EvidencePort Port, Relation Surface, double[] Values);
    public sealed record Step(EvidencePort Port, Relation Features, double[] FeatureValues,
        double Prediction, IReadOnlyList<ProvisionalEquivalence.Publication> Publications,
        ProvisionalEquivalence.Selection Before, ProvisionalEquivalence.Selection After,
        Relation Response, Relation Surface);
    public sealed record Episode(string Id, string Family, string Policy, Receiver Receiver,
        QualitativeMotionFrame Frame, IElement Reach, IElement[] Roles, Relation Domain,
        IReadOnlyList<Form> Forms, ProvisionalEquivalence.Context Context,
        ProvisionalEquivalence.Selection Initial, IReadOnlyList<Step> Steps,
        ProvisionalEquivalence.Selection Final, ProvisionalEquivalence.Selection Evaluation,
        Metric Metric, Relation Surface);

    public sealed class LearnedPolicy
    {
        public Relation Basis { get; }
        public Relation Surface { get; }
        public IReadOnlyList<double> Coefficients { get; }
        public int TrainingRows { get; }
        internal LearnedPolicy(Relation basis, IReadOnlyList<(FeatureRead Features,
            ProvisionalEquivalence.Selection Before, Response Reply, ProvisionalEquivalence.Selection After)> rows)
        {
            Basis = basis; TrainingRows = rows.Count;
            Surface = new(basis, S(rows.Select(row => (IElement)new Relation(row.Features.Surface,
                new Relation(row.Before.Surface, new Relation(row.Reply.Surface, row.After.Surface)))).ToArray()));
            // Ridge least squares is a CPU backend over the native witness rows.
            // Target is observed finite-library reduction; a zero-compatible
            // contradiction is not rewarded as successful identification.
            Coefficients = ReadCoefficients(Surface);
        }
        private LearnedPolicy(Relation surface)
        {
            Surface = surface; Basis = (Relation)surface.Left;
            TrainingRows = E((Relation)surface.Right).Length;
            Coefficients = ReadCoefficients(surface);
        }
        public static LearnedPolicy FromSurface(Relation surface) => new(surface);
        public static double[] ReadCoefficients(Relation surface)
        {
            var matrix = new double[5, 6];
            foreach (var item in E((Relation)surface.Right))
            {
                var row = (Relation)item; var before = (Relation)((Relation)row.Right).Left;
                var after = (Relation)((Relation)((Relation)row.Right).Right).Right;
                var beforeCount = EligibleCount(before); var afterCount = EligibleCount(after);
                var x = ReadFeatureValues((Relation)row.Left, beforeCount);
                var y = afterCount == 0 ? -1 : (double)(beforeCount - afterCount) / Math.Max(1, beforeCount - 1);
                for (var i = 0; i < 5; i++)
                { for (var j = 0; j < 5; j++) matrix[i, j] += x[i] * x[j]; matrix[i, 5] += x[i] * y; }
            }
            for (var i = 0; i < 5; i++) matrix[i, i] += .05;
            for (var pivot = 0; pivot < 5; pivot++)
            {
                var scale = matrix[pivot, pivot];
                for (var col = pivot; col < 6; col++) matrix[pivot, col] /= scale;
                for (var row = 0; row < 5; row++) if (row != pivot)
                { var factor = matrix[row, pivot]; for (var col = pivot; col < 6; col++) matrix[row, col] -= factor * matrix[pivot, col]; }
            }
            return Enumerable.Range(0, 5).Select(i => matrix[i, 5]).ToArray();
        }
        public FeatureRead Features(Receiver receiver, EvidencePort port) => ReadFeatures(receiver, port, Basis);
        public double Predict(FeatureRead features) => features.Values.Select((value, index) => value * Coefficients[index]).Sum();
        public EvidencePort? Select(Receiver receiver) => Available(receiver)
            .Select(port => (Port: port, Score: Predict(Features(receiver, port))))
            .OrderByDescending(item => item.Score).FirstOrDefault().Port;
    }

    public LearnedPolicy Policy { get; }
    public IReadOnlyList<Summary> Summaries { get; }
    public IReadOnlyList<Metric> Metrics { get; }
    public IReadOnlyList<Episode> Episodes { get; }
    public Relation Surface { get; }
    public Options Configuration { get; }
    private EvidenceSelectionStudy(Options options, LearnedPolicy policy, IReadOnlyList<Metric> metrics,
        IReadOnlyList<Episode> episodes)
    {
        Configuration = options; Policy = policy; Metrics = metrics; Episodes = episodes;
        Summaries = metrics.Where(item => item.Split == "held-out").GroupBy(item => item.Policy)
            .Select(group => new Summary(group.Key, group.Count(), group.Count(item => item.Identified),
                group.Count(item => !item.Identified && item.ConflictCount == 0 && item.BlockedCount == 0),
                group.Count(item => item.ConflictCount > 0), group.Count(item => item.BlockedCount > 0), group.Sum(item => item.Requests))).ToArray();
        Surface = new(policy.Surface, S(episodes.Select(item => (IElement)item.Surface).ToArray()));
    }

    public static EvidenceSelectionStudy Build(Options? options = null)
    {
        options ??= new();
        if (options.TrainingEpisodes < 1 || options.EvaluationEpisodes < 1 || options.MaxRequests < 1 || options.RetainedPerPolicy < 0)
            throw new ArgumentOutOfRangeException(nameof(options));
        var random = new Random(options.Seed); var basis = S(new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder());
        var rows = new List<(FeatureRead, ProvisionalEquivalence.Selection, Response, ProvisionalEquivalence.Selection)>();
        var metrics = new List<Metric>(); var retained = new List<Episode>();
        for (var ordinal = 0; ordinal < options.TrainingEpisodes; ordinal++)
        {
            var scenario = CreateScenario(ordinal % 2 == 0 ? "single-exception" : "period-four", ordinal / 8, ordinal / 2 % 4);
            var receiver = scenario.Receiver; var initial = receiver.Selection; var requests = 0;
            while (requests < options.MaxRequests && receiver.Selection.Eligible.Count > 1 && Available(receiver).Length > 0)
            {
                var ports = Available(receiver); var port = ports[random.Next(ports.Length)];
                var features = ReadFeatures(receiver, port, basis); var before = receiver.Selection;
                var response = scenario.Request(port); var after = receiver.Receive(response);
                rows.Add((features, before, response, after)); requests++;
            }
            metrics.Add(Measure("training", scenario.Family, "exploration", ordinal, requests, receiver, options.MaxRequests, scenario.Evaluate()));
        }
        var learned = new LearnedPolicy(basis, rows);
        foreach (var policy in new[] { "fixed", "random", "disagreement", "learned" })
        {
            for (var ordinal = 0; ordinal < options.EvaluationEpisodes; ordinal++)
            {
                var scenario = CreateScenario(ordinal % 2 == 0 ? "contiguous-block" : "reflected-pair", ordinal / 8, ordinal / 2 % 4);
                var episode = Run(scenario, policy, learned, options.MaxRequests, new Random(options.Seed + ordinal), "held-out", ordinal);
                metrics.Add(episode.Metric);
                if (ordinal < options.RetainedPerPolicy) retained.Add(episode);
            }
        }
        foreach (var control in new[] { "open", "conflict", "incapable", "unavailable" })
        {
            var episode = Run(CreateScenario("contiguous-block", 3, 2, control), "learned", learned,
                options.MaxRequests, new Random(options.Seed), control, 0);
            metrics.Add(episode.Metric); retained.Add(episode);
        }
        return new(options, learned, metrics, retained);
    }

    public static Episode Run(Scenario scenario, string policy, LearnedPolicy learned, int budget,
        Random random, string split = "held-out", int ordinal = 0)
    {
        var receiver = scenario.Receiver; var initial = receiver.Selection; var steps = new List<Step>();
        while (steps.Count < budget && receiver.Selection.Eligible.Count > 1 && Available(receiver).Length > 0)
        {
            var available = Available(receiver);
            var port = policy switch
            {
                "fixed" => available[0], "random" => available[random.Next(available.Length)],
                "disagreement" => available.OrderByDescending(item => ReadFeatures(receiver, item, learned.Basis).Values[1]).First(),
                "learned" => learned.Select(receiver)!,
                _ => throw new ArgumentException("Unknown comparison policy.", nameof(policy))
            };
            var features = learned.Features(receiver, port); var before = receiver.Selection;
            var response = scenario.Request(port); var after = receiver.Receive(response);
            steps.Add(new(port, features.Surface, features.Values, learned.Predict(features), response.Publications,
                before, after, response.Surface, new Relation(before.Surface,
                    new Relation(features.Surface, new Relation(response.Surface, after.Surface)))));
        }
        var evaluation = scenario.Evaluate();
        var metric = Measure(split, scenario.Family, policy, ordinal, steps.Count, receiver, budget, evaluation);
        return new($"{split}-{policy}-{ordinal:000}", scenario.Family, policy, receiver, scenario.Frame,
            scenario.Reach, scenario.Roles, scenario.Domain, receiver.Forms, receiver.Context, initial,
            steps, receiver.Selection, evaluation, metric, new Relation(initial.Surface,
                new Relation(S(steps.Select(item => (IElement)item.Surface).ToArray()),
                    new Relation(receiver.Selection.Surface, evaluation.Surface))));
    }

    /// <summary>Four finite source-construction recipes. Family and truth are
    /// generator inputs only; receiver and policy receive neither of them.</summary>
    public static Scenario CreateScenario(string family, int variant, int truth, string? control = null)
    {
        if (truth is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(truth));
        var cells = family is "single-exception" or "period-four" ? 4 + variant % 2 : 8 + variant % 2;
        var frame = new QualitativeMotionFrame(); IElement reach = new Placeholder();
        var author = new LatticeLadderAuthor(frame, reach);
        var roles = Enumerable.Range(0, cells * 2).Select(_ => (IElement)new Placeholder()).ToArray();
        var domain = S(roles); var forms = new List<Form>(); var allValues = new List<Relation>();
        for (var candidate = 0; candidate < 4; candidate++)
        {
            var widths = Enumerable.Range(0, cells).Select(cell => 1 + ((family switch
            {
                "single-exception" => cell == candidate,
                "period-four" => (cell + candidate) % 4 < 2,
                "contiguous-block" => (cell + candidate) % 5 < 3,
                "reflected-pair" => cell == candidate || cell == cells - 1 - candidate,
                _ => throw new ArgumentException("Unknown source recipe family.", nameof(family))
            }) ? 1 + variant % 2 : 0)).ToArray();
            var paths = widths.Select((width, index) => LatticeLadderAuthor.Path(author.Word(index * 5, 0),
                author.Word(width, 0), author.Word(0, 1), author.Word(-width, 0), author.Word(0, -1))).ToArray();
            var selectors = S(paths.SelectMany((path, index) => new IElement[] {
                new Relation(roles[index * 2], new Relation(path, E((Relation)path.Right)[0])),
                new Relation(roles[index * 2 + 1], new Relation(path, E((Relation)path.Right)[1])) }).ToArray());
            var program = new UnitArrangementProgram(frame, $"candidate-{candidate}", S(paths),
                S([.. E(selectors), .. E(LatticeLadderAuthor.Ports(paths))]), S(paths));
            var roleValues = widths.SelectMany(width => new[] { width, 1 }).ToArray();
            var groups = roleValues.Distinct().Order().ToArray();
            var parameters = groups.Select(_ => (IElement)new Placeholder()).ToArray();
            var bindings = S(roles.Select((role, index) => (IElement)new Relation(role, parameters[Array.IndexOf(groups, roleValues[index])])).ToArray());
            var parameterValues = S(groups.Select((value, index) => (IElement)new Relation(parameters[index],
                S(Enumerable.Repeat(reach, value).ToArray()))).ToArray());
            var equalities = S(roles.SelectMany((role, index) => roles.Skip(index + 1)
                .Where((_, offset) => roleValues[index] == roleValues[index + offset + 1])
                .Select(other => (IElement)new Relation(role, other))).ToArray());
            var hypothesis = new ProvisionalEquivalence.Hypothesis(domain, program.Surface, bindings, parameterValues, equalities,
                new Relation(program.Surface, selectors));
            forms.Add(new($"candidate-{candidate}", program, hypothesis, selectors));
            allValues.Add(S(roles.Select((role, index) => (IElement)new Relation(role,
                control == "open" ? StructuralConstraint.Open() : S(Enumerable.Repeat(reach, roleValues[index]).ToArray()))).ToArray()));
        }
        var query = S(new Relation(roles[(variant % cells) * 2], roles[(variant % cells) * 2 + 1]));
        var capabilities = forms.Where((_, index) => control != "incapable" &&
            (control != "unavailable" || index != truth)).Select(form => (IElement)form.Program.Surface).ToArray();
        var context = new ProvisionalEquivalence.Context(S(capabilities), query,
            S(forms.Select(form => (IElement)form.Hypothesis.Surface).ToArray()));
        // Fixed order deliberately includes uninformative vertical services.
        // Other policies see exactly the same order and advertised role scopes.
        var ports = roles.Where((_, index) => index % 2 == 1).Concat(roles.Where((_, index) => index % 2 == 0))
            .Select((role, index) => new EvidencePort($"port-{index + 1}", S(role))).ToList();
        for (var index = 0; index < cells - 1; index += 2)
            ports.Add(new($"pair-{index / 2 + 1}", S(roles[index * 2], roles[(index + 1) * 2])));
        var receiver = new Receiver(new(domain, S(reach)), forms, context, ports);
        IElement source = new Placeholder(), revision = new Placeholder();
        var concealed = new List<ProvisionalEquivalence.Publication> { new(source, revision, allValues[truth]) };
        if (control == "conflict") concealed.Add(new(new Placeholder(), revision, allValues[(truth + 1) % 4]));
        return new(family, receiver, frame, reach, roles, concealed.ToArray());
    }

    private static FeatureRead ReadFeatures(Receiver receiver, EvidencePort port, Relation basis)
    {
        var candidates = receiver.Selection.Eligible; var splitPairs = new List<IElement>();
        for (var i = 0; i < candidates.Count; i++) for (var j = i + 1; j < candidates.Count; j++)
            if (E(port.Roles).Any(role =>
            {
                var a = Sample(candidates[i].Hypothesis, role); var b = Sample(candidates[j].Hypothesis, role);
                return a is not null && b is not null && receiver.Engine.Compare(a, b).IsUnequal;
            })) splitPairs.Add(new Relation(candidates[i].Hypothesis.Surface, candidates[j].Hypothesis.Surface));
        var queryRoles = E(receiver.Context.Query).OfType<Relation>().SelectMany(pair => new[] { pair.Left, pair.Right })
            .Distinct(ReferenceEqualityComparer.Instance).ToArray();
        var touched = E(port.Roles).Where(role => queryRoles.Any(other => ReferenceEquals(role, other))).ToArray();
        var known = E(port.Roles).Where(role => receiver.Publications.Any(pub => E(pub.Values).OfType<Relation>()
            .Any(row => ReferenceEquals(row.Left, role) && Closed((Relation)row.Right)))).ToArray();
        var scopes = new[] { S(port.Surface), S(splitPairs.ToArray()), S(touched), port.Roles, S(known) };
        var featureRows = E(basis).Select((key, index) => (IElement)new Relation(key, scopes[index])).ToArray();
        var surface = new Relation(port.Surface, new Relation(receiver.Context.Query, S(featureRows)));
        return new(port, surface, ReadFeatureValues(surface, candidates.Count));
    }
    private static double[] ReadFeatureValues(Relation surface, int candidateCount)
    {
        var body = (Relation)surface.Right; var query = (Relation)body.Left;
        var rows = E((Relation)body.Right).Cast<Relation>().ToArray();
        var queryRoles = E(query).Cast<Relation>().SelectMany(pair => new[] { pair.Left, pair.Right })
            .Distinct(ReferenceEqualityComparer.Instance).Count();
        var counts = rows.Select(row => E((Relation)row.Right).Length).ToArray();
        var pairCount = candidateCount * (candidateCount - 1) / 2;
        return [counts[0], (double)counts[1] / Math.Max(1, pairCount),
            (double)counts[2] / Math.Max(1, queryRoles), counts[3] / 2d,
            (double)counts[4] / Math.Max(1, counts[3])];
    }
    private static int EligibleCount(Relation selection)
    {
        var context = (Relation)selection.Right; var candidates = (Relation)context.Right;
        var eligible = (Relation)candidates.Right;
        return E((Relation)eligible.Left).Length;
    }
    private static Relation? Sample(ProvisionalEquivalence.Hypothesis hypothesis, IElement role)
    {
        var binding = E(hypothesis.Bindings).OfType<Relation>().SingleOrDefault(row => ReferenceEquals(row.Left, role));
        if (binding is null) return null;
        return E(hypothesis.ParameterValues).OfType<Relation>()
            .SingleOrDefault(row => ReferenceEquals(row.Left, binding.Right))?.Right as Relation;
    }
    private static EvidencePort[] Available(Receiver receiver) => receiver.Ports
        .Where(port => !receiver.Requested.Any(old => ReferenceEquals(old, port))).ToArray();
    private static Metric Measure(string split, string family, string policy, int ordinal, int requests,
        Receiver receiver, int budget, ProvisionalEquivalence.Selection evaluation)
    {
        var selection = receiver.Selection;
        var conflicts = selection.Candidates.SelectMany(item => E(item.InScopeConflicts)).Distinct(ReferenceEqualityComparer.Instance).Count();
        var blocked = selection.Candidates.Count(item => E(item.Blocked).Length != 0);
        return new(split, family, policy, ordinal, requests, selection.Eligible.Count, conflicts, blocked,
            selection.Eligible.Count == 1 && conflicts == 0 && evaluation.Eligible.Any(item =>
                ReferenceEquals(item.Hypothesis, selection.Eligible[0].Hypothesis)),
            requests >= budget && selection.Eligible.Count > 1);
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
