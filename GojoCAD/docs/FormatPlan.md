# GojoCAD Format Plan

This plan starts from the ideas in the Gojo concept-surface notes, read spine,
unit-shape notes, and interpretive graphics notes. It applies them to a
graphical definition library.

GojoCAD should not mimic a conventional vector-command model. It should define
graphic structure in the same way Gojo defines other concepts: connected
structure first, local reads second, external output later.

The lower-level recognition direction is captured in
[ReadAlgebra.md](ReadAlgebra.md). That document treats discovery as a derived
read graph over relation structure, where each local relation read carries
evidence upward instead of every consumer testing every possible shape.

## 1. Primitive Graphic Alphabet

| Graphic need | Gojo-native structure |
| --- | --- |
| Site, marker, endpoint | `CarrierLocation`, `TickRef`, `Relation.Point(...)` |
| Segment | ordinary positive-face point-to-point relation |
| Directed segment | inverse-face start plus positive-face end relation |
| Ray or line seed | orthogonal outlet of a zero-length segment |
| Radial seed | orthogonal outlet of a point |
| Circumference or contour | circular unit shape from a point source |
| Sheet or region seed | orthogonal outlet of a nonzero segment |
| Area | product of non-aligned positive participation |
| Anti-area, hole, cut | opposed or inverse area participation |
| Partition | point, segment, reset, repeat, or resolution structure over a carrier |
| Arc | selected partition of a radial or contour carrier |
| Polyline | ordered group of ordinary or directed segment relations |
| Closure | endpoint return through shared site evidence |
| Branch or merge | shared junction site plus connected spans and lane evidence |
| Stroke order | time sequence over selected spans |
| Fill presence | participation carrier attached to a boundary |
| Color or stroke width | deeper basis read from style participation |
| Resolution | ladder of carriers and scale/carry/read relations |

These should remain primitives or local reads. They should not be collapsed
into broad enums such as shape type, curve type, or junction type.

## 2. Systems To Support As Local Reads

The following external systems are useful inspirations, not mandatory internal
models.

| System | GojoCAD reading |
| --- | --- |
| Point | grounded site on a carrier |
| Segment | ordinary positive-face span between two point relations |
| Polyline | ordered repeated spans with explicit joins |
| Polygon | closed polyline plus fill participation |
| Circle | point-radial seed plus radius and contour evidence |
| Arc | contour partition with start/end markers and optional direction |
| Quadratic Bezier | endpoints plus one control-site relation |
| Cubic Bezier | endpoints plus two control-site relations |
| Spline | repeated local curve spans with continuity witnesses |
| Clothoid | path-length span related to curvature span |
| Involute circle | base contour phase related to tangent/unwrapped length |
| Branch graph | shared sites, spans, lane transitions, and flow witnesses |
| Corner | local feature site where spans meet |
| Rounded corner | corner plus radial transition or radius evidence |
| Intersection | co-location plus connection, crossing, pass/block, or residue read |
| Fill | closed or bounded boundary plus area participation |
| Hole | opposed fill participation inside a boundary |

The goal is not mathematical minimalism. The goal is a structure that matches
how people talk about and refine shapes.

## 3. Curve Definitions Before Calculation

Many useful graphic curves can be defined without calculating all their points.

### Bezier-like curve

```text
selected endpoint span
control site evidence
parameter span
residue: unsampled positions
```

### Clothoid-like curve

```text
selected transition span
path-length carrier
curvature carrier
relation: length span -> curvature span
residue: unsolved placement
```

### Involute-like curve

```text
base circular contour
phase partition on contour
tangent or ray outlet at selected phase
unwrapped length carrier
relation: phase advancement -> tangent length
residue: unsampled path
```

These surfaces can later be sampled, approximated, rendered, or used as AI
conditioning. The definition remains useful before that happens.

## 4. Branches, Merges, And Junctions

Y and T shapes should be defined by real connection:

```text
junction site
incoming span(s)
outgoing span(s)
lane basis
lane transition evidence
read witness or residue
```

The same topology can read as a split, merge, branch, intersection, crossing,
blocked junction, inlet, or outlet depending on flow and lane evidence. That
meaning should come from the local structure, not from a stored junction tag.

## 5. Resolution Ladder

Graphics should support low-to-high resolution reads:

```text
presence
rough mass or contour
feature sites
ordered spans
curve parameters
sampled points
solver/export reference
```

A table with rounded corners can begin as:

```text
table-like surface
four corner feature sites
some rounded-corner evidence
residue for exact size, radius, proportions, and style
```

Then it can refine into edges, radii, fill, stroke, material, and exact
external output references. The low-resolution read remains valid while the
details settle.

## 6. Attribute Participation

Attribute presence should come before attribute detail.

```text
stroke participates
stroke width unresolved
stroke width coarse
stroke width exact

fill participates
fill color unresolved
fill color family
fill exact RGB
fill material or lighting evidence
```

Unknown stroke width is not zero width. Unknown fill color is not transparent.
Missing detail should remain available as unresolved structure, residue, or
tension.

## 7. Near-Term Milestones

1. Keep the current point, radial flow, segment, and directed-segment tests
   passing.
2. Continue expanding `GraphicReadGraph` so GojoDraw and future consumers can
   consume carried local reads rather than flat shape tests.
3. Add a small curve-control surface for quadratic and cubic Bezier-like
   definitions.
4. Add contour partition helpers for arcs and circular phase.
5. Add explicit fill and anti-fill format helpers over closed or bounded
   boundaries.
6. Add style participation surfaces for stroke, fill, color, and width.
7. Add resolution-ladder examples for coarse-to-fine graphic reads.
8. Add local read witnesses for corners, rounded corners, intersections,
   crossings, branches, and merges.

Each milestone should prefer relation structure over wrapper classes. Helper
APIs are acceptable when they only build or inspect ordinary Gojo structure.
