# Contour-grounded rotation and relations between local spaces

Status: source review and exploratory construction plan, September 8, 2026.
Baseline: `037c62e`. No new runtime behavior or grammar is introduced here.
This refines the [CAD/ECAD assessment](GojoCadEcadSyntaxAssessment-2026-09.md)
following the user's clarification of rotation, locality and the experimental
role of GojoEcad.

## The change in emphasis

The first assessment correctly identified incomplete coordinate-transform and
connectivity readers, but gave coordinate matrices too central a role in the
proposed rotation work. Gojo already has a more appropriate starting point:
point-derived contour potential, radial attachment, marked traversal,
partition and correspondence between contours.

Rotation should become readable from such connected construction. Degrees,
radians and a Cartesian matrix can express a calibrated read or an execution
backend. They must not provide otherwise absent turning semantics. A different
connected construction may also support rotation; the contour example is a
small candidate to refine, not an exclusive universal recipe.

GojoEcad is an experimental source of problems and ideas. Its record shapes,
readable format, contact grouping and pinned dependency are not requirements
for new Gojo construction. Preserve useful discoveries and assess proposed
solutions on their merits. There is no obligation to maintain compatibility
with every exploratory representation before finding a satisfactory structure.

## What already exists for rotation

The direction is explicit in the
[potential-shape fact sheet](../axioms/Gojo%20Potential%20Shape%20Fact%20Sheet.md),
[structure-through-tension notes](../axioms/Gojo%20Structure%20Through%20Tension.md)
and [concept-shape codex](../ConceptSurface/ConceptShapesCodex.md). They distinguish
the possibility of a contour from its radius, reference marker, traversal,
partition and eventual measurable geometry.

| Current evidence | What it establishes | What it leaves open |
| --- | --- | --- |
| [UnitShape.CircularFrom](../Core/UnitShape.cs), [circular structure tests](../../Gojo.Tests/Core/CircularStructureTests.cs) | A point's orthogonal source is retained as inspectable unit provenance. A radial endpoint can be connected to a perimeter marker by an ordinary relation. | The seed does not generate a completed circle, discover its container or enact a rigid turn. |
| [Ordinary cycle tests](../../Gojo.Tests/Core/OrdinaryMathFlowTests.cs), [unit-shape tests](../../Gojo.Tests/Core/UnitShapeTests.cs) | Partitioned cycles wrap, count crossings through a carry route, and project between explicit cycle ratios. Circular carriers can participate in these same operations. | A counter alone does not prove equal geometric sectors or radius-preserving movement. |
| [CAD read graph](../../GojoCAD/GraphicReadGraph.cs) and [tests](../../GojoCAD.Tests/GraphicReadGraphTests.cs) | Circular markers, spans, contour partitions, base-to-contour attachments and linear endpoint witnesses are recognized through connected structure. | These selected attachment reads do not supply a complete turning or metric law. |
| [Graphic syntax fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/graphic-primitives.gojo) and [tests](../../Gojo.Encoding.Tests/GojoSyntaxGeometryTests.cs) | Carrier-native circular and linear promises and partial ports survive numeric restoration without needing X/Y tuples. | No general contour-to-pose executor is established by those tests. |
| [Gear model](../../Gojo.GearShapeLadder/GearShapeLadderModel.cs) and [tests](../../Gojo.GearShapeLadder.Tests/GearShapeLadderModelTests.cs) | Motion enters as tension; perimeter projections, opposing motion, phase calibration, shared pitch identity and refinement history remain structural. | The renderer converts ticks to angles; the geometry builder also uses trigonometric sampling. This is not a completed native geometric rotation construction. |

For example, the existing syntax already contains this shape:

```gojo
let $anchor_axis = carrier();
let $anchor_tick = tick(5);
let $anchor = point($anchor_axis, $anchor_tick);
let $circle = crystal(orthogonal($anchor));
let $linear = crystal(orthogonal(span($anchor_axis, $anchor_tick, $anchor_tick)));
let $arc = span($circle, -2, 6);
let $line = span($linear, -3, 8);
root group($anchor, $circle, $linear, $arc, $line);
```

This is an excerpt from the `gojo 1` fixture, not a complete new rotation
program. It establishes distinguishable shape promises and ordered spans.
The missing turning contract should connect to those structures rather than
replace them with a number named `angle`.

Blocks is useful evidence but remains an experimental inventory. In its
[structure catalogue](../../Gojo.Blocks/Glossary/BlocksGlossaryStructureCatalog.cs),
the `rotated-frame` row currently groups two line examples; the description
expresses an intended transform rather than an executed rotation. The
`PartitionedCircle` builder places numbered markers on a circular carrier but
does not itself attach a complete closure/radius/uniformity law. Its descriptive
text is stronger than that construction. These are unfinished examples to
learn from, not canonical definitions to preserve.

## The smallest useful turning construction

The following is a proposed sequence of structural obligations. Several early
pieces already exist separately; their full composition is not implemented.

