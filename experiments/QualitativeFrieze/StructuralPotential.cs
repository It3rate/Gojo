using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using E = Gojo.Experiments.QualitativeFrieze.StructuralEvidenceRules;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Exploratory geometric contracts over one selected pair of straight
/// support families. A possibility concerns extended supporting lines; finite
/// support requires separate ordered encounter-in-span premises. None of these
/// reads creates a material junction or asserts an empty face.</summary>
public static class StructuralPotential
{
    // support family = direction -> (strict transverse span -> Closed(actual lines))
    // encounter = (actual A support -> actual B support) -> open corner role
    // coverage question = actual support -> Closed(start, encounter, encounter, end)
    // construction = actual support source -> (corner roles -> ordered boundary)
    // geometric promises are exposed premises, not inferred from a screen drawing.
    // Rule definitions retain the premise diagrams and their candidate conclusion.
    // completion = source read -> (allowed assumptions -> (introduced evidence ->
    //              (conditional read -> derived boundary observation)))
    public sealed record NamedPredicate(string Name, Relation Predicate);

    public sealed class Frame
    {
        public QualitativeMotionFrame Motion { get; }
        public QualitativeRecognitionObservation SourceObservation { get; }
        public Relation Source { get; }
        public Relation FamilyA { get; }
        public Relation FamilyB { get; }
        public Relation GapA { get; }
        public Relation GapB { get; }
        public Relation Straight { get; }
        public Relation SeparateA { get; }
        public Relation SeparateB { get; }
        public Relation Independent { get; }
        public Relation RightAngle { get; }
        public Relation EqualSpacing { get; }
        public Relation UniformA { get; }
        public Relation UniformB { get; }
        public IReadOnlyList<Relation> SpanCoverage { get; }
        public Relation PotentialBoundary { get; }
        public Relation PotentialRectangle { get; }
        public Relation PotentialSquare { get; }
        public Relation FiniteBoundary { get; }
        public Relation FiniteRectangle { get; }
        public Relation FiniteSquare { get; }
        public IReadOnlyList<NamedPredicate> Leaves { get; }
        public IReadOnlyList<NamedPredicate> Goals { get; }
        public E.Program Laws { get; }
        public Relation Surface { get; }
        public IReadOnlyList<Relation> Supports { get; }
        public IReadOnlyList<Placeholder> CornerRoles { get; }
        public Relation Encounters { get; }
        public Relation Boundary { get; }

        public Frame() : this(DefaultObservation()) { }

        private Frame(QualitativeRecognitionObservation observation) : this(observation,
            Items(observation.Segments).Cast<Relation>().ToArray()) { }

