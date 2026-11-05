namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Authored examples for the encoding experiment. Names select fixtures
/// for presentation; the returned template contains their actual connections.</summary>
public static class FamilyExamples
{
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(new[]
    {
        "Bare signed amount", "Independent opposed pair", "Shared-tip pair",
        "Three-arm hub", "Cycle · independent values", "Cycle · shared value"
    });

    public static FamilyTable Build(int example)
    {
        var template = example switch
        {
            0 => new FamilyTemplate([new(0, 2)], []),
            1 => new FamilyTemplate([new(0, 0), new(0, 1)], [new(0, 1, 0), new(0, 2, 1)]),
            2 => new FamilyTemplate([new(0, 0), new(1, 0)], [new(0, 2, 0), new(1, 2, 1)]),
            3 => new FamilyTemplate([new(0, 0), new(0, 0), new(0, 0)],
                [new(0, 1, 0), new(0, 2, 1), new(0, 3, 2)]),
            4 => new FamilyTemplate([new(0, 0), new(0, 0), new(0, 0), new(0, 0)],
                [new(0, 1, 0), new(1, 2, 1), new(2, 3, 2), new(3, 0, 3)]),
            5 => new FamilyTemplate([new(0, 0)],
                [new(0, 1, 0), new(1, 2, 0), new(2, 3, 0), new(3, 0, 0)]),
            _ => throw new ArgumentOutOfRangeException(nameof(example))
        };
        var table = new FamilyTable(template);
        foreach (var width in new[] { 1, 5 })
        {
            if (example == 0)
            {
                table.Append(width, template.Encode(0, width, false, false, 1));
                table.Append(width, template.Encode(0, width, false, true, 1));
                table.Append(width, template.Encode(0, width, false, true, 0));
            }
            else
            {
                table.Append(width, Enumerable.Repeat(1UL, template.FieldCount).ToArray());
                table.Append(width, Enumerable.Range(0, template.FieldCount).Select(i => i % 2 == 0 ? 1UL : 0UL).ToArray());
                table.Append(width, new ulong[template.FieldCount]);
            }
        }
        return table;
    }
}
