using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Local concept reads over discovered observational boundaries.
/// Counts are participation counts, not measured lengths or angles. Direction,
/// cancellation and the explicitly published cyclic/turn models supply the
/// geometric evidence. Several concepts may remain possible simultaneously.</summary>
public sealed class NativeInteriorFeatures
{
    // read = (source topology -> selected boundary) ->
    //        ((side runs -> ordered turns) -> Closed(claims))
    // side = Closed(actual ordered darts) -> ((from -> to) -> additive word)
    // claim = (predicate -> evidence) ->
    //         (required native pattern -> (assumptions -> missing conditions))
    // Labels and Status are presentation reads of these native surfaces.
    public NativeInteriorTopology Topology { get; }
    public Relation Assumptions { get; }
    public IReadOnlyList<BoundaryRead> Reads { get; }
    public Relation Surface { get; }

    public NativeInteriorFeatures(NativeInteriorTopology topology, Relation? assumptions = null)
    {
        Topology = topology; Assumptions = assumptions ?? S();
        Reads = topology.Boundaries.Select(boundary => new BoundaryRead(this, boundary)).ToArray();
        Surface = new(topology.Surface, new Relation(Assumptions, S(Reads.Select(read => (IElement)read.Surface).ToArray())));
    }

    public sealed class Claim
    {
        public string Name { get; }
        public Relation Predicate { get; }
        public D.Evidence Evidence { get; }
        public Relation Required { get; }
        public Relation Assumptions { get; }
        public Relation Missing { get; }
        public Relation Surface { get; }
        public string Status => Evidence.Answer == "Yes" ? Closed(Assumptions) && Entries(Assumptions).Length == 0 ? "Supported" : "Possible" :
            Evidence.Answer == "No" ? "Refuted" : "Open";
        internal Claim(string name, Relation predicate, D.Evidence evidence, Relation required, Relation assumptions, Relation missing)
        {
            Name = name; Predicate = predicate; Evidence = evidence; Required = required; Assumptions = assumptions; Missing = missing;
            Surface = new(new Relation(predicate, evidence.Surface), new Relation(required, new Relation(assumptions, missing)));
        }
    }

    public sealed class BoundaryRead
    {
        public NativeInteriorTopology.Boundary Boundary { get; }
        public int SideCount => runs.Count;
        public bool SideCountCertain => Entries(UnresolvedSideSeams).Length == 0;
        public Relation UnresolvedSideSeams { get; }
        public Relation Sides { get; }
        public Relation Turns { get; }
        public IReadOnlyList<Claim> Claims { get; }
        public Relation Surface { get; }
        public Claim Find(string name) => Claims.Single(claim => claim.Name == name);
        private readonly NativeInteriorFeatures reader;
        private readonly QualitativeMotionFrame frame;
        private readonly List<Side> runs = [];

