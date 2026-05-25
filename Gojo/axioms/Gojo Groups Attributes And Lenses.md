# Gojo Groups, Attributes, And Lenses

This note captures the current working model for additive participation,
attribute attachment, fill bases, and observer/read lenses. It is a
conceptual bridge between current `UnitShape`/`Relation` behavior and future
group, fill, and graphics-style structures.

Geometric examples such as area, contour, and interior are useful, but they are
not the whole model. The same rules apply to non-geometric combinations such as
people times hours, rate times time, or any two non-aligned positive directions
that create one product participation. In this note, "area" often means the
product participation created by combining non-aligned positives; geometric
area is only one familiar case.

## 1. Additive Participation

The type of a carrier is its `UnitShape`. Additive properties are relations to
that typed carrier or to a holder around it; they are not part of the unit shape
itself unless a later projection explicitly casts them into a shared type.

An additive relation has two natural outcomes depending on unit compatibility.

```text
same compatible unit shapes      calculated sum
different unit shapes            additive participation / group
different units cast to one unit  calculated sum at the shared unit
```

Examples:

```text
3 apples + 1 apple              -> 4 apples
3 apples + 2 oranges            -> apple/orange group
3 apples-as-fruit + 2 oranges   -> 5 fruit
```

This means a group does not need a separate symbolic mode. A group can be an
additive holder whose participants do not collapse to one unit shape. If later
structure projects them into a shared unit, the same additive relation can be
read as a sum at that shared type.

## 2. Aligned And Orthogonal Combination

Aligned properties share a type. A non-additive relation between aligned
properties quantifies, scales, routes, or calibrates that type without creating
a new one.

```text
aligned length ratio       length read in another basis
decimal ladder carry       same numeric type across related carriers
same-axis overflow         internal tension/routing on that carrier type
```

Orthogonal properties are not already the same participation. A non-additive
relation between orthogonal properties creates a new type.

```text
width * height             area-like participation
boundary * interior side   fill/region participation
time * rate                quantity-through-time participation
```

This distinction matters for tension. Overflow or residue inside an aligned
domain is internal tension for that carrier and can route along aligned
structure. Unaccounted change that has no correlation to the expected aligned
change is noise or orthogonal tension from that carrier's point of view.
Additive attributes have no automatic tension path because they are defined as
uncorrelated participation. If later behavior reveals correlation, a new
relation can harden between the attribute and the typed structure.

Example:

```text
banana + yellow       additive attachment at first
banana <-> yellow     correlated relation after repeated evidence
green banana          tension/information only after that correlation exists
```

Current Gojo exposes this distinction through `UnitShape.AlignedProduct` and
`UnitShape.OrthogonalProduct`. Relation point outputs read as additive/aligned
participation; relation segment outputs read as orthogonal product
participation. The distinction is still carried by structure and chosen read,
not by an opaque operation tag.

An orthogonal product can be crystallized as a basis/handle without measuring
the operands. For example, width and height can be combined in an operation
surface and crystallized into an area-like carrier whose unit shape is the
product kind, while the current width and height values remain untouched and
unmultiplied. A measured read is a separate step that uses an explicit output
frame/basis to decide the result ticks.

That means result resolution belongs to the chosen output/read basis, not to
the `UnitShape` alone. The unit shape says what kind of participation one tick
means. A relation or lens that measures the product says how source basis
ticks project into result basis ticks. Creating the product carrier alone is
therefore structural crystallization, not a hidden calculation at a guessed
precision.

## 3. Group Holders

A group holder is a carrier or relation site that gathers participants around a
shared zero or junction. It may have no useful scalar value. Its job is to make
participation inspectable.

```text
group holder at 0
  + boundary segment A
  + boundary segment B
  + boundary segment C
  + boundary segment D
```

The holder can be durable structure, such as a named boundary group, or a
temporary selection created for inspection and discarded after use. Either way,
the grouping should be real Gojo structure, not an external list that holds the
only meaning.

