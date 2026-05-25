# Gojo Potential Shape Fact Sheet

This note consolidates the current working model for potential shape,
orthogonal expansion, containment, and hardening. It should guide future Gojo
structure and naming, especially around `UnitShape.CircularFrom`,
`UnitShape.LinearFrom`, and `UnitShape.SheetFrom`.

## 1. Potential Is Unresolved Possibility

Potential is not stuff, fill, or a completed object.

Potential means the structure has a possible settlement direction that has not
yet been bounded, calibrated, or hardened. "Unbounded" does not mean very large
or infinitely large. It means the extent is not yet known by the current
structure.

For example, a point's outward potential might later harden as a small circle,
a long path, a polygonal route, or nothing at all. Before the bounding
structure exists, Gojo should not pretend the potential already occupied a
large area.

```text
potential      possible settlement surface, not yet bounded
boundary       relation that constrains potential into a site, contour, or span
calibration    relation that makes a potential measurable or countable
hardening      repeated/stable settlement becoming carrier/unit structure
```

## 2. Source Structure Meets Container Space

The same source can express different potential depending on the space or
container it participates in.

```text
point in point-like space    remains point-like
point in line-like space     aligned flow in two opposed directions
point in plane-like space    radial field plus level contours
point in volume-like space   radial field plus shells/spheres
```

It is reasonable to think of these as one generative impulse constrained by the
available degrees of freedom. But Gojo should not store an all-dimensional
sphere and then slice it down. Gojo should represent the source structure and
the currently available/container structure, then read the shape that their
connection exposes.

So the rule is:

```text
realized potential = source structure through available/container structure
```

The source does not know every dimension by itself. The container/read
structure decides which distinctions are available.

## 3. Point Potential

A point supplies place but not orientation.

When point tension cannot be accounted for by the existing local structure, it
can explore all directions available to its container at equal pace. In a
point-centered field, aligned movement away from/toward the point is radial
potential; equal-tension movement around the point is contour potential.

```text
point + no available dimension      point
point + one available dimension     forward/inverse flow along line
point + two available dimensions    radial flow plus circular/level contour
point + three available dimensions  radial flow plus shell/sphere contours
```

`UnitShape.CircularFrom(point)` should be read as a point orthogonal or
level-contour potential carrier. It is not a finished circle. It has circular
provenance, but it does not yet know radius, zero marker, direction,
perimeter length, interior, or fill.

A perimeter or contour becomes more concrete only when relation structure
bounds or calibrates it:

```text
Point(center,n) * orthogonal              level-contour potential
Point(radius,r) -> Point(perimeter,p)     radius endpoint touches contour marker
Point(perimeter,k) -> Point(perimeter,0)  countable closed contour partition
```

## 4. Segment Potential

Two positions create segment structure even when the positions are coincident
at the current resolution.

A zero-length segment is not the same as a point. It has two related points
sharing a visible site, so it carries a collapsed basis. That basis may later
be stabilized by a relation to another segment or by endpoint separation.

```text
point                         place without orientation
zero-length segment           collapsed basis, orientation unresolved
nonzero segment               local orientation and extent
```

A segment has several natural potential expressions:

```text
aligned continuation       flow beyond either endpoint
orthogonal side potential  sheet/side-wall/shell around the segment
parallel contour           equal-distance or equal-tension offset from segment
```

Endpoint escape is intentionally not a default segment potential in this
sheet. A bounded segment is a section of a larger linear potential, so curved
or circular endpoint behavior should come from additional route, boundary, or
container structure when that structure is actually present. This leaves gears,
belts, caps, rounded ends, and branch routes under explicit structural control
instead of making every segment endpoint automatically circular.

`UnitShape.LinearFrom(zeroSegment)` should be read as a zero-segment
orthogonal: a ray/line potential constrained by a collapsed basis. It does not
know external direction until connected to an oriented structure.

`UnitShape.SheetFrom(segment)` should be read as segment orthogonal potential.
In a two-dimensional read, this may look like a sheet or rectangle-like
surface. In a three-dimensional read, related offsets may look cylindrical or
shell-like. The segment itself should remain the source structure; "sheet" is
the current named foothold for that orthogonal use, not a separate symbolic
mode.

## 5. Potential Versus Hardened Objects

Potential does not contain completed objects.

A line potential does not contain all segments. A circular potential does not
contain a completed circle. A spherical potential does not contain all volumes.
Those objects appear when structure bounds, calibrates, or hardens some part
of the potential.

```text
line potential      possible flow carrier
segment             bounded or related span on that carrier
contour potential   possible equal-tension carrier around a source
circle/perimeter    bounded or partitioned contour
fill/disk/area      bounded interior/exterior settlement
volume             hardened bounded three-dimensional fill
```

Fill requires a boundary relation or equivalent hardening structure. A
boundary-only contour can exist without fill. A filled region and an empty
boundary are different settlement claims and should remain distinguishable as
structure.

Fill is not limited to area or volume. A linear segment can be only two related
points, or it can be a filled interval between those points when
multiplication-like potential activates and hardens the in-between
participation. That line fill is the same general fill idea as area fill or
volume fill, constrained by the available/container structure. It can also have
opposed or inverse fill participation.

