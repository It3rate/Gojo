using Gojo.Core;
using A = ConnectionAssembly.AssemblyNetwork;

namespace ConnectionAssembly;

/// <summary>Small experiment-contract checks; no domain calculations are supplied by this harness.</summary>
public static class Checks
{
    public static string[] Run()
    {
        var passed = new List<string>();
        RunOne("Warmup, unknown input and sequence gaps remain incomplete; duplicate and stale admissions do not mutate the world.", AdmissionAndMissingInput);
        RunOne("Isolated square edges are large changes, never sustained small rise or fall.", IsolatedSquareEdges);
        RunOne("Continuous small-step zigzag has directional runs and reversals without jump or scale-change pulses.", ContinuousZigzag);
        RunOne("A newly proposed link cannot account for its discovery frame; it can account for a later arrival.", DiscoveryAndLaterUse);
        RunOne("The attached native product responds to actual producer/request perturbations and a zero gate blocks it.", FunctionalGate);
        RunOne("Unused support fades one packet per four complete arrivals, reaches zero and leaves no negative debt.", SlowFade);
        RunOne("Repeated read-only inspection does not refresh engagement, support or current pulses.", InspectionIsPure);
        RunOne("Repeated signal changes retain the finite connection neighborhood without multiplying a live contribution.", RepeatedChanges);
        RunOne("Probes physically attach a dormant publication; fade withdraws it and later probing reuses its identity.", PhysicalLifecycle);
        return passed.ToArray();

        void RunOne(string description, Action check)
        {
            check();
            passed.Add(description);
        }
    }

    private static void AdmissionAndMissingInput()
    {
        var root = A.Create();
        AssertIncomplete(Observe(ref root, 1, 20));
        AssertIncomplete(Observe(ref root, 2, 21));
        var ready = Observe(ref root, 3, 22);
        Require(A.Complete(ready), "Three adjacent known coordinates complete the reader frame.");
        Require(A.SampleRaw(A.NodeSamples(ready)[0]) == 1, "Two adjacent small positive deltas satisfy the rise reader.");
        RejectWithoutMutation(root, 3, 22);
        RejectWithoutMutation(root, 2, 21);

        var stale = root;
        var controls = Controls(root);
        AssertIncomplete(Observe(ref root, 4, null));
        Require(controls == Controls(root), "An unknown observation neither rewards nor fades retained controls.");
        Require(A.Nodes(root).All(node => A.Raw(node).Value == 0 && A.Request(node).Value == 0 && A.Help(node).Value == 0),
            "Unknown input leaves no published recognition or help pulses.");
        var afterUnknown = GojoNumericFormat.Encode(root);
        RejectWithoutMutation(stale, 5, 30);
        Require(afterUnknown.AsSpan().SequenceEqual(GojoNumericFormat.Encode(root)), "A stale root cannot change its current shared world.");

        AssertIncomplete(Observe(ref root, 5, 30));
        AssertIncomplete(Observe(ref root, 6, 31));
        Require(A.Complete(Observe(ref root, 7, 32)), "Two further adjacent samples restore completeness after unknown input.");
        controls = Controls(root);
        AssertIncomplete(Observe(ref root, 9, 40));
        Require(controls == Controls(root), "A skipped sequence does not earn or fade support.");
        AssertIncomplete(Observe(ref root, 10, 41));
        var afterGap = Observe(ref root, 11, 42);
        Require(A.Complete(afterGap) && A.SampleRaw(A.NodeSamples(afterGap)[0]) == 1,
            "A sequence gap starts a new trace instead of comparing across missing input.");

        var pressured = FlatThenJump(out var pressureWitness, out _);
        var pressure = A.Pressure(A.Nodes(pressured)[2]);
        Require(A.SamplePressure(A.NodeSamples(pressureWitness)[2]) == 1 && pressure.Value == 0 && pressure.Tension == 1,
            "The missing-input check begins with an actual held unmet packet.");
        var beforeMissing = Controls(pressured);
        var missing = Observe(ref pressured, 5, null);
        AssertIncomplete(missing);
        Require(A.Nodes(pressured).All(node => A.Pressure(node).Value == 0 && A.Pressure(node).Tension == 0),
            "An unknown observation explicitly clears previously held request pressure.");
        Require(A.NodeSamples(missing).All(sample => A.SamplePressure(sample) == 0),
            "The incomplete witness does not retain a stale numeric unmet request.");
        Require(beforeMissing == Controls(pressured), "Clearing unknown-frame pressure is not support credit or decay.");
    }

