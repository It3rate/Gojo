using System.Numerics;
using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;
using static Gojo.Experiments.QuickDraw.SequentialEvidence;

namespace Gojo.Experiments.QuickDraw;

/// <summary>Finite, non-generative reads over published source profiles. Actual
/// source intervals, profile controls, ray selections, tolerance and unmatched
/// participation remain connected. Display names never supply a native joint.
/// Angles are an explicit finite XY observer calibration, not a new turn axiom.</summary>
public static class QuickDrawAnnotationEvidence
{
    public const long CoordinateQuantum = 1000, ParameterQuantum = 10000, AngleQuantum = 1000;
    // annotation = group(interpretation, finite profile basis, calibration,
    //                    profile witnesses, policy, line reads, joint reads,
    //                    reflection reads, selected relation reads)
    // profile witness: actual acquisition selection -> published finite curve;
    //                  entire acquisition interval remains unaccounted residue.
    // policy = group(full-turn calibration, angle allowance, proximity,
    //                linearity, reflection allowance, repetition allowance,
    //                minimum reach, parameter grade, coincident-ray allowance)
    // ray = ReadWitness(profile, ordered parameter span, position -> direction,
    //                   profile, profile); no new source site is manufactured.
    // joint result = group(actual ray witnesses, pair witnesses, gap observation,
    //                      participant-location witnesses including zero tangents).
    // Participant provenance separately retains an optional actual acquired
    // sample row; finite parameter equality never supplies source-site identity.
    // pair result = group(angle, opposed residual, right-angle residual,
    //                     same-direction residual) in the selected angle basis.
    // reflection result retains actual profile pairs, target endpoint order,
    // measured finite residuals and unmatched profiles; no geometry is changed.
    public sealed record Profile(int Index, NativeReconstruction.Curve Curve, Relation Witness,
        QuickDrawInterpretationEvidence.Span Source);
    public sealed record Policy(Carrier Angle, Carrier Parameter, long AngleAllowance, long Proximity,
        long Linearity, long Reflection, long Repetition, long MinimumReach, long CoincidentAllowance, Relation Surface);
    public sealed record Ray(Profile Profile, long At, long Toward, Point Position, Point Direction,
        Placeholder? Endpoint, Relation Witness);
    public sealed record Participant(Profile Profile, long At, Point Position, Placeholder? Endpoint, Relation Witness);
    public sealed record Line(Profile Profile, string Orientation, bool Straight, long Bend, Relation Witness);
    public sealed record Joint(string Kind, bool ActualSharedSite, long Gap, Ray[] Rays, Profile[] Profiles,
        int OpposedPairs, int PerpendicularPairs, Relation Witness, Participant[] Participants);
    public sealed record Symmetry(int Pairs, int WithinTolerance, int Unmatched, long MaximumResidual, Relation Witness);
    public sealed record Read(QuickDrawInterpretationEvidence.Read Interpretation, Frame Frame, Policy Policy,
        Profile[] Profiles, Line[] Lines, Joint[] Joints, Symmetry[] Symmetries, int Relations, Relation Surface);

