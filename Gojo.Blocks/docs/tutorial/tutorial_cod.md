# Gojo Blocks Tutorial Script

This is a page-by-page tutorial for Gojo Blocks. It is written like an audio
transcript with animation notes. The audience is programmers who are
comfortable with vectors, graphs, projections, units, numerical resolution, and
basic topology, but who have not seen Gojo's physical style of math before.

The tutorial should teach the intuition first, then attach the Gojo names to
that intuition. The visual should never be a decorative metaphor that replaces
the model. Each screen should show the kind of Gojo structure it is describing:
carriers, locations, relations, unit shapes, flow faces, boundary evidence,
residue, tension, witnesses, and ladder rungs.

## Production Format

Each screen has four parts:

- **On Screen**: the static composition for the page.
- **Animation**: how the scene moves over time.
- **Narration**: the audio transcript.
- **Gojo Structure**: the carrier/relation/unit-shape surface the page is
  trying to reveal.

The screen order is intentional:

```text
resolution
  -> tension
  -> duality
  -> site
  -> point flow
  -> second point
  -> line or radial extent
  -> boundary and flow
  -> orthogonal escape
  -> orientation
  -> partition
  -> tick
  -> unit shape
  -> calibration
  -> crystallization
  -> number
  -> dimensions
  -> settlement
  -> lanes
  -> support
  -> branch / merge
  -> ladders
  -> equations
  -> concepts
```

## Screen 01 - Title: Physical Math

**On Screen**

A dark-neutral field with a few faint channels, points, and boundary marks.
Nothing is labeled yet. A small pulse enters from the left and moves into the
field.

**Animation**

The pulse partially settles into one channel. The rest remains as a visible
glow near a boundary. The scene pauses with both the settled part and the
unsettled part visible.

**Narration**

Gojo is a math kernel that tries to keep structure physical. A value is not
only a scalar in a variable. It is something participating in a local world:
places, channels, boundaries, flow, unresolved pressure, and stable units.

That changes the order of explanation. We will not start with algebraic
operators. We will start with what a read can distinguish, what it cannot
distinguish, and what happens when structure receives more than it can account
for.

**Gojo Structure**

This screen introduces the visual language:

- carriers as rails, channels, or reservoirs
- locations as pins or occupied sites
- relations as ordered links
- tension as visible pressure
- residue as the part a read leaves unresolved

## Screen 02 - Resolution Comes First

**On Screen**

One horizontal span. Above it, a coarse ruler with four large divisions. Below
it, a finer ruler with sixteen divisions. A marker sits somewhere between two
coarse ticks.

**Animation**

The scene alternates between the coarse read and the fine read. In the coarse
read, the marker snaps to the exposed coarse distinction and leaves a small
remainder glow. In the fine read, more of the span becomes distinguishable.

**Narration**

Every Gojo read has resolution. A read exposes a smallest distinction. Anything
below that distinction is not secretly known by the read.

This is not display rounding over a hidden real number. It is a structural
fact about the read. If the surface exposes whole ticks, then sub-tick detail
is unavailable until another structure makes it available.

**Gojo Structure**

Show a carrier read through two different basis surfaces:

```text
coarse: S(C,0,4)
fine:   S(C,0,16)
```

The same source can be read through either surface. The lower-resolution read
is still valid; it just leaves more residue.

## Screen 03 - Tension Is Not Failure

**On Screen**

A channel with a marked capacity boundary. A pulse of seven units enters a
channel that can currently account for five.

**Animation**

Five units settle into the channel. Two units remain as glowing pressure at the
boundary. The glow is labeled "tension", then the label fades back into the
visual.

**Narration**

In Gojo, unresolved participation is not thrown away. If something is below
resolution, unspecified, incompatible, blocked, or not yet settled, it remains
as tension or residue.

That matters because tension is information. It tells the system where the
current structure is incomplete. It can later route, settle, crystallize, or
remain visibly unresolved.

**Gojo Structure**