Order, side, winding, adjacency, and selection membership are extra structure.
An unordered additive group and an ordered contour are not the same read, even
when they contain the same segments. Those distinctions should be carried by
relations among the participants or by the lens used to inspect them.

Additive participation should preserve construction order even when a later
lens ignores it. A polyline, polygon, contour, shopping basket, and temporary
selection may all use additive gathering, but only some reads care about order.
Preserving order in structure lets both ordered and unordered lenses remain
available.

An empty ordered/additive group is still a real group surface, but it must not
look like a holder-origin attachment to itself. Current Gojo creates the empty
chain with a non-origin holder point, so local reads return zero participants
instead of a phantom participant containing the holder carrier.

For contour closure, equal coordinate values are not enough. The current Gojo
foothold treats an ordered contour as closed when its first and last endpoint
positions expose matching carrier locations that share the same `TickRef`s.
That makes closure a physical shared-coordinate relation rather than a
coincidental numeric equality.

Group selection is almost the opposite of type inspection:

```text
group/selection read   follows additive attachments and participants
type read              follows unit shape, aligned structure, and provenance
```

A useful system needs both. Selection asks "what participates here?" Type
inspection asks "what kind of thing is this, and how is that kind derived?"

## 4. Boundary, Fill, And Basis Handles

A boundary group does not automatically contain fill. It exposes potential
uses. A fill exists when there is a region or interior participation carrier
attached to the boundary/group.

Area does not have to be calculated in order for a fill basis to exist. A
fill basis may be a single-marker carrier for the product/interior kind:

```text
0 ticks   no basis participation currently held
1 tick    a basis marker of this fill kind
inverse   inverse/anti-fill basis marker
```

This lets a region carry color, texture, hit participation, clipping, or other
attributes without first computing its exact numeric measure. The fill basis is
a handle for the product/interior kind; its numeric area value is a separate
resolution/calibration read of that participation.

More precisely, a fill basis and an area measure are different uses of the same
interior participation. A fill basis says "this kind of participation is
present and attachable." A square-unit area read measures a source structure
against that basis or another chosen basis. Neither is infinite precision; both
are available only through relation structure.

Fill basis creation may be chosen deliberately when no area expression/mechanic
has been attached yet. The absence of a high-resolution area lens should not
prevent the fill kind from existing as an attachable carrier.

The fill basis is not a hidden area calculation. If width and height
each have their own basis, a measured area read should use explicit relation
structure to decide its result basis and resolution. Creating the basis carrier
alone does not calculate the rectangle and then discard precision; it gives us
the product kind that later reads can measure against.

The same idea applies to low-resolution type selection. "Apple" and "orange"
can become "fruit" when a projection lowers resolution to shared structure such
as edible, sweet, grows-from-plant, or another selected common participation.
The shared type is not found by ignoring structure; it is produced by selecting
which additive/aligned properties matter at that resolution.

Current `CarrierLocation` values are grounded on a carrier. An ordered pair
such as `(xAxis:3, yAxis:2)` may be detached from a larger basis holder, but it
is still calibrated to its component carriers. True ungrounded or uncalibrated
datasets are a separate future concept and should not be modeled with
`Carrier.Unknown`, because `Carrier.Unknown` is already a wildcard in frame and
port reads.

## 5. Attribute Carriers

Attributes are carriers attached by relation structure, not metadata fields.

```text
region/fill holder
  + color carrier
  + stroke/line-weight carrier
  + texture carrier
  + opacity carrier
```

These attachments are additive participation. Color does not multiply area to
become a new spatial fill. A region and a color participate together, and a
renderer or observer can later read that participation.

Attributes can themselves be built through carrier ladders:

```text
color carrier
  <- red component
  <- green component
  <- blue component

same color carrier
  <- hue
  <- saturation
  <- lightness

same color carrier
  <- palette index
```

RGB, HSL, palette index, and sampled texture are not hidden properties. They
are alternate relation paths into color participation. The color carrier is the
thing that can attach to a fill; the component ladders explain or produce it.

