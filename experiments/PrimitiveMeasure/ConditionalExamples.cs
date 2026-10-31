namespace Gojo.Experiments.PrimitiveMeasure;

public static class ConditionalExamples
{
    public static void Run()
    {
        Console.WriteLine("Overview: the previous pass moved contents along one gated path.");
        Console.WriteLine("This pass chooses between two outlets, then continues through the structure attached to the chosen outlet.");
        Console.WriteLine("C present sends source S to Yes; C absent sends S to No. Both paths can continue into shared Done.");
        Console.WriteLine("An eight-byte transition law is a compact table of allowed complete before/after patterns for one passage.");
        Console.WriteLine("The names explain the diagram; the actual connections and law bits determine the behavior. V=1 is still an unmeasured amount.");

        foreach (var control in new[] { false, true })
        {
            var example = new Example(control ? (byte)3 : (byte)1);
            Console.WriteLine($"\nResolved C={(control ? 1 : 0)}; stored order [S,C,Yes,No,Done].");
            example.Select();
            example.Continue();
            example.Select("try selection again with source empty");
        }

        Console.WriteLine("\nBlocking the selected outlet does not select the other route:");
        new Example(7).Select("C=1, Yes already occupied");
        new Example(9).Select("C=0, No already occupied");
        new Example(0).Select("C=0, but source empty: no new contents");

        Console.WriteLine("\nOnce routed, a change of C does not veto the continuation:");
        var changed = new Example(3);
        changed.Select();
        changed.Owner.StoreObservation(1, false, false, 0);
        changed.Show("supply C=0 after choosing Yes");
        changed.Continue();

        var uncertain = new Example(3);
        Console.WriteLine("\nC is unobserved: keep both complete possibilities; execute neither prediction.");
        var before = uncertain.Present.ObserveScope(uncertain.Ports, 0b11101);
        var afterPresent = uncertain.Present.PredictScope(uncertain.Yes, uncertain.Ports, before);
        var afterChoice = uncertain.Absent.PredictScope(uncertain.No, uncertain.Ports, afterPresent);
        var afterYes = uncertain.Forward.PredictScope(uncertain.YesNext, uncertain.Ports, afterChoice);
        var afterDone = uncertain.Forward.PredictScope(uncertain.NoNext, uncertain.Ports, afterYes);
        Console.WriteLine($"scope supports: {before:X8} -> {afterPresent:X8} -> {afterChoice:X8} -> {afterYes:X8} -> {afterDone:X8}");
        Console.WriteLine($"chosen outlet: {States(afterChoice)}");
        Console.WriteLine($"after continuations: {States(afterDone)}; both predict Done present while retaining uncertainty about C.");
        var choiceObservation = uncertain.Present.ObserveScope(uncertain.ChoicePorts, 0b1101);
        var accepted = PassageConnections.TrySequence(uncertain.ChoicePorts, choiceObservation, out _,
            (uncertain.Present, uncertain.Yes), (uncertain.Absent, uncertain.No));
        uncertain.Show($"attempt unresolved selection: accepted={accepted}");
        uncertain.Select("reveal C, then select");
        uncertain.Continue();

        Console.WriteLine("\nConcrete encoding: all five sites have fixed O=0/D=0 and one V bit.");
        Console.WriteLine("Port/binding/value-bit positions: S=0, C=1, Yes=2, No=3, Done=4.");
        Console.WriteLine($"C present: row [0,1,2], bytes {Convert.ToHexString(uncertain.Present.PackedRows(3))}; law {Convert.ToHexString(uncertain.Present.LawBytes())}");
        Console.WriteLine($"C absent: row [0,1,3], bytes {Convert.ToHexString(uncertain.Absent.PackedRows(3))}; law {Convert.ToHexString(uncertain.Absent.LawBytes())}");
        Console.WriteLine($"Continue: rows [2,1,4] and [3,1,4], bytes {Convert.ToHexString(uncertain.Forward.PackedRows(3))}; law {Convert.ToHexString(uncertain.Forward.LawBytes())}");
        Console.WriteLine("Selection uses both complementary rows as one owner-local step. Continuations are separate explicit steps.");
        Console.WriteLine("Only the selected outlet receives this source's new flow; older contents in other structures remain their own participation.");
        Console.WriteLine("Done's presence alone does not remember which route was taken. No branch-history bit or conditional opcode was added.");
    }

    // This helper only assembles and presents ordinary stores and encoded rows;
    // execution never dispatches on a Yes/No label or on a host if(control).
    private sealed class Example
    {
        public readonly PortStructure Owner = new();
        public readonly int[] Ports;
        public readonly int[] ChoicePorts;
        public readonly PassageConnections Present;
        public readonly PassageConnections Absent;
        public readonly PassageConnections Forward;
        public readonly ConnectionRow Yes;
        public readonly ConnectionRow No;
        public readonly ConnectionRow YesNext;
        public readonly ConnectionRow NoNext;

        public Example(byte initial)
        {
            Ports = Enumerable.Range(0, 5).Select(site => Owner.Publish(Owner.Create(0, 0, 1, (ulong)((initial >> site) & 1)))).ToArray();
            ChoicePorts = Ports[..4];
            Present = new PassageConnections(Owner, PassageLaw.Controlled());
            Absent = new PassageConnections(Owner, PassageLaw.Controlled(false));
            Forward = new PassageConnections(Owner, PassageLaw.Unconditional());
            Yes = Present.Connect(3, Ports[0], Ports[1], Ports[2]);
            No = Absent.Connect(3, Ports[0], Ports[1], Ports[3]);
            YesNext = Forward.Connect(3, Ports[2], Ports[1], Ports[4]);
            NoNext = Forward.Connect(3, Ports[3], Ports[1], Ports[4]);
            Show("initial");
        }

        public void Select(string label = "choose outlet using one C observation")
        {
            var before = Present.ObserveScope(ChoicePorts, 15);
            var accepted = PassageConnections.TrySequence(ChoicePorts, before, out _, (Present, Yes), (Absent, No));
            Show($"{label}: accepted={accepted}");
        }

        public void Continue()
        {
            var yes = Forward.TryStep(YesNext, Forward.Observe(YesNext), out _);
            Show($"step Yes continuation: accepted={yes}");
            var no = Forward.TryStep(NoNext, Forward.Observe(NoNext), out _);
            Show($"step No continuation: accepted={no}");
        }

        public void Show(string label)
        {
            var value = Owner.PackedBytes(0, 0, 1)[0];
            Console.WriteLine($"{label}: {Tuple(value)}; stored byte {value:X2}");
        }
    }

    private static string Tuple(int state) => "[" + string.Join(',', Enumerable.Range(0, 5).Select(site => (state >> site) & 1)) + "]";
    private static string States(uint support) => "{" + string.Join(" or ", Enumerable.Range(0, 32)
        .Where(state => (support & (1u << state)) != 0).Select(Tuple)) + "}";
}
