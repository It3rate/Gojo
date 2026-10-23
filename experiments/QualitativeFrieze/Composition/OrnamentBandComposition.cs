using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Finite native compound-band application. A template retains whole
/// threads; substitution acts on every spine and side-offset word, while the
/// placement displacement acts only on position. Each application retains the
/// actual reusable template, consumed substitution rows, additive receipts and
/// fresh site correspondence. Equal placement positions never weld instances.</summary>
public static class OrnamentBandComposition
{
    // template = (frame -> Closed(Closed(envelopes), ...)) -> Closed(child -> threads)
    // instruction = substitution -> displacement
    // application = (template -> instruction) ->
    //               ((output -> Closed(envelope rows)) -> (Closed(site pairs) -> residue))
    // envelope row = (source envelope -> output envelope) ->
    //                (Closed(position rows) -> Closed(offset rows))
    // position row = (source control -> output control) -> (substitution read -> sum read)
    // offset row = (source offset A -> source offset B) -> (substitution read A -> substitution read B)
    // repeat = (seed -> (controller -> instruction)) ->
    //          (Closed(applications) -> (feedback -> (timeline -> stopped/open)))
    // arrangement = (fixed source -> (Closed(planned instructions) -> controller)) ->
    //               (Closed(applications) -> (composed output -> (timeline -> (stop -> residue))))
    // arrangement controller = Closed(phases) -> Closed((phase -> next) -> instruction)

    public sealed record Template(Curve.Frame Frame, IReadOnlyList<Relation> Threads,
        IReadOnlyList<Relation> Envelopes, bool Complete, Relation Surface);
    public sealed record Application(Template Source, Template Output,
        QualitativePatternTransform Transform, Relation Translation,
        IReadOnlyList<Relation> EnvelopePairs, Relation SitePairs, Relation Residue,
        bool Complete, Relation Surface);
    public sealed record Repetition(Template Seed, IReadOnlyList<Application> Applications,
        IReadOnlyList<Template> Instances, Template Output, Relation Controller,
        Relation Feedback, Relation Timeline, Relation Stop, bool Complete, Relation Surface);
    public sealed record PlacementInstruction(QualitativePatternTransform Transform,
        Relation Translation, Relation Surface);
    public sealed record Arrangement(Template Source, Relation Plan,
        IReadOnlyList<Application> Applications, IReadOnlyList<Template> Instances,
        Template Output, Relation Controller, Relation Timeline, Relation Stop,
        Relation Residue, bool Complete, Relation Surface);
    public sealed record SeamRead(bool? Coincident, bool? Tangent, bool? SideAgreement,
        bool? SideExchange, bool Joined, Relation Surface);

    public static Template Create(Curve.Frame frame, params Relation[] threads) =>
        ReadTemplate(frame, new Relation(new Relation(frame.Surface, S(threads)), S()));

    /// <summary>Children are retained as actual native sources, with their
    /// thread scopes reused. Composition is not flattening away the lineage.</summary>
    public static Template Compose(Curve.Frame frame, params Template[] children)
    {
        var threads = children.SelectMany(c => c.Threads).ToArray();
        var sourceRows = children.Select(c => (IElement)new Relation(c.Surface, S(c.Threads))).ToArray();
        return ReadTemplate(frame, new Relation(new Relation(frame.Surface, S(threads)), S(sourceRows)));
    }

    public static Template ReadTemplate(Curve.Frame frame, Relation surface)
    {
        if (surface is not { Left: Relation { Left: var selected, Right: Relation group }, Right: Relation children } ||
            !Same(selected, frame.Surface) || !Entries(group, out var threadItems) ||
            threadItems.Any(t => t is not Relation) || !Entries(children, out var childRows))
            return new(frame, [], [], false, surface);
        var threads = threadItems.Cast<Relation>().ToArray(); var envelopes = new List<Relation>(); var valid = true;
        foreach (var thread in threads)
        {
            if (!Entries(thread, out var items) || items.Any(e => e is not Relation)) { valid = false; continue; }
            foreach (var envelope in items.Cast<Relation>())
            { envelopes.Add(envelope); valid &= Band.ReadEnvelope(frame, envelope).Sweep.Complete; }
        }
        foreach (var row in childRows)
        {
            if (row is not Relation { Left: Relation child, Right: Relation scope } || !Entries(scope, out var items) ||
                child is not { Left: Relation { Left: var basis, Right: Relation original }, Right: Relation } ||
                !Same(basis, frame.Surface) || !Entries(original, out var expected) ||
                !SameItems(items, expected) || !items.All(t => threads.Any(actual => Same(actual, t)))) valid = false;
        }
        return new(frame, threads, envelopes, valid, surface);
    }

