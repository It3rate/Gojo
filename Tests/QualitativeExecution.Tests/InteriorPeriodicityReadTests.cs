using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class InteriorPeriodicityReadTests
{
    [Fact]
    public void ActualFaceCorrespondencesEstablishTwoPeriodDirectionsAndOneOccupiedPeriod()
    {
        var f = Squares(); var read = Read(f, f.Periodicity);
        Assert.True(read.IsComplete); Assert.True(read.IsPeriodic); Assert.Equal(2, read.Summary.IndependentPeriods);
        Assert.Equal(4, read.Summary.AcceptedInstances); Assert.Equal(4, read.Summary.MatchedAtomPairs);
        Assert.True(read.Summary.CanonicalAreaMatchesPeriod); Assert.Equal("2", read.Summary.TwiceCanonicalFormalArea);
        Assert.Empty(E(read.Blocked)); Assert.Empty(E(read.Open));
        Assert.Contains(Reachable(read.Surface), item => ReferenceEquals(item, f.Periodicity));
        Assert.All(Reachable(read.Surface), item => Assert.True(item is Placeholder or Relation));
    }

    [Fact]
    public void AClaimedTranslationMustMatchTheActualOrderedFaceBoundaries()
    {
        var f = Squares(); var rows = E(f.Periodicity.Right).Cast<Relation>().ToArray();
        var old = rows[1]; var context = (Relation)old.Right; var mapping = (Relation)context.Right;
        rows[1] = new(old.Left, new Relation(context.Left, new Relation(f.Author.Word(2, 0), mapping.Right)));
        var read = Read(f, new Relation(f.Periodicity.Left, S(rows)));
        Assert.False(read.IsPeriodic); Assert.NotEmpty(E(read.Blocked));
        Assert.Contains(Reachable(read.Blocked), item => ReferenceEquals(item, rows[1]));
    }

    [Fact]
    public void ALookalikeAtomDoesNotReplaceAnActualCorrespondenceParticipant()
    {
        var f = Squares(); var rows = E(f.Periodicity.Right).Cast<Relation>().ToArray();
        var old = rows[1]; var context = (Relation)old.Right; var mapping = (Relation)context.Right;
        var pair = (Relation)Assert.Single(E(mapping.Right)); var atom = (Relation)pair.Left;
        var falsePair = new Relation(new Relation(atom.Left, atom.Right), pair.Right);
        rows[1] = new(old.Left, new Relation(context.Left, new Relation(mapping.Left, S(falsePair))));
        var read = Read(f, new Relation(f.Periodicity.Left, S(rows)));
        Assert.False(read.IsPeriodic); Assert.NotEmpty(E(read.Blocked));
    }

    [Fact]
    public void MissingAndDuplicateInstanceOffersDoNotReadAsACompleteRepeat()
    {
        var f = Squares(); var rows = E(f.Periodicity.Right).Cast<Relation>().ToArray();
        var missing = Read(f, new Relation(f.Periodicity.Left, S(rows.Take(3).ToArray())));
        Assert.False(missing.IsComplete); Assert.NotEmpty(E(missing.Open));
        var duplicate = Read(f, new Relation(f.Periodicity.Left, S(rows.Append(rows[1]).ToArray())));
        Assert.False(duplicate.IsComplete); Assert.NotEmpty(E(duplicate.Blocked));
        Assert.Equal(3, duplicate.Summary.AcceptedInstances);
    }

    [Fact]
    public void CollinearPeriodsCannotSupplyASecondRepeatDirection()
    {
        var f = Squares(); var bad = new Relation(new Relation(f.Author.Word(1, 0), f.Author.Word(2, 0)), f.Periodicity.Right);
        var read = Read(f, bad); Assert.False(read.IsPeriodic); Assert.Equal(1, read.Summary.IndependentPeriods);
        Assert.NotEmpty(E(read.Blocked));
    }

    [Fact]
    public void TheCanonicalOccupiedAreaMustMatchTheDeclaredPeriod()
    {
        var f = Squares(); var period = new Relation(new Relation(f.Author.Word(2, 0), f.Author.Word(0, 2)), f.Periodicity.Right);
        var read = Read(f, period); Assert.False(read.IsPeriodic); Assert.False(read.Summary.CanonicalAreaMatchesPeriod);
        Assert.Equal("8", read.Summary.TwicePeriodFormalArea); Assert.Equal("2", read.Summary.TwiceCanonicalFormalArea);
    }

    [Fact]
    public void AnEqualLookingGroupWrapperIsNotAnActualInstanceGroup()
    {
        var f = Squares(); var rows = E(f.Periodicity.Right).Cast<Relation>().ToArray();
        var group = (Relation)rows[1].Left; rows[1] = new(new Relation(group.Left, group.Right), rows[1].Right);
        var read = Read(f, new Relation(f.Periodicity.Left, S(rows)));
        Assert.False(read.IsPeriodic); Assert.NotEmpty(E(read.Blocked)); Assert.NotEmpty(E(read.Open));
    }

    [Fact]
    public void AnOpenInstanceScopeAndForeignReachRemainUnresolved()
    {
        var f = Squares(); var rows = E(f.Periodicity.Right).ToArray();
        var open = Read(f, new Relation(f.Periodicity.Left, StructuralConstraint.Open(rows)));
        Assert.False(open.IsComplete); Assert.NotEmpty(E(open.Open));
        var foreign = InteriorPeriodicityRead.Read(f.Complex, f.Groups, f.Bindings, f.Author.Frame, new Placeholder(), f.Periodicity);
        Assert.False(foreign.IsPeriodic); Assert.NotEmpty(E(foreign.Open));
    }

    [Fact]
    public void AllOneHundredAuthoredFieldsHaveActualTwoDimensionalTranslatedTemplateEvidence()
    {
        var study = new InteriorLatticeRecipes(2, 2);
        Assert.Equal(100, study.Cases.Count);
        foreach (var item in study.Cases)
        {
            var stage = item.Current;
            var read = InteriorPeriodicityRead.Read(stage.Complex, stage.Groups, stage.Bindings, item.Frame, item.Reach, item.Periodicity);
            Assert.True(read.IsPeriodic, item.Id + ": " + read.Summary);
            Assert.True(read.Summary.CanonicalAreaMatchesPeriod, item.Id);
        }
    }

    private static InteriorPeriodicityRead Read(Fixture f, Relation periodicity) =>
        InteriorPeriodicityRead.Read(f.Complex, f.Groups, f.Bindings, f.Author.Frame, f.Author.Reach, periodicity);
    private sealed record Fixture(LatticeLadderAuthor Author, Relation Complex, Relation Groups, Relation Bindings, Relation Periodicity);
    private static Fixture Squares()
    {
        var author = new LatticeLadderAuthor();
        var sites = new Dictionary<(int X, int Y), IElement>(); var edges = new Dictionary<(int A, int B), Relation>();
        IElement Site(int x, int y)
        {
            if (!sites.TryGetValue((x, y), out var site)) sites.Add((x, y), site = new Placeholder());
            return site;
        }
        Relation Dart(int x, int y, int dx, int dy)
        {
            var from = Site(x, y); var to = Site(x + dx, y + dy); var a = y * 3 + x; var b = (y + dy) * 3 + x + dx;
            var key = (Math.Min(a, b), Math.Max(a, b));
            if (!edges.TryGetValue(key, out var edge)) edges.Add(key, edge = InteriorCellPartition.Edge(new Placeholder(), from, to));
            return InteriorCellPartition.Dart(edge, from, to);
        }
        var faces = new List<Relation>(); var groups = new List<Relation>(); var shifts = new List<Relation>();
        for (var y = 0; y < 2; y++) for (var x = 0; x < 2; x++)
        {
            var face = InteriorCellPartition.Face(new Placeholder(), Dart(x, y, 1, 0), Dart(x + 1, y, 0, 1),
                Dart(x + 1, y + 1, -1, 0), Dart(x, y + 1, 0, -1));
            faces.Add(face); groups.Add(InteriorCellPartition.Claim(new Placeholder(), face)); shifts.Add(author.Word(x, y));
        }
        var complex = InteriorCellPartition.Topology(S(sites.Values.ToArray()), S(edges.Values.ToArray()), S(faces.ToArray()));
        var bindings = S(sites.Select(entry => (IElement)new Relation(entry.Value, author.Word(entry.Key.X, entry.Key.Y))).ToArray());
        var instances = S(groups.Select((group, i) => (IElement)new Relation(group,
            new Relation(groups[0], new Relation(shifts[i], S(new Relation(faces[i], faces[0])))))).ToArray());
        return new(author, complex, S(groups.ToArray()), bindings,
            new Relation(new Relation(author.Word(1, 0), author.Word(0, 1)), instances));
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(IElement scope) => scope is Relation relation &&
        StructuralConstraint.TryReadScope(relation, out var entries, out _, out _) ? entries : [];
    private static IEnumerable<IElement> Reachable(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { pending.Push(relation.Right); pending.Push(relation.Left); }
        }
    }
}
