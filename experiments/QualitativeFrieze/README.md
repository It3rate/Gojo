# Qualitative frieze generators

The [refined collection](results/ornament-refined/gallery.html) applies the
stronger detail vocabulary across 108 designs, with selected curve rendering,
angular and one-unit views, and a [single sheet](results/ornament-refined/overview.html).
The [three curve studies](results/ornament-curves/gallery.html) isolate vines,
a corrected curl and an interrupted guilloche. Run `--ornament-refined` or
`--ornament-curves` with the experiment project.

The [B08–B14 interior gallery](results/ornament-refined/interiors-gallery.html)
focuses on compartment-filling motifs, larger eight-petal flowers and native
four-colour selections. Its [single sheet](results/ornament-refined/interiors.html)
contains 24 designs. Regenerate these with `--ornament-refined --only B08,B08.B,B09,B10,B11,B12,B13,B14`.

The [detail study](results/ornament-details/gallery.html) explores ten richer
native motifs and four integrations into B02/B05, with close-ups, scaffold
reads and a [single sheet](results/ornament-details/overview.html). Run it with
`dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-details`.

The [full colour collection](results/ornament-designs/gallery.html) extends all
36 Bourgoin skeletons into 108 distinct designs. Compare broad geometric
fields, intermediate flowers/foliage and fine local enrichment on the
[single large page](results/ornament-designs/overview.html), or zoom the
combined SVG. The [study](../../Gojo/docs/GojoOrnamentDesignCollection.md)
separates native construction and landmark attachments from display choices.

~~~powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-designs
~~~

Authored generators combine the existing qualitative phase machinery with
explicit event routes, unmeasured shared reaches and sampling connections.
Their native source graphs contain relations and placeholders, with no carriers
or numeric locations. The authored gallery below is the generation checkpoint;
a following bounded feedback experiment adds explicit legality and preference
reads without changing the kernel.

The [Bourgoin source atlas](results/bourgoin-patterns/gallery.html) compares
thirty-six native constructions with reviewed source diagrams. The
[whole atlas](results/bourgoin-patterns/overview.html) shows them together.
Its [contact and course study](../../Gojo/docs/GojoBourgoinContactAndCourseStudy.md)
examines partial-side slides, boundary refinement and repeated open courses;
earlier constructions remain available beside the source overlays.

The [coloured ornament collection](results/coloured-ornament/gallery.html)
enriches four of those native skeletons through actual exposed landmarks.
Overall, motif and detail focuses retain their siblings while changing
visual prominence. See the [study](../../Gojo/docs/GojoColouredOrnamentStudy.md)
and [Jones research notes](research/jones-colour-notes.md).

~~~powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --bourgoin-colour
~~~

The [primitive flow experiment](../../Gojo/docs/GojoPrimitiveFlowExperiment.md)
executes steps, staircases, waves, rectangles, diamonds, meanders, herringbone
and stars through acknowledged leaf/group calls and existing event sampling.
Sixteen action recipes and three sampling examples compare different causal
constructions, limited completion ports and explicit contour joins.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --primitive-flows
```

Open the [primitive flow gallery](results/primitive-flows/gallery.html) and
[diagnostics](results/primitive-flows/diagnostics.json).

The [skeleton atlas](results/skeleton-atlas/index.html) provides ten named
families with thirty progressive diagrams, twelve goal skeletons and a short
experiment-brief builder. These are authored reference targets for discussion,
with source links, required features, allowed variation and explicit misses.
See the [atlas source and rebuild instructions](SkeletonAtlas/README.md).

The [historical ornament research](../../Gojo/docs/GojoHistoricalOrnamentRules.md)
extracts construction rules and reusable skeletons from Day, Hay, Jones and the
International Correspondence Schools textbook. It separates seam obligations,
motif/field classification and declared aesthetic preferences, and proposes
cut-and-shift repair as the next experiment. These are research directions,
not additional implemented generator modes.

The [adaptive wallpaper checkpoint](../../Gojo/docs/GojoPatternMachineExperiment.md#adaptive-motifs-and-renegotiated-seams-checkpoint-2026-10-17)
lets boundary feedback replace active motifs and publish new ports. Persistent
queues wait for fresh, current-head seam agreements. Four examples range from
two-neighbour repair to symmetric cushion, key and rosette fields; four held
central rosette seams remain blocked with their packets visible.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --adaptive-wallpaper
```

Open the [adaptive gallery](results/adaptive-wallpaper/gallery.html),
[figure](results/adaptive-wallpaper/gallery.svg),
[preview](results/adaptive-wallpaper/gallery-preview.png)
and [diagnostics](results/adaptive-wallpaper/diagnostics.json).
Actual motif/port/seam correspondences verify formal quarter-turn symmetry of
the three centred fields. A captured native feasibility profile distinguishes
accounting, required seam agreement and resolved choices; it is not a beauty
score. See [where training fits](../../Gojo/docs/GojoAdaptiveWallpaperTraining.md)
for genetic, market and transformer proposal/selection roles and controls.

