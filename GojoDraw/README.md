# GojoDraw

GojoDraw is the Skia adapter layer for GojoCAD. It should not become a drawing
environment, scene graph, state store, or source of graphical meaning.

The intended direction is:

```text
GojoCAD structure carries graphic concepts
GojoDraw walks relation structure and renders forms it recognizes
Skia receives paths, points, gradients, colors, and paint at the boundary
```

For now the adapter is intentionally tiny:

- draw a GojoCAD point as a dot
- draw GojoCAD radial flow as a radial gradient around its source point
- draw a GojoCAD segment as a Skia line with small endpoint bars
- draw a GojoCAD directed segment as inverse-start to positive-end flow with
  an arrow tip at the positive end
- infer a circumference from circular carrier unit shape plus projected marker
  evidence
- draw partition sites as small low-intensity squares
- walk a larger `IElement` and render each recognized primitive relation form
- emit render-boundary hover hit areas for visualizer controls

Style and display values are currently supplied by the visualizer because
GojoCAD does not yet define style surfaces. As those concepts arrive in
GojoCAD, GojoDraw should read them from structure rather than owning them as
render state.

The first demo lives in `GojoDraw.Visualizer` and uses Avalonia without XAML.
