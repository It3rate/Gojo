# Core2, Core4 and current Gojo: retained capabilities and gaps

Review date: September 10, 2026. Current Gojo baseline: `c29fbf6`, including
the joined-offset path construction. Historical source checkouts inspected:
Core4 at `c91c7d6`, and ResoEngine/Core2 at `d14f058`. These are source and
test comparisons, not a claim that the archived binaries were rebuilt or that
all historical tests were rerun. Local checkout additions around deployment
are not treated as changes to the historical mathematical implementations.
Historical source links assume Core4 and ResoEngine remain beside the Gojo
checkout; they are reference material, not current project dependencies.

**September 11 follow-up:** the [complex interpretation work](GojoComplexInterpretations-2026-09.md)
now restores selected complex coefficient arithmetic through opposed-face and
contour constructions, exact square-root alternatives and partial fractional-
power equations. The gap descriptions below record the September 10 baseline;
general complex root families, analytic branch tracking and split-complex
arithmetic remain outside the new implementation.

## Main assessment

Gojo retains much of the early primitive intent, but it is not a feature-complete
replacement for every old demo. Three kinds of change are easy to confuse:

1. **Re-expressed:** the capability exists, but a different connected structure
   now carries it. Points, directed segments, units, projection and partial
   constructions mostly belong here.
2. **Implemented under a narrower or older contract:** useful behavior exists,
   but does not yet meet every law of the new construction work. Several
   GojoCAD area and rounding helpers belong here.
3. **Historical capability without a complete current equivalent:** the older
   implementation still exists, but its behavior has not been rebuilt in Gojo.
   Trolley navigation, the letter-formation system, complex fractional-power
   branch selection and resistive-network relaxation are significant examples.

The narrower syntax demonstrations should not be mistaken for the entire
Gojo library. Conversely, a name or shape in Blocks does not establish that
its construction or dynamics have been implemented.

## Which older demos are we comparing?