        /// <summary>Bind the same candidate obligations to actual exposed
        /// passages. The source can contain other strokes; this read selects
        /// only these four, without copying or joining their endpoint sites.</summary>
        public Frame(QualitativeRecognitionObservation observation, IReadOnlyList<Relation> supports)
        {
            if (supports.Count != 4 || supports.Distinct<Relation>(ReferenceEqualityComparer.Instance).Count() != 4 ||
                supports.Any(support => !observation.TryReadSegment(support, out _, out _, out _, out _)))
                throw new ArgumentException("A candidate selects four actual distinct published passages.", nameof(supports));
            Motion = observation.Frame; SourceObservation = observation; Supports = supports.ToArray();
            var endpoints = Supports.SelectMany(support => {
                observation.TryReadSegment(support, out _, out var from, out var to, out _); return new[] { from, to }; }).ToArray();
            GapA = new(new Placeholder(), new Placeholder());
            GapB = new(new Placeholder(), new Placeholder());
            FamilyA = new(Motion.Horizontal, new Relation(GapA, S(Supports[0], Supports[1])));
            FamilyB = new(Motion.Vertical, new Relation(GapB, S(Supports[2], Supports[3])));
            Source = new(SourceObservation.Surface, new Relation(FamilyA, FamilyB));
            CornerRoles = Enumerable.Range(0, 4).Select(_ => new Placeholder()).ToArray();
            // Around the selected cell: A0/B0, A0/B1, A1/B1, A1/B0.
            Encounters = S(Encounter(0, 2, 0), Encounter(0, 3, 1), Encounter(1, 3, 2), Encounter(1, 2, 3));
            Boundary = S(Edge(0, 1, 0), Edge(1, 2, 3), Edge(2, 3, 1), Edge(3, 0, 2));
            Straight = new(Source, S(Supports.Select((line, i) => (IElement)new Relation(line,
                i < 2 ? Motion.Horizontal : Motion.Vertical)).ToArray()));
            // Strict order is a separately asserted transverse-placement contract;
            // neither distinct line identities nor ordered membership proves it.
            SeparateA = new(FamilyA, GapA); SeparateB = new(FamilyB, GapB);
            Independent = new(new Relation(FamilyA, FamilyB), new Relation(Motion.Horizontal, Motion.Vertical));
            var turn = new Placeholder();
            var turns = new[] { new Relation(Motion.Right, Motion.Up), new Relation(Motion.Up, Motion.Left),
                new Relation(Motion.Left, Motion.Down), new Relation(Motion.Down, Motion.Right) };
            RightAngle = new(new Relation(FamilyA, FamilyB), new Relation(
                S(Motion.Right, Motion.Up, Motion.Left, Motion.Down, Motion.Right),
                S(turns.Select(row => (IElement)new Relation(row, turn)).ToArray())));
            var commonReach = new Placeholder();
            EqualSpacing = new(new Relation(GapA, GapB), S(new Relation(GapA, commonReach), new Relation(GapB, commonReach)));
            // Uniformity of either extended repeat family is deliberately NOT
            // cross-family reach equality. These optional streams are unneeded here.
            UniformA = new(FamilyA, new Relation(GapA, new Placeholder()));
            UniformB = new(FamilyB, new Relation(GapB, new Placeholder()));
            SpanCoverage = new[] {
                Coverage(0, 0, 1), Coverage(1, 3, 2), Coverage(2, 0, 3), Coverage(3, 1, 2) };
            PotentialBoundary = new(Source, new Relation(Encounters, Boundary));
            PotentialRectangle = new(PotentialBoundary, RightAngle);
            PotentialSquare = new(PotentialRectangle, EqualSpacing);
            FiniteBoundary = new(PotentialBoundary, S(SpanCoverage.Cast<IElement>().ToArray()));
            FiniteRectangle = new(FiniteBoundary, RightAngle);
            FiniteSquare = new(FiniteRectangle, EqualSpacing);
            Laws = new(new E.Rule[] {
                new("Two separated straight families in an independent planar basis", PotentialBoundary,
                    [Straight, SeparateA, SeparateB, Independent]),
                new("Perpendicular supporting families", PotentialRectangle, [RightAngle, PotentialBoundary]),
                new("One reach shared between the two families", PotentialSquare, [EqualSpacing, PotentialRectangle]),
                new("All selected encounters lie inside their finite supports", FiniteBoundary,
                    new[] { PotentialBoundary }.Concat(SpanCoverage).ToArray()),
                new("Finite rectangular boundary", FiniteRectangle, [RightAngle, FiniteBoundary]),
                new("Finite square boundary", FiniteSquare, [EqualSpacing, FiniteRectangle]) });
            Leaves = new[] {
                new NamedPredicate("Straight supporting families", Straight),
                new("Distinct transverse positions in family A", SeparateA),
                new("Distinct transverse positions in family B", SeparateB),
                new("Independent directions in a common plane", Independent),
                new("Perpendicular geometric directions", RightAngle),
                new("Equal reach between the two families", EqualSpacing),
                new("Uniform repetition within family A", UniformA), new("Uniform repetition within family B", UniformB)
            }.Concat(SpanCoverage.Select((row, i) => new NamedPredicate($"Both encounters inside support {i + 1}", row))).ToArray();
            Goals = [new("Supporting-line boundary potential", PotentialBoundary),
                new("Supporting-line rectangle potential", PotentialRectangle), new("Supporting-line square potential", PotentialSquare),
                new("Finite four-sided boundary", FiniteBoundary), new("Finite rectangle", FiniteRectangle), new("Finite square", FiniteSquare)];
            Surface = new(Source, new Relation(S(Leaves.Select(row => (IElement)row.Predicate).ToArray()),
                new Relation(S(Goals.Select(row => (IElement)row.Predicate).ToArray()), Laws.Surface)));

            Relation Encounter(int a, int b, int corner) => new(new Relation(Supports[a], Supports[b]), CornerRoles[corner]);
            Relation Edge(int from, int to, int support) => new(new Relation(CornerRoles[from], CornerRoles[to]), Supports[support]);
            Relation Coverage(int support, int first, int second)
            {
                observation.TryReadSegment(Supports[support], out _, out _, out _, out var heading);
                var reverse = Items(heading).Any(direction => ReferenceEquals(direction, support < 2 ? Motion.Left : Motion.Down));
                return new(Supports[support], S(endpoints[2 * support], CornerRoles[reverse ? second : first],
                    CornerRoles[reverse ? first : second], endpoints[2 * support + 1]));
            }
        }

