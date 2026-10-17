using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>Finite actions on actual participants. A permutation is a closed
/// scope of participant -> participant rows. Its meaning does not depend on
/// names, angles, coordinate values, axis labels or operation codes. These
/// reads establish actions on this domain, not a metric or a faithful action
/// on some larger unexposed structure.</summary>
public sealed class QualitativeActionAlgebra
{
    // Surface = actual-domain -> actual-identity-map
    // word = Closed(actual-first-map, actual-next-map, ...)
    // read = ((Surface -> word) -> inverse-derivation-or-Closed()) ->
    //        (result-map -> (blocked -> (unresolved -> paths)))
    // path = actual-result-row -> Closed(consumed actual map rows in word order)
    // inverse-derivation = (actual-source-map -> inverse-map) ->
    //                      Closed(actual-inverse-row -> actual-source-row)
    // equality = (Surface -> (left-read -> right-read)) ->
    //            (matched -> (counterexamples -> unresolved))
    // comparison = actual-participant -> (actual-left-result-row -> actual-right-result-row)
    // order = (Surface -> (action-map -> repeated-action-word)) ->
    //         (Closed(all positive-prefix equality reads) -> unresolved)
    public Relation Domain { get; }
    public Relation Identity { get; }
    public Relation Surface { get; }

    public QualitativeActionAlgebra(Relation domain)
    {
        Domain = domain;
        var rows = E(domain).Select(item => (IElement)new Relation(item, item)).ToArray();
        Identity = FiniteLaw.DomainEntries(domain, out _) ? S(rows) : StructuralConstraint.Open(rows);
        Surface = new Relation(Domain, Identity);
    }

    public Relation Evaluate(Relation word) => Evaluate(word, S());

    /// <summary>First acts first, second acts second: the resulting action is
    /// second ∘ first. The native word retains that explicit execution order.</summary>
    public Relation Compose(Relation first, Relation second) => Evaluate(S(first, second));

