using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Reads;
using Gojo.Structures;

// Rechecks of the original information-loss probes after the preservation work.
// Earlier observations remain in out/information-audit; new evidence is separate.
internal static class InformationAudit
{
    internal static void Run()
    {
        var evidence = new List<IElement>();
        var lines = new List<string>();
        var checks = 0;
        void Check(bool condition, string description)
        {
            checks++;
            if (!condition) throw new InvalidOperationException(description);
        }
        void Record(string text, params IElement[] structure)
        {
            lines.Add(text);
            evidence.Add(Group(structure));
        }
        IElement Compact(IElement source) => GojoStructureCompactJson.ToElement(
            GojoStructureCompactJson.Deserialize(GojoStructureCompactJson.Serialize(
                GojoStructureCompactJson.FromElement("information-audit", source)))).Root;

        var axis = new Carrier();
        var backwards = Relation.Segment(axis, 3, 2);
        var restoredSegment = (Relation)Compact(backwards);
        var restoredAxis = ((Relation)restoredSegment.Left).LeftLocation.Carrier;
        Check(backwards.FlowOn(axis).Ticks == -1 && restoredSegment.FlowOn(restoredAxis).Ticks == -1,
            "compact direction survives with entirely positive endpoints");
        Record("Compact segment: 3 -> 2 retains flow -1 after reload.", backwards, restoredSegment);

        var definition = ConceptDefinitionSurface.Create(new Carrier(), "backwards", backwards);
        var interchange = ConceptDefinitionInterchangeJson.FromLedger("information-audit",
            ConceptDefinitionSurface.Ledger(new Carrier(), [definition]));
        Check(interchange.Version == 2 && interchange.Structure is not null,
            "definition export uses the connected graph archive");
        var interLedger = ConceptDefinitionInterchangeJson.ToLedger(interchange);
        ConceptDefinitionSurface.TryReadLedger(interLedger, out var interDefinitions);
        ConceptDefinitionSurface.TryRead(interDefinitions[0], out var interDefinition);
        var interSegment = (Relation)interDefinition.Body;
        Check(interSegment.FlowOn(((Relation)interSegment.Left).LeftLocation.Carrier).Ticks == -1,
            "definition export retains direction after reload");
        Record("Concept-definition interchange retains 3 -> 2 after reload.", definition, interLedger);

        var residualSource = Relation.Segment(axis, 0, 4);
        var residualDefinition = ConceptDefinitionSurface.Create(new Carrier(), "residue-probe",
            Relation.Point(axis, 1), residueOrTension: residualSource);
        var residualDocument = ConceptDefinitionInterchangeJson.FromLedger("residue-probe",
            ConceptDefinitionSurface.Ledger(new Carrier(), [residualDefinition]));
        var residualLedger = ConceptDefinitionInterchangeJson.ToLedger(residualDocument);
        ConceptDefinitionSurface.TryReadLedger(residualLedger, out var residualDefinitions);
        ConceptDefinitionSurface.TryRead(residualDefinitions[0], out var residualReload);
        var restoredResidue = (Relation)residualReload.ResidueOrTension;
        Check(restoredResidue.FlowOn(((Relation)restoredResidue.Left).LeftLocation.Carrier).Ticks == 4,
            "structural residue survives as an executable segment");
        Record("Concept-definition interchange retains the residual segment 0 -> 4 as structure.", residualDefinition, residualLedger);

        var directed = Relation.DirectedSegment(axis, 2, 3);
        var reversed = directed.Reverse();
        directed.TryReadDirectedSegment(out var firstStart, out var firstEnd);
        reversed.TryReadDirectedSegment(out var secondStart, out var secondEnd);
        Check(!directed.Equals(reversed) && firstStart == secondStart && firstEnd == secondEnd,
            "face read forgets relation order while source structures remain different");
        var faceLedger = ConceptDefinitionSurface.Ledger(new Carrier(),
            [ConceptDefinitionSurface.Create(new Carrier(), "one", directed),
             ConceptDefinitionSurface.Create(new Carrier(), "two", reversed)]);
        var faceDocument = ConceptDefinitionInterchangeJson.FromLedger("face-order", faceLedger);
        var faceReload = ConceptDefinitionInterchangeJson.ToLabeledLedger(faceDocument);
        ConceptDefinitionSurface.TryRead(faceReload.SingleDefinition("one"), out var faceOne);
        ConceptDefinitionSurface.TryRead(faceReload.SingleDefinition("two"), out var faceTwo);
        Check(!faceOne.Body.Equals(faceTwo.Body) && ReferenceEquals(((Relation)faceOne.Body).Left, ((Relation)faceTwo.Body).Right),
            "interchange preserves both relation orders and shared faces");
        Record("Fused-face inspection still reads paired components without traversal order; the full saved relations now preserve both orders.", faceLedger, faceReload.Ledger);

        var zeroFaces = new Relation(new CarrierLocation(axis.Inverse(), 0), new CarrierLocation(axis, 0));
        Check(zeroFaces.TryReadFusedCarrierFaces(out _, out _), "paired zero faces are readable before saving");
        var reloadedZeroFaces = (Relation)Compact(zeroFaces);
        Check(reloadedZeroFaces.LeftLocation.Carrier.IsInverse && !reloadedZeroFaces.RightLocation.Carrier.IsInverse,
            "face flags survived");
        Check(reloadedZeroFaces.TryReadFusedCarrierFaces(out _, out _), "inverse pairing identity survives");
        Record("Compact zero-face pair retains both face flags and their paired-carrier connection.", zeroFaces, reloadedZeroFaces);

        var sharedTick = new TickRef(7);
        var shared = new Relation(new CarrierLocation(axis, sharedTick), new CarrierLocation(axis.Inverse(), sharedTick));
        var sharedReload = (Relation)Compact(shared);
        Check(shared.LeftLocation.SameTickRefAs(shared.RightLocation) &&
              sharedReload.LeftLocation.SameTickRefAs(sharedReload.RightLocation), "shared tick alias retained");
        sharedReload.LeftLocation.Tick.Set(9);
        Check(sharedReload.RightLocation.Ticks == 9, "changing one reloaded alias moves its peer");
        Record("Compact shared ticks: changing the first restored coordinate to 9 changes its peer to 9.", shared, sharedReload);

        var sharedPoint = Relation.Point(axis, 2);
        var withSharedEndpoint = Group(new Relation(sharedPoint, Relation.Point(axis, 3)), sharedPoint);
        var sharedPointReload = Parts((Relation)Compact(withSharedEndpoint));
        Check(ReferenceEquals(((Relation)sharedPointReload[0]).Left, sharedPointReload[1]), "endpoint relation alias retained");
        Record("Compact graph preserves an endpoint relation shared elsewhere in the graph.", withSharedEndpoint, Group(sharedPointReload));

        var coarse = new Carrier(UnitShape.ResolutionOf(axis.UnitShape, 100));
        var coarseReload = (Carrier)Compact(coarse);
        Check(coarse.UnitShape.TryReadResolution(out _, out _) && coarseReload.UnitShape.TryReadResolution(out _, out _),
            "compact carrier retains resolution unit shape");
        Record("Compact carrier retains ResolutionOf(source, 100).", coarse, coarseReload);

        var capacity = new Carrier();
        var clamp = Relation.Segment(capacity, 10, 10);
        capacity.Attach(clamp);
        capacity.Add(13);
        Check(capacity.Value == 10 && capacity.Tension == 3, "source capacity state");
        var reloadedWorld = (Relation)Compact(Group(capacity, clamp));
        var capacityReload = (Carrier)Parts(reloadedWorld)[0];
        Check(capacityReload.Value == 10 && capacityReload.Tension == 3, "live state retained");
        capacityReload.Add(1);
        Check(capacityReload.Value == 10 && capacityReload.Tension == 4, "saved clamp still participates in settlement");
        Record("Compact live world: value=10, tension=3 survives; adding 1 gives value=10, tension=4 through the restored clamp.", capacity, clamp, reloadedWorld);

        var descriptor = Relation.Point(new Carrier(), 987654321);
        var described = new Carrier(UnitShape.DescribedAtom(descriptor));
        var packed = GojoEncodedSnapshot.Capture(described);
        Check(described.UnitShape.DescriptorCount == 1 && packed.TickRefs.Any(t => t.Ticks == 987654321),
            "diagnostic packed snapshot retains descriptor-only source");
        Record("Packed diagnostic capture traverses unit descriptor roots, retaining their source.", described, descriptor);

        var metric = Relation.Point(new Carrier(), 43210987);
        var basis = BasisReadSurface.Create(new Carrier(), axis, Relation.Point(axis, 0),
            Relation.Segment(axis, 0, 12), metric, Relation.Point(Carrier.Unknown, 0), axis);
        BasisReadSurface.TryReadWitness(basis, new Carrier(), out var projectedWitness);
        Check(GojoEncodedSnapshot.Capture(projectedWitness).TickRefs.Any(t => t.Ticks == 43210987),
            "basis-to-witness projection retains independently supplied metric evidence");
        Record("BasisReadSurface.TryReadWitness retains the original basis read and its independent metric evidence.", basis, projectedWitness);

        var output = new Carrier();
        var emptyBasis = new Relation(Relation.Segment(axis, 0, 0), Relation.Segment(output, 0, 12));
        var populatedBasis = new Relation(Relation.Segment(axis, 0, 12), Relation.Segment(output, 0, 12));
        Check(emptyBasis.TryProject(new CarrierLocation(axis, 0), output, out var emptyRead), "zero-basis projection accepted");
        populatedBasis.TryProject(new CarrierLocation(axis, 0), output, out var measuredZero);
        Check(!emptyRead.Equals(measuredZero) && emptyRead.Right is Relation, "empty basis remains distinguishable from observed zero");
        var emptyWitness = ReadWitness.Create(new Carrier(), new CarrierLocation(axis, 0), emptyBasis, emptyRead, axis, emptyBasis);
        var measuredWitness = ReadWitness.Create(new Carrier(), new CarrierLocation(axis, 0), populatedBasis, measuredZero, axis, populatedBasis);
        Record("Empty-basis projection retains its missing calibration as structure; it differs from a measured zero even before adding a witness.", emptyWitness, measuredWitness);

        var wrongFlow = Group(Relation.Point(axis, 3), Relation.Point(axis, 2), Relation.Segment(axis, 2, 3));
        Check(!IntervalFocusSettlementSurface.TryReadContinuousCorrection(wrongFlow, out _, out _, out _, out _),
            "continuous correction refuses flow opposite to current-to-proposed order");
        Record("Continuous-correction read fails for current=3, proposed=2, flow=2->3; the attempted structure remains inspectable.", wrongFlow);

        var fineAxis = new Carrier();
        var exactHalfRoute = Equation.Affine(axis, 0, 2, fineAxis, 0, 200);
        exactHalfRoute.TryProject(new CarrierLocation(axis, 1), fineAxis, out var exactHalfRead);
        Check(exactHalfRead.LeftLocation.Ticks == 100 && exactHalfRead.RightLocation.Ticks == 0,
            "finite rational read can have zero actual projection residue");
        Record("Resolution versus actual residue: 1 on a 2-tick basis projects to 100 on a 200-tick basis with zero residue. Coarse source grain is still present in the original basis; a fine display does not add evidence.", exactHalfRoute, exactHalfRead);

        // Two linked numeric reads: a nominal value and a separately based
        // absolute uncertainty radius. The connection is through the selected
        // value witness and a calibration into that witness's carrier.
        var uncertaintyAxis = new Carrier();
        var valueBasis = Relation.Segment(axis, 0, 12);
        var nominal = ReadWitness.Create(new Carrier(), Relation.Point(axis, 8), valueBasis,
            new CarrierLocation(axis, 8), UnitShape.DescriptorSlot(0), valueBasis);
        var uncertaintyBasis = Relation.Segment(uncertaintyAxis, 0, 3);
        var uncertaintyRoute = Equation.Affine(uncertaintyBasis, valueBasis);
        uncertaintyRoute.TryProject(new CarrierLocation(uncertaintyAxis, 1), axis, out var radiusRead);
        var radius = radiusRead.LeftLocation;
        var lower = Equation.Predict(Equation.Difference(axis, axis, axis),
            new Relation(radius, new CarrierLocation(axis, 8)), axis);
        var upper = Equation.Predict(Equation.Sum(axis, Equation.UnitPort(axis), Equation.UnitPort(axis)),
            new Relation(new CarrierLocation(axis, 8), radius), axis);
        Equation.TryReadPrediction(lower, out var lowerValue, out _);
        Equation.TryReadPrediction(upper, out var upperValue, out _);
        Check(radius.Ticks == 4 && lowerValue.Ticks == 4 && upperValue.Ticks == 12, "separate uncertainty basis yields interval 4..12");
        var uncertainty = ReadWitness.Create(new Carrier(), nominal, uncertaintyBasis,
            new CarrierLocation(uncertaintyAxis, 1), UnitShape.DescriptorSlot(1),
            Group(uncertaintyRoute, radiusRead, lower, upper));
        Record("Native construction: nominal 8/12 plus an ABSOLUTE radius 1/3 of the same full scale projects to radius 4/12 and interval [4/12,12/12]. Both original bases remain connected.", nominal, uncertainty);

        var source = new Carrier();
        var time = new Carrier();
        var packets = new Carrier();
        var light = new Carrier();
        var window = new Relation(source, Relation.Segment(time, 0, 1));
        var identified = Relation.Segment(packets, 0, 20);
        var uninterpreted = Relation.Segment(packets, 20, 100);
        var coverage = ReadWitness.Create(new Carrier(), window, Relation.Segment(packets, 0, 100),
            new CarrierLocation(packets, 20), uninterpreted, identified);
        var lightRead = ReadWitness.Create(new Carrier(), identified, Relation.Segment(light, 0, 4000),
            new CarrierLocation(light, 3000), UnitShape.DescriptorSlot(2), window);
        var calibrated = new Carrier();
        var lightProjection = Equation.Affine(light, 0, 4000, calibrated, 0, 100);
        lightProjection.TryProject(new CarrierLocation(light, 3000), calibrated, out var projectedLight);
        Check(projectedLight.LeftLocation.Ticks == 75, "within-selected-subset light read projects independently");
        Record("Native light example: coverage=20/100 packets, remaining 80 packets uninterpreted; separate selected-light read=3000/4000, projecting to 75/100. No total-energy fraction follows without further calibration.", coverage, lightRead, lightProjection, projectedLight);

        var directory = Path.GetFullPath("experiments/ConceptScratchpad/out/information-preservation");
        Directory.CreateDirectory(directory);
        lines.Add($"Completed {checks} preservation checks against the original audit cases.");
        File.WriteAllLines(Path.Combine(directory, "observations.txt"), lines);
        File.WriteAllText(Path.Combine(directory, "evidence.snapshot.gojo.json"),
            JsonSerializer.Serialize(GojoEncodedSnapshot.Capture(Group(evidence.ToArray())), new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(string.Join(Environment.NewLine, lines));
    }

    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private static IElement[] Parts(Relation group)
    {
        group.TryReadOrderedElementsOn((Carrier)group.Right, out var elements);
        return elements;
    }
}
