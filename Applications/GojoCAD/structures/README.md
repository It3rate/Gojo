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

## Offset Surfaces

Offsets are structural requests over drawable sections or boundaries, not
precomputed Cartesian polylines. `GraphicOffsetSurface` records:

- source: the line, arc, Bezier, clothoid, involute, path, or boundary being
  offset
- distance/radius evidence: the amount of separation, which may be exact,
  parameterized, sampled, or unresolved tension
- side pair: opposed interior/exterior witnesses
- direction: the positive flow used to interpret the side pair
- closure: unresolved or explicit evidence for how an open or partial boundary
  should be completed when a fill asks for one
- residue/tension: whatever the current read cannot settle

The side pair deliberately separates "interior/exterior" from "left/right".
Every section has a natural positive flow, and a reader may also sample or read
the inverse flow when a fill needs a consistent boundary direction. Once a
direction has been chosen, the renderer can convert the section to a Cartesian
tangent/normal field and place the red interior and green exterior samples on
opposed sides. That conversion is not the Gojo meaning; it is only a display
read.

Full closed shapes such as circles can settle interior/exterior against a center
or area witness. Partial shapes can still have local side evidence. A letter C,
for example, can expose an interior side for most of its arc and keep the open
gap as closure tension. A render pass may visualize that closure as a faint
line, gradient, clothoid, or other approximate bridge without claiming the
source is fully closed.

Offset rules by curve family:

| Family | Structural offset meaning | Render-boundary approximation |
| --- | --- | --- |
| line / segment | parallel section through the normal side evidence | translate endpoints by the chosen normal |
| circle / circumference | radial offset against center/area evidence | radius plus/minus offset distance |
| arc / circular span | partial radial offset with endpoint closure residue | radius plus/minus offset over the span |
| quadratic / cubic Bezier | offset through endpoint tangent/control evidence | sampled tangent normals or fitted offset curve |
| clothoid | offset of a curvature-transition section between Gojo elements | sample/render the transition with a numeric clothoid or Bezier approximation, then offset normals |
| involute | offset of an unwinding section from circular evidence | sample the involute and offset its normal field |
| open path / partial boundary | local side evidence plus closure tension | offset segments/curves locally and render closure residue if requested |

For filled offsets, the fill should read from the current offset boundary rather
than from a separate copied polygon. If the offset outline changes, the fill
must move with that outline as the same structural result. A temporary sampled
polygon can still be used at the render boundary, but it should be treated as a
projection of the outline read, not as independent Gojo meaning.

## Area Participation Settlement Surfaces

`GraphicAreaParticipationSurface` carries fills and outlines as area
participation and settlement evidence, not as code-owned set truth. It carries:

- input area/fill participants in an ordered area frame
- requested/result outline evidence
- settlement evidence, either the older boundary-cancellation foothold or a
  richer participation-settlement witness
- residue/tension for unsettled overlap, winding, holes, and boundary pieces

`GraphicAreaSettlementSurface` performs the current geometric area read by
splitting boundary fragments, reading occupied positive/inverse participation,
and emitting settled area frames, outline witnesses, residue, and fragment
partitions. Area union surfaces should carry that participation-settlement
witness whenever the occupied flow has been read.

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

## Parameter Surfaces

`GraphicParameterSurface` is the first parameterization layer for these graphic
structures. It still builds ordinary Gojo relation structure rather than a
code-side parameter bag.

`NormalizedValue` records a reusable 0..scale parameter on a basis surface. The
surface keeps both a coarse parameter/result and a refined parameter/result,
plus a `ResolutionLadder` so the read can distinguish rough placement from
later detail. The scale does not need to be decimal; the ladder levels carry
whatever ratios the structure supplies.

The exact normalized-value reader requires a concrete 0..positive-scale span
and coarse/refined parameter points on that same carrier face. A malformed or
unbound domain remains structure without supplying this stronger read. The
reader recognizes supplied evidence; it does not verify the result calculation.
The [syntax geometry exercise](../../../Gojo/docs/GojoSyntaxGeometryReview-2026-09.md)
tests this distinction along with partial templates and curve constructions.

The helper can now build that resolution evidence directly from a refined tick
and supplied level scales. This keeps sample code from owning the tick
decomposition model. Sketch samples still compute concrete points for display,
but their parameter evidence is anchored to basis surfaces such as the wall
width, wall height, roof line, or leaf-local center/radius spans.

`TemplateInstance` records that a concrete result came from a reusable template
under a binding frame. The read requires real placeholder/descriptor slots and
bindings for those slots, so it is not just a four-field wrapper. In the
SketchViewer house ladder, the three tree leaf blobs are instances of one leaf
template. Their centers and radii are normalized parameters over local basis
spans; the earlier tree crown rung uses coarse circular instances, and the
later rung uses refined blob instances.

`TemplateInstanceFromBindings` is the convenience construction for the same
surface when the caller already has relation-shaped slot bindings. It exists as
a separately named helper so the binding-frame surface and the binding-list
surface cannot be confused by overload resolution.

The same surface now carries the window, door, roof-overhang, and chimney
examples. The opening templates bind center/size parameters against the house
wall basis, with separate coarse X and Y center evidence rather than one shared
coarse point. The window additionally refines a corner-radius parameter from a
coarse rectangle to a rounded rectangle. The chimney binds a cap-rise parameter
as zero for the body rung and positive for the capped rung. These are still
ordinary structural ladder transitions: the drawn result is concrete, while the
application provenance keeps the normalized parameter and template evidence
available for reads and replay.

The SketchViewer house sample has begun using those basis surfaces as
construction anchors too. The horizon, wall spans, roof overhang, chimney foot,
tree root, tree junction, and branch tips are derived from local 0..1-ish basis
spans before they are rendered. The tree split/curve/thickening/merge rungs now
carry a reusable topology template with normalized root, junction, branch-tip,
and crown-center evidence. This is still sample-side calculation for concrete
pixels, but the corresponding Gojo structure records the basis and parameter
surfaces needed for later replay or replacement.

The opening and chimney refinements now go one step further: the door rectangle,
arched door, window rectangle, window cross, rounded window, chimney cap, and
door knob are constructed from their center/width/height/radius/rise parameters.
The rendered points are still concrete frame points, but those points are
calculated from the same values bound into the template instance, reducing the
gap between "evidence says this is parameterized" and "the shape was actually
created from the parameters."

The cloud and tree foliage follow the same direction. The cloud seed and puffy
outline are now instances of one anchor/width/height template, with the puffs
constructed from a normalized local profile. Leaf blobs bind the same reusable
profile surface alongside their center and radii, and branch curve controls are
derived from junction/tip landmarks plus local bow values instead of independent
control-point literals.

The final tree wood merge also smooths the offset branch/trunk construction.
The earlier rung can still show offset pieces as a construction approximation,
but the merge rung adds one closed cubic outline around the trunk and both
branches. A soft sampled fill remains underneath because shared-frame area
reads currently derive closed areas from segment boundaries only; a later read
graph upgrade should allow cubic/arc boundaries to participate in fills without
collapsing the curve offset back into straight segments.
