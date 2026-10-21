using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A defeasible local read over exposed predicate evidence. Predicate
/// identities, expected polarity, selected authority, exceptions and witnesses
/// remain ordinary connected structure. This reader neither discovers geometry
/// nor assigns probabilities to defaults.</summary>
public static class OrnamentDefaultsPolicy
{
    // rule = actual predicate -> (Closed(positive predicate) -> Closed(negative predicate))
    // policy = Closed(required rules) -> (Closed(strong defaults) ->
    //          (Closed(preferences) -> Closed(unused rules)))
    // evidence = predicate -> (Supported -> (Refuted -> (Open -> Blocked)))
    // observation = exposed source port -> Closed(evidence)
    // directive = actual policy -> Closed(actual waived default/preference rules)
    // The ordered policy positions carry authority. Labels are display only.

    public sealed class Rule
    {
        public string Name { get; }
        public Relation Surface { get; }
        public Relation Predicate { get; } = null!;
        public bool Expected { get; }
        public Relation Blocked { get; }
        public Relation Open { get; }
        public bool IsValid => Empty(Blocked) && Empty(Open);

        public Rule(string name, Relation predicate, bool expected = true) : this(
            new(predicate, new Relation(expected ? S(predicate) : S(), expected ? S() : S(predicate))), name) { }

        public Rule(Relation surface, string name = "published rule")
        {
            Name = name; Surface = surface;
            var blocked = new List<IElement>(); var open = new List<IElement>();
            if (surface is not { Left: Relation predicate, Right: Relation { Left: Relation positive, Right: Relation negative } })
                blocked.Add(surface);
            else
            {
                Predicate = predicate;
                Scope(positive, open); Scope(negative, open);
                var yes = E(positive); var no = E(negative);
                if (yes.Length == 1 && no.Length == 0 && ReferenceEquals(yes[0], predicate)) Expected = true;
                else if (!(no.Length == 1 && yes.Length == 0 && ReferenceEquals(no[0], predicate)) && open.Count == 0) blocked.Add(surface);
            }
            Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
        }
    }

    public sealed class Policy
    {
        public Relation Surface { get; }
        public IReadOnlyList<Rule> Required { get; } = [];
        public IReadOnlyList<Rule> StrongDefaults { get; } = [];
        public IReadOnlyList<Rule> Preferences { get; } = [];
        public IReadOnlyList<Rule> Unused { get; } = [];
        public IReadOnlyList<Rule> Rules => Required.Concat(StrongDefaults).Concat(Preferences).Concat(Unused).ToArray();
        public Relation Blocked { get; }
        public Relation Open { get; }
        public bool IsValid => Empty(Blocked) && Empty(Open);

        public Policy(IReadOnlyList<Rule>? required = null, IReadOnlyList<Rule>? strongDefaults = null,
            IReadOnlyList<Rule>? preferences = null, IReadOnlyList<Rule>? unused = null) : this(new(
                S((required ?? []).Select(rule => (IElement)rule.Surface).ToArray()), new Relation(
                S((strongDefaults ?? []).Select(rule => (IElement)rule.Surface).ToArray()), new Relation(
                S((preferences ?? []).Select(rule => (IElement)rule.Surface).ToArray()),
                S((unused ?? []).Select(rule => (IElement)rule.Surface).ToArray()))))) { }

        public Policy(Relation surface)
        {
            Surface = surface; var blocked = new List<IElement>(); var open = new List<IElement>();
            if (surface is not { Left: Relation required, Right: Relation { Left: Relation defaults,
                Right: Relation { Left: Relation preferences, Right: Relation unused } } }) blocked.Add(surface);
            else
            {
                Required = Read(required); StrongDefaults = Read(defaults); Preferences = Read(preferences); Unused = Read(unused);
                var rules = Rules;
                if (rules.Select(rule => rule.Surface).Distinct(ReferenceEqualityComparer.Instance).Count() != rules.Count ||
                    rules.Where(rule => rule.Predicate is not null).Select(rule => rule.Predicate)
                        .Distinct(ReferenceEqualityComparer.Instance).Count() != rules.Count) blocked.Add(surface);
            }
            Blocked = S(blocked.ToArray()); Open = S(open.ToArray());

            Rule[] Read(Relation scope)
            {
                Scope(scope, open); var rules = new List<Rule>();
                foreach (var item in E(scope))
                    if (item is Relation row)
                    {
                        var rule = new Rule(row); rules.Add(rule);
                        blocked.AddRange(E(rule.Blocked)); open.AddRange(E(rule.Open));
                    }
                    else blocked.Add(item);
                return rules.ToArray();
            }
        }

