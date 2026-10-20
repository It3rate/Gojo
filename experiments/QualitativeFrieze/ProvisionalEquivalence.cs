using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A bounded observational experiment over declared repeated, positive,
/// unmeasured participation. Shared template parameters propose equivalence;
/// they neither identify the participants nor establish geometric congruence.
/// Publications, assumptions and generated parameter samples remain separate.</summary>
public sealed class ProvisionalEquivalence
{
    // engine = actual role domain -> declared positive participation basis
    // hypothesis = domain -> (program -> (role bindings ->
    //              (sample parameter values -> (equality edges -> provenance))))
    // publication = (source -> revision) -> exposed role values
    // comparison = (basis -> (left -> right)) ->
    //              (paired occurrences -> (left remainder -> (right remainder -> open)))
    // assessment = (hypothesis -> (publications -> context)) -> Closed(
    //              support, refuted, open, blocked, conflicts, in-scope support,
    //              in-scope refuted, in-scope open, retained assumptions,
    //              withdrawn assumptions, dependencies, proposed sample support,
    //              sample refuted, sample open, in-scope sample refuted,
    //              in-scope literal conflicts)
    public Relation Domain { get; }
    public Relation Basis { get; }
    public Relation Surface { get; }

    public ProvisionalEquivalence(Relation domain, Relation basis)
    {
        Domain = domain; Basis = basis; Surface = new(domain, basis);
        if (!Closed(domain) || !Closed(basis) || E(domain).Distinct(ReferenceEqualityComparer.Instance).Count() != E(domain).Length ||
            E(basis).Distinct(ReferenceEqualityComparer.Instance).Count() != E(basis).Length ||
            Participants(Surface).Any(element => element is not (Relation or Placeholder)))
            throw new ArgumentException("A native closed role domain and independent positive participation basis are required.");
    }

    public sealed class Hypothesis
    {
        public Relation Domain { get; }
        public Relation Program { get; }
        public Relation Bindings { get; }
        public Relation ParameterValues { get; }
        public Relation Equalities { get; }
        public IElement Provenance { get; }
        public Relation Surface { get; }
        public Hypothesis(Relation domain, Relation program, Relation bindings, Relation parameterValues,
            Relation equalities, IElement? provenance = null)
        {
            Domain = domain; Program = program; Bindings = bindings; ParameterValues = parameterValues;
            Equalities = equalities; Provenance = provenance ?? S();
            Surface = new(domain, new Relation(program, new Relation(bindings,
                new Relation(parameterValues, new Relation(equalities, Provenance)))));
        }
    }

    public sealed class Publication
    {
        public IElement Source { get; }
        public IElement Revision { get; }
        public Relation Values { get; }
        public Relation Surface { get; }
        public Publication(IElement source, IElement revision, Relation values)
        { Source = source; Revision = revision; Values = values; Surface = new(new Relation(source, revision), values); }
    }

    public sealed class Context
    {
        public Relation Capabilities { get; }
        public Relation Query { get; }
        public Relation Preference { get; }
        public Relation? WorkingSources { get; }
        public Relation Surface { get; }
        public Context(Relation capabilities, Relation query, Relation preference, Relation? workingSources = null)
        {
            Capabilities = capabilities; Query = query; Preference = preference; WorkingSources = workingSources;
            Surface = new(capabilities, new Relation(query, new Relation(preference,
                workingSources ?? StructuralConstraint.Open())));
        }
    }

    public sealed class Comparison
    {
        public Relation Left { get; }
        public Relation Right { get; }
        public Relation Pairs { get; }
        public Relation LeftRemainder { get; }
        public Relation RightRemainder { get; }
        public Relation Open { get; }
        public Relation Surface { get; }
        public bool IsEqual => Closed(Left) && Closed(Right) && E(Open).Length == 0 && E(LeftRemainder).Length == 0 && E(RightRemainder).Length == 0;
        public bool IsUnequal => E(Open).Length == 0 && !IsEqual;
        internal Comparison(Relation basis, Relation left, Relation right)
        {
            Left = left; Right = right;
            var a = Cells(left); var b = Cells(right); var pairs = new List<IElement>(); var open = new List<IElement>();
            var i = 0;
            while (i < a.Length && i < b.Length && ReferenceEquals(a[i].Left, b[i].Left))
            { pairs.Add(new Relation(a[i], b[i])); i++; }
            Pairs = S(pairs.ToArray()); LeftRemainder = S(a.Skip(i).Cast<IElement>().ToArray());
            RightRemainder = S(b.Skip(i).Cast<IElement>().ToArray());
            if (!Closed(left)) open.Add(left);
            if (!Closed(right)) open.Add(right);
            foreach (var cell in a.Concat(b)) if (!E(basis).Any(term => ReferenceEquals(term, cell.Left))) open.Add(cell);
            // Independent unmeasured reaches are not proved unequal. A strict
            // difference is certified only as additional participation of the
            // same declared positive atom; a mismatching atom remains open.
            var terms = a.Concat(b).Select(cell => cell.Left).Distinct(ReferenceEqualityComparer.Instance).ToArray();
            if ((i != a.Length || i != b.Length) && terms.Length != 1)
                open.Add(new Relation(LeftRemainder, RightRemainder));
            Open = S(open.ToArray());
            Surface = new(new Relation(basis, new Relation(left, right)), new Relation(Pairs,
                new Relation(LeftRemainder, new Relation(RightRemainder, Open))));
        }
    }

