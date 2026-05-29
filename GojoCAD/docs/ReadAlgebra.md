# GojoCAD Read Algebra

GojoCAD structure is the source of meaning. A read algebra is an inspection
surface generated from that structure at a chosen purpose, resolution, and
time. It should make local discoveries easier to reuse without becoming a
parallel model that owns the concept.

The read result should usually be a graph, not a strict tree. Gojo structures
share carriers, locations, unit shapes, and `TickRef` sites, so the read must
preserve shared evidence instead of duplicating it into unrelated branches.

```text
GojoCAD relation graph
  canonical structure
  carriers, locations, relations, unit shapes, residue, tension

derived read graph
  temporary inspection surface
  local observations with links back to source structure
  ambiguity, partial detail, sampled choices, and unresolved residue
```

If a read becomes important enough to persist, it should crystallize back into
ordinary Gojo structure as a witness relation. Until then, throwing away the
read graph must not change what the structure means.

## Recognition Direction

Recognition should be algebraic and bottom-up. Instead of testing every known
shape at every relation, the reader should inspect the relation's left side,
carry those findings into the right side, and narrow the possible reads by
local compatibility.

For example:

```text
Relation(left, right)
  read left
  read right
  combine compatible local findings
  carry resulting findings upward
```

The carried result can include several compatible reads. A relation of two
coincident points can be an ordinary zero span, a line seed candidate, and a
future orthogonal source. A parent relation may refine one of those readings
without erasing the others.

## Lower Primitive Table

| Left read | Right read | Local constraint | Carried read |
| --- | --- | --- | --- |
| `CarrierLocation(c,t)` | carrier `c` | same carrier identity | point |
| point `P(c,a)` | point `P(c,b)` | same positive carrier, `a != b` | positive segment/span |
| point `P(c,a)` | point `P(c,b)` | same positive carrier, `a == b` | zero span, coincident site, line seed candidate |
| point `P(c-,a)` | point `P(c,b)` | inverse and positive faces of same carrier | directed segment, fused carrier-face read |
| point `P(c,a)` | point `P(c-,b)` | positive and inverse faces of same carrier | directed/fused face with reversed order evidence |
| point | unknown zero span | right side is `Unknown[0] -> Unknown[0]` | point orthogonal outlet, radial seed |
| zero span | unknown zero span | left read preserved as coincident segment | line/ray orthogonal outlet |
| nonzero segment | unknown zero span | left read preserved as span | sheet/region orthogonal outlet |
| point on circular carrier | circular carrier basis | unit shape is `CircularFrom(origin)` | circumference marker, contour partition |
| point on circular carrier | point on same circular carrier | distinct ticks | circumference span, arc candidate |
| point on base carrier | point on circular carrier | circular origin is on base carrier or attached through a shared site | radius/contour attachment |
| segment | point on same carrier | point tick falls inside segment ticks | segment partition tick |
| circumference span | point on same circular carrier | point tick falls inside span read | contour partition tick |
| span | span | shared endpoint, carrier location, or `TickRef` | connected path fragment |
| span group | shared site | local valence greater than two | branch, merge, junction candidate |
| any read | style participation | style carrier attaches to selected read | styled read with unresolved or exact detail |
| any read | resolution participation | carrier/unit ladder relates to selected read | coarser or finer read surface |

This table is not a shape enum. Each row is a local composition rule. The read
may carry multiple rows upward when the structure supports them.

## Evidence To Carry Upward

A useful read graph node should preserve enough local evidence for parent reads
to narrow without rediscovering the entire subtree:

```text
source element
recognized local reads
source carrier locations
shared TickRef sites
carrier face evidence
unit-shape evidence
child read links
attachments to neighboring reads
unresolved residue or tension-like missing detail
sampled values used by this read pass
```

The sampled values belong to the read pass, not to the structure. They are
useful for rendering or provisional interpretation, but they are not facts
unless crystallized back into Gojo structure.

## Navigating With New Information

When new information appears, the reader should navigate from the affected
local site instead of rescanning as a flat classifier:

1. Re-read the touched relation, carrier, location, or unit shape.
2. Update the local read node and any evidence it carries.
3. Follow shared carriers, `TickRef` sites, parent relations, and attachment
   edges to the next affected reads.
4. Preserve unresolved or incompatible evidence as ambiguity, residue, or
   tension-like read state.
5. Let consumers render, solve, export, or condition AI from the current read
   graph at their requested resolution.

This makes discovery incremental. A point can become a segment endpoint, then a
partition, then a branch site, without the renderer needing to ask every
possible shape question again.

The first executable surface exposes this as:

```text
GraphicReadGraph.NavigateFrom(changed, radius)
  local nodes around a changed relation, carrier, location, or TickRef

GraphicReadGraph.AffectedReadsFrom(changed, radius)
  reads carried by that local neighborhood

GraphicReadGraph.RefreshFrom(changed, radius)
  a rebuilt read graph plus previous/current affected neighborhoods
```

This is still explicit refresh rather than live eventing. It gives consumers a
bounded neighborhood to reconsider when a shared site moves or new structure is
attached, while the canonical source remains the relation graph.

## Direction For GojoDraw

GojoDraw should eventually consume this read graph rather than running its own
flat recognizers. Rendering then becomes a boundary lens:

```text
GojoCAD structure
  -> derived read graph
  -> render recognized reads
  -> sample or draw partial/tension-like detail when exact detail is absent
```

Recognized structure should render as present. Missing radius, period,
orientation, style, or sampled points should produce partial, sampled, or
unresolved visualization instead of a failed draw branch.
