# Primitive patterns through acknowledged structural flow

Implementation checkpoint, 2026-10-17. The
[primitive flow gallery](../../experiments/QualitativeFrieze/results/primitive-flows/gallery.html)
executes small pre-numeric recipes for steps, staircases, square waves,
zigzags, rectangles, diamonds, meanders, open-spaced herringbone and stars.
The [skeleton atlas](../../experiments/QualitativeFrieze/results/skeleton-atlas/index.html)
remains an authored reference; these new diagrams are reads of actual native
execution. No domain-specific kernel primitive is added.

## Shared mechanism

The [flow nodes](../../experiments/QualitativeFrieze/PrimitiveFlowNode.cs)
compose existing acknowledged drawing sequences through their advertised
input, completion and ownership-account surfaces:

```text
leaf
  input packet -> local occupied site -> readiness question
  -> offered drawing action -> exact accepted drawing receipt
  -> packet release -> phase transition -> possible cycle completion

group
  current native phase -> call row -> child input and completion ports
  -> exact child request -> accepted child acknowledgment
  -> whole-child completion -> parent phase transition
  -> next child permission, or whole-group completion
```

Each occurrence of a reused child has a distinct call row and phase. The child
itself remains one operational structure. A step can be called three times by
a flight, and the flight can be called by a higher structure that reverses
the next flight's transverse interpretation. The lower layer need not inspect
the whole pattern or the caller's private controller.

The native group definition connects child surfaces, input/completion ports,
phase transitions, call rows and a finite dispatch law. The host executes the
selected row; an ID or pattern name never selects a movement. Names and the
catalogue are presentation projections.

Upward communication is authenticated completion observation. Downward
communication is permission and an offer of participation through one active
child input. Same-level succession follows the caller's exposed call edges.
Material is imported and accounted at leaves; a completion notification does
not create another packet. Parent accounts read immediate child publications
and deduplicate shared ownership surfaces.

This is serial acknowledged composition. The separate sampling experiment
below uses the existing event network for joint participation. These fixtures
do not establish an arbitrary concurrent scheduler, cyclic material routing,
unrestricted peer calls, learning or continuous signed-carrier settlement.
Discrete uncalibrated packets and occupied sites remain distinct from
`Carrier.Tension`.

## Recipes and expectations

The [recipe library](../../experiments/QualitativeFrieze/PrimitiveFlowLibrary.cs)
contains actual action, transform and call structures. Finite repeated slots
and words remain visible participation. Native source graphs contain no
numeric carriers or carrier locations; authoring a finite number of phases is
not the same claim as having no cardinality or repetition.

| ID | Construction | Expected observation |
|---|---|---|
| P01 | Single step: run child, then rise child | The parent remains incomplete after the run alone. |
| P02 | Three calls to the same step | A staircase with distinct call occurrences and one reused step. |
| P03 | Flight, then transverse inversion; repeat | Ascending and descending stair flights controlled at the higher boundary. |
| P04 | Explicit axial turn cycle | Square wave with intermediate elbow sites. |
| P05 | Reused step with transverse inversion | Square wave assembled from one small component; interruption exposes a different causal decomposition. |
| P06 | Explicit diagonal turn cycle | Zigzag with two direction contributions per stroke and no elbow between them. |
| P07 | Four ordered rectangle sides | Opposite reaches cancel; a requested closure view joins the corresponding endpoints. |
| P08 | Reused two-side component plus turn actions | The rectangle arises through two calls; each side and turn is acknowledged. |
| P09 | One edge and one turn, called four times | A formal order-four traversal; Euclidean squarehood requires the chosen equal-axis interpretation. |
| P10 | Four opposed diagonal chords | A diamond traversal, with explicit closure correspondence. |
| P11 | Square-side words transported by a local substitution | The same diamond chord sequence as P10, retaining the source square and transform derivations. |
| P12 | One nested-key turn cycle | Non-retracing returns on separate lanes. |
| P13 | Rise, inner fold and return components | Native stroke-word correspondence with P12, with different completion and provenance structure. |
| P14 | Bar -> opposed pair -> row -> field | Open-spaced herringbone from one reused rectangular bar and explicit placement/axis-exchange actions. |
| P15 | One twelve-edge outline from an order-six word action | A single star contour, with formal difference and turn evidence. |
| P16 | Two separately completed triangular loops | A star reading with two components; projected crossings remain unjoined. |
| P17 | Joint sampling of independent horizontal and alternating transverse contributions | Diagonal zigzag. |
| P18 | Separate sampling of those same component emissions | Axial square wave, with the same total native displacement as P17. |
| P19 | A slower transverse recurrence | Terraces; absent triggers are known absence, not missing emission laws. |

P14 deliberately leaves gaps between bars. It tests layered construction and
alternating orientation before trying to negotiate a shared physical boundary
between adjoining bars. A tightly packed herringbone, with shared outline
ownership and no duplicate material stroke, remains a further experiment.

## The same emissions can support different path reads

The [sampling study](../../experiments/QualitativeFrieze/PrimitiveSamplingStudy.cs)
reuses `FriezeFixture.CombinedDriveZigzag`. Its combined and separated fixtures
share the exact event network, output law, starting phase bindings, frame,
reach identities and receiver sample port. Only the sampling connections
change.

