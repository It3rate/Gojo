using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A narrow read of retained construction contracts, not a classifier
/// of arbitrary nested relations or an inference of hidden conceptual layers.
/// Only direct evidence entries of the selected complete path program are read.
/// Every accepted row keeps its actual source, target and native witnesses.</summary>
public sealed class BourgoinConstructionRead
{
    public sealed record ImageRead(Relation Evidence, Relation Prototype, Relation SourcePath,
        Relation OutputPath, Relation Transform, Relation Shift, Relation Placement, Relation ApplyReads)
    { public Relation Surface => Evidence; }
    public sealed record RefinementRead(Relation Surface, Relation Coarse, Relation Fine, Relation Replacements, Relation Proofs,
        Relation Participation);
    public sealed record CornerRead(Relation Evidence, Relation Prototype, Relation OutputPath, Relation Correspondences, Relation Successors)
    { public Relation Surface => Evidence; }

    public Relation Source { get; }
    public bool IsReadable { get; }
    public Relation Paths { get; }
    public Relation Ports { get; }
    public Relation ClosedPaths { get; }
    public int PathCount => E(Paths).Length;
    public int ChordCount => E(Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length);
    public int PortCount => E(Ports).Length;
    public int ClosedPathCount => E(ClosedPaths).Length;
    public IReadOnlyList<ImageRead> Images { get; }
    public IReadOnlyList<RefinementRead> Refinements { get; }
    public IReadOnlyList<CornerRead> Corners { get; }
    public Relation Prototypes { get; }
    public Relation Unrecognized { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }

    public BourgoinConstructionRead(QualitativeMotionFrame frame, Relation motifSurface)
    {
        Source = motifSurface; Paths = Ports = ClosedPaths = S();
        var images = new List<ImageRead>(); var refinements = new List<RefinementRead>(); var corners = new List<CornerRead>();
        var unrecognized = new List<IElement>(); var blocked = new List<IElement>(); var unresolved = new List<IElement>();
        if (!TryProgram(frame, motifSurface, out var program)) blocked.Add(motifSurface);
        else
        {
            IsReadable = true; Paths = program.Paths; Ports = program.Ports; ClosedPaths = program.ClosedPaths;
            if (program.Evidence is not Relation evidence || !StructuralConstraint.TryReadScope(evidence, out var entries, out var seam, out var tail))
                unrecognized.Add(program.Evidence);
            else
            {
                if (!seam.Equals(tail)) unresolved.Add(evidence);
                var deferred = new List<IElement>();
                foreach (var entry in entries)
                {
                    if (entry is Relation row && LooksImage(frame, row))
                    {
                        if (TryImage(frame, program, row, out var image)) images.Add(image); else blocked.Add(row);
                    }
                    else deferred.Add(entry);
                }
                foreach (var entry in deferred)
                    if (entry is Relation fine && LooksRefinement(frame, fine))
                    {
                        if (TryRefinement(frame, program, images, fine, out var refinement)) refinements.Add(refinement); else blocked.Add(fine);
                    }
                    else if (entry is Relation row && TryCorners(program, images, row, out var corner)) corners.Add(corner);
                    else unrecognized.Add(entry);
            }
        }
        Images = images.AsReadOnly(); Refinements = refinements.AsReadOnly(); Corners = corners.AsReadOnly();
        Prototypes = S(images.Select(image => (IElement)image.Prototype).Distinct(N.Participants.Instance).ToArray());
        Unrecognized = S(unrecognized.ToArray()); Blocked = S(blocked.ToArray()); Unresolved = S(unresolved.ToArray());
        Surface = new(Source, new Relation(new Relation(S(images.Select(image => (IElement)image.Surface).ToArray()),
            S(refinements.Select(read => (IElement)new Relation(read.Surface, read.Participation)).ToArray())),
            new Relation(new Relation(S(corners.Select(read => (IElement)read.Surface).ToArray()), Unrecognized), new Relation(Blocked, Unresolved))));
    }