    public static Relation Import(JsonElement packet)
    {
        var baseSurface = QuickDrawInterpretationEvidence.Import(packet);
        if (!QuickDrawInterpretationEvidence.TryRead(baseSurface, out var source)) throw new InvalidDataException("Source interpretation unreadable.");
        SequentialEvidence.TryRead(Create(CoordinateQuantum, []), out var empty); var frame = empty.Frame;
        var annotations = packet.GetProperty("annotations"); var rawPolicy = annotations.GetProperty("policy");
        var angle = new Carrier(); var parameter = new Carrier();
        long Distance(string name) => Quantize(rawPolicy.GetProperty(name).GetDouble(), CoordinateQuantum);
        var policySurface = G(Relation.Segment(angle, 0, 360 * AngleQuantum), new CarrierLocation(angle, Quantize(rawPolicy.GetProperty("angleDegrees").GetDouble(), AngleQuantum)),
            new CarrierLocation(frame.X, Distance("proximity")), new CarrierLocation(frame.X, Distance("linearity")),
            new CarrierLocation(frame.X, Distance("reflectionDistance")), new CarrierLocation(frame.X, Distance("repetitionDistance")),
            new CarrierLocation(frame.X, Distance("minSpanLength")), Relation.Segment(parameter, 0, ParameterQuantum),
            new CarrierLocation(angle, Quantize(rawPolicy.GetProperty("coincidentDegrees").GetDouble(), AngleQuantum)));
        if (!TryPolicy(policySurface, frame, out var policy)) throw new InvalidDataException("Invalid finite annotation policy.");
        var sourceCalibration = Entries(source.Calibration);
        var bounds = packet.GetProperty("observation").GetProperty("bounds").EnumerateArray().Select(v => Quantize(v.GetDouble(), CoordinateQuantum)).ToArray();
        var calibration = G(sourceCalibration[0], sourceCalibration[2], frame.Surface,
            new Relation(Position(frame.X, frame.Y, new(bounds[0], bounds[1])), Position(frame.X, frame.Y, new(bounds[2], bounds[3]))));
        var inputProfiles = packet.GetProperty("observation").GetProperty("spans").EnumerateArray().ToArray();
        if (inputProfiles.Length != source.Spans.Count) throw new InvalidDataException("Source profile coverage changed.");
        var profiles = new List<Profile>(); var byId = new Dictionary<string, Profile>(StringComparer.Ordinal);
        for (var i = 0; i < inputProfiles.Length; i++)
        {
            var span = source.Spans[i]; var samples = Entries(span.Selection.Right).Cast<Relation>().ToArray();
            var controls = inputProfiles[i].GetProperty("controls").EnumerateArray().Select(point => PointOf(point)).ToArray();
            var curve = NativeReconstruction.BuildCurve(frame, (Placeholder)samples[0].Left, (Placeholder)samples[^1].Left, controls);
            if (!NativeReconstruction.TryReadCurve(frame, curve, out var geometry)) throw new InvalidDataException("Finite source curve unreadable.");
            var witness = ReadWitness.Create(new Carrier(), span.Selection, calibration, curve, span.Selection, span.Witness);
            var profile = new Profile(i, geometry, witness, span); profiles.Add(profile);
            byId.Add(inputProfiles[i].GetProperty("id").GetString()!, profile);
        }
        Profile[] Selected(JsonElement row) => row.GetProperty("spans").EnumerateArray().Select(id => byId[id.GetString()!]).ToArray();
        var lines = new List<IElement>();
        foreach (var row in annotations.GetProperty("lines").EnumerateArray())
        {
            var selected = Selected(row); if (selected.Length != 1) throw new InvalidDataException("A line read selects one actual source profile.");
            lines.Add(LineWitness(frame, policy, selected[0]));
        }
        var joints = new List<IElement>();
        foreach (var row in annotations.GetProperty("joints").EnumerateArray())
        {
            var selected = Selected(row); var center = PointOf(row.GetProperty("point"));
            var rays = row.GetProperty("rays").EnumerateArray().Select(ray => RayWitness(frame, policy, byId[ray.GetProperty("spanId").GetString()!],
                Quantize(ray.GetProperty("t").GetDouble(), ParameterQuantum), ray.GetProperty("toward").GetString() == "start" ? 0 : ParameterQuantum)).ToArray();
            var participants = row.GetProperty("participants").EnumerateArray().Select(site => ParticipantWitness(frame, policy,
                byId[site.GetProperty("spanId").GetString()!], site)).ToArray();
            joints.Add(JointWitness(frame, policy, selected, center, rays, participants));
        }
        var symmetries = new List<IElement>();
        foreach (var row in annotations.GetProperty("symmetries").EnumerateArray())
        {
            var axis = row.GetProperty("axis"); var a = PointOf(axis.GetProperty("from")); var b = PointOf(axis.GetProperty("to"));
            if (a == b) throw new InvalidDataException("A reflection read needs a nonzero axis.");
            var operation = Reflection(frame, a, b); var basis = G(policy.Surface, operation);
            var pairs = row.GetProperty("correspondences").EnumerateArray().Select(pair => {
                var first = byId[pair.GetProperty("source").GetString()!]; var second = byId[pair.GetProperty("target").GetString()!];
                var reverse = pair.GetProperty("reversed").GetBoolean();
                var order = new Relation(reverse ? second.Curve.To : second.Curve.From, reverse ? second.Curve.From : second.Curve.To);
                var selected = new Relation(first.Witness, new Relation(second.Witness, order));
                var residual = ReflectionResidual(first, second, a, b, reverse);
                return (IElement)ReadWitness.Create(new Carrier(), selected, basis, new CarrierLocation(frame.X, residual), G(first.Witness, second.Witness), operation);
            }).ToArray();
            var unmatched = G(row.GetProperty("unmatchedSpanIds").EnumerateArray().Select(id => (IElement)byId[id.GetString()!].Witness).ToArray());
            var scope = Selected(row).Select(p => (IElement)p.Witness).Concat(Entries(unmatched)).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
            symmetries.Add(ReadWitness.Create(new Carrier(), G(scope), basis, G(pairs), unmatched, operation));
        }
        var relations = annotations.GetProperty("relations").EnumerateArray().Select(row => {
            var selected = Selected(row); var scope = G(selected.Select(p => (IElement)p.Witness).ToArray());
            // Generic source-pair observation. Rich UI names remain adapter
            // interpretations; actual chord/tangent/spacing participation stays.
            var rays = selected.Select(profile => RayWitness(frame, policy, profile, 0, ParameterQuantum)).ToArray();
            var pairs = PairWitnesses(frame, policy, profiles.ToArray(), rays);
            return (IElement)ReadWitness.Create(new Carrier(), scope, policy.Surface, G(G(rays), G(pairs)), scope, baseSurface);
        }).ToArray();
        var result = G(baseSurface, frame.Surface, calibration, G(profiles.Select(p => (IElement)p.Witness).ToArray()), policySurface,
            G(lines.ToArray()), G(joints.ToArray()), G(symmetries.ToArray()), G(relations));
        if (!TryRead(result, out _)) throw new InvalidDataException("Native annotation references or finite observations are inconsistent.");
        return result;
    }