    /// <summary>The four actual direction substitutions are the operation.
    /// Axis-cycle/similarity premises can be supplied as evidence; without a
    /// premise this remains an affine word substitution, not a metric rotation.</summary>
    public static QualitativePatternTransform Map(Curve.Frame frame, Relation right,
        Relation left, Relation up, Relation down, IElement? premise = null) => new(frame.Motion, frame.Motion,
            S(new Relation(QualitativeMotionFrame.Move(frame.Motion.Right, frame.Reach), right),
              new Relation(QualitativeMotionFrame.Move(frame.Motion.Left, frame.Reach), left),
              new Relation(QualitativeMotionFrame.Move(frame.Motion.Up, frame.Reach), up),
              new Relation(QualitativeMotionFrame.Move(frame.Motion.Down, frame.Reach), down)), premise);

    public static QualitativePatternTransform Identity(Curve.Frame frame, IElement? premise = null) =>
        Map(frame, frame.Word(1, 0), frame.Word(-1, 0), frame.Word(0, 1), frame.Word(0, -1), premise);

    public static Application Place(Template source, QualitativePatternTransform transform, Relation translation)
    {
        source = ReadTemplate(source.Frame, source.Surface);
        var frame = source.Frame; var rows = new List<IElement>(); var pairs = new List<Relation>();
        var pending = new List<IElement>(); var threads = new List<Relation>();
        var sites = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var sitePairs = new List<IElement>();
        if (!source.Complete || !ReferenceEquals(transform.Source, frame.Motion) ||
            !ReferenceEquals(transform.Target, frame.Motion) || !frame.Vector(translation, out _)) pending.Add(source.Surface);
        foreach (var thread in source.Threads)
        {
            if (!Entries(thread, out var items)) { pending.Add(thread); continue; }
            var outputEnvelopes = new List<IElement>();
            foreach (var item in items)
            {
                if (item is not Relation envelope) { pending.Add(item); continue; }
                var read = Band.ReadEnvelope(frame, envelope); Relation output;
                var positionRows = new List<IElement>(); var offsetRows = new List<IElement>();
                if (!read.Sweep.Complete || !TryOffsets(read.Sweep.Surface, out var offsetsA, out var offsetsB) ||
                    !Entries(read.Sweep.Spine.ControlWords, out var controls))
                { output = new(envelope, StructuralConstraint.Open()); pending.Add(envelope); }
                else
                {
                    var words = controls.Cast<Relation>().Select(Position).ToArray();
                    var a = new Relation[offsetsA.Length]; var b = new Relation[offsetsB.Length];
                    for (var i = 0; i < a.Length; i++)
                    {
                        var ar = transform.Apply(offsetsA[i]); var br = transform.Apply(offsetsB[i]);
                        a[i] = transform.TryReadComplete(ar, out var aw) ? aw : StructuralConstraint.Open(ar);
                        b[i] = transform.TryReadComplete(br, out var bw) ? bw : StructuralConstraint.Open(br);
                        if (!frame.Vector(a[i], out _) || !frame.Vector(b[i], out _)) pending.Add(new Relation(ar, br));
                        offsetRows.Add(new Relation(new Relation(offsetsA[i], offsetsB[i]), new Relation(ar, br)));
                    }
                    var spine = Section(frame, Site(read.Sweep.Spine.Start), Site(read.Sweep.Spine.End), words);
                    var sweep = Band.Sweep(frame, spine, a, b,
                        Site(read.Sweep.SideA.Start), Site(read.Sweep.SideA.End), Site(read.Sweep.SideB.Start), Site(read.Sweep.SideB.End));
                    var placed = Band.Read(frame, sweep);
                    var start = End(read.Start, placed, true); var end = End(read.End, placed, false);
                    output = Band.BandEnvelope(sweep, start, end, read.Occupied == true ? Band.Occupy(placed) : StructuralConstraint.Open());
                    if (!placed.Complete) pending.Add(output);
                }
                var pair = new Relation(envelope, output); pairs.Add(pair); outputEnvelopes.Add(output);
                rows.Add(new Relation(pair, new Relation(S(positionRows), S(offsetRows))));

                Relation Position(Relation word)
                {
                    var action = transform.Apply(word);
                    var mapped = transform.TryReadComplete(action, out var image) ? image : StructuralConstraint.Open(action);
                    var sum = frame.Motion.Reduce(mapped, translation);
                    var result = frame.Motion.TryReadResult(sum, out var remainder, out _, out _) && frame.Vector(remainder, out _)
                        ? remainder : StructuralConstraint.Open(sum);
                    if (!frame.Vector(mapped, out _) || !frame.Vector(result, out _)) pending.Add(new Relation(action, sum));
                    positionRows.Add(new Relation(new Relation(word, result), new Relation(action, sum))); return result;
                }
                Relation End(Band.EndRead original, Band.SweepRead placed, bool start)
                {
                    if (original.Capped == true) return Band.Cap(frame, placed, start);
                    if (original.Continuing == true && original.Surface.Right is Relation continuation)
                        return Band.Continue(frame, placed, start, Site(continuation.Right));
                    return Band.UnknownEnd(placed, start);
                }
            }
            threads.Add(S(outputEnvelopes));
        }
        var outputTemplate = Create(frame, threads.ToArray()); var remainderScope = S(pending); var siteScope = S(sitePairs);
        var surface = new Relation(new Relation(source.Surface, new Relation(transform.Surface, translation)),
            new Relation(new Relation(outputTemplate.Surface, S(rows)), new Relation(siteScope, remainderScope)));
        return new(source, outputTemplate, transform, translation, pairs, siteScope, remainderScope,
            pending.Count == 0 && outputTemplate.Complete, surface);

        IElement Site(IElement original)
        {
            if (sites.TryGetValue(original, out var actual)) return actual;
            actual = new Placeholder(); sites.Add(original, actual); sitePairs.Add(new Relation(original, actual)); return actual;
        }
    }

