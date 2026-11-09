using Xunit;

namespace Gojo.EncodingB.Tests;

public sealed class LawTests
{
    [Fact]
    public void PassageLawIsTheEightHistoricalBytes()
    {
        Assert.Equal([0x01, 0x02, 0x04, 0x00, 0x10, 0x20, 0x48, 0x80], Passage.Law.Bytes());
        Assert.Equal(0x8048201000040201UL, Passage.Law.AsUInt64());
        Assert.True(Passage.Law.Admits(3, 6));
        Assert.True(Passage.Law.BitAt(3 + 8 * 6));
        Assert.Empty(Enumerable.Range(0, 8).Where(before => Passage.Law.Admits(before, 3)));
        Assert.Equal(["S", "T"], Passage.Law.WrittenRoles);
        Assert.True(Passage.Law.IsFunctional);
    }

    [Fact]
    public void ALawThatBreaksItsSignatureIsRefusedAtLoad()
    {
        var error = Assert.Throws<ArgumentException>(() => TransitionLaw.FromFunction(
            "duplicating passage", Passage.Roles, state => state is [1, 1, 0] ? [1, 1, 1] : state,
            Passage.Signature, ConfluenceClass.ScheduledDescent));
        Assert.Contains("sum of S + T preserved", error.Message);
    }

    [Fact]
    public void ContentHashIdentifiesTheTableNotTheName()
    {
        var renamed = new TransitionLaw("valve", Passage.Roles, Passage.Law.Bytes(), Passage.Signature, ConfluenceClass.ScheduledDescent);
        Assert.Equal(Passage.Law.ContentHash, renamed.ContentHash);
        var asRelation = new RelationLaw("same bytes", [new LawRole("x", Domain.Binary("b", "0", "1")), .. Enumerable.Range(0, 5)
            .Select(i => new LawRole("x" + i, Domain.Binary("b", "0", "1")))], Passage.Law.Bytes());
        Assert.NotEqual(Passage.Law.ContentHash, asRelation.ContentHash);
    }

    [Fact]
    public void SixtyFourPassagesStepAsThreePlaneOperations()
    {
        var random = new Random(7);
        for (var trial = 0; trial < 50; trial++)
        {
            var s = (ulong)random.NextInt64();
            var c = (ulong)random.NextInt64();
            var t = (ulong)random.NextInt64();
            var step = Passage.Law.Step([[s], [c], [t]], ulong.MaxValue);
            Assert.Equal(s & (~c | t), step.After[0][0]);
            Assert.Equal(c, step.After[1][0]);
            Assert.Equal(t | (s & c), step.After[2][0]);
            Assert.Equal(Bits.Count(s) + Bits.Count(t), Bits.Count(step.After[0][0]) + Bits.Count(step.After[2][0]));
            var offered = s & c;
            Assert.Equal(offered, (offered & ~t) | (offered & t));
            Assert.Equal(0UL, step.Unadmitted | step.Ambiguous);
        }
    }

    [Fact]
    public void JointPredictionMovesReadyStatesAndNeverExecutes()
    {
        var joint = new ulong[8];
        joint[1] = 0b01;
        joint[3] = 0b11;
        var next = Passage.Law.PredictJoint(joint);
        Assert.Equal(0b01UL, next[1]);
        Assert.Equal(0UL, next[3]);
        Assert.Equal(0b11UL, next[6]);
        var lane0 = Enumerable.Range(0, 8).Where(t => Bits.Get(next[t], 0)).Aggregate(0, (m, t) => m | 1 << t);
        Assert.Equal(0x42, lane0);
    }

    [Fact]
    public void RowsThatShareASiteRunInDeclaredOrder()
    {
        var tile = new Tile("chain", Passage.Grammar, 5, ["A", "C1", "B", "C2", "T"]);
        tile.Seed("occupied", "10000");
        tile.Seed("open", "01010");
        var rows = Passage.Rows("chain", [[0, 1, 2], [2, 3, 4]]);
        Assert.Equal([0b01UL, 0b10UL], rows.Waves(rows.AllRows));
        var witness = rows.Apply(tile, rows.AllRows);
        Assert.Equal("00001", Bits.Format(tile.Read("occupied"), 5));
        Assert.True(witness.Transitions.All(t => t.Changed));
        Assert.Contains("[S=occupied, C=open, T=empty]", witness.Transitions[0].BeforeRead);
    }

    [Fact]
    public void IndependentRowsShareOneWave()
    {
        var rows = Passage.Rows("pair", [[0, 1, 2], [3, 4, 5]]);
        Assert.Equal([0b11UL], rows.Waves(rows.AllRows));
    }

    [Fact]
    public void ATransitionCannotWriteOneSiteAsTwoRoles()
    {
        Assert.Throws<ArgumentException>(() => Passage.Rows("loop", [[0, 1, 0]]));
    }

    [Fact]
    public void RelationViolationsAreTheLawResidual()
    {
        var tile = new Tile("sums", Odv.Grammar, 3);
        tile.SeedCode("word", 0, OdvWord.Parse("R+?").Code);
        tile.SeedCode("word", 1, OdvWord.Parse("R+?").Code);
        tile.SeedCode("word", 2, OdvWord.Parse("R-?").Code);
        var rows = new LawRows("sum", Odv.Addition(), Odv.Grammar, ["word", "word", "word"], [[0, 1, 2]]);
        Assert.Equal(1UL, rows.Violations(tile));
        tile.SeedCode("word", 2, OdvWord.Parse("R+?").Code);
        Assert.Equal(0UL, rows.Violations(tile));
    }
}
