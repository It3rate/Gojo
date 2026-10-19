# Interior-first lattice constructions

This experiment follows the [geometric lattice transformations](GojoLatticeGeometryTransformations.md).
The previous catalogue could reject retracing while still admitting overlapping
closed outlines, since it did not supply occupied interiors. Its small edge
profiles also often refined a familiar line rather than making a new cell.

The [new gallery](../../experiments/QualitativeFrieze/results/lattice-interiors/gallery.html)
therefore starts with space partitioned into actual small faces. Merging faces
withdraws their internal dividers. A cell cannot borrow the same interior from
another cell simply because its outline looks plausible.

The [whole sheet](../../experiments/QualitativeFrieze/results/lattice-interiors/overview.html)
switches between coloured interiors and outlines. The
[construction strips](../../experiments/QualitativeFrieze/results/lattice-interiors/construction-strips.html)
retain the atomic base, partial merging and final partitions. The
[rule page](../../experiments/QualitativeFrieze/results/lattice-interiors/rules.html)
includes actual rejected examples.

## The native construction

All source participants are ordinary relations and placeholders:

```text
complex = Closed(sites) -> (Closed(edges) -> Closed(atomic faces))
edge = identity -> (actual start site -> actual end site)
face = identity -> Closed(ordered boundary darts)
dart = actual edge identity -> (actual from site -> actual to site)
claim = group identity -> Closed(actual atomic face rows)
embedding = Closed(site -> formal motion word) -> (frame -> shared reach)
```

The two neighbouring atomic faces traverse their shared edge in opposite
orders. A larger cell is an explicit selection of actual faces, with a
connected interior and a single ordered boundary. The reader pairs internal
darts, retains removed internal edge rows, and follows the surviving cycle.
Equal coordinates never create a shared edge or a material join.

Each construction stage retains its actual previous and current ownership,
removed and retained edge selections, and atomic correspondence. These are
face-based construction rungs; they do not masquerade as endpoint-preserving
line refinements. The original small faces stay available even when their
separators no longer appear in the current lattice.

The runner transports the complete stage and period packet, imports it into an
independent world, rebinds the declared motion frame, and executes its reads
again. Names and family labels are presentation only.

## Hard rules for this experiment

| Question | Required evidence | Rejected example |
| --- | --- | --- |
| Do filled interiors compete for the same space? | Every actual atomic face has exactly one owner. | X01: one face is claimed by two cells. |
| Has a region disappeared without accounting? | Every atom in the advertised domain is owned. | X02: an unowned face remains a gap. |
| Is this one cell? | Its atoms connect through actual shared edges; the surviving boundary is one simple closed cycle. | X03: separated pieces are put in one group. A ring with an inner boundary is also outside this initial contract. |
| Is the advertised mesh a valid embedding? | Convex nonzero-area atomic polygons; no positive interior overlap; shared positive-span contacts use the actual shared edge. | X04: topologically independent rectangles have overlapping embedded interiors. |
| Does the boundary account for its interior? | Its exact oriented area agrees with the sum of its atoms; its boundary does not self-intersect. | A folded or crossing boundary cannot pass merely because it returns to its start. |
| Is the repeat real? | Two independent period words, actual repeated group/atom correspondences, ordered translated contours, and zero/column/row instances for each canonical group. | Missing, foreign or incorrectly translated occurrences remain blocked or open. |

`InteriorCellPartition` implements ownership, connectedness and boundary reads.
Its embedding read uses exact integer orientation and separating-axis tests
over the supplied convex atoms. Merged interiors may be concave. A separate
`InteriorPeriodicityRead` verifies the advertised finite repeat and canonical
area against the period parallelogram.

These checks concern the supplied domain and embedding. They do not infer
arbitrary filled polygons from an image, solve general polygon booleans, or
prove every possible infinite continuation of a finite patch. All whole tile
occurrences are retained at the jagged finite frontier; a crop does not become
a fictional small cell.

The source contains no numeric carriers or measured lengths. The embedding
checks do use arithmetic on counted participation in a declared shared reach.
Pure ownership and incidence are topological reads; geometric non-overlap is
a stronger read with this additional basis. Absence of measured inputs does
not mean an absence of arithmetic in the observer.

## Broad shapes and fine edge detail

The [existing aesthetic profiles](GojoPatternAestheticRules.md) distinguish hard
legality from authored preference. This experiment follows that distinction.
No total score or universal beauty threshold is asserted.

The shape reader first collapses straight same-heading subdivisions. It then
retains actual convex and reflex corner pairs, heading participation, and small
returns to the same heading. This helps separate a long side with fine teeth
from a contour whose broad arms turn around its interior. Alternating convex
and reflex corners alone cannot make a star into a bad shape.