    /// <summary>Checks native application receipts without trusting the
    /// diagnostic result arrays. Every offset and position requires the actual
    /// consumed substitution; positions additionally require their sum witness.</summary>
    public static bool Validate(Application application)
    {
        var frame = application.Source.Frame; var transform = application.Transform;
        if (application.Surface is not { Left: Relation { Left: var original, Right: Relation instruction },
            Right: Relation { Left: Relation { Left: Relation target, Right: Relation rows }, Right: Relation { Left: Relation sites, Right: Relation residue } } } ||
            !Same(original, application.Source.Surface) || !Same(instruction.Left, transform.Surface) ||
            !Same(instruction.Right, application.Translation) || !Same(target, application.Output.Surface) ||
            !Entries(rows, out var actualRows) || !Entries(sites, out var siteRows) || !Entries(residue, out var pending) || pending.Length != 0 ||
            !ReadTemplate(frame, target).Complete || !ReadTemplate(frame, application.Source.Surface).Complete ||
            !ReferenceEquals(transform.Source, frame.Motion) || !ReferenceEquals(transform.Target, frame.Motion)) return false;
        var source = ReadTemplate(frame, application.Source.Surface); var output = ReadTemplate(frame, target);
        if (actualRows.Length != source.Envelopes.Count || output.Envelopes.Count != source.Envelopes.Count ||
            output.Threads.Count != source.Threads.Count || !frame.Vector(application.Translation, out _)) return false;
        for (var i = 0; i < source.Threads.Count; i++)
            if (!Entries(source.Threads[i], out var sourceItems) || !Entries(output.Threads[i], out var outputItems) || sourceItems.Length != outputItems.Length) return false;
        var mapping = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        foreach (var entry in siteRows)
        {
            if (entry is not Relation pair || mapping.ContainsKey(pair.Left) || mapping.Values.Any(v => Same(v, pair.Right)) ||
                Same(pair.Left, pair.Right)) return false;
            mapping.Add(pair.Left, pair.Right);
        }
        for (var n = 0; n < actualRows.Length; n++)
        {
            if (actualRows[n] is not Relation { Left: Relation { Left: Relation sourceEnvelope, Right: Relation outputEnvelope },
                Right: Relation { Left: Relation positions, Right: Relation offsets } } ||
                !Same(sourceEnvelope, source.Envelopes[n]) || !Same(outputEnvelope, output.Envelopes[n])) return false;
            var a = Band.ReadEnvelope(frame, sourceEnvelope); var b = Band.ReadEnvelope(frame, outputEnvelope);
            if (!a.Sweep.Complete || !b.Sweep.Complete || !Entries(positions, out var pr) || !Entries(offsets, out var or) ||
                !Entries(a.Sweep.Spine.ControlWords, out var ac) || !Entries(b.Sweep.Spine.ControlWords, out var bc) ||
                pr.Length != ac.Length || bc.Length != ac.Length || !TryOffsets(a.Sweep.Surface, out var aa, out var ab) ||
                !TryOffsets(b.Sweep.Surface, out var ba, out var bb) || or.Length != aa.Length || ba.Length != aa.Length ||
                a.Occupied != b.Occupied || a.Start.Capped != b.Start.Capped || a.End.Capped != b.End.Capped ||
                a.Start.Continuing != b.Start.Continuing || a.End.Continuing != b.End.Continuing) return false;
            foreach (var pair in new[] { (a.Sweep.Spine.Start, b.Sweep.Spine.Start), (a.Sweep.Spine.End, b.Sweep.Spine.End),
                (a.Sweep.SideA.Start,b.Sweep.SideA.Start),(a.Sweep.SideA.End,b.Sweep.SideA.End),
                (a.Sweep.SideB.Start,b.Sweep.SideB.Start),(a.Sweep.SideB.End,b.Sweep.SideB.End) })
                if (!mapping.TryGetValue(pair.Item1, out var mappedSite) || !Same(mappedSite, pair.Item2)) return false;
            foreach (var ends in new[] { (a.Start, b.Start), (a.End, b.End) })
                if (ends.Item1.Continuing == true &&
                    (ends.Item1.Surface.Right is not Relation sourceContinuation || ends.Item2.Surface.Right is not Relation outputContinuation ||
                     !mapping.TryGetValue(sourceContinuation.Right, out var outside) || !Same(outside, outputContinuation.Right))) return false;
            for (var i = 0; i < pr.Length; i++)
            {
                if (pr[i] is not Relation { Left: Relation pair, Right: Relation { Left: Relation action, Right: Relation sum } } ||
                    !Same(pair.Left, ac[i]) || !Same(pair.Right, bc[i]) || action.Left is not Relation request || !Same(request.Right, ac[i]) ||
                    !transform.TryReadComplete(action, out var image) || sum.Left is not Relation { Right: Relation inputs } ||
                    !Same(inputs.Left, image) || !Same(inputs.Right, application.Translation) ||
                    !frame.Motion.TryReadResult(sum, out var result, out _, out _) || !Same(result, bc[i])) return false;
                if (or[i] is not Relation { Left: Relation inputPair, Right: Relation { Left: Relation ar, Right: Relation br } } ||
                    !Same(inputPair.Left, aa[i]) || !Same(inputPair.Right, ab[i]) ||
                    ar.Left is not Relation ap || br.Left is not Relation bp || !Same(ap.Right, aa[i]) || !Same(bp.Right, ab[i]) ||
                    !transform.TryReadComplete(ar, out var aImage) || !transform.TryReadComplete(br, out var bImage) ||
                    !Same(aImage, ba[i]) || !Same(bImage, bb[i])) return false;
            }
        }
        return true;
    }