Represent the accepted part and leftover part as a read witness:

```text
selected source
read surface
result
residue or tension
provenance
```

The result and residue should be separate visible elements, not a thrown
exception or a hidden error flag.

## Screen 04 - Duality Before Negative Numbers

**On Screen**

Three mini-scenes:

- a line with forward and backward arrows
- a point with outward and inward radial arrows
- a ring with clockwise and counterclockwise arrows

**Animation**

The normal direction in each mini-scene becomes bold. Then the opposite
direction becomes bold. The underlying structure does not move.

**Narration**

Everything that participates can be read with some kind of opposite. But that
does not mean all opposites are the same thing.

A negative location on a carrier, an opposed flow face, a reversed frame, an
inward radial read, and counterclockwise traversal are different structures.
They can project into negative numbers later, but negative number is not the
first idea.

**Gojo Structure**

Show the distinction explicitly:

```text
L(C,-5)      negative location on C
L(C*,5)      positive location on opposed carrier face
S(C,10,0)    reversed frame over the same carrier
```

The screen should make clear that opposition is structural before it is
arithmetical.

## Screen 05 - A Site Before A Coordinate System

**On Screen**

A single point-like site in an otherwise unmarked field. No axes. No grid.
Only a small dot and a faint local halo.

**Animation**

The dot appears first. Then a small carrier rail fades in behind it. Finally,
the relation from the location back to the carrier appears.

**Narration**

Now we can introduce the first small piece of structure. For tutorial purposes,
call it a point. More precisely, it is a local site: a place-like participation
before a full coordinate system, ruler, direction, or scale has been attached.

In Gojo, even this point is not an isolated object. It is a relation: a carrier
location grounded back to its carrier.

**Gojo Structure**

Show the construction:

```text
Point(C,n) = L(C,n) -> C
```

The point is already structure. It is not metadata with a coordinate property.

## Screen 06 - A Point Can Emanate Flow

**On Screen**

The point from Screen 05 receives a pulse. The pulse cannot be accounted for by
the point alone. A soft field radiates outward.

**Animation**

The outward field expands. Then it reverses and contracts inward. The point
stays fixed.

**Narration**

A site with unresolved tension can emanate flow. At this stage, do not imagine
a measured circle yet. Imagine an unbounded tendency: away from this site, or
toward this site.

The shape of that flow depends on the environment. In one setting it may find
a line. In another, a radial surface. In another, a spherical shell or a
higher-grade analogue.

**Gojo Structure**

The point remains boundary evidence. The emanation is unresolved participation
seeking settlement through an available outlet.

This is the physical intuition behind an orthogonal outlet:

```text
Relation.OrthogonalOutlet(point)
```

## Screen 07 - Grade Changes What Flow Can Become

**On Screen**

Three panels:

- point on a 1D rail
- point in a 2D plane
- point in a 3D volume projection

**Animation**

The same pulse enters each point. On the rail, it escapes left and right. In
the plane, it spreads radially. In the volume, it forms a projected shell.

**Narration**

The source can be the same local shape, but the grade of the surrounding
environment changes what settlement is available.

This is a recurring Gojo pattern. The structure does not need a symbolic tag
saying "be a line" or "be a sphere". It needs local evidence that makes that
settlement possible.

**Gojo Structure**

The screen should show the same point-like source read through different
surrounding unit-shape or carrier surfaces. The environment decides which
outlet is readable.

## Screen 08 - The Second Point

**On Screen**

A first point sits in the field. A second point appears. The display splits
into two possible reads:

- ordered difference
- radial extent

**Animation**

In the first read, an arrow grows from the first point to the second. In the
second read, the first point remains the center and the second point becomes a
radius marker.

**Narration**

A second point creates the first stable difference. It can do two different
things.

If the points are read in order, they define direction. That gives us linear
structure. If the second point is read as an extent from the first, it can mark
the size of a radial or contour structure without choosing a traversal
direction around that contour.

