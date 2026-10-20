using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeExecution;

namespace QualitativeExecution.Tests;

public sealed class EquivalenceRevisionTests
{
    private static readonly Lazy<EquivalenceRevisionStudy> Study = new(EquivalenceRevisionStudy.Build);

    [Fact]
    public void Revealed_unequal_participation_revises_only_the_shared_span_default()
    {
        var study = Study.Value; var stages = study.Cases[0].Stages;
        Assert.Equal("square", stages[0].Output!.Id);
        Assert.Equal("rectangle", stages[1].Output!.Id);
        Assert.Equal("rectangle", stages[2].Output!.Id);
        Assert.True(stages[0].Output.CoarseClosed); Assert.True(stages[1].Output.CoarseClosed);
        Assert.True(stages[0].CoarseAssessment!.IsSatisfied); Assert.True(stages[1].CoarseAssessment!.IsSatisfied);
        Assert.Equal(1, stages[0].Output.CycleCount); Assert.Equal(1, stages[1].Output.CycleCount);
        Assert.Single(E(stages[1].Withdrawn)); Assert.Empty(E(stages[1].Carried));
        Assert.Single(E(stages[0].Selection.Chosen!.Equalities));
        Assert.Empty(E(stages[1].Selection.Chosen!.Equalities));
        Assert.Contains(stages[1].Selection.Candidates,
            item => ReferenceEquals(item.Hypothesis, stages[0].Selection.Chosen) && E(item.Refuted).Length > 0);
        Assert.Empty(stages[0].Publications);
    }

    [Fact]
    public void Local_exception_preserves_outside_sharing_and_all_three_closures()
    {
        var stages = Study.Value.Cases[1].Stages;
        Assert.Equal("uniform", stages[0].Output!.Id);
        Assert.Equal("exception", stages[1].Output!.Id);
        Assert.Equal(3, stages[0].Output.CycleCount); Assert.Equal(3, stages[1].Output.CycleCount);
        Assert.True(stages[0].Output.CoarseClosed); Assert.True(stages[1].Output.CoarseClosed);
        Assert.Equal(10, E(stages[1].Carried).Length);
        Assert.Equal(5, E(stages[1].Withdrawn).Length);
        Assert.Equal(15, E(stages[0].Selection.Chosen!.Equalities).Length);
        Assert.Equal(10, E(stages[1].Selection.Chosen!.Equalities).Length);
        var domain = E(stages[0].Selection.Chosen.Domain);
        Assert.All(E(stages[1].Withdrawn).Cast<Relation>(), pair =>
            Assert.True(ReferenceEquals(pair.Left, domain[2]) || ReferenceEquals(pair.Right, domain[2])));
    }

    [Fact]
    public void Conflicting_same_event_reports_block_a_global_verdict_but_allow_explicit_working_contexts()
    {
        var stages = Study.Value.Cases[2].Stages;
        Assert.Null(stages[0].Selection.Chosen); Assert.Null(stages[0].Output);
        Assert.All(stages, stage => Assert.All(stage.Selection.Candidates, item => Assert.True(item.HasConflict)));
        Assert.Equal("square", stages[1].Output!.Id);
        Assert.Equal("rectangle", stages[2].Output!.Id);
        Assert.Same(stages[0].Publications[0], stages[1].Publications[0]);
        Assert.Same(stages[0].Publications[1], stages[2].Publications[1]);
        Assert.Same(stages[0].Publications[0].Revision, stages[0].Publications[1].Revision);
        Assert.All(stages[1].Selection.Eligible, item => Assert.Empty(E(item.InScopeRefuted)));
        Assert.All(stages[2].Selection.Eligible, item => Assert.Empty(E(item.InScopeRefuted)));
        Assert.NotNull(stages[1].Context.WorkingSources);
        Assert.NotNull(stages[2].Context.WorkingSources);
        Assert.NotSame(E(stages[1].Context.WorkingSources!).Single(), E(stages[2].Context.WorkingSources!).Single());
    }

