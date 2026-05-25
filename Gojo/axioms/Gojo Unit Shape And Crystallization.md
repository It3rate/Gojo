# Gojo Unit Shape And Crystallization

This note records the current direction for unit shape in Gojo, especially as
it relates to crystallization.

The central split is:

```text
unit shape       what one tick scales as
resolution       what distinctions a carrier or port exposes
accounting       how a surface wraps, carries, clamps, or leaves tension
orientation      how a frame is ordered for read/settlement
```

Wrapping behavior, fractional residue, and reverse frame direction are not
unit shape. They are ways a unit is used through a particular carrier/relation
surface.

## 1. Carrier Unit Shape

Every carrier has an inherent unit shape for its native tick.

The default carrier unit shape is a linear one-tick atom:

```text
P(U,1)
```

where `U` is the carrier's unit atom. This atom does not need a human label
such as inch, meter, or lumen. Labels can be attached later, but the kernel
meaning should remain numeric and relation-shaped.

A carrier may also be created with a known unit shape:

```text
lengthCarrier.UnitShape = P(LengthUnit,1)
areaCarrier.UnitShape   = P(LengthUnit,1) -> P(LengthUnit,1)
```

Two carriers can share the same unit shape without being the same carrier.
This is useful for separate value holders, alternate resolutions, and copied or
crystallized surfaces.

Once a carrier is created, its unit shape is fixed. For a crystallized carrier,
this is part of the promise: one crystal tick must keep meaning the same unit
shape. If a system wants a related unit basis with a different calibration, it
should create a new carrier or explicit adapter, not mutate the crystallized
carrier's basis in place.

Sharing a unit shape does not share value state. Two square-inch carriers can
have compatible tick meaning while holding different `Value` and `Tension`.
The shared unit shape is a stable standard; each carrier remains its own
physical site.

## 2. Relation Unit Shape Is Calculated

Do not store unit shape on ordinary relations by default.

A relation's exposed unit shape should be calculated from:

- the unit shapes of carrier sites it mentions
- the relation surface shape
- the read or operation context

For example:

```text
S(Inch,0,10)
```

does not create `inch^10`. It is an inch-shaped surface with a ten-tick basis
or resolution. Its unit shape remains the carrier's native inch shape.

A port uses the same unit-shape rules as any other relation surface. Being a
port means the surface is exposed, not that it has a separate unit system.

## 3. Unit Shape Is Relation-Shaped

Unit shape should use the same relation structure as other Gojo structure, but
abstracted to unit tick `1`.

Examples:

```text
linear unit          P(u,1)
square unit          P(u,1) -> P(u,1)
man-hour             P(man,1) -> P(hour,1)
meters per second^2  P(m,1) -> (P(s,1)^-1 -> P(s,1)^-1)
```

The notation `^-1` is explanatory. The implementation should eventually use
unit-surface dual/inverse participation, not a string exponent.

Repeated participation expresses powers:

```text
u^2 = u -> u
u^3 = u -> u -> u
```

The repeated relation is visible structure. A compact calculated scale
signature such as `u:+2` may be useful as a read, but it should not replace the
relation-shaped unit surface as the source of truth.

## 3a. Algebra Surface And Optional Provenance

Unit shape has two separable layers:

```text
algebra surface        bare calculational structure
descriptor surface     optional provenance for algebra slots
```

The algebra surface must remain coherent without descriptors. This allows
Gojo to try a structure metaphorically, transfer a calculation pattern, or
discover that a transfer does not work by watching residue and tension.
Descriptors restrict direct collapse, but they should not prevent exploratory
algebraic reads.

Descriptor slots are placeholders in the algebra surface. A shape such as:

```text
A^2 + B^2
```

has two descriptor entries, `A` and `B`, while the algebra surface references
those entries repeatedly. It does not need four descriptor slots merely because
two dimensions are squared.

This gives two useful compatibility reads:

```text
direct compatibility      same algebra and same descriptors
algebraic compatibility   same algebra, descriptors ignored
```

For example, geometric area and man-hours can share product-like algebra while
remaining different direct unit meanings. A metaphor or adapter can still try
to map one structure onto the other, but ordinary arithmetic should not
silently collapse the provenance.

Current code footholds:

```text
UnitShape.DescribedAtom(descriptor)  creates one algebra slot with provenance
UnitShape.Bare()                     drops descriptors, keeps algebra slots
UnitShape.WithDescriptors(...)       reattaches descriptor provenance
UnitShape.AlgebraicallyCompatibleWith(other)
```

The descriptor can be any Gojo element, including a carrier used as a concept
or creation root. It is optional; descriptorless shapes remain valid algebraic
structures.

## 4. Addition And Compatibility

Addition can collapse only when unit shapes are compatible:

```text
3u + 4u -> 7u
```