        public int Tier(Rule rule) => Required.Any(actual => ReferenceEquals(actual.Surface, rule.Surface)) ? 0 :
            StrongDefaults.Any(actual => ReferenceEquals(actual.Surface, rule.Surface)) ? 1 :
            Preferences.Any(actual => ReferenceEquals(actual.Surface, rule.Surface)) ? 2 :
            Unused.Any(actual => ReferenceEquals(actual.Surface, rule.Surface)) ? 3 : -1;
    }

    public sealed class Evidence
    {
        public Relation Surface { get; }
        public Relation Predicate { get; } = null!;
        public Relation Supported { get; } = S();
        public Relation Refuted { get; } = S();
        public Relation Open { get; } = S();
        public Relation Blocked { get; } = S();
        public bool Conflicted => !Empty(Supported) && !Empty(Refuted);
        public string Answer => !Empty(Blocked) ? "Blocked" : Conflicted || !Empty(Open) ? "Open" :
            !Empty(Supported) ? "Yes" : !Empty(Refuted) ? "No" : "Open";

        public Evidence(Relation predicate, Relation supported, Relation refuted, Relation open, Relation blocked)
            : this(new(predicate, new Relation(supported, new Relation(refuted, new Relation(open, blocked))))) { }

        public static Evidence FromAnswer(Relation predicate, string answer, Relation concreteWitness) => answer switch
        {
            "Yes" => new(predicate, S(concreteWitness), S(), S(), S()),
            "No" => new(predicate, S(), S(concreteWitness), S(), S()),
            "Open" => new(predicate, S(), S(), S(concreteWitness), S()),
            "Blocked" => new(predicate, S(), S(), S(), S(concreteWitness)),
            _ => throw new ArgumentException("An exposed predicate read is Yes, No, Open or Blocked.", nameof(answer))
        };

        public Evidence(Relation surface)
        {
            Surface = surface;
            if (surface is not { Left: Relation predicate, Right: Relation { Left: Relation supported,
                Right: Relation { Left: Relation refuted, Right: Relation { Left: Relation open, Right: Relation blocked } } } })
            { Blocked = S(surface); return; }
            Predicate = predicate; Supported = supported; Refuted = refuted; Open = open; Blocked = blocked;
            var unclosed = new List<IElement>();
            foreach (var scope in new[] { supported, refuted, open, blocked }) Scope(scope, unclosed);
            if (unclosed.Count != 0) Open = S(E(open).Concat(unclosed).ToArray());
            var known = (!Empty(supported) ? 1 : 0) + (!Empty(refuted) ? 1 : 0);
            // Two actual assessments may disagree without either publication
            // being malformed. Keep both and leave the selected claim open.
            if (known > 1) Open = S(E(Open).Append(surface).ToArray());
            if (known == 0 && Empty(Open) && Empty(Blocked)) Open = S(surface);
        }
    }

    public sealed class Observation
    {
        public Relation Surface { get; }
        public Relation SourcePort { get; } = null!;
        public IReadOnlyList<Evidence> Facts { get; } = [];
        public Relation Blocked { get; }
        public Relation Open { get; }
        public bool IsValid => Empty(Blocked) && Empty(Open);
        public Observation(Relation sourcePort, IReadOnlyList<Evidence> facts) : this(new(sourcePort,
            S(facts.Select(fact => (IElement)fact.Surface).ToArray()))) { }
        public Observation(Relation surface)
        {
            Surface = surface; var blocked = new List<IElement>(); var open = new List<IElement>(); var facts = new List<Evidence>();
            if (surface is not { Left: Relation port, Right: Relation rows }) blocked.Add(surface);
            else
            {
                SourcePort = port; Scope(rows, open);
                foreach (var item in E(rows))
                    if (item is Relation row) facts.Add(new(row)); else blocked.Add(item);
                if (facts.Any(fact => fact.Predicate is null)) blocked.Add(rows);
            }
            Facts = facts; Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
        }
    }

