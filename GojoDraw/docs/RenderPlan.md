# GojoDraw Render Plan

GojoDraw is a translator for GojoCAD elements, not a replacement format. It
should stay thin enough that GojoCAD remains the place where graphical concepts
are defined.

## Boundary Rule

Convert GojoCAD structures to Skia values only at adapter/render boundaries:

```text
carrier location -> SKPoint
radial flow      -> radial gradient draw
segment          -> line plus endpoint bars
directed segment -> line with arrow tip at positive end
circular carrier -> circumference from circular origin and marker point
point on basis   -> partition line tick when it belongs to a segment/circumference
```

Do not add viewport, timer, stroke, color, or component state as GojoDraw-owned
meaning. Those should eventually be GojoCAD structures. Until that exists,
visualizer/demo code may supply paint values and the viewport boundary.
Projection should be derived from GojoCAD relation evidence where possible.
Ambiguous values can be sampled from bounded ranges for the current run instead
of being fixed in per-site pixel maps.

Linear projection now reads an ordered basis from local evidence when it can:
a linear origin plus a non-origin attachment witness gives direction and scale.
If that witness is a point on a circumference, the line follows the current
projection of that contour point; any unresolved contour period can still be
sampled.

Recognized structures should render as recognized structures. Missing radius,
period, orientation, style, or detail should lead to sampled, partial, or
tension-like visualization rather than a failed render branch.

The recognition path should move toward the GojoCAD
[read algebra](../../GojoCAD/docs/ReadAlgebra.md): derive a read graph from
relation structure, then render carried reads instead of testing every possible
primitive at every relation.

The adapter now consumes the initial `GraphicReadGraph` for point, span,
directed span, radial seed, circular marker/span, partition, radius attachment,
and linear basis witness reads.

## Current Visualizer

`GojoDraw.Visualizer` displays:

- one connected GojoCAD structure with roughly thirty relation elements
- multiple points, radial flows, ordinary segments, and directed segments
- line and sheet orthogonal seeds as low-intensity provisional reads
- inferred circumferences with contour spans, radius attachments, and
  partition marks
- a directed segment aligned by an endpoint attachment to a point on an arc
- a small left-side legend whose glyphs are built as GojoCAD structures and
  rendered through the same adapter path
- hover text over rendered primitive regions
- one composite relation handed to the adapter, which renders the carried
  `GraphicReadGraph` reads

There is no animation yet. Avalonia is used only as a host, with no XAML, and
the actual drawing is done through Skia custom drawing.

## Next Steps

1. Add GojoCAD style participation for stroke and fill.
2. Read dot radius, stroke width, and colors from GojoCAD structure.
3. Add a local projected-frame surface so viewport fitting and sampled
   projection choices can also be expressed as GojoCAD structure.
4. Revisit directed-segment visualization once the inverse-start to
   positive-end semantics have more examples.