```text
horizontal:   forward      forward
transverse:   one face     opposite face

joint sample: (forward + one face), (forward + opposite face)
separate:     forward, one face, forward, opposite face
```

The first produces diagonal chords; the second produces corners. Native
cancellation verifies equal total displacement. That equality does not erase
the different intermediate sites, chord grouping or causal samples. Changing
sampling is an explicit structural read, rather than asking the renderer to
invent a square wave from a diagonal.

The slower fixture changes the recurrence graph. A phase with no transverse
trigger has a complete known absence of participation. A triggered component
whose output-law row is missing instead has unknown motion; it must not be
drawn as a known horizontal stroke. An incomplete cascade budget likewise
does not prove that the remaining events never occur.

## Coordinate closure and topological closure

Drawing a final edge back to the starting displacement creates a fresh site.
The raw stroke ledger keeps that endpoint distinct. A closure request selects
the actual completed leaf or group cycle, follows its accepted drawing
receipts, and proposes an explicit end-to-start declaration. The existing
`QualitativeGraphJoin` checks formal position equality and exposes a derived
connected view, accepted/blocked declarations and correspondence to the raw
sites and strokes.

Thus there are three separate claims: a controller finished, a displacement
returned, and a declared contour joined. Neither completion nor a pleasant
render proves all three. P15's one contour and P16's two loops remain different
under this view; crossings between the two triangles are not silently glued.

The sixfold recipe uses the existing `QualitativeSixfoldWords` action on two
generators, retaining its substitutions and reductions. A 60-degree display
basis is an optional interpretation. The native order-six action alone does
not prove Euclidean angle equality, regularity or a uniquely determined radius.

## What the different recipes let us ask

A flat turn cycle exposes local next-turn questions. A layered step exposes
whole-step readiness and completion, making it possible to gate or repeat a
flight without reading its individual moves. A transported square exposes the
source-to-diamond correspondence. A two-loop star exposes component identity
and crossing policy; the outline recipe exposes a single traversable boundary.

These are useful differences even when a selected view makes results look
similar. Conversely, equal endpoint displacement is too coarse to establish
equal paths, and equal-looking paths do not establish one generating machine.

The gallery's alternative spacing is a rendering read of the same native
words. It does not mutate the recipes. Metric proportions, gap width and
Euclidean symmetry require their own interpretation; the structural
expectations above should remain valid under compatible changes of view.

## Historical construction leads

Steeley and Trotman's *The New Art Geometry* (1901),
[plate 7](https://archive.org/details/the-new-art-geometry-or-geometrical-draw/page/n23/mode/1up),
develops key, zigzag, lozenge and repeated-polygon families from a visible
scaffold. [Plate 12](https://archive.org/details/the-new-art-geometry-or-geometrical-draw/page/n33/mode/1up)
shows different selections over a square subsidiary network. These are direct
construction references for comparing a primitive, its organization and a
selected overall field. The temporal call/return interpretation here is our
experiment, not a claim made by the book.

Burn's *Ornamental Drawing, and Architectural Design* (1857),
[p.13, figure 17](https://archive.org/details/ornamentaldrawin00burn/page/n16/mode/1up),
uses two equilateral triangles in constructing a six-pointed star. P16 retains
the two-loop idea; a filled union or a selected outer contour would be another
explicit operation over occupied support.

Steeley and Trotman's
[plate 3](https://archive.org/details/the-new-art-geometry-or-geometrical-draw/page/n15/mode/1up)
also suggests the next attachment experiments: side versus corner ports,
addition versus subtraction, and overlap versus contact. Those operations
have not been added to the primitive flow fixture. Subtraction will need
explicit occupied support and a carried complement, not graphical erasure.

Billings's *The Power of Form Applied to Geometric Tracery* (1851),
[p.14](https://archive.org/details/powerofformappli00bill_0/page/n17/mode/1up),
provides a further example of a reused radial foundation. Its curved
elaborations can wait; preserving a readable foundation across applications
is already a useful target here.

## Verification and next questions

Focused tests cover acknowledged child boundaries, nested reuse, packet
ownership, stale receipts, counterfeit completions, explicit closure, shared
native recipe correspondences and joint/separate sampling. They also cover
missing emissions, absent sampling and unfinished cascades. A host sampling
budget stopping early must leave the goal unmet.

The checkpoint run completes all nineteen examples with zero unresolved
execution reads and zero native numeric elements. The sixteen actuator recipes
import and release 258 distinct packets, with none held or retained at the
selected end boundaries. Twenty requested end/start joins are accepted in
the separate closure views. Forty-eight new focused test cases pass, including
a completed three-sided program whose requested closure correctly remains
blocked. Desktop/mobile browser checks cover both presentation controls and
the inspectable component details.
Solution verification with `dotnet test Gojo.slnx` passes all 3,540 cases.

The next useful extension is connecting independently placed primitives
through published side/corner surfaces, with acceptance and blocked evidence.
After that, compare one contour against several primitives sharing a boundary,
and connect the selected stroke structure to an explicit occupied/complementary
field. That would make the historical scaffold/selection/field distinction
operational at another layer without teaching the kernel a class for every
pattern name.
