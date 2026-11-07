using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A reusable local word map and ordered-frame correspondence, applied
/// to an explicitly selected scope of native curve sections. This is a copied
/// observation, not a material transfer, weld, or claim about drawing history.</summary>
public static class CentrelineAction
{
    // action = substitution surface -> (displacement word -> traversal)
    // traversal = (a -> b) -> (a -> b OR b -> a), with distinct native markers
    // application = (action -> actual source scope) ->
    //   Closed(output scope, Closed(section rows), Closed(site pairs), Closed(unresolved))
    // section row = (actual source section -> copied section) -> Closed(position rows)
    // position row = (actual source word -> copied word) -> (substitution receipt -> sum receipt)
    // unresolved section = source section -> Closed(attempts through first failure)
    // attempt = substitution receipt -> sum receipt (open if substitution failed)
    // match = (application -> actual target scope) ->
    //   Closed(Closed(section pairs), Closed(control-word pairs), Closed(site pairs),
    //          Closed(coordinate mismatches), Closed(alias mismatches), Closed(unresolved))
    // No success flag is stored. Readers check the native instruction, consumed
    // words, additive accounting, exact scope coverage, and site correspondence.
    // Traversal reverses the selected ordered scope AND each section's ordered
    // control frame. It does not change the spatial basis or any over/under law.

    public sealed record ApplicationRead(Relation Action, Relation Source, Relation Output,
        Relation SectionRows, Relation SitePairs, Relation Unresolved, bool Complete, Relation Surface);
    public sealed record MatchRead(Relation Application, Relation Target, Relation SectionPairs,
        Relation ControlPairs, Relation SitePairs, Relation CoordinateMismatches,
        Relation AliasMismatches, Relation Unresolved, bool GeometryExact,
        bool SitePatternExact, bool Complete, Relation Surface);

    public static Relation Create(Curve.Frame frame, QualitativePatternTransform map,
        Relation displacement, bool reverseTraversal = false)
    {
        var a = new Placeholder(); var b = new Placeholder();
        return new(map.Surface, new Relation(displacement,
            new Relation(new Relation(a, b), reverseTraversal ? new Relation(b, a) : new Relation(a, b))));
    }

    /// <summary>Four derived bits in this explicitly selected local grammar:
    /// bit 0 exchanges the images of H/V; bit 1 opposes the image of input H;
    /// bit 2 opposes the image of input V; bit 3 reverses traversal. Decoding
    /// creates actual substitution rows and ordered-marker relations. These
    /// are not universal ODV bits, and translation remains a native word.</summary>
    public static Relation FromBits(Curve.Frame frame, byte bits, Relation displacement)
    {
        if (bits > 15) throw new ArgumentOutOfRangeException(nameof(bits));
        var swap = (bits & 1) != 0; var h = (bits & 2) == 0 ? 1 : -1; var v = (bits & 4) == 0 ? 1 : -1;
        var map = OrnamentBandComposition.Map(frame,
            frame.Word(swap ? 0 : h, swap ? h : 0), frame.Word(swap ? 0 : -h, swap ? -h : 0),
            frame.Word(swap ? v : 0, swap ? 0 : v), frame.Word(swap ? -v : 0, swap ? 0 : -v));
        return Create(frame, map, displacement, (bits & 8) != 0);
    }

    /// <summary>Encodes only the exact four-row signed basis permutation
    /// grammar. Other executable substitutions have no code in this codec.</summary>
    public static bool TryBits(Curve.Frame frame, Relation action, out byte bits)
    {
        bits = 0;
        if (!Instruction(frame, action, out var map, out _, out var reverse) ||
            !Closed(map.Rows, out var rows) || rows.Length != 4) return false;
        var directions = new[] { frame.Motion.Right, frame.Motion.Left, frame.Motion.Up, frame.Motion.Down };
        var images = new Curve.Vector[4];
        for (var i = 0; i < directions.Length; i++)
        {
            var matching = rows.OfType<Relation>().Where(r => r.Left is Relation m &&
                Same(m.Left, directions[i]) && Same(m.Right, frame.Reach)).ToArray();
            if (matching.Length != 1 || matching[0].Right is not Relation word || !Closed(word, out var terms) ||
                terms.Length != 1 || !frame.Vector(word, out images[i])) return false;
        }
        if (images[1] != -images[0] || images[3] != -images[2] ||
            Curve.Vector.Dot(images[0], images[2]) != 0) return false;
        var swap = images[0].X.IsZero;
        if (swap ? !images[2].Y.IsZero : !images[2].X.IsZero) return false;
        bits = (byte)((swap ? 1 : 0) | ((swap ? images[0].Y : images[0].X).Sign < 0 ? 2 : 0) |
            ((swap ? images[2].X : images[2].Y).Sign < 0 ? 4 : 0) | (reverse ? 8 : 0));
        return true;
    }

