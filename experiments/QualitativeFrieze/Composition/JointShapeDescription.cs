using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A deliberately coarse, independent communication surface. It
/// exposes local ray roles, ordinal projections and action correspondences;
/// the source edge pairing, position words and analysis ancestry are forgotten.</summary>
public static class JointShapeDescription
{
    // message = ((frame -> geometry basis) -> Closed(joint roles)) ->
    //   ((Closed(role -> (ray inventory -> (opposed -> perpendicular))) ->
    //      (horizontal ordered buckets -> vertical ordered buckets)) ->
    //    ((Closed(symmetry clauses) -> Closed(component role scopes)) ->
    //      (closure default -> (ordered interior turns -> open obligations))))
    // ray inventory = Closed/Open(ray role -> direction-only scope)
    // symmetry = direction substitution -> (role correspondence ->
    //            (unmatched roles -> (missing slots -> geometry obligation)))
    // An input stroke, its partner endpoint and its reach NEVER occur here.
    public sealed class Message
    {
        public QualitativeMotionFrame Frame { get; }
        public NativeLineBasis Basis { get; }
        public Relation Roles { get; }
        public Relation Joints { get; }
        public Relation Horizontal { get; }
        public Relation Vertical { get; }
        public Relation Symmetries { get; }
        public Relation Components { get; }
        public Relation ClosureDefault { get; }
        public Relation InteriorTurns { get; }
        public Relation Obligations { get; }
        public Relation Surface { get; }
        public IReadOnlyList<Joint> JointReads { get; }

        internal Message(QualitativeMotionFrame frame, NativeLineBasis basis, Relation roles, Relation joints,
            Relation horizontal, Relation vertical, Relation symmetries, Relation components,
            Relation closure, Relation turns, Relation obligations, Relation? actual = null)
        {
            Frame = frame; Basis = basis; Roles = roles; Joints = joints; Horizontal = horizontal; Vertical = vertical;
            Symmetries = symmetries; Components = components; ClosureDefault = closure; InteriorTurns = turns; Obligations = obligations;
            Surface = actual ?? new(new Relation(new Relation(frame.Surface, basis.Surface), roles),
                new Relation(new Relation(joints, new Relation(horizontal, vertical)),
                    new Relation(new Relation(symmetries, components), new Relation(closure, new Relation(turns, obligations)))));
            JointReads = E(joints).Cast<Relation>().Select(row => new Joint(row)).ToArray();
        }
    }

    public sealed class Joint
    {
        public IElement Role { get; }
        public Relation Rays { get; }
        public Relation Opposed { get; }
        public Relation Perpendicular { get; }
        public Relation Surface { get; }
        public string Kind => (JointNative.Closed(Rays) ? "" : "observed prefix: ") + (E(Rays).Length switch {
            0 => "isolated", 1 => "open endpoint", 2 => E(Opposed).Length != 0 ? "straight continuation" : "corner",
            3 => E(Opposed).Length != 0 ? "T junction" : "three-arm branch / Y-like", 4 => E(Opposed).Length == 2 ? "four-way joint" : "four-arm joint", _ => "branch"
        });
        internal Joint(Relation row)
        {
            Surface = row; Role = row.Left;
            var body = (Relation)row.Right; Rays = (Relation)body.Left;
            var pairs = (Relation)body.Right; Opposed = (Relation)pairs.Left; Perpendicular = (Relation)pairs.Right;
        }
    }

    public sealed record Analysis(NativeLatticeSources.SourceCase Source, Message Message,
        JointSymmetryEvidence.Read Symmetry, Relation SourceMap, Relation Audit);