    /// <summary>Produces count instances. The first is a fresh identity
    /// placement; each later application consumes the actual previous output.
    /// A closed finite phase path is the controller, not a hidden renderer loop.
    /// Seams remain observational questions and are not welded by repetition.</summary>
    public static Repetition Repeat(Template seed, QualitativePatternTransform transform, Relation translation, int count)
    {
        if (count is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(count));
        var phases = Enumerable.Range(0, count + 1).Select(_ => (IElement)new Placeholder()).ToArray();
        var links = Enumerable.Range(0, count).Select(i => (IElement)new Relation(phases[i], phases[i + 1])).ToArray();
        var controller = new Relation(S(phases), S(links)); var applications = new List<Application>();
        var feedback = new List<IElement>(); var timed = new List<Relation>(); var clock = new Carrier(); var before = seed;
        for (var i = 0; i < count; i++)
        {
            var action = Place(before, i == 0 ? Identity(seed.Frame) : transform, i == 0 ? seed.Frame.Word(0, 0) : translation);
            applications.Add(action); feedback.Add(new Relation(links[i], new Relation(before.Surface, action.Surface)));
            timed.Add(TimeStepSequence.Step(new Carrier(), clock, i, i + 1, before.Surface, action.Surface, action.Output.Surface, action.Residue));
            before = action.Output;
            if (!action.Complete) break;
        }
        var instances = applications.Select(a => a.Output).ToArray(); var output = Compose(seed.Frame, instances);
        var complete = applications.Count == count && applications.All(a => a.Complete);
        var stop = new Relation(phases[applications.Count], complete ? S() : StructuralConstraint.Open(controller));
        var timeline = TimeStepSequence.Create(new Carrier(), clock, seed.Surface, timed.ToArray()); var feedbackScope = S(feedback);
        var surface = new Relation(new Relation(seed.Surface, new Relation(controller, new Relation(transform.Surface, translation))),
            new Relation(S(applications.Select(a => (IElement)a.Surface)), new Relation(feedbackScope, new Relation(timeline, stop))));
        return new(seed, applications, instances, output, controller, feedbackScope, timeline, stop, complete, surface);
    }