    public static bool TryRead(Relation surface, out Read read)
    {
        read = null!;
        if (!Fields(surface, 9, out var f) || f[0] is not Relation baseSurface || !QuickDrawInterpretationEvidence.TryRead(baseSurface, out var source) ||
            f[1] is not Relation frameSurface || !TryReadFrame(frameSurface, out var frame) || frame.Quantum != CoordinateQuantum ||
            f[2] is not Relation calibration || !Fields(calibration, 4, out var cb) || !Fields(source.Calibration, 4, out var sourceCalibration) ||
            !ReferenceEquals(cb[0], sourceCalibration[0]) || !ReferenceEquals(cb[1], sourceCalibration[2]) || !ReferenceEquals(cb[2], frame.Surface) ||
            cb[3] is not Relation bounds || !TryPosition(bounds.Left, frame, out var low) || !TryPosition(bounds.Right, frame, out var high) || high.X < low.X || high.Y < low.Y ||
            !Elements(f[3], out var profileRows) || profileRows.Length != source.Spans.Count || f[4] is not Relation policySurface || !TryPolicy(policySurface, frame, out var policy) ||
            !Elements(f[5], out var lineRows) || !Elements(f[6], out var jointRows) || !Elements(f[7], out var symmetryRows) || !Elements(f[8], out var relationRows)) return false;
        var profiles = new List<Profile>();
        for (var i = 0; i < profileRows.Length; i++)
        {
            var span = source.Spans[i]; var samples = Entries(span.Selection.Right).Cast<Relation>().ToArray();
            if (!Witness(profileRows[i], out var selected, out var basis, out var result, out var residue, out var provenance) ||
                !ReferenceEquals(selected, span.Selection) || !ReferenceEquals(basis, calibration) || !ReferenceEquals(residue, selected) || !ReferenceEquals(provenance, span.Witness) ||
                result is not Relation curve || !NativeReconstruction.TryReadCurve(frame, curve, out var geometry) ||
                !ReferenceEquals(geometry.From, samples[0].Left) || !ReferenceEquals(geometry.To, samples[^1].Left) ||
                geometry.Controls.Any(p => !Bounded(p))) return false;
            profiles.Add(new(i, geometry, (Relation)profileRows[i], span));
        }
        var profileMap = new Dictionary<IElement, Profile>(ReferenceEqualityComparer.Instance);
        foreach (var profile in profiles) profileMap.Add(profile.Witness, profile);
        var lines = new List<Line>();
        foreach (var row in lineRows)
        {
            if (!Witness(row, out var selected, out var basis, out var result, out var residue, out var provenance) ||
                !profileMap.TryGetValue(selected, out var profile) || !ReferenceEquals(basis, policy.Surface) || !ReferenceEquals(residue, selected) || !ReferenceEquals(provenance, selected) ||
                !Fields(result, 5, out var parts) || !ReadRay(parts[0], frame, policy, profileMap, out var startRay) || !ReadRay(parts[1], frame, policy, profileMap, out var endRay) ||
                !ReferenceEquals(startRay.Profile, profile) || !ReferenceEquals(endRay.Profile, profile) ||
                startRay.At != 0 || startRay.Toward != ParameterQuantum || endRay.At != ParameterQuantum || endRay.Toward != 0 ||
                !Number(parts[2], policy.Angle, out var h) || !Number(parts[3], policy.Angle, out var v) || !Number(parts[4], frame.X, out var bend)) return false;
            var expected = LineMeasures(profile);
            if (h != expected.H || v != expected.V || bend != expected.Bend) return false;
            lines.Add(new(profile, profile.Curve.Controls[0] == profile.Curve.Controls[^1] ? "unresolved" :
                h <= policy.AngleAllowance ? "horizontal" : v <= policy.AngleAllowance ? "vertical" : "angular", bend <= policy.Linearity, bend, (Relation)row));
        }
        if (lines.Count != profiles.Count || lines.Select(line => line.Profile.Witness).Distinct(ReferenceEqualityComparer.Instance).Count() != profiles.Count) return false;
        var joints = new List<Joint>();
        foreach (var row in jointRows)
        {
            if (!Witness(row, out var selected, out var basis, out var result, out var residue, out var provenance) ||
                !Elements(selected, out var selectedRows) || selectedRows.Any(p => !profileMap.ContainsKey(p)) ||
                !Fields(basis, 2, out var b) || !ReferenceEquals(b[0], policy.Surface) || !TryPosition(b[1], frame, out var center) ||
                !ReferenceEquals(residue, selected) || !ReferenceEquals(provenance, selected) || !Fields(result, 4, out var body) ||
                !Elements(body[0], out var rayRows) || !Elements(body[1], out var pairRows) || !Number(body[2], frame.X, out var gap) || !Elements(body[3], out var participantRows)) return false;
            var participants = new List<Participant>();
            foreach (var site in participantRows)
            { if (!ReadParticipant(site, frame, policy, profileMap, out var p) || !selectedRows.Contains(p.Profile.Witness, ReferenceEqualityComparer.Instance)) return false; participants.Add(p); }
            if (participants.Count == 0 || rayRows.Length > 0 && selectedRows.Any(p => !participants.Any(site => ReferenceEquals(site.Profile.Witness, p)))) return false;
            var rays = new List<Ray>();
            foreach (var ray in rayRows) { if (!ReadRay(ray, frame, policy, profileMap, out var r) || !participants.Any(p => ReferenceEquals(p.Profile, r.Profile) && p.At == r.At)) return false; rays.Add(r); }
            if (!ReadPairs(pairRows, rays.ToArray(), policy, out var opposed, out var right, out var coincident)) return false;
            var selectedProfiles = selectedRows.Select(p => profileMap[p]).ToArray();
            var cycleEnds = selectedProfiles.Length > 0 && participants.Count == 2 &&
                ReferenceEquals(participants[0].Profile, selectedProfiles[0]) && participants[0].At == 0 &&
                ReferenceEquals(participants[1].Profile, selectedProfiles[^1]) && participants[1].At == ParameterQuantum;
            var expectedGap = Diameter(participants.Select(site => site.Position).ToArray());
            if (gap != expectedGap) return false;
            var shared = participants[0].Endpoint is not null && participants.All(site => ReferenceEquals(site.Endpoint, participants[0].Endpoint));
            var kind = coincident ? "coincident rays" : gap > policy.Proximity || rays.Any(ray => ray.Direction == new Point()) ? "unresolved" :
                rays.Count == 0 ? cycleEnds && CycleSupported(selectedProfiles, policy) ? "O candidate" : "unresolved" :
                rays.Count switch { 1 => "endpoint", 2 => opposed > 0 ? "I" : "L", 3 => opposed > 0 ? "T" : "Y",
                    4 => DisjointOpposed(rays, policy) ? "X" : opposed > 0 ? "K" : "unresolved", _ => "unresolved" };
            joints.Add(new(kind, shared, gap, rays.ToArray(), selectedProfiles, opposed, right, (Relation)row, participants.ToArray()));
        }
        var symmetries = new List<Symmetry>();
        foreach (var row in symmetryRows)
        {
            if (!Witness(row, out var selected, out var basis, out var result, out var residue, out var provenance) ||
                !Elements(selected, out var selectedRows) || selectedRows.Any(p => !profileMap.ContainsKey(p)) || selectedRows.Distinct(ReferenceEqualityComparer.Instance).Count() != selectedRows.Length ||
                !Fields(basis, 2, out var b) || !ReferenceEquals(b[0], policy.Surface) || b[1] is not Relation operation || !ReadReflection(operation, frame, out var a, out var c) ||
                !ReferenceEquals(provenance, operation) || !Elements(result, out var pairs) || !Elements(residue, out var unmatched) || unmatched.Any(p => !profileMap.ContainsKey(p))) return false;
            var residuals = new List<long>(); var matched = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            foreach (var pair in pairs)
            {
                if (!Witness(pair, out var picked, out var pb, out var measured, out var remainder, out var pp) || !ReferenceEquals(pb, basis) || !ReferenceEquals(pp, operation) ||
                    picked is not Relation { Left: var first, Right: Relation { Left: var second, Right: Relation order } } || !profileMap.TryGetValue(first, out var sourceProfile) || !profileMap.TryGetValue(second, out var targetProfile) ||
                    !Orientation(order, targetProfile, out var reverse) || !Number(measured, frame.X, out var error) ||
                    !Fields(remainder, 2, out var retained) || !ReferenceEquals(retained[0], first) || !ReferenceEquals(retained[1], second) ||
                    error != ReflectionResidual(sourceProfile, targetProfile, a, c, reverse)) return false;
                if (!selectedRows.Contains(first, ReferenceEqualityComparer.Instance) || !selectedRows.Contains(second, ReferenceEqualityComparer.Instance) ||
                    matched.Contains(first) || matched.Contains(second)) return false;
                matched.Add(first); matched.Add(second); residuals.Add(error);
            }
            if (unmatched.Distinct(ReferenceEqualityComparer.Instance).Count() != unmatched.Length ||
                unmatched.Any(p => matched.Contains(p) || !selectedRows.Contains(p, ReferenceEqualityComparer.Instance)) || matched.Count + unmatched.Length != selectedRows.Length) return false;
            symmetries.Add(new(pairs.Length, residuals.Count(error => error <= policy.Reflection), unmatched.Length, residuals.DefaultIfEmpty().Max(), (Relation)row));
        }
        foreach (var row in relationRows)
        {
            if (!Witness(row, out var selected, out var basis, out var result, out var residue, out var provenance) ||
                !Elements(selected, out var selectedRows) || selectedRows.Any(p => !profileMap.ContainsKey(p)) || !ReferenceEquals(basis, policy.Surface) ||
                !ReferenceEquals(residue, selected) || !ReferenceEquals(provenance, baseSurface) || !Fields(result, 2, out var body) ||
                !Elements(body[0], out var rayRows) || rayRows.Length != selectedRows.Length || !Elements(body[1], out var pairRows)) return false;
            var rays = new List<Ray>();
            for (var i = 0; i < rayRows.Length; i++) { if (!ReadRay(rayRows[i], frame, policy, profileMap, out var ray) || !ReferenceEquals(ray.Profile.Witness, selectedRows[i])) return false; rays.Add(ray); }
            if (!ReadPairs(pairRows, rays.ToArray(), policy, out _, out _, out _)) return false;
        }
        read = new(source, frame, policy, profiles.ToArray(), lines.ToArray(), joints.ToArray(), symmetries.ToArray(), relationRows.Length, surface); return true;
    }

