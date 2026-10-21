using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;
using H = Gojo.Experiments.QualitativeFrieze.ProvisionalEquivalence.Hypothesis;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An executable symbolic path template. A move whose reach is an
/// actual parameter role expands through role -> parameter -> Closed(positive
/// reach atoms). A shared parameter proposes shared participation, not identity
/// of roles or a fact about an unknown external source.</summary>
public sealed class RevisablePatternTemplate
{
    // template = (program.Surface -> Closed(parameter roles)) ->
    //            (Closed(public port-role connections) -> Closed(crossing selectors))
    // realization = (template -> hypothesis) -> (expanded recipe ->
    //               (generation proof -> (Accepted -> (RoleValues ->
    //                (RoleEvidence -> (Open -> Blocked))))))
    // role evidence = role -> Closed((template path/chord/port -> actual port)
    //                 -> (actual mechanical proof ->
    //                    (public endpoint words -> native delta read)))
    public UnitArrangementProgram Program { get; }
    public QualitativeMotionFrame Frame => Program.Frame;
    public Relation Parameters { get; }
    public Relation Connections { get; }
    public Relation Crossings { get; }
    public Relation Surface { get; }
    public Relation Provenance { get; }

    public RevisablePatternTemplate(UnitArrangementProgram program, Relation parameters,
        Relation? connections = null, Relation? crossings = null)
    {
        Program = program; Parameters = parameters; Connections = connections ?? S(); Crossings = crossings ?? S();
        Surface = new(new Relation(program.Surface, parameters), new Relation(Connections, Crossings)); Provenance = S();
    }

    private RevisablePatternTemplate(UnitArrangementProgram program, Relation parameters, Relation connections,
        Relation crossings, Relation surface, Relation provenance)
    { Program = program; Parameters = parameters; Connections = connections; Crossings = crossings; Surface = surface; Provenance = provenance; }

    /// <summary>Exact standalone reader; no class, source factory or case name
    /// is needed. The correspondence imports frame relations while retaining
    /// independent external role and reach identities.</summary>
    public static bool TryRead(Relation surface, out RevisablePatternTemplate template, out Relation witness)
    {
        template = null!; witness = new(surface, StructuralConstraint.Open());
        if (surface is not { Left: Relation { Left: Relation { Left: Relation frameSurface } } } ||
            !QualitativeMotionFrameRead.TryReadStandard(frameSurface, out var frame) ||
            !frame.TryRebind(surface, out var copied, out var proof) || copied is not Relation
            { Left: Relation { Left: Relation { Left: var actualFrame, Right: Relation
                { Left: Relation { Left: Relation paths, Right: Relation ports }, Right: Relation { Left: Relation closed, Right: var evidence } } } programSurface,
                Right: Relation parameters }, Right: Relation { Left: Relation connections, Right: Relation crossings } } ||
            !ReferenceEquals(actualFrame, frame.Target.Surface)) return false;
        var program = new UnitArrangementProgram(frame.Target, "Imported symbolic template", paths, ports, closed, evidence, programSurface);
        if (!C(parameters, out var roles) || roles.Any(role => role is not Placeholder { IsInverse: false }) || !N.Distinct(roles) ||
            roles.Any(role => OrnamentReconstruction.Participants(frame.Target.Surface).Any(member => N.Same(member, role)))) return false;
        var candidate = new RevisablePatternTemplate(program, parameters, connections, crossings, surface, proof);
        var open = new List<IElement>(); var blocked = new List<IElement>(); candidate.CheckProgram(open, blocked);
        witness = new(surface, new Relation(proof, new Relation(S(open.ToArray()), S(blocked.ToArray()))));
        if (open.Count != 0 || blocked.Count != 0) return false;
        template = candidate; return true;
    }

    public H Hypothesis(Relation roleBindings, Relation parameterValues)
    {
        var equalities = new List<IElement>(); var rows = E(roleBindings).OfType<Relation>().ToArray();
        var roles = E(Parameters);
        for (var i = 0; i < roles.Length; i++) for (var j = i + 1; j < roles.Length; j++)
        {
            var first = rows.Where(row => N.Same(row.Left, roles[i])).ToArray(); var second = rows.Where(row => N.Same(row.Left, roles[j])).ToArray();
            if (first is [var a] && second is [var b] && N.Same(a.Right, b.Right)) equalities.Add(new Relation(roles[i], roles[j]));
        }
        return new(Parameters, Surface, roleBindings, parameterValues, S(equalities.ToArray()), Provenance);
    }

    public Realization Realize(H hypothesis) => new(this, hypothesis);

