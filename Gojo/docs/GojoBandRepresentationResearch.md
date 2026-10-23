# Centre paths, paired boundaries and occupied bands

Status: research and implementation audit, October 2026. This note separates
existing reusable mechanisms from proposed laws for the next ornament study.
It does not claim a general skeleton extractor, curved offset solver or learned
perceptual model.

## The useful distinction

A centre path, the two sides of a band, the participation between those sides,
and the part visible through a view are different structures. A width setting
can connect their appearances, but cannot supply all their semantics.

The minimal useful correspondence is:

```text
ordered path span
  -> ordered cross-section landmarks
  -> paired side points and boundary spans
  -> occupied transverse support
  -> end/continuation evidence
  -> selected view and visible/hidden participation
```

This is a proposed construction context, not the unique definition of a thick
line. A contour can instead be supplied directly, and a possible centre path
can remain a hypothesis. Equal rendered bands need not share construction
history or afford the same transformations.

Three different questions should remain independently answerable:

| Question | Sufficient local evidence | Evidence that is insufficient alone |
| --- | --- | --- |
| Do paths join? | Shared endpoint/site or explicit admissible connection | Equal drawing coordinates, projected crossing |
| Does one passage hide another? | Crossing correspondence plus local order and visibility support | Joined site, darker paint, path list order |
| Is this region occupied? | Selected side support or fill participation | A closed incidence cycle, two nearby outlines |

A plus sign can therefore have joined arms with no crossing order. A weave
can have separate continuing paths with crossing order and a hidden interval.
An open stem can retain paired sides and bounded transverse support while both
ends continue beyond the selected view. None of these requires inventing a
visible cap.

## Existing mechanisms worth reusing

- [`SegmentOffset`](../Structures/SegmentOffset.cs) constructs an exact rational
  straight offset in a supplied `PlanarMetric`, or retains an unresolved
  construction when its exact coordinates are unavailable. It retains source,
  calibrated reach, relative side, displacement and endpoint correspondence.
- Its [path composition](../Structures/SegmentOffset.Path.cs) connects bounded
  miters at actual shared sites, admits spans only when their ordered traversal
  remains positive, and keeps rejected candidates and shared open joints. It
  is an open-path observation: caps, curved offsets, closed-region topology and
  non-neighbour intersections are separate questions. The
  [joined-offset study](GojoJoinedOffsetPath.md) describes this contract.
- [`GraphicStructure.AreaFill` and `AntiAreaFill`](../../Applications/GojoCAD/GraphicStructure.cs)
  distinguish positive and inverse area participation from a boundary alone.
  [`GraphicAreaParticipationSurface`](../../Applications/GojoCAD/structures/GraphicAreaParticipationSurface.cs)
  retains participants, requested/result outline, settlement evidence and
  residue. Its default cancellation frame is not proof that every geometric
  union has settled.
- [`GraphicOrthogonalFillSurface`](../../Applications/GojoCAD/structures/GraphicOrthogonalFillSurface.cs)
  already emits area bands around selected line paths, arcs, circles and
  rectangles. Its geometric construction uses calibrated tick coordinates,
  floating-point normal/intersection calculations and rounded polygon points.
  It is not an exact qualitative curved offset law. Its open-path fill joins
  the two sides into a polygon at the ends; that policy cannot stand in for a
  stem continuing outside a view.
- [`RibbonPartitionSupplier`](../../experiments/QualitativeFrieze/RibbonPartitionSupplier.cs)
  makes both roles occupied participants in a finite advertised strip. Band
  extent, gap extent, source calibration and observer calibration remain
  separate. This is useful for equal-value figure/ground experiments, although
  its finite calibrated cells do not supply a general ribbon representation.
- [`QualitativeWeave`](../../experiments/QualitativeFrieze/QualitativeWeave.cs)
  retains native role partitions, repeat order and role exchange. Despite the
  name, those role exchanges do not establish physical crossing depth.
- The Bourgoin [B02 correction](../../experiments/QualitativeFrieze/BourgoinPatternLibrary.CorrectedNetworks.cs)
  supplies repeated elongated rectangle contours and emergent square gaps.
  Its selected geometric construction does not infer source ink width or
  weaving order. The coloured ornament adapters also explicitly describe
  paint/clipping as presentation interpretations rather than native occupied
  faces or depth evidence.

The native curve experiment is the appropriate spine supplier for the new
mini-engine. These older mechanisms provide structural examples and possible
future adapters; importing their measured geometry wholesale would change the
experiment's declared basis.

## Primary research and its limited implications

Feldman and Singh describe a generative skeleton with lateral growth and
axis-to-boundary correspondences. Their model balances contour fit against
skeletal complexity and differs from taking the raw medial axis as a unique
answer. This supports trying a retained path plus growth/side correspondence.
It does not imply that every ornament has one correct skeleton, that a square
has a point as its mathematical medial axis, or that our chosen path was
discovered from its outline.
[Bayesian estimation of the shape skeleton, 2006](https://www.pnas.org/doi/10.1073/pnas.0608811103).

Zhou, Friedman and von der Heydt found responses to border ownership that
depend on surrounding figure context, including cases with the same local
contrast edge. This supports keeping boundary ownership separate from edge
orientation and paint polarity. Their experiments also discuss T-junction
occlusion cues. They do not establish that every T is an occluder or every X
requires a foreground branch.
[Coding of Border Ownership in Monkey Visual Cortex, 2000](https://pmc.ncbi.nlm.nih.gov/articles/PMC4784717/).

Singh and Fulvio measured extrapolated contours behind an occluder. Their
results include dependence on inducing curvature and decay of extrapolated
curvature with distance. These are reasons to preserve endpoint tangent,
curvature evidence and the occlusion context when proposing continuation.
They do not supply a universal gap cutoff or force an exact circular arc.
[Visual extrapolation of contour geometry, 2005](https://pubmed.ncbi.nlm.nih.gov/15647347/).

The SVG specification separately defines stroke width, end caps, line joins
and filling. Its fill operation implicitly closes open subpaths. Therefore a
renderer can create apparent enclosure that the source never asserted. An
implementation should retain native open/continued ends independently and
use an explicit presentation clip when the band continues beyond the page.
SVG paint order is a drawing operation, not automatically evidence of physical
depth in Gojo.
[SVG 2 painting](https://www.w3.org/TR/SVG2/painting.html).

## What the first study should establish

These are proposed checks, not claims of existing general capability:

1. Read the same continuing path as a stroke, paired boundaries, and occupied
   band while retaining explicit correspondences.
2. Change transverse support along the path without redefining its identity.
   Zero width, unpublished width and a lower-resolution stroke read must remain
   distinguishable.
3. Preserve a hidden interval as an observation over a continuing path. Compare
   thin underpass gaps with wide cutoff/occlusion cues, and include a genuinely
   broken path as a counterexample.
4. Compare a joined plus, separate crossing paths with unresolved order, and
   an ordered weave. The renderer must not silently choose a topology.
5. Show occupied side support with no visible end cap, and distinguish an
   object endpoint from the edge of the view.
6. Exchange figure/ground ownership without changing the underlying boundaries,
   and retain the selected domain so that complement has a defined scope.
7. Keep nonlocal self-intersection and band folding unresolved unless checked.
   Passing local cross-section or corner checks does not certify global
   non-overlap, manufacturability or exact equidistant offsets.

Paired interpolated boundary curves are useful chosen bands even when they are
not exact normal offsets of their spine. The first implementation should say
which construction it actually uses and preserve its remaining geometric
questions rather than borrowing a stronger claim from its appearance.
