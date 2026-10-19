namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    // Screen interpretations only. Geometry, grouping and attachment continue
    // to come from the imported native packet.
    private static readonly Palette[] RefinedPalettes =
    [
        new("Parchment, indigo & rust", "#F2E8D5", "#365870", "#B7664B", "#D5B978", "#B99B53", "#FFF6E1", "#283E49", .12),
        new("Chalk, forest & rose", "#F0EDE2", "#3D655D", "#AD7480", "#C6B675", "#B69A59", "#FFF8E8", "#2C403A", .12),
        new("Midnight, flax & copper", "#253A50", "#AFC2B7", "#C88C62", "#F0DFC0", "#C6AA71", "#FCF1D9", "#182C3E", .12),
        new("Sand, teal & vermilion", "#EAD9BB", "#327477", "#BC5948", "#E2C574", "#B99850", "#FFF1D5", "#28474B", .12),
        new("Ink, porcelain & coral", "#243F46", "#DEDCC9", "#BF7967", "#D7BA71", "#BEA268", "#FFF4DE", "#18343B", .12),
        new("Shell, aubergine & olive", "#EFE1D8", "#674D68", "#8C9B6F", "#C89562", "#BC9F64", "#FFF4E4", "#3B3543", .12),
        new("Ivory, lapis & ochre", "#F2EAD7", "#375E87", "#C08E4F", "#7E9B94", "#C4A15B", "#FFF7E6", "#2C4056", .12),
        new("Fir, celadon & apricot", "#294A42", "#A9BBA3", "#D39770", "#EEE3C4", "#C9AC6F", "#F9F0D9", "#19372F", .12),
        new("Terracotta, shell & charcoal", "#A75F4D", "#E9D9B9", "#405B5B", "#D9B77D", "#D0AD65", "#FFF1D5", "#3D3A35", .12),
        new("Mist, petrol & raspberry", "#DDE4DE", "#386671", "#A2596D", "#C3AB66", "#B19A58", "#FCF4DF", "#31464D", .12),
        new("Plum, sage & champagne", "#554352", "#A7B29B", "#D7B190", "#EAE1C7", "#C7AD79", "#FFF3DC", "#302E3C", .12),
        new("Linen, walnut & turquoise", "#EEE5D4", "#755B47", "#4B8C8D", "#C78B66", "#BE9B5D", "#FFF6E4", "#3E3D36", .12),
        new("Saffron, dark blue & cream", "#C5A15A", "#305365", "#EEE1C3", "#9A6355", "#DDC68B", "#FFF3D8", "#263F4A", .12),
        new("Pale jade, burgundy & brass", "#DAE1CA", "#6F3E4B", "#B89456", "#6C8E80", "#B69E62", "#FFF6DC", "#3B4341", .12),
        new("Slate, lavender & warm ivory", "#344650", "#A696B1", "#E2C59D", "#CAD5C0", "#C4AC74", "#FFF1D6", "#25353D", .12),
        new("Warm white, moss & carmine", "#F3EFE3", "#6D8058", "#A34E53", "#CDA76A", "#B99C5B", "#FFF8E6", "#39483C", .12),
        new("Dusty blue, ink & pale gold", "#A6BCC6", "#304A62", "#E3D1A7", "#AE7164", "#C3A569", "#FFF1D6", "#293C50", .12),
        new("Garnet, sea green & ivory", "#713F48", "#7FA49C", "#E3D3B2", "#C39A65", "#C9AF76", "#FFF1D8", "#3A303C", .12)
    ];

    private static Palette RefinedPalette(string source, string mode)
    {
        if (source == "B13") return new("Muted blue, clay, flax & olive chambers", "#EEE7D7", "#68858A", "#AE806D", "#D4C7A7", "#A4AB86", "#F4EDDA", "#45565A", .12);
        if (source == "B14") return new("Muted teal, mauve, flax & ochre chambers", "#EEE7D7", "#6B8E88", "#A08095", "#D0C2A3", "#B5A477", "#F4EDDA", "#4C5554", .12);
        if (source == "B32") return mode switch
        {
            "field" => new("Ivory, ink-blue & sand bands", "#F0E7D4", "#325866", "#A76445", "#EFE6D0", "#C3AA79", "#F9F3E4", "#223F4C", .12),
            "motif" => new("Indigo, ivory & brass bands", "#273C58", "#EEE6D2", "#2B6265", "#BB8052", "#BFA267", "#FAF1DB", "#182D42", .12),
            _ => new("Ivory, terracotta & blue bands", "#EEE9D8", "#A75847", "#325965", "#E8D5AE", "#D2BA80", "#FAF4DF", "#28454F", .12)
        };
        var number = int.Parse(source.AsSpan(1, 2));
        var reading = mode == "field" ? 0 : mode == "motif" ? 1 : 2;
        return RefinedPalettes[(number * 7 + reading * 5 + (source.EndsWith(".B", StringComparison.Ordinal) ? 4 : 0)) % RefinedPalettes.Length];
    }
}
