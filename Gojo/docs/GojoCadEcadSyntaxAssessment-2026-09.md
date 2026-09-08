# Gojo syntax for CAD and electronics: assessment and plan

Status: exploratory assessment, September 8, 2026. This is a documentation
pass, not a new CAD kernel, circuit simulator, importer or grammar revision.
The companion [signal learning plan](GojoSignalPatternLearningPlan-2026-09.md)
covers future observations, changing models and reward.

## Assessment

The existing syntax is a credible structural substrate for these domains.
Identity, ordered relations, exposed ports, exact selected arithmetic, open
slots, bounded interpretations, dependencies and retained transformations are
already useful ingredients. Bézier construction provides a concrete example of
building a recognizable composite from smaller explicit operations.

It would nevertheless be premature to call CAD or electronics representation
complete. There are four different levels of readiness:

1. **Storage:** can the graph retain the participants and their relationships?
2. **Authorship and interchange:** can a person or adapter express those
   relationships consistently and preserve source correspondence?
3. **Interpretation:** is there a specified, executable read that derives the
   intended geometry, connection, equation or uncertainty?
4. **Conformance and evolution:** does that read behave correctly at ambiguous
   boundaries, under changes, and at the required numerical resolution?

A general graph's ability to store a circle or SPICE file does not establish
the last three. The largest gaps found here concern explicit mathematical and
compositional contracts, more than missing punctuation. There is no evidence
yet that each CAD or circuit concept needs its own new primitive element type.

## Inspection baseline and existing work

Gojo was inspected at `8ddc024`. The sibling GojoEcad repository was at
`976fe2b`, with pre-existing local changes to its first-board KiCad files and
local history. Those files were left untouched. Its
[pinned dependency record](../../../GojoEcad/lib/gojo/README.md) identifies a
Gojo assembly from `7045a7e29b72c689e3ba7536a3d51e96feb19fcd`, captured August 27.
Current Gojo features must not be attributed automatically to that assembly.
Sibling links below assume the repositories are checked out beside one another.

| Evidence | Implemented foundation | Boundary |
| --- | --- | --- |
| [Text syntax](GojoTextSyntax.md), [syntax reference](../../Gojo.Encoding/Syntax/README.md), [numeric format](GojoNumericFormat.md) | Ordinary structure authored as `gojo 1`, including shared identity, slots, ordered groups, units, actions and witnesses; graph restoration exercised by fixtures. | No general CAD/SPICE parser, module system, canonical text writer or implicit execution of action names. |
| [Affine construction fixture](GojoAffineConstructionFixture.md), [AffineFrame](../Structures/AffineFrame.cs), [syntax tests](../../Gojo.Encoding.Tests/GojoSyntaxAffineFrameTests.cs) | Exact rational interpolation, coordinate differences, selected per-coordinate frame projections, partial controls and explicit Bézier construction. | Per-coordinate projection is not a general cross-axis rotation or a metric geometry kernel. |
| [GojoCAD](../../GojoCAD/README.md), [GraphicStructure](../../GojoCAD/GraphicStructure.cs), [read tests](../../GojoCAD.Tests/GraphicReadGraphTests.cs) | Sites, directed segments, shared coordinate frames, curve recipes, joins, paths and selected boundaries. | Recognition of a recipe is not conformance for every degeneracy or a complete 3D solid model. |
| [CAD structure inventory](../../GojoCAD/structures/README.md), [area settlement](../../GojoCAD/structures/GraphicAreaSettlementSurface.cs) and [tests](../../GojoCAD.Tests/GraphicAreaSettlementSurfaceTests.cs) | Substantial experiments with planar overlaps, sections, arcs, holes and area settlement. | Some calculations use doubles, rounding and fixed thresholds; not certified topology under arbitrary resolution. |
| [ECAD documentary plan](../../GojoCAD/docs/EcadDocumentaryBundle.md) and [surfaces](../../GojoCAD/structures/EcadDocumentarySurfaces.cs) | Connected text and selected documentary/net surfaces; planned artifact, dialect, validation and manufacturing relationships. | The broader document is direction, not proof that all its stages execute. |
| GojoEcad [parser/import documentation](../../../GojoEcad/docs/KiCadParser.md) and [importer](../../../GojoEcad/src/GojoEcad/Import/KiCadBoardGojoImporter.cs) | Lossless source parsing and a narrow board import: selected setup measures, layers, nets, unrotated through-hole pads, straight tracks, vias and participant groups. | Lossless parsing is much broader than interpreted geometry or circuit behavior. |
| GojoEcad [worked structures](../../../GojoEcad/src/GojoEcad/WorkedStructures/CommonValueWorkedStructures.cs) and [tests](../../../GojoEcad/tests/GojoEcad.Tests/CommonValueWorkedStructuresTests.cs) | Common value families, fused via choices, source/fine/coarse correspondence, plated-hole fit evidence and independent crystallized iterations. | Selected examples, with dependency-specific workarounds; not a general manufacturing uncertainty model. |
| GojoCAD [resistance values](../../GojoCAD/electronics/components/resistors/ResistanceValueSurface.cs) | Preferred nominal values, relative tolerance structure and a separate zero-ohm bridge concept. | These structures do not yet supply Ohm's law, temperature behavior or transient simulation. |

