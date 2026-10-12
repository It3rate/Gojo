using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Gojo.Core;
using SkiaSharp;
using System.Diagnostics;
using System.Text.Json;
using static Phototaxis.Native;

namespace Phototaxis.Browser;

internal sealed class BathCanvas : Control
{
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly List<(Rect Bounds, Action Action)> hits = [];
    internal Bath World { get; private set; } = new(pulsedLights:true, rocks:true, cognitiveEcology:true,
        ecology:EcologySettings.Roving) { Learning=false };
    internal bool JevReady { get; private set; }
    internal bool JevBusy { get; private set; }
    internal int JevRequests { get; private set; }
    internal double JevDollars { get; private set; }
    internal string JevLabel => JevBusy ? "JEV THINKING" : World.InquiriesEnabled ? $"JEV ON · {JevRequests}/20" : JevReady ? "JEV READY · CLICK TO ENABLE" : "JEV OFF · LOCAL KEY NEEDED";
    internal Dictionary<int, List<(double X, double Y)>> Trails { get; } = [];
    private long trailTick = -1;
    internal int Selected { get; private set; }
    internal bool Paused { get; private set; }
    internal bool Vision { get; private set; } = true;
    internal bool Inspector { get; private set; }
    internal bool CircuitView { get; private set; }
    internal int Rate { get; private set; } = 50;
    internal string RateLabel => Rate == 0 ? "Max" : $"{Rate}×";
    internal string ModeLabel => World.CognitiveEcology ? World.BreedingSelection ? "Breeding market" : "Fixed inheritance" :
        World.AuthoredControllers ? "Designed" : World.Learning ? "Learning on" : "Frozen control";
    internal string EcologyLabel => World.Ecology == EcologySettings.Roving ? "Roving bath" :
        World.Ecology == EcologySettings.Focused ? "Focused bath" : "Original bath";
    internal double ActualRate { get; private set; }
    internal double ComputeLoad { get; private set; }
    internal int TrainingMinutes { get; private set; } = 5;
    internal bool Training => trainingTarget is not null;
    internal string Notice { get; private set; } = "Click a swimmer to inspect its native connections.";
    private double previousTime;
    private double carry;
    private long publishedTick = -1;
    private long? trainingTarget;
    private long rateWindowTick;
    private double rateWindowTime;
    private double busySeconds;
    private double lastDrawTime;
    private double lastPublishTime = -1;
    private Rect worldBounds;

    public BathCanvas()
    {
        Focusable = true;
        ClipToBounds = true;
        _ = RefreshJev();
        timer.Tick += (_, _) =>
        {
            var now = clock.Elapsed.TotalSeconds;
            var elapsed = Math.Min(1, now - previousTime);
            previousTime = now;
            if (Program.TakeStopRequest() && Training) TogglePause();
            if (!Paused && !JevBusy)
            {
                carry = Math.Min(2000, carry + elapsed * Rate * Bath.TicksPerSecond);
                var budget = Stopwatch.StartNew();
                var maximum = Training || Rate == 0;
                var sliceMs = maximum || Rate >= 50 ? 32 : 12;
                while ((maximum || carry >= 1) && budget.Elapsed.TotalMilliseconds < sliceMs)
                {
                    World.Step();
                    Selected = Math.Clamp(Selected, 0, Math.Max(0, World.Creatures.Length - 1));
                    if (!maximum) carry--;
                    if (World.Creatures.Length == 0)
                    {
                        trainingTarget = null; Paused = true; publishedTick = -1;
                        Notice = "Population extinct · reset to start a new trial.";
                        InvalidateVisual(); break;
                    }
                    if (trainingTarget is not null && World.Tick >= trainingTarget)
                    {
                        trainingTarget = null; Paused = true; publishedTick = -1;
                        World.CheckConservation();
                        Notice = $"Training complete at {TimeSpan.FromSeconds(World.Seconds):hh\\:mm\\:ss} · population retained. Resume continues.";
                        InvalidateVisual();
                        break;
                    }
                }
                busySeconds += budget.Elapsed.TotalSeconds;
                PumpJev();
            }
            var end = clock.Elapsed.TotalSeconds;
            if (end - rateWindowTime >= 1)
            {
                ActualRate = (World.Tick - rateWindowTick) / (double)Bath.TicksPerSecond / (end - rateWindowTime);
                ComputeLoad = Math.Min(1, busySeconds / (end - rateWindowTime));
                rateWindowTick = World.Tick; rateWindowTime = end; busySeconds = 0;
            }
            if (publishedTick == -1 || end - lastPublishTime >= 1)
            {
                publishedTick = World.Tick; lastPublishTime = end;
                var selected = World.Creatures.ElementAtOrDefault(Selected);
                var sizes = World.Creatures.Select(c => c.ComponentCount).ToArray();
                Program.SetStatus(JsonSerializer.Serialize(new StatusFrame(World.Seed, World.Tick, World.Seconds,
                    World.Creatures.Length, World.TotalFood, World.Births, World.SpentAttention.Value, World.Learning, Paused,
                    World.RocksEnabled, World.ExposureRisk, Rate, Inspector, ActualRate, ComputeLoad, Training,
                    trainingTarget is null ? 0 : (trainingTarget.Value - World.Tick) / (double)Bath.TicksPerSecond,
                    World.MeanEnergy, Selected, selected?.LeftMuscle.Force.Value ?? 0,
                    selected?.RightMuscle.Force.Value ?? 0, World.Components, World.Rewrites,
                    World.FoodAvailability, World.ComputeCalories, World.Resources.Measured,
                    selected?.ComponentCount, sizes.Length == 0 ? null : sizes.Min(),
                    sizes.Length == 0 ? null : sizes.Max(), sizes.Length == 0 ? null : sizes.Average(),
                    World.Operations, World.MeanSpeed, World.StationaryShare, World.HungryTowardLight, World.FullAwayFromLight,
                    World.PulsedLights, World.ActiveLights.Length, World.AuthoredControllers ? DesignedControllers.Champion.Name : null,
                    World.CognitiveEcology, World.BreedingSelection, World.Injuries, World.InjuryCalories,
                    selected?.Cognition is { } selectedBrain ? Math.Sqrt(Math.Max(0, Held(selectedBrain.SpeedSquared))) : null,
                    selected?.Cognition is { } predictionBrain ? Held(predictionBrain.Error) : null,
                    selected?.Cognition is { } pressureBrain ? pressureBrain.Pressure.Tension : null,
                    selected?.Cognition is { } appetiteBrain ? Held(appetiteBrain.Pilot.Appetite) : null,
                    World.InquiriesEnabled, JevReady, JevBusy, JevRequests, JevDollars,
                    World.Ecology == EcologySettings.Focused, World.Ecology == EcologySettings.Roving,
                    World.Ecology.LightRadius, World.Ecology.LightOnScale,
                    World.Ecology.PatchHarvestPerPulse, World.Ecology.LightDriftSpeed, World.LocalFoodShortfall.Value,
                    World.HungryGradientAlignment, World.HungryGradientSamples,
                    selected?.Cognition is { } injuryBrain ? (long)injuryBrain.Observations.Injury.Value : null,
                    selected?.Cognition is { } painBrain ? (long)painBrain.PainMemory.Value : null,
                    selected?.Cognition is { } escapeBrain ? Math.Max(0,
                        Math.Max(Held(escapeBrain.Pilot.RecoveryUntil), escapeBrain.Pilot.Progress is { } progress ? Held(progress.Until) : 0)
                            - (long)escapeBrain.Observations.Time.Value) / 20.0 : null,
                    World.ArenaWidth, World.ArenaHeight,
                    selected?.Cognition?.Pilot.Progress?.Episodes.Value,
                    selected?.Cognition is { } mateBrain ? ReadActive(mateBrain.Pilot.CourtshipActive) : null,
                    selected?.Cognition?.Observations.LeftMate.Value, selected?.Cognition?.Observations.RightMate.Value),
                    StatusJson.Default.StatusFrame));
            }
            // Training retains the same world while suppressing Skia work.
            // Accelerated viewing uses five frames/second, ordinary viewing 30.
            if (!Training && end - lastDrawTime >= (Rate == 1 ? 1.0 / 30 : .2))
            { CaptureTrails(); lastDrawTime = end; InvalidateVisual(); }
            timer.Interval = TimeSpan.FromMilliseconds(Training || Rate == 0 || Rate >= 50 ? 1 : 16);
        };
    }

