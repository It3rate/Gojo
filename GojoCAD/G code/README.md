# GojoCAD G-code Import

This folder imports a useful 2D subset of G-code into Gojo structure. It is not
a G-code scene graph and it does not try to preserve enough information to
write G-code back out yet. The goal is renderable, inspectable GojoCAD
structure that can later be modified as structure.

## Basis

Each parsed program starts with a basis frame. The frame carries:

- a shared XY frame carrier
- X and Y coordinate carriers
- Z, feed, tool, G-word, M-word, and arc-phase carriers
- X/Y ticks per input unit
- arc-phase ticks per full turn

Changing `GCodeParseOptions.CoordinateTicksPerUnit` or
`GCodeParseOptions.ArcTicksPerTurn` changes this basis and the encoded
locations. The renderer reads the resulting Gojo locations; it does not own a
separate G-code scale.

## Managed G-code Structures

| G-code structure | Gojo structure |
| --- | --- |
| `G0` / rapid XY | Travel motion frame with start/end frame points and axis-flow evidence. It has no render span, so it remains inspectable without drawing a stroke. |
| `G1` / linear XY | Shared-frame segment from start point to end point. X/Y movement deltas are also carried as raw positive-face or inverse-face axis-flow relations. |
| `G2` / clockwise arc | Shared-frame arc pieces for rendering, plus center/start/end points, radial elements, and a circumference-flow relation on the inverse face of a circular carrier. |
| `G3` / counterclockwise arc | Same arc structure as `G2`, except circumference-flow uses the positive face. |
| `I` / `J` arc center | Local center offset from the current point. |
| `R` arc radius | Center is resolved from start/end/radius. Negative `R` selects the major arc. |
| `Z` | Pen participation. Z at or below `PenDownZ` is the positive face; lifted Z is the inverse face. |
| `T` / `M6` | Stroke/tool change as a relation between raw tool-carrier locations. It is inspectable but does not render as geometry. |
| `F` | Feed evidence as a raw feed-carrier location on motion frames. |
| `G20` / `G21` | Unit mode evidence and parser scale for following coordinates. |
| `G90` / `G91` | Absolute/incremental mode evidence and parser coordinate interpretation. |
| `M3` / `M5` | Pen down/up shorthand, mapped onto the same Z pen participation. |

Line and arc direction are not stored as symbolic command tags alone. Direction
is carried by the face of the relevant flow relation: positive movement uses
the positive carrier face, and negative or clockwise movement uses the inverse
face. The numeric command word remains present as local provenance.

## Rendering

Rendering comes from the Gojo structure already used by `GraphicReadGraph`:

- `G1` contributes ordinary shared-frame segment relations.
- `G2`/`G3` contribute shared-frame arc relations built from center and
  circumference endpoints.
- machine evidence such as feed, tool, pen, and mode changes is carried as raw
  locations or non-geometric relation frames, so the current renderer does not
  accidentally draw it.

Arcs are split into at most quarter-turn frame arcs before rendering. This keeps
large and full-circle G-code arcs compatible with the current shortest-arc Skia
read while the original arc still carries circumference-flow and radial
evidence.
