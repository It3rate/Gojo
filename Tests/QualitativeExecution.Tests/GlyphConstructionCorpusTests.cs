using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Corpus = Gojo.Experiments.QualitativeFrieze.Composition.GlyphConstructionCorpus;

namespace QualitativeExecution.Tests;

public sealed class GlyphConstructionCorpusTests
{
    private static readonly Lazy<IReadOnlyList<Corpus.Fixture>> CorpusData = new(Corpus.Create);
    private static Corpus.Fixture Get(string glyph,string variant="baseline") => CorpusData.Value.Single(f=>f.Glyph==glyph && f.VariantId==variant);

    [Fact]
    public void Every_capital_and_digit_has_a_complete_native_construction()
    {
        var baselines = CorpusData.Value.Where(f=>f.VariantId=="baseline").ToArray();
        Assert.Equal(36,baselines.Length);
        Assert.Equal("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ",string.Concat(baselines.Select(f=>f.Glyph).Order()));
        Assert.All(CorpusData.Value,fixture=>
        {
            Assert.True(fixture.Topology.Complete,$"{fixture.Glyph}/{fixture.VariantId}");
            Assert.All(fixture.Strokes,s=>Assert.True(s.Continuous));
            Assert.Equal(fixture.Lines.Length*2,fixture.Sites.Sum(s=>s.Degree));
            Assert.All(fixture.Lines,l=>Assert.InRange(l.Controls.Length,2,4));
            Assert.True(fixture.ConstructionRead.Complete,$"history {fixture.Glyph}/{fixture.VariantId}");
        });
        Assert.All(baselines,fixture=>Assert.StartsWith("https://",fixture.SourceUrl));
    }

    [Fact]
    public void Taught_first_stems_descend_and_returning_arms_do_not_reverse_the_stem()
    {
        foreach(var glyph in new[]{"B","D","E","F","H","I","K","M","N","P","R","1"})
        {
            var fixture=Get(glyph); var first=fixture.ConstructionRead.Strokes[0];
            var start=fixture.Sites[first.StartSite]; var end=fixture.Sites[first.EndSite];
            Assert.True(start.Y>end.Y,$"{glyph} first stem must descend");
            Assert.Equal(start.X,end.X);
        }
        foreach(var glyph in new[]{"B","D","E","F","P","R"})
        {
            var read=Get(glyph).ConstructionRead;
            Assert.Equal(read.Strokes[0].StartSite,read.Strokes[1].StartSite);
            Assert.Equal("return-endpoint",read.Strokes[1].StartKind);
        }
    }

    [Fact]
    public void F_endpoint_attachment_and_later_outward_branch_have_different_native_history()
    {
        var f=Get("F");
        Assert.Contains(f.ConstructionRead.Strokes[1].Events,e=>e.Kind=="endpoint-attachment");
        Assert.Contains(f.ConstructionRead.Strokes[2].Events,e=>e.Kind=="outward-branch");
        var reversed=Get("F","bars-incoming");
        Assert.Same(f.Sections,reversed.Sections);
        Assert.Equal("stop-at-endpoint",reversed.ConstructionRead.Strokes[1].EndKind);
        Assert.Equal("stop-at-interior",reversed.ConstructionRead.Strokes[2].EndKind);
        Assert.DoesNotContain(reversed.ConstructionRead.Strokes[2].Events,e=>e.Kind=="outward-branch");
    }

    [Fact]
    public void H_places_a_parallel_stem_before_connecting_two_existing_landmarks()
    {
        var h=Get("H"); var read=h.ConstructionRead;
        Assert.Equal("offset-start",read.Strokes[1].StartKind);
        Assert.Equal(0,read.Strokes[1].PlacementSourceStroke);
        Assert.Equal(read.Strokes[0].StartSite,read.Strokes[1].PlacementSourceSite);
        Assert.True(read.Strokes[1].ParallelToSource);
        Assert.Equal("return-interior",read.Strokes[2].StartKind);
        Assert.Equal("stop-at-interior",read.Strokes[2].EndKind);
        Assert.Contains(read.Strokes[2].Events,e=>e.Kind=="landmark-to-landmark-join");
        Assert.Equal(new[]{0},read.Strokes[2].StartSourceStrokes);
        Assert.Equal(new[]{1},read.Strokes[2].EndSourceStrokes);
    }

