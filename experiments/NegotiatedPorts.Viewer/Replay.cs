using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NegotiatedPorts.Viewer;

// Observer transport only. These projections never enter the Gojo learner.
internal sealed class Replay
{
    public string Protocol { get; set; } = "";
    public string Title { get; set; } = "Negotiated ports";
    public string[] OfferNames { get; set; } = [];
    public Frame[] Frames { get; set; } = [];
    [JsonIgnore] public string SourcePath { get; private set; } = "";
    [JsonIgnore] public string Fingerprint { get; private set; } = "";

    public static Replay Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var replay = JsonSerializer.Deserialize<Replay>(bytes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("The replay has no document.");
        replay.SourcePath = Path.GetFullPath(path);
        replay.Fingerprint = Convert.ToHexString(SHA256.HashData(bytes))[..12];
        if (replay.Frames.Length == 0 || replay.OfferNames.Length != 4)
            throw new InvalidDataException("Expected four offer names and at least one recorded frame.");
        for (var index = 0; index < replay.Frames.Length; index++)
        {
            var frame = replay.Frames[index];
            if (!frame.Offers.Select(o => o.Id).Order().SequenceEqual(new[] { 0, 1, 2, 3 }) ||
                !frame.Producers.Select(p => p.Id).Order().SequenceEqual(new[] { 0, 1, 2 }) ||
                !frame.Consumers.Select(c => c.Id).Order().SequenceEqual(new[] { 0, 1 }) ||
                frame.Offers.Any(o => o.Provider is < 0 or > 2) ||
                frame.Consumers.Any(c => c.SelectedOffer is < 0 or > 3 || c.Previous.Offer is < 0 or > 3) ||
                frame.Contacts.Any(c => c.Consumer is < 0 or > 1 || c.Offer is < 0 or > 3) ||
                index > 0 && frame.Step <= replay.Frames[index - 1].Step)
                throw new InvalidDataException($"Frame {index} does not follow the negotiated-port replay contract.");
        }
        return replay;
    }
}

internal sealed class Frame
{
    public long Step { get; set; }
    public string Phase { get; set; } = "";
    public long? Input { get; set; }
    public long Reservoir { get; set; }
    public long Spent { get; set; }
    public bool Conserved { get; set; }
    public Offer[] Offers { get; set; } = [];
    public Producer[] Producers { get; set; } = [];
    public Consumer[] Consumers { get; set; } = [];
    public Contact[] Contacts { get; set; } = [];
    public Payment[] Payments { get; set; } = [];
}

internal sealed class Offer
{
    public int Id { get; set; }
    public int Provider { get; set; }
    public long Ask { get; set; }
    public long Cost { get; set; }
    public long? LiveValue { get; set; }
}
internal sealed class Producer
{
    public int Id { get; set; }
    public long Reserve { get; set; }
    public long Occupied { get; set; }
}
internal sealed class Consumer
{
    public int Id { get; set; }
    public long Wallet { get; set; }
    public long Bid { get; set; }
    public int Cursor { get; set; }
    public long Completed { get; set; }
    public bool Demand { get; set; }
    public int? SelectedOffer { get; set; }
    public long Gate { get; set; }
    public long? Receipt { get; set; }
    public long? Forecast { get; set; }
    public long Price { get; set; }
    public long ContactFee { get; set; }
    public bool Paid { get; set; }
    public Prior Previous { get; set; } = new();
}
internal sealed class Prior
{
    public bool Issued { get; set; }
    public bool Paid { get; set; }
    public bool Evaluated { get; set; }
    public long? IssueStep { get; set; }
    public int? Offer { get; set; }
    public long? Forecast { get; set; }
    public long? Actual { get; set; }
    public long? BaselineDemand { get; set; }
    public long? DeliveredDemand { get; set; }
    public long? Relief { get; set; }
    public long? Gross { get; set; }
    public long? OngoingNet { get; set; }
    public long? TotalNet { get; set; }
    public long? Price { get; set; }
    public long? ContactFee { get; set; }
    public bool? Demand { get; set; }
    public bool Retained { get; set; }
}
internal sealed class Contact
{
    public int Consumer { get; set; }
    public int Offer { get; set; }
    public long Bid { get; set; }
    public long Ask { get; set; }
    public long Fee { get; set; }
    public bool Paid { get; set; }
    public bool? Fits { get; set; }
    public bool? Free { get; set; }
    public bool Agreed { get; set; }
}
internal sealed class Payment
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public long Amount { get; set; }
}
