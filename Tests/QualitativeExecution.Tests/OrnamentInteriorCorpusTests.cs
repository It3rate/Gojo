using Gojo.Core;
using Gojo.Patterns;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;
using Corpus = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentInteriorCorpus;
using Interior = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentInteriorFrame;
using Attachment = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentMotifAttachment;

namespace QualitativeExecution.Tests;

public sealed class OrnamentInteriorCorpusTests
{
    private static readonly Lazy<Corpus.Design> Weave = new(() => Assert.Single(Corpus.Cases(1)));
    private static readonly Lazy<Corpus.Design> ObliqueCells = new(() => Assert.Single(Corpus.Cases(1, "F012")));
    private static readonly Lazy<Corpus.Design> ScrollPanels = new(() => Assert.Single(Corpus.Cases(1, "F008")));

    [Fact]
    public void New_compartments_preserve_exact_mesh_participants_and_whole_strand_crossing_orders()
    {
        foreach (var design in new[] { Weave.Value, ObliqueCells.Value })
        {
            Assert.Same(design.Previous.Mesh, design.Mesh); Assert.Same(design.Previous.Surface, design.Surface.Left);
            Assert.All(design.Mesh.Template.Envelopes, original => Assert.Contains(design.Unit.Template.Envelopes, e => ReferenceEquals(e, original)));
            Assert.All(design.Previous.Unit.Template.Envelopes.Where(e => !design.Mesh.Template.Envelopes.Any(m => ReferenceEquals(m, e))),
                oldDecoration => Assert.DoesNotContain(design.Unit.Template.Envelopes, e => ReferenceEquals(e, oldDecoration)));
            Assert.Equal(design.Previous.Mesh.Crossings, design.Unit.Crossings);
            Assert.All(design.Unit.Crossings, crossing => Assert.True(Band.ReadOrder(design.Unit.Template.Frame, crossing.Surface).Complete));
        }
        Assert.Equal(25, Weave.Value.Unit.Crossings.Count);
        Assert.All(Weave.Value.Mesh.Template.Envelopes, envelope =>
            Assert.Equal(2, Band.ReadEnvelope(Weave.Value.Unit.Template.Frame, envelope).Sweep.Spine.Controls.Length));
    }

    [Fact]
    public void Direct_centre_placements_consume_the_actual_native_partition_output()
    {
        var design = Weave.Value;
        Assert.All(design.Placements, placement => {
            var interior = Interior.ReadFrame(design.Unit.Template.Frame, placement.Interior.Surface);
            Assert.True(interior.CentreComplete); Assert.Same(placement.Interior.Centre, interior.Centre);
            Assert.Same(interior.Centre, placement.Anchor); Assert.Same(interior.Surface, placement.AnchorRead);
            Assert.Same(placement.Anchor, placement.Application.Translation); Assert.Same(placement.Anchor, placement.Guides.Translation);
            AssertActualTranslation(placement.Application, interior.Centre); AssertActualTranslation(placement.Guides, interior.Centre);
        });
    }

    [Fact]
    public void Corner_relative_placements_consume_the_witnessed_sum_of_the_actual_selected_corner()
    {
        var design = ObliqueCells.Value; var frame = design.Unit.Template.Frame;
        Assert.All(design.Placements, placement => {
            var basis = Assert.IsType<Relation>(placement.AnchorRead.Left); Assert.Same(placement.Interior.Surface, basis.Left);
            Assert.Contains(placement.Interior.Corners, corner => ReferenceEquals(corner, basis.Right));
            var receipt = Assert.IsType<Relation>(placement.AnchorRead.Right);
            var request = Assert.IsType<Relation>(receipt.Left); var operands = Assert.IsType<Relation>(request.Right);
            Assert.Same(basis.Right, operands.Left);
            Assert.True(frame.Motion.TryReadResult(receipt, out var actualOutput, out _, out _));
            Assert.Same(actualOutput, placement.Anchor);
            AssertActualTranslation(placement.Application, actualOutput); AssertActualTranslation(placement.Guides, actualOutput);
        });
    }

    [Fact]
    public void Siblings_share_an_actual_compound_seed_but_retain_distinct_placed_instances()
    {
        foreach (var design in new[] { Weave.Value, ObliqueCells.Value })
        {
            Assert.True(design.Placements.Count > 1); var first = design.Placements[0];
            Assert.True(first.Application.Source.Envelopes.Count >= 3);
            Assert.All(design.Placements, placement => {
                Assert.Same(first.Application.Source.Surface, placement.Application.Source.Surface);
                var nativeRequest = Assert.IsType<Relation>(placement.Application.Surface.Left);
                Assert.Same(first.Application.Source.Surface, nativeRequest.Left);
                Assert.Equal(first.Application.Source.Envelopes.Count, placement.Application.Output.Envelopes.Count);
                Assert.True(placement.Application.Complete);
            });
            var second = design.Placements[1];
            Assert.DoesNotContain(second.Application.Output.Envelopes,
                envelope => first.Application.Output.Envelopes.Any(e => ReferenceEquals(e, envelope)));
        }
    }

