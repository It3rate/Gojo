# A flowing grammar

The [refined collection](../../experiments/QualitativeFrieze/results/ornament-refined/gallery.html)
revisits all 36 Bourgoin skeletons in three constructions each. The
[whole sheet](../../experiments/QualitativeFrieze/results/ornament-refined/overview.html)
puts the 108 designs together. The earlier
[108-design collection](../../experiments/QualitativeFrieze/results/ornament-designs/gallery.html)
remains available as a baseline.

The [three curve studies](../../experiments/QualitativeFrieze/results/ornament-curves/gallery.html)
isolate the vine, returning curl and interrupted guilloche before applying
the same presentation mechanism to the larger collection.

## What changes

The preceding [detail study](GojoOrnamentDetailStudy.md) showed that a valid
attachment is insufficient to make ornament fit or flow. This revision gives
detail a parent, direction and cadence: narrow strips carry aligned sprays;
radial compartments carry guided petals; turns and courses carry restrained
leaves, veins or curls. Overall construction, recurring ornament and finer
articulation retain different introduced path sets. Detail names its role in
the composition, not an absolute size limit: a large rosette or leaf can be
detail when its placement, orientation and repetition accent the overall
arrangement and preserve its counterforms.

The native authoring basis substitutes eight repeated unmeasured moves for
one coarse source move. Source paths, ports, transforms, layout and seam
obligations retain explicit correspondence. Reusable prototypes, actual
images, landmark offsets and layer selections remain Gojo structure. Runtime
loads a saved packet before executing; it does not dispatch on the source
name to redraw the ornament.

An attachment can be an explicit offset from a source landmark. It need not
be a physical branch from the visible source stroke: some B32 sprays sit
beside their parent course. The attachment witness establishes that offset
correspondence, not general clearance or physical contact.

Some overall panels, especially B12's nonconvex fret, use scaled companion
contours that cross the source at re-entrant corners. These are layered
overprinting rather than guaranteed contained insets. The selected strip,
hexagon and fan containment checks do not establish containment for every
ornament family.

The collection uses a 3×3 native sample by default. Its display crop reaches
closer to the finite outer frontier than the old 5×5 collection, showing more
of those actual outer units. No extra repetitions are supplied by the
renderer. This is a practical display sample, not a controlled compute
comparison or a proof of infinite completion.

## Curves as a selected interpretation

The user explicitly permitted an optional curve rendering without requiring
a native curvature law. The connected selection is:

~~~text
actual motif program -> (selected actual paths -> fixed actual public ports)
  -> selected paths -> carried complement -> fixed ports -> blocked/open
~~~

The exact reader rejects copied or foreign paths and ports, duplicated or open
selections, and forged favourable results. A fixed port selects an actual
guide vertex. The source remains its finite ordered chord program.

The renderer rounds unpinned corners with quadratic fillets. A fillet's
control points stay in the local guide triangle; it cannot overshoot that
triangle. Its incoming and outgoing tangent directions agree with the
adjoining straight portions. This is geometric tangent-direction continuity
(G1), not a claim of equal parameter speeds or native analytic curvature.
Starts, ends, explicitly pinned branch landmarks and selected pointed tips
remain exact. Separate paths retain separate ends, including the guilloche's
actual gap. Lattice corners remain angular unless explicitly selected.

The angular and curved views use the same acknowledged paths and native
execution. Construction-only viewing uses the original chord guides. Smooth
display does not establish occupied faces, non-overlap, clearance, tangency
between different paths, hidden continuity or knot order.

## Native transport

Packet version 2 wraps the existing version 1 field packet together with an
optional native presentation-hint scope. Both travel through one capture and
one rebind memo, preserving actual motif/path/port aliases. The renderer reads
the imported selection against the imported motif. It refuses an unrecognised
or invalid hint rather than silently applying curves elsewhere.

Saving without hints still emits version 1. Existing version 1 files retain
their established loader and execution path. No numeric carrier or location
is introduced by a curve hint.

## Structural corrections before smoothing

D02's two intermediate stem bends now turn away from the emerging leaf
shoulders, keeping their original branch roots. D07's returning curl now
arches into the free space beside its parent rather than crossing the parent
stroke. These are native guide changes; smoothing is applied afterwards.
D09 preserves the same interrupted strands and exact gap evidence.

The [art-direction notes](../../experiments/QualitativeFrieze/research/ornament-refined-directions.md)
record historical guidance, source-family fits and the visual review.

## Reproduction

~~~powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-curves
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-refined
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-refined --only B02,B05 --output artifacts/refined-pair
dotnet test Gojo.slnx -c Release
~~~

`--samples 3..6` changes the finite native window. Disjoint source batches may
use `--plates-only` and a common output folder, then `--assemble`. Mode subsets
use `--modes field,motif,detail`. `--resume` checks required files and native
sample dimensions; after an intentional authoring change, regenerate the
affected modes without resume.

Each design retains its native packet and focus reads, curved and angular
views, one-unit close-ups, bare scaffold and construction view. Presentation
records and SVGs describe results; they do not replace native meaning.

## Validation

The completed collection contains 108 executed designs over 36 retained
skeletons, each in a finite 3×3 window. Every run imported its saved native
packet before execution. All 324 layer-focus reads were complete; the runs
reported zero blocked seams, open obligations or numeric participants. The
three isolated curve studies completed with the same checks.

`dotnet test Gojo.slnx -c Release` passed 4,224 tests across 12 assemblies, with
zero failures or skips. Focused tests cover native path identity after packet
import, rejected foreign or forged curve selections, fixed branch landmarks,
endpoint and gap preservation, source retention and selected containment
cases. A subsequent CLI build and assembly check also verified consistent
export records and the completed resume path.

The [browser report](../../experiments/QualitativeFrieze/results/ornament-refined/browser-check.json)
checks all 108 main SVGs and native packets, the 108-image overview, local
links, distinct three-mode geometry, all view controls and desktop/mobile
overflow. No errors were found. Visual review compares source triples and
close-ups; it does not establish a universal aesthetic or non-overlap law.
