# GojoDraw

GojoDraw is the Skia adapter layer for GojoCAD. It should not become a drawing
environment, scene graph, state store, or source of graphical meaning.

The intended direction is:

```text
GojoCAD structure carries graphic concepts
GojoDraw walks relation structure and renders forms it recognizes
Skia receives paths, points, gradients, colors, and paint at the boundary
```

Recognition comes from GojoCAD's `GraphicReadGraph`; GojoDraw should consume
those temporary reads rather than maintaining a separate shape algebra.

For now the adapter is intentionally tiny:

- draw a GojoCAD point as a dot
- draw GojoCAD radial flow as a radial gradient around its source point
- draw a GojoCAD segment as a Skia line with small endpoint bars
- draw a GojoCAD directed segment as inverse-start to positive-end flow with
  an arrow tip at the positive end
- draw line seeds, sheet seeds, and radius attachments as low-intensity
  provisional reads
- derive projection from connected point, attachment, unit-shape, and viewport
  evidence instead of per-site pixel maps
- derive a linear carrier's direction from an origin plus a non-origin
  attachment witness before sampling an unresolved direction
- infer a circumference from circular carrier unit shape plus attached marker
  and radius evidence
- draw partition sites as low-intensity line ticks
- walk a larger `IElement` and render each recognized primitive relation form
- emit render-boundary hover hit areas for visualizer controls

The visualizer also renders a small left-side legend. Its glyphs are ordinary
GojoCAD structures drawn through the adapter; only the labels are visualizer UI.

Style and display values are currently supplied by the visualizer because
GojoCAD does not yet define style surfaces. As those concepts arrive in
GojoCAD, GojoDraw should read them from structure rather than owning them as
render state.

When a projection value is not yet structurally determined, GojoDraw samples a
local value from a bounded range using the visualizer seed. That keeps the
structure inspectable without pretending the format already contains an exact
answer.

Once a primitive relation is recognized, GojoDraw should render it as present.
Incomplete detail becomes sampled or partial visualization, not a quiet failure
to draw.

The first demo lives in `GojoDraw.Visualizer` and uses Avalonia without XAML.