1. **Source and available space.** Preserve the originating point and the local
   space through which its orthogonal possibility is being read. A point alone
   does not choose a plane, an isotropic distance law or all possible dimensions.
2. **Bounded contour.** Connect the selected level/reach condition and closure.
   To read a Euclidean circle, establish equal radial reach in the selected
   geometry. An arbitrary level contour need not be a circle.
3. **Radial attachment and reference.** Connect a centre-to-contour relation
   to an actual marker. This can establish a reference direction. Radius length
   alone does not choose a contour marker, traversal direction or partition.
4. **Traversal and partition.** Expose ordered movement around the contour,
   its closure correspondence, and the repeated units through which it is read.
   Equal counter increments require supporting correspondence before they can
   be interpreted as equal turns. Unmarked orientation remains a valid open state.
5. **Moved radial relation.** Connect the selected contour movement to the new
   radial endpoint while retaining the origin and radial condition. This is the
   first geometric turning result, distinct from advancing a cycle counter.
6. **Attached structure.** Expose how a second ray, pad or frame follows the
   same turn. A rigid-turn claim preserves the selected relative geometry;
   moving one endpoint on a contour alone does not prove rigidity of a whole part.
7. **Witness and history.** Retain the request, construction, old/new sites,
   correspondence, unresolved participation and any chosen coordinate read.

The radius-to-contour relation is therefore a particularly useful bridge. It
makes a change in contour position a candidate change in radial orientation.
It still needs the receiving geometry to interpret that change. This identifies
the gap more precisely than saying that Gojo lacks a rotation matrix.

Crystallized unit provenance is a retained snapshot. The existing syntax tests
explicitly show that moving the source anchor does not change the captured
circular origin. A live pivot or moving centre needs its own exposed pose and
correspondence; it cannot be implemented by silently rewriting the unit promise.

## Example: a pointer on a partitioned ring

Start with a pointer attached at the centre and at one marked site on a ring.
Its length and attachment are known. Its absolute compass direction need not
be known. Other equally valid local spaces may orient the whole assembly
differently.

Suppose the ring exposes twelve equal repeated sectors and the pointer follows
that traversal. Advancing by three sectors is a quarter of the declared cycle.
It becomes a geometric quarter-turn only when the circle and equal-sector
correspondence support that read. A ring with twelve unevenly spaced markers
does not meet the same contract. Nor does a counter that happens to wrap at twelve.

After four such advances, the endpoint returns to its starting site under the
declared closure, while the history records one complete traversal. Zero motion
and a completed cycle can have equal exposed endpoint positions and different
histories. A full-turn return does not generally prove that every transported
orientation on an arbitrary contour returns unchanged; the rigid planar-circle
contract must support that stronger claim.

The current accounting mechanism already demonstrates an important part of
this: introducing 27 ticks into a twelve-part circular carrier with a carry
route leaves phase 3 and two counted crossings. With only a wrap boundary,
the crossed 24 ticks remain tension instead. This is meaningful cycle
accounting without degrees or radians. Negative traversal still needs its own
signed movement/correspondence contract: an unsigned carrier value cannot
silently become a negative modulo phase by drawing below zero.

Reversal of traversal, opposed carrier flow, negating a coordinate and changing
the observer's handedness remain distinct. A perpendicular construction can
provide a quarter-turn in an appropriate planar geometry, with its direction
resolved. The general Gojo orthogonal outlet means participation unaccounted
for in the current order; it must not universally mean a ninety-degree turn.
In three dimensions, an axis or oriented plane supplies additional necessary
context.

Only after this structure is readable should we compare it with coordinate
results, including the rational matrix example in the earlier assessment.
Exporting its turn in degrees or radians then becomes calibration. An optimized
matrix or trigonometric implementation must retain correspondence with the
selected construction and its approximation promise.

## A layer boundary is a local interface

The user's layer example is a natural composition of local spaces. One
perspective may expose a top conductor and its passage interface. Another may
expose the bottom conductor or reference plane. The passage between them has
its own participation. A larger local space can select all three and reason
about their combined behavior; it does not become an all-knowing global observer.

At the lowest useful resolution, the receiving side need only interpret its
exposed attachment and the promised participation. It need not reconstruct the
source's private geometry. The composition should nevertheless retain a route
and enough correspondence for a later request or failure to reach the relevant
interface:

```text
top participation -> exposed passage -> bottom participation
                         |
                 selected transfer/read
                         |
           accepted, blocked, unresolved evidence
                         |
           connected request or response route
```

“Layer-aware” should therefore mean that admissibility is grounded in these
relationships. A layer name can help an importer establish context, but a
string comparison is not the physical connection law. Same coordinates on two
unconnected layers remain distinct sites. Adding a plated passage introduces
connected participation, not an automatic merger of all coincident sites.