    private static bool LooksImage(QualitativeMotionFrame frame, Relation row) =>
        row.Left is Relation { Left: Relation, Right: Relation } &&
        row.Right is Relation { Left: Relation { Left: Relation endpoints }, Right: Relation { Left: Relation, Right: Relation { Left: Relation, Right: Relation } } } &&
        ReferenceEquals(endpoints.Left, frame.Surface) && ReferenceEquals(endpoints.Right, frame.Surface);

    private static bool LooksRefinement(QualitativeMotionFrame frame, Relation row) =>
        ReferenceEquals(row.Left, frame.Surface) &&
        row.Right is Relation { Left: Relation { Left: Relation, Right: Relation },
            Right: Relation { Left: Relation, Right: Relation { Left: Relation coarse,
                Right: Relation { Left: Relation, Right: Relation } } } } &&
        ReferenceEquals(coarse.Left, frame.Surface) &&
        coarse.Right is Relation { Left: Relation { Left: Relation, Right: Relation }, Right: Relation { Left: Relation } };

    private static bool TryImage(QualitativeMotionFrame frame, UnitArrangementProgram selected, Relation row, out ImageRead image)
    {
        image = null!;
        if (row.Left is not Relation { Left: Relation prototype, Right: Relation outputPath } ||
            !Contains(selected.Paths, outputPath) || prototype.Left is not Relation sourcePath || prototype.Right is not Relation sourceClosed ||
            !Closed(sourceClosed, out var closures) || closures.Length > 1 || closures.Any(path => !ReferenceEquals(path, sourcePath)) ||
            !new UnitArrangementProgram(frame, "source contour read", S(sourcePath), S(), sourceClosed).Complete() ||
            row.Right is not Relation { Left: Relation transformSurface, Right: Relation { Left: Relation applyReads, Right: Relation { Left: Relation shift, Right: Relation placement } } } ||
            transformSurface.Left is not Relation endpoints || !ReferenceEquals(endpoints.Left, frame.Surface) || !ReferenceEquals(endpoints.Right, frame.Surface) ||
            transformSurface.Right is not Relation { Left: Relation rows, Right: var transformEvidence } || !Closed(applyReads, out var receipts) ||
            !Word(frame, shift)) return false;
        var transform = new QualitativePatternTransform(frame, frame, rows, transformEvidence);
        var sourceWords = new[] { (Relation)sourcePath.Left }.Concat(E((Relation)sourcePath.Right).Cast<Relation>()).ToArray();
        if (receipts.Length != sourceWords.Length) return false;
        var outputWords = new List<Relation>();
        for (var index = 0; index < receipts.Length; index++)
        {
            if (receipts[index] is not Relation receipt || receipt.Left is not Relation request ||
                !ReferenceEquals(request.Left, transformSurface) || !ReferenceEquals(request.Right, sourceWords[index])) return false;
            // The actual imported declaration retains its identity. The local
            // wrapper has a newly constructed surface; adapt only that reader
            // context and recheck the original result and proof participants.
            var contextual = new Relation(new Relation(transform.Surface, sourceWords[index]), receipt.Right);
            if (!transform.TryReadComplete(contextual, out var result)) return false;
            outputWords.Add(result);
        }
        if (!SameEntries((Relation)outputPath.Right, outputWords.Skip(1).Cast<IElement>().ToArray()) ||
            !Contains(selected.ClosedPaths, outputPath) && closures.Length != 0 ||
            Contains(selected.ClosedPaths, outputPath) && closures.Length == 0 ||
            placement.Left is not Relation { Right: Relation inputs } requestPlacement || !ReferenceEquals(requestPlacement.Left, frame.Surface) ||
            !ReferenceEquals(inputs.Left, outputWords[0]) || !ReferenceEquals(inputs.Right, shift) ||
            !frame.TryReadResult(placement, out var placed, out _, out var pending) || !Closed(placed, out _) || E(pending).Length != 0 ||
            !ReferenceEquals(placed, outputPath.Left)) return false;
        image = new(row, prototype, sourcePath, outputPath, transformSurface, shift, placement, applyReads); return true;
    }