    /// <summary>A fixed-source placement instruction. The actual native
    /// substitution and displacement, rather than an orientation label, are
    /// the plan entry. Translation is a convenience read of Surface.Right.</summary>
    public static PlacementInstruction Plan(QualitativePatternTransform transform, Relation translation) =>
        new(transform, translation, new Relation(transform.Surface, translation));

    /// <summary>A finite ordered arrangement samples one fixed prototype at
    /// each planned pose. This differs from Repeat, where the previous output
    /// becomes the next input. A mirror plus fixed translation can therefore
    /// alternate orientation across a field without collapsing to a two-cycle.
    /// Each independent request keeps its residue; failure of one request does
    /// not silently replace its geometry or prevent the other plan entries.</summary>
    public static Arrangement Arrange(Template source, params PlacementInstruction[] instructions)
    {
        if (instructions.Length is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(instructions));
        source = ReadTemplate(source.Frame, source.Surface);
        var planned = S(instructions.Select(i => (IElement)i.Surface));
        var phases = Enumerable.Range(0, instructions.Length + 1).Select(_ => (IElement)new Placeholder()).ToArray();
        var links = instructions.Select((instruction, i) => (IElement)new Relation(
            new Relation(phases[i], phases[i + 1]), instruction.Surface)).ToArray();
        var controller = new Relation(S(phases), S(links)); var applications = new List<Application>();
        var steps = new List<Relation>(); var pending = new List<IElement>(); var clock = new Carrier();
        foreach (var (instruction, i) in instructions.Select((item, index) => (item, index)))
        {
            var displacement = instruction.Surface.Right as Relation ?? StructuralConstraint.Open(instruction.Surface);
            var application = Place(source, instruction.Transform, displacement); applications.Add(application);
            if (!Same(instruction.Surface.Left, instruction.Transform.Surface))
                pending.Add(new Relation(instruction.Surface, application.Surface));
            if (!application.Complete) pending.Add(new Relation(instruction.Surface, application.Residue));
            var transition = new Relation(links[i], new Relation(instruction.Surface, application.Surface));
            steps.Add(TimeStepSequence.Step(new Carrier(), clock, i, i + 1, source.Surface,
                transition, application.Output.Surface, application.Residue));
        }
        var instances = applications.Select(a => a.Output).ToArray(); var output = Compose(source.Frame, instances);
        var residue = S(pending); var complete = pending.Count == 0 && output.Complete;
        var timeline = TimeStepSequence.Create(new Carrier(), clock, source.Surface, steps.ToArray());
        var stop = new Relation(phases[^1], complete ? S() : StructuralConstraint.Open(residue));
        var surface = new Relation(new Relation(source.Surface, new Relation(planned, controller)),
            new Relation(S(applications.Select(a => (IElement)a.Surface)),
                new Relation(output.Surface, new Relation(timeline, new Relation(stop, residue)))));
        return new(source, planned, applications, instances, output, controller, timeline, stop, residue, complete, surface);
    }