    public sealed class Directive
    {
        public Policy Policy { get; }
        public Relation Surface { get; }
        public IReadOnlyList<Relation> Waived { get; } = [];
        public Relation Blocked { get; }
        public Relation Open { get; }
        public bool IsValid => Empty(Blocked) && Empty(Open);
        public Directive(Policy policy, IReadOnlyList<Rule>? waived = null) : this(new(policy.Surface,
            S((waived ?? []).Select(rule => (IElement)rule.Surface).ToArray()))) { }
        public Directive(Relation surface)
        {
            Surface = surface; var blocked = new List<IElement>(); var open = new List<IElement>();
            if (surface is not { Left: Relation policy, Right: Relation waived })
            { Policy = new Policy(S()); blocked.Add(surface); }
            else
            {
                Policy = new(policy); blocked.AddRange(E(Policy.Blocked)); open.AddRange(E(Policy.Open)); Scope(waived, open);
                var rules = Policy.StrongDefaults.Concat(Policy.Preferences).ToArray(); var actual = new List<Relation>();
                foreach (var item in E(waived))
                    if (item is not Relation row || !rules.Any(rule => ReferenceEquals(rule.Surface, row))) blocked.Add(item);
                    else actual.Add(row);
                if (actual.Distinct(ReferenceEqualityComparer.Instance).Count() != actual.Count) blocked.Add(waived);
                Waived = actual;
            }
            Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
        }
        public bool Waives(Rule rule) => Waived.Any(actual => ReferenceEquals(actual, rule.Surface));
    }

    public sealed class RuleRead
    {
        public Rule Rule { get; }
        public int Tier { get; }
        public Evidence? Fact { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Waived { get; }
        public Relation Carried { get; }
        public Relation Surface { get; }
        public string Answer => !Empty(Carried) ? "Unused" : !Empty(Waived) ? "Waived" : !Empty(Blocked) ? "Blocked" :
            !Empty(Supported) ? "Yes" : !Empty(Refuted) ? "No" : "Open";
        internal RuleRead(Rule rule, int tier, Evidence? fact, Observation source, bool waive)
        {
            Rule = rule; Tier = tier; Fact = fact;
            var basis = new Relation(rule.Surface, fact?.Surface ?? source.Surface);
            Supported = Refuted = Open = Blocked = Waived = Carried = S();
            if (tier == 3) Carried = S(basis);
            else if (waive) Waived = S(basis);
            else if (!Empty(rule.Blocked)) Blocked = S(basis);
            else if (!Empty(rule.Open)) Open = S(basis);
            else if (fact is null || fact.Answer == "Open") Open = S(basis);
            else if (fact.Answer == "Blocked") Blocked = S(basis);
            else if ((fact.Answer == "Yes") == rule.Expected) Supported = S(basis);
            else Refuted = S(basis);
            Surface = new(basis, new Relation(Supported, new Relation(Refuted, new Relation(Open,
                new Relation(Blocked, new Relation(Waived, Carried))))));
        }
    }