    private static bool TryRefinement(QualitativeMotionFrame frame, UnitArrangementProgram selected, IReadOnlyList<ImageRead> images,
        Relation fineSurface, out RefinementRead refinement)
    {
        refinement = null!;
        if (!TryProgram(frame, fineSurface, out var fine) ||
            E(fine.Paths).Any(path => !Contains(selected.Paths, path) && !images.Any(image => ReferenceEquals(image.SourcePath, path))) ||
            E(fine.Ports).Cast<Relation>().Any(port => !Contains(selected.Ports, port) &&
                !images.Any(image => ReferenceEquals(image.SourcePath, ((Relation)port.Right).Left))) ||
            fine.Evidence is not Relation { Left: Relation coarseSurface, Right: Relation { Left: Relation replacements, Right: Relation proofs } } ||
            !TryProgram(frame, coarseSurface, out var coarse) || !Closed(replacements, out var rows) || !Closed(proofs, out var proofRows)) return false;
        var coarsePaths = E(coarse.Paths).Cast<Relation>().ToArray(); var finePaths = E(fine.Paths).Cast<Relation>().ToArray();
        if (coarsePaths.Length != finePaths.Length || rows.Any(row => row is not Relation)) return false;
        var used = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var proofIndex = 0;
        var firstMap = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var pathMap = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        for (var pathIndex = 0; pathIndex < coarsePaths.Length; pathIndex++)
        {
            var sourcePath = coarsePaths[pathIndex]; var targetPath = finePaths[pathIndex];
            if (!ReferenceEquals(sourcePath.Left, targetPath.Left)) return false;
            pathMap.Add(sourcePath, targetPath); var flattened = new List<IElement>();
            foreach (var word in E((Relation)sourcePath.Right).Cast<Relation>())
            {
                var choices = rows.Cast<Relation>().Where(row => ReferenceEquals(row.Left, word)).ToArray();
                if (choices.Length > 1 || proofIndex >= proofRows.Length ||
                    proofRows[proofIndex++] is not Relation proof || !ReferenceEquals(proof.Left, word) ||
                    proof.Right is not Relation { Left: Relation chain, Right: Relation displacement } ||
                    !Closed(chain, out var words) || words.Length == 0 || words.Any(item => item is not Relation actual || !Word(frame, actual))) return false;
                if (choices.Length == 1)
                {
                    if (!ReferenceEquals(choices[0].Right, chain)) return false;
                    used.Add(choices[0]);
                }
                else if (words.Length != 1 || !ReferenceEquals(words[0], word)) return false;
                var moves = words.Cast<Relation>().SelectMany(E).ToArray();
                if (displacement.Left is not Relation { Right: Relation inputs } request || !ReferenceEquals(request.Left, frame.Surface) ||
                    inputs.Left is not Relation before || !SameEntries(before, moves) || inputs.Right is not Relation inverse || !Inverse(frame, inverse, word) ||
                    !UnitArrangementRead.IsZero(frame, displacement)) return false;
                firstMap.TryAdd(word, words[0]); flattened.AddRange(words);
            }
            if (!SameEntries((Relation)targetPath.Right, flattened.ToArray()) || Contains(coarse.ClosedPaths, sourcePath) != Contains(fine.ClosedPaths, targetPath)) return false;
        }
        if (proofIndex != proofRows.Length || used.Count != rows.Length || E(coarse.Ports).Length != E(fine.Ports).Length) return false;
        foreach (var sourcePort in E(coarse.Ports).Cast<Relation>())
        {
            var targetPorts = E(fine.Ports).Cast<Relation>().Where(port => ReferenceEquals(port.Left, sourcePort.Left)).ToArray();
            if (targetPorts.Length != 1 || sourcePort.Right is not Relation sourcePick || targetPorts[0].Right is not Relation targetPick ||
                !pathMap.TryGetValue(sourcePick.Left, out var path) || !ReferenceEquals(targetPick.Left, path) ||
                !ReferenceEquals(targetPick.Right, ReferenceEquals(sourcePick.Left, sourcePick.Right) ? path : firstMap[sourcePick.Right])) return false;
        }
        // A refined program may directly supply executable paths, or may be
        // the actual source prototype of an admitted image. These are distinct
        // connected reads; retained prototype history is not direct execution.
        var participation = S(E(fine.Paths).Select(path => (IElement)new Relation(path,
            new Relation(S(E(selected.Paths).Where(actual => ReferenceEquals(actual, path)).ToArray()),
                S(images.Where(image => ReferenceEquals(image.SourcePath, path)).Select(image => (IElement)image.Evidence).ToArray())))).ToArray());
        refinement = new(fineSurface, coarseSurface, fineSurface, replacements, proofs, participation); return true;
    }