    private async Task RefreshJev()
    {
        try
        {
            using var doc = JsonDocument.Parse(await Program.JevStatus());
            JevReady = doc.RootElement.GetProperty("available").GetBoolean();
            if (doc.RootElement.TryGetProperty("requests", out var requests)) JevRequests = requests.GetInt32();
            if (doc.RootElement.TryGetProperty("dollars", out var dollars)) JevDollars = dollars.GetDouble();
        }
        catch { JevReady = false; }
        InvalidateVisual();
    }
    private async void PumpJev()
    {
        if (!JevReady || JevBusy || !World.InquiriesEnabled) return;
        var c = World.Creatures.FirstOrDefault(c => c.Cognition?.Inquiry.Available == true);
        if (c?.Cognition is not { } brain) return;
        var q = brain.Inquiry.Pending!; JevBusy = true;
        try
        {
            var preparation = World.Resources.Begin(); var request = NativeInquiry.Request(q, c);
            c.Computation.Runtime.Add((ulong)World.Resources.BodyWorkSince(preparation));
            var response = await Program.AskJev(request);
            var admissionStart = World.Resources.Begin(); brain.Inquiry.Admit(q, response);
            c.Computation.Runtime.Add((ulong)World.Resources.BodyWorkSince(admissionStart));
        }
        catch { brain.Inquiry.Fail(q, "Local bridge request failed or reached its trial limit"); }
        finally { JevBusy = false; await RefreshJev(); publishedTick = -1; }
    }
    internal void ToggleJev()
    {
        if (!JevReady) { Notice = "Start the local preview with -KeyFile to enable the bounded Jev trial."; return; }
        Reset(inquiries:!World.InquiriesEnabled, cognitive:true);
        Notice = World.InquiriesEnabled ? "Fresh mixed founders · Jev can suggest two-second trials · 20 requests / $0.02 cap." : "Fresh mixed founders · native decisions and breeding market.";
    }
    private void CaptureTrails()
    {
        if (trailTick == World.Tick) return; trailTick = World.Tick;
        var live = World.Creatures.Select(c => c.Slot).ToHashSet();
        foreach (var id in Trails.Keys.Where(id => !live.Contains(id)).ToArray()) Trails.Remove(id);
        foreach (var c in World.Creatures)
        {
            if (!Trails.TryGetValue(c.Slot, out var trail)) Trails.Add(c.Slot, trail = []);
            trail.Add((c.Px, c.Py)); if (trail.Count > 24) trail.RemoveAt(0);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); previousTime = clock.Elapsed.TotalSeconds; timer.Start(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { timer.Stop(); base.OnDetachedFromVisualTree(e); }
    public override void Render(DrawingContext context) { base.Render(context); context.Custom(new BathDraw(new Rect(Bounds.Size), this)); }
    internal void BeginLayout() => hits.Clear();
    internal void Hit(SKRect rect, Action action) => hits.Add((new Rect(rect.Left, rect.Top, rect.Width, rect.Height), action));
    internal void SetWorldBounds(SKRect rect) => worldBounds = new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
    internal void TogglePause() { trainingTarget = null; Paused = !Paused; carry = 0; publishedTick = -1; Notice = Paused ? "Paused · N advances one physical tick." : "Running · each eye and muscle keeps its own clock."; }
    internal void ChangeRate() { Rate = Rate == 50 ? 0 : Rate == 0 ? 1 : Rate == 1 ? 10 : 50; carry = 0; publishedTick = -1; }
    internal void ChangeTrainingLength() { TrainingMinutes = TrainingMinutes == 5 ? 30 : TrainingMinutes == 30 ? 1 : 5; }
    internal void StartTraining()
    {
        trainingTarget = checked(World.Tick + TrainingMinutes * 60L * Bath.TicksPerSecond);
        Paused = false; carry = 0; publishedTick = -1;
        Notice = "Training at maximum speed · drawing suspended · keep this tab open.";
    }
    internal void ChangePopulation() => Reset(population: World.InitialPopulation == 20 ? 5 : World.InitialPopulation == 5 ? 1 : 20);
    internal void ToggleVision() => Vision = !Vision;
    internal void ToggleInspector() { Inspector = !Inspector; publishedTick = -1; }
    internal void ShowCircuit(bool value) { CircuitView = value; publishedTick = -1; }
    internal void NextCreature()
    {
        if (World.Creatures.Length == 0) return;
        Selected = (Selected + 1) % World.Creatures.Length; publishedTick = -1;
        Notice = $"Creature {World.Creatures[Selected].Slot + 1:00} selected · circuit reads do not train it.";
    }
    internal void ToggleRocks() { World.RocksEnabled = !World.RocksEnabled; publishedTick = -1; Notice = "Rocks change the bath; they do not add steering rules."; }
    internal void ToggleRisk() { World.ExposureRisk = !World.ExposureRisk; publishedTick = -1; Notice = World.ExposureRisk ? "Predation risk is on. Stopping and rock contact increase danger." : "Predation risk is off. Energy use and food remain active."; }
    internal void ToggleLights() { World.PulsedLights = !World.PulsedLights; publishedTick = -1; Notice = World.PulsedLights ? "Timed patches switch independently. Light still feeds through the same physical pool." : "One slowly moving light patch."; }
    internal void ToggleEcology()
    {
        World.ApplyEcology(World.Ecology == EcologySettings.Original ? EcologySettings.Roving :
            World.Ecology == EcologySettings.Roving ? EcologySettings.Focused : EcologySettings.Original);
        publishedTick = -1;
        Notice = World.Ecology == EcologySettings.Roving ?
            "Roving bath · lights drift at 1.5 px/s · 36 shared calories per patch/pulse · current creatures and memories retained." :
            World.Ecology == EcologySettings.Focused ?
            "Focused bath · smaller patches, shorter lights, shared local food · current creatures and memories retained." :
            "Original bath restored · current creatures and memories retained.";
    }
    internal void ToggleLearning()
    {
        if (World.CognitiveEcology && World.BreedingSelection) Reset(cognitive:true, selection:false);
        else if (World.CognitiveEcology) Reset(learning:false, authored:true, cognitive:false);
        else if (World.AuthoredControllers) Reset(learning:true, authored:false, cognitive:false);
        else if (World.Learning) Reset(learning:false, authored:false);
        else Reset(learning:false, authored:false, cognitive:true, selection:true);
        Notice = World.CognitiveEcology ? "Fresh mixed founders · native module market during breeding · own motion and prediction." :
            World.AuthoredControllers ? $"Authored {DesignedControllers.Champion.Name} · fixed Gojo circuits, independent child memory." :
            World.Learning ? "Fresh trial · tension can open the circuit market." : "Fresh neutral control · topology search disabled.";
    }
    internal void Reset(int? seed = null, bool? learning = null, int? population = null, bool? authored = null,
        bool? cognitive = null, bool? selection = null, bool? inquiries = null, double? arenaScale = null)
    {
        var designed = authored ?? World.AuthoredControllers;
        World = new Bath(seed ?? World.Seed, population ?? World.InitialPopulation, rocks: World.RocksEnabled,
            pulsedLights:World.PulsedLights, configureController:designed ? DesignedControllers.Builder(DesignedControllers.Champion) : null,
            cognitiveEcology:cognitive ?? World.CognitiveEcology, inquiries:inquiries ?? World.InquiriesEnabled,
            breedingSelection:selection ?? World.BreedingSelection, eyeLevels:World.EyeLevels,
            ecology:World.Ecology, binocular:World.BinocularVision, arenaScale:arenaScale ?? World.ArenaScale,
            pilotFeatures:World.PilotFeatures)
            { Learning = learning ?? World.Learning, ExposureRisk = World.ExposureRisk };
        Selected = Math.Clamp(Selected, 0, Math.Max(0, World.Creatures.Length - 1));
        trainingTarget = null; carry = 0; publishedTick = -1;
        Trails.Clear(); trailTick = -1;
        rateWindowTick = 0; rateWindowTime = clock.Elapsed.TotalSeconds; busySeconds = 0; ActualRate = 0; ComputeLoad = 0;
        Notice = "Fresh bath · identical seed reproduces the starting structures.";
    }
    internal void ChangeArena()
    {
        Reset(arenaScale:World.ArenaScale < 1.1 ? 1.25 : World.ArenaScale < 1.4 ? 1.5 : 1);
        Notice = $"Fresh {World.ArenaWidth:F0} × {World.ArenaHeight:F0} arena · same population, light count and food budget.";
    }
    private static bool ReadActive(IElement expression) => TryValue(ReadMany(expression)[0], out var active) && active == 15;
    internal void Download()
    {
        if (World.Creatures.Length == 0) { Notice = "No living creature to export. Reset starts a new trial."; return; }
        var bytes = World.Creatures[Selected].ExportNativeGraph();
        Program.DownloadGraph($"gojo-creature-{Selected + 1}-tick-{World.Tick}.gojo", Convert.ToBase64String(bytes));
        Notice = $"Native graph ready · {bytes.Length / 1024:N0} KB. Use Save native graph.";
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e); Focus();
        var p = e.GetPosition(this);
        foreach (var hit in hits) if (hit.Bounds.Contains(p)) { hit.Action(); InvalidateVisual(); e.Handled = true; return; }
        if (!worldBounds.Contains(p) || World.Creatures.Length == 0) return;
        var x = (p.X - worldBounds.Left) / worldBounds.Width * World.ArenaWidth;
        var y = (p.Y - worldBounds.Top) / worldBounds.Height * World.ArenaHeight;
        Selected = Array.IndexOf(World.Creatures, World.Creatures.OrderBy(c => Bath.Distance(x, y, c.Px, c.Py)).First());
        publishedTick = -1;
        Notice = $"Creature {World.Creatures[Selected].Slot + 1:00} selected · inspect its circuit and tension.";
        InvalidateVisual();
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Key.Space: TogglePause(); break;
            case Key.N: trainingTarget = null; Paused = true; World.Step(); Selected = Math.Clamp(Selected, 0, Math.Max(0, World.Creatures.Length - 1)); publishedTick = -1; break;
            case Key.V: ToggleVision(); break;
            case Key.R: Reset(); break;
            default: return;
        }
        e.Handled = true; InvalidateVisual();
    }
}

