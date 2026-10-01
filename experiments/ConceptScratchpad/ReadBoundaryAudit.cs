using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Structures;

// Rechecks the four demonstrated boundaries after the dynamic read changes.
// Earlier observations remain in out/read-boundaries.
internal static class ReadBoundaryAudit
{
    internal static void Run()
    {
        var evidence = new List<IElement>();
        var lines = new List<string>();
        void Check(bool condition, string observation, params IElement[] parts)
        {
            if (!condition) throw new InvalidOperationException(observation);
            lines.Add(observation);
            evidence.Add(Relation.OrderedGroup(new Carrier(), parts));
        }
        Relation Observe(Carrier axis, TickRef tick)
        {
            var value = new CarrierLocation(axis, tick);
            return QualifiedReadSurface.Create(new Carrier(), value, Relation.Segment(axis, 0, 12),
                value, new Relation(value, value), new Placeholder(), new Placeholder(), new Placeholder(), value);
        }

        var axis = new Carrier();
        var tick = new TickRef(2);
        var a = Observe(axis, tick);
        var foreign = Observe(new Carrier(axis.UnitShape), new TickRef(3));
        var pendingAdd = QualifiedReadSurface.Add(new Carrier(), a, foreign);
        var pendingSubtract = QualifiedReadSurface.Subtract(new Carrier(), a, foreign);
        ReadWitness.TryRead(pendingAdd, out _, out var addSurface, out var addResult, out _, out _);
        ReadWitness.TryRead(pendingSubtract, out _, out var subtractSurface, out var subtractResult, out _, out _);
        var addAxis = ((Relation)((Relation)addSurface).Right).LeftLocation.Carrier;
        var subtractAxis = ((Relation)((Relation)subtractSurface).Right).LeftLocation.Carrier;
        var addition = Equation.Predict((Relation)addSurface,
            new Relation(new CarrierLocation(addAxis, 2), new CarrierLocation(addAxis, 3)), addAxis);
        var subtraction = Equation.Predict((Relation)subtractSurface,
            new Relation(new CarrierLocation(subtractAxis, 2), new CarrierLocation(subtractAxis, 3)), subtractAxis);
        Check(addition.LeftLocation.Ticks == 5 && subtraction.LeftLocation.Ticks == -1 &&
            addResult is Placeholder && subtractResult is Placeholder &&
            !QualifiedReadSurface.TryReadValue(pendingAdd, out _) && !QualifiedReadSurface.TryReadValue(pendingSubtract, out _),
            "Unaligned add and subtract stay unresolved and retain executable, distinct equations: the saved operations read 2+3=5 and 2-3=-1 on their own aligned ports.",
            pendingAdd, pendingSubtract);

        var b = Observe(axis, new TickRef(3));
        var sum = QualifiedReadSurface.Add(new Carrier(), a, b);
        QualifiedReadSurface.TryReadValue(sum, out var before);
        tick.Set(9);
        QualifiedReadSurface.TryReadValue(a, out var currentInput);
        QualifiedReadSurface.TryReadValue(sum, out var retainedAnswer);
        var fresh = QualifiedReadSurface.Add(new Carrier(), a, b);
        QualifiedReadSurface.TryReadValue(fresh, out var freshAnswer);
        Check(before.Ticks == 5 && currentInput.Ticks == 9 && retainedAnswer.Ticks == 12 && freshAnswer.Ticks == 12,
            "The same sum circuit reads 5 initially and 12 after its source changes from 2 to 9; no rebuild or stored-answer replacement is needed.",
            sum, fresh);

        var equation = Equation.Sum(axis, Equation.UnitPort(axis), Equation.UnitPort(axis));
        var accepted = Equation.TryPredict(equation, new Placeholder(), axis, out var failed);
        var numeric = Equation.TryReadPrediction(failed, out _, out _);
        Check(!accepted && !numeric,
            "A failed equation prediction has no scalar answer; its requested equation, input and output connection remain inspectable.",
            equation, failed);

        var capacity = new Carrier();
        capacity.Attach(Relation.Segment(capacity, 10, 10));
        capacity.Add(13);
        var complete = GojoStructureCompactJson.FromElement("read-boundaries", capacity);
        var incomplete = complete with
        {
            Carriers = complete.Carriers.Select(row => row.Id == complete.Root ? row with { Tension = null } : row).ToList()
        };
        var imported = GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(
            GojoStructureCompactJson.Serialize(incomplete)), out var loaded);
        ReadWitness.TryRead((Relation)loaded.Root, out _, out _, out var missing, out _, out _);
        Check(!imported && missing is Placeholder && loaded.Root is not Carrier,
            "Removing tension makes archive import an unresolved read. The supplied archive and missing-field evidence remain attached; no guessed carrier state is activated.",
            capacity, loaded.Root);

        var directory = Path.GetFullPath("experiments/ConceptScratchpad/out/read-boundaries-corrected");
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, "observations.txt"), lines);
        File.WriteAllText(Path.Combine(directory, "evidence.gojo.json"), GojoStructureCompactJson.Serialize(
            GojoStructureCompactJson.FromElement("read-boundaries", Relation.OrderedGroup(new Carrier(), evidence.ToArray()))));
        Console.WriteLine(string.Join(Environment.NewLine, lines));
        Console.WriteLine($"Verified {lines.Count} corrected boundaries. Evidence: {directory}");
    }
}