    public Relation Inverse(Relation map)
    {
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        var complete = Validate(map, blocked, pending, out var rows);
        var inverseRows = rows.Select(row => new Relation(row.Right, row.Left)).ToArray();
        var inverse = complete ? S(inverseRows) : StructuralConstraint.Open(inverseRows);
        var correspondences = S(inverseRows.Zip(rows).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        return Evaluate(S(inverse), new Relation(new Relation(map, inverse), correspondences));
    }

    public bool TryApply(Relation map, IElement participant, out IElement output, out Relation witness)
    {
        output = null!; var evaluation = Evaluate(S(map)); var selected = StructuralConstraint.Open();
        var valid = TryReadResult(evaluation, out var result, out var blocked, out var pending, out _) &&
            Closed(result) && E(blocked).Length == 0 && E(pending).Length == 0;
        var rows = valid ? E(result).OfType<Relation>().Where(row => Same(row.Left, participant)).ToArray() : [];
        if (rows.Length == 1) { output = rows[0].Right; selected = S(output); }
        witness = new Relation(new Relation(evaluation, participant),
            new Relation(selected, rows.Length == 1 ? S(rows[0]) : S(new Relation(participant, Domain))));
        return rows.Length == 1;
    }

    public Relation Compare(Relation leftWord, Relation rightWord)
    {
        var left = Evaluate(leftWord); var right = Evaluate(rightWord);
        var matched = new List<IElement>(); var different = new List<IElement>(); var pending = new List<IElement>();
        if (TryReadResult(left, out var a, out _, out _, out _) && Closed(a) &&
            TryReadResult(right, out var b, out _, out _, out _) && Closed(b))
            foreach (var participant in E(Domain))
            {
                var x = E(a).Cast<Relation>().Single(row => Same(row.Left, participant));
                var y = E(b).Cast<Relation>().Single(row => Same(row.Left, participant));
                (Same(x.Right, y.Right) ? matched : different).Add(new Relation(participant, new Relation(x, y)));
            }
        else pending.Add(new Relation(left, right));
        return new Relation(new Relation(Surface, new Relation(left, right)),
            new Relation(S(matched.ToArray()), new Relation(S(different.ToArray()), S(pending.ToArray()))));
    }

    public Relation Commutes(Relation first, Relation second) => Compare(S(first, second), S(second, first));

    /// <summary>Checks exact order using a nonempty word repeating the actual
    /// action. Every proper positive prefix must fail to be identity; full-word
    /// identity alone would also admit an identity action as order four.</summary>
    public Relation CheckOrder(Relation action, Relation repeatedActionWord)
    {
        var reads = new List<IElement>(); var pending = new List<IElement>();
        if (!Closed(repeatedActionWord) || E(repeatedActionWord).Length == 0 || E(repeatedActionWord).Any(item => !ReferenceEquals(item, action)))
            pending.Add(repeatedActionWord);
        else for (var i = 1; i <= E(repeatedActionWord).Length; i++) reads.Add(Compare(S(E(repeatedActionWord).Take(i).ToArray()), S()));
        return new Relation(new Relation(Surface, new Relation(action, repeatedActionWord)), new Relation(S(reads.ToArray()), S(pending.ToArray())));
    }

    /// <summary>Revalidates native inputs and each consumed path. Open, foreign
    /// and nonbijective inputs cannot acquire a closed result by copied fields.</summary>
    public bool TryReadResult(Relation read, out Relation map, out Relation blocked, out Relation unresolved, out Relation paths)
    {
        map = blocked = unresolved = paths = null!;
        if (read.Left is not Relation { Left: Relation context, Right: Relation derivation } || !ReferenceEquals(context.Left, Surface) ||
            context.Right is not Relation word || read.Right is not Relation { Left: Relation result, Right: Relation rest } ||
            rest.Left is not Relation refused || rest.Right is not Relation { Left: Relation pending, Right: Relation traces } ||
            !StructuralConstraint.TryReadScope(result, out _, out _, out _) || !Closed(refused) || !Closed(pending) || !Closed(traces)) return false;
        var expectedBlocked = new List<IElement>(); var expectedPending = new List<IElement>();
        var complete = Inputs(word, derivation, expectedBlocked, expectedPending, out var maps);
        if (!EvidenceScope(refused, expectedBlocked, Surface, word, derivation) || !EvidenceScope(pending, expectedPending, Surface, word, derivation) || Closed(result) != complete) return false;
        if (!complete)
        { if (E(result).Length != 0 || E(traces).Length != 0) return false; }
        else
        {
            if (!FiniteLaw.DomainEntries(Domain, out var domain) || E(result).Length != domain.Length || E(traces).Length != domain.Length) return false;
            for (var i = 0; i < domain.Length; i++)
            {
                if (E(result)[i] is not Relation row || !Same(row.Left, domain[i]) || E(traces)[i] is not Relation path ||
                    !ReferenceEquals(path.Left, row) || path.Right is not Relation consumed || !Closed(consumed) || E(consumed).Length != maps.Length) return false;
                var current = domain[i]; var used = E(consumed);
                for (var j = 0; j < maps.Length; j++)
                {
                    var actual = maps[j].Single(candidate => Same(candidate.Left, current));
                    if (!ReferenceEquals(actual, used[j])) return false;
                    current = actual.Right;
                }
                if (!Same(current, row.Right)) return false;
            }
        }
        map = result; blocked = refused; unresolved = pending; paths = traces; return true;
    }

    public bool TryReadEquality(Relation read, out bool equal)
    {
        equal = false;
        if (read.Left is not Relation context || !ReferenceEquals(context.Left, Surface) ||
            context.Right is not Relation { Left: Relation left, Right: Relation right } ||
            !TryReadResult(left, out var a, out _, out _, out _) || !TryReadResult(right, out var b, out _, out _, out _) ||
            !Closed(a) || !Closed(b) || read.Right is not Relation { Left: Relation matched, Right: Relation rest } ||
            rest.Left is not Relation different || rest.Right is not Relation pending || !Closed(matched) || !Closed(different) ||
            !Closed(pending) || E(pending).Length != 0) return false;
        var yes = new List<IElement>(); var no = new List<IElement>();
        foreach (var participant in E(Domain))
        {
            var x = E(a).Cast<Relation>().Single(row => Same(row.Left, participant));
            var y = E(b).Cast<Relation>().Single(row => Same(row.Left, participant));
            (Same(x.Right, y.Right) ? yes : no).Add(new Relation(participant, new Relation(x, y)));
        }
        if (!EvidenceScope(matched, yes, Surface, left, right) || !EvidenceScope(different, no, Surface, left, right)) return false;
        equal = no.Count == 0; return true;
    }

    public bool TryReadOrder(Relation read, out bool exactOrder)
    {
        exactOrder = false;
        if (read.Left is not Relation context || !ReferenceEquals(context.Left, Surface) ||
            context.Right is not Relation { Left: Relation action, Right: Relation word } || !Closed(word) ||
            E(word).Length == 0 || E(word).Any(item => !ReferenceEquals(item, action)) ||
            read.Right is not Relation { Left: Relation prefixes, Right: Relation pending } || !Closed(prefixes) ||
            !Closed(pending) || E(pending).Length != 0 || E(prefixes).Length != E(word).Length) return false;
        var answers = new List<bool>();
        for (var i = 0; i < E(prefixes).Length; i++)
        {
            if (E(prefixes)[i] is not Relation comparison || !TryReadEquality(comparison, out var same) ||
                comparison.Left is not Relation { Right: Relation { Left: Relation left, Right: Relation right } } ||
                !WordOf(left, out var first) || !WordOf(right, out var second) || E(second).Length != 0 ||
                E(first).Length != i + 1 || E(first).Any(item => !ReferenceEquals(item, action))) return false;
            answers.Add(same);
        }
        exactOrder = answers[^1] && answers.Take(answers.Count - 1).All(answer => !answer); return true;
    }

    private Relation Evaluate(Relation word, Relation derivation)
    {
        var blocked = new List<IElement>(); var pending = new List<IElement>(); var paths = new List<IElement>();
        var complete = Inputs(word, derivation, blocked, pending, out var maps); var rows = new List<IElement>();
        if (complete) foreach (var participant in E(Domain))
        {
            var current = participant; var used = new List<IElement>();
            foreach (var map in maps) { var row = map.Single(row => Same(row.Left, current)); current = row.Right; used.Add(row); }
            var result = new Relation(participant, current); rows.Add(result); paths.Add(new Relation(result, S(used.ToArray())));
        }
        return new Relation(new Relation(new Relation(Surface, word), derivation),
            new Relation(complete ? S(rows.ToArray()) : StructuralConstraint.Open(), new Relation(S(blocked.ToArray()),
                new Relation(S(pending.ToArray()), S(paths.ToArray())))));
    }

    private bool Inputs(Relation word, Relation derivation, List<IElement> blocked, List<IElement> pending, out Relation[][] maps)
    {
        var complete = FiniteLaw.DomainEntries(Domain, out _); if (!complete) pending.Add(Domain);
        if (!Closed(word)) { pending.Add(word); complete = false; }
        var rows = new List<Relation[]>();
        foreach (var item in E(word))
            if (item is Relation map) { complete &= Validate(map, blocked, pending, out var offered); rows.Add(offered); }
            else { blocked.Add(item); complete = false; }
        if (!(Closed(derivation) && E(derivation).Length == 0))
        {
            if (derivation.Left is not Relation { Left: Relation source, Right: Relation inverse } || derivation.Right is not Relation links ||
                E(word) is not [var selected] || !ReferenceEquals(selected, inverse) || !Closed(links) ||
                !Validate(source, blocked, pending, out var originals) || E(links).Length != originals.Length || E(inverse).Length != originals.Length ||
                E(links).Where((item, i) => item is not Relation link || !ReferenceEquals(link.Left, E(inverse)[i]) ||
                    !ReferenceEquals(link.Right, originals[i]) || link.Left is not Relation reversed ||
                    !Same(reversed.Left, originals[i].Right) || !Same(reversed.Right, originals[i].Left)).Any())
            { pending.Add(derivation); complete = false; }
        }
        maps = rows.ToArray(); return complete;
    }

    private bool Validate(Relation map, List<IElement> blocked, List<IElement> pending, out Relation[] rows)
    {
        var valid = FiniteLaw.DomainEntries(Domain, out var domain); if (!valid) pending.Add(Domain);
        if (!Closed(map)) { pending.Add(map); valid = false; }
        rows = E(map).OfType<Relation>().ToArray();
        foreach (var item in E(map))
            if (item is not Relation row || !domain.Any(p => Same(p, row.Left)) || !domain.Any(p => Same(p, row.Right)))
            { blocked.Add(item); valid = false; }
        foreach (var participant in domain)
        {
            if (rows.Count(row => Same(row.Left, participant)) != 1) { pending.Add(new Relation(participant, map)); valid = false; }
            if (rows.Count(row => Same(row.Right, participant)) != 1) { pending.Add(new Relation(map, participant)); valid = false; }
        }
        return valid;
    }
    private bool WordOf(Relation read, out Relation word)
    {
        word = null!;
        if (read.Left is not Relation { Left: Relation context } || !ReferenceEquals(context.Left, Surface) || context.Right is not Relation source || !Closed(source)) return false;
        word = source; return true;
    }
    private static bool EvidenceScope(Relation actual, List<IElement> expected, params IElement[] sources)
    {
        var atoms = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var source in sources) Include(source);
        return Closed(actual) && E(actual).Length == expected.Count && E(actual).Zip(expected).All(pair => Equal(pair.First, pair.Second));
        void Include(IElement element)
        { if (atoms.Add(element) && element is Relation relation) { Include(relation.Left); Include(relation.Right); } }
        bool Equal(IElement a, IElement b) => Same(a, b) || !atoms.Contains(b) && a is Relation x && b is Relation y && Equal(x.Left, y.Left) && Equal(x.Right, y.Right);
    }
    private static bool Same(IElement a, IElement b) => FiniteLaw.SameParticipant(a, b);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static bool Closed(Relation scope) => FiniteLaw.ClosedEntries(scope, out _);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
