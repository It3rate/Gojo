# Bourgoin constructions as qualitative ornament programs

Experimental study, 2026-10-18. Fourteen authored geometric construction
recipes from Jules Bourgoin's *Théorie de l'ornement* now execute through native
motifs, move words, phase relations and explicit field seams.
The test concerns the construction and its readable correspondences, rather
than whether a finished image resembles a historical plate closely enough.

The [2026-10-19 source-correction follow-up](GojoBourgoinRefinementStudy.md)
supersedes several interpretations below. The shared gallery now includes
corrected constructions, the preserved B06 variant and first-pass comparisons.
This report retains the original encoding findings and counts; its initial
recipe interpretations do not all match the source.

Open the
[Bourgoin patterns](../../experiments/QualitativeFrieze/results/bourgoin-patterns/gallery.html).

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --bourgoin-patterns
```

Each case writes its own native field packet and a plain-text encoding read.
For example, replay a saved honeycomb packet at a different finite window:

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --bourgoin-patterns --input experiments/QualitativeFrieze/results/bourgoin-patterns/B04.gojo.json --columns 5 --rows 5 --triangular-display --output artifacts/bourgoin-replay
```

The explicit display flag is needed because the saved native packet does not
store a screen metric or choose one from the recipe's name.

Every default case is archived and imported before execution. All fourteen
complete their adopted native generation and seam contracts. This establishes
source-derived construction examples, with the geometric and connectivity
qualifications below.

## Source and scope

