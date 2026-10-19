# A grammar in colour: three designs from every skeleton

The [browsing gallery](../../experiments/QualitativeFrieze/results/ornament-designs/gallery.html)
and [single-page collection](../../experiments/QualitativeFrieze/results/ornament-designs/overview.html)
extend all 36 retained Bourgoin skeletons into three different native ornament
designs each. The combined SVG is a zoomable sheet of all 108 designs.
The earlier [four-scene focus study](GojoColouredOrnamentStudy.md) remains
available as a smaller checkpoint.

## Three distances become three constructions

The earlier collection changed emphasis among three layers of one enriched
design. This collection makes three separate enrichment programs from the
same actual source skeleton:

| Leading scale | Construction tendency | Subsidiary detail |
| --- | --- | --- |
| Overall disposition | Broad outlined bands, cartouches, nested panels and medallions | Small centre seeds and quiet subdivisions |
| Intermediate motif | Repeated tulips, leaf sprays, trefoils, fans and radial flowers | Stems, centre rings and quieter scaffold |
| Fine ornament | Small paired leaflets at published turns, buds, pearls and veins | Carried skeleton and restrained centre markers |

The three versions have different introduced path sets even in one ink.
Switching designs selects a different native program, rather than only
adjusting opacity. Within each program, all three native layers still have
complete focus reads with their siblings carried.

The source recipe is shared by the three alternatives. Every source path
appears once in each result with its actual identity retained. Added ornament
does not erase detached ends, sliding disposition, explicit joins or the
distinction between a crossing and a joined site.

## A finer unmeasured authoring vocabulary

Small skeletons need room for detail. The new authoring adapter substitutes
four repeated participations for one prior directional move:

~~~text
actual coarse move -> Closed(move, move, move, move)
source start/chord word -> native substitution read -> finer word
actual source path/port -> corresponding finer path/port
coarse pitch -> finer pitch
~~~

Each fine motif retains its original motif, actual substitution surface,
path/word correspondences and complete substitution receipts. Layout phases
and unitary orientation transforms retain their actual identities. Parameter
domains, pitch bindings and seam program references are rebound explicitly.
Shared words, port roles, domains, selectors and repeated offset steps retain
their aliases.

This is a deliberate finer authoring grid, not a discovered sensor subdivision
or new numeric calibration law. Dividing the optional display basis by four
keeps the visible skeleton unchanged. No numeric carrier or location enters
the native program. Factor 1 preserves the actual recipe; the host adapter
bounds requested expansion to 1–16 to keep allocation deliberate.

The adapter exposes its full installation correspondence on its own surface.
The exported ordinary field packet retains the finer executable program,
motif-level coarse construction and substitution evidence. It does not claim
to archive the adapter's entire installation history.

## Reusable ornament and historical guidance

The native vocabulary includes lens leaves, lanceolate leaves, tulips,
palmette fans, trefoils, four/six/eight-part rosettes, geometric panels, seeds,
faceted round markers and veins. Actual prototype sharing and native
substitution images remain in the evidence.

Authoring code may inspect ordinal extents or select centres and sizes.
Those choices are intentional premises, not inferred native face centres or
discovered aesthetic laws. The resulting offset words and finite contours
carry the executable geometry. Source-family choices help author a recipe;
runtime does not dispatch on those labels.

Each addition uses an actual published source port plus an explicit offset.
Native reductions compare that result with the actual added path start.
An attachment is a non-draining correspondence, not an automatic material join
or tangent law.

The [Jones source notes](../../experiments/QualitativeFrieze/research/jones-colour-notes.md)
and [art direction](../../experiments/QualitativeFrieze/research/ornament-design-directions.md)
give the adopted preferences: construction before enrichment, a legible
distant pattern, geometric conventionalisation, growth from a parent, small
strong accents against quieter masses, and separating outlines.
These guide authorship; they are not universal beauty laws, validated fitness
scores or pigment reconstructions.

Palettes include peacock/coral, aubergine/peach, oxblood/sage, charcoal/orange,
blush/petrol, ultramarine/vermilion and quieter alternatives.
Visual review changed broad designs that initially repeated the intermediate
flower into architectural panels or geometric medallions. Intermediate
versions keep the botanical silhouette; fine versions distribute smaller
growth. White space remains a useful counterform.

The small meshes exposed another concrete failure. Individually small
leaflets accumulated visually at repeated grid positions into broad bands, so a proposed fine
design became a competing overall pattern. B15–B18 now use only two or three
small corner/centre accents per unit. Earlier B15/B16 fine SVGs and their
native packets remain under results/ornament-designs/first-fine/. This is an
authored correction to the intended hierarchy, rather than a claim that the
emergent band pattern was intrinsically wrong.

## Native execution and display interpretation

