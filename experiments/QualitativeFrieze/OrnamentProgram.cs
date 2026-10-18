using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Experimental executable program contract over existing path units.
/// A shared definition, a binding, an occurrence and a sampled patch remain
/// distinct native structures. No authoring factory is needed after import.</summary>
public sealed class OrnamentProgram
{
    // program = frame -> (motifs -> (transforms -> (parameters -> layout)))
    // parameter = actual slot -> Closed(actual alternatives)
    // cycle = Closed(phase identities) -> Closed(phase -> successor)
    // layout = (column cycle -> row cycle) -> ((U step slot -> V step slot) -> rules)
    // rule = (column phase -> row phase) -> (motif parameter -> transform parameter)
    // sample = program -> (Closed(parameter -> chosen alternative) -> window)
    // window = (start U phase -> start V phase) -> (Closed(column slots) -> Closed(row slots))
    public QualitativeMotionFrame Frame { get; }
    public Relation Motifs { get; }
    public Relation Transforms { get; }
    public Relation Parameters { get; }
    public Relation Layout { get; }
    public Relation Surface { get; }
    public OrnamentProgram(QualitativeMotionFrame frame, Relation motifs, Relation transforms, Relation parameters, Relation layout)
    {
        Frame = frame; Motifs = motifs; Transforms = transforms; Parameters = parameters; Layout = layout;
        Surface = new(frame.Surface, new Relation(motifs, new Relation(transforms, new Relation(parameters, layout))));
    }

    public static Relation Window(IElement startU, IElement startV, int columns, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(columns); ArgumentOutOfRangeException.ThrowIfNegative(rows);
        return new(new Relation(startU, startV), new Relation(Slots(columns), Slots(rows)));
    }

