# A path, its two sides, and the occupied passage

Status: executable exploratory study, October 2026. The previous curve and
evidence fixtures are preserved. This extension adds native ruled bands above
the kernel; it does not change scalar settlement or the statistical laws.

The [comparison gallery](../../experiments/QualitativeFrieze/results/path-bands/gallery.html)
and [whole sheet](../../experiments/QualitativeFrieze/results/path-bands/overview.html)
show 22 constructions through four views each. Start with W002 for tapering,
W008–W012 for joining versus weaving, W013 for unresolved curved crossing, and
W017 for bounded sides without visible end caps. W019 and W020 deliberately
separate a collapsed band from missing width.

## What this adds

Thickness is useful as a local profile, but it is insufficient as the complete
description. These independent structures now remain connected:

1. The ordered source path, including its actual endpoint identities and
   interpolation dependencies.
2. Two corresponding side curves, constructed from relative displacement
   words at the path's controls.
3. Bounded transverse support between those sides and an optional selection
   of that support for occupancy.
4. End conditions: an actual visible cross-section cap, continuation into an
   exposed outside participant, or an unresolved end.
5. Projected crossing evidence and, separately, optional local front/back order.
6. Focus selections that expose the spine, sides or support while carrying the
   original construction.

A single supplied path can therefore become a narrow passage, a thick ribbon,
or a pointed leaf while keeping the same path identity. A construction axis
need not be an exact geometric centre: asymmetric lateral growth is allowed.
An enclosed route can surround unoccupied central ground. Conversely, an
occupied stem can have both ends continue outside the view without any visible
closed contour.

## Native construction

`QualitativeBand` reads this exact relation grammar:

```text
sweep = ((frame -> spine) -> (sideA -> sideB)) ->
        (Closed(control offset/sum correspondences) -> ruled support)

correspondence = (spine control -> (offsetA -> offsetB)) -> (sumA -> sumB)

support = (longitudinal interval -> transverse interval) ->
          (Interpolate(sideA output, sideB output, u, output) -> output)

envelope = sweep -> ((start condition -> end condition) -> occupancy)
cap = endpoint cross-section -> actual cross-section line
continuation = endpoint cross-section -> (spine endpoint -> outside participant)
occupancy = support -> Closed(sideA -> sideB)

crossing = (frame -> (first spine -> second spine)) -> locus word
order = crossing -> (front spine -> back spine)
```

Each side control is the result of a real `QualitativeMotionFrame.Reduce`
operation over the original control and its offset. The reader verifies the
inputs, native cancellation witness and actual result participation. Side
curves consume the **same longitudinal parameter** as the source curve.
Another bounded parameter interpolates between their actual outputs. Thus
the band is a two-parameter construction, rather than an SVG stroke width
stored beside an unrelated path.

All offsets use repeated participation in the frame's common uncalibrated
reach. Integer authoring counts describe finite words, not measured radii,
pixels or world lengths. Signed coordinates, ordered paths and side selection
retain their different meanings. No carrier receives a measured geometric
value; holder state stays zero and existing structural unit/port ticks stay
at their fixed zero/one landmarks.

The supplied side-control profiles are an explicit interpolation model. For
curves and diagonal corners they **do not establish an exact normal-distance
offset**. Nor does the bounded ruled support establish that its image is a
simple, nonfolding region with positive area. A zero displacement on both
sides is readable and collapsed; a missing or foreign word remains open.

Selecting occupancy is not a material transfer or a global area union. It
selects constructed support for this observation. It does not prove a physical
source or settle overlap across different bands.

## Narrow and broad rungs

Each recipe authors its paths and base side profiles first. Narrow and broad
layers then reuse the **actual same spine sections**, with one or three copies
of each offset word. Both width layers retain source-to-thread correspondence
and the repeated participation used in authoring. The broadening is verified
in focused tests; the gallery's `SameSpinesAcrossWidthRungs` read specifically
checks spine identity, not a general transform-solving law.

The centre-path panel is a lower-detail observation of the broad construction;
it is not a declaration that its width is zero. The other panels show narrow
occupancy, the broad paired sides, and a broad filled/decorated or contrast
reading. W019 is the separate explicit-zero case.

The layer reader consumes native scopes rather than its diagnostic host lists.
Missing entries survive as open reads. A crossing or order must refer to
spines actually exposed by that layer. Side runs join only through actual
matching sites **and** matching positions; equal coordinates alone do not
stitch independent side endpoints. Incomplete spans split runs instead of
being silently bridged. Closed paired-side cycles are a separate read from
the presence of a cap on an individual span.

## Crossing, joining and visibility

