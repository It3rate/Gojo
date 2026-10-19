using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Local native path surgery. A refinement promises unchanged chord
/// endpoints; a cut or fusion instead records explicitly removed participation
/// and changed endpoint publications. No helper chooses a pattern by its name,
/// infers a contact from screen coordinates, or silently rewrites old rungs.</summary>
public static partial class LatticeGeometrySurgery
{
    /// <summary>Refines actual selected chord identities, then publishes the
    /// newly introduced actual vertices in a separate carried-path edit. Old
    /// published roles remain stable through the native refinement mapping.
    /// The returned ladder includes both rungs and their endpoint proofs.</summary>
    public static QualitativeLatticeLadder RefineAndPublish(QualitativeLatticeLadder source,
        string name, Relation actualChordToChainRows, IElement? evidence = null)
    {
        var refined = source.Refine(name, actualChordToChainRows);
        var program = refined.Current.Program;
        var ports = CompletePorts(program.Paths, program.Ports);
        return refined.ReplacePaths(name + " / publish new landmarks", N.S(), N.S(), ports, program.ClosedPaths,
            new Relation(new Relation(source.Surface, actualChordToChainRows), evidence ?? N.S()));
    }

    /// <summary>Builds a five-part detour: lead, offset, middle, inverse offset,
    /// trail. Zero lead/trail disappear; offset and middle must be nonzero.
    /// The actual chord's formal displacement must equal lead+middle+trail.
    /// The caller selects the offset through its actual native word; no angle
    /// or measured notch depth is inferred.</summary>
    public static Relation NotchRow(QualitativeMotionFrame frame, Relation actualChord,
        Relation lead, Relation offset, Relation middle, Relation trail)
    {
        RequireWord(frame, actualChord); RequireWord(frame, lead); RequireWord(frame, offset);
        RequireWord(frame, middle); RequireWord(frame, trail);
        if (Zero(frame, offset) || Zero(frame, middle)) throw new ArgumentException("A notch needs a nonzero offset and middle run.");
        var net = Flatten(lead, middle, trail);
        RequireSameDisplacement(frame, actualChord, net);
        var chain = new[] { lead, offset, middle, UnitArrangementRead.Inverse(frame, offset), trail }
            .Where(word => !Zero(frame, word)).Cast<IElement>().ToArray();
        return new(actualChord, N.S(chain));
    }

    /// <summary>The opposite boundary traverses an actual chain in reverse
    /// order with each chord displacement inverted. Reversing only the group
    /// order or only a flow face does not preserve this correspondence.</summary>
    public static Relation InverseChain(QualitativeMotionFrame frame, Relation chain)
    {
        if (!N.Read(chain, out var words) || words.Any(item => item is not Relation))
            throw new ArgumentException("An inverse chain requires a closed sequence of chord words.", nameof(chain));
        foreach (var word in words.Cast<Relation>()) RequireWord(frame, word);
        return N.S(words.Reverse().Cast<Relation>().Select(word => (IElement)UnitArrangementRead.Inverse(frame, word)).ToArray());
    }

    /// <summary>Removes the selected paths and their public roles. Carried
    /// paths retain their identities and publication roles. Deleting the last
    /// path remains a blocked native edit because the current unit contract
    /// requires a nonempty executable path program.</summary>
    public static QualitativeLatticeLadder DeletePaths(QualitativeLatticeLadder source,
        string name, Relation actualSelectedPaths, IElement? evidence = null)
    {
        if (!N.Read(actualSelectedPaths, out var selected))
            return source.ReplacePaths(name, actualSelectedPaths, N.S(), source.Current.Program.Ports,
                source.Current.Program.ClosedPaths, evidence);
        var program = source.Current.Program;
        var carried = N.E(program.Paths).Where(path => !selected.Any(actual => ReferenceEquals(actual, path))).ToArray();
        var ports = N.S(N.E(program.Ports).OfType<Relation>().Where(port => port.Right is Relation pick &&
            carried.Any(path => ReferenceEquals(path, pick.Left))).Cast<IElement>().ToArray());
        var closed = N.S(N.E(program.ClosedPaths).Where(path => carried.Any(actual => ReferenceEquals(actual, path))).ToArray());
        return source.ReplacePaths(name, actualSelectedPaths, N.S(), ports, closed,
            new Relation(new Relation(program.Surface, actualSelectedPaths), evidence ?? N.S()));
    }