    private static Relation LineWitness(Frame frame, Policy policy, Profile profile)
    {
        var m = LineMeasures(profile);
        return ReadWitness.Create(new Carrier(), profile.Witness, policy.Surface,
            G(RayWitness(frame, policy, profile, 0, ParameterQuantum), RayWitness(frame, policy, profile, ParameterQuantum, 0),
                new CarrierLocation(policy.Angle, m.H), new CarrierLocation(policy.Angle, m.V), new CarrierLocation(frame.X, m.Bend)), profile.Witness, profile.Witness);
    }
    private static Relation JointWitness(Frame frame, Policy policy, Profile[] profiles, Point center, Relation[] rays, Relation[] participants)
    {
        var map = new Dictionary<IElement, Profile>(ReferenceEqualityComparer.Instance); foreach (var profile in profiles) map[profile.Witness] = profile;
        var reads = rays.Select(ray => { if (!ReadRay(ray, frame, policy, map, out var r)) throw new InvalidDataException("Invalid source ray."); return r; }).ToArray();
        var selected = G(profiles.Select(p => (IElement)p.Witness).ToArray());
        var sites = participants.Select(row => { if (!ReadParticipant(row, frame, policy, map, out var p)) throw new InvalidDataException("Invalid source participant."); return p; }).ToArray();
        var gap = Diameter(sites.Select(site => site.Position).ToArray());
        return ReadWitness.Create(new Carrier(), selected, G(policy.Surface, Position(frame.X, frame.Y, center)),
            G(G(rays), G(PairWitnesses(frame, policy, profiles, rays)), new CarrierLocation(frame.X, gap), G(participants)), selected, selected);
    }
    private static Relation ParticipantWitness(Frame frame, Policy policy, Profile profile, JsonElement participant)
    {
        var rawParameter = participant.GetProperty("t"); var exact = ExactNumber.Parse(rawParameter);
        var at = Quantize(rawParameter.GetDouble(), ParameterQuantum);
        if (exact.Numerator < 0 || exact.Numerator > exact.Denominator || at < 0 || at > ParameterQuantum)
            throw new InvalidDataException("Participant parameter lies outside its profile.");
        var exactStart = exact.Numerator.IsZero; var exactEnd = exact.Numerator == exact.Denominator;
        IElement[] acquired = [];
        if ((exactStart || exactEnd) && participant.TryGetProperty("sourceSite", out var site) && site.ValueKind == JsonValueKind.Object)
        {
            var sampleIndex = exactStart ? profile.Source.Start : profile.Source.End;
            if (site.GetProperty("strokeIndex").GetInt32() != profile.Source.Stroke || site.GetProperty("sampleIndex").GetInt32() != sampleIndex)
                throw new InvalidDataException("Acquired participant attestation does not select its actual endpoint sample.");
            var samples = Entries(profile.Source.Selection.Right); acquired = [exactStart ? samples[0] : samples[^1]];
        }
        return ReadWitness.Create(new Carrier(), profile.Witness, new CarrierLocation(policy.Parameter, at),
            Position(frame.X, frame.Y, CurvePoint(profile.Curve.Controls, (double)at / ParameterQuantum)), profile.Witness, G(profile.Witness, G(acquired)));
    }
    private static bool ReadParticipant(IElement row, Frame frame, Policy policy, Dictionary<IElement, Profile> profiles, out Participant participant)
    {
        participant = null!;
        if (!Witness(row, out var selected, out var basis, out var result, out var residue, out var provenance) || !profiles.TryGetValue(selected, out var profile) ||
            !Number(basis, policy.Parameter, out var at) || at < 0 || at > ParameterQuantum || !ReferenceEquals(residue, selected) ||
            !Fields(provenance, 2, out var proof) || !ReferenceEquals(proof[0], selected) || !Elements(proof[1], out var acquired) || acquired.Length > 1 ||
            !TryPosition(result, frame, out var point) || point != CurvePoint(profile.Curve.Controls, (double)at / ParameterQuantum)) return false;
        Placeholder? endpoint = null;
        if (acquired.Length == 1)
        {
            var samples = Entries(profile.Source.Selection.Right);
            if (at == 0 && ReferenceEquals(acquired[0], samples[0])) endpoint = profile.Curve.From;
            else if (at == ParameterQuantum && ReferenceEquals(acquired[0], samples[^1])) endpoint = profile.Curve.To;
            else return false;
        }
        participant = new(profile, at, point, endpoint, (Relation)row); return true;
    }
    private static Relation RayWitness(Frame frame, Policy policy, Profile profile, long at, long toward)
    {
        if (at < 0 || at > ParameterQuantum || toward is not (0 or ParameterQuantum)) throw new InvalidDataException("Ray parameter lies outside its profile.");
        var span = Relation.Segment(policy.Parameter, at, toward); var p = CurvePoint(profile.Curve.Controls, (double)at / ParameterQuantum);
        var direction = Tangent(profile.Curve.Controls, at, toward);
        return ReadWitness.Create(new Carrier(), profile.Witness, span,
            new Relation(Position(frame.X, frame.Y, p), Position(frame.X, frame.Y, direction)), profile.Witness, profile.Witness);
    }
    private static bool ReadRay(IElement row, Frame frame, Policy policy, Dictionary<IElement, Profile> profiles, out Ray ray)
    {
        ray = null!;
        if (!Witness(row, out var selected, out var basis, out var result, out var residue, out var provenance) || !profiles.TryGetValue(selected, out var profile) ||
            basis is not Relation { Left: Relation { Left: CarrierLocation at, Right: Carrier a }, Right: Relation { Left: CarrierLocation toward, Right: Carrier b } } ||
            !ReferenceEquals(a, policy.Parameter) || !ReferenceEquals(b, a) || !ReferenceEquals(at.Carrier, a) || !ReferenceEquals(toward.Carrier, a) ||
            at.Ticks < 0 || at.Ticks > ParameterQuantum || toward.Ticks is not (0 or ParameterQuantum) ||
            !ReferenceEquals(residue, selected) || !ReferenceEquals(provenance, selected) || result is not Relation pair ||
            !TryPosition(pair.Left, frame, out var point) || !TryPosition(pair.Right, frame, out var direction) ||
            point != CurvePoint(profile.Curve.Controls, (double)at.Ticks / ParameterQuantum) || direction != Tangent(profile.Curve.Controls, at.Ticks, toward.Ticks)) return false;
        ray = new(profile, at.Ticks, toward.Ticks, point, direction, at.Ticks == 0 ? profile.Curve.From : at.Ticks == ParameterQuantum ? profile.Curve.To : null, (Relation)row); return true;
    }
    private static IElement[] PairWitnesses(Frame frame, Policy policy, Profile[] profiles, Relation[] rays)
    {
        var map = new Dictionary<IElement, Profile>(ReferenceEqualityComparer.Instance); foreach (var profile in profiles) map[profile.Witness] = profile;
        var reads = rays.Select(ray => { ReadRay(ray, frame, policy, map, out var r); return r; }).ToArray();
        var pairs = new List<IElement>();
        for (var i = 0; i < rays.Length; i++) for (var j = i + 1; j < rays.Length; j++)
        {
            var selected = new Relation(rays[i], rays[j]); var angle = Angle(reads[i].Direction, reads[j].Direction);
            pairs.Add(ReadWitness.Create(new Carrier(), selected, policy.Surface, G(Angles(angle).Select(value => (IElement)new CarrierLocation(policy.Angle, value)).ToArray()), selected, selected));
        }
        return pairs.ToArray();
    }
    private static bool ReadPairs(IElement[] pairs, Ray[] rays, Policy policy, out int opposed, out int right, out bool coincident)
    {
        opposed = right = 0; coincident = false; var index = 0;
        if (pairs.Length != rays.Length * (rays.Length - 1) / 2) return false;
        for (var i = 0; i < rays.Length; i++) for (var j = i + 1; j < rays.Length; j++)
        {
            if (!Witness(pairs[index++], out var selected, out var basis, out var result, out var residue, out var provenance) ||
                selected is not Relation pair || !ReferenceEquals(pair.Left, rays[i].Witness) || !ReferenceEquals(pair.Right, rays[j].Witness) ||
                !ReferenceEquals(basis, policy.Surface) || !ReferenceEquals(residue, selected) || !ReferenceEquals(provenance, selected) || !Fields(result, 4, out var parts)) return false;
            var expected = Angles(Angle(rays[i].Direction, rays[j].Direction));
            for (var k = 0; k < parts.Length; k++) if (!Number(parts[k], policy.Angle, out var value) || value != expected[k]) return false;
            if (expected[1] <= policy.AngleAllowance) opposed++;
            if (expected[2] <= policy.AngleAllowance) right++;
            if (expected[3] <= policy.CoincidentAllowance) coincident = true;
        }
        return true;
    }

