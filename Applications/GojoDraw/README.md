# GojoDraw

GojoDraw is the presentation layer for Gojo-oriented content. It includes the
Skia adapter for GojoCAD plus generic render-boundary presentation tools such
as themes, animation timing, visual cues, controls/components, hit areas, and
audio cue metadata.

GojoDraw may help create interactive content, but it should not become the
source of Gojo meaning. Meaning still lives in Gojo carriers, locations,
relations, unit shapes, residue, tension, and timelines.

The intended direction is:

```text
GojoCAD structure carries graphic concepts
GojoDraw walks relation structure and renders / presents forms it recognizes
Skia receives paths, points, gradients, colors, and paint at the boundary
```

Recognition comes from GojoCAD's `GraphicReadGraph`; GojoDraw should consume
those temporary reads rather than maintaining a separate shape algebra.

For now the GojoCAD adapter is intentionally small:

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
- emit render-boundary hit areas for hover, drag, controls, and navigation

The generic presentation layer currently includes:

- `GojoCadSkiaTheme` as a reusable paint factory for visual treatments
- `GojoDrawAnimationClock` and `IGojoDrawTimeline` for host-driven animation
  timing
- `GojoDrawVisualCue` and `GojoDrawVisualTrack` for drawable presentation
  overlays such as pulses, tension glows, residue chips, flow arrows, and
  correspondence markers
- `GojoDrawControl` and `GojoDrawComponent` for generic buttons, sliders,
  toggles, navigation, and drag handles
- `GojoCadSkiaHitArea` behavior metadata for hover, press, drag, slider,
  toggle, and navigation interactions
- `GojoDrawAudioCue` and `IGojoDrawAudioPlayer` so desktop and web hosts can
  map shared cue metadata to platform audio

The visualizer also renders a small left-side legend. Its glyphs are ordinary
GojoCAD structures drawn through the adapter; only the labels are visualizer UI.

Style and display values are currently presentation-boundary concerns because
GojoCAD does not yet define style surfaces. As those concepts arrive in
GojoCAD, GojoDraw should read them from structure rather than owning them as
meaning.

When a projection value is not yet structurally determined, GojoDraw samples a
local value from a bounded range using the visualizer seed. That keeps the
structure inspectable without pretending the format already contains an exact
answer.

Once a primitive relation is recognized, GojoDraw should render it as present.
Incomplete detail becomes sampled or partial visualization, not a quiet failure
to draw.

Application-specific page layout, tutorial structure, navigation content, and
lesson text belong outside GojoDraw, such as in `Gojo.Blocks`. Generic controls
and presentation timing belong here so desktop and web hosts can share the same
conceptual UI vocabulary.
