using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>The three moves between orientations, and reorienting without
/// copying. Reverse flips D, the face flip flips O, and reorient flips both:
/// the same site read by a reader facing the other way.</summary>
public sealed class OrientationMoveTests
{
    private static readonly OdvWord[] Words = [.. Enumerable.Range(0, 8).Select(c => new OdvWord((byte)c))];

    /// <summary>Where a word sits on one line, read from the forward face: the
    /// sign it shows there, or 0 when no magnitude is detectable.</summary>
    private static int Site(OdvWord word) => !word.Detectable ? 0 : (word.PositiveLeft ? -1 : 1) * (word.Negative ? -1 : 1);

    /// <summary>Each move undoes itself, the three commute, and each is the
    /// other two composed: with the identity they are four moves in which
    /// every move is its own inverse.</summary>
    [Fact]
    public void TheThreeMovesUndoThemselvesCommuteAndComposeIntoEachOther()
    {
        foreach (var word in Words)
        {
            Assert.Equal(word, word.Reverse().Reverse());
            Assert.Equal(word, word.FaceFlip().FaceFlip());
            Assert.Equal(word, word.Reorient().Reorient());
            Assert.Equal(word.Reverse().FaceFlip(), word.FaceFlip().Reverse());
            Assert.Equal(word.Reorient(), word.Reverse().FaceFlip());
            Assert.Equal(word.Reverse(), word.Reorient().FaceFlip());
            Assert.Equal(word.FaceFlip(), word.Reorient().Reverse());
            Assert.NotEqual(word, word.FaceFlip());
        }
    }

    /// <summary>Reorient keeps where a word sits and changes how it is
    /// defined; reverse and the face flip each mirror it.</summary>
    [Fact]
    public void ReorientKeepsTheSiteAndTheOtherMovesMirrorIt()
    {
        foreach (var word in Words.Where(w => w.Detectable))
        {
            Assert.Equal(Site(word), Site(word.Reorient()));
            Assert.Equal(-Site(word), Site(word.Reverse()));
            Assert.Equal(-Site(word), Site(word.FaceFlip()));
            Assert.NotEqual(word.PositiveLeft, word.Reorient().PositiveLeft);
        }
    }

    /// <summary>A reoriented view reads every word of a store as the turned
    /// reader sees it, and copies nothing: the store's bits never change from
    /// reading, and a word written through the view lands in the store as the
    /// turned reader means it.</summary>
    [Fact]
    public void AReorientedViewReadsAndWritesTheStoreTurnedAroundWithoutCopying()
    {
        var store = Odv.Create(4);
        foreach (var (position, name) in new[] { (0, "R+?"), (1, "R-?"), (2, "R+0"), (3, "R-0") }) Odv.Write(store, position, OdvNotation.Parse(name));
        var before = Enumerable.Range(0, store.PlaneCount).Select(p => store.Read(p)).ToArray();
        var view = Odv.Reoriented(store);

        for (var p = 0; p < 4; p++) Assert.Equal(Odv.Read(store, p).Reorient(), Odv.Read(view, p));
        Assert.Equal(before, Enumerable.Range(0, store.PlaneCount).Select(p => store.Read(p)));
        Assert.Equal("L-?", Odv.Read(view, 0).ToString());

        Sides.Field.Write(view, 0, Sides.Before);
        Assert.Equal("R+?", Odv.Read(store, 0).ToString());
    }

    /// <summary>A law run on the view leaves the same bits as the law run on a
    /// copy of the store reoriented in place.</summary>
    [Fact]
    public void ALawRunsOnTheViewAsOnTheStoreTurnedInPlace()
    {
        var isPositive = TransitionLaw.FromFunction([Sides.Word, Shape.Bit],
            s => [s[0], new OdvWord((byte)(s[0] | OdvWord.OrientationBit)).Negative ? 0 : 1], new Accounting(true, new Unchanged(0)));
        int[] Run(Surface words)
        {
            var marks = new Store(1, 4);
            var read = new Wiring(isPositive, new Binding(words, Odv.V, Odv.D), new Binding(marks, 0));
            for (var p = 0; p < 4; p++) read.Add(p, p);
            read.Apply();
            return [.. Enumerable.Range(0, 4).Select(p => marks.Read(0, p) ? 1 : 0)];
        }
        Store Filled()
        {
            var store = Odv.Create(4);
            foreach (var (position, name) in new[] { (0, "R+?"), (1, "R-?"), (2, "R+0"), (3, "R-0") }) Odv.Write(store, position, OdvNotation.Parse(name));
            return store;
        }

        var viewed = Run(Odv.Reoriented(Filled()));
        var turned = Filled();
        Odv.Reorient(turned);

        Assert.Equal(Run(turned), viewed);
        Assert.Equal([0, 1, 0, 1], viewed);
    }

    /// <summary>A view belongs to its store's local structure: another engine
    /// may not bind it, and a machine that does bind it saves the view as a
    /// view and restores it running the same.</summary>
    [Fact]
    public void AViewKeepsItsStoresBoundaryAndIsSavedAsAView()
    {
        var store = Odv.Create(2);
        Odv.Write(store, 0, OdvNotation.Parse("R+?"));
        var marks = new Store(1, 2);
        var copy = TransitionLaw.FromFunction([Shape.Bit, Shape.Bit], s => [s[0], s[0]], new Accounting(true, new Unchanged(0)));
        var read = new Wiring(copy, new Binding(Odv.Reoriented(store), Odv.D), new Binding(marks, 0));
        read.Add(0, 0);
        read.Add(1, 1);
        var engine = new Engine().Add(read);
        var other = new Engine();
        var intruding = new Wiring(copy, new Binding(Odv.Reoriented(store), Odv.D), new Binding(new Store(1, 1), 0));
        intruding.Add(0, 0);

        Assert.Throws<ArgumentException>(() => other.Add(intruding));
        var saved = Transport.Save(engine);
        var (restored, _) = Transport.Load(saved);
        Assert.Equal(saved, Transport.Save(restored));
        Assert.IsType<Flipped>(restored.Wirings[0].Bindings[0].Surface);

        engine.Step();
        restored.Step();

        Assert.Equal(marks.Read(0), ((Store)restored.Wirings[0].Bindings[1].Surface).Read(0));
        Assert.False(marks.Read(0, 0));
        Assert.True(marks.Read(0, 1));
    }
}