    public static Analysis Analyze(NativeLatticeSources.SourceCase source)
    {
        var oldFrame = source.Graph.Frame; var frame = new QualitativeMotionFrame();
        var basis = new NativeLineBasis(frame, source.TurnModel);
        var actualSites = E(source.Graph.Sites); var roles = actualSites.Select(_ => (IElement)new Placeholder()).ToArray();
        var mapping = actualSites.Select((site, index) => (site, role: roles[index])).ToDictionary(pair => pair.site, pair => pair.role, N.Participants.Instance);
        var sourceMap = S(actualSites.Select((site, index) => (IElement)new Relation(site, roles[index])).ToArray());
        var joints = new List<IElement>(); var audits = new List<IElement>(); var obligations = new List<IElement>();
        source.Graph.TryReadResult(source.Graph.Evaluate(), out var nativeJoints, out _, out _, out _);
        foreach (var actualSite in actualSites)
        {
            var raw = E(nativeJoints).Cast<Relation>().Single(row => ReferenceEquals(((Relation)row.Left).Right, actualSite));
            source.Graph.TryReadJunction(raw, out _, out var nativeRays, out var opposite, out _, out var unresolved);
            var rayMap = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
            var rays = new List<IElement>(); var sourceWords = new List<Relation>(); var original = E(nativeRays).Cast<Relation>().ToArray();
            foreach (var ray in original)
            {
                var word = (Relation)((Relation)ray.Right).Right;
                var straight = E(source.StraightPassages).Any(stroke => ReferenceEquals(stroke, ray.Left));
                var direction = straight ? JointNative.Heading(oldFrame, word, frame) : StructuralConstraint.Open();
                var slot = new Placeholder(); rayMap.Add(ray, slot); rays.Add(new Relation(slot, direction)); sourceWords.Add(word);
                if (!straight || !JointNative.Closed(direction)) obligations.Add(new Relation(mapping[actualSite], slot));
            }
            var straightRays = original.Where(ray => E(source.StraightPassages).Any(stroke => ReferenceEquals(stroke, ray.Left)))
                .ToHashSet<Relation>(ReferenceEqualityComparer.Instance);
            var opposed = E(opposite).OfType<Relation>().Where(pair => pair.Left is Relation ends && rayMap.ContainsKey(ends.Left) && rayMap.ContainsKey(ends.Right) &&
                    ends.Left is Relation a && straightRays.Contains(a) && ends.Right is Relation b && straightRays.Contains(b))
                .Select(pair => { var ends = (Relation)pair.Left; return (IElement)new Relation(rayMap[ends.Left], rayMap[ends.Right]); }).ToArray();
            var right = new List<IElement>();
            if (source.TurnModel)
                for (var i = 0; i < sourceWords.Count; i++) for (var j = i + 1; j < sourceWords.Count; j++)
                    if (E(source.StraightPassages).Any(stroke => ReferenceEquals(stroke, original[i].Left)) &&
                        E(source.StraightPassages).Any(stroke => ReferenceEquals(stroke, original[j].Left)) &&
                        JointNative.Perpendicular(oldFrame, sourceWords[i], sourceWords[j]))
                        right.Add(new Relation(rayMap[original[i]], rayMap[original[j]]));
            var rayInventory = JointNative.Closed(source.Graph.Strokes) ? S(rays.ToArray()) : StructuralConstraint.Open(rays.ToArray());
            var opposedScope = JointNative.Closed(opposite) && original.All(straightRays.Contains) ? S(opposed) : StructuralConstraint.Open(opposed);
            var row = new Relation(mapping[actualSite], new Relation(rayInventory, new Relation(opposedScope, S(right.ToArray()))));
            joints.Add(row); audits.Add(new Relation(raw, row));
            if (!JointNative.Closed(rayInventory)) obligations.Add(new Relation(mapping[actualSite], StructuralConstraint.Open()));
            if (E(unresolved).Length != 0) obligations.Add(new Relation(mapping[actualSite], StructuralConstraint.Open()));
            if (rays.Count == 1) obligations.Add(new Relation(mapping[actualSite], StructuralConstraint.Open()));
        }
        var h = Buckets(oldFrame.Horizontal, frame.Horizontal); var v = Buckets(oldFrame.Vertical, frame.Vertical);
        var symmetry = JointSymmetryEvidence.Analyze(source);
        var clauses = new List<IElement>();
        foreach (var operation in symmetry.Operations.Where(operation => operation.NonTrivial && operation.MatchedEdges > 0))
        {
            var dirs = S(E(operation.DirectionMap).Cast<Relation>().Select(row => (IElement)new Relation(
                JointNative.MapDirection(oldFrame, frame, row.Left), JointNative.MapDirection(oldFrame, frame, row.Right))).ToArray());
            var pairs = S(E(operation.SiteMap).Cast<Relation>().Where(row => mapping.ContainsKey(row.Left) && mapping.ContainsKey(row.Right))
                .Select(row => (IElement)new Relation(mapping[row.Left], mapping[row.Right])).ToArray());
            // Keep a known defect's joint location without retaining its
            // position word or an unobserved edge pairing. Passage exceptions
            // without such a role association remain anonymous obligations.
            IElement Exception(IElement item) => mapping.TryGetValue(item, out var role) ? new Relation(role, new Placeholder()) :
                item is Relation row && mapping.TryGetValue(row.Left, out var at) ? new Relation(at, new Placeholder()) : new Placeholder();
            var unmatched = S(E(operation.Unmatched).Select(Exception).ToArray());
            var missing = S(E(operation.Missing).Select(Exception).ToArray());
            var pending = operation.Exact ? S() : StructuralConstraint.Open();
            clauses.Add(new Relation(dirs, new Relation(pairs, new Relation(unmatched, new Relation(missing, pending)))));
        }
        var componentRows = symmetry.Components.Select(component => (IElement)S(E(component.Sites).Where(mapping.ContainsKey).Select(site => mapping[site]).ToArray())).ToArray();
        var components = E(symmetry.Unresolved).Length == 0 ? S(componentRows) : StructuralConstraint.Open(componentRows);
        var turns = new List<IElement>();
        var topology = new NativeInteriorTopology(source, 64);
        var publishedToOriginal = E(topology.Publication.SiteMap).Cast<Relation>().ToDictionary(row => row.Right, row => row.Left, N.Participants.Instance);
        foreach (var boundary in topology.Boundaries)
        {
            var context = new Placeholder();
            for (var index = 0; index < boundary.Darts.Count; index++)
            {
                var at = boundary.Darts[index]; var before = boundary.Darts[(index + boundary.Darts.Count - 1) % boundary.Darts.Count];
                var incoming = JointNative.Heading(oldFrame, before.Word, frame); var outgoing = JointNative.Heading(oldFrame, at.Word, frame);
                foreach (var publicSite in E(at.From.SourceSites))
                    if (publishedToOriginal.TryGetValue(publicSite, out var originalSite) && mapping.TryGetValue(originalSite, out var role))
                    {
                        turns.Add(new Relation(new Relation(role, context), new Relation(incoming, outgoing)));
                    }
            }
        }
        var start = new Placeholder(); var end = new Placeholder(); var pathSlot = new Placeholder();
        var closure = new Relation(new Relation(pathSlot, new Relation(start, end)), new Relation(end, start));
        var message = new Message(frame, basis, S(roles), S(joints.ToArray()), h, v, S(clauses.ToArray()), components,
            closure, S(turns.ToArray()), S(obligations.ToArray()));
        var audit = new Relation(new Relation(source.Graph.Surface, source.Bindings), new Relation(message.Surface,
            new Relation(sourceMap, new Relation(S(audits.ToArray()), new Relation(topology.Surface, symmetry.Surface)))));
        return new(source, message, symmetry, sourceMap, audit);

        Relation Buckets(Relation oldAxis, Relation newAxis)
        {
            var positions = actualSites.ToDictionary(site => site, site => E(source.Bindings).OfType<Relation>().FirstOrDefault(row => ReferenceEquals(row.Left, site))?.Right as Relation, N.Participants.Instance);
            var buckets = new List<List<IElement>>();
            var uncertain = false;
            foreach (var site in actualSites)
            {
                if (positions[site] is not Relation position || JointNative.Compare(oldFrame, oldAxis, position, position) is null) { uncertain = true; obligations.Add(new Relation(mapping[site], StructuralConstraint.Open())); continue; }
                var inserted = false; var unavailable = false;
                for (var index = 0; index < buckets.Count; index++)
                {
                    var order = JointNative.Compare(oldFrame, oldAxis, position, positions[buckets[index][0]]!);
                    if (order is null) { uncertain = true; unavailable = true; obligations.Add(new Relation(mapping[site], mapping[buckets[index][0]])); break; }
                    if (order == 0) { buckets[index].Add(site); inserted = true; break; }
                    if (order < 0) { buckets.Insert(index, [site]); inserted = true; break; }
                }
                if (!inserted && !unavailable) buckets.Add([site]);
            }
            var entries = buckets.Select(bucket => (IElement)S(bucket.Select(site => mapping[site]).ToArray())).ToArray();
            return new(newAxis, uncertain ? StructuralConstraint.Open(entries) : S(entries));
        }
    }

