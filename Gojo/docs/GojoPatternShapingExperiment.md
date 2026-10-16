# Deliberate pattern shaping and two-role partitions

This exploratory checkpoint uses the user's Greek-key and meander references
to author a small library of inspectable qualitative programs, then examines
their ribbon and white-space interpretations. AI-authored deliberate shaping
is allowed here. The experiment performs no random candidate search or model
training, has no learned weights, and makes no claim of exact tracing of the
reference sheets.

The experiment separates three questions: what ordered path was generated,
which two regions occupy a selected strip, and whether a supplied map exchanges
those regions while preserving their topology. A convincing picture alone
does not answer all three.

## Authored centerline programs

[`PatternShapingLibrary`](../../experiments/QualitativeFrieze/PatternShapingLibrary.cs)
shares one `QualitativeMotionFrame` and actual horizontal/vertical reach
participants across eight programs:

| Program | Phases per control period |
| --- | ---: |
| Square wave | 4 |
| Stepped battlement | 8 |
| Folded key | 7 |
| Broad folded key | 7 |
| Alternating opposed keys | 14 |
| Nested key | 10 |
| Deeper nested key | 14 |
| Alternating nested keys | 20 |

Each is a `FriezeFixture.Sequence`: an explicit phase cycle, a finite
phase-to-word output law and exposed sampling connections. A motion is an
ordered direction frame connected to its shared unmeasured reach. A word
contains actual move participants; a longer stroke repeats the same move
inside that word. It does not store a coordinate or substitute a numeric
coefficient for that repetition.

For example, using `r`, `l`, `u`, `d` only as readable aliases for those actual
relations, the folded key emits:

```text
Closed(Closed(r), Closed(u), Closed(l), Closed(u),
       Closed(r,r), Closed(d,d), Closed(r))
```

The inner scopes are committed stroke emissions. Their shared participation
supports cancellation and rightward-period reads before any measured
interpretation. The phases retain their own identities and fresh occurrences;
reusing a word does not reuse its event. The library's maximum is twenty phase
participants, independent of how many periods are observed.