The [earlier geometry review](GojoSyntaxGeometryReview-2026-09.md) remains useful
history, but its missing-construction observations must be read with the later
affine fixture. Likewise, the
[ECAD projection finding](../../../GojoEcad/docs/findings/gojo/GJ-002-projection-remainder-unit-grounding.md)
concerns its pinned dependency. Current Gojo's
[projection conformance tests](../../Gojo.Tests/Core/ProjectionConformanceTests.cs)
check physical remainder in the source basis; the old finding is not evidence
that the current kernel still has that defect.

## What real formats require us to express

The official [KiCad board format](https://dev-docs.kicad.org/en/file-formats/sexpr-pcb/)
contains much more than coordinates: layers, setup, nets, footprints, tracks,
zones and groups are connected concerns. Footprint placements, pad geometry,
drilling, copper participation and zone boundaries make useful coverage cases.
This suggests separate geometry, material, connection and design-rule surfaces,
with correspondence between them.

The [schematic format](https://dev-docs.kicad.org/en/file-formats/sexpr-schematic/)
adds library definitions and instances, pins, wires, junctions, labels,
no-connect markers and hierarchical sheet/instance paths. A drawn crossing is
not sufficient connection evidence. A deliberately unconnected pin is not the
same as an unknown connection. Instance identity must survive reuse of the
same symbol or footprint definition.

| Source or representation | Useful sanity check | What a Gojo representation must retain |
| --- | --- | --- |
| KiCad board and project source | A rotated pad, a via through selected layers, a zone with a hole and an applicable clearance rule | Exact source and dialect, coordinate frames, layer/material participation, source identities, interpreted rules and unsupported remainder. |
| KiCad schematic | Two instances of a symbol, a labelled net crossing a wire, an explicit no-connect | Definition versus instance, hierarchical scope, ordered terminal correspondence, intended connections and visible unresolved cases. |
| STEP / boundary representation | A placed part with oriented edges and trimmed surfaces | Geometry versus incidence, local placements, trims, units, tolerances and assembly correspondence; only the supported interchange subset may be claimed. |
| Mesh or sampled boundary | A coarse approximation of a smooth surface | Adjacency, winding, source correspondence, approximation bounds when known and unresolved fine detail. A coarse triangle is not evidence that the source surface is planar. |
| Constructive or implicit geometry | Difference of two solids; an inside/outside field | The construction law, operands, selected metric or field basis, boundary convention and the validity of the derived boundary. |
| SPICE netlist and subcircuit | A resistor divider, then a capacitor and a changing source | Terminal order, node scope, model definition, parameters, units, initial state, source waveform and requested analysis. |

Open CASCADE's [STEP translator](https://occt3d.com/dev/doc/overview/html/occt_user_guides__step.html)
distinguishes geometry/topology/assembly translation from associated information
such as names, layers and tolerances handled through its extended document
tools. Its [modeling guide](https://occt3d.com/dev/doc/overview/html/occt_user_guides__modeling_data.html)
separates geometric objects from their topological uses. These are useful
comparisons for Gojo; interchange is not just a bag of points.

SPICE also distinguishes a device instance from a shared model and exposes
subcircuits through ordered external nodes. Its numeric spelling is
dialect-sensitive: for example, in the documented MacSpice conventions `m`
means milli and `Meg` means mega. An importer cannot safely treat a value's
suffix as a generic unit string. See the primary
[circuit-description reference](https://www.macspice.com/ug/sec2.html).

The format survey is representative, not an exhaustive STEP, KiCad or SPICE
conformance audit. Before implementing an adapter, pin an actual format version
and create coverage fixtures for that version, including unknown constructs.

## Preserve source without making source spelling the semantics

GojoEcad's lossless parser is a strong starting point. Exact source bytes,
lexemes, spans and unsupported forms can remain available even when the local
interpretation is incomplete. Keeping `1.600` and choosing an ordinary read of
`1.6` are compatible operations when their correspondence and any lost detail
are explicit. Decimal source precision is not physical manufacturing tolerance.

There is an important syntax boundary: the checked
[readable board capture](../../../GojoEcad/examples/first-board/first-board.gojo.txt)
starts with **`gojo-ecad-readable 2`**. It is a derived presentation, not a
document accepted by the current **`gojo 1`** parser. We should plan an explicit
bridge rather than imply these are already one language.

The bridge should connect source artifact and dialect, parsed occurrence,
interpreted structure, chosen read basis, remaining unsupported participation
and export correspondence. External UUIDs and text need lossless identity/text
surfaces; an import-local `$name` is not a durable external identifier. Existing
documentary text and source-byte structures provide precedents, but not yet a
single agreed text interchange contract.

Reusable composites will also benefit from named imports, definition/instance
binding, exposed interfaces and versioned references. These are authorship
needs to specify after the structural contracts stabilize. Their expansion
must leave the actual relationships present without requiring a name such as
“pad”, “circle” or “capacitor” to be the sole meaning.

## Geometry: the important missing contracts

### Frames, affine objects and metrics

The current `AffineFrame.TryProject` reads each target coordinate from one
source coordinate. This supports explicit calibration and per-coordinate scale
and offset, but does not provide a general matrix mixing coordinates. A rotated
footprint normally needs terms such as `x' = a*x + b*y + tx` and
`y' = c*x + d*y + ty`. The next contract should make these participating axes,
coefficients and offsets explicit, with domain/range and inversion conditions.

For a concrete proposed test, choose a Euclidean frame and the rotation matrix
`[[3/5, -4/5], [4/5, 3/5]]`. Placing a local pad point `(2, 1) mm` at an origin
of `(10, 20) mm` gives `(52/5, 111/5) mm`, exactly. This rotation mixes axes,
yet needs no transcendental evaluation. It isolates the composition gap before
arbitrary-angle approximation is introduced. A displacement `(2, 1) mm` under
the same rotation becomes `(2/5, 11/5) mm`; it does not acquire the origin.

Preserve the distinction between a point, a displacement and a direction.
Translation acts on a point; it does not translate the displacement between
two points. A shared length unit alone does not establish a Euclidean metric,
perpendicularity, handedness or the transformation of a normal. Dot/cross
products, determinants, distances and normals need selected laws and compatible
bases. Singular maps can have a valid lower-dimensional read without a valid
inverse. Unit products may have an explicit commutative dimensional read while
the original ordered participation remains intact.

Circles, arbitrary rotations, roots and curve length also exceed finite
rational evaluation in general. Retain the defining construction and give
approximate reads an error/refinement contract. Known rational constructions
should remain exact. A graphical normal vector and a normal probability
distribution are separate concepts, neither supplied merely by an opposed face.

### Geometry is not topology

A vertex may belong to several edge uses; an edge may have different oriented
uses on neighbouring faces. A face can be a trimmed part of an underlying
surface, with correspondence between its parameter-space trim and 3D boundary.
Closed shells, inside/outside, holes and non-manifold cases need explicit
incidence and validation contracts. Equal coordinates alone cannot create
shared identity, a watertight seam or a connected solid.

GojoCAD's existing paths, shared sites and boundary experiments help here, but
do not yet demonstrate this complete 3D hierarchy. Mesh, constructive-solid
and implicit representations could expose different local reads of a common
object. Converting between them should retain which geometry/topology was
preserved, approximated or left unresolved. A signed field is not automatically
a signed *distance* field; distance requires a metric and the appropriate law.

### Robust predicates and uncertainty

For orientation, intersection, containment and clearance, specify what a read
can conclude from its evidence. A useful proposed result distinguishes a
proven relation, a proven contrary relation, insufficient precision and an
unsupported/degenerate request. Boundary inclusion is another explicit choice.
Refinement may make a predicate decidable; uncertainty should not silently turn
an ambiguous contact into a Boolean success.

This does not eliminate established computational geometry. CGAL already
offers [exact predicates and exact-construction kernels](https://doc.cgal.org/latest/Kernel_23/index.html),
and Open CASCADE explicitly models
[shape tolerances](https://dev.opencascade.org/doc/refman/html/class_shape_fix___shape_tolerance.html).
The opportunity is to
connect geometric precision with physical uncertainty, intent, evidence and
evolution. It is not established that Gojo can replace robust predicates with
coarse numbers or outperform existing kernels.

Keep numerical approximation bounds, imported seam tolerances, manufactured
variation, sensor uncertainty, preferred nominal values and desired clearance
separate. A repair that moves geometry to close a gap is an explicit operation
with displacement and correspondence, not an innocent observation. Refining a
measurement need not mutate the object being measured.

## Worked example: a bolt and a hole

Consider an illustrative smooth round bolt in a smooth round hole. These are
assumed example bounds, not a manufacturing standard:

```text
bolt diameter Db: [9.90, 10.00] mm
hole diameter Dh: [10.10, 10.20] mm
```

Under those bounds, diametral clearance `Dh - Db` lies in `[0.10, 0.30] mm`.
For coaxial circular sections, radial clearance is half of that:
`[0.05, 0.15] mm`. Suppose axis offset `e` lies in `[0, 0.04] mm`. The minimum
radial gap in this selected section is `g = (Dh - Db)/2 - e`, conservatively
bounded by `[0.01, 0.15] mm`. A required minimum of `0.02 mm` is therefore not
guaranteed across all admitted combinations, although many combinations satisfy
it. This calculation assumes no additional dependence between the dimensions;
known dependence should constrain the combinations rather than be discarded.

The required structure includes two object identities, their measured or
specified diameter surfaces, the common unit/calibration, axis correspondence,
the conditional bounds, a selected clearance read and a separate requirement.
Its witness should retain the assumptions that produced the result.

If two measurements share a known additive calibration bias `b`, their
difference can cancel that bias: `(Dh + b) - (Db + b) = Dh - Db`. Independent
biases do not cancel merely because the readings use the same approximate
category. This is a practical reason to preserve shared uncertainty structurally;
it does not remove other uncertainty about either manufactured part.

“About 10 mm” alone would not justify either of the starting intervals. It can
remain a landmark with an open expectation. A catalogue's nominal size is a
different surface again. A favourable clearance does not establish compatible
threads, pitch, handedness, lead-in, roundness, full-length alignment or a
collision-free insertion path. Those can remain open without making the
section calculation false.

Changing the desired gap is a design request. Learning that the drill produces
a larger hole is an observation. Actually enlarging a hole is a physical
transformation. They can all create relevant mismatch, but require different
correspondence and change paths. The unknown source of a mismatch must not be
declared “the bolt is wrong” by the clearance reader.

GojoEcad's existing plated-hole example already distinguishes a produced span
from component acceptance and records directional violation. It is a useful
fixture to translate into common syntax before adding a large mechanical CAD
example. The fused via-family example is equally valuable: legal pairs of drill
and diameter are not necessarily the Cartesian product of two independent lists.

## Electronics: a good fit with specific physical obligations

Relations and exposed terminals are a natural fit for circuit topology. There
are nevertheless several different connection claims:

- A schematic or net list says two terminals should connect.
- A chosen ideal circuit model treats them as one node.
- Copper, plating and contact geometry provide a physical path.
- A continuity measurement supports a claim about an actual manufactured board.

These claims can disagree. A net name does not prove a conductive path; a
coordinate coincidence does not establish copper contact; a potential read
need not drain charge from its source. Conversely, two separately represented
conductors can couple through a field without becoming one node.

The next electrical composites need an explicit potential reference, oriented
branch quantities, elapsed time, stored state and compatible units. For ideal
elements under selected sign conventions, useful starting laws are:

```text
resistor:   vab = R * iab
capacitor:  q = C * vab,       dq/dt = iab
inductor:   lambda = L * iab,  d(lambda)/dt = vab
power:      p = vab * iab
```

Here `lambda` is flux linkage. Constant `R`, `C` and `L` are assumptions of
these simple models. At an ideal node with no separately represented stored
charge, the signed branch currents sum to zero. Storage must otherwise appear
in the balance. A passive resistor with `R > 0` dissipates nonnegative power
under the associated current/voltage convention. The SPICE
[element reference](https://www.macspice.com/ug/sec3.html) is a useful external
comparison for terminal conventions and model parameters.

Gojo tension is not automatically voltage, a passage's capacity is not
automatically capacitance, and a carrier face is not electrical phase or the
imaginary unit. Signed physical quantities need explicit representations
consistent with the kernel's nonnegative carrier value and signed locations,
faces and tensions. A voltage read and a material charge transfer are different
operations. A branch must not duplicate conserved charge across multiple
accepted destinations.

Constitutive equations, conservation and integration must remain ordinary
mathematics. Local settlement is a strategy for obtaining an admissible state;
it does not establish that an arbitrary local update schedule will converge to
the circuit solution. Inconsistent ideal constraints, floating references,
algebraic loops, stiffness and discontinuous switching can remain unresolved
until the selected method has a defined response. Simulation tolerances and
physical device tolerances also have different meanings.

For a first static check, an ideal 10 V source across two series 1 kilohm
resistors gives 5 mA and a midpoint 5 V above the chosen return node, assuming
no load at that midpoint. A goal of 6 V creates a design mismatch; it does not
make the conditional 5 V calculation approximate or erroneous. Tolerance,
loading or a different source can be added as explicit participating premises.

A useful next dynamic example is a capacitor initially at zero connected through
a positive resistor to a constant ideal voltage source. Under those assumptions,
`Vc(t) = Vs * (1 - exp(-t/(R*C)))`. Its state persists between observations; the
source input, integration step and observation time are not interchangeable.
A time-step refinement should approach the selected law without inventing or
losing accounted charge. This is a proposed reference experiment, not current
GojoECAD behavior. It connects directly to stage S5 of the signal plan.

SPICE models can initially be external, versioned black-box contributors with
declared ports, parameters, state/initial conditions and applicable analyses.
Their outputs can be compared with selected Gojo reads without claiming to
understand every internal equation. Later, explicit reusable substructures may
replace supported models. The
[ngspice documentation](https://ngspice.sourceforge.io/docs.html) describes a
pipeline from netlist preprocessing and circuit construction through matrix
assembly and simulation; encoding the first stages alone does not implement
the latter ones.

## Evolution, uncertainty and crystallized composites

Confidence should attach to a claim and its basis: a curve may be exactly
defined while the manufactured part is poorly measured; a net may be clearly
specified while its copper continuity is uncertain. A single confidence value
for the whole part or board would discard these distinctions. Evidence may be
reused through several routes without becoming independent evidence.

Keep design revisions separate from physical time and evidence arrival. A
hole's diameter can change while its intended feature identity survives; a
Boolean cut can split one face into several. Persistent correspondence needs
one-to-many and many-to-one histories, not only equal coordinates or a reused
label. Dependencies should identify which fit, contact or prediction claims
became stale. The current CAD read graph has affected-read navigation, but
`RefreshFrom` rebuilds its reads from the root; it is not yet a general
incremental evaluation engine.

History need not be retained forever. A compression or crystallization should
say which useful promises survive, which evidence was summarized and what
detail is no longer available. Stabilizing a unit's readable tick promise is
not the same operation as packaging an arbitrary dynamic algorithm. A reusable
curve or circuit composite needs exposed ports, parameter roles, internal
correspondence, applicable laws and, where necessary, state and error contracts.

The Bézier fixture is the model to follow: its control points and six local
interpolations explain the construction even when a control remains open.
Later spline/NURBS composites will additionally need degree, knot order,
weights, parameter domains and denominator/continuity conditions. A SPICE
subcircuit needs terminal binding and model/state correspondence. These are
compositional obligations, not a requirement to introduce opaque primitive
“Bézier” or “SPICE” opcodes.

## Concrete limitations found during inspection

These are source-inspection findings. They were not repaired in this pass.

| Location | Finding and implication |
| --- | --- |
| `AffineFrame.TryProject` | One source coordinate per output coordinate; a general coupled transform still needs a selected contract and tests. |
| `GraphicAreaSettlementSurface` | Fixed values include `Epsilon = 0.01`, endpoint distance `1.5`, minimum mixed-loop area `256`, six-decimal deduplication and rounded locations. These imply scale-dependent decisions and require review before claiming resolution-independent or certified topology. |
| Compact CAD curve recipes | A pair of spans alone can be ambiguous between a cubic recipe and other relations, especially under degeneracy. The explicit construction fixture addresses this boundary; compact recognizers should not become universal semantic dispatch. |
| GojoEcad `CreateContacts` | Groups participants by net number and fine X/Y coordinates, without a layer/plating test in that grouping. Same-net points on different copper layers need not be electrically connected. Treat this as an insufficient contact predicate for broader imports; the first-board case is not a general connectivity proof. |
| GojoEcad pad placement | Nonzero footprint rotation remains an unsettled case. A rotated-pad fixture should precede wider geometric import claims. |
| ECAD readable serialization | `gojo-ecad-readable 2` and `gojo 1` are distinct formats. A parser/restore bridge has not been demonstrated. |
| Current inspected CAD/electronics surfaces | Nominal component structure and documentary evidence do not establish a general circuit-law or transient evaluator. |

Missing verification evidence is an open claim. It is not automatically
material tension. Likewise a failed geometric/electrical read may motivate a
change, but the change still needs its own explicit operation and accounting.

## Recommended representation fixtures

These are proposed tests, not implementation commitments for this pass. Each
should have an authored syntax example, numeric restoration, a selected read,
negative cases and a plain-language explanation before wider abstraction.

| Order | Fixture | What it should establish |
| --- | --- | --- |
| C1 | Import one exact board measure and one fused via family | Source spelling versus value versus nominal choice; physical projection remainder; unsupported source retained; an explicit bridge into common syntax. |
| C2 | Place a pad in a translated and rotated footprint | Point/displacement distinction, coupled axes, exact rational transform cases first, inverse/degenerate cases, source-to-board correspondence. |
| C3 | Same XY on different layers, then add a plated connection | Distinguish intended net, geometric coincidence, layer membership, physical contact and selected ideal-node equivalence. An unknown layer must not imply connection. |
| C4 | Bolt/hole or lead/plated-hole fit | Nominal family, tolerance evidence, desired clearance, shared uncertainty and unresolved pose; recompute affected claims after a design or evidence change. |
| C5 | A trimmed curve boundary with shared and merely coincident endpoints | Parameter-to-geometry correspondence, orientation, boundary closure and partial construction; retain ambiguity instead of silently welding sites. |
| C6 | A copper region with a hole and a nearby keepout | Selected metric and clearance, robust predicate outcomes, scale/refinement behavior, topology-changing repair recorded as an action. |
| C7 | A resistor divider, then an RC transient | First exact static equations and gauge/sign/unit checks; then retained physical state, charge accounting, integration refinement and separate prediction assessment. |
| C8 | Revise a footprint or split a face, then export | Definition/instance identity, multiple successor correspondence, stale evidence, bounded history and version-specific source/export preservation. |

Start with C1–C4 when this domain work resumes. They exercise the largest shared
gaps without requiring a full solid model or nonlinear simulator. C5–C8 can
then justify broader contracts from concrete examples. A viewer is worthwhile
for rotation, contact, fit and changing topology; pure unit arithmetic already
has sufficient numerical fixtures.

The acceptance boundaries should remain separate: source round-trip, restored
Gojo structure, external application acceptance, geometric validity, design-rule
compliance, simulated behavior and measured physical behavior each answer a
different question. Passing one is evidence for its selected claim, not a
global declaration that the design is correct.

## Verification of this assessment

The documentation pass changed no runtime code, grammar or fixtures. Existing
tests were run with the following focused solution command:

```powershell
dotnet test Gojo.slnx --no-restore --filter "FullyQualifiedName~GojoCAD.Tests|FullyQualifiedName~GojoSyntaxForecastTests|FullyQualifiedName~GojoSyntaxAffineFrameTests|FullyQualifiedName~GojoSyntaxBoundedQuantityTests|FullyQualifiedName~ProjectionConformanceTests" --verbosity quiet
```

All 260 selected cases passed: 155 CAD, 79 encoding and 26 kernel cases.
This verifies the selected existing examples, not the proposed fixtures or
broader CAD/circuit conformance. Core4 and GojoEcad were inspected as source;
their tests were not rerun or their dependencies changed. Local links in the
new documents and updated entrypoints were checked. External references were
reviewed on September 8, 2026.