**Gojo Structure**

Show two possible surfaces:

```text
direction: P0 -> P1
extent:    center point + radius marker
```

The same visible points can participate in different reads.

## Screen 09 - Segment As Directed Frame

**On Screen**

Two points connected by a segment. The left point is marked start. The right
point is marked end. A forward arrow overlays the segment.

**Animation**

The endpoints stay fixed while the read direction flips. The arrow changes,
but the points do not move.

**Narration**

Two ordered points make a segment. A segment is not just a set of locations. It
is a directed frame. Once a directed frame exists, words like before, after,
toward, away, forward, backward, and signed read start to mean something.

Reversing a read over the segment does not move the endpoints. It changes the
surface through which the structure is being read.

**Gojo Structure**

Show the helper form:

```text
Segment(C,a,b) = Point(C,a) -> Point(C,b)
```

The order of `a` and `b` matters.

## Screen 10 - Extent Without Traversal

**On Screen**

A center point, a radius marker, and a ring. At first the ring has no start
marker and no arrow around it.

**Animation**

The radius marker moves outward and inward. The ring scales. Then a separate
marker appears on the ring, and only then do clockwise and counterclockwise
arrows become available.

**Narration**

Extent is not the same as traversal. A point and radius marker can support a
circle-like or sphere-like boundary. But the circumference does not yet know
where zero is, and it does not yet know which way is positive around the loop.

To count around the contour, another marker or orientation relation has to
appear.

**Gojo Structure**

Separate three things:

- center/site evidence
- extent or radius calibration
- contour origin and orientation evidence

Do not collapse them into one circle object with hidden fields.

## Screen 11 - Boundary Is Not Flow

**On Screen**

One circular boundary in the center. A vertical list of flow overlays sits to
the side: no flow, inward, outward, interior fill, exterior pressure,
clockwise, counterclockwise.

**Animation**

Each overlay activates on the same boundary. The boundary never changes. Only
the flow or participation evidence changes.

**Narration**

Boundary and flow are separate. A boundary says where a structure is
addressable. Flow says how participation moves, presses, routes, or opposes.

The same contour can be read as no-flow placement, inward pressure, outward
pressure, interior fill, exterior pressure, clockwise traversal, or
counterclockwise traversal. Those are different read surfaces over stable
boundary evidence.

**Gojo Structure**

Show boundary evidence separately from flow evidence:

```text
boundary: point, endpoint, segment, rim, outline, cut, site
flow:     face, lane, route, pressure, fill, anti-fill, traversal
```

## Screen 12 - Orthogonal Means Unaccounted Here

**On Screen**

Three stacked cases:

- point with radial escape
- line with side escape
- closed boundary with interior/exterior pressure

**Animation**

In each case, tension first tries to move along the existing structure. When
the existing structure cannot account for it, a perpendicular or higher-grade
outlet appears.

**Narration**

Orthogonal does not only mean ninety degrees. In Gojo's physical intuition,
orthogonal means "not accountable in this structure's native order".

A point cannot absorb directional spread, so radial possibility appears. A
line cannot absorb side pressure as along-line motion, so side structure
appears. A perimeter cannot by itself account for fill, so interior or exterior
participation appears.

**Gojo Structure**

Use the general outlet:

```text
Relation.OrthogonalOutlet(source)
```

Later reads may identify the outlet as circular, linear, sheet-like, radial,
or fill-like depending on the source and environment.

## Screen 13 - Flow And Dimensions

**On Screen**

A dimension ladder:

- two points in 1D make a line
- a line in 2D has a perpendicular line or sheet-like side
- two distant points in 3D have a cylinder-like orthogonal surface
- two touching or coincident points in 3D have a plane-like orthogonal surface

**Animation**

Start with two coincident points in 3D. Their orthogonal possibility is shown
as a plane-like surface of directions. Then the points separate. The plane-like
possibility resolves into an axis plus cylinder-like side surface around that
axis.

**Narration**

