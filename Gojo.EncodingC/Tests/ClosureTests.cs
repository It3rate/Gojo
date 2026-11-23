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

    /// <summary>Opening a port is crossing one sealed boundary. Kept closed,
    /// the row reading the port brings in only its lane. Opened once, the lane
    /// selects the owner's position, the plane it shows there, and growth runs
    /// on in the owner's own structure. Opening is a selection, not a
    /// connection: nothing binds the owner's store.</summary>
    [Fact]
    public void OpeningAPortSelectsTheOwnersPositionAndGrowsInItsStructure()
    {
        var other = new Store(2, 4);
        var inside = new Wiring(Settling.Same(Shape.Bit), new Binding(other, 1), new Binding(other, 1));
        for (var i = 0; i < 3; i++) inside.Add(i, i + 1);
        new Engine().Add(inside);
        var port = other.Publish([2, 3], [1]);
        var here = new Store(1, 1);
        var reads = new Wiring(Settling.Same(Shape.Bit), new Binding(port, 0), new Binding(here, 0));
        reads.Add(0, 0);
        var engine = new Engine().Add(reads);

        var closed = Closure.Of(engine, Seed(here, 0), reach: 1);
        var opened = Closure.Of(engine, Seed(here, 0), reach: 1, open: 1);

        Assert.False(closed.Areas.ContainsKey(other));
        Assert.Equal(Plane.Of(2, 0), closed.Of(port));
        Assert.Equal(Plane.Of(4, 1, 2, 3), opened.Of(other));
        Assert.Equal(Plane.Of(3, 1, 2), opened.Rows(inside));
        Assert.Equal(Plane.Of(2, 1), opened.PlanesOf(other));
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

    /// <summary>A settled unit is read through its edge. Garage 1 has been
    /// seen to be Bob's, so the question's cone reaches the garage and stops:
    /// what settled the garage is not reread, and the answer is Bob. Opening
    /// that one boundary rereads the inside, a deed saying Dee, and the
    /// question is contradicted: sealing is a resolution, not a proof.</summary>
    [Fact]
    public void ASettledUnitSealsGrowthUntilItIsOpened()
    {
        Cars Seen()
        {
            var cars = World((Dee, Garage1));
            cars.Owner.Restrict(Garage1, 1UL << Bob);
            return cars;
        }
        var sealedWorld = Seen();
        var sealedQuestion = new Question(sealedWorld.Owner, ThisCar);
        var openedWorld = Seen();

        Assert.True(sealedQuestion.Cone(sealedWorld.Engine, reach: 1).Rows(sealedWorld.Deeds).None);
        Assert.Equal(Plane.Of(2, 0), sealedQuestion.Cone(sealedWorld.Engine, reach: 1, open: 1).Rows(sealedWorld.Deeds));
        Assert.Equal(1, sealedQuestion.Pursue(sealedWorld.Engine, reach: 1));
        Assert.Equal(Bob, sealedQuestion.Answer);
        Assert.Equal(0, new Question(openedWorld.Owner, ThisCar).Pursue(openedWorld.Engine, reach: 1, open: 1));
    }

    /// <summary>Following a referent across structures, out and back. A town
    /// publishes its places, and four structures read them: mornings (Bob on
    /// Main Street, Bob at the garage, and something of its own about Bob),
    /// evenings (Bob on Main Street, Bob in the park), a weekend that never
    /// mentions Bob (Main Street, the library), and a walk read through a
    /// socket (Bob on Main Street). From Bob, the structures that read him are
    /// mornings, evenings and the walk; what all three share is Bob and Main
    /// Street. Computing it again from that core gives it back: it is closed.
    /// Letting one user lack a position changes nothing here; letting two lack
    /// it gives everything any of them reaches, the mornings' own position
    /// included. The weekend's rows are never selected, and neither is a row
    /// that would bind a position trimmed away: rows stay whole.</summary>
    [Fact]
    public void FollowingAReferentAcrossStructuresKeepsWhatItsUsesShare()
    {
        const int Bob = 0, Main = 1, Garage = 2, Park = 3, Library = 4;
        var town = new Store(1, 5);
        var townEngine = new Engine().Hold(town);
        var places = town.Publish([0, 1, 2, 3, 4], [0]);
        Wiring Reads(Surface a, Surface b, params (int, int)[] rows)
        {
            var w = new Wiring(Settling.Same(Shape.Bit), new Binding(a, 0), new Binding(b, 0));
            foreach (var (x, y) in rows) w.Add(x, y);
            return w;
        }
        var own = new Store(1, 1);
        var morningsOwn = Reads(places, own, (Bob, 0));
        var mornings = new Engine().Add(Reads(places, places, (Bob, Main), (Bob, Garage))).Add(morningsOwn);
        var evenings = new Engine().Add(Reads(places, places, (Bob, Main), (Bob, Park)));
        var weekendRows = Reads(places, places, (Main, Library));
        var weekend = new Engine().Add(weekendRows);
        var socket = Socket.For(places);
        socket.Plug(places);
        var walk = new Engine().Add(Reads(socket, socket, (Bob, Main)));
        Engine[] view = [mornings, evenings, weekend, walk, townEngine];

        var core = Closure.Common(view, Seed(places, Bob));
        var again = Closure.Common(view, new Dictionary<Surface, Plane>(ReferenceEqualityComparer.Instance) { [town] = core.Of(town) });
        var loose = Closure.Common(view, Seed(town, Bob), missing: 1);
        var any = Closure.Common(view, Seed(town, Bob), missing: 2);

        Assert.Equal(Plane.Of(5, Bob, Main), core.Of(town));
        Assert.Equal(core.Of(town), again.Of(town));
        Assert.Equal(core.Of(town), loose.Of(town));
        Assert.Equal(Plane.Of(5, Bob, Main, Garage, Park), any.Of(town));
        Assert.Equal(Plane.Of(1, 0), any.Of(own));
        Assert.True(core.Of(own).None);
        Assert.True(core.Rows(weekendRows).None);
        Assert.True(core.Rows(morningsOwn).None);
        Assert.Equal(Plane.Of(1, 0), any.Rows(morningsOwn));
        Assert.Equal(Plane.Of(1, 0), core.PlanesOf(town));
    }

    /// <summary>Gathering by reach, then trimming to what the uses share. Bob's
    /// day is three structures, each a chain from Bob: mornings go Bob, Main
    /// Street, the bakery, the river; evenings go Bob, Main Street, the bakery,
    /// and Bob to the park; a walk goes Bob, Main Street, the library. Each use
    /// grows from Bob through its own rows only, and a place's score is how
    /// many uses reached it. Plain growth two rows out takes in the river,
    /// which only the mornings reach that far. Letting one use lack a place
    /// keeps the bakery, two rows out but reached by two days, and drops the
    /// park, the library and the river; the rows kept are exactly the whole
    /// rows among the places kept.</summary>
    [Fact]
    public void GatheringByReachThenTrimmingKeepsWhatIsNearAndShared()
    {
        const int Bob = 0, Main = 1, Bakery = 2, Park = 3, Library = 4, River = 5;
        var town = new Store(1, 6);
        var townEngine = new Engine().Hold(town);
        var places = town.Publish([0, 1, 2, 3, 4, 5], [0]);
        Wiring Chain(params (int, int)[] rows)
        {
            var w = new Wiring(Settling.Same(Shape.Bit), new Binding(places, 0), new Binding(places, 0));
            foreach (var (x, y) in rows) w.Add(x, y);
            return w;
        }
        var morningRows = Chain((Bob, Main), (Main, Bakery), (Bakery, River));
        var eveningRows = Chain((Bob, Main), (Main, Bakery), (Bob, Park));
        var walkRows = Chain((Bob, Main), (Main, Library));
        Engine[] view = [new Engine().Add(morningRows), new Engine().Add(eveningRows), new Engine().Add(walkRows), townEngine];

        var near = Closure.Common(view, Seed(town, Bob), reach: 0);
        var reached = Closure.Common(view, Seed(town, Bob), reach: 1);
        var grown = Closure.Common(view, Seed(town, Bob), missing: 2, reach: 2);
        var trimmed = Closure.Common(view, Seed(town, Bob), missing: 1, reach: 2);

        Assert.Equal(Plane.Of(6, Bob, Main), near.Of(town));
        Assert.Equal([3, 3, 2, 1, 1, 0], Enumerable.Range(0, 6).Select(p => reached.Score(town, p)));
        Assert.Equal(Plane.Of(6, Bob, Main, Bakery, Park, Library, River), grown.Of(town));
        Assert.Equal(Plane.Of(6, Bob, Main, Bakery), trimmed.Of(town));
        Assert.Equal(Plane.Of(3, 0, 1), trimmed.Rows(morningRows));
        Assert.Equal(Plane.Of(3, 0, 1), trimmed.Rows(eveningRows));
        Assert.Equal(Plane.Of(2, 0), trimmed.Rows(walkRows));
    }
}
