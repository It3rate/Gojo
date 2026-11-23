using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Selection as closure over a seed, and a question as a goal that its
/// open value settle (docs/GojoStructureClosure-2026-11.md). A selection is
/// built from structure: a seed snaps up to the whole rows it is part of, those
/// rows bring in what they bind, reach grows it row by row, a port keeps only
/// its head, and what no selected row reads is left out.</summary>
public sealed class ClosureTests
{
    private static Dictionary<Surface, Plane> Seed(Surface surface, params int[] positions) =>
        new(ReferenceEqualityComparer.Instance) { [surface] = Plane.Of(surface.Count, positions) };

    /// <summary>"Mary put the book on the shelf", and Tom, whom nothing is said
    /// about. Selecting Mary selects the clause, not "Mary put": the whole row
    /// and everything it binds. Selecting the clause's row directly is the same
    /// unit. Tom, bound by no row, is kept alone as a referent.</summary>
    [Fact]
    public void ASeedSnapsUpToItsWholeRowAndALonePositionStaysAReferent()
    {
        const int Mary = 0, Book = 1, Shelf = 2, Tom = 3;
        var things = new Store(1, 4);
        var put = new Wiring(RelationLaw.FromPredicate([Shape.Bit, Shape.Bit, Shape.Bit], _ => true),
            new Binding(things, 0), new Binding(things, 0), new Binding(things, 0));
        put.Add(Mary, Book, Shelf);
        var engine = new Engine().Add(put);

        var fromMary = Closure.Of(engine, Seed(things, Mary));
        var fromRow = Closure.Of(engine, Seed(put.Rows, 0));
        var fromTom = Closure.Of(engine, Seed(things, Tom));

        Assert.Equal(Plane.Of(4, Mary, Book, Shelf), fromMary.Of(things));
        Assert.Equal(Plane.Of(1, 0), fromMary.Rows(put));
        Assert.Equal(fromMary.Of(things), fromRow.Of(things));
        Assert.Equal(Plane.Of(4, Tom), fromTom.Of(things));
        Assert.True(fromTom.Rows(put).None);
    }

    /// <summary>A chain of positions, each held to the next, a second field no
    /// row reads, and a port into another structure. From position 0, reach 0
    /// is the first row; each unit of reach adds one row. A row that binds the
    /// port brings in only its lane, the head of the structure behind it, and
    /// growth does not continue through that lane to the row that shares it.
    /// The unread field's plane is not part of the selection.</summary>
    [Fact]
    public void ClosureGathersGrowsByReachStopsAtAPortAndDropsUnreadPlanes()
    {
        var chain = new Store(2, 6);
        var other = new Store(1, 3);
        new Engine().Hold(other);
        var port = other.Publish([0, 1], [0]);
        var links = new Wiring(Settling.Same(Shape.Bit), new Binding(chain, 0), new Binding(chain, 0));
        for (var i = 0; i < 4; i++) links.Add(i, i + 1);
        var reads = new Wiring(Settling.Same(Shape.Bit), new Binding(port, 0), new Binding(chain, 0));
        reads.Add(0, 1);
        reads.Add(0, 5);
        var engine = new Engine().Add(links).Add(reads);

        var near = Closure.Of(engine, Seed(chain, 0));
        var far = Closure.Of(engine, Seed(chain, 0), reach: 2);

        Assert.Equal(Plane.Of(6, 0, 1), near.Of(chain));
        Assert.True(near.Of(port).None);
        Assert.Equal(Plane.Of(4, 0, 1, 2), far.Rows(links));
        Assert.Equal(Plane.Of(6, 0, 1, 2, 3), far.Of(chain));
        Assert.Equal(Plane.Of(2, 0), far.Rows(reads));
        Assert.Equal(Plane.Of(2, 0), far.Of(port));
        Assert.False(far.Areas.ContainsKey(other));
        Assert.Equal(Plane.Of(2, 0), far.PlanesOf(chain));
    }

