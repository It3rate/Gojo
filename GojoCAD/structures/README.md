# GojoCAD Structural Concept Shapes

This folder holds the first pass at unparameterized graphic structure. These
shapes are not render commands and they are not code-side type labels. A helper
such as `ClothoidSection` or `Branch` builds ordinary Gojo relation structure:
ports, unresolved trace slots, local evidence, and tension/residue.

The base drawable primitive is a section: a start port, an unresolved trace, an
end port, and tension. Straight lines, circular arcs, Beziers, clothoids,
involutes, and splines are higher rungs over that same lower section idea. The
curve family is represented by the shape of the evidence surface rather than an
enum.

The current ladder is:

| Rung | Concept surface | Added evidence |
| --- | --- | --- |
| 0 | site / port | unresolved position, flow, tangent, normal, residue |
| 1 | section | two ports plus unresolved trace |
| 2 | curve family | straight, circular, Bezier, clothoid, involute, spline evidence |
| 3 | endpoint behavior | direction, endpoint tangents, orthogonal/normal evidence |
| 4 | connection | join, tangent join, open path, branch, merge |
| 5 | boundary/body | closed boundary, area, anti-area, thickened section, offset |

## Composite Shapes

Connecting concept shapes knowingly creates a new current concept shape. It does
not have to wait for a later read to become true. A later read is useful for
rediscovering the same surface from arbitrary structure, but construction can
create the higher rung immediately.

A `CompositeShape` has:

- exposed ports: the ports visible from the current rung
- visible surface: the top concept surface to treat as current
- lower structure: the child sections, prior composites, and other evidence
- interior joins: the consumed joins or junctions hidden inside the current
  surface
- previous rung: provenance for replay/refinement
- residue/tension: unresolved compatibility, missing settlement, or ambiguity

The exposed-port rule is:

```text
composite exposed ports =
all child ports minus the ports consumed by joins
```

Two sections joined end to end become a `CompositeSection`: four child ports,
two consumed by the interior join, and two exposed ports. The visible surface is
therefore section-like again, even though the lower child sections and the join
remain inspectable.

Three or more sections meeting at a shared interior junction become a
`JunctionShape`: the visible surface has three or more exposed ports. Direction
is an elaboration over that junction surface, so branch and merge are directed
multi-port composite shapes rather than separate primitive tags.

## Resurfacing A Composite

A composite does not have to stay the same kind of visible surface as its lower
parts. Two straight lower sections can first fold into a two-port composite
section, and a later rung can resurface that composite as a single curve.

```text
straight A + straight B -> composite AB
composite AB + curve evidence -> curved AB'
```

The lower straight sections and their interior join remain inspectable. The new
current rung exposes the same two outer ports, but its visible surface can be a
generic curve, circular arc, Bezier, clothoid, involute, spline, or another
future section structure. This is deliberately separate from `Join`: joining
establishes connectivity, while resurfacing changes what the current rung uses
as the visible section.

## Editing Inside A Composite

When a later operation focuses an interior join of a composite, the earlier
composite is not mutated in place. The operation creates a new current rung.

Example:

```text
A + B -> AB
AB exposes two ports and hides interior join J.

Attach C at J:
A, B, C remain lower leaves.
AB remains prior composite/provenance.
ABC becomes the current junction shape with three exposed ports.
```

This lets a transform work through a high-level shape, descend to a lower-rung
join, and still produce a clean current surface. The prior surface stays
available for replay, history, and structural ladders. A later crystallization
step may choose to forget that provenance, but this structural concept layer
keeps it by default.

## Discovery From Completed Structure

Construction is not the only way to get a concept shape. A finished structure
can also be inspected and folded into a fresh current concept surface.

`TryDiscoverCompositeShape` currently verifies this first step. It reads local
section concepts and looks for:

- shared endpoint port references
- explicit `PortJoin` evidence
- existing composite shapes

From that evidence it creates a new `CompositeSection` or `JunctionShape`. The
new surface has lower structure and interior joins, but its previous-rung slot
is unresolved tension unless real provenance was present. This is intentional:
discovery should not pretend to know the edit history.

This is the path for importing completed geometry. For example, an SVG adapter
can convert path endpoints and curve pieces into Gojo ports/sections first.
Then discovery can infer joins, exposed ports, and junctions from the resulting
Gojo structure. Later reads can extend the same pattern to tangent continuity,
closed boundaries, area/anti-area, branch/merge direction, and curve-family
evidence.

`Elaborate` creates a compact higher-rung shape by keeping the lower rung,
adding evidence, and preserving unresolved residue. `ElaborationLadder` records
the same move as a structural ladder rung, so the lower and higher structures
remain independently traversable.

Most of this structure is intentionally ambiguous. The ambiguity is represented
as placeholder/tension structure so a later reader can settle a concrete line,
arc, tangent, branch, or area without pretending the current rung already knows
coordinates or curve parameters.
