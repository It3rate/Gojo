using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveLearning.Tests;

public sealed class ProgramLearnerTests
{
    [Fact]
    public void NativeBranchEditsLearnACompositionNeitherInputCanSolveAlone()
    {
        var learner = Create(); Train(learner);
        Assert.Equal(16, learner.CandidateCount); Assert.True(learner.AcceptedEdits > 0); Assert.Equal(8, learner.SelectedComplexity);
        for (var mode = 0; mode < 2; mode++)
        foreach (var direction in new[] { -1, 1 })
            Assert.Equal(PhysicalTarget(mode,direction), Value(learner.Act(P(learner.ModeAxis,mode), P(learner.DirectionAxis,direction),false),learner.ActionAxis));
        Assert.True(learner.Root.TryReadOrderedGroup(out var fields));
        Assert.True(((Relation)fields[10]).TryReadOrderedGroup(out var candidates));
        Assert.Equal(16,candidates.Length);
        for (var index=1;index<candidates.Length;index++)
        {
            Assert.True(((Relation)candidates[index]).TryReadOrderedGroup(out var candidate));
            Assert.True(ReadWitness.TryRead((Relation)candidate[4],out var parent,out var editSurface,out var changed,out _,out _));
            Assert.Contains(parent,candidates.Take(index)); Assert.IsType<Relation>(editSurface); Assert.Same(candidate[1],changed);
        }
    }

    [Fact]
    public void InitialSinglePortProgramsCannotSolveTheBalancedInteraction()
    {
        var learner=Create(); learner.Root.TryReadOrderedGroup(out var root); ((Relation)root[10]).TryReadOrderedGroup(out var candidates);
        for(var index=0;index<6;index++)
        {
            ((Relation)candidates[index]).TryReadOrderedGroup(out var candidate); var correct=0;
            for(var mode=0;mode<2;mode++) foreach(var direction in new[]{-1,1})
            {
                var read=SupportDecisionSurface.Select(new Carrier(),(Relation)candidate[1],
                    SupportDecisionSurface.Binding(new Carrier(),root[8],P(learner.ModeAxis,mode)),
                    SupportDecisionSurface.Binding(new Carrier(),root[9],P(learner.DirectionAxis,direction)));
                Assert.True(SupportDecisionSurface.TryReadSelection(read,out _,out var response,out _,out _));
                if(Assert.IsType<CarrierLocation>(response).Ticks==PhysicalTarget(mode,direction)) correct++;
            }
            Assert.Equal(2,correct);
        }
    }

    [Fact]
    public void FrozenRunCannotUpdateUtilityCoverageOrRetainedEdits()
    {
        var learner=Create(); Train(learner); var counts=Counts(learner); var edits=learner.AcceptedEdits;
        learner.Act(P(learner.ModeAxis,0),P(learner.DirectionAxis,1),false);
        AssertOpen(learner.Feedback(learner.BindUtility(P(learner.UtilityAxis,900))));
        Assert.Equal(counts,Counts(learner)); Assert.Equal(edits,learner.AcceptedEdits);
        learner.Act(P(learner.ModeAxis,1),P(learner.DirectionAxis,1),true);
        AssertOpen(learner.Feedback(learner.BindUtility(P(learner.UtilityAxis,900)),false)); Assert.Equal(counts,Counts(learner));
    }

    [Fact]
    public void MissingOrMiswiredPortsAndUnrelatedFeedbackStayOpen()
    {
        var learner=Create();
        AssertOpen(learner.Act(P(new Carrier(),0),P(learner.DirectionAxis,1),true));
        var missing=ReadWitness.Create(new Carrier(),new Placeholder(),learner.DirectionAxis,new Placeholder(),new Placeholder(),new Placeholder());
        AssertOpen(learner.Act(P(learner.ModeAxis,0),missing,true));
        learner.Act(P(learner.ModeAxis,0),P(learner.DirectionAxis,1),true);
        AssertOpen(learner.Feedback(learner.BindUtility(P(new Carrier(),4)))); Assert.All(Counts(learner),count=>Assert.Equal(0,count));
    }

    [Fact]
    public void DuplicateAndAliasedOutcomeDoNotMultiplyUtilityEvidence()
    {
        var learner=Create(); var source=new Relation(new Placeholder(),new Placeholder());
        learner.Act(P(learner.ModeAxis,0),P(learner.DirectionAxis,1),true);
        var utility=learner.BindUtility(Publication(learner.UtilityAxis,4,source)); learner.Feedback(utility);
        AssertOpen(learner.Feedback(utility));
        learner.Act(P(learner.ModeAxis,0),P(learner.DirectionAxis,1),true);
        AssertOpen(learner.Feedback(learner.BindUtility(Publication(learner.UtilityAxis,4,source))));
        Assert.Equal(1,Counts(learner).Sum());
    }

