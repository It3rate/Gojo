# Detail that follows the structure

This experiment revisits the weakest part of the
[108-design collection](GojoOrnamentDesignCollection.md): small details could
have valid landmark attachments yet still look scattered, disconnected or
crowded. The target is richer detail with a readable parent and direction.

The [gallery](../../experiments/QualitativeFrieze/results/ornament-details/gallery.html)
contains ten detail studies and four integrations into the existing B02 basket
and B05 trihexagonal patterns. The
[single sheet](../../experiments/QualitativeFrieze/results/ornament-details/overview.html)
compares them together. The earlier designs remain available for comparison.

## Construction before placement

The working construction is:

~~~text
source scaffold -> actual exposed landmark and direction
  -> shared finite prototype -> explicit substitution image
  -> witnessed landmark offset -> native enrichment layer
  -> imported executable program -> acknowledged paths
  -> close-up and repeated-field reads
~~~

An attachment proves the declared path start agrees with its source port and
offset. It does not by itself establish that the addition follows a parent,
fits inside a compartment, has clearance, shares a physical site or preserves
an intended visual hierarchy. Those are separate questions.

| Study | Detail construction |
| --- | --- |
| D01 | A parent stem, two mirrored veined leaves and a terminal bud |
| D02 | Alternating leaf images following successive vine landmarks |
| D03 | Eight guided petals with separate necks, vein forks and a centre seed |
| D04 | Sixfold petal substitutions in a declared triangular display basis |
| D05 | A graded five-lobed anthemion on visible branch guides |
| D06 | Three opening lotus leaves with clear channels between them |
| D07 | A turning parent, outward leaves and an open returning curl |
| D08 | A longitudinal pearl chain with alternating leaf branches |
| D09 | An interrupted two-strand guilloche with an exact native gap read |
| D10 | A closed quatrefoil loop with compartments on its diagonal guides |
| D11–D12 | B02 strip-following willow sprigs and feather-veined leaves |
| D13–D14 | B05 outward lotus petals and inward buds around an open heart |

This round therefore authors the hierarchy deliberately. Stems give leaves a
direction and cadence; mirrored lobes grow from a common axis; repeated petals
share a centre; bands receive aligned chains rather than independent vertex
marks. Detail can make its own larger repeat, but it should keep the source's
major arrangement readable. These are proposed design rules evaluated by
inspection, not a learned or universally valid aesthetic score.

## Native structure and display

The resulting finite paths, prototype images, ports, offsets and three layer
selections are ordinary pre-numeric Gojo structure. Colours, outlines, filled
contours and the crop are display interpretations. A radial petal arrangement
uses declared directional substitutions; a regular-looking display does not
establish an intrinsic equal-angle theorem.

Each program is saved, decompressed and imported before execution as a 3×3
study field or 4×4 integration field. The close-up selects the first actual executed unit from that same
run. It does not redraw a separate SVG glyph or skip native drawing receipts.
Scaffold, ornament and finer internal marks remain separately readable, with
their sibling layers carried in native focus reads.

The interlace studies use actual interrupted paths. Their open ends and gaps
remain in the program; apparent continuation behind another strand is not
assumed. This is a finite construction vocabulary for knot-like ornament,
not a general knot invariant, three-dimensional crossing law or occlusion
solver. Native crossings, shared-site joins and projected coincidence remain
different reads.

The new gap contract connects an actual source program, an actual terminal
port and an actual restart port on distinct open paths. Their retained prefix
reads and ordered nonzero difference account for the interruption. Its exact
reader rejects foreign or copied ports, closed contours, same-path claims,
zero displacement and forged favourable witnesses. D09 retains this contract
inside its source evidence, and an archive-only check follows the same gap
through its actual placed publications. It does not claim that all three
source paths are disconnected: their outer ends have explicit joins.

## What visual inspection changed

The first D10 had legal attachments but put axial compartments across the
diagonal loop's narrow waists. The corrected version exposes four diagonal
guide rays and fits each compartment to one of those lobes. Its earlier
[close-up](../../experiments/QualitativeFrieze/results/ornament-details/first-loop/D10-closeup.svg)
and native program remain available as a concrete counterexample to treating
attachment validity as sufficient visual fit.

D03's first petal bases also overlapped outside the centre seed. The new
petals begin at actual radius-two guide landmarks and end at their unchanged
outer tips. D09's large filled beads initially overwhelmed its fine strand
lines at thumbnail size; stronger strand outlines and quieter bead fill
improve that read without changing the native program.

B02's additions explicitly name their own rectangle's public port, avoiding
an ambiguous nearest-port tie even when the displayed position would be the
same. Focused checks inspect each addition's parent and finite strip
envelope. B05's inward buds use the same sixfold substitution orbit as its
source frame, with a retained open centre. These are authored, tested fits;
there is no general automatic ornament placement or clearance solver yet.

## Reproduction

~~~powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-details
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-details --only D01,D11 --output artifacts/two-detail-studies
dotnet test Gojo.slnx -c Release
~~~

Disjoint source batches may use `--plates-only` and the same output directory,
followed by `--assemble`. Assembly combines completed records and views; it
does not rerun native execution. Individual exports include the repeated
field, one-unit close-up, bare scaffold and all paths in one ink, together
with compressed native programs, focus reads and diagnostic records.

The [art direction and source notes](../../experiments/QualitativeFrieze/research/ornament-detail-directions.md)
explain the historical guidance and the limits of the chosen interpretations.

## Verified checkpoint

All fourteen final designs received visual review. Their imported fields
execute 154 units and 13,355 acknowledged strokes, with 1,566 accepted seam
witnesses, zero blocked or unresolved seams and zero numeric participants.
All 42 native layer focus reads are complete. Finite outer frontiers remain
explicit; this is not a claim of an infinitely completed wallpaper.

The [browser report](../../experiments/QualitativeFrieze/results/ornament-details/browser-check.json)
records working per-plate view controls, valid SVG references, valid local
links, no browser errors and no horizontal overflow on desktop or mobile.
Both combined SVG/PNG sheets were exported and inspected.

The final solution rebuild and run passed 4,195 tests across twelve assemblies
with no failures or skips. The 31 new focused cases cover source preservation,
native focus, archive-only execution, actual parent-strip ownership, fitted
hexagonal detail, guide/root/tip correspondence, the corrected loop fit and
false gap claims. These checks establish the specific constructions and
retained contracts; they do not establish a general beauty or clearance law.