    public Comparison Compare(Relation left, Relation right) => new(Basis, left, right);

    public sealed class Assessment
    {
        public Hypothesis Hypothesis { get; }
        public Relation Support { get; internal set; } = S();
        public Relation Refuted { get; internal set; } = S();
        public Relation Open { get; internal set; } = S();
        public Relation Blocked { get; internal set; } = S();
        public Relation Conflicts { get; internal set; } = S();
        public Relation InScopeSupport { get; internal set; } = S();
        public Relation InScopeRefuted { get; internal set; } = S();
        public Relation InScopeOpen { get; internal set; } = S();
        public Relation SampleSupport { get; internal set; } = S();
        public Relation SampleRefuted { get; internal set; } = S();
        public Relation SampleOpen { get; internal set; } = S();
        public Relation InScopeSampleRefuted { get; internal set; } = S();
        public Relation InScopeConflicts { get; internal set; } = S();
        public Relation Retained { get; internal set; } = S();
        public Relation Withdrawn { get; internal set; } = S();
        public Relation Dependencies { get; internal set; } = S();
        public Relation Surface { get; internal set; } = S();
        public int ParameterCount { get; internal set; }
        public int NativeCost => Participants(Hypothesis.Program).Count();
        public int QueryOpenCount { get; internal set; }
        public bool HasConflict => E(Conflicts).Length != 0;
        public bool IsEligible => E(Blocked).Length == 0 && E(InScopeRefuted).Length == 0 &&
            E(InScopeSampleRefuted).Length == 0 && E(InScopeConflicts).Length == 0;
        internal Assessment(Hypothesis hypothesis) { Hypothesis = hypothesis; }
    }

    public sealed class Selection
    {
        public IReadOnlyList<Assessment> Candidates { get; }
        public IReadOnlyList<Assessment> Eligible { get; }
        public IReadOnlyList<Assessment> Pareto { get; }
        public Hypothesis? Chosen { get; }
        public Relation Surface { get; }
        internal Selection(ProvisionalEquivalence engine, IReadOnlyList<Assessment> candidates, Context context)
        {
            Candidates = candidates; Eligible = candidates.Where(item => item.IsEligible).ToArray();
            // Missing query promises, distinct parameters and native program
            // cost are separate axes; no opaque weighted score is introduced.
            Pareto = Eligible.Where(item => !Eligible.Any(other => !ReferenceEquals(item, other) &&
                other.QueryOpenCount <= item.QueryOpenCount && other.ParameterCount <= item.ParameterCount && other.NativeCost <= item.NativeCost &&
                (other.QueryOpenCount < item.QueryOpenCount || other.ParameterCount < item.ParameterCount || other.NativeCost < item.NativeCost))).ToArray();
            Chosen = E(context.Preference).Select(preferred => Pareto.FirstOrDefault(item => ReferenceEquals(item.Hypothesis.Surface, preferred)))
                .FirstOrDefault(item => item is not null)?.Hypothesis;
            Surface = new(engine.Surface, new Relation(context.Surface, new Relation(S(candidates.Select(item => (IElement)item.Surface).ToArray()),
                new Relation(S(Eligible.Select(item => (IElement)item.Hypothesis.Surface).ToArray()),
                    new Relation(S(Pareto.Select(item => (IElement)item.Hypothesis.Surface).ToArray()), Chosen?.Surface ?? StructuralConstraint.Open())))));
        }
    }

    public Selection Assess(IReadOnlyList<Hypothesis> hypotheses, IReadOnlyList<Publication> publications, Context context)
    {
        return new(this, hypotheses.Select(hypothesis => AssessOne(hypothesis, publications, context)).ToArray(), context);
    }