        private static QualitativeRecognitionObservation DefaultObservation()
        {
            var motion = new QualitativeMotionFrame();
            var endpoints = Enumerable.Range(0, 8).Select(_ => new Placeholder()).ToArray();
            var supports = Enumerable.Range(0, 4).Select(i => new Relation(new Placeholder(),
                new Relation(new Relation(endpoints[2 * i], endpoints[2 * i + 1]), S(i < 2 ? motion.Right : motion.Up)))).ToArray();
            return new(motion, S(endpoints), S(supports.Cast<IElement>().ToArray()), crossings: StructuralConstraint.Open());
        }

        /// <summary>Publishes explicit geometric premises. This fixture does not
        /// pretend to have discovered straightness, perpendicularity or equal
        /// reach from its coarse endpoint headings.</summary>
        public E.Publication Publish(params (Relation Predicate, string Answer)[] facts) => new(Source,
            facts.Select(fact => D.Evidence.FromAnswer(fact.Predicate, fact.Answer, new Relation(Source, fact.Predicate))).ToArray());

        public Completion Complete(E.Publication source, Relation goal, Relation allowedAssumptions, int queryCapacity = 64) =>
            new(this, source, goal, allowedAssumptions, queryCapacity);
    }

    public sealed class Completion
    {
        public E.Receipt SourceRead { get; }
        public E.Receipt ConditionalRead { get; }
        public Relation Assumptions { get; }
        public Relation AssumedPredicates { get; }
        public Relation AllowedAssumptions { get; }
        public Relation Blocked { get; }
        public BoundarySample? Sample { get; }
        public Relation Surface { get; }
        public string Answer => Items(Blocked).Length != 0 ? "Blocked" : Sample is not null ? "Yes" : ConditionalRead.Answer;

        internal Completion(Frame frame, E.Publication source, Relation goal, Relation allowed, int capacity)
        {
            AllowedAssumptions = allowed;
            SourceRead = E.Read(frame.Laws, source, goal, capacity);
            var invalid = new List<IElement>();
            var finite = ReferenceEquals(goal, frame.FiniteBoundary) || ReferenceEquals(goal, frame.FiniteRectangle) || ReferenceEquals(goal, frame.FiniteSquare);
            if (!finite) invalid.Add(goal);
            if (!ReferenceEquals(source.Source, frame.Source)) invalid.Add(source.Surface);
            if (!Closed(allowed) || Items(allowed).Any(predicate => !frame.Leaves.Any(leaf => ReferenceEquals(leaf.Predicate, predicate)))) invalid.Add(allowed);
            var assumed = new List<D.Evidence>();
            if (invalid.Count == 0 && SourceRead.Answer == "Open" && Items(SourceRead.BudgetCuts).Length == 0)
                foreach (var predicate in Items(SourceRead.Missing).OfType<Relation>().Distinct(ReferenceEqualityComparer.Instance).Cast<Relation>())
                    if (Items(allowed).Any(actual => ReferenceEquals(actual, predicate)))
                        assumed.Add(D.Evidence.FromAnswer(predicate, "Yes", new Relation(allowed,
                            new Relation(source.Surface, predicate))));
            Assumptions = S(assumed.Select(row => (IElement)row.Surface).ToArray());
            AssumedPredicates = S(assumed.Select(row => (IElement)row.Predicate).ToArray());
            // The extended publication is a conditional model, never imported
            // into the original source. Opposed source evidence is not waived.
            var conditional = new E.Publication(source.Source, source.Evidence.Concat(assumed).ToArray());
            ConditionalRead = E.Read(frame.Laws, conditional, goal, capacity);
            Blocked = S(invalid.ToArray());
            if (invalid.Count == 0 && ConditionalRead.Answer == "Yes")
                Sample = new(frame, goal, ConditionalRead, Assumptions);
            Surface = new(SourceRead.Surface, new Relation(allowed, new Relation(Assumptions,
                new Relation(ConditionalRead.Surface, new Relation(Blocked, Sample?.Surface ?? StructuralConstraint.Open())))));
        }
    }

