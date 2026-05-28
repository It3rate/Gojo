# GojoDraw

GojoDraw is the Skia adapter layer for GojoCAD. It should not become a drawing
environment, scene graph, state store, or source of graphical meaning.

The intended direction is:

```text
GojoCAD structure carries graphic concepts
GojoDraw locally reads those concepts
Skia receives paths, points, gradients, colors, and paint at the boundary
```

For now the adapter is intentionally tiny:

- draw a GojoCAD point as a dot
- draw GojoCAD radial flow as a radial gradient around its source point
- draw a GojoCAD segment as a Skia line
- draw a GojoCAD directed segment when a caller wants to inspect inverse-start
  to positive-end flow

Style and display values are currently supplied by the visualizer because
GojoCAD does not yet define style surfaces. As those concepts arrive in
GojoCAD, GojoDraw should read them from structure rather than owning them as
render state.

The first demo lives in `GojoDraw.Visualizer` and uses Avalonia without XAML.
