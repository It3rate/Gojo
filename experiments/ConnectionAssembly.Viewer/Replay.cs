using System.Text.Json;

namespace ConnectionAssembly.Viewer;

// These types are rendering adapters for a recorded observation file. They do
// not participate in learning, calculate recognition, or infer connections.
internal sealed class Replay
{
    public string Title { get; set; } = "Connection assembly";
    public string[] NodeNames { get; set; } = [];
    public ReplayFrame[] Frames { get; set; } = [];

    public static Replay Load(string path)
    {
        var replay = JsonSerializer.Deserialize<Replay>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("The replay file has no document.");
        if (replay.NodeNames.Length != 6 || replay.Frames.Length == 0)
            throw new InvalidDataException("A replay requires six node names and at least one frame.");
        for (var i = 0; i < replay.Frames.Length; i++)
        {
            var frame = replay.Frames[i];
            if (frame.Nodes.Length != 6 || frame.Nodes.Select(node => node.Id).Distinct().Count() != 6 ||
                frame.Links.Any(link => link.From is < 0 or > 5 || link.To is < 0 or > 5 || link.From == link.To) ||
                frame.Links.Select(link => (link.From, link.To)).Distinct().Count() != frame.Links.Length ||
                frame.Value is double value && !double.IsFinite(value))
                throw new InvalidDataException($"Frame {i} does not follow the replay contract.");
            if (i > 0 && frame.Step <= replay.Frames[i - 1].Step)
                throw new InvalidDataException("Recorded steps must increase.");
        }
        return replay;
    }
}

internal sealed class ReplayFrame
{
    public int Step { get; set; }
    public string Phase { get; set; } = "";
    public double? Value { get; set; }
    public bool Complete { get; set; }
    public ReplayNode[] Nodes { get; set; } = [];
    public ReplayLink[] Links { get; set; } = [];
    public int? NativeRelations { get; set; }
    public int? NativeCarriers { get; set; }
}

internal sealed class ReplayNode
{
    public string Id { get; set; } = "";
    public int Raw { get; set; }
    public int Request { get; set; }
    public int Help { get; set; }
    public int Pressure { get; set; }
    public int Engagement { get; set; }
    public bool Accounted { get; set; }
}

internal sealed class ReplayLink
{
    public int From { get; set; }
    public int To { get; set; }
    public int Support { get; set; }
    public int Gate { get; set; }
    public int Receipt { get; set; }
    public bool Proposed { get; set; }
    public bool Useful { get; set; }
    public bool Released { get; set; }
}
