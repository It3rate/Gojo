namespace Gojo.EncodingC;

internal enum DialogueQuestion { Owner, Referent, Location, SharedOwner }
internal enum DialogueScope { Local, Context, Direct }
internal enum DialogueClue { Context, Identification, Witness }

/// <summary>A single question about two possible cars. The driver posts what
/// each speaker says. The selected evidence rows and the exchange decide what
/// the speakers still owe each other.</summary>
internal sealed class ShortDialogue
{
    private static readonly Field Sample = Field.Bit(2);
    private static readonly RelationLaw EqualBits = RelationLaw.FromPredicate(
        [Shape.Bit, Shape.Bit], values => values[0] == values[1]);

    private readonly Engine evidence;
    private readonly DialogueExchange exchange;
    private readonly Store utterances;
    private readonly Supports answer, referent;

    private ShortDialogue(Engine evidence, DialogueExchange exchange)
    {
        this.evidence = evidence;
        this.exchange = exchange;
        utterances = (Store)evidence.Wirings[0].Bindings[0].Surface;
        var repairInputs = evidence.Wirings[^1].Bindings;
        answer = Open((Store)repairInputs[0].Surface);
        referent = Open((Store)repairInputs[1].Surface);
    }

    public static ShortDialogue Create(DialogueQuestion question, DialogueScope scope = DialogueScope.Local)
    {
        // The digits are values of the selected role. They have no distance or
        // ordering as names of cars, owners or places.
        int[] answers = question switch
        {
            DialogueQuestion.Owner or DialogueQuestion.Referent => [0, 1],
            DialogueQuestion.Location => [1, 0],
            DialogueQuestion.SharedOwner => [0, 0],
            _ => throw new ArgumentOutOfRangeException(nameof(question)),
        };
        int[] nearReplies = question == DialogueQuestion.Location ? [0, 0] : [0, 1];
        int[] identificationReplies = [0, 1];

        var utterances = new Store(2, 3);
        var answer = NewOpen();
        var car = NewOpen();
        var description = NewOpen();
        var relay = NewOpen(3);
        var evidence = new Engine();

        // A local reply can arrive directly or through a description of the car.
        var near = Hear(utterances, DialogueClue.Context, car, 0, nearReplies);
        var identify = Hear(utterances, DialogueClue.Identification, description, 0, identificationReplies);
        var describes = Add(EqualBits, description.Variable, car.Variable, 0, 0);

        // An independent witness can make a claim about the answer itself. Its
        // report is behind a separate chain in the evidence graph.
        var witness = Hear(utterances, DialogueClue.Witness, relay, 2, [0, 1]);
        var witnessRelay = Add(EqualBits, relay.Variable, relay.Variable, 2, 1);
        var witnessRoute = Add(EqualBits, relay.Variable, relay.Variable, 1, 0);
        var report = Add(EqualBits, relay.Variable, answer.Variable, 0, 0);

        var property = Add(RelationLaw.FromPredicate([Shape.Bit, Shape.Bit], values => answers[values[0]] == values[1]),
            car.Variable, answer.Variable, 0, 0);
        evidence.Add(near).Add(identify).Add(describes).Add(witness).Add(witnessRelay).Add(witnessRoute).Add(report).Add(property);

        int reach = scope switch
        {
            DialogueScope.Direct => 1,
            DialogueScope.Local => 2,
            DialogueScope.Context => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(scope)),
        };
        var cone = new Question(answer, 0).Cone(evidence, reach);
        cone.Attend();

        // The probe costs are the number of evidence rows each reply needs
        // to reach the car. The answer relation is shared by both probes.
        DialogueProbe Probe(int[] replies, params Wiring[] route) => new(replies[0], replies[1],
            route.Sum(wiring => wiring.Rows.Count), route.All(wiring => cone.Rows(wiring).Count == wiring.Rows.Count));
        DialogueProbe[] probes = [Probe(nearReplies, near), Probe(identificationReplies, identify, describes)];
        var repairs = DialogueRepairs.Add(evidence, (Store)answer.Surface, (Store)car.Surface, answers, probes);
        return new ShortDialogue(evidence, DialogueExchange.Create((Store)answer.Surface, repairs));
    }

    public void Ask() => exchange.Ask();

    public void Observe(DialogueClue clue, int answer)
    {
        if ((uint)clue >= (uint)utterances.Count) throw new ArgumentOutOfRangeException(nameof(clue));
        if ((uint)answer > 1) throw new ArgumentOutOfRangeException(nameof(answer));
        var site = (int)clue;
        // Each lane represents one authored utterance. A second claim goes in
        // a second lane, so it remains present even when it disagrees.
        if (utterances.Read(0, site) && utterances.Read(1, site) != (answer == 1))
            throw new InvalidOperationException("This utterance already has a reply.");
        utterances.Write(1, site, answer == 1);
        utterances.Write(0, site, true);
    }

    public void Acknowledge(int answer) => exchange.Acknowledge(answer);

    public DialogueReading Read() => exchange.Read();

    public ulong AnswerSupports => answer.Values(0);

    public ulong ReferentSupports => referent.Values(0);

    public bool Step()
    {
        var evidenceStep = evidence.Step();
        var exchangeStep = exchange.Engine.Step();
        return evidenceStep.Steps.All(step => step.Quiet) && exchangeStep.Steps.All(step => step.Quiet);
    }

    public byte[] Save() => Transport.SaveLocals(evidence, exchange.Engine);

    public static ShortDialogue Restore(byte[] saved)
    {
        var owners = Transport.LoadLocals(saved);
        return new ShortDialogue(owners[0].Engine, DialogueExchange.Restore(owners[1].Engine));
    }

    private static Supports Open(Store store) => new(store, Sample, 0, 1);

    private static Supports NewOpen(int count = 1)
    {
        var store = new Store(3, count);
        for (var site = 0; site < count; site++)
        {
            store.Write(0, site, true);
            store.Write(1, site, true);
        }
        return Open(store);
    }

    private static Wiring Hear(Store utterances, DialogueClue clue, Supports target, int position, int[] replies)
    {
        var law = RelationLaw.FromPredicate([Shape.Bit, Shape.Bit, Shape.Bit],
            values => values[0] == 0 || values[1] == replies[values[2]]);
        var wiring = new Wiring(law, new Binding(utterances, 0), new Binding(utterances, 1), target.Variable);
        wiring.Add((int)clue, (int)clue, position);
        return wiring;
    }

    private static Wiring Add(RelationLaw law, Binding first, Binding second, int source, int target)
    {
        var wiring = new Wiring(law, first, second);
        wiring.Add(source, target);
        return wiring;
    }
}