Every design is exported, decompressed and imported before execution. Native
layers and focus reads are recovered without the authoring factory.
The renderer follows actual placed-path correspondence and acknowledged
receipts. Every displayed chord has an accepted native draw action.
Each design packet is standalone; importing separate packets does not assert
shared physical identity between their independently restored worlds.

Colour, width, separating edges, contour fill and crop are presentation
interpretations. A filled contour is not a native occupied-area proof.
Finite curved-looking silhouettes do not establish analytic curvature,
tangency or equal angles. Colour does not establish knot order.
Closed masses are painted once in their coarsest selected layer before
outlines and details, preserving overlapping non-draining selections.
Fine designs additionally lower the opacity of the source's closed mass
fills while retaining its outlines, so a large panel does not overwhelm
small accents. Collection assembly can refresh this presentation rule
without rerunning or modifying the saved native programs.

This is an authored construction collection. Recognition, learned aesthetics,
automatic motif fitting, general clearance solving and adaptive learning
remain separate experiments.

## Reproduction and evidence

~~~powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-designs
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-designs --only B01,B26 --output artifacts/two-ornament-families
dotnet test Gojo.slnx -c Release
~~~

The default sampling window is 5×5; 2–8 rows/columns are supported.
For parallel batches use disjoint source lists, one output directory and
--plates-only, then --assemble after all batches finish. Assembly uses
completed display records and SVGs; it does not infer or rerun native success.
--modes field,motif,detail selects a subset for individual plates.
--resume reuses completed records only when their native sampling dimensions
and required files match. It assumes unchanged authoring rules; after an
intentional design change regenerate the affected modes without --resume.

Each design has full and construction SVGs, a gzip-compressed native field
packet, compressed native focus reads and a display/diagnostic record.
Gzip wraps the existing JSON format losslessly. Decompress .gojo.json.gz
before passing its JSON to OrnamentFieldPacket.TryLoad. The gallery displays
SVG directly. Diagnostic records do not own canonical meaning.

Tests check all 36 skeletons before/after finer authoring, actual ports and
aliases, source/substitution receipts, archive-only neighbour seams, all 108
distinct enrichment sets, and preservation of every source path. Retained
diagnostics and browser verification record the final execution and display
checks.

## Execution cost found while scaling the collection

The larger collection exposed repeated authentication of the same historical
drawing states and receipts. A bounded synchronous read scope now reuses
successful exact reads during one local unit build. New states and actions
are still checked, all native evidence remains, and the cache is discarded
at scope disposal. Public ordinary reads remain fresh. Scoped execution
assumes its input graph is not concurrently retargeted.

Per-read ledger indexes retain duplicate-event and duplicate-site rejection.
An additional ephemeral direction index skips searches for absent opposite
faces and skips survivor-pair scans when no axis has both faces. Ordered
cancellation and mixed-reach unresolved evidence retain their original paths.
Neither optimisation is a new motion law or a replacement for native proof.

Before the direction optimisation, the scoped-read change reduced a
representative B01 intermediate 3×3 generation from 33.51 s to 6.53 s in the
local diagnostic. Its field assembly fell from 0.50 s to 0.25 s. These are
single-machine diagnostic timings, not a new controlled benchmark.
Scope/topology equivalence, stale commits, forged receipts, disposal,
duplicate events/bindings, ordered cancellation and mixed-reach comparisons
have focused checks, followed by the full solution suite.

A second bounded scope authenticates a new cycle-prefix edge and reuses its
already checked actual earlier-prefix/controller pair. This is an inductive
read, with all receipts, material releases, producer edges and phase history
retained. Copied, omitted, reordered or forged prefixes and wrong controllers
do not inherit another pair's cached verdict. Outside the scope the exact
reader authenticates the full prefix again. This further reduced the same
B01 3×3 generation diagnostic to 2.54 s.

## Completed collection and verification

The retained collection contains 108 designs from 36 skeletons, each executed
as a 5×5 native field after packet export and import: 2,700 placed units,
245,775 acknowledged drawing strokes and 55,098 accepted seam witnesses.
There are 1,465 introduced paths across the unit programs. All 324 layer focus
reads are complete; the sampled runs have zero blocked or open seams and zero
numeric participants. Finite outer frontiers remain explicit and are not
claims of an infinite completed wallpaper.

All 108 designs received visual review. The
[browser report](../../experiments/QualitativeFrieze/results/ornament-designs/browser-check.json)
records valid SVGs and local links, distinct geometry for all three versions
of every skeleton, working global and independent design selection,
construction-only viewing, and desktop/mobile layouts without horizontal
overflow. The single-page collection contains all 108 plates. The combined
SVG and PNG provide an additional single-sheet view; a smaller four-family
preview makes the differences easier to compare at once.

Solution verification passed 4,164 tests across 12 test assemblies with no
failures or skipped tests using `dotnet test Gojo.slnx -c Release`. These
checks establish the retained structural and execution contracts, not a
universal aesthetic score or learned ornament generator.
