using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;
using Corpus = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentIntegratedCorpus;
using Source = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrateCorpus;
using Attachment = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentMotifAttachment;

namespace QualitativeExecution.Tests;

public sealed class OrnamentIntegratedTests
{
    private static readonly Lazy<IReadOnlyList<Corpus.Design>> Cases = new(() => Corpus.Cases());
    private static IElement[] E(Relation s)
    { Assert.True(StructuralConstraint.TryReadScope(s, out var entries, out _, out _)); return entries; }

    [Fact]
    public void The_new_layer_preserves_actual_source_mesh_and_carries_old_decoration()
    {
        Assert.Equal(12, Cases.Value.Count);
        foreach (var d in Cases.Value)
        {
            var f = d.Unit.Template.Frame; Assert.True(Parts.ReadTemplate(f, d.Unit.Template.Surface).Complete);
            Assert.NotEmpty(d.Mesh.Template.Envelopes); Assert.NotEmpty(d.Motifs);
            Assert.Same(d.Source.Surface, d.Surface.Left);
            Assert.All(d.Mesh.Template.Envelopes, e => {
                Assert.Contains(d.Source.Ornament.Unit.Template.Envelopes, original => ReferenceEquals(original, e));
                Assert.Contains(d.Unit.Template.Envelopes, result => ReferenceEquals(result, e));
            });
            Assert.All(E(d.Removed), e => Assert.DoesNotContain(d.Unit.Template.Envelopes, result => ReferenceEquals(result, e)));
            Assert.Equal(d.Source.Ornament.Unit.Crossings, d.Unit.Crossings);
        }
    }

    [Fact]
    public void Mesh_selection_reads_native_generation_outputs_instead_of_diagnostic_arrays()
    {
        var d = Cases.Value[0]; var original = d.Source;
        var revised = Corpus.Integrate(original with { Generations = [] });
        Assert.Equal(d.Mesh.Template.Envelopes, revised.Mesh.Template.Envelopes);
    }

    [Fact]
    public void Every_root_is_incident_and_belongs_to_an_actual_preserved_mesh_participant()
    {
        foreach (var d in Cases.Value) foreach (var a in d.Attachments)
        {
            var read = Attachment.ReadAttachment(d.Unit.Template.Frame, a.Surface);
            Assert.True(read.Incident); Assert.True(read.Port.Complete);
            Assert.Contains(d.Mesh.Template.Envelopes, e => ReferenceEquals(e, read.Port.Envelope));
            Assert.Contains(d.Unit.Template.Envelopes, e => ReferenceEquals(e, read.Motif));
            if (read.Port.Interior) Assert.False(read.SharedSite);
            else Assert.True(read.SharedSite);
        }
    }

    [Fact]
    public void Curve_following_lens_blades_reuse_the_actual_mesh_section()
    {
        var d = Cases.Value[5]; var f = d.Unit.Template.Frame;
        Assert.Contains(d.Attachments, a => ReferenceEquals(Band.ReadEnvelope(f, a.Motif).Sweep.Spine.Section, a.Port.Section));
        Assert.All(d.Attachments.Where(a => ReferenceEquals(Band.ReadEnvelope(f, a.Motif).Sweep.Spine.Section, a.Port.Section)), a => {
            Assert.True(a.Incident); Assert.True(a.Smooth); Assert.True(a.SharedSite);
            var band = Band.ReadEnvelope(f, a.Motif).Sweep;
            Assert.Equal(f.Read(a.Port.Section).Controls, band.SideA.Controls);
            Assert.False(band.Collapsed);
        });
    }

    [Fact]
    public void Rooted_control_generation_consumes_the_actual_exposed_position()
    {
        var d = Cases.Value[4]; var f = d.Unit.Template.Frame;
        foreach (var g in d.Generations)
        {
            var basis = (Relation)g.Left; var port = Attachment.ReadPort(f, (Relation)basis.Right);
            var detail = (Relation)g.Right; var receipts = E((Relation)detail.Left).Cast<Relation>().ToArray();
            var curve = Band.ReadEnvelope(f, (Relation)detail.Right).Sweep.Spine; var controls = E(curve.ControlWords);
            Assert.Equal(controls.Length, receipts.Length);
            foreach (var (receipt, i) in receipts.Select((receipt, i) => (receipt, i)))
            {
                var inputs = (Relation)((Relation)receipt.Left).Right; Assert.Same(port.Position, inputs.Left);
                Assert.True(f.Motion.TryReadResult(receipt, out var word, out _, out _)); Assert.Same(word, controls[i]);
            }
        }
    }

    [Fact]
    public void Whole_strands_and_crossing_order_survive_an_ornamental_attachment()
    {
        var d = Cases.Value[0]; Assert.Equal(10, d.Mesh.Template.Envelopes.Count);
        Assert.Equal(25, d.Unit.Crossings.Count);
        Assert.All(d.Attachments, a => Assert.True(a.Port.Interior));
        Assert.All(d.Unit.Crossings, c => Assert.True(Band.ReadOrder(d.Unit.Template.Frame, c.Surface).Complete));
        Assert.Contains(d.Attachments, a => a.Incident == true && (a.Forward == true || a.Opposed == true));
    }
}