This means a filled segment is not identical to its endpoints or to the
construction relation that discovered them. When crystallized, the segment fill
is an ordinary carrier holding the interval magnitude and the forward or inverse
face of that participation. It does not need to know the start point or end
point unless that construction provenance is still preserved by neighboring
relations. A crystallized area behaves the same way at a higher grade: it is a
carrier of square participation, while boundary access is optional preserved
structure rather than the fill itself.

Before crystallization, a fill may keep its boundary as the source of truth and
measure itself only when read. If the boundary moves, the next measure read uses
the new boundary. Once crystallized, the measured carrier is independent; later
changes to the original boundary do not rewrite that crystallized value unless a
live relation explicitly connects them again.

Non-additive operation results are the same kind of hardening. Relating two
length-like bounds through a segment-output operation can produce an area-like
carrier whose value is the filled participation. The operands remain the
construction bounds from which a rectangle or other source shape may be
computed. If the same relation is kept only as a reusable transform, the result
may be discarded; this is closer to ratio/process use than to saved fill.
Reusable origin bases, such as `0..1` unit frames, may read later carriers with
the same unit shape. Positioned frames remain local to their carrier unless a
relation explicitly connects or projects them.

This is a key distinction. A square made from four boundary segments can remain
linear boundary structure; its interior does not fill merely because the
segments enclose a region. Fill appears when orthogonal potential is activated
and bounded, for example by a multiplication-like use of the boundary:

```text
boundary-only shape       connected contour, no interior material claim
boundary * orthogonal     fill potential activated
bounded fill potential    interior hardens as area/material participation
exterior orthogonal       unbounded outside potential, interior may remain empty
```

This lets branching, merging, outlines, tracks, belts, and frames exist without
accidentally creating area. Fill is a settlement claim, not an automatic
consequence of closure.

## 6. Shared Rule For Shapes

The common rule is:

```text
source structure + unresolved participation + available container
-> candidate potential
-> boundary/calibration/route
-> hardened carrier, contour, fill, or relation
```

This applies beyond points and segments:

```text
multiple connected segments     polygon/graph potential
closed boundary                 interior/exterior pressure potential
parallel contours               shell/buffer potential
enclosed volume boundary        inside/outside volume potential
junction                        branch/merge/route potential
```

The same rule should avoid special-case shape classes. If a behavior seems to
need a new symbolic type, first ask which source, container, and calibration
relations would make it discoverable.

For additive grouping, attributes, and observer reads over these shape
structures, see `Gojo Groups Attributes And Lenses.md`.

## 7. Direction And Inverse

Each settlement orientation has inverse directions.

```text
aligned/radial orientation   outward / inward
flow orientation             forward / inverse
contour orientation          contour-forward / contour-inverse
surface orientation          filled / empty, inside / outside
```

For ordinary line carriers, the forward face is the normal positive direction
and the inverse face is the opposed direction. This does not mean "right" until
the line is calibrated to a space where right and left exist. For perimeter
carriers, the forward face is the normal contour direction and the inverse face
is the opposed contour direction. This does not mean clockwise or
counterclockwise until the contour is calibrated to a view/container where
front/back, up/down, or handedness exists.

A floating grid has no true up. A drifting contour has no true clockwise. It
may have internally consistent forward/inverse relations, but names like up,
right, clockwise, and counterclockwise are supplied by later attachment to a
field, screen, gravitational frame, observer frame, or other oriented
container. If all internal definitions are consistent, attaching the structure
to such a container should make the visible orientation settle correctly.

## 8. Code Implications

Current constructor names are useful but should be interpreted carefully:

```text
CircularFrom(point)       point orthogonal / level-contour potential
LinearFrom(zeroSegment)   collapsed-basis orthogonal / ray-line potential
SheetFrom(segment)        segment orthogonal / sheet-side potential
```

These constructors create unit-shape provenance. They do not create completed
geometric objects.

Do not create wrapper classes such as `Circle`, `Calibration`, `Sheet`, or
`Sphere` merely to gather origin, extent, marker, fill, and direction. Those
facts must be connected by relation structure. Code may add local inspection
helpers, but if the helper disappears, the Gojo structure should still be able
to express the concept.

Prefer:

```text
carrier.UnitShape -> source provenance
relations         -> boundary/calibration/route/fill
local reads       -> what can be discovered from those structures
```

Avoid:

```text
external classifier receives unrelated pieces and declares their meaning
wrapper object stores the only connection between the pieces
metadata flag says circle/sheet/fill instead of relation structure
```

## 9. Open Questions

- How should Gojo represent the available/container space explicitly?
- Is orthogonal outlet enough for all potential directions, or do we need a
  visible relation between source and container grade?
- What is the minimal relation structure that makes contour handedness
  inspectable rather than defaulted?
- How should potential harden into fill without confusing boundary-only
  contours with material area/volume?
- Can a single repeated rule express point, segment, closed-boundary, and
  higher-dimensional potential without adding symbolic shape modes?