    private static void IsolatedSquareEdges()
    {
        var root = A.Create();
        long[] values = [20, 20, 20, 32, 32, 32, 20, 20, 20];
        var jumps = 0;
        for (var index = 0; index < values.Length; index++)
        {
            var witness = Observe(ref root, index + 1, values[index]);
            if (!A.Complete(witness)) continue;
            var nodes = A.NodeSamples(witness);
            Require(A.SampleRaw(nodes[0]) == 0 && A.SampleRaw(nodes[1]) == 0,
                "A square edge is not a sustained small movement.");
            Require(A.SampleRaw(nodes[4]) == 0 && A.SampleRaw(nodes[5]) == 0,
                "A plateau separates square edges; they are not adjacent-delta reversal or scale-change events.");
            if (A.SampleRaw(nodes[3]) == 1) jumps++;
        }
        Require(jumps == 2, "The two authored square transitions each produce one large-change pulse.");
    }

    private static void ContinuousZigzag()
    {
        var root = A.Create();
        long[] values = [20, 21, 22, 23, 22, 21, 20, 19, 20, 21, 22, 23, 22, 21, 20];
        var rose = false;
        var fell = false;
        var reversed = false;
        for (var index = 0; index < values.Length; index++)
        {
            var witness = Observe(ref root, index + 1, values[index]);
            if (!A.Complete(witness)) continue;
            var nodes = A.NodeSamples(witness);
            rose |= A.SampleRaw(nodes[0]) == 1;
            fell |= A.SampleRaw(nodes[1]) == 1;
            reversed |= A.SampleRaw(nodes[4]) == 1;
            Require(A.SampleRaw(nodes[3]) == 0 && A.SampleRaw(nodes[5]) == 0,
                "The continuous unit-step zigzag contains no amplitude reset or magnitude change.");
        }
        Require(rose && fell && reversed, "Both sustained directions and a genuine reversal are recognized.");
    }

    private static void DiscoveryAndLaterUse()
    {
        var root = FlatThenJump(out var discovery, out var link);
        var node = A.NodeSamples(discovery)[2];
        var proposed = LinkSample(discovery, link);
        Require(A.SampleRaw(node) == 0 && A.SampleRequest(node) == 1,
            "Previously engaged flat reader requests help on the isolated jump.");
        Require(A.SampleHelp(node) == 0 && A.SamplePressure(node) > 0 && !A.SampleAccounted(node),
            "A current proposal cannot discharge the discovery frame's request.");
        Require(A.Pressure(A.Nodes(root)[2]).Value == 0 &&
                A.Pressure(A.Nodes(root)[2]).Tension == A.SamplePressure(node),
            "The discovery deficit is held as physical tension, not absorbed carrier value.");
        Require(A.WasProposed(proposed) && !A.WasUseful(proposed) && A.SampleReceipt(proposed) == 0,
            "Probe establishment is visible but is not current-frame utility credit.");
        Require(A.Receipt(link).Value == 0 && A.Help(A.Nodes(root)[2]).Value == 0,
            "Opening the next gate after pulse clearing cannot leave a live discovery receipt.");
        var seededSupport = A.Support(link).Value;

        var later = Observe(ref root, 5, 44);
        var consumer = A.NodeSamples(later)[2];
        var used = LinkSample(later, link);
        Require(A.SampleRaw(consumer) == 0 && A.SampleRequest(consumer) == 1 &&
                A.SampleHelp(consumer) == 1 && A.SamplePressure(consumer) == 0 && A.SampleAccounted(consumer),
            "The existing helper accounts for the next requested arrival without changing raw recognition.");
        Require(A.Pressure(A.Nodes(root)[2]).Value == 0 &&
                A.Pressure(A.Nodes(root)[2]).Tension == A.SamplePressure(consumer),
            "A later accounting receipt discharges the physical held request.");
        Require(A.SampleReceipt(used) == 1 && A.WasUseful(used) && !A.WasProposed(used),
            "Later use is distinguished from recruitment.");
        Require(A.Support(link).Value > seededSupport, "A necessary later receipt strengthens native link support.");
    }

    private static void FunctionalGate() => AssertNativeGate(A.Create());