    [Fact]
    public void A_and_Y_start_from_the_taught_ends_and_make_distinct_landmark_returns()
    {
        var a=Get("A"); Assert.Equal(3,a.Strokes.Length);
        var ar=a.ConstructionRead.Strokes;
        Assert.Equal(ar[0].StartSite,ar[1].StartSite);
        Assert.True(a.Sites[ar[0].StartSite].Y>a.Sites[ar[0].EndSite].Y);
        Assert.True(a.Sites[ar[1].StartSite].Y>a.Sites[ar[1].EndSite].Y);
        var y=Get("Y"); Assert.Single(y.Strokes[0].Visits);
        Assert.Equal(2,y.Strokes[1].Visits.Length);
        Assert.Contains(y.ConstructionRead.Strokes[1].Events,e=>e.Kind=="meet-landmark-and-continue");
        Assert.Equal(0,y.Sites[y.ConstructionRead.Strokes[1].EndSite].Y);
    }

    [Fact]
    public void X_descends_twice_and_round_letters_start_counterclockwise()
    {
        var x=Get("X");
        Assert.All(x.ConstructionRead.Strokes,stroke=>Assert.True(x.Sites[stroke.StartSite].Y>x.Sites[stroke.EndSite].Y));
        var six=Get("6").Lines[0].Controls;
        Assert.True(six[1].X<six[0].X && six[1].Y<six[0].Y,"6 starts down-left rather than curling upwards first");
        foreach(var glyph in new[]{"O","Q","0"})
        {
            var f=Get(glyph); var visit=f.Strokes[0].Visits[0]; var edge=f.Lines[visit.Section];
            var controls=visit.Reverse?edge.Controls.Reverse().ToArray():edge.Controls;
            Assert.True(controls[1].X<controls[0].X,$"{glyph} must begin towards the left from the top");
            Assert.True(f.Strokes[0].Closed);
        }
    }

    [Fact]
    public void Eight_starts_at_top_and_crosses_the_waist_smoothly_instead_of_turning_back()
    {
        var eight=Get("8"); var stroke=Assert.Single(eight.Strokes);
        var first=eight.Lines[stroke.Visits[0].Section];
        Assert.Equal(10,first.Controls[0].Y);
        Assert.Single(eight.Sites,s=>s.Joint=="crossing-like");
        Assert.Equal(2,eight.Topology.CycleRank);
        Assert.True(stroke.Closed);
        Assert.DoesNotContain(eight.ConstructionRead.Strokes[0].Events,e=>e.Kind=="continuous-turn");
        Assert.Equal(2,Get("8","two-loops").Strokes.Length);
        Assert.All(Get("8","two-loops").Strokes,s=>Assert.True(s.Closed));
    }

    [Fact]
    public void Continuous_L_and_attached_L_share_ink_and_sites_but_not_pen_history()
    {
        var bend=Get("L"); var attached=Get("L","attached");
        Assert.Same(bend.Sections,attached.Sections);
        Assert.NotSame(bend.History,attached.History);
        Assert.Single(bend.Strokes); Assert.Equal(2,attached.Strokes.Length);
        Assert.Equal(bend.Topology.Components,attached.Topology.Components);
        Assert.Equal(bend.Topology.FreeEnds,attached.Topology.FreeEnds);
        Assert.Equal(1,attached.Topology.Components);
    }

    [Fact]
    public void Two_corner_T_reuses_one_actual_stem_in_opposed_traversal()
    {
        var single=Get("T"); var corners=Get("T","two-corners");
        Assert.Same(single.Sections,corners.Sections);
        Assert.Equal(3,corners.Lines.Length); Assert.Equal(4,corners.Strokes.Sum(s=>s.Visits.Length));
        Assert.Equal(1,corners.Topology.RevisitedSections);
        Assert.False(corners.Strokes[0].Visits[1].Reverse);
        Assert.True(corners.Strokes[1].Visits[0].Reverse);
        Assert.Equal(corners.Strokes[0].Visits[1].Section,corners.Strokes[1].Visits[0].Section);
        Assert.Equal(1,corners.Topology.Branches); Assert.Equal(0,corners.Topology.CycleRank);
    }