The current local crossing reader certifies a supplied locus strictly inside
two nonparallel straight sections. It neither welds their sites nor chooses
depth order. Curved crossing requests remain unresolved in this round.

- W008 has four branches sharing one actual centre site. Their support is
  displayed as a united plus with no over/under relation. Interior-edge
  suppression is a presentation reading of meeting supports, not a new
  globally settled native polygon union.
- W009 has two crossing paths with order left open. Both boundaries remain
  uninterrupted over neutral translucent support. Painter order must not
  masquerade as known foreground.
- W010 and W011 supply opposite order relations for the same kind of crossing.
- W012 repeats alternating local orders over a basket field. It is a new
  continuous-thread interpretation inspired by B02; it does not claim to
  recover depth from the older B02 rectangle contours.
- W013 supplies curved passages but leaves their crossing order unresolved.
  Its repeat extends beyond the observation to hide rendering-only end
  closures outside the crop.

For an ordered crossing, the renderer masks the back passage using the front
passage's read support. In the centre-path drawing it instead preserves a small
gap, an authored view policy at that display resolution. The hidden path,
side construction and occupancy remain connected and continuous. A display
gap is not a deleted native segment. No learnt perceptual cutoff is claimed.

## Continuation and figure/ground

SVG implicitly closes an open filled subpath. That is not evidence for source
closure. We draw only the actual native side boundaries and explicit caps;
presentation support may close outside the selected viewport. W017 continues
past both edges of the view and has no native visible caps. Internal spans
may also continue into their sibling scope rather than cap at every join.

W006, W014 and W015 reverse paint contrast over the same supports. This makes
competing figure/ground readings easier to see. **It does not implement a
general border-ownership inference or native area complement.** Those remain
open extensions. A future receiver can instead select boundaries as enclosing
edges, route sides or part of a cutout, with its own contextual evidence.

## Coverage and findings

The generated round contains 22 cases, 88 SVG views, 71 complete broad sweeps,
one deliberately open width, one explicitly collapsed sweep, 23 certified
straight crossings and 22 explicit crossing orders. The additional curved
crossing request remains open. These counts are construction/read coverage,
not recognition accuracy or a training result.

The examples include angular meanders, closed square/diamond circuits,
tapered and asymmetric vines, a rounded turn, a basket weave, guilloche,
square-wave courses, diagonal strips and radial petals. The radial example
uses cardinal/diagonal words; it does not certify equal-angle circle partitions
or an exact rotation group.

The useful outcome is a shared construction between views. Width changes no
longer require an unrelated outline recipe, and a cap, underpass or occupancy
answer can be inspected without reconstructing it from rendered paint. The
remaining inverse task is substantial: given only outlines, which spine,
width profile and ownership interpretation are useful? This implementation
retains supplied correspondence; it is not a blind medial-axis extractor.

## Reproduction and verification

```powershell
dotnet run --project experiments/QualitativeFrieze -- --path-bands
node experiments/QualitativeFrieze/tools/check-band-evidence.cjs experiments/QualitativeFrieze/results/path-bands/gallery.html
dotnet test Tests/QualitativeExecution.Tests --filter "FullyQualifiedName~QualitativeBandTests|FullyQualifiedName~BandEvidenceIntegrationTests"
dotnet test Gojo.slnx
```

The core and integration suites contain 33 tests/executions covering source
identity, real interpolation dependencies, forged controls, unknown versus
zero, independent occupancy and ends, branching versus crossing, opposite
orders, foreign-layer rejection, open entries, broken side-run continuity,
unselected occupancy and an ordered foreground with unknown width.
The browser/data checker validates every SVG and native archive and visits
the inspector on desktop/mobile plus the overview.

Verification recorded for this checkpoint: the solution-wide run passed 5,201
tests with no failures or skips. The final focused run passed all 33 band
tests/executions after the last presentation/occupancy safeguards. Browser
validation passed 28 states and 136 local references with no errors, covering
all 88 SVGs and 22 archives. These are correctness and presentation checks,
not an estimate of perceptual or recognition accuracy.

Each case links a compressed native graph archive. Compact transport preserves
within-graph aliases, including source/side parameter sharing; the core suite
checks those correspondences after import. A general independent imported
`QualitativeCurve.Frame` execution/rebinding API is not added here. The archive
is inspectable graph transport, not a promise of standalone band-engine replay.

Sources and limits of the analogy are recorded in the accompanying
[research and existing-mechanism audit](GojoBandRepresentationResearch.md).
The generative-skeleton work motivates axis/boundary correspondence, while
border-ownership research motivates keeping local edges and ownership
separate; neither supplies the missing inverse law for this engine.