    /// <summary>A query-scoped relational completion: finite boundary topology,
    /// optional angle/reach promises, no measured coordinates. Its corner sites
    /// belong to an observational arrangement, not to the source's flow graph.</summary>
    public sealed class BoundarySample
    {
        public QualitativeRecognitionObservation Observation { get; }
        public Relation Correspondence { get; }
        public Relation Incidences { get; }
        public Relation Parameters { get; }
        public Relation Promises { get; }
        public QualitativeMotifInduction Motifs { get; }
        public Relation CycleWitness { get; }
        public Relation Surface { get; }

        internal BoundarySample(Frame frame, Relation goal, E.Receipt read, Relation assumptions)
        {
            var sites = Enumerable.Range(0, 4).Select(_ => new Placeholder()).ToArray();
            var directions = new[] { frame.Motion.Right, frame.Motion.Up, frame.Motion.Left, frame.Motion.Down };
            var edges = Enumerable.Range(0, 4).Select(i => new Relation(new Placeholder(),
                new Relation(new Relation(sites[i], sites[(i + 1) % 4]), S(directions[i])))).ToArray();
            Observation = new(frame.Motion, S(sites), S(edges.Cast<IElement>().ToArray()));
            var supports = new[] { 0, 3, 1, 2 };
            Correspondence = new(S(Enumerable.Range(0, 4).Select(i => (IElement)new Relation(frame.CornerRoles[i], sites[i])).ToArray()),
                S(Enumerable.Range(0, 4).Select(i => (IElement)new Relation(edges[i], frame.Supports[supports[i]])).ToArray()));
            var encounters = Items(frame.Encounters).Cast<Relation>().ToArray();
            Incidences = S(Enumerable.Range(0, 4).Select(i => (IElement)new Relation(encounters[i].Left, sites[i])).ToArray());
            var rectangle = ReferenceEquals(goal, frame.FiniteRectangle) || ReferenceEquals(goal, frame.FiniteSquare);
            var square = ReferenceEquals(goal, frame.FiniteSquare);
            // Independent open parameters do NOT mean unequal parameters. Only
            // the square question requests a shared-reach binding.
            var reach = new Placeholder();
            Parameters = S(new Relation(frame.GapA, reach), new Relation(frame.GapB, square ? reach : new Placeholder()));
            Promises = S((rectangle ? new IElement[] { frame.RightAngle } : [])
                .Concat(square ? new IElement[] { frame.EqualSpacing } : []).ToArray());
            Motifs = new(Observation, maxCycleEdges: 4, proposalCapacity: 256, retainedCapacity: 64);
            var cycles = Motifs.Families.Where(family => family.Kind == "cycle" && family.SegmentCount == 4)
                .SelectMany(family => family.Occurrences.Select(occurrence => (IElement)occurrence.Read)).ToArray();
            CycleWitness = S(cycles);
            var output = new Relation(Observation.Surface, new Relation(Correspondence, new Relation(Incidences,
                new Relation(Parameters, new Relation(Promises, new Relation(Motifs.Surface, CycleWitness))))));
            Surface = new(new Relation(frame.Source, goal), new Relation(new Relation(read.Surface, assumptions), output));
        }
    }

    public static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    public static IElement[] Items(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var start, out var end) && start.Equals(end);
}