    [Fact]
    public void Capability_limit_preserves_blocked_exact_request_beside_explicit_coarse_success()
    {
        var stages = Study.Value.Cases[3].Stages;
        Assert.Null(stages[0].Output); Assert.Null(stages[0].Selection.Chosen);
        Assert.False(stages[0].LowerResolution);
        Assert.True(stages[1].LowerResolution); Assert.NotNull(stages[1].Output);
        Assert.True(stages[1].Output!.CoarseClosed);
        Assert.Same(stages[0].Selection.Surface, stages[1].BlockedExact);
        Assert.Empty(E(stages[1].Context.WorkingSources!));
        Assert.Same(stages[0].Publications[0], stages[1].Publications[0]);
        Assert.Contains(stages[0].Selection.Candidates,
            item => E(item.Blocked).Length > 0 && !item.IsEligible);
        // The source still says uu/u while the deliberately coarse sample is
        // u/u. A successful low-resolution output is not an exact source match.
        var observed = E(stages[1].Publications[0].Values).Cast<Relation>().First();
        var sample = E(stages[1].Output.SpanValues).Cast<Relation>().Single(row => ReferenceEquals(row.Left, observed.Left));
        Assert.NotEqual(E((Relation)observed.Right).Length, E((Relation)sample.Right).Length);
    }