A useful rule is that unsettled flow often seeks the next grade, while settled
structure can also carry flow along its own grade.

In one dimension, two points make a line. In two dimensions, a line can emit a
perpendicular line or sheet-like side structure. In three dimensions, two
distant points define an axis; orthogonal possibility around that axis is
cylinder-like. But if the two points are touching or coincident, there is no
stable distant axis yet, so the orthogonal possibility is plane-like.

This is a rule of physical reading, not a symbolic case table.

**Gojo Structure**

Show how the source shape changes the outlet:

```text
point source                -> radial / spherical possibility
zero-length segment source  -> plane-like or ray/line seed possibility
nonzero segment source      -> side surface / cylinder-like possibility
```

The same outlet idea can settle differently when the source gains or loses
extent.

## Screen 14 - Local Positive And Negative

**On Screen**

Three structures:

- segment with normal forward
- radial source with normal outward
- contour with normal clockwise

Each has a secondary opposite arrow.

**Animation**

The user toggles which direction is normal. Numeric signs on small read labels
change, but the underlying structure remains.

**Narration**

Now positive and negative can enter naturally. Positive means the current read
agrees with the selected normal direction. Negative means it opposes that
direction.

That normal is local. A segment can choose forward. A radial source can choose
outward or inward. A contour can choose clockwise or counterclockwise. The
choice belongs to the structure and read surface, not to a universal sign
floating outside the model.

**Gojo Structure**

Keep these distinct:

```text
negative location       L(C,-n)
opposed flow face       C*
reversed segment read   S(C,b,a)
```

A later projection may collapse these into a signed scalar, but Gojo should
preserve the structural difference while it matters.

## Screen 15 - Partition Makes Counting Possible

**On Screen**

A segment, a ring, and a filled square. Each one gains internal partitions.

**Animation**

The partition count changes from coarse to fine. The source structure stays
the same while the read resolution changes.

**Narration**

To count, a structure needs exposed distinctions. Partition is how a span,
cycle, region, or whole becomes countable at a selected resolution.

Partition is not the same as unit shape. It says how a structure is divided
for this read. The unit shape says what kind of thing one tick means.

**Gojo Structure**

Show examples:

```text
linear basis: S(C,0,10)
cycle basis:  P(C,12) -> P(C,0)
whole basis:  bounded surface partitioned into cells
```

## Screen 16 - Tick

**On Screen**

One partition mark is highlighted on each structure: one linear tick, one cycle
tick, one area cell.

**Animation**

A read marker steps from tick to tick. The visible value increments, but the
substructure of each tick remains visible.

**Narration**

A tick is one exposed distinction at the current resolution. It is not a bare
integer in the void. It belongs to a carrier or carrier-like surface.

This is why Gojo can distinguish "three linear ticks", "three contour ticks",
and "three area cells" before deciding how or whether to project them into the
same numerical display.

**Gojo Structure**

Ticks live on carriers and carry unit-shape meaning through those carriers:

```text
Carrier.Value
CarrierLocation(C,n)
Carrier.UnitShape
```

## Screen 17 - Unit Shape

**On Screen**

A row of stamps:

- line stamp
- ring stamp
- sheet stamp
- product stamp
- opposed-face stamp

Two separate carriers share the same line stamp but hold different values.

**Animation**

The same unit stamp is copied onto two carriers. Their values change
independently.

**Narration**

Unit shape is what one tick scales as. It is the structural kind of a tick.
Two carriers can share a unit shape without sharing identity or value.

This is a major Gojo split. Unit shape, resolution, accounting behavior, and
orientation are related, but they are not the same thing.

**Gojo Structure**

Show the split:

```text
unit shape   - what one tick means
resolution   - what distinctions this read exposes
accounting   - how tension wraps, clamps, routes, or remains
orientation  - how the frame is ordered for read
```

## Screen 18 - Calibration

**On Screen**

An unmarked span is placed beside a marked ruler. A circle is shown with a
radius marker. A contour is shown with a zero marker on its perimeter.

