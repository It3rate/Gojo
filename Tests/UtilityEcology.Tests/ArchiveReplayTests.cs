using Gojo.Core;

namespace UtilityEcology.Tests;

public sealed class ArchiveReplayTests
{
    [Theory]
    [InlineData("opportunity/success-paid")]
    [InlineData("opportunity/success-paid/use-reset")]
    public void ActualFileKeepsModelPublicationsAndCoupledPaidGateContinuation(string arm)
    {
        var run = EcologyRunner.Run(605, EcologyRunner.Arms.Single(candidate => candidate.Name == arm), new(PhaseLength: 64));
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, GojoNumericFormat.Encode(run.NativeRoot));
            var restored = Assert.IsType<Relation>(GojoNumericFormat.Decode(File.ReadAllBytes(path))[0]);
            var check = ArchiveReplay.Check(run.NativeRoot, restored);
            Assert.True(check.Matched); Assert.True(check.ContinuedMoneyConserved);
            Assert.True(check.SamplesReplayed >= 4); Assert.True(check.AttentionSpent >= 4);
        }
        finally { File.Delete(path); }
    }
}
