using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeDrawingReadScopeTests
{
    [Fact]
    public void ScopedAndOrdinaryExecutionKeepTheSameActionLedgerAndStrokeTopology()
    {
        var ordinary = Run(false); var scoped = Run(true);
        Assert.Equal(ordinary.Actions, scoped.Actions);
        Assert.Equal(ordinary.Position, scoped.Position);
        Assert.Equal(ordinary.Strokes, scoped.Strokes);
        Assert.Equal(ordinary.Sites, scoped.Sites);
        Assert.Equal(ordinary.History, scoped.History);
        Assert.Equal(ordinary.Endpoints, scoped.Endpoints);

        static (int Actions, (int, int) Position, int Strokes, int Sites, int History, int[] Endpoints) Run(bool cache)
        {
            var frame = new QualitativeMotionFrame(); var drawing = new QualitativeDrawingReceiver(frame);
            IElement reach = new Placeholder();
            var right = S(QualitativeMotionFrame.Move(frame.Right, reach));
            var up = S(QualitativeMotionFrame.Move(frame.Up, reach));
            var actions = new[] { drawing.Draw(right), drawing.Draw(up), drawing.Mirror(drawing.HorizontalMirror),
                drawing.Draw(right), drawing.Reset(), drawing.Jump(up), drawing.Anchor(), drawing.Draw(up) };
            var scope = cache ? drawing.BeginSynchronousReadScope() : null;
            try
            {
                foreach (var action in actions)
                {
                    var receipt = drawing.Apply(new Placeholder(), S(action), S());
                    Assert.True(drawing.TryReadAccepted(receipt, out _, out _));
                    Assert.True(drawing.TryReadAccepted(receipt, out _, out _));
                }
            }
            finally { scope?.Dispose(); }
            // Reading after disposal is still an ordinary exact read.
            Assert.True(drawing.TryReadState(drawing.State, out _, out _, out _, out _, out _, out _, out _));
            var sites = E(drawing.Sites).Cast<Relation>().Select(binding => binding.Left).ToArray();
            var ends = E(drawing.Strokes).Cast<Relation>().SelectMany(stroke =>
            {
                Assert.True(QualitativeDrawingReceiver.TryReadStroke(stroke, out _, out var a, out var b, out _));
                return new[] { Array.FindIndex(sites, site => ReferenceEquals(site, a)),
                    Array.FindIndex(sites, site => ReferenceEquals(site, b)) };
            }).ToArray();
            var x = 0; var y = 0;
            foreach (var move in E(drawing.Position).Cast<Relation>())
            {
                Assert.True(frame.Direction(move, out var axis, out var forward, out _));
                if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
            }
            return (actions.Length, (x, y), E(drawing.Strokes).Length, sites.Length, E(drawing.History).Length, ends);
        }
    }

    [Fact]
    public void NestedScopesStillRejectNewForgeryAndCannotCommitAStaleAcceptedReceipt()
    {
        var drawing = new QualitativeDrawingReceiver(new());
        Relation first, cachedAdded;
        using (drawing.BeginSynchronousReadScope())
        {
            first = drawing.Apply(new Placeholder(), S(drawing.Draw(S())), S());
            Assert.True(drawing.TryReadAccepted(first, out _, out cachedAdded));
            using (drawing.BeginSynchronousReadScope())
            {
                Assert.True(drawing.TryReadAccepted(first, out _, out var repeatedAdded));
                Assert.Same(cachedAdded, repeatedAdded);
            }
            var body = (Relation)first.Right; var rest = (Relation)body.Right; var pending = (Relation)rest.Right;
            var forged = new Relation(first.Left, new Relation(body.Left,
                new Relation(rest.Left, new Relation(pending.Left, S()))));
            Assert.False(drawing.TryReadAccepted(forged, out _, out _));
            var next = drawing.Apply(new Placeholder(), S(drawing.Jump(S())), S());
            Assert.True(drawing.TryReadAccepted(next, out _, out _));
            Assert.False(drawing.TryCommit(first));
        }
        Assert.True(drawing.TryReadAccepted(first, out _, out var freshAdded));
        Assert.NotSame(cachedAdded, freshAdded); Assert.Equal(E(cachedAdded), E(freshAdded));
        Assert.False(drawing.TryCommit(first));
        // The same newly built fake is rechecked outside the disposed scope.
        var outer = (Relation)first.Right; var tail = (Relation)outer.Right; var proof = (Relation)tail.Right;
        Assert.False(drawing.TryReadAccepted(new(first.Left, new Relation(outer.Left,
            new Relation(tail.Left, new Relation(proof.Left, S())))), out _, out _));
    }

    [Fact]
    public void PerReadIndexesPreserveDuplicateEventAndDuplicateBindingRejection()
    {
        var drawing = new QualitativeDrawingReceiver(new());
        drawing.Apply(new Placeholder(), S(drawing.Draw(S())), S());
        var state = drawing.State; var positions = (Relation)state.Right; var body = (Relation)positions.Right;
        var orientation = (Relation)body.Left; var ledger = (Relation)body.Right; var ink = (Relation)ledger.Left;
        var events = (Relation)ink.Right; var bindings = (Relation)orientation.Right;
        var duplicateEvents = S([.. E(events), E(events)[^1]]);
        var duplicateBindings = S([.. E(bindings), E(bindings)[0]]);
        var repeatedEventState = new Relation(state.Left, new Relation(positions.Left,
            new Relation(body.Left, new Relation(new Relation(ink.Left, duplicateEvents), ledger.Right))));
        var repeatedBindingState = new Relation(state.Left, new Relation(positions.Left,
            new Relation(new Relation(orientation.Left, duplicateBindings), body.Right)));
        using (drawing.BeginSynchronousReadScope())
        {
            Assert.True(Read(state));
            Assert.False(Read(repeatedEventState));
            Assert.False(Read(repeatedBindingState));
        }
        Assert.False(Read(repeatedEventState)); Assert.False(Read(repeatedBindingState));
        bool Read(Relation input) => drawing.TryReadState(input, out _, out _, out _, out _, out _, out _, out _);
    }

    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