Incompatible addition is still valid. It remains relation-shaped rather than
collapsing:

```text
3u + 4v -> L(u,3) -> L(v,4)
```

This is like complex addition: real and imaginary parts can sit side by side
without reducing to one scalar tick.

Compatibility should not be confused with identical carrier identity. Carriers
may differ while sharing compatible unit shape. Conversely, two relation
surfaces on the same carrier may expose different higher structures depending
on operation context.

## 5. Transfer, Fusion, And Operation Results

Transfer/calibration does not multiply units.

For example:

```text
Light -> Heat
```

does not produce `light*heat`. The target unit shape wins for the received
value. Source residue remains source-unit tension.

Fusion/operation can produce composite unit shape:

```text
length * length       -> length^2
man * hour            -> man-hour
distance / time^2     -> acceleration shape
```

The operation surface decides which unit shape is exposed. A port group:

```text
S(X,0,3) -> S(Y,0,4)
```

is only paired extents until an operation surface reads it. A multiply-like
operation may expose area shape. An add-like operation may expose compatible
length shape or an unreduced relation-shaped sum.

## 6. Orthogonal Shape Units

Multiplicative unit shape can also be read as orthogonal shape formation. In
that read, `*` means pushing a structure into an orthogonal supplied by the
source structure:

```text
point * positive orthogonal        -> circle-like level-line shape
zero-segment * positive orthogonal -> ray/line seed
segment * positive orthogonal      -> sheet/rectangle-like shape
```

These are shape units, not necessarily measured values. An uncalibrated point
orthogonal may be known as circular without knowing its radius. An
uncalibrated segment orthogonal may be known as sheet-like without knowing its
height. Calibration can be attached later:

```text
shape | calibrator
```

where the right side supplies measurable extent, marker, or basis for the
left. For example, a segment can calibrate its own orthogonal to create a
square-like unit, while a radius/ray-like structure can calibrate a point's
circle-like contour. The `|` notation is provisional and should be read as
"left through right" rather than as a new stored operator.

This gives two related but distinct products:

```text
shape-unit product       preserves abstract orthogonal provenance
calibrated ratio/product preserves the supplied proportions
```

A generic rectangle-like shape unit may normalize its abstract basis, while a
calibrated `4:1` rectangle or ratio must preserve that relation. Crystallizing
a shape unit should therefore harden the unit-shape provenance. Crystallizing
a calibrated geometry should harden the calibrated relation as well.

Contour direction is not part of this section yet. Marker and partition can
make a contour countable without deciding which traversal direction is positive
in an external frame. Direction calibration needs a later structural rule,
possibly from an oriented relation, another contour, or a handedness-like
coupling.

## 7. Resolution, Basis Direction, And Unit Inverse

Resolution is not unit shape.

```text
S(C,0,10) -> P(U,1)
```

may expose whole/residue reads such as `2 7|10`, but the `|10` is a resolution
or basis feature, not a new unit shape.

Reverse orientation is also not automatically unit inverse:

```text
S(C,10,0)
```

has the same carrier unit shape as `S(C,0,10)`, but it reads through an
opposite ordered frame.

Unit inverse is a scale-law denominator, such as `s^-1` or `s^-2`. It should
be represented by inverse participation in the unit surface. It should not be
silently equated with every reverse frame or every opposed-flow carrier face.

An opposed carrier face is nevertheless part of the unit-shape surface. The
opposed face remains compatible with the positive face, but it is no longer
the literally identical unit shape:

```text
positive face      U
opposed face       U -> P(Unknown,-1)
```

Compatibility can read through the opposed-face wrapper when asking "is this
the same kind of unit?", while inspection can still see that a carrier tick is
flowing through the opposed face.

This matters for multiplication. Multiplying by an opposed-flow carrier is not
a lookup-table operation. It is the physical action of inverting the carrier
face of what it acts on. Repeated inverse-flow participation gives the familiar
cycle:

```text
i * 1   ->  i
i * i   -> -1
i * -1  -> -i
i * -i  ->  1
```

In product reads, an odd number of opposed-flow operands writes to the opposed
output carrier face. Each pair of opposed-flow operands negates the output
tick value on the resulting face. Addition is different: there is no flow
interaction, so positive and opposed components should remain separate unless
an explicit structure says how to merge them.

## 8. Crystallization Split

Crystallization creates a reusable unit shape and may optionally project a
current value onto a carrier using that shape.

These are separate actions:

```text
derive unit shape
project current value
```

For example:

```text
width inch port -> height inch port -> multiply-like operation
```

can derive:

```text
square inch unit shape = inch -> inch
```

Then a carrier can be created with that unit shape:

```text
squareInchCarrier.UnitShape = inch -> inch
```

That carrier can later measure circles, triangles, or any other area-like
structure compatible with square inches. It is not permanently tied to the
rectangle or operation that first derived it.