    /// <summary>Cuts an explicit middle span out of one actual chord. Two new
    /// open paths retain the visible prefix and suffix; their starts and the
    /// removed gap remain connected to the actual old path through native
    /// reduction witnesses. This is a deletion, not a refinement claim.</summary>
    public static QualitativeLatticeLadder CutChordAndPublish(QualitativeLatticeLadder source,
        string name, Relation actualPath, Relation actualChord, Relation lead, Relation removedGap, Relation trail)
    {
        var program = source.Current.Program; var frame = program.Frame;
        var words = Select(program, actualPath);
        var occurrences = words.Select((word, index) => (word, index)).Where(item => ReferenceEquals(item.word, actualChord)).ToArray();
        if (occurrences is not [var selected]) throw new ArgumentException("A cut selects one actual chord occurrence in its actual path.");
        RequireWord(frame, lead); RequireWord(frame, removedGap); RequireWord(frame, trail);
        if (Zero(frame, removedGap)) throw new ArgumentException("A cut requires an explicitly removed nonzero span.", nameof(removedGap));
        var decomposition = Flatten(lead, removedGap, trail);
        var endpointRead = RequireSameDisplacement(frame, actualChord, decomposition);
        var prefix = words.Take(selected.index).Concat(Zero(frame, lead) ? [] : new[] { lead }).ToArray();
        var suffix = (Zero(frame, trail) ? [] : new[] { trail }).Concat(words.Skip(selected.index + 1)).ToArray();
        var nextStartRead = frame.Reduce((Relation)actualPath.Left,
            Flatten(words.Take(selected.index).Append(lead).Append(removedGap).ToArray()));
        if (!frame.TryReadResult(nextStartRead, out var nextStart, out _, out var unresolved) || N.E(unresolved).Length != 0)
            throw new ArgumentException("The cut requires a complete native suffix-start read.");
        var introduced = new List<Relation>();
        if (prefix.Length != 0) introduced.Add(new(actualPath.Left, N.S(prefix)));
        if (suffix.Length != 0) introduced.Add(new(nextStart, N.S(suffix)));
        var evidence = new Relation(new Relation(actualPath, new Relation(actualChord, N.S(lead, removedGap, trail))),
            new Relation(endpointRead, nextStartRead));
        return Replace(source, name, [actualPath], introduced.ToArray(), evidence);
    }

    /// <summary>Fuses two actual single-run paths into one explicitly located
    /// run. Their native chord displacements must agree, possibly under ordered
    /// reversal. The supplied target start is a declared landmark, not an
    /// invented midpoint. Both old endpoint pairs and the new pair remain in
    /// the retargeting receipt; no endpoint-preservation promise is made.</summary>
    public static QualitativeLatticeLadder FuseParallelRuns(QualitativeLatticeLadder source,
        string name, Relation firstActualPath, Relation secondActualPath, Relation targetStart)
    {
        var program = source.Current.Program; var frame = program.Frame;
        var first = Select(program, firstActualPath); var second = Select(program, secondActualPath);
        if (ReferenceEquals(firstActualPath, secondActualPath) || first.Length != 1 || second.Length != 1)
            throw new ArgumentException("Fusion selects two distinct actual single-run paths.");
        RequireWord(frame, targetStart);
        if (Zero(frame, first[0]) || Zero(frame, second[0])) throw new ArgumentException("Fusion requires nonzero runs.");
        var forward = frame.Reduce(first[0], UnitArrangementRead.Inverse(frame, second[0]));
        var reversed = frame.Reduce(first[0], second[0]);
        if (!UnitArrangementRead.IsZero(frame, forward) && !UnitArrangementRead.IsZero(frame, reversed))
            throw new ArgumentException("The selected runs must have the same native displacement, possibly reversed.");
        var replacement = new Relation(targetStart, N.S(first[0]));
        Relation End(Relation path) => frame.Reduce((Relation)path.Left, (Relation)N.E((Relation)path.Right)[0]);
        var firstEnd = End(firstActualPath); var secondEnd = End(secondActualPath); var targetEnd = End(replacement);
        var endpoints = N.S(new Relation(firstActualPath, new Relation(firstActualPath.Left, firstEnd)),
            new Relation(secondActualPath, new Relation(secondActualPath.Left, secondEnd)),
            new Relation(replacement, new Relation(targetStart, targetEnd)));
        var proof = new Relation(new Relation(N.S(firstActualPath, secondActualPath), replacement),
            new Relation(new Relation(forward, reversed), endpoints));
        return Replace(source, name, [firstActualPath, secondActualPath], [replacement], proof);
    }

