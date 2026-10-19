using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class InteriorLatticeRecipesTests
{
    private static readonly Lazy<InteriorLatticeRecipes> Study = new(() => new(2, 2));

    [Fact]
    public void OneHundredRepeatedAreaPartitionsKeepExclusiveActualAtomOwnership()
    {
        var study = Study.Value;
        Assert.Equal(100, study.Cases.Count);
        Assert.Equal(Enumerable.Range(1, 100).Select(index => $"I{index:000}"), study.Cases.Select(item => item.Id));
        Assert.Equal(10, study.Cases.Select(item => item.Family).Distinct().Count());
        foreach (var example in study.Cases)
        {
            var stage = example.Current;
            var read = InteriorCellPartition.Read(stage.Complex, stage.Groups, stage.Bindings, example.Frame.Surface, example.Reach);
            Assert.True(read.IsPartition, example.Id + ": every supplied atom must belong to one connected room.");
            Assert.True(read.IsEmbeddedPartition, example.Id + ": filled native room interiors must not overlap.");
            Assert.All(Reachable(new Relation(stage.Surface, example.Periodicity)), element => Assert.True(element is Relation or Placeholder, example.Id));
            var sourceAtoms = E(((Relation)stage.Complex.Right).Right);
            var claimed = E(stage.Groups).Cast<Relation>().SelectMany(group => E(group.Right)).ToArray();
            Assert.Equal(sourceAtoms.Length, claimed.Length);
            Assert.All(sourceAtoms, atom => Assert.Single(claimed, actual => ReferenceEquals(atom, actual)));
            Assert.True(E(stage.Groups).Cast<Relation>().Any(group => E(group.Right).Length > 1), example.Id);
        }
    }

    [Fact]
    public void PeriodsConnectActualRepeatedClaimAndFaceCorrespondences()
    {
        foreach (var example in Study.Value.Cases)
        {
            var current = E(example.Current.Groups).Cast<Relation>().ToArray();
            Assert.Same(example.Column, ((Relation)example.Periodicity.Left).Left);
            Assert.Same(example.Row, ((Relation)example.Periodicity.Left).Right);
            var rows = E(example.Periodicity.Right).Cast<Relation>().ToArray();
            Assert.Equal(current.Length, rows.Length);
            foreach (var row in rows)
            {
                Assert.Contains(current, actual => ReferenceEquals(actual, row.Left));
                var basis = (Relation)row.Right;
                Assert.Contains(current, actual => ReferenceEquals(actual, basis.Left));
                var displacement = (Relation)basis.Right;
                Assert.NotNull(displacement.Left as Relation);
                var correspondence = E(displacement.Right).Cast<Relation>().ToArray();
                var fromAtoms = E(((Relation)basis.Left).Right);
                var toAtoms = E(((Relation)row.Left).Right);
                Assert.Equal(fromAtoms.Length, correspondence.Length);
                Assert.Equal(toAtoms.Length, correspondence.Length);
                foreach (var pair in correspondence)
                {
                    Assert.Contains(fromAtoms, actual => ReferenceEquals(actual, pair.Right));
                    Assert.Contains(toAtoms, actual => ReferenceEquals(actual, pair.Left));
                }
            }
        }
    }

    [Fact]
    public void SeparatorWithdrawalKeepsTheOldGroupsAndActualRemovedEdges()
    {
        foreach (var example in Study.Value.Cases.Take(90))
        {
            Assert.Equal(3, example.Stages.Count);
            for (var i = 1; i < example.Stages.Count; i++)
            {
                var current = example.Stages[i]; var source = example.Stages[i - 1];
                var transition = current.Transition;
                Assert.Same(source.Groups, ((Relation)transition.Left).Left);
                Assert.Same(current.Groups, ((Relation)transition.Right).Left);
                var removed = E(((Relation)transition.Left).Right);
                Assert.All(removed, edge => Assert.Contains(E(((Relation)current.Complex.Right).Left), actual => ReferenceEquals(actual, edge)));
            }
            Assert.NotEmpty(E(((Relation)example.Current.Transition.Left).Right));
        }
    }

    [Fact]
    public void IndependentDesiredInteriorsStayBlockedUntilActualFaceChoicesSettle()
    {
        foreach (var example in Study.Value.Cases.Skip(90))
        {
            Assert.Equal(4, example.Stages.Count); Assert.NotNull(example.SeamOffers);
            var desired = example.Stages[2];
            var desiredRead = InteriorCellPartition.Read(desired.Complex, desired.Groups);
            Assert.False(desiredRead.IsPartition);
            var offers = example.SeamOffers!;
            Assert.Same(desired.Groups, ((Relation)offers.Left).Right);
            var choices = (Relation)((Relation)offers.Right).Left;
            Assert.NotEmpty(E(choices));
            var negotiation = InteriorBoundaryNegotiation.Reconcile(desired.Complex, desired.Groups, choices);
            Assert.True(negotiation.IsSettled, example.Id);
            Assert.NotEmpty(E(negotiation.Blocked));
            Assert.Empty(E(negotiation.Invalid)); Assert.Empty(E(negotiation.Open));
        }
    }

    [Fact]
    public void CanonicalPeriodAtomsCoverTheDeclaredWholeOnceAtTheAuthoringResolution()
    {
        foreach (var example in Study.Value.Cases)
        {
            var bindings = new Dictionary<IElement, (long X, long Y)>(ReferenceEqualityComparer.Instance);
            foreach (var binding in E(example.Current.Bindings).Cast<Relation>()) bindings.Add(binding.Left, Vector(example, (Relation)binding.Right));
            var u = Vector(example, example.Column); var v = Vector(example, example.Row);
            var determinant = u.X * v.Y - u.Y * v.X;
            Assert.True(determinant > 0, example.Id);
            var representatives = E(example.Periodicity.Right).Cast<Relation>()
                .Select(instance => (Relation)((Relation)instance.Right).Left)
                .Distinct<Relation>(ReferenceEqualityComparer.Instance).ToArray();
            var atoms = representatives.SelectMany(group => E(group.Right)).Cast<Relation>().ToArray();
            long area = 0; var folded = new HashSet<(long U, long V, int Corners)>();
            foreach (var atom in atoms)
            {
                var vertices = E(atom.Right).Cast<Relation>().Select(dart => bindings[((Relation)dart.Right).Left]).ToArray();
                var doubledArea = Math.Abs(vertices.Zip(vertices.Skip(1).Append(vertices[0]), (a, b) => a.X * b.Y - a.Y * b.X).Sum());
                Assert.True(doubledArea > 0, example.Id); area += doubledArea;
                var x = vertices.Sum(point => point.X); var y = vertices.Sum(point => point.Y);
                var whole = vertices.Length * determinant;
                var foldedU = Mod(x * v.Y - y * v.X, whole);
                var foldedV = Mod(u.X * y - u.Y * x, whole);
                Assert.True(folded.Add((foldedU, foldedV, vertices.Length)), example.Id + ": two native period atoms fold to the same site in the selected regular atomic basis.");
            }
            Assert.Equal(2 * determinant, area);
        }
    }

    [Fact]
    public void RepeatedFaceCorrespondencesHaveTheDeclaredActualTranslation()
    {
        foreach (var example in Study.Value.Cases)
        {
            var positions = new Dictionary<IElement, (long X, long Y)>(ReferenceEqualityComparer.Instance);
            foreach (var binding in E(example.Current.Bindings).Cast<Relation>()) positions.Add(binding.Left, Vector(example, (Relation)binding.Right));
            foreach (var instance in E(example.Periodicity.Right).Cast<Relation>())
            {
                var translated = (Relation)((Relation)instance.Right).Right;
                var shift = Vector(example, (Relation)translated.Left);
                foreach (var pair in E(translated.Right).Cast<Relation>())
                {
                    var actual = E(((Relation)pair.Left).Right).Cast<Relation>().Select(dart => positions[((Relation)dart.Right).Left]).Order().ToArray();
                    var basis = E(((Relation)pair.Right).Right).Cast<Relation>().Select(dart => positions[((Relation)dart.Right).Left])
                        .Select(point => (X: point.X + shift.X, Y: point.Y + shift.Y)).Order().ToArray();
                    Assert.Equal(basis, actual);
                }
            }
        }
    }

    [Fact]
    public void TheOneHundredWholeInteriorAssembliesAreDistinctAfterTranslationAndUniformScale()
    {
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        foreach (var example in Study.Value.Cases)
        {
            var stage = example.Current;
            var positions = new Dictionary<IElement, (long X, long Y)>(ReferenceEqualityComparer.Instance);
            foreach (var binding in E(stage.Bindings).Cast<Relation>()) positions.Add(binding.Left, Vector(example, (Relation)binding.Right));
            var left = positions.Values.Min(point => point.X); var bottom = positions.Values.Min(point => point.Y);
            var divisor = positions.Values.SelectMany(point => new[] { point.X - left, point.Y - bottom }).Aggregate(0L, Gcd);
            Assert.True(divisor > 0);
            string Point((long X, long Y) point) => $"{(point.X - left) / divisor},{(point.Y - bottom) / divisor}";
            var read = InteriorCellPartition.Read(stage.Complex, stage.Groups);
            var cycles = new List<string>();
            foreach (var group in read.GroupReads)
            {
                var boundary = E(group.Boundary).Cast<Relation>().Select(dart => positions[((Relation)dart.Right).Left]).ToList();
                // Extra collinear sites are resolution, not new room shape.
                var changed = true;
                while (changed && boundary.Count > 3)
                {
                    changed = false;
                    for (var i = 0; i < boundary.Count; i++)
                    {
                        var a = boundary[(i + boundary.Count - 1) % boundary.Count];
                        var b = boundary[i]; var c = boundary[(i + 1) % boundary.Count];
                        if ((b.X - a.X) * (c.Y - b.Y) != (b.Y - a.Y) * (c.X - b.X)) continue;
                        boundary.RemoveAt(i); changed = true; break;
                    }
                }
                var words = boundary.Select(Point).ToArray();
                cycles.Add(Enumerable.Range(0, words.Length).Select(start => string.Join(';', words.Skip(start).Concat(words.Take(start))))
                    .Min(StringComparer.Ordinal)!);
            }
            var signature = string.Join('|', cycles.Order(StringComparer.Ordinal));
            Assert.True(signatures.Add(signature), example.Id + ": an earlier case already has this normalized collection of actual room boundaries.");
        }
    }

    private static (long X, long Y) Vector(InteriorLatticeCase example, Relation word)
    {
        long x = 0, y = 0;
        foreach (var term in E(word).Cast<Relation>())
        {
            Assert.True(example.Frame.Direction(term, out var axis, out var forward, out var reach));
            Assert.Same(example.Reach, reach);
            if (ReferenceEquals(axis, example.Frame.Horizontal)) x += forward ? 1 : -1;
            else y += forward ? 1 : -1;
        }
        return (x, y);
    }
    private static long Mod(long value, long period) => (value % period + period) % period;
    private static long Gcd(long a, long b) { while (b != 0) (a, b) = (b, a % b); return Math.Abs(a); }

    private static IElement[] E(IElement scope) => FiniteLaw.Entries((Relation)scope);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation row) { pending.Push(row.Left); pending.Push(row.Right); }
        }
    }
}
