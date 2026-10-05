using System.Globalization;
using SkiaSharp;

namespace NegotiatedPorts.Viewer;

// This adapter places recorded observations on pixels. It does not calculate
// a prediction, infer a connection, normalize a port, or decide usefulness.
internal static class ReplayRenderer
{
    private static readonly SKColor Background = SKColor.Parse("#F0F3F5");
    private static readonly SKColor Ink = SKColor.Parse("#182C3B");
    private static readonly SKColor Muted = SKColor.Parse("#627581");
    private static readonly SKColor Border = SKColor.Parse("#D9E3E8");
    private static readonly SKColor Teal = SKColor.Parse("#147E76");
    private static readonly SKColor Purple = SKColor.Parse("#7556AA");
    private static readonly SKColor Amber = SKColor.Parse("#A76321");
    private static readonly SKColor Red = SKColor.Parse("#B4434C");
    private static readonly SKTypeface Regular = SKTypeface.FromFamilyName("Segoe UI");
    private static readonly SKTypeface Bold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);

    public static void Draw(SKCanvas canvas, float width, float height, Replay replay, int index)
    {
        canvas.Clear(Background);
        if (width <= 0 || height <= 0) return;
        var scale = Math.Min(width / 1600, height / 1120);
        canvas.Save();
        canvas.Translate((width - 1600 * scale) / 2, (height - 1120 * scale) / 2);
        canvas.Scale(scale);
        var frame = replay.Frames[index];
        Text(canvas, "Gojo · Negotiated live ports", 28, 39, 29, Ink, true);
        Text(canvas, "RECORDED EXPERIMENT · NO LIVE LEARNING", 30, 62, 12, Muted, true);
        Text(canvas, $"{replay.Title} · protocol {replay.Protocol} · SHA-256 {replay.Fingerprint}", 30, 81, 12, Muted, maxWidth: 1290);
        Text(canvas, $"STEP {frame.Step}", 1572, 39, 23, Ink, true, align: SKTextAlign.Right);
        Text(canvas, $"{index + 1} / {replay.Frames.Length}", 1572, 63, 14, Muted, align: SKTextAlign.Right);

        Resources(canvas, frame);
        Signal(canvas, replay, index);
        Network(canvas, replay, frame);
        Outcomes(canvas, replay, frame);
        Contacts(canvas, replay, frame);
        Payments(canvas, frame);

        Text(canvas, "A paid connection is a service promise, not proof that it helped. Unpublished ports are blank; a published zero is shown as 0.",
            30, 1081, 13, Ink);
        Text(canvas, replay.SourcePath, 30, 1104, 11, Muted, maxWidth: 1538);
        canvas.Restore();
    }

    private static void Resources(SKCanvas canvas, Frame frame)
    {
        Card(canvas, new SKRect(28, 97, 1572, 161));
        Text(canvas, "FINITE RESOURCES", 46, 121, 11, Muted, true);
        Text(canvas, "4,096 supplied credits · costs are accounting prices, not measured CPU time", 46, 144, 12, Muted);
        Resource(canvas, "RESERVOIR", frame.Reservoir, 708);
        Resource(canvas, "CONSUMER WALLETS", frame.Consumers.Sum(c => c.Wallet), 938);
        Resource(canvas, "PROVIDER RESERVES", frame.Producers.Sum(p => p.Reserve), 1200);
        Resource(canvas, "SPENT", frame.Spent, 1450);
        Circle(canvas, 1545, 132, 7, frame.Conserved ? Teal : Red);
        Text(canvas, frame.Conserved ? "Conserved" : "Unresolved", 1545, 153, 9,
            frame.Conserved ? Teal : Red, align: SKTextAlign.Center);
    }

    private static void Resource(SKCanvas canvas, string label, long value, float x)
    {
        Text(canvas, label, x, 120, 10, Muted, true, align: SKTextAlign.Center);
        Text(canvas, value.ToString("N0", CultureInfo.InvariantCulture), x, 145, 22, Ink, true, align: SKTextAlign.Center);
    }

    private static void Signal(SKCanvas canvas, Replay replay, int index)
    {
        Card(canvas, new SKRect(28, 177, 1572, 333));
        var current = replay.Frames[index];
        Text(canvas, "INPUT", 46, 203, 12, Muted, true);
        Text(canvas, current.Phase, 106, 204, 18, Ink, true);
        Text(canvas, current.Input is null ? "Missing observation" : $"Observed level {N(current.Input)}",
            1551, 203, 14, current.Input is null ? Amber : Ink, align: SKTextAlign.Right);
        var first = Math.Max(0, index - 79);
        var history = replay.Frames.Skip(first).Take(index - first + 1).ToArray();
        var values = history.Where(f => f.Input is not null).Select(f => (double)f.Input!.Value).ToArray();
        var minimum = values.Length == 0 ? 0 : values.Min();
        var maximum = values.Length == 0 ? 1 : values.Max();
        if (minimum == maximum) { minimum -= 1; maximum += 1; }
        const float left = 99, right = 1548, top = 221, bottom = 279;
        float X(int i) => left + (right - left) * (i - first) / Math.Max(1, index - first);
        float Y(double n) => bottom - (float)((n - minimum) / (maximum - minimum)) * (bottom - top);
        Line(canvas, left, top, right, top, Border, 1);
        Line(canvas, left, bottom, right, bottom, Border, 1);
        Text(canvas, maximum.ToString("0.#", CultureInfo.InvariantCulture), left - 13, top + 4, 11, Muted, align: SKTextAlign.Right);
        Text(canvas, minimum.ToString("0.#", CultureInfo.InvariantCulture), left - 13, bottom + 4, 11, Muted, align: SKTextAlign.Right);
        using var path = new SKPath();
        var connected = false;
        for (var i = first; i <= index; i++)
        {
            var sample = replay.Frames[i];
            if (sample.Input is null)
            {
                connected = false;
                Line(canvas, X(i), top, X(i), bottom, Amber.WithAlpha(125), 2, [2, 4]);
                continue;
            }
            if (!connected) path.MoveTo(X(i), Y(sample.Input.Value));
            else path.LineTo(X(i), Y(sample.Input.Value));
            connected = true;
            if (i == index) Circle(canvas, X(i), Y(sample.Input.Value), 4, Teal);
        }
        using (var paint = Stroke(Teal, 2.3f)) canvas.DrawPath(path, paint);
        var phaseStart = index;
        while (phaseStart > 0 && replay.Frames[phaseStart - 1].Phase == current.Phase) phaseStart--;
        var phase = replay.Frames.Skip(phaseStart).Take(index - phaseStart + 1).ToArray();
        var outlay = phase.SelectMany(f => f.Payments).Where(p => p.From.StartsWith("consumer-", StringComparison.Ordinal)).Sum(p => p.Amount);
        var contacts = phase.Sum(f => f.Contacts.Count(c => c.Paid));
        Text(canvas, "This phase so far", 46, 313, 12, Muted, true);
        Text(canvas, $"{outlay} consumer credits spent · {contacts} paid contacts", 170, 313, 13, current.Phase == "Flat" ? Amber : Ink);
        Text(canvas, "Past observations only · phase labels are observer annotations", 1551, 313, 12, Muted, align: SKTextAlign.Right);
    }

    private static readonly SKRect[] ProviderBoxes =
    [new(49, 411, 410, 535), new(49, 552, 410, 676), new(49, 693, 410, 872)];
    private static readonly SKRect[] ConsumerBoxes = [new(740, 417, 1004, 613), new(740, 661, 1004, 857)];
    private static SKPoint PortPoint(int offer) => new(410, offer switch { 0 => 497, 1 => 638, 2 => 783, _ => 835 });
    private static SKPoint SocketPoint(int consumer) => new(740, ConsumerBoxes[consumer].Top + 90);
    private static SKColor ConsumerTone(int id) => id == 0 ? Teal : Purple;

    private static void Network(SKCanvas canvas, Replay replay, Frame frame)
    {
        Card(canvas, new SKRect(28, 349, 1024, 897));
        Text(canvas, "CURRENT OFFERS → NEXT OBSERVATION", 48, 378, 13, Ink, true);
        Text(canvas, "Port data are published only for paid service. Two rich ports share one provider slot.", 48, 398, 12, Muted);
        foreach (var contact in frame.Contacts.Where(c => !c.Agreed))
            Edge(canvas, contact.Offer, contact.Consumer, Amber.WithAlpha(160), 1.6f, [5, 5]);
        foreach (var consumer in frame.Consumers.Where(c => c.Paid && c.Gate > 0 && c.SelectedOffer.HasValue))
            Edge(canvas, consumer.SelectedOffer!.Value, consumer.Id, ConsumerTone(consumer.Id), 3.5f);
        foreach (var producer in frame.Producers) ProviderCard(canvas, frame, producer);
        foreach (var consumer in frame.Consumers) ConsumerCard(canvas, replay, consumer);
        Text(canvas, "SOLID: paid live publication", 433, 855, 11, Teal, true);
        Text(canvas, "DASHED: attempted contact only", 433, 875, 11, Amber, true);
    }

    private static void ProviderCard(SKCanvas canvas, Frame frame, Producer producer)
    {
        var box = ProviderBoxes[producer.Id];
        Rounded(canvas, box, 10, Background.WithAlpha(130));
        Rounded(canvas, box, 10, Border, 1);
        var name = producer.Id switch { 0 => "P1 · Cheap interpreter", 1 => "P2 · Independent duplicate", _ => "P3 · Richer interpreter" };
        Text(canvas, name, box.Left + 15, box.Top + 25, 15, Ink, true, maxWidth: 330);
        Text(canvas, $"Reserve {producer.Reserve}/32", box.Left + 15, box.Top + 46, 11, Muted);
        Text(canvas, $"Slot {producer.Occupied}/1", box.Right - 15, box.Top + 46, 11, producer.Occupied > 0 ? Teal : Muted, align: SKTextAlign.Right);
        Bar(canvas, new SKRect(box.Left + 15, box.Top + 54, box.Right - 15, box.Top + 58), producer.Reserve, 32, Muted);
        foreach (var offer in frame.Offers.Where(o => o.Provider == producer.Id))
        {
            var point = PortPoint(offer.Id);
            var title = offer.Id switch { 0 or 1 => "Synthesized: direction(d)", 2 => "Intermediate: d", _ => "Extrapolated: 2d − a" };
            Text(canvas, title, box.Left + 15, point.Y - 6, 13, Ink, true, maxWidth: 228);
            Text(canvas, $"ask {offer.Ask} · operating cost {offer.Cost}", box.Left + 15, point.Y + 12, 11, Muted);
            var published = offer.LiveValue.HasValue;
            Text(canvas, published ? N(offer.LiveValue) : "—", box.Right - 22, point.Y + 2, 25, published ? Ink : Muted, true, align: SKTextAlign.Right);
            Text(canvas, published ? offer.Id < 2 ? "direction" : "change" : "unpublished", box.Right - 22, point.Y + 18, 10, Muted, align: SKTextAlign.Right);
            Circle(canvas, point.X, point.Y, 6, published ? Teal : SKColors.White, published ? Teal : Border);
        }
    }

    private static void ConsumerCard(SKCanvas canvas, Replay replay, Consumer consumer)
    {
        var box = ConsumerBoxes[consumer.Id];
        var tone = ConsumerTone(consumer.Id);
        var active = consumer.Paid && consumer.Gate > 0 && consumer.SelectedOffer.HasValue;
        Rounded(canvas, box, 11, SKColors.White);
        Rounded(canvas, box, 11, active ? tone : Border, 1.7f);
        Text(canvas, consumer.Id == 0 ? "C1 · Direction reader" : "C2 · Change reader", box.Left + 14, box.Top + 26, 15, tone, true);
        Text(canvas, consumer.Demand ? "Demand on" : "Demand withdrawn", box.Left + 14, box.Top + 46, 12, consumer.Demand ? Ink : Amber);
        Text(canvas, $"Wallet {consumer.Wallet}/16", box.Right - 14, box.Top + 46, 12, Muted, align: SKTextAlign.Right);
        Bar(canvas, new SKRect(box.Left + 14, box.Top + 55, box.Right - 14, box.Top + 59), consumer.Wallet, 16, tone);
        Text(canvas, active ? replay.OfferNames[consumer.SelectedOffer!.Value] : "No paid publication", box.Left + 14, box.Top + 84, 14, active ? Ink : Muted, true, 237);
        Text(canvas, active ? $"Received {N(consumer.Receipt)}" : consumer.Forecast.HasValue
            ? "Zero / no-service baseline" : "No current forecast", box.Left + 14, box.Top + 110, 19, active ? tone : Muted, true, 238);
        Text(canvas, "Supplied source-change correspondence", box.Left + 14, box.Top + 129, 10, Muted, maxWidth: 237);
        Text(canvas, $"Service {consumer.Price} · contact {consumer.ContactFee} credits", box.Left + 14, box.Top + 151, 12, Ink, maxWidth: 237);
        Text(canvas, $"Continuation bid {consumer.Bid}", box.Left + 14, box.Top + 174, 12, Muted);
        Circle(canvas, box.Left, box.Top + 90, 6, active ? tone : SKColors.White, active ? tone : Border);
    }

    private static void Edge(SKCanvas canvas, int offer, int consumer, SKColor color, float width, float[]? dash = null)
    {
        var start = PortPoint(offer);
        var end = SocketPoint(consumer);
        var offset = consumer == 0 ? -17 : 17;
        using var path = new SKPath();
        path.MoveTo(start);
        path.CubicTo(start.X + 128, start.Y + offset, end.X - 124, end.Y + offset, end.X - 8, end.Y);
        using (var paint = Stroke(color, width, dash)) canvas.DrawPath(path, paint);
        using var arrow = new SKPath();
        arrow.MoveTo(end.X - 8, end.Y);
        arrow.LineTo(end.X - 17, end.Y - 5);
        arrow.LineTo(end.X - 17, end.Y + 5);
        arrow.Close();
        using var fill = Fill(color);
        canvas.DrawPath(arrow, fill);
    }

    private static void Outcomes(SKCanvas canvas, Replay replay, Frame frame)
    {
        Card(canvas, new SKRect(1042, 349, 1572, 897));
        Text(canvas, "PREVIOUS PROMISE → OBSERVED NOW", 1062, 378, 13, Ink, true);
        Text(canvas, "A different time from the current service on the left.", 1062, 399, 12, Muted);
        foreach (var consumer in frame.Consumers)
        {
            var y = 415 + consumer.Id * 231f;
            var p = consumer.Previous;
            Rounded(canvas, new SKRect(1061, y, 1553, y + 215), 10, Background.WithAlpha(130));
            Text(canvas, consumer.Id == 0 ? "C1 · Errors in direction ticks" : "C2 · Errors in source-change ticks", 1078, y + 26, 14, ConsumerTone(consumer.Id), true);
            if (!p.Issued)
            {
                Text(canvas, "No earlier promise", 1078, y + 58, 16, Muted);
                continue;
            }
            var offer = p.Offer.HasValue ? replay.OfferNames[p.Offer.Value] : "No-service baseline";
            Text(canvas, $"Step {p.IssueStep} · {offer}", 1078, y + 49, 12, Muted, maxWidth: 456);
            var status = !p.Evaluated ? "Unresolved outcome" : !p.Paid ? "No paid service" : p.Gross > 0 ? "Paid service helped" : p.Gross < 0 ? "Paid service harmed" : "Paid, no added relief";
            var statusTone = !p.Evaluated ? Amber : !p.Paid ? Muted : p.Gross > 0 ? Teal : p.Gross < 0 ? Red : Amber;
            Text(canvas, status, 1078, y + 77, 18, statusTone, true);
            Text(canvas, $"Raw forecast {N(p.Forecast)}  →  actual source change {N(p.Actual)}", 1078, y + 100, 12, Ink, maxWidth: 456);
            if (!p.Evaluated)
            {
                Text(canvas, "No numeric usefulness is inferred from missing data.", 1078, y + 129, 12, Muted, maxWidth: 456);
                Text(canvas, $"Committed price {N(p.Price)} · contact {N(p.ContactFee)}", 1078, y + 156, 12, Muted);
                continue;
            }
            Text(canvas, $"Error without / with:  {N(p.BaselineDemand)} / {N(p.DeliveredDemand)}", 1078, y + 127, 14, Ink);
            Text(canvas, $"Relief {Signed(p.Relief)}", 1536, y + 127, 14, statusTone, true, align: SKTextAlign.Right);
            Line(canvas, 1078, y + 141, 1536, y + 141, Border, 1);
            Text(canvas, $"Estimated value {Signed(p.Gross)} credits", 1078, y + 163, 13, Ink);
            Text(canvas, $"Net {Signed(p.TotalNet)}", 1536, y + 163, 17, p.TotalNet < 0 ? Red : p.TotalNet > 0 ? Teal : Muted, true, align: SKTextAlign.Right);
            Text(canvas, $"Price {N(p.Price)} + contact {N(p.ContactFee)} · estimated value does not mint money", 1078, y + 187, 11, Muted, maxWidth: 458);
        }
        Text(canvas, "Do not add direction errors to source-change errors.", 1062, 884, 12, Muted);
    }

    private static void Contacts(SKCanvas canvas, Replay replay, Frame frame)
    {
        Card(canvas, new SKRect(28, 913, 1024, 1061));
        Text(canvas, "CONTACTS / NEGOTIATION NOW", 48, 941, 13, Ink, true);
        if (frame.Contacts.Length == 0)
        {
            Text(canvas, "No new contact attempts at this step.", 48, 974, 15, Muted);
            Text(canvas, "A continuing paid route can renew without a new contact.", 48, 1002, 12, Muted);
            return;
        }
        for (var index = 0; index < Math.Min(frame.Contacts.Length, 3); index++)
        {
            var contact = frame.Contacts[index];
            var y = 973 + index * 31f;
            var status = !contact.Paid ? "Contact not funded" : contact.Free == false ? "Provider capacity blocked" : contact.Fits == false ? "Quote above bid" : contact.Agreed ? "Agreement recorded" : "No agreement";
            Text(canvas, $"C{contact.Consumer + 1} → {replay.OfferNames[contact.Offer]}", 48, y, 13, ConsumerTone(contact.Consumer), true, 308);
            Text(canvas, $"bid {contact.Bid} / quote {contact.Ask} · fee {(contact.Paid ? contact.Fee : 0)}", 362, y, 12, Ink, maxWidth: 243);
            Text(canvas, status, 625, y, 13, contact.Agreed ? Teal : Amber, true, maxWidth: 370);
        }
    }

    private static void Payments(SKCanvas canvas, Frame frame)
    {
        Card(canvas, new SKRect(1042, 913, 1572, 1061));
        Text(canvas, "RESOURCE MOVEMENT NOW", 1062, 941, 13, Ink, true);
        var flows = frame.Payments.GroupBy(p => (p.From, p.To)).Select(g => new Payment
            { From = g.Key.From, To = g.Key.To, Amount = g.Sum(p => p.Amount) }).ToArray();
        if (flows.Length == 0) Text(canvas, "No recorded resource transfer.", 1062, 974, 14, Muted);
        for (var index = 0; index < Math.Min(flows.Length, 10); index++)
        {
            var x = 1062 + index / 5 * 253f;
            var y = 967 + index % 5 * 18f;
            Text(canvas, $"{Account(flows[index].From)} → {Account(flows[index].To)}", x, y, 11, Muted, maxWidth: 205);
            Text(canvas, N(flows[index].Amount), x + 231, y, 12, Ink, true, align: SKTextAlign.Right);
        }
    }

    private static string Account(string value) => value switch
    {
        "reservoir" => "Reservoir", "spent" => "Spent", "consumer-0" => "C1", "consumer-1" => "C2",
        "provider-0" => "P1", "provider-1" => "P2", "provider-2" => "P3", _ => value
    };
    private static string N(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "—";
    private static string Signed(long? value) => value is > 0 ? "+" + N(value) : N(value);
    private static void Bar(SKCanvas canvas, SKRect box, long value, long capacity, SKColor tone)
    {
        Rounded(canvas, box, 2, Border);
        var width = box.Width * Math.Clamp((float)value / capacity, 0, 1);
        if (width > 0) Rounded(canvas, new SKRect(box.Left, box.Top, box.Left + width, box.Bottom), 2, tone.WithAlpha(175));
    }
    private static void Card(SKCanvas canvas, SKRect box)
    {
        Rounded(canvas, box, 13, SKColors.White);
        Rounded(canvas, box, 13, Border, 1);
    }
    private static void Text(SKCanvas canvas, string value, float x, float y, float size, SKColor color,
        bool bold = false, float maxWidth = float.MaxValue, SKTextAlign align = SKTextAlign.Left)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = color, TextSize = size,
            Typeface = bold ? Bold : Regular, TextAlign = align };
        if (paint.MeasureText(value) > maxWidth)
        {
            while (value.Length > 1 && paint.MeasureText(value + "…") > maxWidth) value = value[..^1];
            value += "…";
        }
        canvas.DrawText(value, x, y, paint);
    }
    private static SKPaint Fill(SKColor color) => new() { IsAntialias = true, Color = color, Style = SKPaintStyle.Fill };
    private static SKPaint Stroke(SKColor color, float width, float[]? dash = null) => new()
    {
        IsAntialias = true, Color = color, StrokeWidth = width, Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round, PathEffect = dash is null ? null : SKPathEffect.CreateDash(dash, 0)
    };
    private static void Rounded(SKCanvas canvas, SKRect box, float radius, SKColor color, float stroke = 0)
    {
        using var paint = stroke > 0 ? Stroke(color, stroke) : Fill(color);
        canvas.DrawRoundRect(box, radius, radius, paint);
    }
    private static void Circle(SKCanvas canvas, float x, float y, float radius, SKColor color, SKColor? outline = null)
    {
        using var paint = Fill(color);
        canvas.DrawCircle(x, y, radius, paint);
        if (outline.HasValue) { using var border = Stroke(outline.Value, 1.2f); canvas.DrawCircle(x, y, radius, border); }
    }
    private static void Line(SKCanvas canvas, float x1, float y1, float x2, float y2, SKColor color, float width, float[]? dash = null)
    {
        using var paint = Stroke(color, width, dash);
        canvas.DrawLine(x1, y1, x2, y2, paint);
    }
}
