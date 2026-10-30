using Gojo.Experiments.PrimitiveMeasure;

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

static string Label(byte word) => $"{((word & 4) == 0 ? "R" : "L")}{((word & 2) == 0 ? "+" : "-")}{((word & 1) == 0 ? "0" : "?")}";