    /// <summary>Routes two actual parallel chord occurrences through one
    /// selected shared span while preserving both old endpoints. Unselected
    /// chords and other aliases remain carried. Opposed paths traverse the
    /// shared span in opposite order. New contact landmarks are published;
    /// actual joins still require the consuming field's accepted seam reads.</summary>
    public static QualitativeLatticeLadder FuseChordRuns(QualitativeLatticeLadder source,
        string name, Relation actualPathA, Relation actualChordA, Relation actualPathB, Relation actualChordB,
        Relation sharedStart, Relation sharedEnd)
    {
        var program = source.Current.Program; var frame = program.Frame;
        RequireWord(frame, sharedStart); RequireWord(frame, sharedEnd);
        var wordsA = Select(program, actualPathA); var wordsB = Select(program, actualPathB);
        if (ReferenceEquals(actualPathA, actualPathB) && ReferenceEquals(actualChordA, actualChordB))
            throw new ArgumentException("Fusion selects two distinct actual chord occurrences.");
        var opposed = Parallel(frame, actualChordA, actualChordB, out var directionEvidence);
        var middle = Delta(frame, sharedStart, sharedEnd, out var middleRead);
        if (Zero(frame, middle)) throw new ArgumentException("A shared run needs distinct start and end landmarks.");
        Parallel(frame, actualChordA, middle, out var middleDirection);
        // The declared middle follows A's direction. An opposite input is not
        // silently reversed; the selected sharedStart/sharedEnd is meaningful.
        if (Parallel(frame, actualChordA, middle, out _))
            throw new ArgumentException("The shared span must follow the first selected chord's ordered direction.");
        var proof = new List<IElement> { directionEvidence, middleDirection, middleRead };
        var edits = new List<(Relation Path, Relation Chord, Relation Chain)>();
        Add(actualPathA, actualChordA, wordsA, sharedStart, sharedEnd);
        Add(actualPathB, actualChordB, wordsB, opposed ? sharedEnd : sharedStart, opposed ? sharedStart : sharedEnd);
        return RewritePaths(source, name, edits, new Relation(new Relation(sharedStart, sharedEnd), N.S(proof.ToArray())));

        void Add(Relation path, Relation chord, Relation[] words, Relation first, Relation last)
        {
            var indices = words.Select((word, index) => (word, index)).Where(item => ReferenceEquals(item.word, chord)).ToArray();
            if (indices is not [var selected]) throw new ArgumentException("Fusion selects one actual chord occurrence in each path.");
            var prefixRead = frame.Reduce((Relation)path.Left, Flatten(words.Take(selected.index).ToArray()));
            if (!frame.TryReadResult(prefixRead, out var start, out _, out var unresolved) || N.E(unresolved).Length != 0)
                throw new ArgumentException("Fusion needs a complete native chord-start read.");
            var endRead = frame.Reduce(start, chord);
            if (!frame.TryReadResult(endRead, out var end, out _, out unresolved) || N.E(unresolved).Length != 0)
                throw new ArgumentException("Fusion needs a complete native chord-end read.");
            var approach = Delta(frame, start, first, out var approachRead);
            var shared = Delta(frame, first, last, out var sharedRead);
            var departure = Delta(frame, last, end, out var departureRead);
            var chain = N.S(new[] { approach, shared, departure }.Where(word => !Zero(frame, word)).Cast<IElement>().ToArray());
            var endpointRead = RequireSameDisplacement(frame, chord, Flatten(N.E(chain).Cast<Relation>().ToArray()));
            proof.Add(new Relation(new Relation(path, new Relation(chord, chain)),
                new Relation(new Relation(prefixRead, endRead), new Relation(N.S(approachRead, sharedRead, departureRead), endpointRead))));
            edits.Add((path, chord, chain));
        }
    }