    private static void SlowFade()
    {
        var root = FlatThenJump(out _, out var link);
        var previous = A.Support(link).Value;
        Require(previous > 0, "Probe support exists before its unused interval.");
        var previousDrop = -1L;
        var sawSlowSurvival = false;
        var sawWithdrawal = false;
        for (long sequence = 5; sequence <= 24; sequence++)
        {
            var witness = Observe(ref root, sequence, 32);
            var sample = LinkSample(witness, link);
            Require(!A.WasUseful(sample) && !A.WasProposed(sample), "An unneeded jump-to-flat link receives no refresh.");
            var current = A.Support(link).Value;
            Require(current <= previous && previous - current <= 1, "Fade is gradual and never increases unused support.");
            if (sequence == 5) sawSlowSurvival = current > 0;
            if (current < previous)
            {
                if (previousDrop >= 0) Require(sequence - previousDrop == 4, "Support drains only once per four complete arrivals.");
                previousDrop = sequence;
            }
            sawWithdrawal |= A.WasReleased(sample);
            Require(A.Support(link).Tension == 0, "Fade does not accumulate an unpaid negative draw.");
            previous = current;
        }
        Require(sawSlowSurvival && sawWithdrawal, "An unused link persists briefly and later withdraws.");
        Require(A.Support(link).Value == 0 && A.Gate(link).Value == 0 && A.Receipt(link).Value == 0,
            "Expired support closes the actual contribution gate.");
    }

    private static void InspectionIsPure()
    {
        var root = FlatThenJump(out var witness, out _);
        var before = GojoNumericFormat.Encode(root, witness);
        for (var iteration = 0; iteration < 8; iteration++)
        {
            _ = A.Complete(witness);
            foreach (var node in A.Nodes(root))
            {
                _ = A.Raw(node).Value;
                _ = A.Request(node).Value;
                _ = A.Help(node).Value;
                _ = A.Pressure(node).Value;
                _ = A.Engagement(node).Value;
                _ = A.Accounted(node);
            }
            foreach (var sample in A.NodeSamples(witness))
            {
                _ = A.SampleRaw(sample);
                _ = A.SampleRequest(sample);
                _ = A.SampleHelp(sample);
                _ = A.SamplePressure(sample);
                _ = A.SampleEngagement(sample);
                _ = A.SampleAccounted(sample);
            }
            foreach (var sample in A.LinkSamples(witness))
            {
                _ = A.SampleSupport(sample);
                _ = A.SampleGate(sample);
                _ = A.SampleReceipt(sample);
                _ = A.WasProposed(sample);
                _ = A.WasUseful(sample);
                _ = A.WasReleased(sample);
            }
        }
        Require(before.AsSpan().SequenceEqual(GojoNumericFormat.Encode(root, witness)), "Inspection leaves native state and frozen evidence unchanged.");
    }

    private static void RepeatedChanges()
    {
        var root = A.Create();
        var slots = A.Links(root).Length;
        long[] values = [20, 21, 22, 23, 22, 21, 20, 20, 32, 32, 20, 20, 21, 24, 21, 20, 20, 20];
        long sequence = 0;
        for (var repetition = 0; repetition < 4; repetition++)
        foreach (var value in values)
        {
            Observe(ref root, ++sequence, value);
            Require(A.Links(root).Length == slots, "Pattern changes do not add duplicate helper slots.");
            Require(A.Links(root).All(link => A.Receipt(link).IsAttached((Relation)A.Parts(link)[4]) == (A.Gate(link).Value > 0)),
                "Physical publications agree with current supported participation through repeated changes.");
        }
        AssertNativeGate(root);
    }

    private static void AssertNativeGate(Relation root)
    {
        var nodes = A.Nodes(root);
        foreach (var link in A.Links(root)) A.Gate(link).Set(0);
        foreach (var node in nodes) { A.Raw(node).Set(0); A.Request(node).Set(0); }
        var selected = FindLink(root, producer: 0, consumer: 2);
        var publication = (Relation)A.Parts(selected)[4];
        A.Receipt(selected).Attach(publication);
        var producer = A.ProducerPort(selected);
        var request = A.ConsumerRequest(selected);
        producer.Set(1);
        request.Set(1);
        Require(A.Receipt(selected).Value == 0 && A.Help(nodes[2]).Value == 0,
            "A zero gate blocks a real producer and requested consumer.");
        A.Gate(selected).Set(1);
        Require(A.Receipt(selected).Value == 1 && A.Help(nodes[2]).Value == 1,
            "The attached native product supplies exactly one receipt without another learner call.");
        producer.Set(0);
        Require(A.Receipt(selected).Value == 0 && A.Help(nodes[2]).Value == 0,
            "The product responds when producer participation is removed.");
        producer.Set(1);
        request.Set(0);
        Require(A.Receipt(selected).Value == 0 && A.Help(nodes[2]).Value == 0,
            "The product requires the consumer's current request.");
        request.Set(1);
        A.Gate(selected).Set(0);
        Require(A.Receipt(selected).Value == 0 && A.Help(nodes[2]).Value == 0,
            "Closing the gate withdraws its actual receipt.");
    }