        internal BoundaryRead(NativeInteriorFeatures reader, NativeInteriorTopology.Boundary boundary)
        {
            this.reader = reader; Boundary = boundary; frame = reader.Topology.Source.Graph.Frame;
            foreach (var dart in boundary.Darts)
            {
                if (runs.Count != 0 && SameDirection(runs[^1].Word, runs[^1].Sector, dart.Word, dart.Sector)) runs[^1].Append(dart, frame);
                else runs.Add(new(dart));
            }
            if (runs.Count > 1 && SameDirection(runs[^1].Word, runs[^1].Sector, runs[0].Word, runs[0].Sector))
            {
                var joined = runs[^1]; foreach (var dart in runs[0].Darts) joined.Append(dart, frame);
                runs.RemoveAt(runs.Count - 1); runs[0] = joined;
            }
            Sides = S(runs.Select(run => (IElement)run.Surface).ToArray());
            var turns = runs.Select((run, i) => ReadTurn(run, runs[(i + 1) % runs.Count])).ToArray();
            Turns = S(turns.Select(turn => (IElement)turn.Proof).ToArray());
            UnresolvedSideSeams = S(turns.Select((turn, i) => (turn, i))
                .Where(pair => pair.turn.Sign is null && runs[pair.i].Sector == runs[(pair.i + 1) % runs.Count].Sector)
                .Select(pair => (IElement)pair.turn.Proof).ToArray());
            var claims = new List<Claim>();
            var closurePattern = boundary.Darts.Count == 0 ? StructuralConstraint.Open() :
                new Relation(boundary.Darts[0].From.Site, boundary.Darts[^1].To.Site);
            var closed = boundary.Darts.Count >= 3 && boundary.Darts.Select((dart, i) =>
                ReferenceEquals(dart.To, boundary.Darts[(i + 1) % boundary.Darts.Count].From)).All(equal => equal);
            Add("Closed", closed ? "Yes" : "Open", closurePattern,
                new Relation(boundary.Surface, closurePattern), closed ? S() : S(closurePattern));
            // Known exterior tails do not spoil an interior. A potential
            // unresolved encounter does, and a contained disconnected contour
            // obstructs the larger region even if its outer walk is chordless.
            var interiorUnknown = S(Entries(reader.Topology.EmbeddingUnknown).Concat(Entries(boundary.InteriorUnknown)).ToArray());
            Add("Elementary", Entries(boundary.InteriorObstructions).Length != 0 ? "No" : Entries(interiorUnknown).Length == 0 ? "Yes" : "Open",
                new Relation(reader.Topology.Graph.Surface, boundary.Surface),
                new Relation(boundary.Surface, new Relation(boundary.InteriorObstructions, interiorUnknown)),
                interiorUnknown);
            Shape("Triangle", 3); Shape("Four-sided", 4); Shape("Hexagon", 6);
            var angles = turns.Select((turn, i) => RightAngle(runs[i], runs[(i + 1) % runs.Count], turn.Proof)).ToArray();
            var angleAnswer = FindClaim("Four-sided").Evidence.Answer is "No" ? "No" :
                !SideCountCertain || SideCount != 4 ? "Open" : All(angles.Select(angle => angle.Answer));
            var angleProof = new Relation(reader.Topology.GeometryBasis.Surface, S(angles.Select(angle => (IElement)angle.Proof).ToArray()));
            Add("Rectangle", angleAnswer, new Relation(ShapePattern(4), reader.Topology.GeometryBasis.Surface), angleProof,
                S(angles.Where(angle => angle.Answer == "Open").Select(angle => (IElement)angle.Proof)
                    .Concat(Entries(UnresolvedSideSeams)).ToArray()));
            var equality = runs.Count < 3 || !SideCountCertain ? new Calc("Open", UnresolvedSideSeams) : EqualSides();
            Add("Equal-sided", equality.Answer, new Relation(Sides, reader.Topology.GeometryBasis.Contour.ReachSupport), equality.Proof,
                equality.Answer == "Open" ? S(equality.Proof) : S());
            var square = All([angleAnswer, equality.Answer]);
            Add("Square", square, new Relation(FindClaim("Rectangle").Predicate, FindClaim("Equal-sided").Predicate),
                new Relation(FindClaim("Rectangle").Evidence.Surface, FindClaim("Equal-sided").Evidence.Surface),
                S(Entries(FindClaim("Rectangle").Missing).Concat(Entries(FindClaim("Equal-sided").Missing)).ToArray()));
            var concavity = turns.Any(turn => turn.Sign == -1) ? "Yes" : turns.All(turn => turn.Sign is not null) ? "No" : "Open";
            var reflex = S(turns.Where(turn => turn.Sign == -1).Select(turn => (IElement)turn.Proof).ToArray());
            var reverseCycle = S(Entries((Relation)reader.Topology.Basis.Right).OfType<Relation>()
                .Select(edge => (IElement)edge.Reverse()).ToArray());
            Add("Contains concavity", concavity, new Relation(reader.Topology.Basis, reverseCycle),
                new Relation(Turns, reflex), concavity == "Open" ? S(turns.Where(turn => turn.Sign is null).Select(turn => (IElement)turn.Proof).ToArray()) : S());
            // An inset is more specific than an inward corner: two consecutive
            // reflex turns sit between outward turns and the flanking side
            // directions agree. This is a return to an outer heading; no depth
            // or midpoint measurement is needed.
            var insets = new List<IElement>();
            for (var i = 0; i < turns.Length; i++)
            {
                if (turns[i].Sign != 1 || turns[(i + 1) % turns.Length].Sign != -1 ||
                    turns[(i + 2) % turns.Length].Sign != -1 || turns[(i + 3) % turns.Length].Sign != 1) continue;
                var before = runs[i]; var after = runs[(i + 4) % runs.Count];
                if (SameDirection(before.Word, before.Sector, after.Word, after.Sector))
                    insets.Add(S(Enumerable.Range(0, 4).Select(offset =>
                        (IElement)turns[(i + offset) % turns.Length].Proof).ToArray()));
            }
            var notch = insets.Count != 0 ? "Yes" : turns.Any(turn => turn.Sign is null) ? "Open" : "No";
            var returnHeading = new Placeholder();
            Add("Contains notch", notch, new Relation(S(reverseCycle, reverseCycle), new Relation(returnHeading, returnHeading)),
                new Relation(Turns, S(insets.ToArray())), notch == "Open" ? S(Turns) : S());
            // Three consecutive alternating turns describe a zigzag. It is a
            // local boundary observation, not a claim that the whole figure is
            // a zigzag, nor that its amplitude is small decoration.
            var alternations = new List<IElement>();
            for (var i = 0; i < turns.Length; i++)
            {
                var a = turns[i]; var b = turns[(i + 1) % turns.Length]; var c = turns[(i + 2) % turns.Length];
                if (a.Sign is int first && first != 0 && b.Sign == -first && c.Sign == first)
                    alternations.Add(S(a.Proof, b.Proof, c.Proof));
            }
            var zigzag = alternations.Count != 0 ? "Yes" : turns.Any(turn => turn.Sign is null) ? "Open" : "No";
            Add("Contains zigzag", zigzag, new Relation(reader.Topology.Basis, AlternatingPattern()),
                new Relation(Turns, S(alternations.ToArray())), zigzag == "Open" ? S(Turns) : S());
            // A coarse star-like claim requires at least three separate reflex
            // turns with outward turns between them. Its fine ray geometry and
            // symmetry remain separate questions.
            var inwardSectors = turns.Select((turn, index) => (turn, index)).Where(pair => pair.turn.Sign == -1)
                .Select(pair => runs[pair.index].Sector).ToArray();
            var opposedPoints = inwardSectors.Any(sector => inwardSectors.Contains((sector + 4) % 8));
            var star = inwardSectors.Length >= 3 && alternations.Count >= 3 && opposedPoints ? "Yes" :
                turns.Any(turn => turn.Sign is null) ? "Open" : "No";
            Add("Star-like", star, new Relation(S(AlternatingPattern(), AlternatingPattern(), AlternatingPattern()), frame.Opposites),
                new Relation(reflex, new Relation(reader.Topology.Basis, S(alternations.ToArray()))), star == "Open" ? S(Turns) : S());
            Claims = claims;
            Surface = new(new Relation(reader.Topology.Port, boundary.Surface),
                new Relation(new Relation(Sides, Turns), S(claims.Select(claim => (IElement)claim.Surface).ToArray())));

            Claim FindClaim(string name) => claims.Single(claim => claim.Name == name);
            void Shape(string name, int count)
            {
                var pattern = ShapePattern(count);
                var bindings = SideCount == count ? S(runs.Select((run, i) =>
                    (IElement)new Relation(Entries(pattern)[i], run.Surface)).ToArray()) : S();
                var uncertain = Entries(UnresolvedSideSeams).Length;
                var answer = !closed ? "Open" : uncertain == 0 ? SideCount == count ? "Yes" : "No" :
                    count <= SideCount && count >= Math.Max(3, SideCount - uncertain) ? "Open" : "No";
                Add(name, answer, pattern, new Relation(new Relation(boundary.Surface, Sides), bindings),
                    !closed ? S(closurePattern) : answer == "Open" ? UnresolvedSideSeams : S());
            }
            void Add(string name, string answer, Relation required, Relation proof, Relation missing)
            {
                var predicate = new Relation(boundary.Surface, required);
                var evidence = D.Evidence.FromAnswer(predicate, answer,
                    new Relation(new Relation(reader.Topology.Port, boundary.Surface), new Relation(required, proof)));
                claims.Add(new(name, predicate, evidence, required, reader.Assumptions, missing));
            }
        }

