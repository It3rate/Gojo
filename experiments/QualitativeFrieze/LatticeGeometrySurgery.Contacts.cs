using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class LatticeGeometrySurgery
{
    /// <summary>An explicitly selected supplier law: publish a chord landmark
    /// where an already advertised endpoint lies strictly inside that chord.
    /// The optional actual period words offer endpoints from neighbouring
    /// copies (-1, 0, +1 in either supplied direction). Common-reach formal
    /// coefficient reads propose cuts; native refinement verifies endpoints,
    /// publication exposes roles, and a later field must separately accept
    /// actual seam agreements. An observer never invokes this law implicitly.
    /// Proper crossings without advertised endpoint landmarks remain unjoined.
    /// </summary>
    public static QualitativeLatticeLadder ConformPublishedContacts(QualitativeLatticeLadder source,
        string name, Relation? column = null, Relation? row = null)
    {
        var program = source.Current.Program; var frame = program.Frame;
        if (column is not null) RequireWord(frame, column);
        if (row is not null) RequireWord(frame, row);
        var paths = N.E(program.Paths).Cast<Relation>().ToArray();
        var allWords = paths.SelectMany(path => N.E((Relation)path.Right).Cast<Relation>().Prepend((Relation)path.Left))
            .Concat(column is null ? [] : new[] { column }).Concat(row is null ? [] : new[] { row }).ToArray();
        var reaches = allWords.SelectMany(N.E).Cast<Relation>().Select(move => move.Right).Distinct(N.Participants.Instance).ToArray();
        var context = new Relation(column ?? N.S(), row ?? N.S());
        if (reaches is not [var reach])
            return source.ReplacePaths(name, StructuralConstraint.Open(), N.S(), program.Ports, program.ClosedPaths,
                new Relation(new Relation(frame.Surface, N.S(reaches)), context));
        var basis = new Relation(frame.Surface, reach);
        var published = new List<(Relation Port, Relation Word, ContactPoint Point, Relation Prefix)>();
        foreach (var port in N.E(program.Ports).Cast<Relation>())
        {
            var pick = (Relation)port.Right; var path = (Relation)pick.Left;
            var words = N.E((Relation)path.Right).Cast<Relation>().ToArray();
            var prefixLength = ReferenceEquals(pick.Right, path) ? words.Length : Array.FindIndex(words, word => ReferenceEquals(word, pick.Right));
            if (prefixLength < 0) throw new ArgumentException("A published contact must select an actual current chord cursor.");
            var prefix = frame.Reduce((Relation)path.Left, Flatten(words.Take(prefixLength).ToArray()));
            var position = Result(prefix);
            published.Add((port, position, Point(position), prefix));
        }
        var offers = new List<(Relation Word, ContactPoint Point, Relation Witness)>();
        foreach (var horizontal in column is null ? new[] { 0 } : new[] { -1, 0, 1 })
        foreach (var vertical in row is null ? new[] { 0 } : new[] { -1, 0, 1 })
        {
            var displacement = Flatten(horizontal == 0 ? N.S() : horizontal > 0 ? column! : UnitArrangementRead.Inverse(frame, column!),
                vertical == 0 ? N.S() : vertical > 0 ? row! : UnitArrangementRead.Inverse(frame, row!));
            foreach (var endpoint in published)
            {
                var translated = frame.Reduce(endpoint.Word, displacement); var position = Result(translated);
                offers.Add((position, Point(position), new Relation(new Relation(endpoint.Port, endpoint.Prefix),
                    new Relation(context, new Relation(displacement, translated)))));
            }
        }
        var cuts = new Dictionary<Relation, SortedSet<long>>(ReferenceEqualityComparer.Instance);
        var provenance = new List<IElement>();
        foreach (var path in paths)
        {
            var at = (Relation)path.Left;
            foreach (var chord in N.E((Relation)path.Right).Cast<Relation>())
            {
                var start = Point(at); var vector = Point(chord);
                var span = ContactGcd(vector.X, vector.Y); var endRead = frame.Reduce(at, chord);
                if (span == 0) { at = Result(endRead); continue; }
                var primitive = new ContactPoint(vector.X / span, vector.Y / span);
                foreach (var offer in offers)
                {
                    var relative = offer.Point - start;
                    if (checked(vector.X * relative.Y - vector.Y * relative.X) != 0) continue;
                    var step = primitive.X != 0 ? relative.X / primitive.X : relative.Y / primitive.Y;
                    if (step <= 0 || step >= span || primitive * step != relative) continue;
                    if (!cuts.TryGetValue(chord, out var selected)) cuts.Add(chord, selected = []);
                    if (!selected.Add(step)) continue;
                    var relativeRead = frame.Reduce(offer.Word, UnitArrangementRead.Inverse(frame, at));
                    provenance.Add(new Relation(new Relation(path, chord), new Relation(new Relation(at, endRead),
                        new Relation(offer.Witness, relativeRead))));
                }
                at = Result(endRead);
            }
        }
        if (cuts.Count == 0) return source;
        var author = new LatticeLadderAuthor(frame, reach);
        var replacements = new List<IElement>();
        foreach (var selected in cuts)
        {
            var vector = Point(selected.Key); var count = ContactGcd(vector.X, vector.Y);
            var primitive = new ContactPoint(vector.X / count, vector.Y / count);
            var boundaries = new[] { 0L }.Concat(selected.Value).Append(count).ToArray();
            var chain = boundaries.Zip(boundaries.Skip(1), (from, to) =>
                (IElement)author.Word(checked((int)(primitive.X * (to - from))), checked((int)(primitive.Y * (to - from))))).ToArray();
            replacements.Add(new Relation(selected.Key, N.S(chain)));
        }
        return RefineAndPublish(source, name, N.S(replacements.ToArray()),
            new Relation(new Relation(basis, context), N.S(provenance.ToArray())));

        Relation Result(Relation read)
        {
            if (!frame.TryReadResult(read, out var word, out _, out var unresolved) || N.E(unresolved).Length != 0)
                throw new ArgumentException("Contact conformation requires a complete native prefix/translation read.");
            return word;
        }
        ContactPoint Point(Relation word)
        {
            long x = 0, y = 0;
            foreach (var move in N.E(word).Cast<Relation>())
            {
                if (!frame.Direction(move, out var axis, out var forward, out var actualReach) || !N.Same(actualReach, reach))
                    throw new ArgumentException("Contact conformation requires one selected common reach in the actual frame.");
                if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
            }
            return new(x, y);
        }
    }

    private readonly record struct ContactPoint(long X, long Y)
    {
        public static ContactPoint operator -(ContactPoint first, ContactPoint second) => new(first.X - second.X, first.Y - second.Y);
        public static ContactPoint operator *(ContactPoint point, long value) => new(checked(point.X * value), checked(point.Y * value));
    }
    private static long ContactGcd(long a, long b)
    { a = Math.Abs(a); b = Math.Abs(b); while (b != 0) (a, b) = (b, a % b); return a; }
}
