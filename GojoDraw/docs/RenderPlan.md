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

## Current Visualizer

`GojoDraw.Visualizer` displays:

- one point as a dot
- one point with radial flow as a radial gradient
- one ordinary segment
- one inferred circumference with partition marks
- hover text over rendered primitive regions
- one composite relation handed to the adapter, which walks the structure and
  renders recognized primitive forms

There is no animation yet. Avalonia is used only as a host, with no XAML, and
the actual drawing is done through Skia custom drawing.

## Next Steps

1. Add GojoCAD style participation for stroke and fill.
2. Read dot radius, stroke width, and colors from GojoCAD structure.
3. Add a local projected-frame surface so viewport fitting and sampled
   projection choices can also be expressed as GojoCAD structure.
4. Revisit directed-segment visualization once the inverse-start to
   positive-end semantics have more examples.