    [Fact]
    public void Construction_guides_are_native_applications_connected_to_the_same_anchor_and_excluded_from_ornament_output()
    {
        foreach (var design in new[] { Weave.Value, ObliqueCells.Value })
        {
            Assert.NotEmpty(design.Guides.Envelopes);
            var guideSource = design.Placements[0].Guides.Source.Surface;
            Assert.All(design.Placements, placement => {
                Assert.True(placement.Guides.Complete); Assert.Same(guideSource, placement.Guides.Source.Surface);
                var nativeBody = Assert.IsType<Relation>(placement.Surface.Right); var applications = Assert.IsType<Relation>(nativeBody.Right);
                var guideAndBranch = Assert.IsType<Relation>(applications.Right);
                Assert.Same(placement.Application.Surface, applications.Left); Assert.Same(placement.Guides.Surface, guideAndBranch.Left);
                Assert.All(placement.Guides.Output.Envelopes, guide => {
                    Assert.Contains(design.Guides.Envelopes, e => ReferenceEquals(e, guide));
                    Assert.DoesNotContain(design.Unit.Template.Envelopes, e => ReferenceEquals(e, guide));
                });
            });
        }
    }

    [Fact]
    public void Boundary_branches_consume_actual_mesh_positions_and_tangents_with_a_shared_smooth_root()
    {
        var design = ScrollPanels.Value; var frame = design.Unit.Template.Frame;
        Assert.All(design.Placements, placement => {
            Assert.NotNull(placement.BoundaryBranch);
            Assert.Contains(design.Unit.Template.Envelopes, e => ReferenceEquals(e, placement.BoundaryBranch));
            var seedAndApplications = Assert.IsType<Relation>(placement.Surface.Right);
            var applications = Assert.IsType<Relation>(seedAndApplications.Right);
            var guideAndBranch = Assert.IsType<Relation>(applications.Right);
            var branch = Assert.IsType<Relation>(guideAndBranch.Right);
            var focus = Assert.IsType<Relation>(branch.Left);
            Assert.Same(placement.Interior.Surface, focus.Left); Assert.Same(placement.Anchor, focus.Right);
            var portAndReceipts = Assert.IsType<Relation>(branch.Right);
            var receiptsAndAttachment = Assert.IsType<Relation>(portAndReceipts.Right);
            var attachment = Attachment.ReadAttachment(frame, Assert.IsType<Relation>(receiptsAndAttachment.Right));
            Assert.True(attachment.Incident); Assert.True(attachment.Smooth); Assert.True(attachment.SharedSite);
            Assert.Same(placement.BoundaryBranch, attachment.Motif);
            Assert.Same(placement.Interior.Sections[0], attachment.Port.Section);
            Assert.Contains(design.Mesh.Template.Envelopes, e => ReferenceEquals(e, attachment.Port.Envelope));
            Assert.Same(attachment.Port.Surface, portAndReceipts.Left);
            Assert.Same(attachment.Port.Position, attachment.Root.Position);
            var firstReceipt = Assert.IsType<Relation>(Entries(Assert.IsType<Relation>(receiptsAndAttachment.Left))[0]);
            var operands = Assert.IsType<Relation>(((Relation)firstReceipt.Left).Right);
            Assert.Same(attachment.Port.Position, operands.Left); Assert.Same(attachment.Port.Tangent, operands.Right);
            Assert.True(frame.Motion.TryReadResult(firstReceipt, out var firstControl, out _, out _));
            var source = Band.ReadEnvelope(frame, placement.BoundaryBranch!).Sweep.Spine;
            Assert.Same(firstControl, Entries(source.ControlWords)[1]);
        });
    }

    [Fact]
    public void Filled_seed_petals_consume_actual_construction_guide_sections_as_their_spines()
    {
        foreach (var design in new[] { Weave.Value, ObliqueCells.Value })
        {
            var placement = design.Placements[0]; var frame = design.Unit.Template.Frame;
            var guideSections = placement.Guides.Source.Envelopes.Select(e => Band.ReadEnvelope(frame, e).Sweep.Spine.Section).ToArray();
            var petals = placement.Application.Source.Envelopes.Select(e => Band.ReadEnvelope(frame, e).Sweep)
                .Where(sweep => sweep.Collapsed == false).ToArray();
            Assert.True(petals.Length >= 3);
            Assert.All(petals, petal => Assert.Contains(guideSections, section => ReferenceEquals(section, petal.Spine.Section)));
        }
    }

    [Fact]
    public void Authored_scaffolds_and_actual_closed_cell_cycles_remain_distinct_native_sources()
    {
        var weave = Weave.Value; var cells = ObliqueCells.Value;
        Assert.All(weave.Placements, placement => {
            var read = Interior.ReadFrame(weave.Unit.Template.Frame, placement.Interior.Surface);
            Assert.True(read.Complete); Assert.True(read.Scaffold); Assert.False(read.ClosedBySite);
        });
        Assert.Equal(cells.Previous.Source.Cells.Count, cells.Placements.Count);
        for (var i = 0; i < cells.Placements.Count; i++)
        {
            var placement = cells.Placements[i]; var read = Interior.ReadFrame(cells.Unit.Template.Frame, placement.Interior.Surface);
            Assert.True(read.ClosedBySite); Assert.False(read.Scaffold);
            var source = Assert.IsType<Relation>(((Relation)placement.Interior.Surface.Left).Right);
            Assert.Same(cells.Previous.Source.Cells[i], Assert.Single(Entries(source)));
            Assert.Equal(Entries(cells.Previous.Source.Cells[i]), read.Sections.Cast<IElement>().ToArray());
        }
    }

    private static void AssertActualTranslation(Parts.Application application, Relation actual)
    {
        Assert.Same(actual, application.Translation);
        var request = Assert.IsType<Relation>(application.Surface.Left);
        var instruction = Assert.IsType<Relation>(request.Right); Assert.Same(actual, instruction.Right);
    }
    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail)); Assert.True(seam.Equals(tail)); return entries;
    }
}
