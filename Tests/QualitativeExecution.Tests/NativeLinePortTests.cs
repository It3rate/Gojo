using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class NativeLinePortTests
{
    [Theory]
    [InlineData("N01")]
    [InlineData("N02")]
    [InlineData("N04")]
    [InlineData("N05")]
    [InlineData("N07")]
    [InlineData("N13")]
    public void Public_port_alone_reconstructs_candidate_questions_without_example_names_or_private_history(string id)
    {
        var source = NativeLatticeSources.Cases().Single(item => item.Id == id);
        var original = new NativeLineSupplier(source, candidateCapacity: 12);
        var packet = GojoStructureCompactJson.FromElement("exposed-native-lattice", original.Port);
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(
            GojoStructureCompactJson.Serialize(packet)), out var restored));
        Assert.True(NativeLineSupplier.TryReadPort(Assert.IsType<Relation>(restored.Root), out var imported, out var witness, candidateCapacity: 12));
        Assert.NotNull(witness); Assert.Equal("imported", imported.Source.Id);
        Assert.Equal(original.Candidates.Count, imported.Candidates.Count);
        Assert.DoesNotContain(OrnamentReconstruction.Participants(original.Port), element => ReferenceEquals(element, source.Provenance));
        for (var i = 0; i < original.Candidates.Count; i++)
            for (var q = 0; q < original.Candidates[i].Frame.Goals.Count; q++)
            {
                var a = original.Candidates[i]; var b = imported.Candidates[i];
                var before = a.Read(a.Frame.Goals[q].Predicate); var after = b.Read(b.Frame.Goals[q].Predicate);
                Assert.Equal(before.Answer, after.Answer);
                Assert.Equal(StructuralPotential.Items(before.ServiceRequests).Length, StructuralPotential.Items(after.ServiceRequests).Length);
            }
        Assert.All(OrnamentReconstruction.Participants(imported.Port), element => Assert.True(element is Relation or Placeholder));
    }
}