Attributes may begin as uncorrelated additive participation. When repeated
structure shows correlation, an attribute can become an expected relation
rather than a casual attachment. That change should add visible relation
structure; it should not silently mutate the unit shape.

## 6. Lenses And Observer Reads

A lens is relation structure that reads a group, boundary, fill, or attribute
into some result carrier. A single boundary or region can expose many lenses:

```text
boundary group -> perimeter length lens
boundary group -> bounding box lens
fill region    -> area lens
fill region    -> hit/membership lens
fill region    -> centroid lens
fill region    -> render/tessellation lens
fill + mapping -> color sample lens
```

Each lens has a result unit shape and a mechanical read. The read mechanism may
eventually be exact, approximate, sampled, or unresolved. It should still be
encoded as Gojo-readable structure rather than as an enum value that only an
external interpreter understands.

Examples of future mechanics:

```text
bounding box      project boundary points onto axes, read min/max
perimeter length  traverse ordered segments and add compatible lengths
area              read fill through square-unit resolution or count samples
centroid          read balance of filled participation
hit area          read interior marker at chosen point/resolution
```

The lens result can be live, reading from the current source structure, or
crystallized as an independent carrier copy. Crystallization copies a read; it
does not make the source boundary or fill stop existing.

## 7. Metaphorical Attachment

Gojo should allow useful metaphorical units when the relation is explicit.

For example, line weight is not physical mass, but "heavy line" and "light
line" behave enough like weight to be useful. This should be represented by a
stroke-weight carrier attached to a line or contour, possibly with projection
relations to other weight-like structures.

The rule is not "forbid category mistakes." The rule is "make the projection or
attachment visible." A metaphor becomes meaningful when there is relation
structure that carries how the metaphor participates.

## 8. Compatibility With Current Code

Current Gojo code is already close in several places:

```text
UnitShape.Additive(...)   collapses compatible units; preserves unlike units
TryReadAdditiveOperands   inspects unlike additive participation in order
AdditiveAttachment        attaches one participant to a holder origin
AdditiveGroup             anchors ordered additive participants on a holder
OrderedGroup              names the same ordered/additive holder structure
Carrier.AttachAdditive    attaches an additive ladder to a carrier holder
TryReadAdditiveParticipantsOn
                           inspects actual holder participants in order
TryReadOrderedElementsOn  names ordered participant inspection
TryReadOrderedClosureOn   detects ordered endpoint closure through shared refs
TryCreateContourFillBasisOn
                           creates fill basis only for closed contour groups
TryReadAdditiveUnitShapeOn
                           reads a holder group's additive unit participation
CreateFillBasis            creates fill-kind basis participation
CreateInverseFillBasis     creates inverse/anti-fill basis participation
UnitShape.AlignedProduct   keeps compatible aligned operands at shared type
UnitShape.OrthogonalProduct
                           creates product type from non-aligned operands
TryReadOrthogonalProductOperands
                           inspects explicit orthogonal product operands
UnitShape.Product(...)    represents multiplicative/product participation
TryCrystallize(input, out carrier, out basis, out read)
                           returns operation result plus rebound basis
Relation point output     aligned/additive operation read
Relation segment output   orthogonal product operation read
TryReadFillMeasureOn      deferred interval-fill measure
TryCrystallizeFill        independent interval-fill carrier copy
OrthogonalOutlet          generic source-to-orthogonal unit-shape foothold
AttachLiveOutput          live read into a concrete result carrier
```

The missing pieces are mostly semantic and inspection footholds:

```text
named lens structures for area, bbox, perimeter, hit, centroid, render
stable live result carriers with provenance
unit projection/cast structure for apples/oranges -> fruit-like reads
ordered additive group reads separate from unordered participation reads
orthogonal/noise tension distinct from aligned overflow tension
```

The current code should therefore be treated as compatible but incomplete. It
already has the local mechanics for unit compatibility, additive/product shape,
orthogonal surfaces, and crystallized reads. It does not yet make groups,
attributes, and lenses explicit enough to use them as ordinary inspectable
Gojo objects.