The source is the 1883 Paris edition, published by Ducher & cie., available as
[Internet Archive item `thoriedelorneme01bourgoog`](https://archive.org/details/thoriedelorneme01bourgoog).
Printed page numbers below identify the book; the linked scan leaves have
different numbers. The previously discussed scan167 is printed p.144, in the
chapter on derived star polygons. The network discussion begins at printed
p.246, scan269.

| Source passage | Construction idea used here | Qualification |
| --- | --- | --- |
| [pp.143–145, derived polygons, figures37–44](https://archive.org/details/thoriedelorneme01bourgoog/page/n166/mode/1up) | Distinguish one continuous star circuit from several intersecting polygon circuits | Regularly spaced contour landmarks and exact geometric turns are additional premises; an intersection does not automatically become a join |
| [pp.202–206, §§125–127](https://archive.org/details/thoriedelorneme01bourgoog/page/n225/mode/1up) | Vary instances while retaining a composition diagram or type | The preserved type needs an explicit structural correspondence; symmetry and detailed outlines need not remain identical |
| [pp.218–222, §130, figures100–111](https://archive.org/details/thoriedelorneme01bourgoog/page/n241/mode/1up) | Repetition, alternation, intercalation, periods and nested recurring arrangements | Bourgoin's recurring arrangements describe motif order; they do not by themselves assert causal feedback between successive events |
| [pp.224–227, §§132–134, figures112–116](https://archive.org/details/thoriedelorneme01bourgoog/page/n247/mode/1up) | Separate the directing arrangement from its motif; compare row organizations by shifting and reflecting their parts | Detached, touching, crossed and articulated motifs are different legitimate constructions, rather than one universal connectivity requirement |
| [pp.246–250, §§140–144, figures173–184](https://archive.org/details/thoriedelorneme01bourgoog/page/n269/mode/1up) | Read or construct a network through its points, lines or cells; compare aligned and alternate rows, centres and diagonals | The book's geometric equalities cannot be inferred from incidence or similar rendered appearance alone |

These are source-derived structural adaptations unless a particular case
explicitly establishes more. They are not traced plates or verified metric
reconstructions. The preceding
[historical ornament research](GojoHistoricalOrnamentRules.md) provides related
construction exercises from Day, Hay and other manuals; it is not independent
corroboration of every Bourgoin classification.

Bourgoin supplies useful distinctions and constructive examples, not a
universal beauty score. His network chapter also does not imply that every
ornament has one periodic lattice or one connected ink component.

## Recipe catalogue

The catalogue separates the source construction from the authored proportions
and display interpretation. Case names make the native definitions referable;
they are not execution opcodes.

| Case | Construction target | Primary source | Fidelity contract |
| --- | --- | --- | --- |
| B01 | Running brick bond | [p.251, figure186](https://archive.org/details/thoriedelorneme01bourgoog/page/n274/mode/1up) | Reconstruct the diagram's arrangement with selected proportions |
| B02 | Basket rectangles and square gaps | [p.252, figure188](https://archive.org/details/thoriedelorneme01bourgoog/page/n275/mode/1up) | Adapt the arrangement and intervening-gap organization |
| B03 | Squares meet diamonds | [p.252, figure189](https://archive.org/details/thoriedelorneme01bourgoog/page/n275/mode/1up) | Adapt the arrangement; regularity requires its own geometric premise |
| B04 | Honeycomb from three reused contours | [p.254, figure196](https://archive.org/details/thoriedelorneme01bourgoog/page/n277/mode/1up) | Reconstruct declared site joins and arrangement with an explicit display basis |
| B05 | Hexagons with triangular interstices | [p.254, figure197](https://archive.org/details/thoriedelorneme01bourgoog/page/n277/mode/1up) | Reconstruct the diagram's relationships with selected proportions |
| B06 | Octagons leave small squares | [p.255, figure201](https://archive.org/details/thoriedelorneme01bourgoog/page/n278/mode/1up) | Reconstruct the mixed-cell diagram without inferring metric regularity |
| B07 | Alternating-width orthogonal bands | [p.256, figure202](https://archive.org/details/thoriedelorneme01bourgoog/page/n279/mode/1up) | Reconstruct the division scheme with authored span participation |
| B08 | Cranked diagonal trellis | [p.256, figure203](https://archive.org/details/thoriedelorneme01bourgoog/page/n279/mode/1up) | Adapt the arrangement and its related directional readings |
| B09 | Greek-key border control | [plate1, figure1](https://archive.org/details/thoriedelorneme01bourgoog/page/n392/mode/1up); explanation p.343 | Source-inspired path reconstruction, rather than exact tracing |
| B10 | Direct bent-cross compartments | [plate9, figure4](https://archive.org/details/thoriedelorneme01bourgoog/page/n408/mode/1up) | Source-inspired reconstruction through repeated component paths |
| B11 | Returned rows of bent crosses | [plate9, figure5](https://archive.org/details/thoriedelorneme01bourgoog/page/n408/mode/1up) | Source-inspired reconstruction with a different return structure |
| B12 | Fourfold spiral-arm field | [plate9, figure10](https://archive.org/details/thoriedelorneme01bourgoog/page/n408/mode/1up) | Source-inspired arm organization, without a traced metric outline |
| B13 | Four-part revolved key scaffold | [plate14, figure3](https://archive.org/details/thoriedelorneme01bourgoog/page/n418/mode/1up) | Source-inspired repeated component construction |
| B14 | Connected meander-grid adaptation | [plate19, figure4](https://archive.org/details/thoriedelorneme01bourgoog/page/n428/mode/1up) | Source-inspired construction, with the density interpretation kept explicit |

The first eight cases exercise networks and compartment arrangements. The
plate-derived cases exercise directional paths, returns and combinations of
parts. B09 repeats a border construction in separate rows; it is a useful
control against assuming that every valid field must be one connected
two-dimensional network.

## Three separate structures

The experiment keeps the reusable recipe, the generated field and its display
interpretation distinct.

**The native program** consists of actual move words, motif definitions,
transform substitutions, finite alternative scopes, parameter bindings and
phase/successor relations. A name identifies a case for discussion; it does not
select its execution. Shared unmeasured reach participation can constrain
different uses without giving that reach a measured value.

The authoring helpers use integer lattice descriptions and compile them into
repeated participation in one shared reach. The resulting words therefore
retain exact finite counts and formal displacement relationships. This is
stronger than bare topology or an above/below detector. Having no numeric
carrier elements does not mean that all relative size information has been
discarded; the claim is uncalibrated structural execution, rather than an
absence of counting anywhere in construction.

B12 and B13 additionally retain a native four-part construction. A seed part
produces subsequent parts through actual word-substitution receipts. Each part
selects real executable path/chord participation and its actual prefix/start
correspondence; phase bindings and return witnesses connect the cycle back to
the seed. B12 retains four generated paths. B13 retains four selected chord
groups within one closed path. This evidence survives transport with the
source, rather than relying on a description saying that the shape is fourfold.
The builders check these receipts and part-start correspondences before
publishing them. Restoring this provenance is separate from providing a general
validator for arbitrary construction-evidence claims in an imported packet.

**The field** contains actual occurrence identities and any declared seam or
shared-site relations. A periodic disposition can organize detached motifs.
A continuous scaffold makes the stronger claim that selected passages really
join. Both constructions need an inspectable account of how their participants
relate, but only the latter promises continuous passage through the joins.

**The display** interprets the accepted native words and sites in a selected
Cartesian or triangular presentation basis. It supplies screen coordinates,
colours and thickness. A triangular-looking render does not establish
equilateral native geometry; a four-edge contour does not establish a regular
square. Direction, shared site, formal displacement, calibrated distance and
screen position remain different reads.

The earlier [executable ornament format](GojoOrnamentProgramEncoding.md)
establishes the source/sample distinction and archive-only execution. The
Bourgoin study exercises a broader authored vocabulary; it does not claim
that the vocabulary or the construction recipes were learned.

`OrnamentFieldPacket` transports the program, selected bindings, finite window
and companion seam contract as one reference graph. Import preserves their
within-world aliases, restores fresh identities and retains the explicit
standard-frame correspondence. Source citations and display-basis coefficients
stay in the presentation catalogue. Replaying a packet does not require the
original source recipe factory; loading and subsequent execution/seam
validation remain separate stages.

`QualitativeWordOrbit` supplies a separate finite question about a substitution:
apply it along each actual successor in a declared cycle, then compare the
terminal word's formal displacement with the seed through the frame's native
difference read.
Its steps retain the selected seed, actual phase transition and actual
substitution receipt. Successful returns, known nonreturns, blocked moves and
open reads remain separate. This return is not literal word identity or proof
that a drawn path's shape is unchanged. A four- or six-step return for supplied
words does not by itself prove a Euclidean angle or establish the transform's
order for every possible input.

## Cohesion is an explicit contract

A useful finite field should account for the relationships its recipe adopts.
For a seam-bearing network, this includes actual endpoint publications,
compatible continuation, unambiguous shared-site correspondence and consistent
two-direction composition. For a detached-motif field, it instead includes the
declared phase, placement and transform correspondences. The absence of seam
obligations is not proof of a connected network, and it need not be a defect in
a detached arrangement.

`OrnamentProgramField` reads a companion contract over the actual program and
its two cycles. Each rule selects source and target motif/transform identities,
two public endpoint roles and a finite signed offset word. A step refers to an
actual cycle direction or its ordered reverse. It walks the sampled occurrence
slot order; its integer authoring helper does not supply a measured distance.
The same contract can therefore be reused for a different finite window.

The library's seam authoring helper adopts a deliberate policy: join every
pair of exposed endpoints whose transformed formal positions agree within a
fixed neighbour stencil. That stencil includes the same cell, the next column,
the next row and both adjacent diagonals. A shared-reach integer index prunes
candidate pairs; a native translated-word difference read must then be zero
before the helper emits the explicit rule. Those rule rows are frozen in the
packet. The live assembler subsequently checks the actual current endpoint
publications and bindings again.

This policy is part of this authored recipe library. It does not infer
physical identity from a renderer, discover a designer's intention from an
image or join a stroke's interior merely because another line passes through
it. The helper compiles the transforms selected by the original bindings;
changing to another advertised transform may require a new seam contract if
the same connectivity goal is to be retained.

Only current authenticated publications can admit a seam. Raw endpoint
publications are mapped through each unit's already justified contour joins
before a field-level join is declared. A missing finite neighbour is a frontier
read. An existing neighbour with incompatible endpoint evidence is blocked;
a changed runtime head leaves stale source applications unresolved. A rule
whose target motif/transform condition is false remains inactive rather than
inventing a replacement.

These seams certify the selected site identifications. They do not by
themselves certify tangent smoothness, merged edge identity or bounded faces.
A complete field read means its adopted obligations have settled; that property
alone does not prove that its ink is one connected component.

Two paths that reach equal formal positions are still distinct until an actual
join is justified. Conversely, a projected crossing may preserve independent
threads. Rendering cannot choose between those topologies.

Finite sampling boundaries also differ from broken interior connections.
Retain the boundary cut and its scope, rather than reporting every cropped
passage as an invalid ornament or claiming that a finite patch proves an
unbounded tiling. A repeatability claim needs the appropriate periodic seam
and corner obligations.

When adjacent units both supply an edge, a shared-edge read must retain the
actual occurrence-to-edge correspondence and incidence. Drawing their strokes
on top of each other does not establish that fold. Repeated drawing histories
also remain distinct from a later observational identification of shared
support. Any implementation limitation in this area must remain visible in
the case results.

## Checks that matter

The following controls distinguish a readable construction from a pleasing
projection of unrelated copies:

| Check | Positive evidence | Counterexample or unresolved case |
| --- | --- | --- |
| Shared recipe, separate occurrences | Instances reuse the actual definition while their runtime heads and sites remain fresh | Reusing one mutable machine or silently identifying equal-position sites |
| Declared continuous passage | Actual seam/site correspondences support the required route | Identical rendered endpoints without an accepted join |
| Two-direction consistency | The actual U-then-V and V-then-U seam paths agree at the declared corner | A local seam fits while the composed corner disagrees |
| Repeat phase | Actual successor links select the recurring motif/transform sequence | A missing or conflicting rule, rather than a default motif |
| Changed observation window | The selected phase origin and finite cut change while source correspondences survive | Mistaking a different cut for a different source or a cropped boundary for an interior defect |
| Preserved type under variation | A changed instance retains the declared coarse diagram correspondence | A fine edit destroys a required passage but is still labelled a refinement |
| Point/line/cell readings | Alternative organizations retain actual source incidence and their distinct complements | Treating common ancestry or a similar picture as proof of correspondence |

These are structural requirements. Uniform physical gaps, regular angles,
balanced visual weight and human preference need separate measured or
perceptual premises. Report supported, blocked and unresolved obligations
before combining any optional preferences.

## Present limits

This study is authored generation and structural validation. It does not learn
the recipes from the scanned book, recognize arbitrary ornament images or
prove a universal geometry of attractive patterns.

The finite native words and substitutions have a declared frame. Curves,
general continuous deformations and arbitrary angular generators are separate
extensions. Selecting a triangular display basis supplies a presentation, not
an inferred equality of angles or lengths. Shared-edge folding, where required,
needs its own actual correspondence rather than coordinate deduplication.

The shared execution unit is currently the authored macrocell. B12/B13 retain
native inner derivations, while other helpers such as repeated hexagonal
contours still compile into ordinary paths. The execution format does not yet
offer a general recursive child-program call system. Retained construction
evidence and separately shared executable child definitions are different
capabilities; further factoring is useful for both compactness and recognition.

A library of successful finite constructions also does not establish
indefinite non-overlap, full wallpaper-group classification, uniform clearance
or an optimal aesthetic profile. Alternative causal recipes can produce
corresponding outputs while retaining different identities and histories.

The companion field read observes declared joins among generated unit outputs.
It does not by itself train parameter choices, repair a blocked motif or move
material through a new seam. Connecting these source recipes to the earlier
live negotiation and adaptation experiments requires those explicit response
and settlement routes.

## First-pass results and verification

The [first-pass diagnostics](../../experiments/QualitativeFrieze/results/bourgoin-patterns/first-pass/diagnostics.json)
record 220 completed units, 4,064 native strokes and 1,865 accepted seam
declarations across the fourteen default fields. Every case has zero blocked
and zero unresolved entries. The count of
accepted seam declarations includes both within-cell and between-cell joins;
it is not a count of independent geometric facts.

| Case | Units | Strokes | Ink components | Accepted seams | Finite frontier | Repeated endpoint pairs |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| B01 | 16 | 96 | 1 | 108 | 20 | 0 |
| B02 | 16 | 384 | 1 | 459 | 85 | 136 |
| B03 | 16 | 384 | 1 | 204 | 20 | 0 |
| B04 | 16 | 288 | 1 | 351 | 81 | 113 |
| B05 | 16 | 96 | 1 | 61 | 19 | 0 |
| B06 | 16 | 128 | 1 | 76 | 20 | 24 |
| B07 | 16 | 128 | 1 | 169 | 39 | 0 |
| B08 | 16 | 160 | 1 | 73 | 23 | 0 |
| B09 | 12 | 240 | 9 | 27 | 9 | 0 |
| B10 | 16 | 160 | 1 | 73 | 23 | 0 |
| B11 | 16 | 160 | 1 | 73 | 54 | 0 |
| B12 | 16 | 512 | 40 | 24 | 8 | 0 |
| B13 | 16 | 1,024 | 1 | 118 | 42 | 0 |
| B14 | 16 | 304 | 1 | 49 | 15 | 0 |

Twelve fields have one connected ink component. B09 deliberately keeps the key
passage and two rails separate in each of three rows. B12 keeps its inward
spiral ends: neighbouring outer endpoints join, while the result has forty
components. Neither result is silently relabelled as one continuous network.
The more complete star-compartment construction of B12's source remains outside
this adaptation's fidelity claim.

Repeated endpoint pairs count retained parallel stroke participations between
the same joined native endpoints. B02, B04 and B06 make the missing shared-edge
fold especially visible. Their component counts do not certify the source's
cell-edge complex or bounded face structure. Likewise, a graph's cycle rank is
not automatically a number of filled faces.

Every saved source packet contains only relations and placeholders. Current
packet graphs range from 554 to 8,994 distinct objects; the minified JSON files
range from 41,514 to 708,286 UTF-8 bytes. B12's retained construction evidence
uses a 344,093-byte packet; B13's richer sixty-four-chord contour and evidence
use 708,286 bytes. These are retained-evidence descriptions, rather than minimal
recursive recipes or a storage/computation benchmark.

`dotnet test Gojo.slnx` passed all 3,982 tests across twelve test assemblies,
with no failures or skipped tests. This checkpoint adds 39 cases: fourteen
field, ten packet, nine word-orbit and six library cases. They cover actual
finite returns and nonreturns, open/illegal substitutions, endpoint
authentication, closure-site mapping, wrong pitch, stale heads, retained
parallel strokes, malformed native-run claims and archive-only execution.

Four per-family controls cover B01, B04, B06 and B14: removing cross-cell seams
while retaining valid local joins must lose the target field's cross-cell
connectivity. This prevents a complete local motif or an internal seam count
from masquerading as evidence of the intended larger network.

Fresh-process replay checks used only saved packets, with newly selected finite
windows:

| Imported packet and sampling window | Units | Strokes | Accepted seams | Components | Blocked / unresolved |
| --- | ---: | ---: | ---: | ---: | ---: |
| B04, 3 columns by 2 rows, triangular display | 6 | 108 | 113 | 1 | 0 / 0 |
| B13, 2 columns by 2 rows | 4 | 256 | 20 | 1 | 0 / 0 |

The gallery's fourteen cards, controls, source images, local links and SVG
outputs passed browser checks at desktop and narrow-screen sizes. These checks
verify the presentation and access to evidence; they do not validate historical
fidelity or human aesthetic preference.

Solution-wide verification uses:

```powershell
dotnet test Gojo.slnx
```

The next recognition experiment should freeze these native recipes, generate
fresh fields and expose only the intended observation surface. It should retain
detached-motif and joined-network controls, hide source recipe labels, and test
which larger descriptions survive local perturbations. That tests transfer of
structural understanding without presenting a source-generated example as an
already solved recognition problem.