    private static void PhysicalLifecycle()
    {
        var root = A.Create();
        var link = FindLink(root, producer: 3, consumer: 2);
        var publication = (Relation)A.Parts(link)[4];
        Require(!A.Receipt(link).IsAttached(publication), "An unprobed connection starts physically dormant.");
        Observe(ref root, 1, 20);
        Observe(ref root, 2, 20);
        Observe(ref root, 3, 20);
        Observe(ref root, 4, 32);
        Require(A.Receipt(link).IsAttached(publication), "A new probe attaches its actual publication after the discovery frame.");
        for (long sequence = 5; sequence <= 24; sequence++) Observe(ref root, sequence, 32);
        Require(!A.Receipt(link).IsAttached(publication), "Fading support removes the publication from settlement participation.");
        Require(A.Receipt(link).Value == 0 && A.Receipt(link).Tension == 0,
            "The completed frame cleared the receipt before physical withdrawal.");
        var reprobe = Observe(ref root, 25, 44);
        Require(A.WasProposed(LinkSample(reprobe, link)) && A.Receipt(link).IsAttached(publication),
            "Later useful input reattaches the same dormant relation, not a duplicate publication.");
        var later = Observe(ref root, 26, 56);
        Require(A.SampleReceipt(LinkSample(later, link)) == 1, "The reattached connection works on a subsequent arrival.");
    }

    private static Relation FlatThenJump(out Relation witness, out Relation link)
    {
        var root = A.Create();
        Observe(ref root, 1, 20);
        Observe(ref root, 2, 20);
        Observe(ref root, 3, 20);
        witness = Observe(ref root, 4, 32);
        link = FindLink(root, producer: 3, consumer: 2);
        return root;
    }

    private static Relation FindLink(Relation root, int producer, int consumer)
    {
        var nodes = A.Nodes(root);
        return A.Links(root).Single(link => ReferenceEquals(A.ProducerPort(link), A.Raw(nodes[producer])) &&
            ReferenceEquals(A.ConsumerRequest(link), A.Request(nodes[consumer])));
    }

    private static Relation LinkSample(Relation witness, Relation link) =>
        A.LinkSamples(witness).Single(sample => ReferenceEquals(A.SampleProducer(sample), A.ProducerPort(link)) &&
            ReferenceEquals(A.SampleConsumer(sample), A.ConsumerRequest(link)));

    private static Relation Observe(ref Relation root, long sequence, long? value)
    {
        Require(A.TryObserve(root, sequence, value, out var next, out var witness), "The authored input frame is admitted.");
        root = next;
        return witness;
    }

    private static void AssertIncomplete(Relation witness)
    {
        Require(!A.Complete(witness), "Insufficient input remains explicitly incomplete.");
        Require(A.NodeSamples(witness).All(sample => A.SampleRaw(sample) == 0 &&
            A.SampleRequest(sample) == 0 && A.SampleHelp(sample) == 0), "An incomplete frame publishes no success or help request.");
        Require(A.LinkSamples(witness).All(sample => !A.WasProposed(sample) && !A.WasUseful(sample)),
            "Incomplete input cannot recruit or reward a connection.");
    }

    private static string Controls(Relation root) => string.Join(";", A.Nodes(root)
        .Select(node => $"{A.Engagement(node).Value}|{A.Engagement(node).Tension}")) + "/" +
        string.Join(";", A.Links(root).Select(link => $"{A.Support(link).Value}|{A.Support(link).Tension}"));

    private static void RejectWithoutMutation(Relation root, long sequence, long? value)
    {
        var before = GojoNumericFormat.Encode(root);
        Require(!A.TryObserve(root, sequence, value, out var rejected, out _), "A duplicate, backward or stale frame is rejected.");
        Require(ReferenceEquals(root, rejected), "Rejected admission returns its unchanged root.");
        Require(before.AsSpan().SequenceEqual(GojoNumericFormat.Encode(root)), "Rejected admission makes no partial physical update.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Connection assembly check failed: " + message);
    }
}
