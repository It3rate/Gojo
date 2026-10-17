using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ConnectedPatternLibraryTests
{
    [Fact]
    public void FourOrderedLevelsAndExplicitColumnSeamsGroundTheNativeGraphs()
    {
        var library = new ConnectedPatternLibrary(); Assert.Equal(8, library.Entries.Count); Assert.Equal(16, library.Candidates.Count);
        Assert.DoesNotContain(Traverse(library.Surface), item => item is Carrier or CarrierLocation);
        foreach (var entry in library.Candidates)
        {
            Assert.Same(library.Frame, entry.Graph.Frame); Assert.True(Contains(library.Surface, entry.Surface));
            Assert.Equal(4, E(entry.Levels).Length); Assert.Equal(3, E(entry.LevelOrder).Length);
            var columns = E(entry.Columns); var levels = E(entry.Levels); Assert.InRange(columns.Length, 4, 8);
            Assert.Equal(columns.Length, E(entry.ColumnCycle).Length); Assert.Same(entry.ColumnCycle, entry.Period);
            var seam = Assert.IsType<Relation>(E(entry.ColumnCycle)[^1]); Assert.Same(columns[^1], seam.Left); Assert.Same(columns[0], seam.Right);
            Assert.Equal(columns.Length, E(entry.PeriodWord).Length);
            Assert.All(E(entry.PeriodWord).Cast<Relation>(), move => Assert.Same(library.Frame.Right, move.Left));
            Assert.Equal(columns.Length, E(entry.ColumnSteps).Length); Assert.Equal(3, E(entry.LevelSteps).Length);
            foreach (var step in E(entry.ColumnSteps).Cast<Relation>())
            {
                Assert.Contains(E(entry.ColumnCycle), edge => ReferenceEquals(edge, step.Left));
                Assert.Same(library.Frame.Right, Assert.IsType<Relation>(Assert.Single(E(Assert.IsType<Relation>(step.Right)))).Left);
            }
            foreach (var step in E(entry.LevelSteps).Cast<Relation>())
            {
                Assert.Contains(E(entry.LevelOrder), edge => ReferenceEquals(edge, step.Left));
                Assert.Same(library.Frame.Up, Assert.IsType<Relation>(Assert.Single(E(Assert.IsType<Relation>(step.Right)))).Left);
            }
            Assert.Equal(columns.Length * levels.Length, E(entry.Coordinates).Length);
            Assert.Equal(columns.Length * levels.Length, E(entry.Graph.Sites).Length); Assert.Same(entry.Coordinates, entry.SiteBindings);
            foreach (var binding in E(entry.Coordinates).Cast<Relation>())
            {
                Assert.Contains(E(entry.Graph.Sites), site => ReferenceEquals(site, binding.Left));
                var location = Assert.IsType<Relation>(binding.Right);
                Assert.Contains(columns, column => ReferenceEquals(column, location.Left)); Assert.Contains(levels, level => ReferenceEquals(level, location.Right));
            }
            Assert.Empty(E(entry.Unresolved)); Assert.InRange(E(entry.Graph.Strokes).Length, 1, 48);
        }
    }

    [Fact]
    public void AuthoredVariationsChangeOneExposedCoarseBindingAndRetainOtherCategories()
    {
        var library = new ConnectedPatternLibrary();
        for (var i = 0; i < library.Entries.Count; i++)
        {
            var parent = library.Entries[i]; var variation = library.Candidates[i + library.Entries.Count];
            var before = E(parent.Publications).Cast<Relation>().ToArray(); var after = E(variation.Publications).Cast<Relation>().ToArray();
            Assert.Equal(before.Length, after.Length); var changes = 0;
            for (var j = 0; j < before.Length; j++)
            {
                Assert.True(QualitativePublication.TryRead(before[j], out var source, out _, out var occurrence, out var basis,
                    out var domain, out var oldSupport, out _));
                Assert.True(QualitativePublication.TryRead(after[j], out var otherSource, out _, out var otherOccurrence,
                    out var otherBasis, out var otherDomain, out var newSupport, out _));
                Assert.Same(library.SignalDomain, domain); Assert.Same(domain, otherDomain); Assert.Same(library.CoarseBasis, basis); Assert.Same(basis, otherBasis);
                Assert.NotSame(source, otherSource); Assert.NotSame(occurrence, otherOccurrence);
                var differs = !ReferenceEquals(Assert.Single(E(oldSupport)), Assert.Single(E(newSupport)));
                if (differs) changes++; Assert.Equal(j == 1, differs);
            }
            Assert.Equal(1, changes);
        }
    }

    [Fact]
    public void ChordParticipationAccountsForEveryAdjacentColumnAndHeightStep()
    {
        var library = new ConnectedPatternLibrary(); var diagonal = false; var heightJump = false;
        foreach (var entry in library.Candidates)
        {
            var columns = E(entry.Columns); var levels = E(entry.Levels);
            var coordinates = E(entry.Coordinates).Cast<Relation>().ToArray();
            foreach (var stroke in E(entry.Graph.Strokes).Cast<Relation>())
            {
                Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word));
                var a = Assert.IsType<Relation>(coordinates.Single(binding => ReferenceEquals(binding.Left, from)).Right);
                var b = Assert.IsType<Relation>(coordinates.Single(binding => ReferenceEquals(binding.Left, to)).Right);
                var ax = Index(columns, a.Left); var bx = Index(columns, b.Left); var ay = Index(levels, a.Right); var by = Index(levels, b.Right);
                var dx = bx - ax; if (dx < 0) dx += columns.Length;
                var moves = E(word).Cast<Relation>().ToArray(); Assert.NotEmpty(moves);
                var h = 0; var v = 0;
                foreach (var move in moves)
                {
                    Assert.True(library.Frame.Direction(move, out var axis, out var forward, out var reach));
                    if (ReferenceEquals(axis, library.Frame.Horizontal)) { h += forward ? 1 : -1; Assert.Same(library.HorizontalReach, reach); }
                    else { v += forward ? 1 : -1; Assert.Same(library.VerticalReach, reach); }
                }
                Assert.Equal(dx, h); Assert.Equal(by - ay, v);
                diagonal |= h != 0 && v != 0; heightJump |= Math.Abs(v) > 1;
            }
        }
        Assert.True(diagonal); Assert.True(heightJump);
    }

    [Fact]
    public void ReceiverSurfacesAndPublicationsExcludePrivateProducerPhases()
    {
        var library = new ConnectedPatternLibrary();
        foreach (var entry in library.Entries)
        {
            foreach (var producer in E(entry.Producers).Cast<Relation>())
            {
                var privateClock = Assert.IsType<Relation>(Assert.IsType<Relation>(producer.Left).Left);
                var privateDomain = Assert.IsType<Relation>(Assert.IsType<Relation>(privateClock.Left).Right);
                foreach (var phase in E(privateDomain))
                {
                    Assert.False(Contains(entry.MainConnection.Surface, phase));
                    Assert.All(E(entry.Publications), publication => Assert.False(Contains(publication, phase)));
                    if (entry.DecorationConnection is not null) Assert.False(Contains(entry.DecorationConnection.Surface, phase));
                }
            }
            Assert.Equal(E(entry.Columns).Length * E(entry.Producers).Length, E(entry.Publications).Length);
            foreach (var selection in E(entry.Selections).Cast<Relation>())
            {
                var delivery = Assert.IsType<Relation>(selection.Left);
                Assert.True(Contains(entry.Surface, delivery));
                foreach (var conditional in E(Assert.IsType<Relation>(selection.Right)).Cast<Relation>())
                    Assert.True(Contains(entry.ConditionalReceipts, conditional));
            }
        }
    }

    [Fact]
    public void DifferentHiddenImplementationsWithTheSameCoarseOutputProduceTheSameAction()
    {
        var input = Input(); var p = new Placeholder(); var q = new Placeholder(); var r = new Placeholder();
        var first = new PhaseRecurrence(S(p), S(new Relation(p, p)));
        var second = new PhaseRecurrence(S(q, r), S(new Relation(q, r), new Relation(r, q)));
        var probe = new Placeholder(); var probes = S(probe);
        var firstLaw = FiniteLaw.Create(first.Domain, probes, input.Domain, FiniteLaw.Row(p, probe, S(input.Signal)));
        var secondLaw = FiniteLaw.Create(second.Domain, probes, input.Domain,
            FiniteLaw.Row(q, probe, S(input.Signal)), FiniteLaw.Row(r, probe, S(input.Signal)));
        FiniteLaw.TryReadResult(FiniteLaw.Evaluate(firstLaw, S(p), probes), out var exposedFirst, out _, out _);
        FiniteLaw.TryReadResult(FiniteLaw.Evaluate(secondLaw, S(q), probes), out var exposedSecond, out _, out _);
        var one = input.Connection.Consume(Publication(input, exposedFirst), S(input.Column));
        var two = input.Connection.Consume(Publication(input, exposedSecond), S(input.Column));
        Assert.True(input.Connection.TryReadResult(one, out var a, out var blockedA, out var pendingA));
        Assert.True(input.Connection.TryReadResult(two, out var b, out var blockedB, out var pendingB));
        Assert.Same(input.Action, Assert.Single(E(a))); Assert.Same(Assert.Single(E(a)), Assert.Single(E(b)));
        Assert.Empty(E(blockedA)); Assert.Empty(E(blockedB)); Assert.Empty(E(pendingA)); Assert.Empty(E(pendingB));
        Assert.False(Contains(one, first.Surface)); Assert.False(Contains(two, second.Surface));
        Assert.All(new IElement[] { p, q, r }, phase => { Assert.False(Contains(one, phase)); Assert.False(Contains(two, phase)); });
    }

    [Fact]
    public void UnknownAndCompetingCoarsePublicationsRemainUnknownRatherThanRest()
    {
        var input = Input();
        foreach (var support in new[] { StructuralConstraint.Open(), StructuralConstraint.Open(input.Signal), S(input.Signal, input.Other), S() })
        {
            var publication = Publication(input, support); var read = input.Connection.Consume(publication, S(input.Column));
            Assert.True(input.Connection.TryReadResult(read, out var actions, out var blocked, out var pending));
            Assert.False(Closed(actions)); Assert.Empty(E(blocked)); Assert.NotEmpty(E(pending)); Assert.True(Contains(read, publication));
        }
        var unknownColumn = input.Connection.Consume(Publication(input, S(input.Signal)), StructuralConstraint.Open(input.Column));
        Assert.True(input.Connection.TryReadResult(unknownColumn, out var unknown, out _, out var remainder));
        Assert.False(Closed(unknown)); Assert.NotEmpty(E(remainder));
    }

    [Fact]
    public void ForeignPortsAreBlockedAndKnownDenialDiffersFromUnknown()
    {
        var input = Input();
        foreach (var publication in new[]
        {
            QualitativePublication.Create(new Placeholder(), input.Receiver, input.Event, input.Basis, input.Domain, S(input.Signal)),
            QualitativePublication.Create(input.Producer, new Placeholder(), input.Event, input.Basis, input.Domain, S(input.Signal)),
            QualitativePublication.Create(input.Producer, input.Receiver, input.Event, new Placeholder(), input.Domain, S(input.Signal)),
            QualitativePublication.Create(input.Producer, input.Receiver, input.Event, input.Basis, S(input.Signal, input.Other), S(input.Signal))
        })
        {
            var read = input.Connection.Consume(publication, S(input.Column));
            Assert.True(input.Connection.TryReadResult(read, out var selected, out var blocked, out var pending));
            Assert.False(Closed(selected)); Assert.Same(publication, Assert.Single(E(blocked))); Assert.Empty(E(pending));
        }
        var denied = Publication(input, S(input.Other)); var receipt = input.Connection.Consume(denied, S(input.Column));
        Assert.True(input.Connection.TryReadResult(receipt, out var actions, out var no, out var unknown));
        Assert.True(Closed(actions)); Assert.Empty(E(actions)); Assert.Same(denied, Assert.Single(E(no))); Assert.Empty(E(unknown));
    }

    [Fact]
    public void MixedAllowAndDenyRowsCannotHideDenialInAUnion()
    {
        var input = Input(); var law = FiniteLaw.Create(input.Domain, input.Columns, S(input.Action),
            FiniteLaw.Row(input.Signal, input.Column, S(input.Action)), FiniteLaw.Row(input.Signal, input.Column, S()));
        var connection = new CoarsePatternConnection(input.Connection.Port, input.Columns, law);
        var read = connection.Consume(Publication(input, S(input.Signal)), S(input.Column));
        Assert.True(connection.TryReadResult(read, out var actions, out var blocked, out var unresolved));
        Assert.False(Closed(actions)); Assert.Empty(E(blocked)); Assert.NotEmpty(E(unresolved));
    }

    [Fact]
    public void OmittedOrForeignNativeLawProofCannotAuthorizeCopiedSuccessFields()
    {
        var input = Input(); var publication = Publication(input, S(input.Signal)); var read = input.Connection.Consume(publication, S(input.Column));
        var body = Assert.IsType<Relation>(read.Right);
        Assert.False(input.Connection.TryReadResult(new Relation(read.Left, new Relation(S(), body.Right)), out _, out _, out _));
        var foreign = Input(); var foreignRead = foreign.Connection.Consume(Publication(foreign, S(foreign.Signal)), S(foreign.Column));
        var foreignProof = Assert.IsType<Relation>(foreignRead.Right).Left;
        Assert.False(input.Connection.TryReadResult(new Relation(read.Left, new Relation(foreignProof, body.Right)), out _, out _, out _));
        var options = Assert.IsType<Relation>(body.Right); var falseResult = new Relation(S(new Placeholder()), options.Right);
        Assert.False(input.Connection.TryReadResult(new Relation(read.Left, new Relation(body.Left, falseResult)), out _, out _, out _));
    }

    [Fact]
    public void ForgedOutputBasisAndBorrowedUnknownResidueCannotMasqueradeAsThisRead()
    {
        var input = Input(); var publication = Publication(input, S(input.Signal));
        var receipt = input.Connection.Consume(publication, S(input.Column));
        var body = Assert.IsType<Relation>(receipt.Right); var proof = Assert.IsType<Relation>(body.Left);
        var proofContract = Assert.IsType<Relation>(proof.Left);
        var forgedProof = new Relation(new Relation(proofContract.Left, S(input.Action)), proof.Right);
        Assert.False(input.Connection.TryReadResult(new Relation(receipt.Left, new Relation(forgedProof, body.Right)), out _, out _, out _));

        var unknown = input.Connection.Consume(Publication(input, StructuralConstraint.Open(input.Signal)), S(input.Column));
        var other = input.Connection.Consume(Publication(input, StructuralConstraint.Open(input.Signal)), S(input.Column));
        var unknownBody = Assert.IsType<Relation>(unknown.Right); var unknownProof = Assert.IsType<Relation>(unknownBody.Left);
        var otherProof = Assert.IsType<Relation>(Assert.IsType<Relation>(other.Right).Left);
        Assert.True(FiniteLaw.TryReadResult(unknownProof, out var support, out var residue, out var rows));
        Assert.True(FiniteLaw.TryReadResult(otherProof, out _, out var borrowed, out _)); Assert.Equal(E(residue).Length, E(borrowed).Length); Assert.NotEmpty(E(borrowed));
        var wrongProof = new Relation(unknownProof.Left, new Relation(support, new Relation(borrowed, rows)));
        var unknownResult = Assert.IsType<Relation>(unknownBody.Right); var unknownRest = Assert.IsType<Relation>(unknownResult.Right);
        var publicationOnRequest = Assert.IsType<Relation>(unknown.Left).Left;
        var copiedPending = S(new Relation(publicationOnRequest, wrongProof));
        var alteredResult = new Relation(unknownResult.Left, new Relation(unknownRest.Left, copiedPending));
        Assert.False(input.Connection.TryReadResult(new Relation(unknown.Left, new Relation(wrongProof, alteredResult)), out _, out _, out _));
    }

    [Fact]
    public void ANewFailureSlotCannotBeAliasedToAnExistingInputParticipant()
    {
        var input = Input(); var invalidLaw = StructuralConstraint.Open();
        var connection = new CoarsePatternConnection(input.Connection.Port, input.Columns, invalidLaw);
        var publication = Publication(input, S(input.Signal)); var receipt = connection.Consume(publication, S(input.Column));
        Assert.True(connection.TryReadResult(receipt, out _, out _, out _));
        var body = Assert.IsType<Relation>(receipt.Right); var proof = Assert.IsType<Relation>(body.Left);
        Assert.True(FiniteLaw.TryReadResult(proof, out var support, out var residue, out var rows));
        var failure = Assert.IsType<Relation>(Assert.Single(E(residue))); Assert.Same(invalidLaw, failure.Left);
        var aliasedResidue = S(new Relation(invalidLaw, input.Signal));
        var forgedProof = new Relation(proof.Left, new Relation(support, new Relation(aliasedResidue, rows)));
        Assert.False(connection.TryReadResult(new Relation(receipt.Left, new Relation(forgedProof, body.Right)), out _, out _, out _));
    }

    [Fact]
    public void KnownBlockedDecorationsAndOpenCounterexampleRetainTheirActualRequests()
    {
        var library = new ConnectedPatternLibrary(); var blocked = library.BlockedExample;
        Assert.NotNull(blocked.DecorationConnection); var denied = 0; var permitted = 0;
        foreach (var receipt in E(blocked.ConditionalReceipts).Cast<Relation>())
        {
            if (!blocked.DecorationConnection!.TryReadResult(receipt, out var actions, out var no, out var rowUnresolved)) continue;
            Assert.Empty(E(rowUnresolved)); Assert.True(Closed(actions));
            if (E(no).Length > 0) { denied++; Assert.Empty(E(actions)); }
            else { permitted++; Assert.Single(E(actions)); }
        }
        Assert.Equal(4, denied); Assert.Equal(2, permitted); Assert.Empty(E(blocked.Unresolved));
        var unknown = library.UnknownExample; Assert.NotEmpty(E(unknown.Unresolved)); Assert.False(Closed(unknown.Graph.Strokes));
        Assert.Contains(E(unknown.Publications).Cast<Relation>(), publication =>
            QualitativePublication.TryRead(publication, out _, out _, out _, out _, out _, out var support, out _) && !Closed(support));
        Assert.NotEmpty(E(unknown.Graph.Strokes)); Assert.True(E(unknown.Graph.Strokes).Length < E(blocked.Graph.Strokes).Length);
    }

    [Fact]
    public void IndependentSixAndThreePhaseSuppliersKeepSeparateHistoriesAndCoarsePorts()
    {
        var library = new ConnectedPatternLibrary(); var entry = library.BlockedExample;
        var producers = E(entry.Producers).Cast<Relation>().ToArray(); Assert.Equal(2, producers.Length);
        var expectedDomains = new[] { 6, 3 }; var allEvents = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var sourceIdentities = new List<IElement>();
        for (var i = 0; i < producers.Length; i++)
        {
            var clock = Assert.IsType<Relation>(Assert.IsType<Relation>(producers[i].Left).Left);
            var domain = Assert.IsType<Relation>(Assert.IsType<Relation>(clock.Left).Right);
            Assert.Equal(expectedDomains[i], E(domain).Length);
            var history = E(Assert.IsType<Relation>(producers[i].Right)).Cast<Relation>().ToArray(); Assert.Equal(6, history.Length);
            var actualPhases = new List<IElement>(); IElement? beforePhase = null;
            foreach (var record in history)
            {
                var receipt = Assert.IsType<Relation>(record.Left);
                var context = Assert.IsType<Relation>(receipt.Left); Assert.Same(clock, context.Right);
                var prior = Assert.IsType<Relation>(Assert.IsType<Relation>(context.Left).Left);
                var beforeSupport = Assert.IsType<Relation>(Assert.IsType<Relation>(prior.Right).Left);
                beforePhase ??= Assert.Single(E(beforeSupport));
                var after = Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.IsType<Relation>(receipt.Right).Left).Left);
                var stateResult = Assert.IsType<Relation>(after.Right);
                actualPhases.Add(Assert.Single(E(Assert.IsType<Relation>(stateResult.Left))));
                var eventHistory = Assert.IsType<Relation>(Assert.IsType<Relation>(stateResult.Right).Left);
                Assert.Equal(actualPhases.Count + 1, E(eventHistory).Length);
                var publication = Assert.IsType<Relation>(Assert.IsType<Relation>(record.Right).Right);
                Assert.True(QualitativePublication.TryRead(publication, out var source, out _, out var occurrence,
                    out var basis, out var signals, out _, out _));
                Assert.Same(library.CoarseBasis, basis); Assert.Same(library.SignalDomain, signals); Assert.True(allEvents.Add(occurrence));
                if (actualPhases.Count == 1) sourceIdentities.Add(source); else Assert.Same(sourceIdentities[i], source);
                Assert.DoesNotContain(E(domain), phase => Contains(publication, phase));
            }
            Assert.Same(beforePhase, actualPhases[^1]);
            if (i == 0) Assert.Equal(6, actualPhases.Distinct(ReferenceEqualityComparer.Instance).Count());
            else
            {
                Assert.Equal(3, actualPhases.Distinct(ReferenceEqualityComparer.Instance).Count());
                Assert.Same(actualPhases[0], actualPhases[3]); Assert.Same(actualPhases[1], actualPhases[4]); Assert.Same(actualPhases[2], actualPhases[5]);
            }
        }
        Assert.NotSame(sourceIdentities[0], sourceIdentities[1]);
        var mainOwner = Assert.IsType<Relation>(entry.MainConnection.Port.Left).Left;
        var decorationOwner = Assert.IsType<Relation>(entry.DecorationConnection!.Port.Left).Left;
        Assert.Same(sourceIdentities[0], mainOwner); Assert.Same(sourceIdentities[1], decorationOwner);
        var mainPublication = E(entry.Publications).Cast<Relation>().First(publication =>
            QualitativePublication.TryRead(publication, out var source, out _, out _, out _, out _, out _, out _) && ReferenceEquals(source, mainOwner));
        var column = S(E(entry.Columns)[0]); var cross = entry.DecorationConnection.Consume(mainPublication, column);
        Assert.True(entry.DecorationConnection.TryReadResult(cross, out var actions, out var blocked, out var unresolved));
        Assert.False(Closed(actions)); Assert.Same(mainPublication, Assert.Single(E(blocked))); Assert.Empty(E(unresolved));
    }

    [Fact]
    public void SuppliedNativeMapsPreserveActualSitesEdgesSharedWordsAndPrivatePhaseEdges()
    {
        var library = new ConnectedPatternLibrary(); var translation = false; var reflection = false; var glide = false;
        foreach (var entry in library.Entries)
        {
            var coordinates = E(entry.Coordinates).Cast<Relation>().ToArray();
            var producer = Assert.IsType<Relation>(E(entry.Producers)[0]);
            var clock = Assert.IsType<Relation>(Assert.IsType<Relation>(producer.Left).Left);
            var phaseEdges = E(Assert.IsType<Relation>(clock.Right)).Cast<Relation>().ToArray();
            foreach (var transform in entry.Correspondences)
            {
                Assert.True(entry.Graph.TryReadMapPassed(entry.Graph, transform.Read));
                Assert.Equal(E(entry.Graph.Sites).Length, E(transform.SiteMap).Length);
                Assert.Equal(E(entry.Graph.Strokes).Length, E(transform.StrokeMap).Length);
                var movedColumn = false; var movedLevel = false;
                foreach (var row in E(transform.SiteMap).Cast<Relation>())
                {
                    var a = Assert.IsType<Relation>(coordinates.Single(binding => ReferenceEquals(binding.Left, row.Left)).Right);
                    var b = Assert.IsType<Relation>(coordinates.Single(binding => ReferenceEquals(binding.Left, row.Right)).Right);
                    movedColumn |= !ReferenceEquals(a.Left, b.Left); movedLevel |= !ReferenceEquals(a.Right, b.Right);
                }
                translation |= movedColumn && !movedLevel; reflection |= !movedColumn && movedLevel; glide |= movedColumn && movedLevel;
                var phaseMap = E(transform.PhaseMap).Cast<Relation>().ToArray();
                foreach (var edge in phaseEdges)
                {
                    var from = phaseMap.Single(row => ReferenceEquals(row.Left, edge.Left)).Right;
                    var to = phaseMap.Single(row => ReferenceEquals(row.Left, edge.Right)).Right;
                    Assert.Contains(phaseEdges, actual => ReferenceEquals(actual.Left, from) && ReferenceEquals(actual.Right, to));
                }
                var omittedSites = S(E(transform.SiteMap).Skip(1).ToArray());
                Assert.False(entry.Graph.TryReadMapPassed(entry.Graph, entry.Graph.EvaluateMap(entry.Graph, omittedSites, transform.StrokeMap, transform.ChordMap)));
            }
        }
        Assert.True(translation); Assert.True(reflection); Assert.True(glide);
    }

    [Fact]
    public void BranchesUseSharedSiteIdentityAndCrossingDrawingsDoNotCreateNewSites()
    {
        var library = new ConnectedPatternLibrary();
        Assert.Contains(library.Entries, entry => entry.Graph.Describe(entry.Graph.Evaluate()).Branches > 0);
        var braces = library.Entries[3]; Assert.Equal(E(braces.Columns).Length * 4, E(braces.Graph.Sites).Length);
        Assert.All(E(braces.Graph.Strokes).Cast<Relation>(), stroke =>
        {
            Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _));
            Assert.Contains(E(braces.Graph.Sites), site => ReferenceEquals(site, from)); Assert.Contains(E(braces.Graph.Sites), site => ReferenceEquals(site, to));
        });
    }

    private sealed record ConnectionInput(CoarsePatternConnection Connection, IElement Producer, IElement Receiver,
        IElement Event, IElement Basis, Relation Domain, Relation Columns, IElement Column, IElement Signal, IElement Other, Relation Action);
    private static ConnectionInput Input()
    {
        var producer = new Placeholder(); var receiver = new Placeholder(); var eventIdentity = new Placeholder(); var basis = new Placeholder();
        var signal = new Placeholder(); var other = new Placeholder(); var domain = S(signal, other); var column = new Placeholder(); var columns = S(column);
        var action = S(new Relation(new Placeholder(), new Placeholder()));
        var law = FiniteLaw.Create(domain, columns, S(action), FiniteLaw.Row(signal, column, S(action)), FiniteLaw.Row(other, column, S()));
        var connection = new CoarsePatternConnection(ConditionalPassage.Decision(producer, receiver, basis, domain), columns, law);
        return new(connection, producer, receiver, eventIdentity, basis, domain, columns, column, signal, other, action);
    }
    private static Relation Publication(ConnectionInput input, Relation support) => QualitativePublication.Create(input.Producer,
        input.Receiver, input.Event, input.Basis, input.Domain, support, new Placeholder());
    private static int Index(IElement[] values, IElement item) => Array.FindIndex(values, value => ReferenceEquals(value, item));
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
    }
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(item => ReferenceEquals(item, target));
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