    private Assessment AssessOne(Hypothesis h, IReadOnlyList<Publication> publications, Context context)
    {
        var result = new Assessment(h); var support = new List<IElement>(); var refuted = new List<IElement>(); var open = new List<IElement>();
        var blocked = new List<IElement>(); var conflicts = new List<IElement>();
        var scopedSupport = new List<IElement>(); var scopedRefuted = new List<IElement>(); var scopedOpen = new List<IElement>();
        var retained = new List<IElement>(); var withdrawn = new List<IElement>(); var dependencies = new List<IElement>();
        var sampleSupport = new List<IElement>(); var sampleRefuted = new List<IElement>(); var sampleOpen = new List<IElement>();
        var scopeSampleRefuted = new List<IElement>(); var scopeConflicts = new List<IElement>();
        var roles = E(Domain); var bindings = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        if (!ReferenceEquals(h.Domain, Domain) || !Closed(h.Bindings) || !Closed(h.Equalities) || !Closed(h.ParameterValues)) blocked.Add(h.Surface);
        foreach (var entry in E(h.Bindings))
            if (entry is not Relation binding || !roles.Any(role => ReferenceEquals(role, binding.Left)) || !bindings.TryAdd(binding.Left, binding)) blocked.Add(entry);
        if (!Closed(context.Capabilities) || !E(context.Capabilities).Any(program => ReferenceEquals(program, h.Program))) blocked.Add(new Relation(h.Program, context.Capabilities));
        if (!Closed(context.Query) || !Closed(context.Preference) || (context.WorkingSources is not null && !Closed(context.WorkingSources))) blocked.Add(context.Surface);
        result.ParameterCount = bindings.Values.Select(binding => binding.Right).Distinct(ReferenceEqualityComparer.Instance).Count();
        var parameters = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        foreach (var entry in E(h.ParameterValues))
            if (entry is not Relation { Right: Relation word } row ||
                !bindings.Values.Any(binding => ReferenceEquals(binding.Right, row.Left)) ||
                !StructuralConstraint.TryReadScope(word, out _, out _, out _) || !parameters.TryAdd(row.Left, row)) blocked.Add(entry);
        if (Participants(new Relation(h.Surface, context.Surface)).Any(element => element is not (Relation or Placeholder))) blocked.Add(h.Surface);
        var claims = new List<(Relation Edge, Relation Dependency)>();
        foreach (var entry in E(h.Equalities))
            if (entry is not Relation edge || !bindings.TryGetValue(edge.Left, out var a) || !bindings.TryGetValue(edge.Right, out var b) || !ReferenceEquals(a.Right, b.Right)) blocked.Add(entry);
            else { var dependency = new Relation(new Relation(a, b), edge); claims.Add((edge, dependency)); dependencies.Add(dependency); }
        foreach (var query in E(context.Query))
            if (query is not Relation pair || !roles.Any(role => ReferenceEquals(role, pair.Left)) || !roles.Any(role => ReferenceEquals(role, pair.Right))) blocked.Add(query);
            else if (!bindings.TryGetValue(pair.Left, out var a) || !bindings.TryGetValue(pair.Right, out var b) || !ReferenceEquals(a.Right, b.Right)) result.QueryOpenCount++;
        var scoped = publications.Where(pub => context.WorkingSources is null || E(context.WorkingSources).Any(source => ReferenceEquals(source, pub.Source))).ToArray();
        foreach (var pub in publications)
        {
            var values = E(pub.Values); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            if (!StructuralConstraint.TryReadScope(pub.Values, out _, out _, out _) || values.Any(entry => entry is not Relation { Right: Relation word } row ||
                !roles.Any(role => ReferenceEquals(role, row.Left)) || !seen.Add(row.Left) || !StructuralConstraint.TryReadScope(word, out _, out _, out _)) ||
                Participants(pub.Surface).Any(element => element is not (Relation or Placeholder)))
                if (scoped.Any(actual => ReferenceEquals(actual, pub))) blocked.Add(pub.Surface);
            // These are proposed-sample conformance checks, never added to the
            // source equality support below. The independent output reader is
            // still responsible for checking that execution realizes the sample.
            foreach (var binding in bindings.Values)
            {
                var actual = Value(pub, binding.Left);
                if (actual is null) continue;
                var expected = parameters.TryGetValue(binding.Right, out var row) ? (Relation)row.Right : StructuralConstraint.Open(binding.Right);
                var comparison = Compare(expected, actual);
                var witness = new Relation(new Relation(pub.Surface, binding), comparison.Surface);
                if (comparison.IsEqual) sampleSupport.Add(witness);
                else if (comparison.IsUnequal)
                { sampleRefuted.Add(witness); if (scoped.Any(item => ReferenceEquals(item, pub))) scopeSampleRefuted.Add(witness); }
                else sampleOpen.Add(witness);
            }
        }
        foreach (var (edge, dependency) in claims)
        {
            var thisSupport = new List<(Publication Pub, IElement Witness)>(); var thisRefuted = new List<(Publication Pub, IElement Witness)>();
            var scopeRefuted = false; var scopeRead = false;
            foreach (var pub in publications)
            {
                var a = Value(pub, edge.Left); var b = Value(pub, edge.Right);
                var comparison = Compare(a ?? StructuralConstraint.Open(edge.Left), b ?? StructuralConstraint.Open(edge.Right));
                var witness = new Relation(new Relation(pub.Surface, dependency), comparison.Surface);
                var isScoped = scoped.Any(actual => ReferenceEquals(actual, pub));
                if (comparison.IsEqual) { support.Add(witness); thisSupport.Add((pub, witness)); if (isScoped) scopedSupport.Add(witness); }
                else if (comparison.IsUnequal) { refuted.Add(witness); thisRefuted.Add((pub, witness)); if (isScoped) { scopedRefuted.Add(witness); scopeRefuted = true; } }
                else { open.Add(witness); if (isScoped) scopedOpen.Add(witness); }
                scopeRead |= isScoped;
            }
            if (!scopeRead) { var missing = new Relation(dependency, StructuralConstraint.Open()); scopedOpen.Add(missing); open.Add(missing); }
            foreach (var a in thisSupport) foreach (var b in thisRefuted)
                if (ReferenceEquals(a.Pub.Revision, b.Pub.Revision)) conflicts.Add(new Relation(edge, new Relation(a.Witness, b.Witness)));
            if (scopeRefuted) withdrawn.Add(new Relation(edge, S(scopedRefuted.Where(witness => witness is Relation { Left: Relation source } && ReferenceEquals(source.Right, dependency)).ToArray())));
            else retained.Add(edge);
        }
        // Literal supplier conflicts are also visible where a more flexible
        // hypothesis has not proposed an equality between the affected roles.
        for (var i = 0; i < publications.Count; i++) for (var j = i + 1; j < publications.Count; j++) foreach (var role in roles)
        {
            if (!ReferenceEquals(publications[i].Revision, publications[j].Revision)) continue;
            var a = Value(publications[i], role); var b = Value(publications[j], role);
            if (a is null || b is null) continue;
            var comparison = Compare(a, b);
            if (comparison.IsUnequal)
            {
                var witness = new Relation(role, new Relation(new Relation(publications[i].Surface, publications[j].Surface), comparison.Surface)); conflicts.Add(witness);
                if (scoped.Any(pub => ReferenceEquals(pub, publications[i])) && scoped.Any(pub => ReferenceEquals(pub, publications[j]))) scopeConflicts.Add(witness);
            }
        }
        result.Support = S(support.ToArray()); result.Refuted = S(refuted.ToArray()); result.Open = S(open.ToArray()); result.Blocked = S(blocked.ToArray());
        result.Conflicts = S(conflicts.ToArray()); result.InScopeSupport = S(scopedSupport.ToArray()); result.InScopeRefuted = S(scopedRefuted.ToArray()); result.InScopeOpen = S(scopedOpen.ToArray());
        result.SampleSupport = S(sampleSupport.ToArray()); result.SampleRefuted = S(sampleRefuted.ToArray()); result.SampleOpen = S(sampleOpen.ToArray());
        result.InScopeSampleRefuted = S(scopeSampleRefuted.ToArray()); result.InScopeConflicts = S(scopeConflicts.ToArray());
        result.Retained = S(retained.ToArray()); result.Withdrawn = S(withdrawn.ToArray()); result.Dependencies = S(dependencies.ToArray());
        result.Surface = new(new Relation(h.Surface, new Relation(S(publications.Select(pub => (IElement)pub.Surface).ToArray()), context.Surface)),
            S(result.Support, result.Refuted, result.Open, result.Blocked, result.Conflicts,
                result.InScopeSupport, result.InScopeRefuted, result.InScopeOpen, result.Retained,
                result.Withdrawn, result.Dependencies, result.SampleSupport, result.SampleRefuted,
                result.SampleOpen, result.InScopeSampleRefuted, result.InScopeConflicts));
        return result;
    }

    private Relation? Value(Publication publication, IElement role)
    {
        if (!StructuralConstraint.TryReadScope(publication.Values, out _, out _, out _)) return null;
        var values = E(publication.Values).OfType<Relation>().Where(row => ReferenceEquals(row.Left, role)).ToArray();
        return values.Length == 1 && values[0].Right is Relation value ? value : null;
    }
    private static bool SamePair(Relation a, Relation b) => ReferenceEquals(a.Left, b.Left) && ReferenceEquals(a.Right, b.Right);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && ReferenceEquals(seam, tail);
    private static Relation[] Cells(Relation word)
    {
        if (!StructuralConstraint.TryReadScope(word, out _, out _, out _)) return [];
        var cells = new List<Relation>(); var cursor = word.Right;
        while (cursor is Relation cell) { cells.Add(cell); cursor = cell.Right; }
        return cells.ToArray();
    }
    private static IEnumerable<IElement> Participants(IElement source)
    {
        var pending = new Stack<IElement>(); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); pending.Push(source);
        while (pending.TryPop(out var item)) if (seen.Add(item)) { yield return item; if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); } }
    }
}