The source operation does not retain control over the crystal basis. It may
continue to project values into the crystal if an explicit live projection is
kept, but changing the lower source structure does not silently change what one
crystal tick means.

Projecting a current operation value onto that carrier is optional and should
not be conflated with creating the unit shape. Projection feeds the crystal
carrier by delta, like other port-style value input; it does not directly
overwrite carrier state.

## 9. Current Code Foothold

In current Gojo code:

- `Carrier.UnitShape` is the native shape of one tick on that carrier.
- a default carrier gets a fresh linear atom, so two unrelated carriers are
  not unit-compatible by coincidence.
- carriers can be created with an existing `UnitShape`, giving them compatible
  ticks without sharing carrier identity.
- carriers that share a `UnitShape` keep independent `Value` and `Tension`.
- unit-shape surfaces are readable as snapshots; external perturbation of a
  read surface does not mutate the carrier's basis.
- an inverse carrier has an inspectable opposed-face unit shape that remains
  compatible with the forward unit shape.
- unit shapes can carry optional descriptor provenance separate from their
  algebra surface:
  - `UnitShape.DescribedAtom(descriptor)` creates one algebra placeholder with
    provenance
  - `UnitShape.Bare()` returns the same algebra without descriptor provenance
  - `UnitShape.WithDescriptors(...)` reattaches descriptors to an algebra
    surface
  - `CompatibleWith` requires both algebra and descriptors to match
  - `AlgebraicallyCompatibleWith` compares the bare algebra only
- `UnitShape.OrthogonalOf(element)` creates an orthogonal shape unit whose
  surface preserves the source element as provenance and uses a zero-segment
  marker as the orthogonal outlet.
- `UnitShape.CircularFrom(point)`, `UnitShape.LinearFrom(zeroSegment)`, and
  `UnitShape.SheetFrom(segment)` are official names for the currently supported
  orthogonal pushes. They all use the same outlet surface; the source structure
  distinguishes perimeter-like, ray-like, and sheet-like unit shapes.
- `Relation.Point(...)`, `Relation.Segment(...)`, and
  `Relation.OrthogonalOutlet(...)` are construction helpers for those same
  Gojo relation surfaces, not wrapper objects or symbolic operation tags.
- `UnitShape.TryReadOrthogonalSource(out source)` and
  `UnitShape.TryReadOrthogonalSource(surface, out source)` expose that
  provenance as a snapshot so callers can inspect point, segment, or
  zero-segment origin structure without mutating the shape.
- `Relation.TryReadUnitShape(...)` calculates unit shape from existing
  relation surfaces:
  - `source -> S(Unknown,0,0)` exposes an orthogonal shape unit, and this read
    wins before the same surface can be treated as a zero-extent port
  - same-carrier point/segment frames expose that carrier's unit shape
  - transfer/calibration ports expose the concrete target unit shape
  - `Unknown` output ports inherit the source unit shape for discovery reads
  - point-output operation surfaces fold operand shapes additively
  - segment-output operation surfaces fold operand shapes multiplicatively
- `Relation.TryCrystallize(out carrier)` creates a new carrier with the
  derived unit shape and does not project any current operation value onto it.
- `Relation.TryCrystallize(input, out carrier, out read)` creates the crystal
  carrier and projects the current operation read onto it.
- `Relation.TryProjectCrystallizedValueFrom(input, crystal, out read)` projects
  the current operation read onto an existing compatible crystal carrier.
- product operation reads preserve opposed-flow participation:
  - one opposed-flow operand flips the output carrier face
  - two opposed-flow operands return to the forward carrier face and negate
    the output value
  - the same cycle repeats for higher inverse counts

The projection read preserves operand residue as visible relation-shaped
residue. It does not treat residue as fresh source tension merely because a
crystal read occurred.

The current value projection is intentionally conservative. Product-like
operation surfaces can project into one crystal scalar. Additive point-output
surfaces can project only when operand unit shapes are compatible. Incompatible
additive surfaces can still derive and crystallize an unreduced unit shape, but
their current value is not collapsed into one scalar until Gojo has a
multi-component crystal value surface.

## 10. Open Questions

The initial implementation can stay simple:

- multi-component crystal value surfaces for unreduced additive shapes
- richer compatibility reads for structured and inverse unit participation
- circular and angular adapters should be treated carefully: freezing an angle
  or radius-like surface may preserve resolution/tension behavior that is lost
  if it is immediately flattened into Cartesian projection

Later work must clarify:

- inverse/dual unit participation in code
- compact scale-signature reads for fast scaling behavior
- deeper structured units such as radians, arc-length/radius, and geometric or
  dynamic units whose net scale signature is not the whole meaning
- expected basis and resolution derivation, described in
  `Gojo Crystallized Resolution.md`