        private Calc RightAngle(Side a, Side b, Relation turn)
        {
            var quarter = reader.Topology.GeometryBasis.ReadRightAngle(new Relation(a.Surface, b.Surface));
            var proof = new Relation(turn, quarter.Surface);
            var difference = (b.Sector - a.Sector + 8) % 8;
            var opposite = UnitArrangementRead.Inverse(frame, b.Word);
            if (SameDirection(a.Word, a.Sector, b.Word, b.Sector) ||
                SameDirection(a.Word, a.Sector, opposite, (b.Sector + 4) % 8)) return new("No", proof);
            if (quarter.Answer != "Yes") return new(quarter.Answer == "Blocked" ? "Open" : quarter.Answer, proof);
            // Cardinal/diagonal sectors exclude a quarter turn only in this
            // equal-turn geometry. In an unresolved oblique basis, a mixed
            // word can in fact be perpendicular to an axis direction.
            if ((a.Sector & 1) != (b.Sector & 1) || difference == 0 || difference == 4) return new("No", proof);
            if (difference is not (2 or 6)) return new("Open", proof);
            var rotated = Rotate(a.Word, difference == 2 ? 1 : 3);
            var expectedSector = (a.Sector + (difference == 2 ? 2 : 6)) % 8;
            return new(SameDirection(rotated, expectedSector, b.Word, b.Sector) ? "Yes" : "Open",
                new Relation(proof, new Relation(new Relation(a.Word, rotated), b.Word)));
        }