    public static bool TryRead(Relation port, out Message message, out Relation witness)
    {
        message = null!; witness = new(port, StructuralConstraint.Open());
        if (port is not { Left: Relation { Left: Relation { Left: Relation rawFrame } } } ||
            !QualitativeMotionFrameRead.TryReadStandard(rawFrame, out var frame) || !frame.TryRebind(port, out var imported, out var rebind) ||
            imported is not Relation actual || actual.Left is not Relation { Left: Relation { Right: Relation rawBasis }, Right: Relation roles } ||
            !NativeLineBasis.TryReadInFrame(frame.Target, rawBasis, out var basis, out _) ||
            actual.Right is not Relation { Left: Relation { Left: Relation joints, Right: Relation { Left: Relation h, Right: Relation v } },
                Right: Relation { Left: Relation { Left: Relation symmetries, Right: Relation components },
                    Right: Relation { Left: Relation closure, Right: Relation { Left: Relation turns, Right: Relation obligations } } } }) return false;
        var roleItems = E(roles);
        if (!JointNative.Closed(roles) || !JointNative.Closed(joints) || roleItems.Any(role => role is not Placeholder) ||
            !Unique(roleItems) || E(joints).Length != roleItems.Length) return false;
        var seenJoints = new HashSet<IElement>(N.Participants.Instance);
        var allRaySlots = new HashSet<IElement>(N.Participants.Instance);
        foreach (var row in E(joints))
        {
            if (row is not Relation joint || !IsRole(joint.Left) || !seenJoints.Add(joint.Left) ||
                joint.Right is not Relation { Left: Relation rays, Right: Relation { Left: Relation opposed, Right: Relation right } } ||
                !Scope(rays) || !Scope(opposed) || !JointNative.Closed(right)) return false;
            var slots = new HashSet<IElement>(N.Participants.Instance);
            foreach (var item in E(rays))
                if (item is not Relation { Left: Placeholder slot, Right: Relation direction } || IsRole(slot) ||
                    !slots.Add(slot) || !allRaySlots.Add(slot) || !Heading(direction)) return false;
            if (!RayPairs(opposed, slots) || !RayPairs(right, slots)) return false;
        }
        if (!ReferenceEquals(h.Left, frame.Target.Horizontal) || !ReferenceEquals(v.Left, frame.Target.Vertical)) return false;
        foreach (var order in new[] { h, v })
        {
            if (order.Right is not Relation buckets || !Scope(buckets) ||
                E(buckets).Any(bucket => bucket is not Relation scope || !JointNative.Closed(scope) || E(scope).Length == 0)) return false;
            var members = E(buckets).Cast<Relation>().SelectMany(E).ToArray();
            if (!Unique(members) || members.Any(role => !IsRole(role)) ||
                JointNative.Closed(buckets) && members.Length != roleItems.Length) return false;
        }
        if (!Scope(symmetries) || !Scope(components) || !Scope(turns) || !Scope(obligations)) return false;
        var directions = new IElement[] { frame.Target.Right, frame.Target.Up, frame.Target.Left, frame.Target.Down };
        foreach (var item in E(symmetries))
        {
            if (item is not Relation { Left: Relation action,
                Right: Relation { Left: Relation pairs, Right: Relation { Left: Relation unmatched,
                    Right: Relation { Left: Relation missing, Right: Relation pending } } } } ||
                !JointNative.Closed(action) || E(action).Length != directions.Length ||
                E(action).Any(entry => entry is not Relation map || !directions.Contains(map.Left, N.Participants.Instance) || !directions.Contains(map.Right, N.Participants.Instance)) ||
                !Unique(E(action).Cast<Relation>().Select(map => map.Left)) || !Unique(E(action).Cast<Relation>().Select(map => map.Right)) ||
                !PreservesOpposites(action) ||
                !Scope(pairs) || E(pairs).Any(entry => entry is not Relation pair || !IsRole(pair.Left) || !IsRole(pair.Right)) ||
                !Unique(E(pairs).Cast<Relation>().Select(pair => pair.Left)) || !Unique(E(pairs).Cast<Relation>().Select(pair => pair.Right)) ||
                !OpaqueSlots(unmatched) || !OpaqueSlots(missing) || !Scope(pending)) return false;
        }
        var componentMembers = new List<IElement>(); var completeComponents = JointNative.Closed(components);
        foreach (var item in E(components))
        {
            if (item is not Relation component || !Scope(component) || E(component).Any(role => !IsRole(role))) return false;
            completeComponents &= JointNative.Closed(component);
            componentMembers.AddRange(E(component));
        }
        if (!Unique(componentMembers) || completeComponents && componentMembers.Count != roleItems.Length) return false;
        var contexts = new HashSet<IElement>(N.Participants.Instance);
        var turnParticipants = new List<Relation>();
        foreach (var item in E(turns))
        {
            if (item is not Relation { Left: Relation { Left: var role, Right: Placeholder context },
                Right: Relation { Left: Relation incoming, Right: Relation outgoing } } ||
                !IsRole(role) || IsRole(context) || allRaySlots.Contains(context) ||
                turnParticipants.Any(prior => ReferenceEquals(prior.Left, role) && ReferenceEquals(prior.Right, context)) ||
                !Heading(incoming) || !Heading(outgoing)) return false;
            contexts.Add(context); turnParticipants.Add(new Relation(role, context));
        }
        if (closure is not { Left: Relation { Left: Placeholder path, Right: Relation { Left: Placeholder start, Right: Placeholder end } },
                Right: Relation close } || !ReferenceEquals(close.Left, end) || !ReferenceEquals(close.Right, start) ||
            ReferenceEquals(start, end) || ReferenceEquals(path, start) || ReferenceEquals(path, end) ||
            new IElement[] { path, start, end }.Any(slot => IsRole(slot) || allRaySlots.Contains(slot) || contexts.Contains(slot))) return false;
        if (Reachable(actual).Any(item => item is Carrier or CarrierLocation)) return false;
        message = new(frame.Target, basis, roles, joints, h, v, symmetries, components, closure, turns, obligations, actual);
        witness = new(port, new Relation(rebind, actual)); return true;

        bool IsRole(IElement item) => roleItems.Contains(item, N.Participants.Instance);
        static bool Unique(IEnumerable<IElement> items)
        { var seen = new HashSet<IElement>(N.Participants.Instance); return items.All(seen.Add); }
        static bool Scope(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out _, out _);
        bool OpaqueSlots(Relation scope) => Scope(scope) && E(scope).All(item =>
            item is Placeholder && !IsRole(item) || item is Relation row && IsRole(row.Left) && row.Right is Placeholder && !IsRole(row.Right)) && Unique(E(scope));
        bool Heading(Relation heading)
        {
            if (!Scope(heading)) return false;
            var items = E(heading);
            if (!Unique(items) || items.Length > 2 || JointNative.Closed(heading) && items.Length == 0 ||
                items.Any(item => !new IElement[] { frame.Target.Right, frame.Target.Up, frame.Target.Left, frame.Target.Down }.Contains(item, N.Participants.Instance))) return false;
            return items.Length != 2 || !E(frame.Target.Opposites).Cast<Relation>().Any(pair =>
                ReferenceEquals(pair.Left, items[0]) && ReferenceEquals(pair.Right, items[1]));
        }
        bool PreservesOpposites(Relation action)
        {
            var maps = E(action).Cast<Relation>().ToArray();
            return E(frame.Target.Opposites).Cast<Relation>().All(pair =>
            {
                var a = maps.Single(map => ReferenceEquals(map.Left, pair.Left)).Right;
                var b = maps.Single(map => ReferenceEquals(map.Left, pair.Right)).Right;
                return E(frame.Target.Opposites).Cast<Relation>().Any(opposite => ReferenceEquals(opposite.Left, a) && ReferenceEquals(opposite.Right, b));
            });
        }
        static bool RayPairs(Relation pairs, HashSet<IElement> slots)
        {
            var seen = new List<Relation>();
            foreach (var item in E(pairs))
            {
                if (item is not Relation pair || !slots.Contains(pair.Left) || !slots.Contains(pair.Right) || ReferenceEquals(pair.Left, pair.Right) ||
                    seen.Any(prior => ReferenceEquals(prior.Left, pair.Left) && ReferenceEquals(prior.Right, pair.Right) ||
                        ReferenceEquals(prior.Left, pair.Right) && ReferenceEquals(prior.Right, pair.Left))) return false;
                seen.Add(pair);
            }
            return true;
        }
        static IEnumerable<IElement> Reachable(IElement root)
        {
            var seen = new HashSet<IElement>(N.Participants.Instance); var pending = new Stack<IElement>(); pending.Push(root);
            while (pending.TryPop(out var item))
            {
                if (!seen.Add(item)) continue;
                yield return item;
                if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
            }
        }
    }