    [Fact]
    public void ActualRuleAndCandidateRemainBoundToTheFeedbackWitness()
    {
        var learner=Create(); var action=learner.Act(P(learner.ModeAxis,0),P(learner.DirectionAxis,1),true);
        Assert.True(ReadWitness.TryRead(action,out _,out _,out _,out _,out var provenance));
        Assert.True(((Relation)provenance).TryReadOrderedGroup(out var fields));
        Assert.True(SupportDecisionSurface.TryReadSelection((Relation)fields[2],out var rule,out _,out _,out _)); Assert.IsType<Relation>(rule);
        var feedback=learner.Feedback(learner.BindUtility(P(learner.UtilityAxis,4)));
        Assert.True(ReadWitness.TryRead(feedback,out _,out var decision,out _,out _,out var feedbackProvenance)); Assert.Same(action,decision);
        Assert.True(((Relation)feedbackProvenance).TryReadOrderedGroup(out var update)); Assert.Same(fields[0],update[2]);
    }

    [Fact]
    public void NativeArchiveRediscoveryRetainsTheLearnedComposition()
    {
        var learner=Create(); Train(learner);
        var restored=ProgramLearner.FromRoot(Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(learner.Root)))));
        Assert.Equal(Counts(learner),Counts(restored)); Assert.Equal(learner.AcceptedEdits,restored.AcceptedEdits);
        for(var mode=0;mode<2;mode++) foreach(var direction in new[]{-1,1})
            Assert.Equal(Value(learner.Act(P(learner.ModeAxis,mode),P(learner.DirectionAxis,direction),false),learner.ActionAxis),
                Value(restored.Act(P(restored.ModeAxis,mode),P(restored.DirectionAxis,direction),false),restored.ActionAxis));
    }

    private static ProgramLearner Create()=>new(new Carrier(),new Carrier(),new Carrier(),new Carrier());
    private static void Train(ProgramLearner learner)
    {
        for(var tick=0;tick<320;tick++)
        {
            var mode=tick%4/2; var direction=tick%2==0?-1:1;
            var action=Value(learner.Act(P(learner.ModeAxis,mode),P(learner.DirectionAxis,direction),true),learner.ActionAxis);
            // This physical fixture alone supplies the target and consequence.
            learner.Feedback(learner.BindUtility(P(learner.UtilityAxis,action==PhysicalTarget(mode,direction)?4:-4)));
        }
    }

    [Fact]
    public void SameAxisOrStaleReceiptCannotCreditANewProgramTrial()
    {
        var learner=Create(); learner.Act(P(learner.ModeAxis,0),P(learner.DirectionAxis,1),true);
        AssertOpen(learner.Feedback(P(learner.UtilityAxis,4)));
        var stale=learner.BindUtility(P(learner.UtilityAxis,4));
        learner.Act(P(learner.ModeAxis,0),P(learner.DirectionAxis,1),true);
        AssertOpen(learner.Feedback(stale)); Assert.All(Counts(learner),count=>Assert.Equal(0,count));
    }
    private static int PhysicalTarget(int mode,int direction)=>mode==1?direction:-direction;
    private static long[] Counts(ProgramLearner learner)=>Enumerable.Range(0,learner.CandidateCount).Select(learner.ObservationCount).ToArray();
    private static Relation P(Carrier axis,long value)=>Publication(axis,value,Relation.Point(axis,value));
    private static Relation Publication(Carrier axis,long value,IElement source)=>ReadWitness.Create(new Carrier(),source,
        Relation.Segment(axis,value,value),new CarrierLocation(axis,value),Relation.OrderedGroup(new Carrier()),source);
    private static long Value(Relation publication,Carrier axis)
    {
        Assert.True(ReadWitness.TryRead(publication,out _,out _,out var result,out _,out _));
        var point=Assert.IsType<CarrierLocation>(result); Assert.Same(axis,point.Carrier); return point.Ticks;
    }
    private static void AssertOpen(Relation publication)
    {
        Assert.True(ReadWitness.TryRead(publication,out _,out _,out var result,out var residue,out _));
        Assert.IsType<Placeholder>(result); Assert.IsType<Relation>(residue);
    }
}