    private static Relation Reflection(Frame frame, Point a, Point b)
    {
        var coefficients = ReflectionMatrix(a, b); var grade = new Carrier();
        // Ordered affine rows: coefficients act on actual calibrated X/Y/one
        // participants. Axis and denominator remain connected; no operation tag.
        var one = new Carrier(); var input = G(frame.X, frame.Y, new CarrierLocation(one, 1));
        var rows = new[] { G(coefficients.Take(3).Select(n => (IElement)new CarrierLocation(grade, n)).ToArray()),
            G(coefficients.Skip(3).Take(3).Select(n => (IElement)new CarrierLocation(grade, n)).ToArray()) };
        return G(new Relation(Position(frame.X, frame.Y, a), Position(frame.X, frame.Y, b)), input,
            new Relation(G(rows), Relation.Segment(grade, 0, coefficients[6])));
    }
    private static bool ReadReflection(Relation operation, Frame frame, out Point a, out Point b)
    {
        a = b = default;
        if (!Fields(operation, 3, out var f) || f[0] is not Relation axis || !TryPosition(axis.Left, frame, out a) || !TryPosition(axis.Right, frame, out b) || a == b || !Bounded(a) || !Bounded(b) ||
            !Fields(f[1], 3, out var input) || !ReferenceEquals(input[0], frame.X) || !ReferenceEquals(input[1], frame.Y) || input[2] is not CarrierLocation { Ticks: 1 } ||
            f[2] is not Relation { Left: var matrix, Right: Relation divisor } || !Fields(matrix, 2, out var rows) ||
            !Axis(divisor, out var grade, out var denominator) || denominator <= 0) return false;
        var expected = ReflectionMatrix(a, b); if (expected[6] != denominator) return false;
        for (var r = 0; r < 2; r++)
        { if (!Fields(rows[r], 3, out var coefficients)) return false;
            for (var c = 0; c < 3; c++) if (!Number(coefficients[c], grade, out var value) || value != expected[r * 3 + c]) return false; }
        return true;
    }
    private static long[] ReflectionMatrix(Point a, Point b)
    {
        BigInteger dx = (BigInteger)b.X - a.X, dy = (BigInteger)b.Y - a.Y;
        BigInteger[] matrix = [dx * dx - dy * dy, 2 * dx * dy, 2 * (dy * dy * a.X - dx * dy * a.Y),
            2 * dx * dy, dy * dy - dx * dx, 2 * (dx * dx * a.Y - dx * dy * a.X), dx * dx + dy * dy];
        var gcd = matrix.Aggregate(BigInteger.Zero, (value, next) => BigInteger.GreatestCommonDivisor(value, BigInteger.Abs(next)));
        return matrix.Select(value => checked((long)(value / gcd))).ToArray();
    }
    private static long ReflectionResidual(Profile source, Profile target, Point a, Point b, bool reversed)
    {
        var m = ReflectionMatrix(a, b); double maximum = 0;
        for (var i = 0; i <= 8; i++)
        { var p = CurvePoint(source.Curve.Controls, (double)i / 8); var q = CurvePoint(target.Curve.Controls, reversed ? 1 - (double)i / 8 : (double)i / 8);
            var x = ((double)m[0] * p.X + (double)m[1] * p.Y + m[2]) / m[6];
            var y = ((double)m[3] * p.X + (double)m[4] * p.Y + m[5]) / m[6];
            maximum = Math.Max(maximum, Hypot(x - q.X, y - q.Y)); }
        return Quantize(maximum, 1);
    }
    private static bool Orientation(Relation order, Profile profile, out bool reversed)
    { reversed = ReferenceEquals(order.Left, profile.Curve.To) && ReferenceEquals(order.Right, profile.Curve.From);
        return reversed || ReferenceEquals(order.Left, profile.Curve.From) && ReferenceEquals(order.Right, profile.Curve.To); }
    private static bool TryPolicy(Relation surface, Frame frame, out Policy policy)
    {
        policy = null!;
        if (!Fields(surface, 9, out var p) || !Axis(p[0], out var angle, out var turn) || turn != 360 * AngleQuantum ||
            !Number(p[1], angle, out var tolerance) || tolerance < 0 || tolerance >= 90 * AngleQuantum ||
            !Number(p[2], frame.X, out var proximity) || proximity < 0 || !Number(p[3], frame.X, out var linearity) || linearity < 0 ||
            !Number(p[4], frame.X, out var reflection) || reflection < 0 || !Number(p[5], frame.X, out var repetition) || repetition < 0 ||
            !Number(p[6], frame.X, out var reach) || reach < 0 || !Axis(p[7], out var parameter, out var extent) || extent != ParameterQuantum ||
            !Number(p[8], angle, out var coincident) || coincident < 0 || coincident > tolerance ||
            ReferenceEquals(angle, parameter) || ReferenceEquals(angle, frame.X) || ReferenceEquals(parameter, frame.X)) return false;
        policy = new(angle, parameter, tolerance, proximity, linearity, reflection, repetition, reach, coincident, surface); return true;
    }
    private static (long H, long V, long Bend) LineMeasures(Profile profile)
    {
        var controls = profile.Curve.Controls; var a = controls[0]; var b = controls[^1];
        var h = Quantize(Math.Atan2(Math.Abs((double)b.Y - a.Y), Math.Abs((double)b.X - a.X)) * 180 / Math.PI, AngleQuantum);
        var bend = Enumerable.Range(0, 33).Select(i => SegmentDistance(CurvePoint(controls, (double)i / 32), a, b)).Max();
        return (h, 90 * AngleQuantum - h, Quantize(bend, 1));
    }
    private static Point CurvePoint(Point[] controls, double parameter)
    {
        var points = controls.Select(p => ((double)p.X, (double)p.Y)).ToArray();
        for (var n = points.Length - 1; n > 0; n--) for (var i = 0; i < n; i++)
            points[i] = ((1 - parameter) * points[i].Item1 + parameter * points[i + 1].Item1, (1 - parameter) * points[i].Item2 + parameter * points[i + 1].Item2);
        return new(Quantize(points[0].Item1, 1), Quantize(points[0].Item2, 1));
    }
    private static Point Tangent(Point[] controls, long at, long toward)
    {
        if (at == toward) return default;
        var vectors = controls.Zip(controls.Skip(1), (a, b) => ((BigInteger)b.X - a.X, (BigInteger)b.Y - a.Y)).ToArray();
        for (var n = vectors.Length - 1; n > 0; n--) for (var i = 0; i < n; i++)
            vectors[i] = ((ParameterQuantum - at) * vectors[i].Item1 + at * vectors[i + 1].Item1,
                          (ParameterQuantum - at) * vectors[i].Item2 + at * vectors[i + 1].Item2);
        var sign = toward > at ? 1 : -1; return new(checked((long)(sign * vectors[0].Item1)), checked((long)(sign * vectors[0].Item2)));
    }
    private static long Angle(Point a, Point b)
    { if (a == default || b == default) return -1;
        var cross = (BigInteger)a.X * b.Y - (BigInteger)a.Y * b.X; var dot = (BigInteger)a.X * b.X + (BigInteger)a.Y * b.Y;
        return Quantize(Math.Atan2(Math.Abs((double)cross), (double)dot) * 180 / Math.PI, AngleQuantum); }
    private static long[] Angles(long angle) => angle < 0 ? [-1, long.MaxValue, long.MaxValue, long.MaxValue] : [angle, 180 * AngleQuantum - angle, Math.Abs(90 * AngleQuantum - angle), angle];
    private static long Diameter(Point[] points)
    { long maximum = 0; for (var i = 0; i < points.Length; i++) for (var j = i + 1; j < points.Length; j++) maximum = Math.Max(maximum, Distance(points[i], points[j])); return maximum; }
    private static bool DisjointOpposed(IReadOnlyList<Ray> rays, Policy policy)
    {
        bool Opposed(int i, int j) => Angles(Angle(rays[i].Direction, rays[j].Direction))[1] <= policy.AngleAllowance;
        return rays.Count == 4 && (Opposed(0, 1) && Opposed(2, 3) || Opposed(0, 2) && Opposed(1, 3) || Opposed(0, 3) && Opposed(1, 2));
    }
    private static bool CycleSupported(Profile[] profiles, Policy policy)
    {
        if (profiles.Length == 0 || profiles.Skip(1).Where((p, i) => !ReferenceEquals(profiles[i].Curve.To, p.Curve.From)).Any()) return false;
        var points = profiles.SelectMany(p => p.Curve.Controls).ToArray();
        return Math.Min(points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y)) > policy.Linearity &&
            Math.Max(points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y)) >= policy.MinimumReach;
    }
    private static double SegmentDistance(Point p, Point a, Point b)
    { double dx = (double)b.X - a.X, dy = (double)b.Y - a.Y; var square = dx * dx + dy * dy;
        var t = square == 0 ? 0 : Math.Clamp((((double)p.X - a.X) * dx + ((double)p.Y - a.Y) * dy) / square, 0, 1);
        return Hypot(p.X - a.X - t * dx, p.Y - a.Y - t * dy); }
    private static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);
    private static bool Bounded(Point p) => p.X >= -(1 << 24) && p.X <= (1 << 24) && p.Y >= -(1 << 24) && p.Y <= (1 << 24);
    private static long Distance(Point a, Point b) => Quantize(Hypot((double)a.X - b.X, (double)a.Y - b.Y), 1);
    private static Point PointOf(JsonElement value) { var p = value.EnumerateArray().Select(v => Quantize(v.GetDouble(), CoordinateQuantum)).ToArray(); return p.Length == 2 ? new(p[0], p[1]) : throw new InvalidDataException("A point needs two coordinates."); }
    private static long Quantize(double value, long quantum) => double.IsFinite(value) ? checked((long)Math.Round(value * quantum, MidpointRounding.ToEven)) : throw new InvalidDataException("Finite observed metric required.");
    private static bool Number(IElement element, Carrier carrier, out long value) { value = 0; if (element is not CarrierLocation location || !ReferenceEquals(location.Carrier, carrier)) return false; value = location.Ticks; return true; }
    private static bool Witness(IElement element, out IElement selected, out IElement basis, out IElement result, out IElement residue, out IElement provenance)
    { selected = basis = result = residue = provenance = null!; return element is Relation row && Fields(row, 5, out _) && ReadWitness.TryRead(row, out selected, out basis, out result, out residue, out provenance); }
    private static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private static bool Elements(IElement element, out IElement[] fields) { if (element is Relation row && row.TryReadOrderedGroup(out fields)) return true; fields = []; return false; }
    private static bool Fields(IElement element, int count, out IElement[] fields) => Elements(element, out fields) && fields.Length == count;
    private static IElement[] Entries(IElement element) => Elements(element, out var fields) ? fields : throw new InvalidDataException("Expected ordered native scope.");
}