The shapes are deliberately chosen members of familiar fret/meander families.
For related algorithmic work, Zarrinmehr and colleagues construct generalized
meanders by remeshing; this checkpoint does not implement that algorithm or
claim its fixtures reproduce the same constructions.
[Zarrinmehr et al., Bridges 2017](https://archive.bridgesmathart.org/2017/bridges2017-87.html)

### A native phase-origin edit

`CenteredMeander` selects an existing starting phase through `WithStarts`.
It shares the original folded key's exact network, output law, sampling routes,
frame and reach identities. It changes the point in the cycle from which an
observation begins. Under the selected positive embedding this places the
vertical range around the initial level; it does not write a vertical numeric
offset into the source.

The old view remains connected to the new view:

```text
(old-fixture-surface -> new-fixture-surface)
  -> (old-start-bindings -> new-start-bindings)
```

This distinguishes a changed observation origin from a changed path grammar.
`CenteringEdit` and the library's closed `Edits` scope retain that distinction.

## Ribbon width and gap are separate participation

[`RibbonPartitionSupplier`](../../experiments/QualitativeFrieze/RibbonPartitionSupplier.cs)
connects band and gap extents to the source and explicitly decomposes every
advertised axis/reach pitch into those two participants:

```text
(fixture -> (band-extent -> gap-extent))
  -> Closed((axis -> reach) -> Closed(band-extent,gap-extent))
```

Using the same extent identity for band and gap supplies an elementary equality
premise. It does not assert that every white region, corridor or separation in
the completed pattern has the same width. That stronger question depends on
which paths are neighbors, their corners, the selected strip and the actual
geometry.

The runner selects three interpretations with horizontal and vertical pitch
four source ticks:

| Interpretation | Band width | Elementary gap | Native extent relation |
| --- | ---: | ---: | --- |
| Thin | 1 | 3 | Distinct extent participants |
| Shared extent | 2 | 2 | The same extent participates twice |
| Heavy | 3 | 1 | Distinct extent participants |

These are explicit calibrations of the same qualitative paths. The supplier
checks both their sum against the advertised pitch and any shared-identity
equality. A source with unbound calibration does not already promise these
measured widths.

The observer uses a half-source-tick cell grid. For the supported whole-tick
rectilinear paths and widths, ribbon rectangle boundaries align to that grid.
The cell labels describe the exact union of those aligned rectangles inside
the advertised finite strip, rather than a sparse set of occupancy probes.
This precision is conditional on the selected rectangular ribbon construction,
calibration and domain; it is not a general curve-fill algorithm.

Both roles receive actual cell labels. The second role is the complement
inside the chosen domain, with its own regions and interfaces, rather than
unoccupied default background. Ribbon fill witnesses retain which measured
strokes and translated repetitions cover each cell. Separate native rail
frames sit outside the tested domain `D`; they are excluded from any exchange
claim about `D`.

## A topology-level exchange certificate

[`QualitativePartition`](../../experiments/QualitativeFrieze/QualitativePartition.cs)
connects a finite cell scope, directed adjacency, two actual role participants
and cell-to-role bindings. Region reads follow weak incidence of the supplied
edges while preserving those edges' original order. Interfaces retain the
actual edge and the two incident label rows.

An exchange read takes an explicit cell map. A successful certificate requires:

1. A complete bijection of the advertised cells.
2. Opposed roles at every mapped pair of cells.
3. Preservation of the supplied directed adjacency by the map and its inverse.

Matched evidence, mismatches and unresolved obligations remain separate native
scopes. Open cells, labels, topology or maps cannot establish complete exchange.
Equal role counts are insufficient: a bijection that breaks neighborhood
relations fails, and an identity map fails the opposed-role obligation even
when both roles have equal counts.

This is a finite topological statement. Calling its map a translation, mirror
or glide additionally requires an explicit geometric interpretation. Color
reversal in decorative friezes likewise combines a spatial action with an
exchange of the two colors; the exchange is additional structure, not an
ordinary geometric symmetry by itself.
[James, Kalisperis and James, Bridges 2003](https://archive.bridgesmathart.org/2003/bridges2003-135.pdf)
Jablan's presentation makes the two-color permutation explicit alongside the
spatial generators.
[Jablan, *Geometric-Visual Analysis of Symmetry Groups*](https://emis.de/monographs/jablan/chap17.htm)

Changing which role is displayed in black changes the view. It is not an
application of Gojo's element-local `Dual`, relation reversal or scalar
negation. Likewise, equal elementary ink/gap widths and equal area do not by
themselves prove that a particular spatial map exchanges the whole partition.

## A source-authored two-role field

[`QualitativeWeave`](../../experiments/QualitativeFrieze/QualitativeWeave.cs)
provides a second construction route. Authored seed rows contain actual role
participants. A native `FiniteLaw` supplies the opposed role at each paired
cell in the second half. The graph retains those law reads, cell identities,
ordered column/row relations, coordinate correspondences and pitch bindings.
Column order wraps; row order retains exposed endpoints. This is a finite
cylinder before distances or area are assigned.

The explicit half-column shift is a cell permutation. Its exchange read can
settle using role membership and native adjacency alone. Repeated column and
row pitch participants are unmeasured construction promises. Under a uniform
positive rectangular calibration, the same map becomes a half-period
translation, and paired cells have equal area. Equal-area conclusions concern
that interpretation; they do not follow from an arbitrary uncalibrated graph.

The three factories are **stepped chevron**, **nested fret** and **folded
blocks**. They are deliberate authored inspirations, not historical copies or
recovered image semantics. The seed construction is visible source structure;
there is no hidden complement toggle in the renderer. Unknown roles or
incomplete generation laws preserve their failed reads and cannot certify a
complete field.

## Representation cost and scope

A stroke program stores ordered words and recurring phase structure compactly.
A topology-level partition additionally materializes cells, adjacency edges,
role rows, regions, interfaces and map witnesses. Its cost grows with the
selected cell field and retained evidence. Refining a ribbon grid can therefore
be much larger than retaining its originating stroke program. This checkpoint
makes no compute or compact-memory improvement claim for that explicit field.

The finite cell budgets bound an exploratory read. They do not establish a
universal fill/cavity algebra, continuous topology for arbitrary shapes,
automatic shape recognition or a complete color-frieze classifier. Source
paths retain ordered repeated participation; exact angles, distances, areas
and screen pixels belong to a selected interpretation or presentation.

The reference images guide style and deliberate choices. They are not a
training set, a strict tracing specification or evidence of learned weights.
Preference learning, compression and any economic interpretation would need
their own connected goals and experiments.

## Running and verification

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --shape --output experiments/QualitativeFrieze/results/shaping
```

The output targets are the [SVG gallery](../../experiments/QualitativeFrieze/results/shaping/gallery.svg),
[HTML gallery](../../experiments/QualitativeFrieze/results/shaping/gallery.html),
[diagnostics](../../experiments/QualitativeFrieze/results/shaping/diagnostics.json),
[width-style comparison](../../experiments/QualitativeFrieze/results/shaping/styles.svg)
and [phase-origin comparison](../../experiments/QualitativeFrieze/results/shaping/centering.svg).
The [PNG overview](../../experiments/QualitativeFrieze/results/shaping/gallery-preview.png)
is the visual inspection artifact.

Focused coverage is in `PatternShapingLibraryTests`, `QualitativePartitionTests`,
`QualitativeWeaveTests` and the ribbon-supplier tests. The intended checks
separate source graph structure, local qualitative rules, measured periodic
overlap/crossing, interpretation conformance and complete exchange evidence.
The previous [learning experiment](GojoFriezeLearningExperiment.md) remains a
different experiment with a different search contract.

## Recorded results

The 2026-10-16 run generated eleven catalog entries: eight ribbon programs and
three counterchanging fields, together with twenty-four width interpretations
and a two-view phase-origin comparison. The overview, width comparison and
centering SVGs were rendered to PNG and visually checked against the supplied
reference families. Folded, nested and deeper nested keys reproduce recognizable
simple fret/spiral constructions; opposed and alternating versions provide
related variations. The references remain stylistic guidance rather than
pixel-accurate tracing specifications.

Every ribbon source passed the qualitative period/local checks and the current
global centerline overlap/crossing read under both `H=4,V=4` and `H=6,V=4`
interpretations. The latter is an aspect sanity check, not a new independent
measurement. The rectilinear partition supplier used `H=4,V=4`, with elementary
band/gap widths `1/3`, `2/2` and `3/1`. All twenty-four partitions were complete.
The source and native field graphs contained zero carriers and zero carrier
locations; measured observation graphs deliberately contained both.

| Catalog family | Phases | Reach participations in words | Ink in the selected equal-extent field | Dark / light connected regions |
| --- | ---: | ---: | ---: | ---: |
| Square wave | 4 | 4 | 57.1% | 1 / 2 |
| Stepped battlement | 8 | 8 | 36.4% | 1 / 2 |
| Folded key | 7 | 9 | 54.5% | 1 / 2 |
| Broad folded key | 7 | 13 | 47.3% | 1 / 2 |
| Alternating opposed keys | 14 | 18 | 31.6% | 1 / 2 |
| Nested key | 10 | 25 | 52.6% | 1 / 2 |
| Deeper nested key | 14 | 51 | 47.0% | 1 / 2 |
| Alternating nested keys | 20 | 50 | 28.6% | 1 / 2 |

These fractions concern each advertised strip and exclude rails. Shared
elementary width does not imply half the total area: the paths, corners and
larger white spaces differ. The tested half-period translation and glide maps
failed exact exchange for all eight ribbon fields, with mismatch cells retained.
This is not a complete search over geometric maps. The topology provides a
stronger obstruction for these particular complete partitions: an adjacency
automorphism exchanging roles would have to pair their connected regions;
one dark region cannot be bijected to two light regions. The domain and its
periodic-X/bounded-Y boundary contract are essential to that statement.

| Source-authored field | Cells | Dark / light cells | Dark / light regions | Native half-shift exchange |
| --- | ---: | ---: | ---: | --- |
| Stepped chevron | 128 | 64 / 64 | 1 / 1 | Passed |
| Nested fret | 128 | 64 / 64 | 1 / 1 | Passed |
| Folded blocks | 200 | 100 / 100 | 2 / 2 | Passed |

These exchange certificates use no measured carriers or coordinates. They
retain 128, 128 and 200 actual map rows, with zero mismatches and zero unresolved
obligations. The paired regions and cells can be inspected as entities on both
sides of the correspondence. Equal physical area and a literal half-period
translation then follow in the selected uniform rectangular interpretation.

The centering example retains the folded key's exact phase graph and output
law while selecting an existing start phase. In one fixed observation strip,
the lower/upper white margins change from **17/1 cells to 9/9 cells**; their
imbalance falls from sixteen to zero. The ink fraction stays the same. Actual
boundary-gap spans and the start-binding edit remain connected to the read.
The observed counts feed native `Equation.Difference` ports with an explicit
zero-error goal; prediction and residue remain in the witness. That is a
selected measured preference, rather than an innate law requiring all gaps
to be equal.
This is deliberate structural shaping, not a numerical translation assignment
or a learned controller.

The ribbon source surfaces, including band/gap promises, contain 267–483
distinct elements under identity-visited traversal. The two 128-cell woven
sources contain 3,535 elements each; the 200-cell source contains 5,441. Those
counts include generation evidence but exclude measured view structure.
Thus an explicit field buys region/exchange inspection at appreciably greater
representation cost than the compact motion program. No storage or execution
speedup is claimed.

Verification: `dotnet test Gojo.slnx -c Release --no-restore --verbosity minimal`
passed **3,085 tests across 12 projects**, including **655 qualitative execution
tests**. This checkpoint adds 43 cases across the four fixture/test families.
The diagnostics file is a projection of retained native observations, rather
than a restorable serialization of every witness.

## What this suggests next

The small reusable rules are: retain ordered turns and repeated extents; keep
band and gap participation separate; declare the finite field and its joins;
give both roles actual regions and interfaces; and justify each proposed
exchange by an explicit topology-preserving map. Phase selection and extent
calibration can then shape a valid construction without replacing its causal
program.

The next useful refinement is a compact reusable motif/field correspondence:
for example, derive a white corridor or interface from a partition and relate
it back to a stroke program. Forward ribbon-to-field correspondence now has
actual fill witnesses; the reverse recognition/compression step has not been
demonstrated. Curved ribbons, arbitrary intersection continuation, nonuniform
metric calibration, automatic motif induction and financial bidding remain
separate future experiments. The present exposed reads and counterexamples
could serve as inputs to those later choices.