The [archived website](https://gojo.inklingai.ca/core4/) contains three engines:
Core4 Interactive Probe, Core2 Pattern Visualizer, and the separate Foom Numbers
tour. Foom is earlier related work, not another name for Core2.

The Core2 desktop [page registration](../../../ResoEngine/Visualizer.WinForms.Core2/MainForm.cs)
lists 20 pages: pinning and shared carriers, resolution, symbolic operations,
boundary continuation, friezes, booleans, fractional powers, units and letter
formation. Its [browser registration](../../../ResoEngine/Visualizer.Core2.Browser/Program.cs)
includes only six: Letter A Goal Pins, Letter Formation Dynamics, Resolution
Modes, Boolean Operations, Orthogonal Boolean and Parallel Boolean. Absence
from that browser menu is therefore not evidence that a desktop experiment
was deleted.

Core4 has a separate [primitive example collection](../../../Core4/Core4.Examples/PrimitiveBasisExamples.cs),
[site examples](../../../Core4/Core4.Examples/SiteExamples.cs), and runtime
experiments in topology, calibration and circuits. The current Gojo solution
does not reference those projects. The archive preserves access to them;
it does not make them current Gojo behavior.

## Primitive and construction correspondence

| Earlier family / demo | Current Gojo counterpart | Assessment and remaining difference |
| --- | --- | --- |
| Scalar and Proportion; dominant amount over recessive support | Signed carrier locations, explicit spans/calibrations, projection residue and quantity quotient reads | **Re-expressed.** Native carrier state is integral; exact rational observations retain their basis. A denominator no longer doubles as a universal encoding of resolution, orientation and missing information. |
| Axis / directed segment | `S(C,a,b)`, opposed-face directed segments, endpoint identity and selected face-product reads | **Retained with changed semantics.** Ordered endpoints remain fundamental. Negative coordinates, opposed flow and reversed traversal are separate operations. |
| Pinning Axis / Axis Pinning Geometry / Shared Carrier Shapes | Shared `TickRef` sites, point/segment relations, ports, explicit contact and correspondence | **Structural capability retained; old explorer not ported.** A pin can be represented without recreating the old pin-mode taxonomy. Topology is not inferred from equal coordinates. |
| Orthogonal Axes / Core4 Primitive Bases | Orthogonal outlets, unit-shape products, directed-face participation, selected affine and metric bases | **Re-expressed, not a literal six-form migration.** Orthogonal participation is contextual. The old unit-sign rules that selected a carrier rank do not define the current metric. |
| Area, anti-area and expanded quadrants | Product units, fill/anti-fill participation, boundary and lane surfaces, graphic area structures | **Retained in several separate reads.** Dimensional product, occupied area, orientation and rendered region are no longer one obligatory `Area` wrapper or one fixed grade ladder. |
| Circle / radial carrier / geometry at a pin | Point-derived circular unit possibility; reach, radius attachment, actual seam, subdivision and metric-quarter construction | **More explicit construction.** An angle convention and rendered grid need not be assumed at the start. Arbitrary intrinsic fractions still lack a complete exact metric embedding. |
| Frozen basis and live attachment | Stable `UnitShape`, crystallization, source/read surfaces, shared-site ports and explicit live outputs | **Retained and separated.** Stable tick meaning and a live sampled value are different. Reading a source does not silently move material. |
| Resolution Modes | Resolution/subdivision unit shapes, calibrated refinement reads, residue and explicit material refinement | **Retained with stronger accounting.** The former rounding-rule menu is not a complete one-for-one API migration. Current projection and enclosure reads state their rounding and remainder contracts separately. |
| Units / Quantity | Ordered product and quotient unit structure, explicit dimensional matching, selected standards and calibrations | **Retained and expanded.** Repetition and authored order survive an explicit commutative dimensional read. Compatible dimensions do not imply compatible scale or observation identity. |
| Parallel and Orthogonal Boolean galleries / containment | Scoped span support, overlap, exclusive and uncovered reads; lane occupancy and participation witnesses; CAD area settlement | **Retained in selected scopes.** Exact span reads and approximate planar geometry coexist. This is not one universal Boolean/solid kernel. |
| Boundary Repetition / Landmark Continuation | Carrier wrap/clamp and routes; repeat/controller/timeline surfaces; explicit contour traversal | **Partly re-expressed.** A generic old bounce/continuation option does not automatically have a corresponding current propagation law. |
| Frieze Gallery / Pattern Editor / Pinning Editor / Square Wave Dynamics | Motif ports, affine transform composition, glide/reflection/translation, repeat structure and selected feedback | **Core construction retained; application breadth reduced.** The new fixtures check selected generators. The former fire/commit program editor and its full pattern catalog are not reproduced. |
| Fractional Power / complex and split-complex axes | Repeated integer products, explicit quotient reads, exact square-root checks and open square-constrained geometry | **Substantial gap.** No equivalent general complex/split-complex fractional-power branch API or branch-following workbench was found in the current solution. |
| Symbolic Workbench | Text syntax, partial patterns/bindings, equation surfaces, claims, transforms and replay | **Different purpose.** Gojo syntax authors the numeric graph; it does not replace the old symbolic reduction/search workbench. A parsed request does not itself choose or run its reader. |

Current evidence for these rows includes the
[core representation](GojoSystemPrompt.md),
[directed-segment tests](../../Tests/Gojo.Tests/Core/DirectedSegmentTests.cs),
[unit conformance](../../Tests/Gojo.Tests/Core/UnitAlgebraConformanceTests.cs),
[quotient reads](../../Tests/Gojo.Tests/Core/QuantityQuotientTests.cs),
[refinement tests](../../Tests/Gojo.Tests/Core/RefinementReadTests.cs),
[span reads](../Reads/RelationReads.cs),
[participation settlement](../ConceptSurface/ParticipationSettlementSurface.cs),
[graphic constructors](../../Applications/GojoCAD/GraphicStructure.cs),
[geometry syntax tests](../../Tests/Gojo.Encoding.Tests/GojoSyntaxGeometryTests.cs),
and [affine composition tests](../../Tests/Gojo.Encoding.Tests/GojoSyntaxAffineFrameTests.cs).

## Changes that affect interpretation, not just naming

### A directed pair does not imply the old complex algebra

Core2's [Axis](../../../ResoEngine/Core2/Elements/Axis.cs) defaults to a complex
two-slot algebra and can select a split-complex table. Its
[algebra table](../../../ResoEngine/Core2/Algebra/AlgebraTable.cs) specifies where
each cross-product goes. Its
[pin interpreter](../../../ResoEngine/Core2/Elements/PinAxisInterpreter.cs) also
uses the sign of a proportion's support to select carrier rank and orthogonal
breakout.

Gojo has explicitly opposed carrier-face products and selected folds, tested
as such. These should not be silently interpreted as the same complex or
split-complex algebra. A negative scalar location, the dual of a carrier, a
reversed relation and an inverse unit have distinct meanings. Reintroducing
complex arithmetic would require its own connected basis and product law,
with ordinary complex conformance tests. The shapes alone do not supply it.

The early intuition that a direction has an opposite survives. The historical
rule coupling denominator signs, dimensional lift and imaginary participation
does not survive as a universal kernel law.

### The former unresolved ratio ladder has been replaced

Core2 [Proportion.Fold](../../../ResoEngine/Core2/Elements/Proportion.cs) maps
`0/0` to its scalar zero fallback and nonzero-over-zero to a positive or negative
overflow sentinel. The original proportion still exists, but that folded read
can hide its unresolved character.

Current Gojo retains missing operands as slots, unavailable quotient results,
residue, blocked lanes or unsettled tension according to the actual cause.
Our measured offset can keep `65*s*s = 4` with a nonnegative scale domain
without inventing a zero or a floating-point exact answer. This is a substantive
improvement in how intermediate constructions remain usable.

Approximation is also no longer assumed to carry an intrinsic hard tolerance.
Nominal landmarks, allowed variation, conditional bounds, selected resolution
and learned expectations remain different structures. The
[uncertainty reference](GojoUncertaintyReference.md) describes that separation.

### A visible Boolean result may have had a narrower law even then

Core2 [Area.Union](../../../ResoEngine/Core2/Elements/Area.cs) calls `Envelope`,
whose own comment says it is an axis-wise convex envelope, not a union that
preserves separate regions. Two disjoint rectangles can consequently acquire
filled space between them under that particular helper. This does not say all
Core2 Boolean implementations behaved that way; its separate axis projection
and gallery mechanisms need their own reads.

Gojo's scoped span operations explicitly retain covered, overlapping,
exclusive and uncovered participation. Its CAD area implementation can preserve
separate loops and openings, but has a different numerical boundary discussed
below. Similar pictures should be compared by their selected laws, not by
matching the word “union.”

## Important behavior that exists in Gojo outside the new syntax fixtures

The recent statement that “caps, rounded joins and regions remain future work”
is true **for the new exact measured-offset/path contract**. It is too broad
as a statement about the whole repository.

Current [GraphicBoundaryConceptSurface](../../Applications/GojoCAD/structures/GraphicBoundaryConceptSurface.cs)
already finds cusp candidates, rounds selected line/line and line/arc corners,
and builds circular caps at selected line tips. The corresponding
[tests](../../Tests/GojoCAD.Tests/GraphicBoundaryConceptSurfaceTests.cs) exercise them.

Current [GraphicAreaSettlementSurface](../../Applications/GojoCAD/structures/GraphicAreaSettlementSurface.cs)
splits segment/arc boundaries, resolves positive and anti-area loops, preserves
holes, and assembles outlines. Its
[tests](../../Tests/GojoCAD.Tests/GraphicAreaSettlementSurfaceTests.cs) include separated
composites, rings, crescents, overlapping boundaries and rounded H-like forms.
These are executable behaviors, not only labeled pictures.

However, this code also uses `double`, trigonometry and fixed constants such as
`Epsilon = 0.01`, `SampleDistance = 0.55` and
`EndpointSettlementDistance = 1.5`. The line/line rounding helper limits trim
distance to 45% of the shorter incident length, computes an effective radius
and rounds output coordinates. These are useful implementation choices, but
they do not amount to an exposed, scale-aware uncertainty or admissibility law.
The presence of structural result and participation witnesses does not by
itself make every internal numerical choice an exact construction.

This is an **integration and conformance gap within current Gojo**, not a lost
Core2 feature. The useful next move is to reconnect those existing cases to
explicit metric, support, resolution, correspondence and rejected-candidate
structure. Reimplementing them as if no prior code existed would waste work.

Similarly, `GraphicStructure` can retain transition/clothoid and
arc/unwound-length involute recipes. That alone is not evidence of a general
clothoid or involute solver. The
[construction audit](GojoPrimitiveConstructionAudit-2026-09.md) distinguishes
supplied geometry, recipes and calculated results.

## Historical dynamics without complete current replacements

| Earlier working experiment | What the old source actually contains | Current position |
| --- | --- | --- |
| Core2 letter formation | Goals/desires, tension evaluation, per-site proposals, joined-site resolution, momentum and jitter in an iterative stepper | General slots, claims, transforms and local calibration now exist, but the letter system itself was not ported. |
| Core4 connected topology / trolley | T/Y/H junctions, triangle closure, terminal reflection, continuation selection from local basis agreement, partial reflection and diamond-merge behavior | Gojo has shared topology and selected traversal/route reads. No current equivalent covers this entire carried-calibration traversal suite. |
| Core4 resistive circuit | Connected node grouping, fixed potentials, resistance-weighted local relaxation and current/drop observations | Gojo has routing/capacity primitives and electronics descriptive structures. Those do not yet implement the same network physics. |
| Core4 PatternState / Calibrater | Rolling three-sample windows, deltas, ratios, reversal history, smoothing and fit pressures | Current Gojo has prospective affine forecasts and later assessment. Broader pattern-family discovery remains a documented plan. |
| Earlier optimized backends | Separate Core4-era kernel/SIMD and demo projects | They are not current solution targets or evidence that the new syntax already compiles into those backends. |

The [letter stepper](../../../ResoEngine/Applied/Geometry/LetterFormation/LetterFormationStepper.cs)
is real application behavior. It also contains explicit proposal types,
numeric response constants and random jitter. It is evidence of a useful
experiment, not evidence that general letter recognition or adaptive local
intelligence had already emerged from the old primitive kernel.

The [trolley tests](../../../Core4/Core4.Tests/Topology/TopologyTrolleyTests.cs)
and [topology tests](../../../Core4/Core4.Tests/Topology/ConnectedSiteTopologyTests.cs)
are particularly valuable recovery material: they specify concrete situations,
not just aspirations. We should preserve the problems and useful expected
behavior while rebuilding the representation.

The [resistive-circuit tests](../../../Core4/Core4.Tests/Dynamics/ResistiveCircuitTests.cs)
include a 12-to-0 potential network with unequal branches whose middle
potentials settle to 9 and 6. That is a concrete old capability. Current
[resistor value structures](../../Applications/GojoCAD/electronics/components/resistors/ResistanceValueSurface.cs)
and [ECAD documentary tests](../../Tests/GojoCAD.Tests/EcadDocumentarySurfaceTests.cs)
describe nominal values, tolerances, terminals and nets; they are not a
replacement for the relaxation procedure. Two-space calibration is also a
different law, despite the similar visual motif of neighbours balancing.

Core2's [fractional-power tests](../../../ResoEngine/Tests.Core2/FractionalPowerTests.cs)
are another clear loss of application coverage: they test `sqrt(-1)` candidates
`i` and `-i`, plus selection near a reference branch. They also explicitly defer
negative fractional exponents, so the old implementation was not complete
either. Current [integer power tests](../../Tests/Gojo.Tests/Equations/EquationPowerTests.cs)
and constrained geometric square roots do not cover those branch semantics.

For signals, the [existing plan](GojoSignalPatternLearningPlan-2026-09.md) already
records the Core4 code and its caveats. For example, its `LastSlope` is a sample
difference rather than a rate divided by elapsed time. Current
[forecast construction](../Structures/LocalCalibration.Forecast.cs) explicitly
separates source time, arrival, issuance and later assessment. That is better
experimental discipline, while still covering fewer signal-model families.

## What I would recover next

The strongest direction is a capability ledger attached to concrete fixtures,
not a promise that every old class will return. For each family retain its old
example, current structural equivalent, known false reads and remaining law.
This review is the first version of that ledger.

For the current construction work, I would prioritize:

1. **Bring the existing CAD corner and region helpers into the audit.** Start
   with a rounded corner whose requested radius cannot fit, and two boundaries
   that are close but not the same site. Expose the adjustment or refusal,
   rather than leaving it inside fixed thresholds.
2. **Recover a small topology sequence from the old tests:** shared T junction,
   terminal reflection, then a loop/merge. Distinguish geometric contact,
   permissible traversal and conserved transfer in each fixture.
3. **Keep the larger deferred capabilities explicit:** inverse/root alternatives,
   letter-like goal formation, a passive resistor network and signal-family
   discovery. Each needs its own selected law and comparison experiment before
   claiming migration parity.

There is no need to abandon the recent primitive work. Its stronger treatment
of identity, construction evidence, mathematical conformance and partial
results gives us a better basis for recovering those capabilities. But it is
accurate to say that the older demos explored a broader application surface
than current Gojo has fully re-established.

## Verification boundary

The current solution passes 1,617 tests at this checkpoint. Publication checks
also exercise all 18 indexed syntax examples and seven full walkthroughs,
including copied source, page assets, desktop/phone layouts and step links.
Historical implementations and tests were inspected, not executed in this
review. No mathematical implementation was changed as part of the comparison.
The three newly published construction pages present traces already checked
against current Gojo tests; their browsers do not execute the kernel.
