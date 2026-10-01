using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Core;
using SkiaSharp;
using static PosterScratchpad.PosterSurfaces;

namespace PosterScratchpad;

// Three new compositions. They reuse graphic primitives, not the first pair's
// layout or logos. No evaluation data is used by these construction methods.
internal static class IndependentPosters
{
    internal static Relation C()
    {
        var paper = Color(245, 243, 236);
        var navy = Color(33, 49, 70);
        var red = Color(206, 68, 43);
        var logo = Group(
            Ink(Circle(735, 799, 207), navy), Ink(Circle(735, 799, 194), paper),
            Ink(Circle(735, 799, 168), navy), Ink(Circle(735, 799, 155), paper),
            Ink(Circle(735, 799, 129), navy), Ink(Circle(735, 799, 116), paper),
            Ink(Polygon((735, 799), (985, 538), (1040, 572), (1040, 735)), paper),
            Ink(Polygon((850, 679), (875, 654), (937, 716), (912, 741)), red));
        return Document(
            Ink(Rectangle(0, 0, 1080, 1440), paper),
            Ink(Text("AN OPEN INVITATION", 76, 97, 21, 760, "Arial", 400), navy),
            Ink(Rectangle(76, 132, 928, 2), navy),
            Ink(Text("Gather", 65, 376, 187, 930, "Georgia", 400), navy),
            Ink(Rectangle(80, 449, 119, 10), red),
            logo,
            Ink(Rectangle(76, 1101, 928, 2), navy),
            Ink(Text("SEPT 29, 2006", 76, 1170, 31, 620, "Arial", 400), navy),
            Ink(Text("2:00PM", 74, 1247, 62, 560, "Georgia", 400), navy),
            Ink(Text("$10", 814, 1224, 76, 192, "Georgia", 400), navy),
            Ink(Text("ENTRANCE FEE", 816, 1270, 19, 188, "Arial", 400), navy));
    }

    internal static Relation D()
    {
        var yellow = Color(255, 209, 48);
        var blue = Color(37, 66, 209);
        var pink = Color(250, 123, 166);
        var green = Color(162, 232, 103);
        var paper = Color(255, 248, 229);
        var black = Color(22, 26, 29);
        var logo = Group(
            Ink(Polygon((535, 402), (737, 461), (586, 647), (535, 688), (484, 534)), pink),
            Ink(Polygon((823, 688), (764, 890), (578, 739), (535, 688), (691, 637)), green),
            Ink(Polygon((535, 977), (333, 918), (484, 732), (535, 688), (586, 845)), yellow),
            Ink(Polygon((247, 688), (306, 486), (492, 637), (535, 688), (380, 739)), paper),
            Ink(Circle(535, 688, 58), blue));
        return Document(
            Ink(Rectangle(0, 0, 1080, 1440), yellow),
            Ink(Text("AN OPEN INVITATION", 56, 79, 23, 920, "Arial", 700), black),
            Ink(Text("GATHER", 45, 278, 229, 987, "Impact", 400), black),
            Ink(Polygon((81, 342), (1021, 365), (984, 1034), (55, 1011)), blue),
            logo,
            Ink(Rectangle(55, 1080, 610, 300), paper),
            Ink(Rectangle(694, 1080, 332, 300), pink),
            Ink(Text("SEPT 29, 2006", 81, 1171, 43, 562, "Arial", 700), black),
            Ink(Text("2:00PM", 78, 1298, 98, 559, "Arial", 700), black),
            Ink(Text("$10", 752, 1247, 116, 230, "Arial", 700), black),
            Ink(Text("ENTRANCE FEE", 739, 1322, 26, 245, "Arial", 700), black));
    }

    internal static Relation E()
    {
        var paper = Color(239, 236, 226);
        var ink = Color(25, 27, 29);
        var logo = Group(
            Ink(Rectangle(470, 424, 474, 474), paper),
            Ink(Rectangle(500, 454, 414, 414), ink),
            Ink(Rectangle(560, 514, 294, 294), paper),
            Ink(Rectangle(590, 544, 234, 234), ink),
            Ink(Rectangle(650, 604, 114, 114), paper),
            Ink(Rectangle(680, 634, 54, 54), ink),
            Ink(Rectangle(430, 661, 277, 54), ink));
        return Document(
            Ink(Rectangle(0, 0, 1080, 1440), ink),
            Ink(Rectangle(0, 0, 374, 1440), paper),
            Ink(Text("SEPTEMBER", 46, 147, 28, 298, "Arial", 400), ink),
            Ink(Text("29", 29, 401, 250, 319, "Impact", 400), ink),
            Ink(Text("2006", 48, 472, 58, 273, "Arial", 400), ink),
            Ink(Text("GATHER", 427, 150, 128, 602, "Impact", 400), paper),
            logo,
            Ink(Rectangle(46, 1020, 280, 2), ink),
            Ink(Text("2:00PM", 46, 1123, 61, 288, "Arial", 700), ink),
            Ink(Text("$10", 45, 1260, 99, 287, "Arial", 700), ink),
            Ink(Text("ENTRANCE FEE", 48, 1320, 22, 281, "Arial", 400), ink),
            Ink(Rectangle(428, 1216, 590, 2), paper),
            Ink(Text("AN OPEN INVITATION", 430, 1281, 23, 584, "Arial", 400), paper));
    }

    internal static Relation[] Build() => [C(), D(), E()];

    internal static void Generate(string output)
    {
        Directory.CreateDirectory(output);
        var documents = Build();
        var names = new[] { "c", "d", "e" };
        var manifest = new List<string>();
        for (var index = 0; index < documents.Length; index++)
        {
            var png = Path.Combine(output, $"poster-{names[index]}.png");
            PosterRenderer.Render(documents[index], png);
            File.WriteAllText(Path.Combine(output, $"poster-{names[index]}.snapshot.gojo.json"),
                JsonSerializer.Serialize(GojoEncodedSnapshot.Capture(documents[index]), new JsonSerializerOptions { WriteIndented = true }));
            manifest.Add($"{names[index]} SHA256: {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(png)))}");
        }
        // A view over the same three graphs, not a fourth poster composition.
        using var surface = SKSurface.Create(new SKImageInfo(1620, 720));
        surface.Canvas.Scale(0.5f);
        for (var index = 0; index < documents.Length; index++)
        {
            surface.Canvas.Save();
            surface.Canvas.Translate(index * 1080, 0);
            PosterRenderer.DrawGroup(surface.Canvas, (Relation)Parts(documents[index])[1]);
            surface.Canvas.Restore();
        }
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using (var stream = File.Create(Path.Combine(output, "three-posters.png"))) data.SaveTo(stream);
        File.WriteAllText(Path.Combine(output, "generation-manifest.txt"),
            $"Generated UTC: {DateTime.UtcNow:O}\n" + string.Join("\n", manifest) +
            "\nThese designs were generated before their assessment was authored.\n");
        Console.WriteLine($"Generated three independent posters at {output}");
    }
}