internal sealed class BathDraw(Rect bounds, BathCanvas view) : ICustomDrawOperation
{
    public Rect Bounds { get; } = bounds;
    public void Dispose() { }
    public bool HitTest(Point p) => Bounds.Contains(p);
    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
    private static readonly SKColor Ink = new(224, 238, 231);
    private static readonly SKColor Muted = new(136, 164, 167);
    private static readonly SKColor Green = new(153, 226, 185);
    private static readonly SKColor Blue = new(127, 193, 218);
    private static readonly SKColor Gold = new(239, 198, 123);
    private static readonly SKColor Panel = new(17, 34, 42);
    private static readonly SKColor Edge = new(43, 65, 71);
    private static readonly SKTypeface Typeface = LoadTypeface();
    private static SKTypeface LoadTypeface()
    {
        using var stream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://Avalonia.Fonts.Inter/Assets/Inter-Regular.ttf"));
        return SKTypeface.FromStream(stream);
    }
    private readonly List<SKPaint> paints = [];
    private SKCanvas canvas = null!;

    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature feature) return;
        using var lease = feature.Lease();
        canvas = lease.SkCanvas;
        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height));
        try { Draw((float)Bounds.Width, (float)Bounds.Height); }
        finally { canvas.Restore(); foreach (var p in paints) p.Dispose(); paints.Clear(); }
    }
    private SKPaint Paint(SKColor color, bool stroke = false, float width = 1)
    { var p = new SKPaint { Color = color, IsAntialias = true, Style = stroke ? SKPaintStyle.Stroke : SKPaintStyle.Fill, StrokeWidth = width }; paints.Add(p); return p; }
    private void Text(string value, float x, float y, float size = 13, SKColor? color = null)
    { using var font = new SKFont(Typeface, size); canvas.DrawText(value, x, y, SKTextAlign.Left, font, Paint(color ?? Ink)); }
    private void Box(SKRect rect, SKColor color, float radius = 9) => canvas.DrawRoundRect(rect, radius, radius, Paint(color));
    private void Line(float x1, float y1, float x2, float y2, SKColor color, float width = 1) => canvas.DrawLine(x1, y1, x2, y2, Paint(color, true, width));
    private void Button(string label, float x, float y, float width, Action action, bool on = false)
    {
        var rect = new SKRect(x, y, x + width, y + 32);
        Box(rect, on ? new SKColor(37, 74, 65) : Panel, 7);
        canvas.DrawRoundRect(rect, 7, 7, Paint(on ? new SKColor(87, 132, 113) : Edge, true));
        Text(label, x + 12, y + 21, 12, on ? Green : Ink);
        view.Hit(rect, action);
    }
    private void Metric(string label, string value, float x, float y, float width, SKColor color)
    { Box(new SKRect(x, y, x + width, y + 56), Panel); Text(label, x + 12, y + 18, 10, Muted); Text(value, x + 12, y + 43, 21, color); }

    private void Draw(float w, float h)
    {
        var world = view.World;
        view.BeginLayout();
        canvas.Clear(new SKColor(9, 22, 29));
        Text("GOJO   /   EXPERIMENT 02", 24, 25, 10, Green);
        Text("Light / appetite", 24, 56, 28);
        if (w < 900) { DrawCompact(w, h); return; }
        if (w > 1240) Text(world.CognitiveEcology ? "Own motion · prediction tension · breeding market" : world.AuthoredControllers ? $"Authored {DesignedControllers.Champion.Name} · decisions run in Gojo." : "A native learning bath. Circuits adapt through tension.", 266, 53, 12, Muted);
        Box(new SKRect(w - 300, 24, w - 24, 57), Panel, 16);
        Text(view.JevLabel, w - 285, 45, 10, Blue);
        view.Hit(new SKRect(w - 300, 24, w - 24, 57), view.ToggleJev);
        Button(view.Paused ? "Resume" : "Pause", 24, 82, 81, view.TogglePause);
        Button(view.RateLabel + " speed", 114, 82, 87, view.ChangeRate);
        Button("Reset", 210, 82, 67, () => view.Reset());
        Button($"Seed {world.Seed}", 286, 82, 85, () => view.Reset(world.Seed + 1));
        Button("Rocks " + (world.RocksEnabled ? "on" : "off"), 380, 82, 90, view.ToggleRocks, world.RocksEnabled);
        Button("Risk " + (world.ExposureRisk ? "on" : "off"), 479, 82, 84, view.ToggleRisk, world.ExposureRisk);
        Button(view.ModeLabel, 572, 82, 123, view.ToggleLearning, world.CognitiveEcology || world.Learning || world.AuthoredControllers);
        Button("Eye view", 704, 82, 86, view.ToggleVision, view.Vision);
        Button(world.PulsedLights ? "Timed lights" : "Moving light", 799, 82, 101, view.ToggleLights, world.PulsedLights);
        if (w >= 1050) Button(view.EcologyLabel, 909, 82, 116,
            view.ToggleEcology, world.Ecology != EcologySettings.Original);
        var available = Math.Max(400, w - 372);
        var metricWidth = (available - 24) / 4;
        Metric("LIGHT HARVESTED", $"{world.TotalFood:N0} cal", 24, 130, metricWidth, Green);
        Metric("MEAN ENERGY", $"{world.MeanEnergy:P0}", 32 + metricWidth, 130, metricWidth, Gold);
        Metric("BIRTHS / DEATHS", $"{world.Births} / {world.PredationDeaths + world.StarvationDeaths}", 40 + metricWidth * 2, 130, metricWidth, Blue);
        Metric(world.CognitiveEcology ? "RECOVERABLE INJURIES" : "CIRCUIT CHANGES", $"{(world.CognitiveEcology ? world.Injuries : world.Rewrites):N0}", 48 + metricWidth * 3, 130, metricWidth, Ink);
        DrawRunControls(24, 199, available);
        var area = new SKRect(24, 242, 24 + available, h - 144);
        DrawWorld(area);
        DrawHistory(new SKRect(24, h - 127, 24 + available, h - 38));
        DrawInspector(w - 324, 130, 300, h - 168);
        Text(view.Notice, 24, h - 15, 11, Muted);
    }

    private void DrawCompact(float w, float h)
    {
        var world = view.World;
        Text("SKIA + WASM · " + view.JevLabel, 24, 71, 9, Blue);
        view.Hit(new SKRect(24, 60, 400, 80), view.ToggleJev);
        Button(view.Paused ? "Resume" : "Pause", 24, 85, 81, view.TogglePause);
        Button(view.RateLabel + " speed", 114, 85, 87, view.ChangeRate);
        Button("Reset", 210, 85, 67, () => view.Reset());
        Button($"Seed {world.Seed}", 286, 85, 85, () => view.Reset(world.Seed + 1));
        Button(view.Inspector ? "World" : "Inspect", 380, 85, 86, view.ToggleInspector, view.Inspector);
        if (w >= 615) Button(view.EcologyLabel, 475, 85, 116,
            view.ToggleEcology, world.Ecology != EcologySettings.Original);
        Button("Rocks " + (world.RocksEnabled ? "on" : "off"), 24, 126, 90, view.ToggleRocks, world.RocksEnabled);
        Button("Risk " + (world.ExposureRisk ? "on" : "off"), 123, 126, 84, view.ToggleRisk, world.ExposureRisk);
        Button(view.ModeLabel, 216, 126, 123, view.ToggleLearning, world.CognitiveEcology || world.Learning || world.AuthoredControllers);
        Button("Eye view", 348, 126, 86, view.ToggleVision, view.Vision);
        Button(world.PulsedLights ? "Timed lights" : "Moving light", 443, 126, 101, view.ToggleLights, world.PulsedLights);
        var width = w - 48;
        var card = (width - 24) / 4;
        Metric("HARVEST · CAL", $"{world.TotalFood:N0}", 24, 172, card, Green);
        Metric("ENERGY", $"{world.MeanEnergy:P0}", 32 + card, 172, card, Gold);
        Metric("BIRTHS", $"{world.Births}", 40 + card * 2, 172, card, Blue);
        Metric(world.CognitiveEcology ? "INJURIES" : "CHANGES", $"{(world.CognitiveEcology ? world.Injuries : world.Rewrites):N0}", 48 + card * 3, 172, card, Ink);
        DrawRunControls(24, 237, width);
        var area = new SKRect(24, 280, w - 24, h - 64);
        if (view.Inspector) DrawCompactInspector(area); else DrawWorld(area);
        var c = world.Creatures.ElementAtOrDefault(view.Selected);
        if (c is null) { Text("Population extinct · Reset starts a new trial.", 24, h - 42, 12, Gold); return; }
        Text($"Creature {c.Slot + 1:00} · gen {c.Generation.Value} · energy {c.Fullness:P0} · pushes {c.LeftMuscle.Force.Value}/{c.RightMuscle.Force.Value}", 24, h - 42, 11, Ink);
        var sizes = world.Creatures.Select(body => body.ComponentCount).ToArray();
        Text($"{world.Creatures.Length} alive · circuits {sizes.Min()}–{sizes.Max()} terms, {sizes.Average():F1} average · {world.FoodAvailability:P0} supply", 24, h - 21, 10, Muted);
    }

    private void DrawRunControls(float x, float y, float width)
    {
        Text($"Actual {view.ActualRate:F1}×", x, y + 21, 13, Green);
        if (width > 750) Text($"· supply {view.World.FoodAvailability:P0} · work {view.ComputeLoad:P0}", x + 94, y + 21, 11, Muted);
        else if (width >= 550) Text($"supply {view.World.FoodAvailability:P0}", x + 110, y + 21, 11, Muted);
        if (width >= 450) Button($"Arena {view.World.ArenaScale:0.##}×", x + width - 349, y, 91, view.ChangeArena);
        Button($"Start {view.World.InitialPopulation}", x + width - 249, y, 81, view.ChangePopulation);
        Button($"{view.TrainingMinutes} min", x + width - 159, y, 69, view.ChangeTrainingLength);
        Button("Train", x + width - 81, y, 81, view.StartTraining);
    }

    private void DrawCompactInspector(SKRect area)
    {
        DrawInspectorPanel(area, true);
    }

    private void DrawInspectorPanel(SKRect area, bool compact)
    {
        Box(area, Panel);
        if (view.World.Creatures.Length == 0) { Text("No living circuit · Reset to start again.", area.Left + 18, area.Top + 30, 14, Gold); return; }
        Button("Circuit", area.Left + 12, area.Top + 8, 80, () => view.ShowCircuit(true), view.CircuitView);
        Button("Senses", area.Left + 100, area.Top + 8, 80, () => view.ShowCircuit(false), !view.CircuitView);
        Button("Next", area.Right - 86, area.Top + 8, 74, view.NextCreature);
        var content = new SKRect(area.Left, area.Top + 46, area.Right, area.Bottom);
        if (!view.CircuitView && view.World.Creatures[view.Selected].Cognition is not null) DrawCognitiveInspector(content);
        else if (view.CircuitView) DrawCircuitInspector(content);
        else if (compact) DrawCompactSensesInspector(content);
        else DrawSensesInspector(content.Left, content.Top, content.Width, content.Height);
    }

    private void DrawCompactSensesInspector(SKRect area)
    {
        Box(area, Panel);
        if (view.World.Creatures.Length == 0) { Text("No living circuit · Reset to start again.", area.Left + 18, area.Top + 30, 14, Gold); return; }
        var s = Math.Min(1, area.Height / 400);
        var x = area.Left + (area.Width - 300 * s) / 2;
        canvas.Save(); canvas.Translate(x, area.Top); canvas.Scale(s);
        var c = view.World.Creatures[view.Selected];
        Text($"Creature {c.Slot + 1:00} / local structure", 18, 27, 18);
        Bar("ENERGY", c.Fullness, $"{c.Energy.Value} / {Creature.Capacity}", 46, Gold);
        DrawEyeStrip(c.LeftEye, "LEFT EYE", 91, Blue);
        DrawEyeStrip(c.RightEye, "RIGHT EYE", 145, Gold);
        Bar("LEFT PUSH", c.LeftMuscle.Force.Value / 15.0, $"{c.LeftMuscle.Force.Value} / 15", 206, Green);
        Bar("RIGHT PUSH", c.RightMuscle.Force.Value / 15.0, $"{c.RightMuscle.Force.Value} / 15", 239, Blue);
        Text("CIRCUIT SIZE : UTILITY QUOTES", 18, 284, 10, Green);
        DrawBids(c.LeftMuscle, 295, "L"); DrawBids(c.RightMuscle, 324, "R");
        Text($"{c.ComponentCount} terms · tension {c.Pressure.Tension} · {c.Rewrites} changes", 18, 364, 11, Muted);
        var button = new SKRect(18, 375, 273, 398);
        Box(button, new SKColor(32, 59, 57), 6); Text("Export selected native graph", 33, 391, 11, Green);
        view.Hit(new SKRect(x + button.Left * s, area.Top + button.Top * s, x + button.Right * s, area.Top + button.Bottom * s), view.Download);
        canvas.Restore();
    }

    private void DrawWorld(SKRect area)
    {
        var world = view.World;
        Box(area, new SKColor(12, 28, 34));
        var scale = Math.Min(area.Width / (float)world.ArenaWidth, area.Height / (float)world.ArenaHeight);
        var rect = new SKRect(area.Left + (area.Width - (float)world.ArenaWidth * scale) / 2, area.Top + (area.Height - (float)world.ArenaHeight * scale) / 2,
            area.Left + (area.Width + (float)world.ArenaWidth * scale) / 2, area.Top + (area.Height + (float)world.ArenaHeight * scale) / 2);
        view.SetWorldBounds(rect);
        canvas.Save(); canvas.ClipRect(rect); canvas.Translate(rect.Left, rect.Top); canvas.Scale(scale);
        var illumination = (float)(world.Daylight * world.Clouds);
        var baseColor = new SKColor(20, 45, 49);
        var centerColor = new SKColor((byte)(40 + 137 * illumination), (byte)(66 + 128 * illumination), (byte)(62 + 91 * illumination));
        canvas.DrawRect(0, 0, (float)world.ArenaWidth, (float)world.ArenaHeight, Paint(baseColor));
        foreach (var source in world.ActiveLights)
        {
            using var shader = SKShader.CreateRadialGradient(new SKPoint((float)source.X, (float)source.Y), (float)source.Radius * 3,
                [centerColor, new SKColor(32, 72, 67), baseColor], [0, .56f, 1], SKShaderTileMode.Clamp);
            var bathPaint = Paint(baseColor); bathPaint.Shader = shader; bathPaint.BlendMode = SKBlendMode.Lighten;
            canvas.DrawRect(0, 0, (float)world.ArenaWidth, (float)world.ArenaHeight, bathPaint);
            if (world.PulsedLights) Text($"{source.RemainingSeconds:F0}s", (float)source.X - 9, (float)source.Y - 9, 10, new SKColor(194, 219, 202));
            if (world.PulsedLights && world.Ecology.LightDriftSpeed > 0)
            {
                var next = world.TimedLights.PositionAt(source.Source, world.Tick + 1);
                var angle = Math.Atan2(next.Y - source.Y, next.X - source.X);
                var tipX = (float)(source.X + 16 * Math.Cos(angle));
                var tipY = (float)(source.Y + 16 * Math.Sin(angle));
                Line((float)source.X, (float)source.Y, tipX, tipY, Blue.WithAlpha(180), 1.2f);
                foreach (var turn in new[] { -.55, .55 })
                    Line(tipX, tipY, (float)(tipX - 5 * Math.Cos(angle + turn)),
                        (float)(tipY - 5 * Math.Sin(angle + turn)), Blue.WithAlpha(180), 1.2f);
            }
        }
        var gridPaint = Paint(new SKColor(150, 193, 181, 15), true);
        for (var x = 0; x <= world.ArenaWidth; x += 50) canvas.DrawLine(x, 0, x, (float)world.ArenaHeight, gridPaint);
        for (var y = 0; y <= world.ArenaHeight; y += 50) canvas.DrawLine(0, y, (float)world.ArenaWidth, y, gridPaint);
        if (world.RocksEnabled)
            foreach (var rock in world.Rocks)
            {
                canvas.DrawCircle((float)rock.X, (float)rock.Y, (float)rock.Radius, Paint(new SKColor(38, 51, 56)));
                canvas.DrawCircle((float)rock.X, (float)rock.Y, (float)rock.Radius, Paint(new SKColor(89, 103, 99), true, 2));
            }
        if (world.RecoverableHazards)
            foreach (var zone in world.StingZones)
            {
                canvas.DrawCircle((float)zone.X, (float)zone.Y, (float)zone.Radius, Paint(new SKColor(209, 111, 134, 48)));
                canvas.DrawCircle((float)zone.X, (float)zone.Y, (float)zone.Radius, Paint(new SKColor(209, 111, 134, 160), true, 1.5f));
                Text("sting", (float)zone.X - 13, (float)zone.Y + 3, 9, new SKColor(244, 185, 195));
            }
        foreach (var creature in world.Creatures)
        {
            if (!view.Trails.TryGetValue(creature.Slot, out var trail)) continue;
            var color = EnergyColor(creature.Fullness);
            for (var i = 1; i < trail.Count; i++)
                if (Math.Abs(trail[i].X - trail[i - 1].X) < world.ArenaWidth / 3 && Math.Abs(trail[i].Y - trail[i - 1].Y) < world.ArenaHeight / 3)
                    Line((float)trail[i - 1].X, (float)trail[i - 1].Y, (float)trail[i].X, (float)trail[i].Y,
                        color.WithAlpha((byte)(15 + 65 * i / trail.Count)), 1.2f);
        }
        if (view.Vision && world.Creatures.Length != 0) DrawEyes(world.Creatures[view.Selected]);
        var selectedSlot = world.Creatures.ElementAtOrDefault(view.Selected)?.Slot;
        foreach (var creature in world.Creatures) DrawSwimmer(creature, creature.Slot == selectedSlot);
        Text($"{world.Creatures.Length} SWIMMERS", 18, 27, 14, Ink);
        Text($"{TimeSpan.FromSeconds(world.Seconds):mm\\:ss}  ·  {world.Daylight:P0} daylight  ·  {world.ActiveLights.Length} lights", 18, 48, 12, Ink);
        Text("ENERGY", 18, (float)world.ArenaHeight - 32, 9, Muted);
        for (var i = 0; i < 26; i++)
            canvas.DrawRect(66 + i * 4, (float)world.ArenaHeight - 39, 4, 7, Paint(EnergyColor(i / 25.0)));
        Text("low → full", 178, (float)world.ArenaHeight - 32, 9, Ink);
        Text("Pink outline: injury · fades with pain memory", 255, (float)world.ArenaHeight - 32, 9, Ink);
        Text(world.CognitiveEcology ? "Nose: green seeks · gold leaves · blue escapes · violet courts" :
            "Light feeds · resting costs energy · full contact in darkness breeds", 18, (float)world.ArenaHeight - 16, 9, Ink);
        canvas.Restore();
        canvas.DrawRect(rect, Paint(Edge, true));
    }
    private void DrawEyes(Creature creature)
    {
        foreach (var eye in new[] { creature.LeftEye, creature.RightEye })
        {
            var origin = Bath.EyeOrigin(creature, eye);
            var a = creature.Angle + eye.Offset * Math.PI / 180;
            Line((float)origin.X, (float)origin.Y, (float)(origin.X + Math.Cos(a) * Bath.VisionDistance),
                (float)(origin.Y + Math.Sin(a) * Bath.VisionDistance), eye.Offset < 0 ? Blue : Gold, 2);
        }
    }
    private void DrawSwimmer(Creature creature, bool selected)
    {
        var color = EnergyColor(creature.Fullness);
        var mode = Blue;
        var injury = 0.0; var pain = 0.0;
        if (creature.Cognition is { } body)
        {
            mode = Held(body.Pilot.RecoveryUntil) > (long)body.Observations.Time.Value ||
                (body.Pilot.Progress is { } p && Held(p.Until) > (long)body.Observations.Time.Value) || body.Blocked.Value > 1 ? Blue :
                body.Pilot.CourtshipSignal.Value == 15 && body.Observations.LeftMate.Value + body.Observations.RightMate.Value > 0 &&
                    Held(body.Pilot.Modules[3].Parameters[6]) > 0 ? new SKColor(188,155,255) : body.Pilot.Appetite.Value == 0 ? Gold : Green;
            injury = body.Observations.Injury.Value / 15.0; pain = body.PainMemory.Value / 15.0;
        }
        if (selected) canvas.DrawCircle((float)creature.Px, (float)creature.Py, 19, Paint(Ink.WithAlpha(150), true, 1.4f));
        canvas.Save(); canvas.Translate((float)creature.Px, (float)creature.Py); canvas.RotateDegrees((float)(creature.Angle * 180 / Math.PI));
        canvas.DrawLine(-10, -5, -10 - (float)creature.LeftMuscle.Force.Value * .8f, -8, Paint(Green.WithAlpha(180), true, 1.8f));
        canvas.DrawLine(-10, 5, -10 - (float)creature.RightMuscle.Force.Value * .8f, 8, Paint(Green.WithAlpha(180), true, 1.8f));
        var hurtColor = new SKColor(252, 113, 173);
        if (injury > 0)
            canvas.DrawOval(new SKRect(-13, -9, 14, 9), Paint(hurtColor.WithAlpha(235), true, (float)(1.2 + 2.4 * injury)));
        else if (pain > 0)
            canvas.DrawOval(new SKRect(-13, -9, 14, 9), Paint(hurtColor.WithAlpha((byte)(110 * pain)), true, 1.4f));
        canvas.DrawOval(new SKRect(-10, -6, 11, 6), Paint(color.WithAlpha(220)));
        canvas.DrawOval(new SKRect(-5, -3, 5, 3), Paint(new SKColor(24, 66, 65)));
        canvas.DrawRect(-4, -1.5f, (float)(8 * creature.Fullness), 3, Paint(Ink.WithAlpha(180)));
        canvas.DrawCircle(6, -3.5f, 2, Paint(Ink)); canvas.DrawCircle(6, 3.5f, 2, Paint(Ink));
        canvas.DrawCircle(7, -3.5f, .8f, Paint(Panel)); canvas.DrawCircle(7, 3.5f, .8f, Paint(Panel));
        canvas.DrawCircle(10, 0, 2, Paint(mode));
        canvas.Restore();
        if (selected) Text($"{creature.Slot + 1:00}", (float)creature.Px + 22, (float)creature.Py + 4, 14);
    }

    // Presentation reads physical calories independently of injury and behavior.
    private static SKColor EnergyColor(double fullness)
    {
        var amount = Math.Clamp(fullness, 0, 1);
        var low = new SKColor(229, 103, 72); var middle = new SKColor(235, 194, 100); var high = new SKColor(108, 225, 177);
        var a = amount < .5 ? low : middle; var b = amount < .5 ? middle : high;
        var t = amount < .5 ? amount * 2 : (amount - .5) * 2;
        return new SKColor((byte)Math.Round(a.Red + (b.Red - a.Red) * t),
            (byte)Math.Round(a.Green + (b.Green - a.Green) * t), (byte)Math.Round(a.Blue + (b.Blue - a.Blue) * t));
    }

    private void DrawHistory(SKRect area)
    {
        Box(area, Panel); Text("OUTCOME HISTORY", area.Left + 12, area.Top + 19, 10, Muted);
        Text("calories / 5 s", area.Right - 155, area.Top + 19, 10, Gold); Text("energy", area.Right - 64, area.Top + 19, 10, Green);
        var trends = view.World.Trends;
        if (trends.Count < 2) { Text("Collecting the first five-second outcome window…", area.Left + 12, area.Top + 52, 12, Muted); return; }
        var maxFood = Math.Max(1, trends.Max(t => t.Food));
        for (var i = 1; i < trends.Count; i++)
        {
            var x1 = area.Left + 12 + (area.Width - 24) * (i - 1) / Math.Max(20, trends.Count - 1);
            var x2 = area.Left + 12 + (area.Width - 24) * i / Math.Max(20, trends.Count - 1);
            Line(x1, area.Bottom - 12 - (float)trends[i - 1].Energy * 42, x2, area.Bottom - 12 - (float)trends[i].Energy * 42, Green, 1.6f);
            Line(x1, area.Bottom - 12 - (float)(trends[i - 1].Food / maxFood) * 42, x2, area.Bottom - 12 - (float)(trends[i].Food / maxFood) * 42, Gold, 1.4f);
        }
    }

    private void DrawInspector(float x, float y, float width, float height)
    {
        DrawInspectorPanel(new SKRect(x, y, x + width, y + height), false);
    }

    private void DrawCognitiveInspector(SKRect area)
    {
        var c = view.World.Creatures[view.Selected]; var b = c.Cognition!;
        var scale = Math.Min(1, Math.Min(area.Width / 300, area.Height / 600));
        var originX = area.Left + (area.Width - 300 * scale) / 2;
        canvas.Save(); canvas.Translate(originX, area.Top); canvas.Scale(scale);
        Text($"Creature {c.Slot + 1:00} / own observations", 18, 27, 17);
        Text($"generation {c.Generation.Value} · {c.Age.Value / 20.0:F0}s old · {b.Samples.Value} samples", 18, 47, 10, Muted);
        var escaping = Held(b.Pilot.RecoveryUntil) > (long)b.Observations.Time.Value;
        var progressEscape = b.Pilot.Progress is { } progress && Held(progress.Until) > (long)b.Observations.Time.Value;
        var courting = TryValue(ReadMany(b.Pilot.CourtshipActive)[0],out var active) && active == 15;
        Text(escaping ? "ESCAPING INJURY" : b.Blocked.Value > 1 ? "ESCAPING CONTACT" : progressEscape ? "ESCAPING LOW PROGRESS" :
            courting ? "APPROACHING MATE" : b.Pilot.Appetite.Value == 0 ? "LEAVING LIGHT" : "SEEKING LIGHT", 18, 73, 13,
            escaping || progressEscape || b.Blocked.Value > 1 ? Blue : courting ? new SKColor(188,155,255) : b.Pilot.Appetite.Value == 0 ? Gold : Green);
        Bar("ENERGY", c.Fullness, $"{c.Energy.Value}/1200", 85, EnergyColor(c.Fullness));
        DrawEyeStrip(c.LeftEye, "LEFT EYE", 122, Blue); DrawEyeStrip(c.RightEye, "RIGHT EYE", 175, Gold);
        Text("MOTION FROM POSITION + TIME", 18, 245, 10, Green);
        Text($"Inferred {Math.Sqrt(Math.Max(0, Held(b.SpeedSquared))):F1} px/s · forecast {Math.Sqrt(Math.Max(0, Held(b.Forecast))):F1}", 18, 265, 12);
        Text($"Mismatch {Held(b.Error)} · pressure {b.Pressure.Tension} · blocked {b.Blocked.Value}", 18, 285, 10, Muted);
        Text($"Touch {b.Observations.LeftContact.Value}/{b.Observations.RightContact.Value} · nearby {b.Observations.LeftProximity.Value}/{b.Observations.RightProximity.Value}", 18, 304, 10, Muted);
        Text($"Injury {b.Observations.Injury.Value}/15 · pain {b.PainMemory.Value}/15 · escape {Math.Max(0,Held(b.Pilot.RecoveryUntil)-(long)b.Observations.Time.Value)/20.0:F1}s", 18, 323, 10, Muted);
        Text($"Mate cue {b.Observations.LeftMate.Value}/{b.Observations.RightMate.Value} · progress escapes {b.Pilot.Progress?.Episodes.Value ?? 0}", 18, 338, 10, Muted);
        Text("BREEDING MODULES · OWN UTILITY", 18, 350, 10, Green);
        string[] names = ["drive", "gradient", "appetite", "recovery"];
        var bids = ReadMany(b.Pilot.Modules.Select(m => (IElement)m.Quote).ToArray());
        for (var i = 0; i < b.Pilot.Modules.Length; i++)
        {
            var m = b.Pilot.Modules[i]; var quote = TryValue(bids[i], out var bid) ? bid.ToString("+0;-0;0") : "?";
            Text($"{names[i]}  [{string.Join(",", m.Parameters.Take(i == 3 ? 4 : m.Parameters.Length).Select(Held))}]", 18, 372 + i * 21, 10, Ink);
            Text($"{quote} / {m.Utility.Visits.Value}" + (Held(m.EstimatedUtility) == 0 ? " trials" : $" +{Held(m.EstimatedUtility)} est"), 181, 372 + i * 21, 9, Muted);
        }
        Text($"Jev: {b.Inquiry.Answers.Value}/{b.Inquiry.Requests.Value} replies · {b.Inquiry.AdviceTrials.Value} trials", 18, 470, 11, Blue);
        Text($"Stale {b.Inquiry.Stale.Value} · failed {b.Inquiry.Failed.Value} · fuel {Held(b.Inquiry.Fuel)}", 18, 491, 10, Muted);
        Text($"API session ${view.JevDollars:F5} / $0.02 · {view.JevRequests}/20 calls", 18, 512, 10, Muted);
        var button = new SKRect(18, 535, 273, 567);
        Box(button, new SKColor(32, 59, 57), 7); Text("Export selected native graph", 31, 556, 11, Green);
        view.Hit(new SKRect(originX + button.Left * scale, area.Top + button.Top * scale,
            originX + button.Right * scale, area.Top + button.Bottom * scale), view.Download);
        Text($"Actual computation cost: {c.Computation.Billed.Value} cal", 18, 589, 10, Muted);
        canvas.Restore();
    }

    private void DrawSensesInspector(float x, float y, float width, float height)
    {
        Box(new SKRect(x, y, x + width, y + height), Panel);
        if (view.World.Creatures.Length == 0) { Text("No living circuit", x + 18, y + 30, 16, Gold); return; }
        var s = Math.Min(1, height / 650);
        canvas.Save(); canvas.Translate(x, y); canvas.Scale(s);
        var creature = view.World.Creatures[view.Selected];
        Text("LOCAL STRUCTURE", 18, 23, 10, Green);
        Text($"Creature {creature.Slot + 1:00}", 18, 50, 22);
        Text($"generation {creature.Generation.Value}  ·  age {creature.Age.Value / 20.0:N0}s", 18, 69, 11, Muted);
        Bar("ENERGY", creature.Fullness, $"{creature.Energy.Value} / {Creature.Capacity}", 91, Gold);
        DrawEyeStrip(creature.LeftEye, "LEFT EYE", 134, Blue);
        DrawEyeStrip(creature.RightEye, "RIGHT EYE", 189, Gold);
        Text("INDEPENDENT PUSHES", 18, 252, 10, Muted);
        Bar("L", creature.LeftMuscle.Force.Value / 15.0, $"{creature.LeftMuscle.Force.Value} / 15", 267, Green);
        Bar("R", creature.RightMuscle.Force.Value / 15.0, $"{creature.RightMuscle.Force.Value} / 15", 300, Blue);
        Text("Right push turns left · left push turns right", 18, 340, 10, Muted);
        Text("PAID CIRCUIT OFFERS", 18, 368, 10, Green);
        DrawBids(creature.LeftMuscle, 386, "L");
        DrawBids(creature.RightMuscle, 415, "R");
        Text($"{creature.ComponentCount} terms · tension {creature.Pressure.Tension} · {creature.Rewrites} changes", 18, 458, 11, Muted);
        GraphNode("eyes", 18, 472, 58, Blue); GraphNode("circuit", 98, 472, 75, Gold); GraphNode("push", 196, 472, 75, Green);
        Line(76, 486, 98, 486, Muted); Line(173, 486, 196, 486, Muted);
        Text("20 s trial → tension → add / remove / reconnect", 18, 517, 10, Muted);
        var reviews = creature.LeftMuscle.Reviews + creature.RightMuscle.Reviews;
        Text($"{reviews} closed reviews  ·  {creature.Wallet.Value} wallet credits", 18, 540, 11);
        var review = Parts(creature.LeftMuscle.LastReview);
        if (review.Length == 5)
        {
            var outcome = Parts((Relation)review[3]);
            var relief = (Relation)outcome[2];
            Text($"Latest shared return: {(relief.Left is CarrierLocation r ? r.Ticks.ToString("+0;-0;0") : "unresolved")}", 18, 562, 11, Gold);
        }
        else Text("Waiting for the first closed action interval", 18, 562, 10, Muted);
        var export = new SKRect(18, 580, 273, 612);
        Box(export, new SKColor(32, 59, 57), 7); Text("Export selected native graph", 31, 601, 11, Green);
        view.Hit(new SKRect(x + export.Left * s, y + export.Top * s, x + export.Right * s, y + export.Bottom * s), view.Download);
        Text($"Compute demand {creature.Computation.Billed.Value} cal · supply {view.World.FoodAvailability:P0}", 18, 634, 10, Muted);
        canvas.Restore();
    }
    private void Bar(string label, double fraction, string number, float y, SKColor color)
    {
        Text(label, 18, y + 10, 10, Muted); Text(number, 204, y + 10, 10, Ink);
        Box(new SKRect(18, y + 18, 273, y + 23), new SKColor(37, 56, 61), 2);
        Box(new SKRect(18, y + 18, 18 + (float)Math.Clamp(fraction, 0, 1) * 255, y + 23), color, 2);
    }
    private void DrawEyeStrip(Eye eye, string label, float y, SKColor tint)
    {
        Text(label, 18, y + 10, 10, tint);
        Text(eye.Presence.Value == 0 ? "unsampled" : $"sample {(view.World.Tick - (long)eye.SampleTime.Value) * 50} ms ago", 143, y + 10, 9, Muted);
        for (var i = 0; i < eye.Pixels.Length; i++)
        {
            var cell = 255f / eye.Pixels.Length;
            var level = (byte)(30 + eye.Pixels[i].Value * 195 / (ulong)eye.Maximum.Ticks);
            Box(new SKRect(18 + i * cell, y + 18, 18 + (i + 1) * cell - 3, y + 43), new SKColor(level, level, level), 3);
            Text(eye.Presence.Value == 0 ? "?" : eye.Pixels[i].Value + $" / {eye.Maximum.Ticks} · central 10 px ray", 26 + i * cell, y + 35, 10, level > 100 ? Panel : Ink);
        }
    }

    private static readonly string[] SourceLabels = ["constant", "left eye", "right eye", "energy deficit", "fullness",
        "left eye × deficit", "right eye × deficit", "left eye × fullness", "right eye × fullness"];

    private void DrawCircuitInspector(SKRect area)
    {
        if (view.Training) return;
        var creature = view.World.Creatures[view.Selected];
        var paired = area.Width >= 470;
        var width = paired ? Math.Min(680, area.Width - 28) : 276;
        var height = paired ? 322 : 548;
        var scale = Math.Min(1, Math.Min(area.Width / (width + 24), area.Height / (height + 4)));
        canvas.Save(); canvas.Translate(area.Left + (area.Width - width * scale) / 2, area.Top); canvas.Scale(scale);
        var left = CircuitInspection.Observe(creature.LeftMuscle.Current.Root);
        var right = CircuitInspection.Observe(creature.RightMuscle.Current.Root);
        var size = CircuitInspection.Size(creature.LeftMuscle.Current.Root, creature.RightMuscle.Current.Root);
        Text($"Creature {creature.Slot + 1:00} / processing circuits", 0, 20, 17);
        var operations = ExpressionGraph.Count(creature.Features, creature.LeftMuscle.Current.Components.Concat(creature.RightMuscle.Current.Components).Select(t => t.Source).ToArray());
        Text($"{creature.ComponentCount} / 8 terms · {operations} internal ops · {size.Relations} Gojo relations · {size.Carriers} carriers", 0, 39, 10, Muted);
        var column = paired ? (width - 18) / 2 : width;
        DrawCircuitColumn(creature, creature.LeftMuscle, left, "LEFT PUSH", 0, 56, column, Blue);
        DrawCircuitColumn(creature, creature.RightMuscle, right, "RIGHT PUSH", paired ? column + 18 : 0,
            paired ? 56 : 282, column, Green);
        var foot = paired ? 283 : 509;
        var nonzero = 0; var unresolved = 0;
        foreach (var observation in new[] { left, right })
        {
            var fields = Parts(observation);
            if (fields.Length != 3) continue;
            foreach (var term in Parts((Relation)fields[1]).Cast<Relation>())
            {
                var contribution = (Relation)Parts(term)[3];
                if (!TryValue(contribution, out var value)) unresolved++; else if (value != 0) nonzero++;
            }
        }
        Text($"{nonzero} nonzero whole-tick contributions · {unresolved} unresolved", 0, foot, 10, Muted);
        Text("Graph counts share inputs and exclude utility / trial history.", 0, foot + 16, 9, Muted);
        var cells = Parts(creature.ControllerState);
        var modes = cells.Select(cell => Parts((Relation)cell)[0]).OfType<Carrier>().Select(stock => stock.Value.ToString());
        Text(cells.Length == 0 ? "Five input signals · arithmetic and conditional choices." : $"{cells.Length} memory stocks · current {string.Join(" / ", modes)} · native feedback", 0, foot + 32, 9, Gold);
        canvas.Restore();
    }

    private void DrawCircuitColumn(Creature creature, Muscle muscle, Relation observation, string label,
        float x, float y, float width, SKColor tint)
    {
        var fields = Parts(observation);
        if (fields.Length != 3) { Text("Unresolved circuit surface", x, y + 12, 12, Gold); return; }
        var terms = Parts((Relation)fields[1]).Cast<Relation>().ToArray();
        Text($"{label} · {terms.Length} / {Circuit.MaximumComponents} terms", x, y + 12, 12, tint);
        var sourceWidth = width - 145;
        var gainX = x + sourceWidth + 14; var outX = gainX + 69;
        Text("INPUT", x, y + 30, 9, Muted); Text("STRENGTH", gainX, y + 30, 9, Muted); Text("PUSH", outX, y + 30, 9, Muted);
        for (var i = 0; i < terms.Length; i++)
        {
            var term = Parts(terms[i]); var component = Parts((Relation)term[0]);
            var index = CircuitInspection.SourceIndex(creature.Features, component[0]);
            var source = index >= 0 && index < SourceLabels.Length ? SourceLabels[index] : ExpressionGraph.Describe(creature.Features, component[0]);
            using (var font = new SKFont(Typeface, 9))
            {
                while (source.Length > 3 && font.MeasureText(source) > sourceWidth - 12) source = source[..^2] + "…";
            }
            var input = TryValue((Relation)term[1], out var sample) ? sample.ToString() : "?";
            var coefficient = ((CarrierLocation)term[2]).Ticks.ToString("+0;-0;0");
            var known = TryValue((Relation)term[3], out var contribution);
            var color = !known || contribution == 0 ? Muted : contribution < 0 ? Gold : Green;
            var row = y + 39 + i * 33;
            Box(new SKRect(x, row, x + sourceWidth, row + 29), new SKColor(23, 43, 51), 4);
            var termOps = ExpressionGraph.Count(creature.Features, component[0]);
            Text(source, x + 6, row + 12, 9, tint); Text($"value {input}" + (termOps == 0 ? "" : $" · {termOps} ops"), x + 6, row + 24, 9, Muted);
            Line(x + sourceWidth + 2, row + 15, gainX - 2, row + 15, color);
            Box(new SKRect(gainX, row, gainX + 55, row + 29), new SKColor(24, 44, 51), 4);
            Text(coefficient + "/15", gainX + 5, row + 19, 11, color);
            Line(gainX + 57, row + 15, outX - 2, row + 15, color);
            Box(new SKRect(outX, row, outX + 48, row + 29), color.WithAlpha(25), 4);
            Text(known ? contribution.ToString("+0;-0;0") : "?", outX + 7, row + 20, 14, color);
        }
        if (terms.Length == 0) Text("No operating terms", x, y + 58, 11, Gold);
        var sum = TryValue((Relation)fields[2], out var total) ? total.ToString("+0;-0;0") : "?";
        Text($"Computed {sum} → applied {muscle.Force.Value}/15", x, y + 188, 11);
        Text(CircuitInspection.TryLastSizeChange(muscle.LastRewrite, out var before, out var after)
            ? $"Last edit: {before} → {after} terms" : "No local circuit edit yet", x, y + 205, 10, Muted);
    }
    private void GraphNode(string label, float x, float y, float width, SKColor color)
    { var r = new SKRect(x, y, x + width, y + 27); Box(r, color.WithAlpha(20), 5); canvas.DrawRoundRect(r, 5, 5, Paint(color.WithAlpha(100), true)); Text(label, x + 8, y + 18, 10, color); }
    private void DrawBids(Muscle muscle, float y, string side)
    {
        Text(side, 18, y + 15, 12, Muted);
        var auction = Parts(muscle.LastAuction);
        if (auction.Length < 2) { Text("no quotes yet", 40, y + 15, 10, Muted); return; }
        var quotes = Parts((Relation)auction[1]);
        for (var i = 0; i < quotes.Length; i++)
        {
            var fields = Parts((Relation)quotes[i]);
            if (fields.Length != 3 || fields[0] is not Relation circuit) continue;
            var bid = (Relation)fields[1];
            var text = bid.Left is CarrierLocation score ? score.Ticks.ToString("+0;-0;0") : "?";
            var selected = ReferenceEquals(circuit, muscle.Current.Root);
            var terms = Parts((Relation)Parts(circuit)[0]).Length;
            Box(new SKRect(38 + i * 58, y, 91 + i * 58, y + 23), selected ? new SKColor(42, 76, 62) : new SKColor(24, 44, 51), 4);
            Text($"{terms}: {text}", 42 + i * 58, y + 15, 9, selected ? Green : Muted);
        }
    }
}