    public sealed class Receipt
    {
        public Policy Policy { get; }
        public Observation Observation { get; }
        public Directive Directive { get; }
        public IReadOnlyList<RuleRead> Reads { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Ranking { get; }
        public Relation Surface { get; }
        public string Answer => !Empty(Blocked) ? "Blocked" : !Empty(Refuted) ? "No" : !Empty(Open) ? "Open" : "Yes";
        public bool Eligible => Answer is "Yes" or "Open";
        public int StrongViolations => Reads.Count(read => read.Tier == 1 && read.Answer == "No");
        public int StrongUnresolved => Reads.Count(read => read.Tier == 1 && read.Answer is "Open" or "Blocked");
        public int PreferenceViolations => Reads.Count(read => read.Tier == 2 && read.Answer == "No");
        public int PreferenceUnresolved => Reads.Count(read => read.Tier == 2 && read.Answer is "Open" or "Blocked");
        public (int Lower, int Upper) StrongRange => (StrongViolations, StrongViolations + StrongUnresolved);
        public (int Lower, int Upper) PreferenceRange => (PreferenceViolations, PreferenceViolations + PreferenceUnresolved);

        public Receipt(Policy policy, Observation observation, Directive? directive = null)
        {
            Policy = policy; Observation = observation; Directive = directive ?? new(policy);
            var blocked = E(policy.Blocked).Concat(E(observation.Blocked)).Concat(E(Directive.Blocked)).ToList();
            var open = E(policy.Open).Concat(E(observation.Open)).Concat(E(Directive.Open)).ToList();
            if (!ReferenceEquals(Directive.Policy.Surface, policy.Surface)) blocked.Add(Directive.Surface);
            var reads = new List<RuleRead>();
            foreach (var rule in policy.Rules)
                reads.Add(new(rule, policy.Tier(rule), Combine(rule.Predicate, observation.Facts),
                    observation, Directive.Waives(rule)));
            Reads = reads;
            foreach (var read in reads.Where(read => read.Tier == 0))
            { blocked.AddRange(E(read.Blocked)); open.AddRange(E(read.Open)); }
            Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
            Refuted = S(reads.Where(read => read.Tier == 0 && read.Answer == "No").Select(read => (IElement)read.Surface).ToArray());
            Supported = S(reads.Where(read => read.Tier == 0 && read.Answer == "Yes").Select(read => (IElement)read.Surface).ToArray());
            Ranking = new(Bucket(1, "No"), new Relation(Bucket(1, "Open", "Blocked"),
                new Relation(Bucket(2, "No"), Bucket(2, "Open", "Blocked"))));
            Surface = new(new Relation(policy.Surface, new Relation(observation.Surface, Directive.Surface)),
                new Relation(S(reads.Select(read => (IElement)read.Surface).ToArray()), new Relation(Supported,
                    new Relation(Refuted, new Relation(Open, new Relation(Blocked, Ranking))))));

            Relation Bucket(int tier, params string[] answers) => S(reads.Where(read => read.Tier == tier && answers.Contains(read.Answer))
                .Select(read => (IElement)read.Surface).ToArray());
        }

        /// <summary>Partial ordinal preference over candidates under one
        /// policy. Only separated uncertainty ranges establish precedence.
        /// Overlapping ranges remain incomparable; no probability is invented.</summary>
        public static int? ComparePreference(Receipt first, Receipt second)
        {
            if (!ReferenceEquals(first.Policy.Surface, second.Policy.Surface))
                throw new ArgumentException("Preference comparison requires one shared native policy.");
            if (!first.Eligible || !second.Eligible) return null;
            foreach (var (a, b) in new[] { (first.StrongRange, second.StrongRange), (first.PreferenceRange, second.PreferenceRange) })
            {
                if (a.Upper < b.Lower) return -1;
                if (b.Upper < a.Lower) return 1;
                if (a.Lower != a.Upper || b.Lower != b.Upper) return null;
            }
            return 0;
        }

        private static Evidence? Combine(Relation predicate, IReadOnlyList<Evidence> facts)
        {
            var matches = facts.Where(fact => ReferenceEquals(fact.Predicate, predicate)).ToArray();
            if (matches.Length == 0) return null;
            if (matches.Length == 1) return matches[0];
            return new(predicate, S(matches.SelectMany(fact => E(fact.Supported)).ToArray()),
                S(matches.SelectMany(fact => E(fact.Refuted)).ToArray()),
                S(matches.SelectMany(fact => E(fact.Open)).ToArray()),
                S(matches.SelectMany(fact => E(fact.Blocked)).ToArray()));
        }
    }

    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Empty(Relation scope) => E(scope).Length == 0;
    private static void Scope(Relation scope, List<IElement> open)
    {
        if (!StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) || !FieldNative.Same(seam, tail)) open.Add(scope);
    }
}