A ground via connects reference conductors. A signal via carries its signal
net; it should not thereby connect that net directly to ground. Signal and
return participation must be distinguishable. A low-resolution model may expose
only continuity. A stricter model can ask whether the *combined* forward and
return paths meet a frequency/time-dependent response requirement. TI's
[high-speed layout example](https://www.ti.com/document-viewer/lit/html/SLLA653/GUID-A4F1CD83-9D39-45B1-B7A5-0E03429E4305)
illustrates why reference-plane continuity and stitching at transitions matter.
Its device-specific layout dimensions are not proposed Gojo laws.

Inductance and capacitance describe physical participation already associated
with the geometry and materials; failure does not create them. A changed signal
or a finer observation can expose their previously omitted effects. A local
space may publish a response shortfall without knowing whether it originated
in the passage, reference path, source, load or measurement. Both neighbours
can receive related mismatch without being declared independently at fault.

A need for information can become an explicit request and a demand for change.
Unbound information alone does not inject carrier tension. A connected response
can turn relevant mismatch into exploratory pressure, with its basis and route
preserved. Geometry changes, another connection, a revised operating condition
or a revised model are candidate responses; no present rule guarantees that
geometry will autonomously settle into a successful design.

## Fit uses the same composition discipline

A bolt and a hole can expose mating surfaces without either local space knowing
the other's entire object. Their receiving relation selects a fit requirement:
clearance, interference, alignment, travel, thread compatibility or another
condition. Measurements and tolerances participate through their own bases and
evidence. The [earlier fit example](GojoCadEcadSyntaxAssessment-2026-09.md#worked-example-a-bolt-and-a-hole)
is a small read of that connection.

The shared pattern is exposed structure, correspondence, a selected relation,
conditional assessment and a possible response. The physical laws remain
specific to the participating quantities. Mechanical clearance is not electrical
conductance; observing either is not automatically a material transfer. A
common composition mechanism should not force them into an identical scalar
equation or an identical update policy.

## Revised experiment order and stopping points

Implementation follow-up: [the first rotation fixture](GojoContourRotationFixture.md)
now exercises a discrete planar instance of R1–R4 with syntax, exact selected
reads and a small demo. Continuous, uncertain and three-dimensional rotation
remain open. The verification section below records the earlier documentation
review; the fixture document records the new implementation checks.

| Experiment | Required observation | Refuse or retain as open |
| --- | --- | --- |
| R1: contour grounding | Point-derived seed, selected container, radial attachment, marker and closure can be discovered from their connections after syntax restoration. | A seed alone cannot be read as a complete circle; an unrelated radius or equal coordinates cannot supply attachment. |
| R2: traversal and calibration | Partition and closure support phase/cycle correspondence; reversal and coarse/fine reads preserve the declared traversal. | No implicit uniform geometry from numbered markers; no clockwise default; no loss of completed traversals. |
| R3: transported radial relation | A chosen movement preserves radial reach and exposes a changed orientation, with a valid inverse under the chosen contract. | Missing contour geometry, ambiguous direction or incompatible radius remains visible; do not manufacture Cartesian output. |
| R4: attached frame or pad | Two connected radial features follow the same turn, preserving their specified relationship; coordinate checks agree afterward. | Moving just one feature does not establish a rigid-body rotation; a singular or partial frame cannot claim an unrestricted inverse. |
| L1: connection across spaces | Two independently readable surfaces connect through an exposed passage; shared/blocked participation survives restoration. | Coincidence, the same net label or unknown layer participation cannot establish a physical passage. |
| L2: a stricter request | A continuity read remains valid while a higher-frequency response claim stays open or fails; feedback returns through explicit interfaces. | Do not equate DC continuity with adequate signal response or infer the faulty participant from one mismatch. |
| F1: a mating relation | Bolt/hole or lead/hole surfaces expose a conditional fit, with source dependence and desired clearance preserved. | A nominal size does not imply tolerance, conformance, compatible pose or a successful full insertion. |

R1–R3 should guide the next rotation work before a general matrix API is treated
as the solution. Some R1/R2 pieces can be reused from existing tests; a new
fixture must check their composition and important false reads. L1 and F1 should
exercise the same exposed-interface mechanism with different local laws. L2
initially needs only a declared response source and an unresolved or violated
claim, not a complete electromagnetic solver.

Keep the syntax aligned as these constructions settle. Introduce authoring
shorthand only when it expands into the actual participating structure and can
be restored and inspected without the helper's name. An attractive drawing,
ECAD capture or passing scalar test alone is not the promised construction.

## Verification

The focused existing tests passed 193 cases: 71 kernel, 52 CAD, 49 syntax and
21 gear-ladder cases. The command was:

```powershell
dotnet test Gojo.slnx --no-restore --filter "FullyQualifiedName~CircularStructureTests|FullyQualifiedName~UnitShapeTests|FullyQualifiedName~OrdinaryMathFlowTests|FullyQualifiedName~GraphicReadGraphTests|FullyQualifiedName~GojoSyntaxGeometryTests|FullyQualifiedName~GearShapeLadderModelTests" --verbosity quiet
```

This verifies the inspected footholds, including the existing syntax excerpt.
The proposed R/L/F experiments are not implemented or covered by this result.
This pass changes documentation and clarifies the canonical instructions and
their design notes; it changes no runtime code, fixture or grammar.