    [Fact]
    public void Coincident_crossing_is_not_the_same_as_a_shared_four_arm_junction()
    {
        var crossing=Get("X"); var joined=Get("X","joined");
        Assert.Equal(2,crossing.Topology.Components); Assert.Equal(1,joined.Topology.Components);
        Assert.Equal(0,crossing.Topology.Branches); Assert.Equal(1,joined.Topology.Branches);
        Assert.Equal(2,crossing.Sites.Count(s=>s.X==4 && s.Y==5));
        Assert.Single(joined.Sites.Where(s=>s.X==4 && s.Y==5));
        Assert.Equal(crossing.Lines.SelectMany(l=>l.Controls),joined.Lines.SelectMany(l=>l.Controls));
    }

    [Theory]
    [InlineData("A",1)] [InlineData("B",2)] [InlineData("C",0)] [InlineData("D",1)]
    [InlineData("H",0)] [InlineData("O",1)] [InlineData("P",1)] [InlineData("6",1)]
    [InlineData("8",2)] [InlineData("9",1)]
    public void Selected_cycle_rank_comes_from_actual_incidence(string glyph,int rank)
    { Assert.Equal(rank,Get(glyph).Topology.CycleRank); }

    [Fact]
    public void Closed_top_four_changes_topology_while_reversing_H_bridge_changes_only_history()
    {
        Assert.Equal(0,Get("4").Topology.CycleRank); Assert.Equal(1,Get("4","closed-top").Topology.CycleRank);
        var h=Get("H"); var reversed=Get("H","bridge-reversed");
        Assert.Same(h.Sections,reversed.Sections); Assert.True(reversed.Strokes[^1].Visits.Single().Reverse);
        Assert.Equal(h.Topology,reversed.Topology);
    }

    [Fact]
    public void Renaming_a_glyph_cannot_change_any_native_read()
    {
        var original=Get("F");
        var renamed=Corpus.Read(original.Surface,"Bridge","renamed","not a letter","Any caption","Any note");
        Assert.Equal(original.Topology,renamed.Topology);
        Assert.Equal(original.Lines.SelectMany(l=>l.Controls),renamed.Lines.SelectMany(l=>l.Controls));
        Assert.Equal(original.Strokes.SelectMany(s=>s.Visits),renamed.Strokes.SelectMany(s=>s.Visits));
        Assert.Same(original.Surface,renamed.Surface);
    }

    [Fact]
    public void Roundtrip_preserves_repeated_section_aliases_and_opposed_visits()
    {
        var original=Get("T","two-corners");
        var decoded=Assert.IsType<Relation>(GojoNumericFormat.Decode(GojoNumericFormat.Encode(original.Surface))[0]);
        var copy=Corpus.Read(decoded);
        Assert.Equal(original.Topology,copy.Topology);
        var strokes=E(copy.History).Cast<Relation>().ToArray();
        var down=Assert.IsType<Relation>(E(strokes[0])[1]); var up=Assert.IsType<Relation>(E(strokes[1])[0]);
        Assert.Same(down.Left,up.Left);
        Assert.True(copy.Strokes[1].Visits[0].Reverse);
    }

    [Fact]
    public void Curves_keep_interpolation_structure_and_are_not_replaced_by_their_chords()
    {
        foreach(var glyph in new[]{"B","C","D","G","J","O","P","Q","R","S","U","0","2","3","5","6","8","9"})
        {
            var fixture=Get(glyph);
            Assert.Contains(fixture.Lines,l=>l.Controls.Length>2 && l.Straight==false);
            var copy=Corpus.Read((Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(fixture.Surface))[0]);
            Assert.True(copy.Topology.Complete);
            Assert.Equal(fixture.Lines.Select(l=>l.Controls.Length),copy.Lines.Select(l=>l.Controls.Length));
        }
    }