**Animation**

The calibrator slides. The read value updates. When the source does not divide
cleanly by the calibrator, residue remains visible.

**Narration**

Calibration compares a structure to another structure used as a ruler, marker,
or extent. It makes measurement possible without pretending the structure was
born numbered.

A segment can be compared to a ruler. A circle can be calibrated by a radius.
A contour can become countable when it has a marker and an orientation.

**Gojo Structure**

Calibration should be an explicit relation:

```text
source structure
  read through calibrator
  -> measured result + residue
```

Do not store calibration as an opaque scale property if relation structure can
carry it.

## Screen 19 - Crystallization

**On Screen**

A stable measured span is lifted from the scene and turned into a reusable
stamp. The stamp is then repeated along a second span.

**Animation**

The original shape freezes into a unit glyph. A new carrier appears whose one
tick uses that glyph. The new carrier accumulates independent value.

**Narration**

When a structure is stable enough, Gojo can crystallize it into a reusable
unit. That means the structure becomes the meaning of one tick on a new
carrier.

Crystallization hardens unit shape, not live value. The new carrier can hold
its own value and tension. It is not secretly bound to the original structure
unless another relation says so.

**Gojo Structure**

Represent a crystallized carrier as a carrier with stable `UnitShape`:

```text
new Carrier(crystallizedUnitShape)
```

The provenance can remain visible, but the carrier's mutable value is its own.

## Screen 20 - Number As Read

**On Screen**

A source span is read against a unit ruler. The result appears as:

```text
3 ticks + residue
```

A second layer shows a finer internal partition turning the residue into
sub-ticks.

**Animation**

Change the read resolution. The value moves from `3 + residue` to `3 4|10`
style structured read. The source does not become more exact by magic; a finer
surface was introduced.

**Narration**

A number is a read through partition and unit structure. Internal partition can
create fractional or sub-tick reads. External comparison can create measured
counts.

The residue is not a hidden decimal waiting to be printed. It is structure the
current read did not account for.

**Gojo Structure**

Show layered value structure:

```text
whole carrier layer
sub-tick carrier layer
carry or boundary relation
residue
```

This prepares the viewer for resolution ladders.

## Screen 21 - Resolution Ladders

**On Screen**

Stacked rails labeled ones, tens, hundreds. A lower rail fills to ten, then
routes overflow into the next rail.

**Animation**

Ticks enter the ones rail. At ten, the lower rail wraps and a pulse moves into
the tens rail. The remainder stays visible on the lower rail.

**Narration**

A value does not have to be one carrier with one large tick count. It can be a
rooted ladder of carriers connected by scale and carry relations.

This keeps every resolution inspectable. The ones level, tens level, and
hundreds level each have their own value and tension. A read can stop at any
level without pretending to know lower detail.

**Gojo Structure**

Use ordinary relations:

```text
Segment(ones,0,10) -> Point(tens,0)
Segment(tens,0,10) -> Point(hundreds,0)
```

The ladder helper may create and inspect this structure, but the meaning lives
in the carriers and relations.

## Screen 22 - Dimensions From Structure

**On Screen**

Three transformations:

- segment swept sideways into a sheet
- ring filled into a disk
- surface shell enclosing a volume

**Animation**

Each transformation starts as boundary evidence. Flow or orthogonal
participation fills the next grade. Any mismatch or open part remains glowing
as residue.

**Narration**

Higher dimensions should not be introduced as coordinate tuples first. In Gojo,
they grow from structure: orthogonal outlets, products, fills, shells, and
boundary-flow settlement.

A line can become a sheet-like source. A contour can become an area-like
source. A surface can participate in volume-like structure. Coordinates can
come later as one read over that structure.

**Gojo Structure**

Show unit-shape products and orthogonal shapes:

```text
length * length       -> area-like unit shape
point orthogonal      -> circular or spherical possibility
segment orthogonal    -> sheet, band, or cylinder-like possibility
```

