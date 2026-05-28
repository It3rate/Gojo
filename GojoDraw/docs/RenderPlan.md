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
```

Do not add viewport, timer, stroke, color, or component state as GojoDraw-owned
meaning. Those should eventually be GojoCAD structures. Until that exists,
visualizer/demo code may supply paint and projection values directly.

## Current Visualizer

`GojoDraw.Visualizer` displays:

- one point as a dot
- one point with radial flow as a radial gradient
- one ordinary segment
- one composite relation handed to the adapter, which walks the structure and
  renders recognized primitive forms

There is no animation yet. Avalonia is used only as a host, with no XAML, and
the actual drawing is done through Skia custom drawing.

## Next Steps

1. Add GojoCAD style participation for stroke and fill.
2. Read dot radius, stroke width, and colors from GojoCAD structure.
3. Add a local projected-frame surface so coordinates can come from GojoCAD
   rather than visualizer projection maps.
4. Revisit directed-segment visualization once the inverse-start to
   positive-end semantics have more examples.