    public static byte Encode(Curve.Frame frame, Relation action) => TryBits(frame, action, out var bits)
        ? bits : throw new ArgumentException("The native action is outside the four-bit local basis/traversal grammar.", nameof(action));

    public static Relation Apply(Curve.Frame frame, Relation action, Relation source)
    {
        var output = new List<IElement>(); var rows = new List<IElement>(); var pending = new List<IElement>();
        var sitePairs = new List<IElement>();
        var sites = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        if (!Instruction(frame, action, out var map, out var displacement, out var reverse))
        { pending.Add(action); return Finish(); }
        if (!Sections(frame, source, out var sections)) { pending.Add(source); return Finish(); }
        foreach (var section in reverse ? sections.Reverse() : sections)
        {
            var read = frame.Read(section);
            var controls = E(read.ControlWords).Cast<Relation>().ToArray();
            if (reverse) Array.Reverse(controls);
            var positions = new List<IElement>(); var attempts = new List<IElement>();
            var words = new List<Relation>(); var complete = true;
            foreach (var word in controls)
            {
                var receipt = Substitute(map, (Relation)action.Left, word);
                if (!SubstitutionComplete(map, (Relation)action.Left, receipt, out var mapped))
                { attempts.Add(new Relation(receipt, StructuralConstraint.Open())); complete = false; break; }
                var sum = frame.Motion.Reduce(mapped, displacement);
                attempts.Add(new Relation(receipt, sum));
                if (!frame.Motion.TryReadResult(sum, out var result, out _, out var unresolved) ||
                    E(unresolved).Length != 0 || !frame.Vector(result, out _)) { complete = false; break; }
                positions.Add(new Relation(new Relation(word, result), new Relation(receipt, sum))); words.Add(result);
            }
            if (!complete) { pending.Add(new Relation(section, S(attempts))); continue; }
            var copy = Section(frame, Site(reverse ? read.End : read.Start), Site(reverse ? read.Start : read.End), words.ToArray());
            output.Add(copy); rows.Add(new Relation(new Relation(section, copy), S(positions)));
        }
        return Finish();

        IElement Site(IElement original)
        {
            if (sites.TryGetValue(original, out var copied)) return copied;
            copied = new Placeholder(); sites.Add(original, copied); sitePairs.Add(new Relation(original, copied)); return copied;
        }
        Relation Finish() => new(new Relation(action, source), S(
            pending.Count == 0 ? S(output) : StructuralConstraint.Open(output.ToArray()), S(rows), S(sitePairs), S(pending)));
    }