## Screen 23 - Settlement

**On Screen**

A connected network of carriers and relation channels. Tension enters one
carrier. Some routes accept it, one clamps it, and one leaves unresolved
pressure.

**Animation**

The pulse travels through the attached relation world. Each route shows what
it accepted and what it left behind.

**Narration**

Settlement is the local accounting process. Tension tries to move through
available structure. It may absorb, route, wrap, clamp, branch, merge, or
remain unresolved.

This is why Gojo prefers relation structure over symbolic operation tags. The
question is not "which enum operation is this?" The question is "what local
structure can account for the incoming participation?"

**Gojo Structure**

Examples:

```text
P(C,k) -> P(C,0)          reset or modulo boundary
P(C,k) -> P(C,k)          clamp-like boundary
P(lower,k) -> P(upper,0)  compact carry route
```

## Screen 24 - Lanes And Flow Faces

**On Screen**

One boundary surface with lane overlays:

- positive-forward
- negative-forward
- positive-inverse
- negative-inverse
- no-flow
- both-flow

**Animation**

An occupied value enters one lane. It can cross into another lane only when a
transition relation appears. Opposed lanes meeting without transition remain
as visible tension.

**Narration**

Once boundary and flow are separate, values can occupy lanes. A lane says which
boundary side and flow face are participating.

This lets Gojo distinguish positive position, negative position, forward flow,
inverse flow, no-flow placement, and explicitly opposed flow without reducing
everything to one signed scalar too early.

**Gojo Structure**

Show the local evidence:

```text
flow face
boundary side
lane basis
occupied lane
transition
residue or tension
```

The compact lane names are reads over structure, not symbolic primitives.

## Screen 25 - Support And Occupancy

**On Screen**

Two spans overlap on a partitioned rail. Cells are colored as occupied by A,
occupied by B, shared, exclusive, blocked, or uncovered.

**Animation**

The spans slide. The partition cells update. When boundaries align exactly,
show shared coordinate separately from shared occupied support.

**Narration**

Boolean-like ideas come late. A point is not true. A line segment is not true.
An area is not true. They become occupied, shared, blocked, or uncovered only
relative to a selected scope and boundary-flow basis.

That means union, intersection, difference, complement, and containment are
low-resolution reads over occupied participation. They are not primitive
booleans.

**Gojo Structure**

The read should expose:

```text
selected participants
scope
boundary evidence
flow basis
partitioned cells
occupied lanes
support / tension / uncovered result
residue
```

## Screen 26 - Branch And Merge

**On Screen**

A source channel forks into three candidate routes. Each route has a different
support ladder. Two routes later merge into a result surface.

**Animation**

At coarse resolution, one route clearly wins. Then a closer case appears where
two routes remain unresolved until a finer resolution rung is read.

**Narration**

Branching is routed participation, not a symbolic `if`. A source can fan out
into several possible structures. A later surface can merge, compare, or settle
those possibilities.

Because every route can carry tension and residue, a branch can remain
unresolved without breaking the model.

**Gojo Structure**

Represent branch and merge with relation-shaped surfaces:

```text
input
  -> branch group
       -> branch A
       -> branch B
       -> branch C

branch outputs
  -> merge / comparison / operation surface
```

## Screen 27 - Ladders As Refinement

**On Screen**

Four rungs:

- rectangle
- rounded rectangle
- thick frame
- ornamented frame

Correspondence lines connect a corner, carried edges, replaced transition, and
residue across rungs.

**Animation**

One corner rounds while the rest of the rectangle is carried forward. Tangent
mismatch remains as visible residue. Then another rung thickens the result.

**Narration**

A ladder is not just a render stack and not just history. A rung is a complete
structure, but it can also correspond to coarser and finer structures.

The lower rung does not become wrong when a finer rung appears. A rounded
rectangle can still be read as a rectangle. An ornate frame can still be read
as a thick frame. Each read should preserve what it explains and what it leaves
as residue.