    private static bool TryCorners(UnitArrangementProgram selected, IReadOnlyList<ImageRead> images, Relation row, out CornerRead corner)
    {
        corner = null!;
        if (row.Left is not Relation { Left: Relation prototype, Right: Relation outputPath } ||
            !images.Any(image => ReferenceEquals(image.Prototype, prototype) && ReferenceEquals(image.OutputPath, outputPath)) ||
            row.Right is not Relation { Left: Relation mappings, Right: Relation successors } ||
            !Closed(mappings, out var entries) || entries.Length == 0 || !Closed(successors, out var links) || links.Length != entries.Length ||
            prototype.Left is not Relation sourcePath) return false;
        var sourceWords = E((Relation)sourcePath.Right); var targetWords = E((Relation)outputPath.Right); var previous = -1;
        for (var index = 0; index < entries.Length; index++)
        {
            if (entries[index] is not Relation { Left: Relation pick, Right: Relation actualPort } ||
                !ReferenceEquals(pick.Left, sourcePath) || !Contains(selected.Ports, actualPort) ||
                actualPort.Right is not Relation targetPick || !ReferenceEquals(targetPick.Left, outputPath)) return false;
            var selectedIndices = Enumerable.Range(0, sourceWords.Length).Where(i => ReferenceEquals(sourceWords[i], pick.Right)).ToArray();
            if (selectedIndices.Length != 1 || selectedIndices[0] <= previous || selectedIndices[0] >= targetWords.Length ||
                !ReferenceEquals(targetPick.Right, targetWords[selectedIndices[0]]) || links[index] is not Relation next ||
                !ReferenceEquals(next.Left, entries[index]) || !ReferenceEquals(next.Right, entries[(index + 1) % entries.Length])) return false;
            previous = selectedIndices[0];
        }
        corner = new(row, prototype, outputPath, mappings, successors); return true;
    }

    private static bool TryProgram(QualitativeMotionFrame frame, Relation surface, out UnitArrangementProgram program)
    {
        program = null!;
        if (!ReferenceEquals(surface.Left, frame.Surface) ||
            surface.Right is not Relation { Left: Relation { Left: Relation paths, Right: Relation ports }, Right: Relation { Left: Relation closed, Right: var evidence } }) return false;
        var parsed = new UnitArrangementProgram(frame, "native construction read", paths, ports, closed, evidence);
        if (!parsed.Complete()) return false;
        program = parsed; return true;
    }
    private static bool Word(QualitativeMotionFrame frame, Relation word) => Closed(word, out var moves) && moves.All(item => item is Relation move && frame.Direction(move, out _, out _, out _));
    private static bool Inverse(QualitativeMotionFrame frame, Relation inverse, Relation word)
    {
        if (!Closed(inverse, out var moves) || !Closed(word, out var source) || moves.Length != source.Length) return false;
        for (var index = 0; index < moves.Length; index++)
            if (moves[index] is not Relation actual || source[source.Length - 1 - index] is not Relation original || !N.Same(actual.Right, original.Right) ||
                !E(frame.Opposites).OfType<Relation>().Any(pair => ReferenceEquals(pair.Left, original.Left) && ReferenceEquals(pair.Right, actual.Left))) return false;
        return true;
    }
    private static bool Contains(Relation scope, IElement item) => E(scope).Any(actual => ReferenceEquals(actual, item));
    private static bool SameEntries(Relation scope, IElement[] expected) => Closed(scope, out var entries) && entries.Length == expected.Length && entries.Zip(expected).All(pair => ReferenceEquals(pair.First, pair.Second));
    private static bool Closed(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