    /// <summary>Reads and checks the actual application. Neither a diagnostic
    /// record nor a copied favourable output can authorize a complete read.</summary>
    public static bool TryReadApplication(Curve.Frame frame, Relation application, out ApplicationRead read)
    {
        read = null!;
        if (application.Left is not Relation { Left: Relation action, Right: Relation source } ||
            application.Right is not Relation body || !Closed(body, out var fields) ||
            fields is not [Relation output, Relation rows, Relation pairs, Relation unresolved] ||
            !Scope(output, out var outputItems, out var outputClosed) || !Closed(rows, out var rowItems) ||
            !Closed(pairs, out var siteItems) || !Closed(unresolved, out var causes)) return false;
        if (!Instruction(frame, action, out var map, out var displacement, out var reverse))
        { if (!Invalid(action)) return false; read = new(action, source, output, rows, pairs, unresolved, false, application); return true; }
        if (!Sections(frame, source, out var sourceSections))
        { if (!Invalid(source)) return false; read = new(action, source, output, rows, pairs, unresolved, false, application); return true; }
        var expected = reverse ? sourceSections.Reverse().ToArray() : sourceSections;
        var expectedPending = new List<IElement>(); var successful = new List<Relation>();
        foreach (var section in expected)
        {
            var controls = E(frame.Read(section).ControlWords).Cast<Relation>();
            if (controls.All(word =>
            {
                var receipt = map.Apply(word);
                if (!map.TryReadComplete(receipt, out var mapped)) return false;
                var sum = frame.Motion.Reduce(mapped, displacement);
                return frame.Motion.TryReadResult(sum, out var result, out _, out var pending) &&
                    E(pending).Length == 0 && frame.Vector(result, out _);
            })) successful.Add(section);
            else expectedPending.Add(section);
        }
        if (causes.Length != expectedPending.Count || outputClosed != (expectedPending.Count == 0) ||
            outputItems.Length != successful.Count || rowItems.Length != successful.Count) return false;
        for (var i = 0; i < causes.Length; i++)
            if (causes[i] is not Relation { Left: Relation failed, Right: Relation attempts } ||
                !Same(failed, expectedPending[i]) || !FailedAttempts(failed, attempts)) return false;
        var sites = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var copied = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var allSourceSites = sourceSections.SelectMany(s => { var r = frame.Read(s); return new[] { r.Start, r.End }; })
            .ToHashSet(ReferenceEqualityComparer.Instance);
        foreach (var item in siteItems)
        {
            if (item is not Relation pair || !allSourceSites.Contains(pair.Left) || allSourceSites.Contains(pair.Right) ||
                pair.Right is not Placeholder || !sites.TryAdd(pair.Left, pair.Right) || !copied.Add(pair.Right)) return false;
        }
        var used = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < successful.Count; i++)
        {
            var original = successful[i];
            if (outputItems[i] is not Relation copy || rowItems[i] is not Relation
                { Left: Relation pair, Right: Relation positionScope } || !Same(pair.Left, original) || !Same(pair.Right, copy) ||
                !Closed(positionScope, out var positions)) return false;
            var before = frame.Read(original); var after = frame.Read(copy);
            var beforeWords = E(before.ControlWords).Cast<Relation>().ToArray(); if (reverse) Array.Reverse(beforeWords);
            if (!after.Complete || !Closed(after.ControlWords, out var afterWords) || positions.Length != beforeWords.Length ||
                afterWords.Length != beforeWords.Length || !Site(reverse ? before.End : before.Start, after.Start) ||
                !Site(reverse ? before.Start : before.End, after.End)) return false;
            for (var j = 0; j < positions.Length; j++)
            {
                if (positions[j] is not Relation { Left: Relation words, Right: Relation { Left: Relation sub, Right: Relation sum } } ||
                    !Same(words.Left, beforeWords[j]) || !Same(words.Right, afterWords[j]) ||
                    sub.Left is not Relation request || !Same(request.Right, beforeWords[j]) ||
                    !SubstitutionComplete(map, (Relation)action.Left, sub, out var mapped) ||
                    sum.Left is not Relation { Left: var basis, Right: Relation operands } || !Same(basis, frame.Motion.Surface) ||
                    !Same(operands.Left, mapped) || !Same(operands.Right, displacement) ||
                    !frame.Motion.TryReadResult(sum, out var result, out _, out var pending) ||
                    !Same(result, afterWords[j]) || E(pending).Length != 0) return false;
            }
        }
        if (used.Count != sites.Count) return false;
        read = new(action, source, output, rows, pairs, unresolved, outputClosed, application); return true;