        private Calc EqualSides()
        {
            var first = runs[0]; var normalized = Align(first.Word, first.Sector); var rows = new List<IElement>(); var answers = new List<string>();
            foreach (var side in runs.Skip(1))
            {
                if ((first.Sector & 1) != (side.Sector & 1)) { answers.Add("Open"); rows.Add(new Relation(first.Surface, side.Surface)); continue; }
                var word = Align(side.Word, side.Sector); var reduction = Difference(normalized, word);
                var answer = Zero(reduction) ? "Yes" : (side.Sector & 1) == 0 && KnownSign(reduction) is not null ? "No" : "Open";
                answers.Add(answer); rows.Add(new Relation(new Relation(first.Surface, side.Surface),
                    new Relation(new Relation(normalized, word), reduction)));
            }
            // A quarter-turn of a diagonal sum is length preserving only under
            // the actual equal-turn model. Common radial reach alone makes
            // axis-aligned reaches comparable; it does not make an oblique
            // rhombus's two diagonals equal.
            if (runs.Any(side => (side.Sector & 1) != 0))
            {
                var quarter = reader.Topology.GeometryBasis.ReadRightAngle(new Relation(Sides, reader.Topology.GeometryBasis.Contour.Surface));
                rows.Add(quarter.Surface); answers.Add(quarter.Answer == "Yes" ? "Yes" : "Open");
            }
            return new(All(answers), new Relation(reader.Topology.GeometryBasis.Surface, S(rows.ToArray())));
        }

        private Turn ReadTurn(Side a, Side b)
        {
            var difference = (b.Sector - a.Sector + 8) % 8;
            var local = difference == 0 ? WithinSectorTurn(a, b) : null;
            int? sign = difference is > 0 and < 4 ? 1 : difference is > 4 and < 8 ? -1 :
                difference == 0 && SameDirection(a.Word, a.Sector, b.Word, b.Sector) ? 0 : local?.Sign;
            var cycle = Entries((Relation)reader.Topology.Basis.Right).OfType<Relation>().ToArray();
            var path = new List<IElement>();
            if (sign is 1 or -1)
                for (var index = a.Sector; index != b.Sector; index = (index + (sign == 1 ? 1 : 7)) % 8)
                    path.Add(sign == 1 ? cycle[index] : cycle[(index + 7) % 8].Reverse());
            return new(sign, new Relation(new Relation(a.Surface, b.Surface),
                new Relation(reader.Topology.Basis, new Relation(sign is null ? StructuralConstraint.Open() : S(path.ToArray()),
                    local?.Proof ?? S()))));
        }

        private Turn WithinSectorTurn(Side a, Side b)
        {
            // Rotate the quadrant into the first positive cone. If one positive
            // component grows and the other shrinks, its slope order follows
            // without computing either ratio. If both change in the same
            // direction, this partial-order read deliberately stays open.
            var first = Align(a.Word, a.Sector); var second = Align(b.Word, b.Sector);
            Relation Component(Relation word, Relation axis) => S(Entries(word).OfType<Relation>()
                .Where(move => frame.Direction(move, out var actual, out _, out _) && ReferenceEquals(actual, axis))
                .Select(move => (IElement)QualitativeMotionFrame.Move(frame.Right, move.Right)).ToArray());
            var horizontal = Difference(Component(second, frame.Horizontal), Component(first, frame.Horizontal));
            var vertical = Difference(Component(second, frame.Vertical), Component(first, frame.Vertical));
            var x = KnownSign(horizontal); var y = KnownSign(vertical);
            int? sign = x is >= 0 && y is <= 0 && (x > 0 || y < 0) ? -1 :
                x is <= 0 && y is >= 0 && (x < 0 || y > 0) ? 1 : x == 0 && y == 0 ? 0 : null;
            return new(sign, new Relation(new Relation(first, second), new Relation(horizontal, vertical)));
        }