| Descriptive read | Possible use | Qualification |
| --- | --- | --- |
| Sides after straight collapse | Compare the broad complexity of a cell. | A side count alone cannot distinguish star arms from noise. |
| Fine same-heading returns | Notice repeated little excursions along a persistent directional edge. | The reader's crosspiece threshold is an authored fraction of an interior scale. |
| Interior scale `4A/P` | Compare edge detail with the region it bounds. | It is in the selected formal embedding, without physical calibration. |
| Minimum/median side relative to interior scale | Expose very small turns on a large cell. | Useful small detail is not automatically a violation. |
| Area divided by axis-aligned bounding box | Make slender or widely reaching shapes noticeable. | A good slanted lattice can have low box occupancy. |
| Heading participation and turn alternation | Distinguish sustained direction from turns around a region. | These are observations, not pattern-name classifiers. |

For the current gallery, a fine return has a crosspiece shorter than one
quarter of `4A/P`. A boundary-detail cue appears at three or more such returns
and a fine-return fraction above 35%. These are revisable experimental choices.
The derived distances, compactness and aspect measures use the formal embedding
before the optional affine display basis; they are not perception-invariant.

Focused tests compare repeated small edge teeth with a turning four-arm
outline. The catalogue also includes a broad six-point star, I090. The aim is
to make the distinction inspectable before trying to learn it from ratings.

## Families and seam compromise

The hundred cases use ten families: rectangular rooms; L/elbow rooms; T/fork
rooms; stair/Z rooms; mixed broad polyomino rooms; oblique/shifted rooms;
six-triangle hexagon and polyhex fusions; rhombic/chevron fusions;
triangular/trapezoid rooms; and independently proposed stepped seams.

A bounded authoring search finds periodic exact covers from small shape
palettes. It can rotate and reflect those shapes, then compiles every atomic
site, face, shared edge, owner and repeat into native structure. It is an
authoring supplier, not a hidden runtime classifier or learned preference
model. Repetition is present in the supplied period/instance relations rather
than assumed from a caption.

I091–I100 retain neighbours' independent desired ownerships. These overlap at
actual contested atoms. `InteriorBoundaryNegotiation` requires an explicit
choice of owner for each contested face, retains accepted and losing proposals,
and produces exclusive output claims. It never chooses the first binding by
scope order. The resulting claims must still pass the separate partition and
embedding readers: an accepted ownership decision can produce a disconnected
cell and is not sufficient by itself.

These are supplied compromises, not autonomous bargaining or optimisation.
The rejected claims are observational mismatch evidence. No physical carrier
tension is invented when no material change route has been supplied.

The shared atomic seam prevents two neighbours from independently placing
incompatible outlines over each other. More general continuous boundary
negotiation, rounded shapes and multi-boundary cells remain extensions.

## Running and reviewing

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --lattice-interiors
dotnet run --project experiments/QualitativeFrieze -c Release -- --lattice-interiors --limit 3 --output artifacts/interior-preview
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj -c Release --filter "FullyQualifiedName~Interior"
dotnet test Gojo.slnx -c Release
```

The gallery supports three reads of the same current structure: coloured
interiors, plain outlines, and retained removed dividers. Human ratings and
notes have a separate browser-storage key and can be exported/imported with
source fingerprints. They remain independent of the hard acceptance result.

Use a fragment-free local file link when opening the gallery from the desktop
app, then choose an ID in its selector. Browser probes confirmed that the older
HTML's `#T005` route itself works; the reported local-link dispatch problem is
outside that route. The new gallery also accepts `?design=I011` inside a browser.

`diagnostics.json`, `verification.json`, `browser-check.json` and the native
packets record the executed scope. Candidate counts and formal fingerprints
are not substituted for human judgement about how different or satisfying the
fields appear.

## Recorded result

The final three-by-three run produces 100 distinct embedded partitions across
ten families. Their 25,668 atomic faces account for 5,373 final interiors,
22,500 removed internal edges and 19,091 shared inter-cell boundary edges.
Every final field has exclusive complete ownership, zero embedded atomic
overlap pairs, and accepted native period correspondences. All source packets
remain free of numeric carriers.

The 310 displayed stages include ten deliberately incompatible proposal stages.
Their rejected claims remain visible before the accepted common seams. Final
cells have between three and twenty broad sides; none trigger the current
fine-return warning. Strongly stretched fields remain useful examples of
geometric legality with weaker shape legibility, so they are retained for
ratings rather than declared beautiful by the acceptance gate.

The complete solution passes 4,389 tests across twelve assemblies, including
38 new structural cases. Browser verification passes 2,826 checks, walks all
310 stages in all three modes, and independently checks the filled polygons
of all hundred final drawings. Desktop and mobile layouts, source packet
digests, ratings, local resources and both entry/query routes pass.

The render audit exposed a useful small failure: simplifying one cell's straight
boundary before rounding could omit a neighbour's shared T landmark and leave
a tiny projected sliver. Fill now retains every actual boundary-dart vertex.
Only the shape diagnostics and normalized fingerprint collapse straight
subdivision. The final independent SVG checks pass at a `1e-7` display-unit
tolerance, separately from the exact native embedding checks.