        bool Site(IElement before, IElement after)
        { used.Add(before); return sites.TryGetValue(before, out var mapped) && Same(mapped, after); }
        bool FailedAttempts(Relation section, Relation scope)
        {
            var controls = E(frame.Read(section).ControlWords); if (reverse) Array.Reverse(controls);
            if (!Closed(scope, out var attempts) || attempts.Length == 0 || attempts.Length > controls.Length) return false;
            for (var i = 0; i < attempts.Length; i++)
            {
                if (attempts[i] is not Relation { Left: Relation sub, Right: Relation sum } ||
                    sub.Left is not Relation request || !Same(request.Left, action.Left) || !Same(request.Right, controls[i]) ||
                    !map.TryReadResult(new Relation(new Relation(map.Surface, controls[i]), sub.Right), out _, out _, out _, out _)) return false;
                if (!SubstitutionComplete(map, (Relation)action.Left, sub, out var mapped))
                    return i == attempts.Length - 1 && Scope(sum, out var empty, out var closed) && !closed && empty.Length == 0;
                if (sum.Left is not Relation { Left: var basis, Right: Relation operands } || !Same(basis, frame.Motion.Surface) ||
                    !Same(operands.Left, mapped) || !Same(operands.Right, displacement) ||
                    !frame.Motion.TryReadResult(sum, out var result, out _, out var pending)) return false;
                if (E(pending).Length != 0 || !frame.Vector(result, out _)) return i == attempts.Length - 1;
            }
            return false;
        }
        bool Invalid(IElement cause)
        {
            // A failed input/action stays readable as explicitly open. It has
            // no copied result or made-up default operation.
            if (outputClosed || outputItems.Length != 0 || rowItems.Length != 0 || siteItems.Length != 0 ||
                causes.Length != 1 || !Same(causes[0], cause)) return false;
            return true;
        }
    }

    public static Relation Match(Curve.Frame frame, Relation application, Relation target)
    {
        var sections = new List<IElement>(); var controls = new List<IElement>(); var sites = new List<IElement>();
        var coordinates = new List<IElement>(); var aliases = new List<IElement>(); var pending = new List<IElement>();
        if (!TryReadApplication(frame, application, out var applied) || applied is null || !applied.Complete)
        { pending.Add(application); return Finish(); }
        if (!Sections(frame, target, out var candidates)) { pending.Add(target); return Finish(); }
        var output = E(applied.Output).Cast<Relation>().ToArray();
        if (output.Length != candidates.Length) { pending.Add(new Relation(applied.Output, target)); return Finish(); }
        var forward = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var backward = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < output.Length; i++)
        {
            var pair = new Relation(output[i], candidates[i]); sections.Add(pair);
            var a = frame.Read(output[i]); var b = frame.Read(candidates[i]);
            if (a.Controls.Length != b.Controls.Length) { pending.Add(pair); continue; }
            var aw = E(a.ControlWords); var bw = E(b.ControlWords);
            for (var j = 0; j < aw.Length; j++)
            { var controlPair = new Relation(aw[j], bw[j]); controls.Add(controlPair); if (a.Controls[j] != b.Controls[j]) coordinates.Add(controlPair); }
            Site(a.Start, b.Start); Site(a.End, b.End);
        }
        return Finish();

        void Site(IElement a, IElement b)
        {
            var pair = new Relation(a, b); sites.Add(pair);
            var consistent = (!forward.TryGetValue(a, out var f) || Same(f, b)) && (!backward.TryGetValue(b, out var r) || Same(r, a));
            if (!consistent) aliases.Add(pair);
            else { forward[a] = b; backward[b] = a; }
        }
        Relation Finish() => new(new Relation(application, target), S(S(sections), S(controls), S(sites), S(coordinates), S(aliases), S(pending)));
    }

    public static bool TryReadMatch(Curve.Frame frame, Relation witness, out MatchRead read)
    {
        read = null!;
        if (witness.Left is not Relation { Left: Relation application, Right: Relation target } ||
            witness.Right is not Relation body || !Closed(body, out var fields) ||
            fields is not [Relation sections, Relation controls, Relation sites, Relation coordinates, Relation aliases, Relation unresolved]) return false;
        var expected = Match(frame, application, target);
        var wanted = E((Relation)expected.Right);
        for (var i = 0; i < fields.Length; i++)
            if (fields[i] is not Relation actual || wanted[i] is not Relation reference || !PairScopeEqual(actual, reference)) return false;
        var complete = E(unresolved).Length == 0;
        read = new(application, target, sections, controls, sites, coordinates, aliases, unresolved,
            complete && E(coordinates).Length == 0, complete && E(aliases).Length == 0, complete, witness); return true;
    }

    private static bool Instruction(Curve.Frame frame, Relation action, out QualitativePatternTransform map,
        out Relation displacement, out bool reverse)
    {
        map = null!; displacement = null!; reverse = false;
        if (action is not { Left: Relation surface, Right: Relation { Left: Relation shift, Right: Relation order } } ||
            surface is not { Left: Relation basis, Right: Relation { Left: Relation rows, Right: var evidence } } ||
            !Same(basis.Left, frame.Motion.Surface) || !Same(basis.Right, frame.Motion.Surface) || !frame.Vector(shift, out _) ||
            order is not { Left: Relation { Left: Placeholder a, Right: Placeholder b }, Right: Relation image } ||
            Same(a, b) || a.IsInverse || b.IsInverse) return false;
        if (Same(image.Left, a) && Same(image.Right, b)) reverse = false;
        else if (Same(image.Left, b) && Same(image.Right, a)) reverse = true;
        else return false;
        map = new(frame.Motion, frame.Motion, rows, evidence); displacement = shift; return true;
    }

    private static Relation Substitute(QualitativePatternTransform map, Relation actualSurface, Relation word)
    { var receipt = map.Apply(word); return new(new Relation(actualSurface, word), receipt.Right); }

    private static bool SubstitutionComplete(QualitativePatternTransform map, Relation actualSurface, Relation receipt, out Relation output)
    {
        output = null!;
        if (receipt.Left is not Relation { Right: Relation word } request || !Same(request.Left, actualSurface)) return false;
        // Reconstituting the typed reader allocates its facade Surface. The
        // stored request remains bound to the actual instruction surface;
        // only this temporary validation wrapper uses the facade identity.
        return map.TryReadComplete(new Relation(new Relation(map.Surface, word), receipt.Right), out output);
    }

    private static bool Sections(Curve.Frame frame, Relation scope, out Relation[] sections)
    {
        sections = [];
        if (!Closed(scope, out var entries) || entries.Any(x => x is not Relation)) return false;
        var locations = new Dictionary<IElement, Curve.Vector>(ReferenceEqualityComparer.Instance);
        foreach (var section in entries.Cast<Relation>())
        {
            var read = frame.Read(section);
            if (!read.Complete || !Site(read.Start, read.Controls[0]) || !Site(read.End, read.Controls[^1])) return false;
        }
        sections = entries.Cast<Relation>().ToArray(); return true;
        bool Site(IElement site, Curve.Vector position)
        {
            if (locations.TryGetValue(site, out var old)) return old == position;
            locations.Add(site, position); return true;
        }
    }

    private static Relation Section(Curve.Frame f, IElement a, IElement b, Relation[] words) => words.Length switch
    { 2 => f.Line(a, b, words[0], words[1]), 3 => f.Quadratic(a, b, words[0], words[1], words[2]),
      4 => f.Cubic(a, b, words[0], words[1], words[2], words[3]), _ => throw new ArgumentException("Unsupported section controls.") };
    private static bool PairScopeEqual(Relation actual, Relation expected)
    {
        if (!Closed(actual, out var a) || !Closed(expected, out var b) || a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
        {
            if (Same(a[i], b[i])) continue;
            if (a[i] is not Relation ar || b[i] is not Relation br || !Same(ar.Left, br.Left) || !Same(ar.Right, br.Right)) return false;
        }
        return true;
    }
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static Relation S(IEnumerable<IElement> values) => S(values.ToArray());
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] values) => Scope(scope, out values, out var closed) && closed;
    private static bool Scope(Relation scope, out IElement[] values, out bool closed)
    { closed = false; if (!StructuralConstraint.TryReadScope(scope, out values, out var seam, out var tail)) return false; closed = seam.Equals(tail); return true; }
}
