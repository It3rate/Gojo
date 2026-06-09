# Participation Settlement And Boolean-Style Reads

This note records the current Gojo direction for boolean-style reads across
grades. The word "boolean" is useful shorthand, but the target is broader:
support, overlap, filtering, cutout, occupancy, absence, blockage, routing, and
settlement should all be readable as Gojo structure.

The core rule is:

```text
boolean-style operations are reads of occupied flow or participation
through a boundary-flow basis over a selected scope
```

Boundaries can create, route, limit, witness, or explain flow. A bare boundary
is not itself the boolean operand unless it carries explicit participation.

## Boundary, Flow, And Containment

Keep these three ideas separate:

```text
boundary
  discrete structure: point, endpoint, segment, outline, rim, edge, cut, site

flow or participation
  occupied tendency: support, fill, anti-fill, radiation, traversal, capacity,
  blockage, acceptance, transfer, or pressure

containment
  a settled bounded read of flow through boundary evidence
```

Flow can exist without containment. A point may radiate without yet saying how
far it reaches. A `C`-shaped boundary can have a readable inside and outside
without closing a contained area. A cup is a stronger version: its rim and wall
route an interior/exterior read even though the opening remains important.

Containment is therefore not the source of flow. It is one high-resolution
settlement that can occur when boundary and flow evidence support it.

No-flow is also real information. A boundary with no fill or no flow should not
accidentally participate in union/intersection reads. If that boundary is meant
to remove material from another area, Gojo should first create a negative,
anti-fill, blocked, or otherwise opposed participation surface, then settle the
flow read.

## Identity Is Not Intersection

Two points at the same tick reference are not `A AND B`. They are one shared
site or one shared reference. Two distinct point structures with numerically
equal locations are still not boolean-overlapping until a read projects them
into a shared flow or participation basis.

This matters because discrete objects in a finite-resolution system do not
become true by coincidental placement. Equality, shared tick reference,
low-distance contact, and support overlap are different reads:

```text
same tick ref
  identity or shared site evidence

equal coordinate through a basis
  equality claim or projection read

low-distance contact
  contact/gap read with residue or tolerance

shared occupied participation
  boolean-style support/tension read
```

Only the last shape is boolean-style.

## Grades

The same participation-settlement shape should work across grades.

### Grade 0: Site Or Point

A point is a site. It may carry no flow, open flow, radial flow, blocked flow,
or a specific occupied lane.

```text
point with no flow
  boundary/site evidence only

point with radial or open flow
  site plus flow participation, possibly unbounded

point support read
  occupied participation at the selected site through a chosen basis
```

The point-level boolean read is site occupancy, not primitive `true` or
`false`. "False" only means uncovered or unoccupied relative to an explicit
scope and basis.

### Grade 1: Segment Or Span

A segment has boundary sites and an interior span:

```text
start/end points
  boundary evidence

between them
  interior flow or support span, if participation is present
```

The existing core support reads already split a carrier at segment boundaries
and read support, tension, exclusive support, covered spans, and uncovered
spans. Those are the low-resolution grade-1 boolean foothold.

The next refinement is to carry boundary-flow lane evidence on each partitioned
span, so positive, negative, inverse, negative-inverse, no-flow, and blocked
participation remain visible instead of collapsing into one scalar support
answer.

### Grade 2: Area Or Region

An area has boundary outline evidence and interior participation:

```text
outline
  boundary evidence

fill / anti-fill / blocked fill
  flow or participation evidence

settled contained area
  high-resolution read where boundary and flow agree
```

A closed outline with no fill is not an area boolean operand. It can still be
measured, rendered, offset, selected, or used to generate flow. To subtract a
circle from a rectangle, the circle boundary should produce anti-area or
negative interior participation, then the rectangle fill and circle anti-fill
settle together.

Open outlines can still carry flow. A `C` shape, cup, channel, groove, or
partial contour may expose inside/outside or passage behavior even when the
containment read remains unresolved. That unresolved part should remain as
residue, not be hidden behind an invalid-shape failure.

### Higher Grades

The same split extends upward:

```text
surface shell
  boundary evidence for a volume-like read

volume fill / void / pressure / material
  participation evidence

settled solid, pocket, cavity, or passage
  higher-grade containment or support read
```

The names change, but the Gojo shape does not.

## Read Shape

The desired relation shape is:

```text
participation settlement read
  selected participants
  selected scope or bounded whole
  boundary evidence
  flow / participation basis
  partitioned sites, spans, fragments, or cells
  occupied lane evidence for each partition
  support / tension / exclusive / uncovered read
  cancellation or non-settlement witness
  result participation
  residue or tension
  provenance or timeline
```

This is a shape to build with carriers, relations, unit shapes, lane surfaces,
read witnesses, and residue. It should not become a meaning-owning boolean
class or symbolic operation table.

## Boolean-Style Names As Low-Resolution Reads

Traditional names can remain as convenient read labels:

| Name | Gojo read |
| --- | --- |
| Union / OR | occupied support in at least one selected flow source. |
| Intersection / AND | shared occupied participation in the same cell, span, or fragment. |
| Exclusive / XOR | occupied participation that does not settle as shared support. |
| Difference | selected positive support with opposing, blocked, or anti-participation from another source. |
| Complement / NOT | uncovered participation inside an explicit scope. |
| Containment | all requested cells settle as occupied through a chosen boundary-flow basis. |

These names are projections. The richer structure is the partition, occupied
lanes, settlement evidence, and residue.

## Positive, Negative, Inverse, And Read Direction

Geometric location should stay separate from interpretation:

```text
same point or outline
  stable boundary structure

different perspective
  different basis or lane read over that structure

different measurement direction
  different traversal/projection route over the same basis
```

A clockwise outline measured backward does not become a different outline. A
segment with start/end meaning adjusted does not move its endpoint locations.
Those are different reads over stable boundary evidence.

Positive and negative participation in the same perspective can cancel when
transition/cancellation evidence connects them. Positive and inverse
participation are different flow faces. They should remain fused, unresolved,
or tension-bearing until a specific fold, projection, or phase transition read
settles them.

## Geometry First, Concepts Later

The geometry use case gives the clearest implementation path:

```text
point occupancy
  selected site plus flow lane

segment overlap
  partitioned spans plus support lanes

area union or cutout
  boundary fragments plus fill/anti-fill lanes

radial shape ladder
  offset boundaries generate fills, fills settle into outlines, residue and
  cancellation remain inspectable at each rung
```

But the same structure should later support non-geometric concepts. A filter,
bridge, valve, attention gate, policy gate, dataflow route, or conceptual
selection surface can all read as occupied participation through a selected
basis. The concept word is a later read over the structure, not the source of
meaning.

## Implementation Direction

Core should keep the existing grade-1 span reads, but expose a richer
participation-settlement witness around them. GojoCAD should emit the same kind
of witness around area fragment settlement. GojoDraw should render boundary,
flow, result, residue, and tension as separate inspectable layers rather than
flattening everything into a filled shape too early.

Implementation should prefer:

- relation-shaped read witnesses over operation enums
- local partition/cell reads over global classifiers
- explicit flow/participation lanes over boundary-only booleans
- anti-fill or blocked-flow structure over implicit subtraction
- residue/tension when an open or incompatible boundary cannot settle cleanly
- stable geometric boundary evidence with multiple possible read perspectives

The first tests should prove that point occupancy, segment support, and area
fill settlement produce analogous support, intersection, exclusive, and
uncovered reads while preserving incompatible or unresolved participation.