The [live junction feedback checkpoint](../../Gojo/docs/GojoPatternMachineExperiment.md#live-junction-feedback-checkpoint-2026-10-17)
feeds accepted neighbour seams into conserved packet passages, delayed delivery,
local readiness questions and acknowledged drawing responses. Three paired
examples show circulation, two-neighbour coincidence and a neighbour selecting
the opposite diagonal. Their controls cut an actual seam or withhold one seed.
All execution remains pre-numeric; the captured substrate stays fixed while
separate local response traces grow.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --junction-feedback
```

Open the [interactive feedback gallery](results/junction-feedback/gallery.html),
[figure](results/junction-feedback/gallery.svg),
[preview](results/junction-feedback/gallery-preview.png)
and [ownership diagnostics](results/junction-feedback/diagnostics.json).
The round slider exposes actual historical states; feedback routes and response
colours are independently inspectable.

The [connected wallpaper fields](../../Gojo/docs/GojoPatternMachineExperiment.md)
add declared motif ports, checked neighbour periods and a derived shared-site
view of actual machine output. Six 5×4 examples include continuous keys,
diamonds, crossing/junction contrasts, hexagonal rings and six-point stars.
Native word substitution supplies an order-six action; the 60° screen basis
is an optional measured interpretation.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --wallpaper-field
```

Open the [field gallery](results/wallpaper-field/gallery.html),
[figure](results/wallpaper-field/gallery.svg),
[preview](results/wallpaper-field/gallery-preview.png)
and [diagnostics](results/wallpaper-field/diagnostics.json).

The [composed wallpaper checkpoint](../../Gojo/docs/GojoPatternMachineExperiment.md)
connects motif completion to an individually acknowledged cell passage and
admits the next motif only after accepted placement. Four live 4×3 patches
combine quarter-turns, mirrors, glide half-periods and independent row moves.
The native run contains no numeric carriers or preconstructed output sites.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --wallpaper-machine
```

See its [interactive gallery](results/wallpaper-machine/gallery.html),
[figure](results/wallpaper-machine/gallery.svg),
[preview](results/wallpaper-machine/gallery-preview.png)
and [diagnostics](results/wallpaper-machine/diagnostics.json).

The [wallpaper flow checkpoint](../../Gojo/docs/GojoPatternMachineExperiment.md)
adds live cross-axis direction actions, threshold-driven quarter-turn hooks,
glide staples and independent X/Y placement ports. It retains actual receiver
receipts, unmeasured half-period correspondence and the nonzero result of two
glides. It is a foothold toward a composed wallpaper machine, not a full group
classification.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --wallpaper-flow
```

Open its [flow gallery](results/wallpaper-flow/gallery.html) and
[diagnostics](results/wallpaper-flow/diagnostics.json).

The [qualitative square-cell checkpoint](../../Gojo/docs/GojoQualitativeSquareCellExperiment.md)
connects a marked unmeasured contour, axis/sign actions and two independent
periodic seams. Three generated fields produce eight square/stretch/lean views.
The checks separate actual motif symmetry, structural correspondence and the
optional Euclidean metric. A native two-direction patch joins sites through
seam rows before interpreting any coordinates.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --square-cell
```

See its [gallery](results/square-cell/gallery.html),
[figure](results/square-cell/gallery.svg),
[mechanics](results/square-cell/mechanics.svg)
and [diagnostics](results/square-cell/diagnostics.json).

The [operating-machine checkpoint](../../Gojo/docs/GojoPatternMachineExperiment.md)
adds packet reservoirs, threshold questions and stateful drawing muscles.
The machines create fresh strokes and sites while running: independent pumps
combine into diagonal chords, released packets can fill a second reservoir,
and control muscles mirror future output or reset, jump and reanchor the cursor.
Their definition contains local laws and initial states rather than output
coordinates. Discrete occupancy remains separate from continuous carrier tension.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --machines
```

See the [machine gallery and causal events](results/machines/gallery.html),
[figure](results/machines/gallery.svg),
[preview](results/machines/gallery-preview.png)
and [packet diagnostics](results/machines/diagnostics.json).

The [connected-pattern checkpoint](../../Gojo/docs/GojoConnectedPatternExperiment.md)
returns to elementary generators: four ordered levels, diagonal chords,
shared-site junctions, native graph correspondences and coarse conditional
ports. Eight constructions and eight one-phase variations expose separate
native and measured aesthetic profiles. An independent 6/3-phase supplier pair
drives the conditional ladder. Unknown input stays unranked.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --connected
```

See its [interactive gallery](results/connected/gallery.html),
[main figure](results/connected/gallery.svg),
[ranked comparison](results/connected/ranked.svg),
[calibration alternatives](results/connected/refined.svg),
[layer diagram](results/connected/layers.svg)
and [diagnostics](results/connected/diagnostics.json).
The [aesthetic rule vocabulary](../../Gojo/docs/GojoPatternAestheticRules.md)
separates primary research from the authored profile and records a dense-ink
counterexample that motivated adding crowding as a separate criterion.

The [rhythm-to-frieze checkpoint](../../Gojo/docs/GojoRhythmFriezeExperiment.md)
uses onset roles and an explicit phrase phase to select whole sequences of
turns, backward reaches and returns. The same rhythm can select several
different fret grammars; source slots, selection laws and emitted stroke
phases remain connected. Expansion preserves event order rather than musical
duration. The gallery includes phrase colours, black/white inversion, exact
finite-strip fill and periodic centerline checks.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --rhythm-friezes
```

See the [interactive gallery](results/rhythm-friezes/gallery.html),
[SVG](results/rhythm-friezes/gallery.svg),
[preview](results/rhythm-friezes/gallery-preview.png)
and [diagnostics](results/rhythm-friezes/diagnostics.json).
The [phrase-colour figure](results/rhythm-friezes/phrases.svg) and
[colour preview](results/rhythm-friezes/phrases-preview.png) expose the selected
phrase identities directly.

The [rhythm and diagonal checkpoint](../../Gojo/docs/GojoRhythmPatternExperiment.md)
adds exact thick diagonal square-brush footprints, slow/fast/middle phase
compositions, onset-to-pulse correspondences, local colour views and descriptive
word/coordination features. The [research note](../../Gojo/docs/GojoPatternFitnessResearch.md)
grounds future aesthetic fitness work in six primary studies while retaining
context and observer differences.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --rhythms
```

See the [interactive gallery](results/rhythms/gallery.html),
[main preview](results/rhythms/gallery-preview.png),
[style controls](results/rhythms/styles.svg),
[feature counterexamples](results/rhythms/controls.svg)
and [diagnostics](results/rhythms/diagnostics.json).

The [layer-analysis checkpoint](../../Gojo/docs/GojoPatternLayerExperiment.md)
connects rhythmic drives, sampling, actual contributions, ordinal quiet regions,
multiple field support, explicit union/subtraction passes and reusable motif
correspondences. It preserves separate histories and both field roles.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --layers
```

See its [interactive gallery](results/layers/gallery.html),
[SVG](results/layers/gallery.svg), [diagnostics](results/layers/diagnostics.json)
and [preview](results/layers/gallery-preview.png). The edits and preferences are
authored; this supplies analysis mechanics for later intentional training.
The recorded comparison retains an all-dark counterexample to filling every
quiet plateau, a one-landmark addition, and union/subtraction of a shifted
motif. Equal dark/light counts can return while the original role exchange
fails. Compact source programs and complete retained proof graphs remain
different storage promises.

The [pattern-shaping checkpoint](../../Gojo/docs/GojoPatternShapingExperiment.md)
adds eight deliberately authored centerline programs and explicit two-role
partitions inspired by the user's fret/meander references. It uses no random
search. Shared unmeasured reaches, repeated actual moves and a native
phase-origin edit retain source meaning; measured ribbon width and finite-strip
geometry remain separate interpretations. Both black and white are actual
participating roles. Equal width or equal counts alone do not prove a
color-exchanging symmetry: an explicit bijection must exchange every role and
preserve directed adjacency.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --shape --output experiments/QualitativeFrieze/results/shaping
```

The shaping output targets are the [SVG gallery](results/shaping/gallery.svg),
[HTML gallery](results/shaping/gallery.html), [diagnostics](results/shaping/diagnostics.json),
[thin/equal/heavy styles](results/shaping/styles.svg),
[phase-origin comparison](results/shaping/centering.svg)
and [PNG overview](results/shaping/gallery-preview.png).
The recorded catalog has eleven constructions, including three proved native
color exchanges. The phase-origin edit changes boundary margins from 17/1 to
9/9 cells. All three figures were visually checked. The report distinguishes
the compact stroke source from the larger explicit cell field and separates
native role exchange from measured geometric and area claims.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --output experiments/QualitativeFrieze/results
```

The gallery compares a coupled and directly sequenced square wave, a Greek-key
meander, slow/fast motions with drift under two sampling arrangements, three
pauses followed by movement, and alternating short/long reaches. Two explicit
positive measured interpretations render the same qualitative traces.

See [the experiment report](../../Gojo/docs/GojoQualitativeFriezeExperiment.md),
the [SVG gallery](results/gallery.svg), [HTML gallery](results/gallery.html)
and [diagnostics](results/diagnostics.json).
The [PNG preview](results/gallery-preview.png) shows the visually checked gallery.

The [learning experiment report](../../Gojo/docs/GojoFriezeLearningExperiment.md)
describes the next checkpoint: phase-cap and local retracing certificates,
global measured overlap/crossing checks, finite probe objectives and bounded
edits to phase-to-word bindings. The recorded run evaluates 720 candidates,
finds legal examples in all three families, and includes both crossed and
uncrossed examples. A local reversal check is not a global no-retracing proof;
finite probe coverage is not continuous area coverage.

Run the bounded feedback search with:

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --learn --trials 240 --seed 431 --output experiments/QualitativeFrieze/results/learning
```

The output targets are the [learning SVG](results/learning/gallery.svg),
[HTML](results/learning/gallery.html) and [diagnostics](results/learning/diagnostics.json).
The [learning preview](results/learning/gallery-preview.png) was visually checked.
The three profiles reorder fixed motion-word
bags; the meander bag includes one westward word, without a separate meander
reward. Complexity is a hard phase cap. Numeric utility reads probe occupancy,
clearance spread and symmetry; the secondary aspect ratio checks hard geometry
without recomputing that utility. Native search and archive surfaces retain
the grammar, budgets, actual archive transitions and ranking evidence.
The utility's weights are fixed. Phase structure, event routes, sampling,
reach identities and word multiplicities remain unchanged while word order
is searched. Complete retained witnesses can be much larger than the source
generator; this experiment makes no performance or compact-memory claim.

Crossings are properties of continuous periodic ink: two noncollinear
through-lines form a crossing even when they meet at sampled endpoints.
Ordinary joins and T contacts remain distinct from crossings and from
positive-length retracing. This is a straight-through ink criterion; general
bent-strand interleaving needs a separate path-continuation read. X junctions
with extra branches remain crossing sites, with all incident rays retained.
The measured source boundary currently requires whole calibrated source
ticks; fractional source points remain unresolved. Internal intersections and
symmetry maps still retain exact fractions. A finer explicit unit can make
fractional lengths representable as whole source ticks.

`FriezeFixture.Surface` connects the event network, motion frame, output law,
sampling routes, initial phases and reach-order obligations. `Run` bounds an
observation window; phase edges and trigger routes decide what fires. Motion
applications and committed samples remain separate. A sampled chord can hide
intermediate motion without removing it from the native trace.

`FriezeRealization` is a separate interpretation adapter. Its native calibration
binds actual axis/reach pairs to positive measured spans. Display names, pixels
and diagnostic counts are derived presentation data. They do not select source
behavior. A drawing is evidence for that calibration, rather than a proof of
all geometric symmetries or all possible unmeasured embeddings.

Run the focused experiments with:

```powershell
dotnet test Tests/QualitativeExecution.Tests -c Release --filter "FullyQualifiedName~Frieze|FullyQualifiedName~QualitativeCoverageTests|FullyQualifiedName~PhaseEventNetworkTests|FullyQualifiedName~QualitativeMotionFrameTests|FullyQualifiedName~PatternShapingLibraryTests|FullyQualifiedName~QualitativePartitionTests|FullyQualifiedName~QualitativeWeaveTests|FullyQualifiedName~RibbonPartitionSupplierTests"
```

## Hierarchical recognition

The [layered study](../../Gojo/docs/GojoHierarchicalRecognition.md) reads fine
motifs as chamber roles, then lifts whole row programs into fresh participants
for the same finite phase-graph reader at the next level. An upper unresolved
claim can query a row service, which queries a motif service, preserving actual
contrary answers, ambiguous alternatives, capacity blocks and causal timelines.
The [interactive gallery](results/hierarchical-recognition/gallery.html)
provides initial/final stages and fine, cell-role and row-program views.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --hierarchical-recognition
```

The primitive vocabulary and layout are induced from the full observation;
sequence programs alone use disjoint training and held-out instances. This is
bounded symbolic induction and observational refinement, not gradient training
or material tension transfer. Detailed proofs stay on private audit surfaces;
public layers communicate through fresh identities, prototypes and ports.

## Executable ornament programs

The [experimental v0 contract](../../Gojo/docs/GojoOrnamentProgramEncoding.md)
stores shared motif path programs, explicit transforms, finite parameter ports
and periodic disposition rules as one native graph. Bindings and a finite
sampling window select a patch. The
[six-sample gallery](results/ornament-program/gallery.html) includes a native
archive and a graph-derived readable inspection. Standalone replay loads and
executes that archive without invoking the source motif factory.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-program
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-program --input experiments/QualitativeFrieze/results/ornament-program/program.gojo.json --output artifacts/ornament-program-replay
```

The format is a narrow executable experiment rather than a frozen dataset
standard. Recognition services and negotiated seams remain separate mechanisms.