    public sealed class Realization
    {
        public OrnamentReconstructionCases.Sample? Sample { get; private set; }
        public Relation Recipe { get; }
        public Relation Accepted { get; }
        public Relation RoleValues { get; }
        public Relation RoleEvidence { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Surface { get; }
        public Relation Correspondence { get; }
        public Relation PortCorrespondence { get; }
        public Relation CrossingCorrespondence { get; }
        public Relation ExecutionPortCorrespondence { get; }
        public string Answer => E(Blocked).Length != 0 ? "Blocked" : E(Open).Length != 0 ? "Open" : E(Accepted).Length != 0 ? "Yes" : "Open";

        internal Realization(RevisablePatternTemplate template, H hypothesis)
        {
            var open = new List<IElement>(); var blocked = new List<IElement>(); var proof = new List<IElement>();
            var values = new List<IElement>(); var roleEvidence = new List<IElement>();
            var pathMap = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
            var wordMap = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
            var bindings = new Dictionary<IElement, Relation>(N.Participants.Instance);
            var samples = new Dictionary<IElement, Relation>(N.Participants.Instance);
            Recipe = S(); Correspondence = S(); PortCorrespondence = S(); CrossingCorrespondence = S(); ExecutionPortCorrespondence = S();
            template.CheckProgram(open, blocked);
            if (OrnamentReconstruction.Participants(new Relation(template.Surface, hypothesis.Surface)).Any(element => element is not (Relation or Placeholder)))
                blocked.Add(hypothesis.Surface);
            var roles = E(template.Parameters);
            if (!ReferenceEquals(hypothesis.Program, template.Surface)) blocked.Add(hypothesis.Surface);
            Scope(hypothesis.Domain); Scope(hypothesis.Bindings); Scope(hypothesis.ParameterValues); Scope(hypothesis.Equalities);
            if (C(hypothesis.Domain, out var domain) && C(template.Parameters, out _) &&
                (domain.Length != roles.Length || domain.Where((role, i) => !N.Same(role, roles[i])).Any())) blocked.Add(hypothesis.Surface);
            foreach (var entry in E(hypothesis.Bindings))
                if (entry is not Relation row || !roles.Any(role => N.Same(role, row.Left)) || row.Right is not Placeholder { IsInverse: false } || !bindings.TryAdd(row.Left, row)) blocked.Add(entry);
            foreach (var role in roles) if (!bindings.ContainsKey(role)) open.Add(new Relation(role, hypothesis.Bindings));
            foreach (var entry in E(hypothesis.ParameterValues))
            {
                if (entry is not Relation { Right: Relation atoms } row || !bindings.Values.Any(binding => N.Same(binding.Right, row.Left)) || !samples.TryAdd(row.Left, atoms))
                { blocked.Add(entry); continue; }
                Scope(atoms);
                if (C(atoms, out var terms) && (terms.Length == 0 || terms.Any(atom => atom is not Placeholder { IsInverse: false } || roles.Any(role => N.Same(role, atom))))) blocked.Add(row);
            }
            foreach (var binding in bindings.Values) if (!samples.ContainsKey(binding.Right)) open.Add(new Relation(binding, hypothesis.ParameterValues));
            foreach (var entry in E(hypothesis.Equalities))
                if (entry is not Relation equality || !bindings.TryGetValue(equality.Left, out var a) || !bindings.TryGetValue(equality.Right, out var b) || !N.Same(a.Right, b.Right)) blocked.Add(entry);
            if (open.Count == 0 && blocked.Count == 0)
            {
                foreach (var path in E(template.Program.Paths).Cast<Relation>())
                    pathMap.Add(path, new Relation(Expand((Relation)path.Left), S(E((Relation)path.Right).Cast<Relation>().Select(word => (IElement)Expand(word)).ToArray())));
                var sourcePorts = E(template.Program.Ports).Cast<Relation>().ToArray();
                var mappedPorts = sourcePorts.Select(port =>
                {
                    var selection = (Relation)port.Right; var path = (Relation)selection.Left;
                    return (IElement)new Relation(port.Left, new Relation(pathMap[path], ReferenceEquals(selection.Right, path) ? pathMap[path] : wordMap[(Relation)selection.Right]));
                }).ToArray();
                var ports = S(mappedPorts);
                PortCorrespondence = S(sourcePorts.Select((port, i) => (IElement)new Relation(port, mappedPorts[i])).ToArray());
                var closed = S(E(template.Program.ClosedPaths).Cast<Relation>().Select(path => (IElement)pathMap[path]).ToArray());
                var sourceCrossings = E(template.Crossings).Cast<Relation>().ToArray();
                var mappedCrossings = sourceCrossings.Select(row => (IElement)new Relation(Selector((Relation)row.Left), Selector((Relation)row.Right))).ToArray();
                var crossings = S(mappedCrossings);
                CrossingCorrespondence = S(sourceCrossings.Select((row, i) => (IElement)new Relation(row, mappedCrossings[i])).ToArray());
                Correspondence = new(new Relation(S(pathMap.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray()),
                    S(wordMap.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray())), new Relation(PortCorrespondence, CrossingCorrespondence));
                var expanded = new UnitArrangementProgram(template.Frame, "Realized symbolic template", S(pathMap.Values.Cast<IElement>().ToArray()), ports, closed,
                    new Relation(template.Surface, new Relation(hypothesis.Surface, Correspondence)));
                Recipe = new(expanded.Surface, new Relation(template.Connections, crossings));
                foreach (var path in E(closed).Cast<Relation>())
                {
                    var read = template.Frame.Reduce(S(), S(E((Relation)path.Right).Cast<Relation>().SelectMany(E).ToArray())); proof.Add(read);
                    if (!template.Frame.TryReadResult(read, out var remaining, out _, out var unresolved) || !C(remaining, out var terms) || E(unresolved).Length != 0) open.Add(read);
                    else if (terms.Length != 0) blocked.Add(read);
                }
                if (open.Count == 0 && blocked.Count == 0)
                {
                    if (OrnamentReconstructionCases.TryGenerate(Recipe, out var sample, out var generation))
                    {
                        Sample = sample; proof.Add(generation);
                        ExecutionPortCorrespondence = S(mappedPorts.Cast<Relation>().Select(port => (IElement)new Relation(port,
                            E(sample!.Program.Ports).Cast<Relation>().Single(actual => N.Same(actual.Left, port.Left)))).ToArray());
                        proof.Add(new Relation(PortCorrespondence, ExecutionPortCorrespondence));
                        ReadExecutedRoles();
                    }
                    else
                    {
                        proof.Add(generation);
                        if (generation.Right is Relation { Right: Relation { Left: Relation pending, Right: Relation refused } })
                        { open.AddRange(E(pending)); blocked.AddRange(E(refused)); }
                        if (open.Count == 0 && blocked.Count == 0) blocked.Add(generation);
                    }
                }
            }
            RoleValues = S(values.ToArray()); RoleEvidence = S(roleEvidence.ToArray()); Open = S(open.ToArray()); Blocked = S(blocked.ToArray());
            Accepted = Sample is not null && open.Count == 0 && blocked.Count == 0 ? S(Sample.Surface) : S();
            Surface = new(new Relation(template.Surface, hypothesis.Surface), new Relation(Recipe, new Relation(S(proof.ToArray()),
                new Relation(Accepted, new Relation(RoleValues, new Relation(RoleEvidence, new Relation(Open, Blocked)))))));

            void Scope(Relation scope)
            {
                if (!StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail)) blocked.Add(scope);
                else if (!N.Same(seam, tail)) open.Add(scope);
            }
            Relation Expand(Relation word)
            {
                if (wordMap.TryGetValue(word, out var existing)) return existing;
                var terms = E(word).Cast<Relation>().SelectMany(move => bindings.TryGetValue(move.Right, out var binding)
                    ? E(samples[binding.Right]).Select(atom => (IElement)QualitativeMotionFrame.Move(move.Left, atom)) : [move]).ToArray();
                var result = S(terms); wordMap.Add(word, result); return result;
            }
            Relation Selector(Relation selector) => new(pathMap[(Relation)selector.Left], wordMap[(Relation)selector.Right]);
            void ReadExecutedRoles()
            {
                var sample = Sample!;
                var actualBindings = E(sample.Bindings).Cast<Relation>().ToArray();
                foreach (var role in roles)
                {
                    var reads = new List<(Relation Value, Relation Witness)>();
                    foreach (var path in E(template.Program.Paths).Cast<Relation>()) foreach (var chord in E((Relation)path.Right).Cast<Relation>())
                    {
                        if (E(chord) is not [Relation move] || !N.Same(move.Right, role)) continue;
                        var sourcePorts = E(template.Program.Ports).Cast<Relation>().Where(port => port.Right is Relation pick && ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, chord)).ToArray();
                        if (sourcePorts.Length == 0) { open.Add(new Relation(role, chord)); continue; }
                        foreach (var sourcePort in sourcePorts)
                        {
                            var actualPort = E(sample.Program.Ports).Cast<Relation>().SingleOrDefault(port => N.Same(port.Left, sourcePort.Left));
                            var mechanical = E(sample.MechanicalEvidence).Cast<Relation>().FirstOrDefault(row => actualPort?.Right is Relation selection &&
                                row.Right is Relation evidence && ReferenceEquals(evidence.Left, selection.Left));
                            var actualProof = mechanical?.Right is Relation { Right: Relation rows }
                                ? E(rows).OfType<Relation>().SingleOrDefault(row => ReferenceEquals(row.Left, actualPort)) : null;
                            if (actualPort is null || actualProof?.Right is not Relation { Right: Relation { Right: Relation { Right: Relation actualStroke } } } ||
                                !sample.Publisher.TryMapSegment(actualStroke, out var segment) || !sample.Observation.TryReadSegment(segment, out _, out var from, out var to, out _))
                            { open.Add(new Relation(role, actualPort ?? sourcePort)); continue; }
                            var first = actualBindings.SingleOrDefault(row => sample.Publisher.TryMapSite(row.Left, out var site) && N.Same(site, from));
                            var last = actualBindings.SingleOrDefault(row => sample.Publisher.TryMapSite(row.Left, out var site) && N.Same(site, to));
                            if (first?.Right is not Relation start || last?.Right is not Relation end)
                            { open.Add(new Relation(segment, sample.Bindings)); continue; }
                            var delta = sample.Frame.Reduce(end, UnitArrangementRead.Inverse(sample.Frame, start));
                            template.Frame.Direction(move, out var axis, out var forward, out _);
                            var expectedDirection = ReferenceEquals(axis, template.Frame.Horizontal) ? forward ? sample.Frame.Right : sample.Frame.Left : forward ? sample.Frame.Up : sample.Frame.Down;
                            var witness = new Relation(new Relation(new Relation(path, chord), new Relation(sourcePort, actualPort)),
                                new Relation(new Relation(mechanical!, actualProof), new Relation(new Relation(first, last), delta)));
                            roleEvidence.Add(new Relation(role, witness));
                            if (!sample.Frame.TryReadResult(delta, out var remaining, out _, out var pending) || !C(remaining, out var terms) || E(pending).Length != 0)
                            { open.Add(witness); continue; }
                            if (terms.Length == 0 || terms.Any(term => term is not Relation step || !ReferenceEquals(step.Left, expectedDirection)))
                            { blocked.Add(witness); continue; }
                            reads.Add((S(terms.Cast<Relation>().Select(term => term.Right).ToArray()), witness));
                        }
                    }
                    if (reads.Count == 0) { open.Add(new Relation(role, template.Parameters)); continue; }
                    var firstValue = E(reads[0].Value);
                    if (reads.Any(read => E(read.Value).Length != firstValue.Length || E(read.Value).Where((atom, i) => !N.Same(atom, firstValue[i])).Any()))
                    { open.Add(new Relation(role, S(reads.Select(read => (IElement)read.Witness).ToArray()))); continue; }
                    values.Add(new Relation(role, reads[0].Value));
                }
            }
        }
    }

    private void CheckProgram(List<IElement> open, List<IElement> blocked)
    {
        foreach (var scope in new[] { Parameters, Program.Paths, Program.Ports, Program.ClosedPaths, Connections, Crossings })
            if (!StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail)) blocked.Add(scope);
            else if (!N.Same(seam, tail)) open.Add(scope);
        if (open.Count != 0 || blocked.Count != 0) return;
        var roles = E(Parameters);
        if (roles.Any(role => role is not Placeholder { IsInverse: false }) || !N.Distinct(roles) ||
            roles.Any(role => OrnamentReconstruction.Participants(Frame.Surface).Any(member => N.Same(member, role)))) blocked.Add(Parameters);
        // Keep symbolic closure obligations outside this structural preflight.
        var openProgram = new UnitArrangementProgram(Frame, "Symbolic preflight", Program.Paths, Program.Ports, S());
        if (!openProgram.Complete()) { blocked.Add(Program.Surface); return; }
        var paths = E(Program.Paths).Cast<Relation>().ToArray();
        if (E(Program.ClosedPaths).Any(path => !paths.Any(actual => ReferenceEquals(actual, path)))) blocked.Add(Program.ClosedPaths);
        var portRoles = E(Program.Ports).Cast<Relation>().Select(port => port.Left).ToArray();
        foreach (var row in E(Connections))
            if (row is not Relation pair || !portRoles.Any(role => N.Same(role, pair.Left)) || !portRoles.Any(role => N.Same(role, pair.Right))) blocked.Add(row);
        foreach (var row in E(Crossings))
            if (row is not Relation { Left: Relation a, Right: Relation b } || !Selector(a) || !Selector(b)) blocked.Add(row);
        bool Selector(Relation selector) => selector.Left is Relation path && paths.Any(actual => ReferenceEquals(actual, path)) &&
            E((Relation)path.Right).Count(chord => ReferenceEquals(chord, selector.Right)) == 1;
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool C(Relation scope, out IElement[] entries) => UnitArrangementRead.ClosedEntries(scope, out entries);
}