**Gojo Structure**

A refinement surface should carry:

```text
source
focus
carry-forward
replacement or settlement
detail
correspondence
residue
result
```

All of those should be traversable Gojo structure.

## Screen 28 - Equations As Reads

**On Screen**

An input segment maps to an output segment. A live input marker moves. A
prediction marker appears on the output. An observed marker sits nearby, with a
visible residual gap.

**Animation**

The input marker moves from 0 to 10. The output prediction moves from 0 to
100. The observed marker deviates, and the residual is drawn as a signed gap.

**Narration**

In Gojo, an equation is not primarily a symbolic formula object. It is a
relation-shaped read surface. It admits input participation, predicts output
participation, and exposes residue or error.

The error is not hidden in a loss variable. It is structure: predicted output,
observed output, residual, violation evidence, and provenance.

**Gojo Structure**

Start with affine surface:

```text
Segment(input,0,10) -> Segment(output,0,100)
```

Then show comparison:

```text
predicted
observed
residual
violation
residue
```

## Screen 29 - Concepts As Stable Reads

**On Screen**

An object-like structure with overlays:

- boundary
- support
- flow
- blocked passage
- affordance arrows
- material or capacity hint
- unresolved residue
- lower-resolution concept rung

**Animation**

The view fades from raw structure to low-resolution concept read, then to a
higher-resolution read. The underlying structural evidence remains faintly
visible.

**Narration**

A concept is not a label pasted onto data. A concept is a stable read over
structure: boundary, flow, support, function, material, capacity, residue, and
history.

This is why Gojo tries to avoid meaning-owning wrapper classes. If the helper
code disappears, the structure should still carry the concept well enough for
another read to rediscover it.

**Gojo Structure**

Concept-surface evidence may include:

```text
selected source
boundary-flow basis
support lanes
focus surface
capacity or settlement witness
timeline
residue or tension
provenance
```

## Screen 30 - Closing: Why The Order Matters

**On Screen**

The full teaching spine appears as a ladder. Each rung briefly highlights, and
the visual from that screen flashes beside it.

**Animation**

The ladder compresses into one connected graph of carriers, relations,
locations, unit shapes, residue, and tension. The final frame shows the graph
still inspectable, not collapsed to a single equation.

**Narration**

The order matters because Gojo is trying to make structure inevitable.

Resolution comes before precision. Tension comes before exceptions. Duality
comes before negative numbers. Sites come before coordinates. Flow and
boundary come before booleans. Unit shape comes before ordinary units. Reads
come before symbolic equations. Concepts come after structure, not before it.

That is the center of Gojo Blocks: show the structure first, then let the read
explain itself.

**Gojo Structure**

End on the common read shape:

```text
selected source
read through surface or basis
produced result
left residue or tension
with provenance
```

This shape should be visible across geometry, numbers, equations, and concept
surfaces.

## Implementation Notes For Blocks

The first executable version does not need to implement every screen. A useful
initial subset would be:

1. resolution and tension
2. point/site and emitted flow
3. second point as line versus radial extent
4. boundary versus flow overlays
5. dimension/orthogonal screen with coincident-to-distant points
6. partition, tick, unit shape, and calibration
7. resolution ladder overflow
8. refinement ladder with visible residue

For rendering:

- Use carriers as rails, channels, contours, or reservoirs.
- Use carrier locations as pins, tick marks, or occupied sites.
- Use relations as ordered links, frames, routes, or structural bindings.
- Use flow faces as directional overlays.
- Use boundary evidence as endpoints, rims, outlines, cuts, and cells.
- Use unit shapes as repeated stamps or tick molds.
- Use tension as pressure, glow, spring compression, or unabsorbed pulse.
- Use residue as leftover chips, gap marks, or carried remainder.
- Use ladder rungs as complete structures with correspondence lines.

Do not hide incomplete states. A partial, incompatible, or low-resolution read
is still a Gojo read.