    /// <summary>Position/tangent/side compatibility at two exposed envelope
    /// ends is evidence for a possible seam. Same-side and exchanged-side
    /// correspondences are independent reads; neither exchanges sides, reverses
    /// the path, accepts a transfer or creates shared sites.</summary>
    public static SeamRead Seam(Curve.Frame frame, Relation incomingEnvelope, Relation outgoingEnvelope)
    {
        var a = Band.ReadEnvelope(frame, incomingEnvelope).Sweep; var b = Band.ReadEnvelope(frame, outgoingEnvelope).Sweep;
        if (!a.Complete || !b.Complete) return new(null, null, null, null, false,
            new Relation(new Relation(incomingEnvelope, outgoingEnvelope), StructuralConstraint.Open()));
        var p = Difference(a.Spine, false, b.Spine, true); var sa = Difference(a.SideA, false, b.SideA, true); var sb = Difference(a.SideB, false, b.SideB, true);
        var axb = Difference(a.SideA, false, b.SideB, true); var bxa = Difference(a.SideB, false, b.SideA, true);
        var tangent = !a.Spine.EndVector.Zero && !b.Spine.StartVector.Zero
            ? Curve.Vector.Cross(a.Spine.EndVector, b.Spine.StartVector).IsZero && Curve.Vector.Dot(a.Spine.EndVector, b.Spine.StartVector).Sign > 0 : (bool?)null;
        var tangentWords = new Relation(a.Spine.EndTangent, b.Spine.StartTangent);
        var surface = new Relation(new Relation(incomingEnvelope, outgoingEnvelope), new Relation(p, new Relation(tangentWords, new Relation(new Relation(sa, sb), new Relation(axb, bxa)))));
        return new(Zero(p), tangent, Zero(sa) && Zero(sb), Zero(axb) && Zero(bxa), Curve.Join(a.Spine, b.Spine).Connected, surface);

        Relation Difference(Curve.Read first, bool firstStart, Curve.Read second, bool secondStart)
        {
            Entries(first.ControlWords, out var aw); Entries(second.ControlWords, out var bw);
            var from = (Relation)aw[firstStart ? 0 : aw.Length - 1]; var to = (Relation)bw[secondStart ? 0 : bw.Length - 1];
            var inverse = S(Items(from).Cast<Relation>().Select(move =>
            {
                frame.Motion.Direction(move, out var axis, out var positive, out var reach);
                return (IElement)QualitativeMotionFrame.Move(ReferenceEquals(axis, frame.Motion.Horizontal)
                    ? positive ? frame.Motion.Left : frame.Motion.Right : positive ? frame.Motion.Down : frame.Motion.Up, reach);
            }));
            return frame.Motion.Reduce(inverse, to);
        }
        bool Zero(Relation witness) => frame.Motion.TryReadResult(witness, out var remainder, out _, out _) && Entries(remainder, out var entries) && entries.Length == 0;
    }

    private static bool TryOffsets(Relation sweep, out Relation[] a, out Relation[] b)
    {
        a = b = [];
        if (sweep.Right is not Relation { Left: Relation rows } || !Entries(rows, out var entries)) return false;
        var aa = new List<Relation>(); var bb = new List<Relation>();
        foreach (var item in entries)
        {
            if (item is not Relation { Left: Relation { Right: Relation { Left: Relation first, Right: Relation second } } }) return false;
            aa.Add(first); bb.Add(second);
        }
        a = aa.ToArray(); b = bb.ToArray(); return true;
    }
    private static Relation Section(Curve.Frame frame, IElement from, IElement to, Relation[] words) => words.Length switch
    { 2 => frame.Line(from, to, words[0], words[1]), 3 => frame.Quadratic(from, to, words[0], words[1], words[2]),
      4 => frame.Cubic(from, to, words[0], words[1], words[2], words[3]), _ => new(new Relation(from, to), StructuralConstraint.Open(words)) };
    private static bool SameItems(IElement[] a, IElement[] b) => a.Length == b.Length && a.Zip(b).All(p => Same(p.First, p.Second));
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static bool Entries(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] Items(Relation scope) => Entries(scope, out var entries) ? entries : [];
    private static Relation S(IEnumerable<IElement> entries) => StructuralConstraint.Closed(entries.ToArray());
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
