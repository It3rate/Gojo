using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Builds small, human-facing JSON fixture entries from one live Gojo structure.
/// The fixture projection is intentionally lossy: it keeps only local reads
/// that help us understand what the structure claims to know. The full
/// diagnostic relation-shaped snapshot surface is still available from
/// DiagnosticEntrySurfaceFromStructure.
/// Each call reads exactly one source structure, not a glossary page or bundle
/// of sibling examples.
/// </summary>
public static class GojoStructureEncodingFixtureProjection
{
    public readonly record struct Source(
        string EntryId,
        string Represents,
        string Expression,
        string? EncodingNote,
        IElement Structure);

    public static GojoEncodingJsonFixtureEntry EntryFromStructure(Source source) =>
        new()
        {
            Represents = source.Represents,
            EntryId = source.EntryId,
            Expression = source.Expression,
            EncodingNote = string.IsNullOrEmpty(source.EncodingNote) ? null : source.EncodingNote,
            Gojo = SparseGojoProjection(source)
        };

    public static Relation DiagnosticEntrySurfaceFromStructure(Source source)
    {
        var raw = GojoStructureEncodingSurface.CreateDiagnosticSnapshot(new Carrier(), source.EntryId, source.Structure);
        if (!GojoStructureEncodingSurface.TryRead(raw, out var read))
        {
            throw new InvalidOperationException("Could not read generated Gojo diagnostic snapshot surface.");
        }

        return GojoStructureEncodingBundleSurface.Entry(
            new Carrier(),
            source.EntryId,
            source.Expression,
            source.EncodingNote,
            raw,
            [
                OpenedStructurePath(source, raw, read),
                FirstRelationRowPath(source, raw, read)
            ]);
    }

    private static GojoEncodingJsonFixtureGojo SparseGojoProjection(Source source)
    {
        var read = SparseRead.From(source);
        return new GojoEncodingJsonFixtureGojo
        {
            Known = NullIfEmpty(read.Known),
            Paths = NullIfEmpty(read.Paths),
            Tension = NullIfEmpty(read.Tension),
            Definitions = NullIfEmpty(read.Definitions)
        };
    }