    /// <summary>"Whose car is this?"</summary>
    private sealed record Cars(Engine Engine, Store Facts, Store Owned, Supports Owner, Wiring Deeds, Wiring Parked);

    private const int Ann = 0, Bob = 1, Cy = 2, Dee = 3;
    private const int Car0 = 0, Car1 = 1, ThisCar = 2, Garage0 = 3, Garage1 = 4;
    private static readonly Shape Person = Shape.Binary(4);

    /// <summary>Who owns each car and garage is open. Deeds are facts, each a
    /// person, about whose a garage or car is; a car parked in a garage is its
    /// owner's. This car is in garage 1. Unrelated: car 0 has a deed of its
    /// own, and car 1 is parked in garage 0.</summary>
    private static Cars World(params (int Person, int Of)[] deeds)
    {
        var facts = new Store(2, deeds.Length + 1);
        var person = new Field(Person, 0, 1);
        var owned = new Store(0, 5);
        var owner = Supports.On(owned, new Field(Person, owned.AddPlane(), owned.AddPlane()));
        var deed = new Wiring(Settling.Same(Person), new Binding(facts, 0, 1), owner.Variable);
        var parked = new Wiring(Settling.Same(Person), owner.Variable, owner.Variable);
        for (var i = 0; i < deeds.Length; i++)
        {
            person.Write(facts, i, deeds[i].Person);
            deed.Add(i, deeds[i].Of);
        }
        person.Write(facts, deeds.Length, Cy);
        deed.Add(deeds.Length, Car0);
        parked.Add(Garage0, Car1);
        parked.Add(Garage1, ThisCar);
        return new Cars(new Engine().Add(deed).Add(parked), facts, owned, owner, deed, parked);
    }

    /// <summary>Closure from the question selects only what could answer it:
    /// the garage this car is in, the deed to that garage, and the rows joining
    /// them. Pursuing it answers Bob. Car 0's deed would settle car 0, but it
    /// is not in the question's cone, so it never runs and car 0 stays open.
    /// With too little reach the deed is out of sight and the question stays
    /// open, the closure was not enough, not the world.</summary>
    [Fact]
    public void WhoseCarIsThisSelectsOnlyWhatCouldAnswerIt()
    {
        var cars = World((Bob, Garage1));
        var question = new Question(cars.Owner, ThisCar);

        var cone = question.Cone(cars.Engine, reach: 1);

        Assert.Equal(Plane.Of(5, ThisCar, Garage1), cone.Of(cars.Owned));
        Assert.Equal(Plane.Of(2, 0), cone.Of(cars.Facts));
        Assert.Equal(Plane.Of(2, 1), cone.Rows(cars.Parked));
        Assert.Equal(Plane.Of(2, 0), cone.Rows(cars.Deeds));
        Assert.Equal(4, question.Remaining);

        var shallow = World((Bob, Garage1));
        Assert.Equal(4, new Question(shallow.Owner, ThisCar).Pursue(shallow.Engine, reach: 0));
        Assert.Equal(1, question.Pursue(cars.Engine, reach: 1));
        Assert.Equal(Bob, question.Answer);
        Assert.Equal(4, new Question(cars.Owner, Car0).Remaining);
    }

    /// <summary>With the deciding fact absent, the question stays open; with
    /// two deeds that disagree, it is contradicted. Neither is a failure: each
    /// says what the structure knows.</summary>
    [Fact]
    public void AQuestionStaysOpenWithoutTheFactAndIsContradictedByConflictingOnes()
    {
        var missing = World();
        var conflicting = World((Bob, Garage1), (Dee, Garage1));

        Assert.Equal(4, new Question(missing.Owner, ThisCar).Pursue(missing.Engine, reach: 1));
        var contradicted = new Question(conflicting.Owner, ThisCar);
        Assert.Equal(0, contradicted.Pursue(conflicting.Engine, reach: 1));
        Assert.Null(contradicted.Answer);
    }
}