    public OrnamentProgramRun Generate(Relation bindings, Relation window)
    {
        var sample = new Relation(Surface, new Relation(bindings, window));
        var blocked = new List<IElement>(); var pending = new List<IElement>(); var units = new List<UnitArrangementNode>();
        var applications = new List<IElement>(); var projections = new List<IElement>(); var lowerings = new List<IElement>();
        var motifs = new Dictionary<IElement, UnitArrangementProgram>(N.Participants.Instance);
        var transforms = new Dictionary<IElement, QualitativePatternTransform>(N.Participants.Instance);
        var parameters = new Dictionary<IElement, Relation>(N.Participants.Instance);
        var choices = new Dictionary<IElement, IElement>(N.Participants.Instance);
        var rules = new List<Relation>();
        ReadScope(Motifs, out var motifRows); ReadScope(Transforms, out var transformRows); ReadScope(Parameters, out var parameterRows);
        if (!N.Distinct(motifRows) || !N.Distinct(transformRows)) blocked.Add(new Relation(Motifs, Transforms));
        foreach (var item in motifRows)
        {
            if (item is not Relation { Left: var frame, Right: Relation { Left: Relation { Left: Relation paths, Right: Relation ports },
                    Right: Relation { Left: Relation closed, Right: var evidence } } } actual || !ReferenceEquals(frame, Frame.Surface))
            { blocked.Add(item); continue; }
            var program = new UnitArrangementProgram(Frame, "native shared unit", paths, ports, closed, evidence);
            if (!program.Complete() || !motifs.TryAdd(actual, program)) blocked.Add(item);
            else lowerings.Add(new Relation(actual, program.Surface));
        }
        foreach (var item in transformRows)
        {
            if (item is not Relation { Left: Relation endpoints, Right: Relation { Left: Relation substitutions, Right: var evidence } } actual ||
                !ReferenceEquals(endpoints.Left, Frame.Surface) || !ReferenceEquals(endpoints.Right, Frame.Surface) || transforms.ContainsKey(actual))
            { blocked.Add(item); continue; }
            var transform = new QualitativePatternTransform(Frame, Frame, substitutions, evidence);
            transforms.Add(actual, transform); lowerings.Add(new Relation(actual, transform.Surface));
        }
        foreach (var item in parameterRows)
        {
            if (item is not Relation { Left: Placeholder slot, Right: Relation domain } || parameters.ContainsKey(slot))
            { blocked.Add(item); continue; }
            ReadScope(domain, out var alternatives);
            if (!N.Distinct(alternatives)) blocked.Add(item);
            parameters.Add(slot, domain);
        }
        ReadScope(bindings, out var selectedRows);
        foreach (var item in selectedRows)
        {
            if (item is not Relation choice || !parameters.TryGetValue(choice.Left, out var domain) || choices.ContainsKey(choice.Left))
            { blocked.Add(item); continue; }
            if (!N.E(domain).Any(allowed => N.Same(allowed, choice.Right)))
            { (N.Read(domain, out _) ? blocked : pending).Add(new Relation(choice, domain)); continue; }
            choices.Add(choice.Left, choice.Right);
        }
        foreach (var parameter in parameters)
            if (!choices.ContainsKey(parameter.Key)) pending.Add(new Relation(parameter.Key, parameter.Value));
        Relation uCycle = S(), vCycle = S(), ruleScope = S();
        IElement stepUSlot = new Placeholder(), stepVSlot = new Placeholder();
        if (Layout is { Left: Relation { Left: Relation u, Right: Relation v }, Right: Relation { Left: Relation stepSlots, Right: Relation rr } })
        { uCycle = u; vCycle = v; stepUSlot = stepSlots.Left; stepVSlot = stepSlots.Right; ruleScope = rr; }
        else blocked.Add(Layout);
        var us = ReadCycle(uCycle); var vs = ReadCycle(vCycle);
        ReadScope(ruleScope, out var ruleRows);
        foreach (var item in ruleRows)
        {
            if (item is not Relation { Left: Relation phasePair, Right: Relation slots } rule ||
                !us.Any(phase => N.Same(phase, phasePair.Left)) || !vs.Any(phase => N.Same(phase, phasePair.Right)) ||
                !parameters.ContainsKey(slots.Left) || !parameters.ContainsKey(slots.Right))
            { blocked.Add(item); continue; }
            rules.Add(rule);
        }
        foreach (var up in us) foreach (var vp in vs)
        {
            var matches = rules.Where(rule => rule.Left is Relation pair && N.Same(pair.Left, up) && N.Same(pair.Right, vp)).ToArray();
            if (matches.Length != 1) (matches.Length == 0 ? pending : blocked).Add(new Relation(new Relation(up, vp), ruleScope));
        }
        Relation stepU = S(), stepV = S();
        if (!ReadWord(stepUSlot, out stepU)) pending.Add(new Relation(stepUSlot, bindings));
        if (!ReadWord(stepVSlot, out stepV)) pending.Add(new Relation(stepVSlot, bindings));
        IElement startU = new Placeholder(), startV = new Placeholder(); IElement[] columns = [], rows = [];
        if (window is { Left: Relation initial, Right: Relation { Left: Relation cc, Right: Relation rc } })
        {
            startU = initial.Left; startV = initial.Right; ReadScope(cc, out columns); ReadScope(rc, out rows);
            if (!us.Any(phase => N.Same(phase, startU)) || !vs.Any(phase => N.Same(phase, startV))) blocked.Add(initial);
            var definitionParticipants = Participants(Surface);
            if (!N.Distinct(columns.Concat(rows).ToArray()) || columns.Concat(rows).Any(slot => slot is not Placeholder { IsInverse: false } || definitionParticipants.Contains(slot))) blocked.Add(window);
        }
        else blocked.Add(window);
        // Preflight the entire selected patch before any actuator is created.
        foreach (var rule in rules)
        {
            var slots = (Relation)rule.Right;
            if (!choices.TryGetValue(slots.Left, out var motif) || !choices.TryGetValue(slots.Right, out var transform)) continue;
            if (!motifs.TryGetValue(motif, out var program) || !transforms.TryGetValue(transform, out var mapping))
            { blocked.Add(new Relation(rule, new Relation(motif, transform))); continue; }
            try
            {
                if (!program.Transform("validated selected transform", mapping).Complete()) blocked.Add(new Relation(rule, mapping.Surface));
            }
            catch (ArgumentException) { blocked.Add(new Relation(rule, mapping.Surface)); }
        }
        if (blocked.Count == 0 && pending.Count == 0)
        {
            Relation rowStart = S(); var rowPhase = startV;
            foreach (var row in rows)
            {
                var position = rowStart; var columnPhase = startU;
                foreach (var column in columns)
                {
                    var rule = rules.Single(rule => rule.Left is Relation pair && N.Same(pair.Left, columnPhase) && N.Same(pair.Right, rowPhase));
                    var slots = (Relation)rule.Right; var program = motifs[choices[slots.Left]]; var mapping = transforms[choices[slots.Right]];
                    var unit = new UnitArrangementNode("sampled native occurrence", [program], program, position, transform: mapping);
                    units.Add(unit);
                    var application = new Relation(new Relation(new Relation(row, column), rule),
                        new Relation(new Relation(choices[slots.Left], choices[slots.Right]), new Relation(position, unit.Current.Surface)));
                    applications.Add(application);
                    position = Advance(position, stepU); columnPhase = Next(uCycle, columnPhase);
                }
                rowStart = Advance(rowStart, stepV); rowPhase = Next(vCycle, rowPhase);
            }
        }
        return new(this, bindings, window, units, S(applications.ToArray()), S(blocked.ToArray()), S(pending.ToArray()),
            new(sample, new Relation(S(lowerings.ToArray()), new Relation(S(applications.ToArray()), new Relation(S(projections.ToArray()), new Relation(S(blocked.ToArray()), S(pending.ToArray())))))));

        void ReadScope(Relation scope, out IElement[] entries)
        {
            if (!StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail)) { entries = []; blocked.Add(scope); }
            else if (!seam.Equals(tail)) pending.Add(scope);
        }
        IElement[] ReadCycle(Relation cycle)
        {
            if (cycle is not { Left: Relation domain, Right: Relation edges }) { blocked.Add(cycle); return []; }
            ReadScope(domain, out var phases); ReadScope(edges, out var links);
            if (phases.Length == 0 || !N.Distinct(phases) || phases.Any(phase => phase is not Placeholder) || links.Length != phases.Length)
            { blocked.Add(cycle); return []; }
            for (var i = 0; i < phases.Length; i++)
                if (links.Count(link => link is Relation pair && N.Same(pair.Left, phases[i]) && N.Same(pair.Right, phases[(i + 1) % phases.Length])) != 1)
                { blocked.Add(cycle); return []; }
            return phases;
        }
        bool ReadWord(IElement slot, out Relation word)
        {
            word = S();
            if (!parameters.ContainsKey(slot)) { blocked.Add(slot); return false; }
            if (!choices.TryGetValue(slot, out var chosen)) return false;
            if (chosen is not Relation selected || !N.Read(selected, out var moves) || moves.Any(move => move is not Relation actual || !Frame.Direction(actual, out _, out _, out _)))
            { blocked.Add(new Relation(slot, chosen)); return false; }
            word = selected; return true;
        }
        Relation Advance(Relation before, Relation step)
        {
            var read = Frame.Reduce(before, step); projections.Add(read);
            if (!Frame.TryReadResult(read, out var after, out _, out _)) throw new InvalidOperationException("A validated formal word did not read.");
            return after;
        }
    }

    public string Save(Relation bindings, Relation window)
    {
        var native = GojoStructureCompactJson.FromElement("ornament-program-v0", new Relation(Surface, new Relation(bindings, window)));
        return JsonSerializer.Serialize(new OrnamentProgramDocument(0, native), JsonOptions);
    }

    public static bool TryLoad(string json, out OrnamentProgramSample sample, out Relation unresolved)
    {
        sample = null!; unresolved = StructuralConstraint.Open();
        OrnamentProgramDocument? document;
        try { document = JsonSerializer.Deserialize<OrnamentProgramDocument>(json, JsonOptions); }
        catch (JsonException) { return false; }
        if (document is null || document.Version != 0 || document.Native is null || document.Native.Version != 3 ||
            document.Native.Carriers is null || document.Native.Nodes is null || document.Native.Carriers.Count != 0 ||
            document.Native.Nodes.Any(node => node is null || node.Kind is not ("relation" or "placeholder"))) return false;
        RehydratedCompactStructure restored;
        try { if (!GojoStructureCompactJson.TryToElement(document.Native, out restored)) return false; }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException or NullReferenceException or IndexOutOfRangeException or OverflowException) { return false; }
        if (restored.Root is not Relation source ||
            source.Left is not Relation { Left: Relation frameSurface } ||
            !QualitativeMotionFrameRead.TryReadStandard(frameSurface, out var frameRead)) return false;
        unresolved = StructuralConstraint.Open(source);
        if (!frameRead.TryRebind(source, out var rebound, out var witness) || rebound is not Relation root ||
            root.Left is not Relation { Left: var actualFrame, Right: Relation { Left: Relation motifs,
                Right: Relation { Left: Relation transforms, Right: Relation { Left: Relation parameters, Right: Relation layout } } } } program ||
            !ReferenceEquals(actualFrame, frameRead.Target.Surface) || root.Right is not Relation { Left: Relation bindings, Right: Relation window }) return false;
        var read = new OrnamentProgram(frameRead.Target, motifs, transforms, parameters, layout);
        sample = new(read, bindings, window, source, new(frameRead.Witness, witness)); unresolved = S(); return true;
    }

    private static IElement Next(Relation cycle, IElement phase) => N.E((Relation)cycle.Right).Cast<Relation>().Single(row => N.Same(row.Left, phase)).Right;
    private static HashSet<IElement> Participants(IElement source)
    {
        var result = new HashSet<IElement>(N.Participants.Instance); var stack = new Stack<IElement>(); stack.Push(source);
        while (stack.TryPop(out var item))
        {
            if (!result.Add(item)) continue;
            if (item is Placeholder slot) { result.Add(slot.Dual()); continue; }
            if (item is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); }
        }
        return result;
    }
    private static Relation Slots(int count) => S(Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray());
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    private sealed record OrnamentProgramDocument(int? Version, GojoCompactStructureDocument? Native);
}

public sealed record OrnamentProgramSample(OrnamentProgram Program, Relation Bindings, Relation Window, Relation Source, Relation Correspondence);

public sealed record OrnamentProgramRun(OrnamentProgram Program, Relation Bindings, Relation Window,
    IReadOnlyList<UnitArrangementNode> Units, Relation Applications, Relation Blocked, Relation Unresolved, Relation Surface)
{
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;
}