        private bool SameDirection(Relation a, int aSector, Relation b, int bSector)
        {
            if (aSector != bSector) return false;
            if ((aSector & 1) == 0) return true;
            return Zero(Difference(a, b)) || Balanced(a) && Balanced(b);
        }

        private bool Balanced(Relation word)
        {
            var horizontal = new List<IElement>(); var vertical = new List<IElement>();
            foreach (var item in Entries(word))
            {
                if (item is not Relation move || !frame.Direction(move, out var axis, out _, out var reach)) return false;
                (ReferenceEquals(axis, frame.Horizontal) ? horizontal : vertical).Add(QualitativeMotionFrame.Move(frame.Right, reach));
            }
            return horizontal.Count != 0 && vertical.Count != 0 && Zero(Difference(S(horizontal.ToArray()), S(vertical.ToArray())));
        }
        private Relation Align(Relation word, int sector) => Rotate(word, (8 - (sector & ~1)) / 2 % 4);
        private Relation Rotate(Relation word, int quarters)
        {
            IElement[] directions = [frame.Right, frame.Up, frame.Left, frame.Down];
            return S(Entries(word).Select(item => item is Relation move && frame.Direction(move, out _, out _, out var reach)
                ? (IElement)QualitativeMotionFrame.Move(directions[(Array.FindIndex(directions, direction => ReferenceEquals(direction, move.Left)) + quarters) % 4], reach)
                : item).ToArray());
        }
        private Relation Difference(Relation a, Relation b) => frame.Reduce(a, UnitArrangementRead.Inverse(frame, b));
        private bool Zero(Relation reduction) => frame.TryReadResult(reduction, out var remaining, out _, out var pending) &&
            Closed(remaining) && Entries(pending).Length == 0 && Entries(remaining).Length == 0;
        private int? KnownSign(Relation reduction)
        {
            if (!frame.TryReadResult(reduction, out var remaining, out _, out var pending) || !Closed(remaining) || Entries(pending).Length != 0) return null;
            var moves = Entries(remaining).OfType<Relation>().ToArray();
            if (moves.Length == 0) return 0;
            return moves.All(move => ReferenceEquals(move.Left, frame.Right)) ? 1 : moves.All(move => ReferenceEquals(move.Left, frame.Left)) ? -1 : null;
        }
    }

    private sealed class Side(NativeInteriorTopology.Dart dart)
    {
        public List<NativeInteriorTopology.Dart> Darts { get; } = [dart];
        public int Sector { get; } = dart.Sector;
        public Relation Word { get; private set; } = dart.Word;
        public Relation Surface => new(S(Darts.Select(item => (IElement)item.Surface).ToArray()),
            new Relation(new Relation(Darts[0].From.Site, Darts[^1].To.Site), Word));
        public void Append(NativeInteriorTopology.Dart next, QualitativeMotionFrame frame)
        {
            Darts.Add(next); var reduction = frame.Reduce(Word, next.Word);
            if (frame.TryReadResult(reduction, out var remaining, out _, out _)) Word = remaining;
        }
    }
    private sealed record Calc(string Answer, Relation Proof);
    private sealed record Turn(int? Sign, Relation Proof);
    private static string All(IEnumerable<string> answers)
    {
        var values = answers.ToArray();
        return values.Contains("No") ? "No" : values.Any(answer => answer != "Yes") ? "Open" : "Yes";
    }
    private static Relation ShapePattern(int count)
    {
        var vertices = Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
        return S(vertices.Select((vertex, i) => (IElement)new Relation(vertex, vertices[(i + 1) % count])).ToArray());
    }
    private static Relation AlternatingPattern()
    {
        var a = new Placeholder(); var b = new Placeholder();
        return S(new Relation(a, b), new Relation(b, a), new Relation(a, b));
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] Entries(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var head, out var tail) && head.Equals(tail);
}