    private static QualitativeLatticeLadder RewritePaths(QualitativeLatticeLadder source, string name,
        List<(Relation Path, Relation Chord, Relation Chain)> edits, Relation evidence)
    {
        var program = source.Current.Program;
        var pathMap = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var firstMap = new Dictionary<Relation, Dictionary<Relation, Relation>>(ReferenceEqualityComparer.Instance);
        foreach (var group in edits.GroupBy(edit => edit.Path, (IEqualityComparer<Relation>)ReferenceEqualityComparer.Instance))
        {
            var selectedWords = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance); firstMap.Add(group.Key, selectedWords);
            var words = Select(program, group.Key); var output = new List<IElement>();
            foreach (var word in words)
            {
                var matching = group.Where(edit => ReferenceEquals(edit.Chord, word)).ToArray();
                if (matching.Length == 0) { output.Add(word); continue; }
                if (matching is not [var edit]) throw new ArgumentException("One chord occurrence needs one selected fusion route.");
                var chain = N.E(edit.Chain).Cast<Relation>().ToArray();
                if (chain.Length == 0) throw new ArgumentException("A retained endpoint route cannot disappear in fusion.");
                output.AddRange(chain); selectedWords.Add(word, chain[0]);
            }
            pathMap.Add(group.Key, new Relation(group.Key.Left, N.S(output.ToArray())));
        }
        var paths = N.S(N.E(program.Paths).Select(path => path is Relation actual && pathMap.TryGetValue(actual, out var result)
            ? (IElement)result : path).ToArray());
        var ports = N.S(N.E(program.Ports).Cast<Relation>().Select(port =>
        {
            var pick = (Relation)port.Right;
            if (pick.Left is not Relation oldPath || !pathMap.TryGetValue(oldPath, out var changed)) return (IElement)port;
            var selector = ReferenceEquals(pick.Right, oldPath) ? changed : pick.Right;
            if (pick.Right is Relation oldChord && firstMap.TryGetValue(oldPath, out var selectedWords) && selectedWords.TryGetValue(oldChord, out var first)) selector = first;
            return new Relation(port.Left, new Relation(changed, selector));
        }).ToArray());
        var closed = N.S(N.E(program.ClosedPaths).Select(path => path is Relation actual && pathMap.TryGetValue(actual, out var changed)
            ? (IElement)changed : path).ToArray());
        return source.ReplacePaths(name, N.S(pathMap.Keys.Cast<IElement>().ToArray()),
            N.S(pathMap.Values.Cast<IElement>().ToArray()), CompletePorts(paths, ports), closed,
            new Relation(N.S(edits.Select(edit => (IElement)new Relation(edit.Path, new Relation(edit.Chord, edit.Chain))).ToArray()), evidence));
    }

    // One shared reach and the actual two declared frame directions supply a
    // formal coefficient basis. Host counting reads repeated participation;
    // no numeric coordinate or measured angle is written into the program.
    private static bool Parallel(QualitativeMotionFrame frame, Relation first, Relation second, out Relation evidence)
    {
        RequireWord(frame, first); RequireWord(frame, second);
        var reaches = N.E(first).Concat(N.E(second)).Cast<Relation>().Select(move => move.Right).Distinct(N.Participants.Instance).ToArray();
        if (reaches is not [var reach]) throw new ArgumentException("This fusion read needs one declared common reach.");
        (long X, long Y) Vector(Relation word)
        {
            long x = 0, y = 0;
            foreach (var move in N.E(word).Cast<Relation>())
            {
                frame.Direction(move, out var axis, out var forward, out _);
                if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
            }
            return (x, y);
        }
        var a = Vector(first); var b = Vector(second);
        if (a == (0L, 0L) || b == (0L, 0L) || checked(a.X * b.Y - a.Y * b.X) != 0)
            throw new ArgumentException("Fusion requires nonzero parallel native common-reach displacements.");
        evidence = new(new Relation(frame.Surface, reach), new Relation(first, second));
        return checked(a.X * b.X + a.Y * b.Y) < 0;
    }

    private static Relation Delta(QualitativeMotionFrame frame, Relation from, Relation to, out Relation witness)
    {
        witness = frame.Reduce(to, UnitArrangementRead.Inverse(frame, from));
        if (!frame.TryReadResult(witness, out var delta, out _, out var unresolved) || N.E(unresolved).Length != 0)
            throw new ArgumentException("A displacement needs a complete native endpoint read.");
        return delta;
    }

    private static QualitativeLatticeLadder Replace(QualitativeLatticeLadder source, string name,
        Relation[] selected, Relation[] introduced, Relation evidence)
    {
        var program = source.Current.Program;
        var carried = N.E(program.Paths).Where(path => !selected.Any(actual => ReferenceEquals(path, actual))).ToArray();
        var retainedPorts = N.S(N.E(program.Ports).OfType<Relation>().Where(port => port.Right is Relation pick &&
            carried.Any(path => ReferenceEquals(path, pick.Left))).Cast<IElement>().ToArray());
        var paths = N.S(carried.Concat(introduced).ToArray());
        var ports = CompletePorts(paths, retainedPorts);
        var closed = N.S(N.E(program.ClosedPaths).Where(path => carried.Any(actual => ReferenceEquals(actual, path))).ToArray());
        return source.ReplacePaths(name, N.S(selected), N.S(introduced), ports, closed, evidence);
    }

    private static Relation CompletePorts(Relation paths, Relation retainedPorts)
    {
        var ports = N.E(retainedPorts).ToList();
        foreach (var path in N.E(paths).Cast<Relation>())
        {
            foreach (var selector in N.E((Relation)path.Right).Append(path))
            {
                if (ports.OfType<Relation>().Any(port => port.Right is Relation pick &&
                    ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, selector))) continue;
                ports.Add(new Relation(new Placeholder(), new Relation(path, selector)));
            }
        }
        return N.S(ports.ToArray());
    }

    private static Relation[] Select(UnitArrangementProgram program, Relation path)
    {
        if (!N.E(program.Paths).Any(actual => ReferenceEquals(actual, path)) || path.Right is not Relation scope ||
            !N.Read(scope, out var words) || words.Any(item => item is not Relation))
            throw new ArgumentException("Surgery selects an actual complete path in the current native program.", nameof(path));
        return words.Cast<Relation>().ToArray();
    }
    private static Relation RequireSameDisplacement(QualitativeMotionFrame frame, Relation source, Relation result)
    {
        var read = frame.Reduce(source, UnitArrangementRead.Inverse(frame, result));
        if (!UnitArrangementRead.IsZero(frame, read)) throw new ArgumentException("The supplied native decomposition changes the promised endpoint.");
        return read;
    }
    private static void RequireWord(QualitativeMotionFrame frame, Relation word)
    {
        if (!N.Read(word, out var terms) || terms.Any(item => item is not Relation move || !frame.Direction(move, out _, out _, out _)))
            throw new ArgumentException("Surgery requires a closed word in the selected native frame.", nameof(word));
    }
    private static bool Zero(QualitativeMotionFrame frame, Relation word) => UnitArrangementRead.IsZero(frame, frame.Reduce(N.S(), word));
    private static Relation Flatten(params Relation[] words) => N.S(words.SelectMany(N.E).ToArray());
}