    [Fact]
    public void Every_output_executes_freshly_and_span_reader_confirms_actual_sample_not_source_truth()
    {
        var outputs = Study.Value.Cases.SelectMany(item => item.Stages).Where(stage => stage.Output is not null).ToArray();
        foreach (var stage in outputs)
        {
            var output = stage.Output!; var chosen = stage.Selection.Chosen!;
            Assert.True(output.Execution.Current.Execution.IsAtBoundary);
            Assert.Single(E(output.Execution.Current.Execution.CompletionEvents));
            Assert.True(output.Execution.Current.Join.TryReadPassed(output.Execution.Current.Join.Evaluate()));
            Assert.True(output.Publisher.IsComplete); Assert.True(output.Critic.IsComplete);
            Assert.Same(output.Publisher.Observation, output.Critic.Observation);
            Assert.NotSame(output.Frame, output.Publisher.Observation.Frame);
            foreach (var binding in E(chosen.Bindings).Cast<Relation>())
            {
                var expected = (Relation)E(chosen.ParameterValues).Cast<Relation>()
                    .Single(row => ReferenceEquals(row.Left, binding.Right)).Right;
                var actual = (Relation)E(output.SpanValues).Cast<Relation>()
                    .Single(row => ReferenceEquals(row.Left, binding.Left)).Right;
                Assert.True(new ProvisionalEquivalence(chosen.Domain, S(Study.Value.Reach)).Compare(expected, actual).IsEqual);
            }
            Assert.DoesNotContain(Reachable(output.Publisher.Observation.Surface),
                element => ReferenceEquals(element, output.Program.Surface) || ReferenceEquals(element, output.Execution.Current.Surface));
        }
        Assert.Equal(outputs.Length, outputs.Select(stage => stage.Output!.Execution).Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void Simpler_shared_parameter_hypothesis_wins_even_when_preference_lists_rectangle_first()
    {
        var study = Study.Value; var square = study.Forms.Single(form => form.Id == "square");
        var rectangle = study.Forms.Single(form => form.Id == "rectangle");
        var engine = new ProvisionalEquivalence(square.Hypothesis.Domain, S(study.Reach));
        var context = new ProvisionalEquivalence.Context(S(rectangle.Program.Surface, square.Program.Surface), S(),
            S(rectangle.Hypothesis.Surface, square.Hypothesis.Surface));
        var result = engine.Assess([rectangle.Hypothesis, square.Hypothesis], [], context);
        Assert.Same(square.Hypothesis, result.Chosen);
        Assert.Equal(1, result.Candidates.Single(item => ReferenceEquals(item.Hypothesis, square.Hypothesis)).ParameterCount);
        Assert.Equal(2, result.Candidates.Single(item => ReferenceEquals(item.Hypothesis, rectangle.Hypothesis)).ParameterCount);
    }

    [Fact]
    public void Span_reader_refuses_a_foreign_equal_looking_stroke()
    {
        var output = Study.Value.Cases[0].Stages[0].Output!;
        var actual = (Relation)E(output.Graph.Strokes)[0];
        Assert.True(QualitativeStrokeGraph.TryReadStroke(actual, out _, out var a, out var b, out var word));
        var foreign = QualitativeStrokeGraph.Stroke(new Placeholder(), a, b, word);
        var read = EquivalenceRevisionStudy.ReadSpans(output.Graph, S(new Relation(new Placeholder(), foreign)), out var values);
        Assert.Empty(E(values));
        Assert.Contains(Reachable(read), element => ReferenceEquals(element, foreign));
    }

    [Fact]
    public void Span_reader_refuses_mixed_axis_chords_instead_of_inventing_a_length()
    {
        var frame = new QualitativeMotionFrame(); IElement u = new Placeholder(), a = new Placeholder(), b = new Placeholder();
        var word = S(QualitativeMotionFrame.Move(frame.Right, u), QualitativeMotionFrame.Move(frame.Up, u));
        var stroke = QualitativeStrokeGraph.Stroke(new Placeholder(), a, b, word);
        var graph = new QualitativeStrokeGraph(frame, S(a, b), S(stroke));
        EquivalenceRevisionStudy.ReadSpans(graph, S(new Relation(new Placeholder(), stroke)), out var values);
        Assert.Empty(E(values));
    }

    [Fact]
    public void Open_span_or_open_focus_cannot_certify_its_known_prefix()
    {
        var frame = new QualitativeMotionFrame(); IElement u = new Placeholder(), a = new Placeholder(), b = new Placeholder();
        var openWord = StructuralConstraint.Open(QualitativeMotionFrame.Move(frame.Right, u));
        var stroke = QualitativeStrokeGraph.Stroke(new Placeholder(), a, b, openWord);
        var graph = new QualitativeStrokeGraph(frame, S(a, b), S(stroke));
        EquivalenceRevisionStudy.ReadSpans(graph, S(new Relation(new Placeholder(), stroke)), out var openSpan);
        Assert.Empty(E(openSpan));
        var goodWord = S(QualitativeMotionFrame.Move(frame.Right, u));
        var goodStroke = QualitativeStrokeGraph.Stroke(new Placeholder(), a, b, goodWord);
        var goodGraph = new QualitativeStrokeGraph(frame, S(a, b), S(goodStroke));
        EquivalenceRevisionStudy.ReadSpans(goodGraph, StructuralConstraint.Open(new Relation(new Placeholder(), goodStroke)), out var openFocus);
        Assert.Empty(E(openFocus));
    }

    [Fact]
    public void Duplicate_span_roles_remain_unresolved_instead_of_selecting_a_first_binding()
    {
        var output = Study.Value.Cases[0].Stages[0].Output!;
        var strokes = E(output.Graph.Strokes); IElement role = new Placeholder();
        EquivalenceRevisionStudy.ReadSpans(output.Graph,
            S(new Relation(role, strokes[0]), new Relation(role, strokes[1])), out var values);
        Assert.Empty(E(values));
    }

    [Fact]
    public void Next_message_keeps_the_actual_distinguishing_observation_and_its_repair_context()
    {
        foreach (var item in Study.Value.Cases.Take(2))
        {
            var revealed = item.Stages[1];
            Assert.Same(item.NextMessage, revealed.Publications.Single().Surface);
            Assert.Contains(Reachable(item.Correction), element => ReferenceEquals(element, revealed.Surface));
            Assert.DoesNotContain(Reachable(item.NextMessage), element => ReferenceEquals(element, revealed.Selection.Surface));
            Assert.True(E(revealed.Withdrawn).Length > 0);
        }
    }

    [Fact]
    public void Entire_study_is_native_unmeasured_structure_and_preserves_independent_role_identity()
    {
        var study = Study.Value;
        Assert.Equal(4, study.Cases.Count); Assert.Equal(10, study.Cases.Sum(item => item.Stages.Count));
        Assert.DoesNotContain(Reachable(study.Surface), element => element is Carrier or CarrierLocation);
        var equal = study.Forms.Single(form => form.Id == "square").Hypothesis;
        var bindings = E(equal.Bindings).Cast<Relation>().ToArray();
        Assert.NotSame(bindings[0].Left, bindings[1].Left);
        Assert.Same(bindings[0].Right, bindings[1].Right);
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; yield return element;
            if (element is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); }
        }
    }
}
