using Gojo.Experiments.PrimitiveMeasure;

if (args.Contains("--connections"))
{
    ConnectionExamples.Run();
    return;
}

Console.WriteLine("O D V: orientation / direction / zero-or-nonzero");
Console.WriteLine("O: 0 positive-right, 1 positive-left. D: 0 positive, 1 negative. V: 0 zero, 1 nonzero.");
for (byte word = 0; word < 8; word++)
    Console.WriteLine($"{Convert.ToString(word, 2).PadLeft(3, '0')}  {Label(word)}");
var packed = ThreeBitMeasure.Pack([0, 1, 2, 3, 4, 5, 6, 7]);
Console.WriteLine($"\nEight words packed in order: {Convert.ToHexString(packed)} ({packed.Length} bytes, no header)");
Console.WriteLine("\nAddition in the positive-right orientation (? means nonzero of unknown magnitude):");
for (byte a = 0; a < 4; a++)
    for (byte b = 0; b < 4; b++)
        Console.WriteLine($"{Label(a)} + {Label(b)} = {string.Join(" or ", ThreeBitMeasure.Add(a, b).Select(Label))}");
Console.WriteLine("\nAt this resolution: -? < either zero < +?; identical words compare equal.");
Console.WriteLine("+? - +? can be negative, zero or positive: equal encodings do not prove equal magnitudes.");
Console.WriteLine($"Right-positive nonzero reoriented: 001 -> {Convert.ToString(ThreeBitMeasure.Reorient(1), 2).PadLeft(3, '0')}");
Console.WriteLine("Different orientations remain incomparable until that explicit change of perspective.");

Console.WriteLine("\nOne shared word has two opposed reads; neither read owns a second amount:");
byte shared = 0;
foreach (var next in new byte[] { 1, 3, 2, 5, 7 })
{
    shared = next;
    Console.WriteLine($"{Label(shared)} -> positive {Label(ThreeBitMeasure.ReadSide(shared, false))}, negative {Label(ThreeBitMeasure.ReadSide(shared, true))}");
}

Console.WriteLine("\nPrediction, actual merge, then conditional (opening order: negative / zero / positive):");
Console.WriteLine("Fine integers below belong only to the demonstration world; the predictor cannot inspect them.");
foreach (var (first, second) in new (long, long)[] { (5, -2), (2, -2), (2, -5) })
{
    long[] separate = [first, second];
    var a = FineFlowFixture.Observe(separate[0]);
    var b = FineFlowFixture.Observe(separate[1]);
    var prediction = ThreeBitMeasure.Add(a, b);
    var possibleBranches = prediction.Select(w => SignPassage.Select(w, SignPassage.All)!.Value).Distinct().Order();
    long[] cells = [FineFlowFixture.Join(separate), 0, 0, 0];
    var actual = cells[0];
    var observed = FineFlowFixture.Observe(actual);
    var selected = FineFlowFixture.Route(cells, SignPassage.All);
    Console.WriteLine($"{Label(a)} + {Label(b)} predicts [{string.Join(", ", possibleBranches.Select(BranchName))}]; " +
        $"world {first} + ({second}) settles {actual}, observed {Label(observed)} -> {BranchName(selected!.Value)}; " +
        $"outlets [{string.Join(", ", cells.Skip(1))}]");
}
Console.WriteLine("In this net-only fixture, zero selected a branch but retained no opposed participation.");
Console.WriteLine("Run with --connections to compare empty zero with retained balanced participation.");

long[] opposing = [2, -5];
long[] filtered = [FineFlowFixture.Join(opposing), 0, 0, 0];
var blocked = FineFlowFixture.Route(filtered, SignPassage.Positive);
Console.WriteLine($"\nOnly the positive opening exists: negative settlement is {(blocked is null ? "held" : "passed")}; source {filtered[0]}, outlets [{string.Join(", ", filtered.Skip(1))}].");
var released = FineFlowFixture.Route(filtered, SignPassage.All);
Console.WriteLine($"Opening the negative passage releases that held flow to {BranchName(released!.Value)}: [{string.Join(", ", filtered)}].");

long[] communicating = [5, -2];
FineFlowFixture.Transfer(communicating, SignPassage.All);
Console.WriteLine($"\nTwo separate stores, open passage: [5, -2] -> [{string.Join(", ", communicating)}].");
// Explicit new inputs in the fine demonstration world, not an ODV operation.
communicating[0] += 2;
communicating[1] -= 5;
FineFlowFixture.Transfer(communicating, 0);
Console.WriteLine($"After local inputs +2 and -5, with passage closed: [{string.Join(", ", communicating)}]. Both stores can now be occupied.");
FineFlowFixture.Transfer(communicating, SignPassage.All);
Console.WriteLine($"Reopen the passage: [{string.Join(", ", communicating)}]. These are still two stores, even when their behavior resembles one flow.");

static string BranchName(int position) => new[] { "negative", "zero", "positive" }[position];
static string Label(byte word) => $"{((word & 4) == 0 ? "R" : "L")}{((word & 2) == 0 ? "+" : "-")}{((word & 1) == 0 ? "0" : "?")}";