    internal static IElement[] E(Relation scope) => StructuralPotential.Items(scope);
    internal static Relation S(params IElement[] rows) => StructuralConstraint.Closed(rows);
}

internal static class JointNative
{
    public static Relation S(params IElement[] rows) => StructuralConstraint.Closed(rows);
    public static IElement[] E(Relation scope) => StructuralPotential.Items(scope);
    public static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var a, out var b) && a.Equals(b);
    public static bool Zero(QualitativeMotionFrame frame, Relation read) => frame.TryReadResult(read, out var left, out _, out var open) && Closed(left) && E(left).Length == 0 && E(open).Length == 0;
    public static Relation Inverse(QualitativeMotionFrame frame, Relation word) => UnitArrangementRead.Inverse(frame, word);
    public static Relation Difference(QualitativeMotionFrame frame, Relation a, Relation b)
    { frame.TryReadResult(frame.Reduce(a, Inverse(frame, b)), out var left, out _, out _); return left; }
    public static IElement MapDirection(QualitativeMotionFrame old, QualitativeMotionFrame fresh, IElement direction) =>
        ReferenceEquals(direction, old.Right) ? fresh.Right : ReferenceEquals(direction, old.Up) ? fresh.Up : ReferenceEquals(direction, old.Left) ? fresh.Left : fresh.Down;
    public static Relation Heading(QualitativeMotionFrame frame, Relation word, QualitativeMotionFrame? target = null)
    {
        if (!frame.TryReadResult(frame.Reduce(S(), word), out var left, out _, out var open) || !Closed(left) || E(open).Length != 0) return StructuralConstraint.Open();
        var directions = E(left).OfType<Relation>().Select(move => move.Left).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
        if (directions.Length is < 1 or > 2 || directions.Any(direction => !new IElement[] { frame.Right, frame.Up, frame.Left, frame.Down }.Contains(direction, ReferenceEqualityComparer.Instance))) return StructuralConstraint.Open();
        return S(directions.Select(direction => MapDirection(frame, target ?? frame, direction)).ToArray());
    }
    public static int? Compare(QualitativeMotionFrame frame, Relation axis, Relation a, Relation b)
    {
        Relation Project(Relation word) => S(E(word).OfType<Relation>().Where(move => frame.Direction(move, out var actual, out _, out _) && ReferenceEquals(axis, actual)).Cast<IElement>().ToArray());
        bool Known(Relation word) => Closed(word) && E(word).All(item => item is Relation move && frame.Direction(move, out _, out _, out _));
        if (!Known(a) || !Known(b)) return null;
        var diff = frame.Reduce(Project(a), Inverse(frame, Project(b)));
        if (!frame.TryReadResult(diff, out var remaining, out _, out var open) || E(open).Length != 0 || !Closed(remaining)) return null;
        var flags = E(remaining).OfType<Relation>().Select(move => { frame.Direction(move, out _, out var forward, out _); return forward; }).Distinct().ToArray();
        return flags.Length == 0 ? 0 : flags.Length == 1 ? flags[0] ? 1 : -1 : null;
    }
    public static Relation Rotate(QualitativeMotionFrame frame, Relation word)
    {
        IElement[] source = [frame.Right, frame.Up, frame.Left, frame.Down];
        return S(E(word).OfType<Relation>().Select(move => (IElement)QualitativeMotionFrame.Move(source[(Array.FindIndex(source, direction => ReferenceEquals(direction, move.Left)) + 1) % 4], move.Right)).ToArray());
    }
    public static bool Perpendicular(QualitativeMotionFrame frame, Relation a, Relation b)
    {
        var x = Heading(frame, a); var y = Heading(frame, b); var sx = Sector(frame, x); var sy = Sector(frame, y);
        if (sx >= 0 && sy >= 0 && (sx & 1) == 0 && (sy & 1) == 0 && (sy - sx + 8) % 8 is 2 or 6) return true;
        return Zero(frame, frame.Reduce(Rotate(frame, a), Inverse(frame, b))) || Zero(frame, frame.Reduce(Rotate(frame, a), b));
    }
    public static int Sector(QualitativeMotionFrame frame, Relation heading)
    {
        var dirs = E(heading); bool Has(IElement direction) => dirs.Contains(direction, ReferenceEqualityComparer.Instance);
        if (dirs.Length == 1) return Has(frame.Right) ? 0 : Has(frame.Up) ? 2 : Has(frame.Left) ? 4 : Has(frame.Down) ? 6 : -1;
        return Has(frame.Right) && Has(frame.Up) ? 1 : Has(frame.Left) && Has(frame.Up) ? 3 : Has(frame.Left) && Has(frame.Down) ? 5 : Has(frame.Right) && Has(frame.Down) ? 7 : -1;
    }
    public static string HeadingName(QualitativeMotionFrame frame, Relation heading) => Sector(frame, heading) switch { 0 => "right", 1 => "up + right", 2 => "up", 3 => "up + left", 4 => "left", 5 => "down + left", 6 => "down", 7 => "down + right", _ => "unresolved tangent" };
}