    private static Relation OpenedStructurePath(
        Source source,
        Relation rawEncoding,
        GojoStructureEncodingSurface.StructureRead read) =>
        GojoObserverPathSurface.Create(
            new Carrier(),
            rawEncoding,
            GojoObserverPathSurface.ObserverSurface(
                new Carrier(),
                rawEncoding,
                Label("opened glossary block")),
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(
                    new Carrier(),
                    rawEncoding,
                    Label("opened structure"),
                    read.OpenedStructure,
                    source.Structure)),
            read.OpenedStructure,
            provenance: Label(source.Expression));

    private static Relation FirstRelationRowPath(
        Source source,
        Relation rawEncoding,
        GojoStructureEncodingSurface.StructureRead read)
    {
        var row = read.Relations.Length == 0
            ? read.OpenedStructure
            : read.Relations[0];

        return GojoObserverPathSurface.Create(
            new Carrier(),
            rawEncoding,
            GojoObserverPathSurface.ObserverSurface(
                new Carrier(),
                rawEncoding,
                Label("relation table scan")),
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(
                    new Carrier(),
                    rawEncoding,
                    Label("first relation row"),
                    row,
                    read.OpenedStructure)),
            row,
            provenance: Label(source.EntryId));
    }

    private static Relation Label(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);

    private static List<T>? NullIfEmpty<T>(IReadOnlyCollection<T> values) =>
        values.Count == 0 ? null : [.. values];

    private sealed class SparseRead
    {
        private readonly HashSet<IElement> seen = new(ReferenceEqualityComparer.Instance);
        private readonly SortedSet<string> known = new(StringComparer.Ordinal);
        private readonly SortedSet<string> paths = new(StringComparer.Ordinal);
        private readonly SortedSet<string> tension = new(StringComparer.Ordinal);
        private readonly List<GojoEncodingJsonFixtureDefinition> definitions = [];
        private readonly HashSet<string> definitionNames = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Known => known;

        public IReadOnlyCollection<string> Paths => paths;

        public IReadOnlyCollection<string> Tension => tension;

        public IReadOnlyCollection<GojoEncodingJsonFixtureDefinition> Definitions => definitions;

        private static readonly HashSet<string> DefinitionSampleIds = new(StringComparer.Ordinal)
        {
            "site-flow-boundary-site",
            "site-flow-boundary-radial-flow",
            "site-flow-boundary-boundary-support",
            "boolean-offset-offset-reads",
            "boolean-offset-union-hole"
        };

        public static SparseRead From(Source source)
        {
            var read = new SparseRead();
            read.Visit(source.Structure);
            read.ReadExpressionHints(source.Expression, source.EncodingNote);
            read.ApplyLocalParticipationPrinciple(source);
            if (read.known.Count == 0)
            {
                read.known.Add("structure");
            }

            read.AddDefinitionSamples(source);
            return read;
        }

        private void AddDefinitionSamples(Source source)
        {
            if (!DefinitionSampleIds.Contains(source.EntryId))
            {
                return;
            }

            var positionBody = Relation.Point(new Carrier(), 0);
            var spanBody = Relation.Segment(new Carrier(), 0, 1);

            Relation? position = null;
            Relation? span = null;
            Relation? circle = null;
            Relation? boundary = null;
            Relation? offset = null;
            Relation? selection = null;

            if (known.Contains("position") ||
                known.Contains("circle-boundary") ||
                source.EntryId is "site-flow-boundary-site" or "site-flow-boundary-radial-flow" or "site-flow-boundary-boundary-support")
            {
                position = AddDefinition("position", positionBody);
            }

            if (known.Contains("span") ||
                source.EntryId is "boolean-offset-offset-reads" or "boolean-offset-union-hole")
            {
                span = AddDefinition("span", spanBody);
            }

            if (known.Contains("flow") ||
                source.EntryId == "site-flow-boundary-radial-flow")
            {
                AddDefinition(
                    "flow",
                    Relation.OrthogonalOutlet(positionBody),
                    position is null ? [] : [position],
                    residueOrTension: Text("flow shape depends on available dimension"));
            }

            if (source.EntryId == "site-flow-boundary-boundary-support")
            {
                var radius = AddDefinition(
                    "radius-evidence",
                    Address("R", Atom("position"), Atom("radius-participant")),
                    position is null ? [] : [position]);
                circle = AddDefinition(
                    "circle-boundary",
                    Address("C", Atom("position"), Atom("radius-evidence")),
                    [position ?? ConceptAddressSurface.Atom(new Carrier(), "position"), radius]);
                boundary = AddDefinition(
                    "boundary",
                    Address("BdSelf", Atom("circle-boundary")),
                    [circle],
                    residueOrTension: Text("inside/outside support not selected"));
                AddDefinition(
                    "area-support",
                    Address("Area+", Atom("boundary"), Atom("side?")),
                    [boundary],
                    residueOrTension: Text("area side/lane not fully selected"));
            }

            if (source.EntryId == "boolean-offset-offset-reads")
            {
                boundary = AddDefinition(
                    "boundary",
                    Address("Bd0", Atom("span")),
                    span is null ? [] : [span]);
                AddDefinition(
                    "offset-read",
                    Address("Off", Atom("span"), Atom("O(span)"), Atom("+d|-d"), Atom("Res(n)")),
                    [span ?? ConceptAddressSurface.Atom(new Carrier(), "span"), boundary],
                    residueOrTension: Text("offset lane compatibility unresolved"));
            }

            if (source.EntryId == "boolean-offset-union-hole")
            {
                boundary = AddDefinition(
                    "boundary",
                    Address("Bd", Atom("closed-path")),
                    span is null ? [] : [span]);
                offset = AddDefinition(
                    "offset-read",
                    Address("Off", Atom("span"), Atom("O(span)"), Atom("d"), Atom("Res(n)")),
                    [span ?? ConceptAddressSurface.Atom(new Carrier(), "span"), boundary]);
                selection = AddDefinition(
                    "selection",
                    Address("Sel", Atom("offset-read"), Atom("Focus(boundary)")),
                    [offset],
                    residueOrTension: Text("selection stop rule not encoded"));
                var area = AddDefinition(
                    "area-support",
                    Address("Area+", Atom("boundary")),
                    [boundary, selection]);
                AddDefinition(
                    "anti-area-support",
                    Address("Area-", Atom("boundary"), Atom("inverse-or-opposed-participation")),
                    [area],
                    residueOrTension: Text("anti-area participates as a hole, not an eraser"));
            }
        }

        private Relation AddDefinition(
            string name,
            IElement body,
            IReadOnlyList<IElement>? dependencies = null,
            IElement? provenance = null,
            IElement? residueOrTension = null)
        {
            var definition = ConceptDefinitionSurface.Create(
                new Carrier(),
                name,
                body,
                dependencies,
                provenance,
                residueOrTension);

            if (definitionNames.Add(name))
            {
                definitions.Add(DefinitionJson(definition));
            }

            return definition;
        }

        private static GojoEncodingJsonFixtureDefinition DefinitionJson(Relation definition)
        {
            if (!ConceptDefinitionSurface.TryRead(definition, out var read))
            {
                throw new InvalidOperationException("Could not read generated concept definition.");
            }

            var name = ElementDebug(read.Name);
            var body = ElementDebug(read.Body);
            var provenance = ElementDebug(read.Provenance);
            var dependencies = read.Dependencies.Select(ElementDebug).ToList();
            var residue = EncodedTextSurface.IsUnknownPoint(read.ResidueOrTension)
                ? null
                : ElementDebug(read.ResidueOrTension);
            var dependencyText = dependencies.Count == 0
                ? string.Empty
                : $"; deps={string.Join(",", dependencies)}";
            var residueText = residue is null
                ? string.Empty
                : $"; residue={residue}";

            return new GojoEncodingJsonFixtureDefinition
            {
                Name = name,
                Body = body,
                DependsOn = dependencies.Count == 0 ? null : dependencies,
                Provenance = provenance == body ? null : provenance,
                ResidueOrTension = residue,
                Encoding = $"Def({name},{body}{dependencyText})+Prov({provenance}){residueText}"
            };
        }

        private static Relation Address(string lexical, params IElement[] parts) =>
            ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

        private static Relation Atom(string name) =>
            ConceptAddressSurface.Atom(new Carrier(), name);

        private static Relation Text(string text) =>
            EncodedTextSurface.Create(new Carrier(), text);

        private static string ElementDebug(IElement element)
        {
            if (EncodedTextSurface.IsUnknownPoint(element))
            {
                return "?";
            }

            if (ConceptAddressSurface.TryReadAtom(element, out _, out _) ||
                ConceptAddressSurface.TryReadCompound(element, out _, out _, out _, out _))
            {
                return ConceptAddressSurface.ToDebugString(element);
            }

            if (ConceptDefinitionSurface.TryRead(element, out var definition))
            {
                return ElementDebug(definition.Name);
            }

            if (ConceptDefinitionSurface.TryReadReference(element, out var reference))
            {
                var context = EncodedTextSurface.IsUnknownPoint(reference.Context)
                    ? string.Empty
                    : $";context={ElementDebug(reference.Context)}";
                var residue = EncodedTextSurface.IsUnknownPoint(reference.ResidueOrTension)
                    ? string.Empty
                    : $";residue={ElementDebug(reference.ResidueOrTension)}";

                return $"Cr({ElementDebug(reference.DefinitionOrAddress)}{context}{residue})";
            }

            if (UnitShape.TryReadOrthogonalSource(element, out var orthogonalSource))
            {
                return $"F(O({ElementDebug(orthogonalSource)}))";
            }

            if (TryReadPoint(element, out _))
            {
                return "P(X)";
            }

            if (TryReadSegment(element, out var start, out var end))
            {
                return start.Ticks == end.Ticks
                    ? "S0(P(A@X),P(B@X))"
                    : "S(P(A@X),P(B@Y))";
            }

            if (EncodedTextSurface.TryRead(element, out var text))
            {
                return text;
            }

            if (TryReadGroup(element, out var participants))
            {
                return string.Join(" + ", participants.Select(ElementDebug));
            }

            return "E?";
        }

        private void Visit(IElement element)
        {
            if (!seen.Add(element))
            {
                return;
            }

            if (TryReadGroup(element, out var participants))
            {
                foreach (var participant in participants)
                {
                    Visit(participant);
                }

                return;
            }

            if (UnitShape.TryReadOrthogonalSource(element, out var orthogonalSource))
            {
                AddOrthogonalPath(orthogonalSource);
                return;
            }

            if (TryReadFramePoint(element, out _))
            {
                known.Add("position");
                return;
            }

            if (TryReadPoint(element, out _))
            {
                known.Add("position");
                return;
            }

            if (TryReadSegment(element, out var start, out var end))
            {
                known.Add(start.Carrier.SameIdentityAs(end.Carrier) && start.Ticks == end.Ticks
                    ? "collapsed-basis"
                    : "span");
                return;
            }

            if (element is Relation relation)
            {
                Visit(relation.Left);
                Visit(relation.Right);
            }
        }

        private void AddOrthogonalPath(IElement source)
        {
            if (TryReadFramePoint(source, out _) || TryReadPoint(source, out _))
            {
                known.Add("position");
                known.Add("flow");
                paths.Add("position --orthogonal outlet--> flow");
                tension.Add("flow shape depends on available dimension");
                return;
            }

            if (TryReadSegment(source, out var start, out var end))
            {
                if (start.Carrier.SameIdentityAs(end.Carrier) && start.Ticks == end.Ticks)
                {
                    known.Add("collapsed-basis");
                    known.Add("flow");
                    paths.Add("collapsed-basis --orthogonal outlet--> flow");
                    tension.Add("length unresolved");
                    return;
                }

                known.Add("span");
                known.Add("flow");
                paths.Add("span --orthogonal outlet--> flow");
                return;
            }

            known.Add("flow");
            paths.Add("source --orthogonal outlet--> flow");
            tension.Add("flow source unresolved");
        }

        private void ReadExpressionHints(string expression, string? note)
        {
            var text = $"{expression} {note}".ToLowerInvariant();

            if (expression.Contains("P(", StringComparison.Ordinal))
            {
                known.Add("position");
            }

            if (expression.Contains("S(", StringComparison.Ordinal))
            {
                known.Add("span");
            }

            if (expression.Contains("Circle", StringComparison.Ordinal))
            {
                known.Add("circle-boundary");
                known.Add("radius-evidence");
                paths.Add("position + radius-evidence --> circle-boundary");
            }

            if (expression.Contains("source", StringComparison.OrdinalIgnoreCase))
            {
                known.Add("source");
            }

            if (text.Contains("orthogonal-outlet") ||
                text.Contains("normal"))
            {
                known.Add("flow");
                if (known.Contains("collapsed-basis"))
                {
                    paths.Add("collapsed-basis --orthogonal outlet--> flow");
                    tension.Add("length unresolved");
                }
                else if (known.Contains("span"))
                {
                    paths.Add("span --orthogonal outlet--> flow");
                }
                else if (known.Contains("position"))
                {
                    paths.Add("position --orthogonal outlet--> flow");
                    tension.Add("flow shape depends on available dimension");
                }
                else
                {
                    paths.Add("source --orthogonal outlet--> flow");
                    tension.Add("flow source unresolved");
                }
            }

            if (text.Contains("shared-reference") ||
                text.Contains("coincident") ||
                text.Contains("a=b"))
            {
                known.Add("shared-reference");
            }

            if (expression.Contains("Tension", StringComparison.Ordinal))
            {
                tension.Add("unresolved change remains inspectable");
            }

            if (expression.Contains("residue", StringComparison.Ordinal))
            {
                paths.Add("source --requested read--> result + residue");
                tension.Add("read residue preserved");
            }

            if (expression.Contains("coarse and fine resolution", StringComparison.Ordinal))
            {
                paths.Add("span --resolution read--> coarse + fine views");
                tension.Add("detail below selected resolution remains open");
            }

            if (expression.Contains("close(", StringComparison.Ordinal))
            {
                paths.Add("open path --closure read--> closure tension");
                tension.Add("closure unresolved");
            }

            if (expression.Contains("settled read", StringComparison.Ordinal))
            {
                paths.Add("source + basis + resolution --> settled read");
            }

            if (note?.Contains("preferred direction", StringComparison.OrdinalIgnoreCase) == true)
            {
                tension.Add("no preferred direction");
            }

            AddGeneralExpressionHints(
                text,
                expression.Contains("D(", StringComparison.Ordinal));
            AddContextualExpressionForms(expression, text);
        }

        private void ApplyLocalParticipationPrinciple(Source source)
        {
            if (source.Expression.Contains("unresolved carrier potential", StringComparison.Ordinal))
            {
                known.Remove("collapsed-basis");
                known.Remove("span");
                known.Add("flow");
                known.Add("position");
                known.Add("shared-reference");

                paths.RemoveWhere(path => path.Contains("collapsed-basis", StringComparison.Ordinal));
                paths.Add("shared-reference --open basis read--> unresolved carrier potential");
                paths.Add("unresolved carrier potential --orthogonal outlet--> flow");

                tension.Add("basis orientation unresolved");
                tension.Add("length unresolved");
                tension.Add("opposed lane not asserted");
            }

            if (source.Expression.Contains("S(C,0,0)", StringComparison.Ordinal))
            {
                known.Remove("span");
            }

            if (source.Expression.Equals("P(O)+radius-evidence(R) -> Circle(O,R)", StringComparison.Ordinal))
            {
                known.Add("radius-evidence");

                paths.Add("position + radius-evidence --> circle-boundary");
                paths.Add("position + orthogonal outlet + radius-evidence --> radial terminal boundary");

                tension.Add("contour traversal not selected");
                tension.Add("inside/outside support not selected");
            }
        }

        private void AddContextualExpressionForms(string expression, string text)
        {
            var hasSpan = known.Contains("span") || expression.Contains("S(", StringComparison.Ordinal);
            var hasCircle = known.Contains("circle-boundary") || expression.Contains("Circle", StringComparison.Ordinal);
            var hasArea = known.Contains("area-support") ||
                text.Contains("areafill") ||
                text.Contains("antifill") ||
                text.Contains("area*");
            var hasAntiArea = text.Contains("anti") ||
                text.Contains("inverse") ||
                text.Contains("negative") ||
                expression.Contains("Area*", StringComparison.Ordinal);
            var hasPositiveArea = text.Contains("areafill") ||
                expression.Contains("Area(", StringComparison.Ordinal) ||
                text.Contains("positive support");
            var hasOffset = known.Contains("offset-read") || text.Contains("offsetread");
            var hasSelection = known.Contains("selection");
            var hasCurve = known.Contains("curve");
            var hasSettlement = known.Contains("constraint-or-settlement") ||
                text.Contains("settled") ||
                text.Contains("settlement") ||
                text.Contains("constraint") ||
                text.Contains("residue");
            var hasRuntimeTrace = text.Contains("feedback") ||
                text.Contains("timeline") ||
                text.Contains("at each tick") ||
                text.Contains("repeat until");
            var hasClosedOrLoopRead = text.Contains("loop") ||
                text.Contains("outline") ||
                text.Contains("closedpath") ||
                text.Contains("closed path") ||
                text.Contains("closed topology");

            if (hasSpan &&
                (known.Contains("boundary") ||
                 hasArea ||
                 hasOffset ||
                 text.Contains("pathread") ||
                 text.Contains("path carrier") ||
                 text.Contains("source carrier")))
            {
                known.Add("boundary");
                paths.Add("span --source boundary read--> Bd0(S)");
            }

            if (hasCircle)
            {
                known.Add("boundary");
                paths.Add("circle-boundary --self boundary read--> BdSelf(C)");

                if (known.Contains("flow") ||
                    text.Contains("orthogonal-outlet") ||
                    text.Contains("radial") ||
                    text.Contains("radius-evidence"))
                {
                    paths.Add("position + orthogonal outlet + radius-evidence --> radial terminal boundary");
                }
            }

            if (hasClosedOrLoopRead)
            {
                known.Add("boundary");
                paths.Add("closed path --boundary read--> Bd(Path)");
            }

            if (hasArea)
            {
                known.Add("area-support");

                if (known.Contains("boundary") || hasCircle)
                {
                    paths.Add("area-support --separator boundary read--> BdArea");
                }

                if (hasPositiveArea)
                {
                    paths.Add("compatible positive boundaries --> Area+");
                }

                if (hasAntiArea)
                {
                    known.Add("inverse-or-opposed-participation");
                    paths.Add("compatible inverse boundaries --> Area-");
                }

                if (hasPositiveArea && hasAntiArea)
                {
                    paths.Add("positive area + anti-area --> support with holes");
                }

                if (!hasPositiveArea && !hasAntiArea)
                {
                    tension.Add("area side/lane not fully selected");
                }
            }

            if (text.Contains("mixed lane") || text.Contains("mixed-lane"))
            {
                paths.Add("mixed lane boundaries --> diagonal tension");
                tension.Add("area mixed-lane read does not settle as area");
            }

            if (hasOffset)
            {
                known.Add("offset-read");
                paths.Add("source + orthogonal outlet + extent --> offset-read");

                if (hasSpan || text.Contains("pathread") || text.Contains("pathcarrier"))
                {
                    paths.Add("offset-read --terminal boundary read--> Bd1(Off)");
                }

                if (text.Contains("+d,-d") ||
                    text.Contains("+d, +d") ||
                    text.Contains("paired") ||
                    text.Contains("positive and inverse"))
                {
                    paths.Add("paired offset reads --lane compatibility read--> area or tension");
                    tension.Add("offset lane compatibility unresolved");
                }

                if (text.Contains("residue") || text.Contains("mismatch"))
                {
                    tension.Add("offset residue preserved");
                }
            }

            if (hasSelection)
            {
                paths.Add("selection --local focus read--> selected structure + residue");
                tension.Add("selection stop rule not encoded");

                if (text.Contains("cusp") || text.Contains("rounding"))
                {
                    paths.Add("selection --crystallize--> reusable selection read");
                }
            }

            if (known.Contains("crystallized-read"))
            {
                paths.Add("read --crystallize--> reusable structure");
                tension.Add("crystallization provenance must remain inspectable");
            }

            if (hasSettlement)
            {
                known.Add("constraint-or-settlement");
                paths.Add("unresolved structure --settlement read--> settled structure + residue");

            }

            if (hasRuntimeTrace)
            {
                paths.Add("runtime pass --records--> residue + tension");
                paths.Add("runtime trace --crystallize--> behavior witness");
                tension.Add("runtime/static distinction must remain explicit");
            }

            if (hasCurve)
            {
                paths.Add("curve --acceleration footing--> unresolved");
                tension.Add("curve acceleration footing unresolved");

                if (text.Contains("arc"))
                {
                    paths.Add("arc evidence --curve read--> constant curvature placeholder");
                }

                if (text.Contains("bezier"))
                {
                    paths.Add("bezier evidence --curve read--> control-handle placeholder");
                }

                if (text.Contains("clothoid"))
                {
                    paths.Add("clothoid evidence --curve read--> curvature-ramp placeholder");
                }

                if (text.Contains("tangent") || text.Contains("tangency"))
                {
                    paths.Add("tangency evidence --settlement read--> smoothing bridge");
                }
            }
        }

        private void AddGeneralExpressionHints(string text, bool hasDirectedSpan)
        {
            if (text.Contains("area"))
            {
                known.Add("area-support");
            }

            if (text.Contains("anti") || text.Contains("inverse") || text.Contains("negative"))
            {
                known.Add("inverse-or-opposed-participation");
            }

            if (text.Contains("boundary") || text.Contains("outline") || text.Contains("loop"))
            {
                known.Add("boundary");
            }

            if (text.Contains("arc") || text.Contains("curve") || text.Contains("bezier") || text.Contains("clothoid"))
            {
                known.Add("curve");
            }

            if (text.Contains("frame") || text.Contains("basis"))
            {
                known.Add("basis");
            }

            if (text.Contains("orientation") ||
                text.Contains("clockwise") ||
                text.Contains("counterclockwise") ||
                hasDirectedSpan)
            {
                known.Add("orientation");
            }

            if (text.Contains("outward"))
            {
                known.Add("flow");
                paths.Add("flow --direction read--> outward-or-positive");
            }

            if (text.Contains("inward") || text.Contains("moving into"))
            {
                known.Add("flow");
                paths.Add("flow --direction read--> inward-or-inverse");
            }

            if (text.Contains("positive"))
            {
                if (known.Contains("flow"))
                {
                    paths.Add("flow --direction read--> outward-or-positive");
                }
                else if (known.Contains("orientation"))
                {
                    paths.Add("orientation --signed direction read--> positive");
                }
                else if (known.Contains("area-support") || known.Contains("inverse-or-opposed-participation"))
                {
                    paths.Add("participation --sign read--> positive");
                }
            }

            if (text.Contains("partition") || text.Contains("tick"))
            {
                known.Add("partition");
            }

            if (text.Contains("unit"))
            {
                known.Add("unit");
            }

            if (text.Contains("repeat"))
            {
                known.Add("repeat");
            }

            if (text.Contains("constraint") || text.Contains("settle"))
            {
                known.Add("constraint-or-settlement");
            }

            if (text.Contains("topology") || text.Contains("bond") || text.Contains("join") || text.Contains("connected"))
            {
                known.Add("topological-participation");
            }

            if (text.Contains("select"))
            {
                known.Add("selection");
            }

            if (text.Contains("offset"))
            {
                known.Add("offset-read");
            }

            if (text.Contains("crystall"))
            {
                known.Add("crystallized-read");
            }

            if (text.Contains("schematic") || text.Contains("net") || text.Contains("trace") || text.Contains("layout") || text.Contains("pcb"))
            {
                known.Add("ecad-documentary-structure");
            }
        }

        private static bool TryReadGroup(IElement element, out IElement[] participants)
        {
            if (element is Relation relation &&
                relation.Right is Carrier holder &&
                relation.TryReadOrderedElementsOn(holder, out participants))
            {
                return true;
            }

            participants = [];
            return false;
        }

        private static bool TryReadFramePoint(IElement element, out CarrierLocation[] coordinates)
        {
            if (TryReadGroup(element, out var participants) &&
                participants.Length > 0 &&
                participants.All(participant => participant is CarrierLocation))
            {
                coordinates = [.. participants.Cast<CarrierLocation>()];
                return true;
            }

            coordinates = [];
            return false;
        }

        private static bool TryReadPoint(IElement element, out CarrierLocation location)
        {
            if (element is Relation point &&
                point.Left is CarrierLocation pointLocation &&
                point.Right is Carrier carrier &&
                pointLocation.Carrier.SameIdentityAs(carrier))
            {
                location = pointLocation;
                return true;
            }

            location = default;
            return false;
        }

        private static bool TryReadSegment(
            IElement element,
            out CarrierLocation start,
            out CarrierLocation end)
        {
            if (element is Relation segment &&
                TryReadPoint(segment.Left, out start) &&
                TryReadPoint(segment.Right, out end) &&
                start.Carrier.SameIdentityAs(end.Carrier))
            {
                return true;
            }

            start = default;
            end = default;
            return false;
        }
    }
}