    [Fact]
    public void Joint_reads_distinguish_tangent_continuation_bends_and_shared_branches()
    {
        Assert.All(Get("O").Sites,s=>Assert.Equal("tangent-continuation",s.Joint));
        Assert.Single(Get("L").Sites,s=>s.Joint=="bend");
        Assert.Single(Get("F").Sites,s=>s.Joint=="T-like");
        Assert.Equal(2,Get("H").Sites.Count(s=>s.Joint=="T-like"));
        Assert.Single(Get("Y").Sites,s=>s.Joint=="three-arm");
        Assert.Single(Get("X","joined").Sites,s=>s.Joint=="crossing-like");
        Assert.Equal(2,Get("X").Sites.Count(s=>s.Joint=="tangent-continuation"));
        Assert.All(Get("O").Sites,s=>Assert.True(s.DirectionComplete));
        var restored=Corpus.Read((Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(Get("H").Surface))[0]);
        Assert.Equal(Get("H").Sites,restored.Sites);
        Assert.Equal(restored.Sites.Length,E(restored.JointWitnesses).Length);
    }

    [Fact]
    public void Open_section_inventory_keeps_known_rays_without_certifying_joint_completeness()
    {
        var source=Get("H");
        var opened=Corpus.Read(new(new Relation(source.Frame.Surface,StructuralConstraint.Open(E(source.Sections))),source.History));
        Assert.False(opened.Topology.Complete);
        Assert.Equal(source.Lines.Length,opened.Lines.Length);
        Assert.Equal(source.Sites.Select(s=>s.Degree),opened.Sites.Select(s=>s.Degree));
        Assert.All(opened.Sites,s=> { Assert.Equal("unresolved",s.Joint); Assert.False(s.DirectionComplete); });
    }

    [Fact]
    public void A_pen_cannot_continue_at_an_equal_coordinate_with_a_different_site_identity()
    {
        var f=new Curve.Frame(new QualitativeMotionFrame()); var a=new Placeholder(); var j=new Placeholder(); var k=new Placeholder(); var b=new Placeholder();
        var first=f.Line(a,j,f.Word(0,1),f.Word(0,0)); var second=f.Line(k,b,f.Word(0,0),f.Word(1,0));
        var fixture=Corpus.Read(new(new Relation(f.Surface,S(first,second)),S(S(Corpus.Visit(f,first),Corpus.Visit(f,second)))));
        Assert.False(fixture.Topology.Complete); Assert.False(fixture.Strokes.Single().Continuous);
        Assert.Equal(2,fixture.Topology.Components);
    }

    [Fact]
    public void Shared_site_with_conflicting_positions_does_not_certify_a_complete_glyph()
    {
        var f=new Curve.Frame(new QualitativeMotionFrame()); var a=new Placeholder(); var j=new Placeholder(); var b=new Placeholder();
        var first=f.Line(a,j,f.Word(0,1),f.Word(0,0)); var second=f.Line(j,b,f.Word(1,0),f.Word(2,0));
        var fixture=Corpus.Read(new(new Relation(f.Surface,S(first,second)),S(S(Corpus.Visit(f,first),Corpus.Visit(f,second)))));
        Assert.False(fixture.Topology.Complete);
        Assert.Contains(fixture.Sites,s=>s.Joint=="unresolved" && !s.DirectionComplete);
    }

    [Fact]
    public void Foreign_section_and_open_history_remain_incomplete()
    {
        var source=Get("L"); var foreign=Get("H");
        var badHistory=S(S(Corpus.Visit(foreign.Frame,(Relation)E(foreign.Sections)[0])));
        var bad=Corpus.Read(new(new Relation(source.Frame.Surface,source.Sections),badHistory));
        Assert.False(bad.Topology.Complete);
        var open=Corpus.Read(new(new Relation(source.Frame.Surface,source.Sections),StructuralConstraint.Open(E(source.History))));
        Assert.False(open.Topology.Complete);
    }

    [Fact]
    public void Reversing_only_endpoint_claim_without_reversing_control_references_is_not_a_valid_visit()
    {
        var source=Get("L"); var section=(Relation)E(source.Sections)[0]; var read=source.Frame.Read(section);
        var falseReverse=new Relation(section,new Relation(new Relation(read.End,read.Start),read.ControlWords));
        var invalid=Corpus.Read(new(new Relation(source.Frame.Surface,source.Sections),S(S(falseReverse))));
        Assert.False(invalid.Topology.Complete);
    }

    private static Relation S(params IElement[] items)=>StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope)=>FiniteLaw.Entries(scope);
}
