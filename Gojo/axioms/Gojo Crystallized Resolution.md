# Gojo Crystallized Resolution

This note captures the current working model for how unit shape, basis, and
resolution interact when Gojo structure is crystallized.

The short rule:

```text
UnitShape says what one tick participates as.
Basis/read structure says how finely that participation is being measured.
Crystallization preserves kind; accurate resolution usually requires preserving source structure.
```

## 1. Unit Shape Can Own A Coarse Tick Promise

`UnitShape` is the crystallized kind of a carrier tick. It can say that a tick
is length-like, area-like, circular, additive, inverse, or a product of other
participations. It should not silently decide the precision of a measurement.
It can, however, crystallize a stable coarse tick promise when that is the
concept being represented.

```text
length unit shape                 what one length tick means
ResolutionOf(length, 100)          what one hundred-length tick means
hundreds slot value 3              three coarse ticks
full lower slots absent            about 300 at this scope
ones/tens supplied as zero         exactly 300 at ones resolution
```

This keeps coarse resolution structural without forcing every workspace read
to pretend it has lower detail.

Resolution lives in the basis/read relation:

```text
length unit shape        what a length tick means
S(length,0,12)           a twelve-tick basis/read over that kind
square-length unit shape what a product tick means
area read basis          how a source structure projects into product ticks
```

A crystallized unit can return an expected basis when it was constructed from
sources that already had basis information. That basis is structure returned by
the construction/read; it is not an invisible property hidden inside the unit
shape. A coarse unit shape says what one coarse tick means; supplied-slot
witnesses and read lenses still say whether lower detail was absent,
explicitly zero, or projected from live structure.

## 2. Crystallization And Scientific Resolution

For scientific-style resolution, avoid crystallizing too early. Keep the source
dimensions, bases, and residues available until the final value is chosen.

Example:

```text
width  measured to inches
height measured to feet
area   measured from width * height
```

Choosing an area display basis and calculating area uncertainty are separate
questions. Neither input's tick size alone establishes the uncertainty of the
product. If the input values have justified absolute error bounds `a` and `b`
around `w0` and `h0`, ordinary multiplication gives:

```text
width = w0 + dw,  |dw| <= a
height = h0 + dh, |dh| <= b
|width * height - w0 * h0| <= |h0| * a + |w0| * b + a * b
```

Dropping `a * b` is a first-order sensitivity approximation, not this guaranteed
bound. A statistical uncertainty read needs its own assumptions and dependence
information. Input resolution is not automatically either an error bound or a
standard deviation.

Each input uncertainty is scaled by the other operand's magnitude; shared
dependencies can also constrain the result. The accurate read needs the
original parameterized structure. A flat crystallized area carrier cannot
recover this evidence unless it remains connected through attached relations.

This is similar to circular/perimeter resolution. A degree-like tick may have a
stable angular meaning, but its linear spacing changes with radius. The
parameterized structure knows that. A flattened perimeter read may not.

## 3. Product Basis Is A Read Choice

An orthogonal product can be crystallized as a handle without measuring the
operands:

```text
width * height -> product carrier with width-height UnitShape
```

That carrier is a place where area-like participation can attach. It does not
mean the rectangle was measured and then rounded away. A measured area read is
a separate relation:

```text
width value, height value, output basis -> product ticks + read residue
```

The output basis decides whether the visible value is square feet, square
inches, one interior marker, a thumbnail fill, or another calibrated read.

## 4. Current Default Policy

Until Gojo has full scientific-resolution propagation, the practical default
is an inspectable output-basis heuristic.

For orthogonal products where both operands participate as independent
dimensions, derive an expected output basis from the coarsest participating
resolution. In count terms, this means the minimum ticks-per-unit basis. In
tick-size terms, this means the largest tick size.

```text
fine length basis * coarse width basis -> coarse expected product basis
```

This chooses a representational granularity. It does not guarantee an
uncertainty bound or establish the product's justified precision: operand
magnitudes, actual uncertainty and shared causes still matter. A finer explicit
output basis can preserve more calculational detail without supplying new
measurement information. Keep the source structure available when later reads
need that distinction.

For "per" or ratio-style units, the surviving/target participation should
dominate the output basis:

```text
miles per hour * hours -> miles
```

The time basis affects read residue, but it should not become the mile
carrier's tick kind. Time uncertainty can still affect the distance result.
Dimensional cancellation alone does not cancel uncertain quantities; that
requires the relevant shared quantity and dependency. Neither numerical effect
redefines what one mile tick means, and neither automatically injects tension.

The current code has a first foothold for this: when an operation output frame
is unbound, concrete target frames exposed by operand ports are treated as
dominant basis candidates. This lets a relation surface say "read this operand
through miles" and have the crystallized output basis preserve that mile
calibration. Because that operand has already been projected through the
dominant basis, the output read does not multiply by that basis a second time.
The later [unit quotient reads](../docs/GojoUnitQuotientReads.md) supply explicit
denominator groups, stable reciprocal/quotient tick promises, cancellation
witnesses and equivalent output reads. Automatic operation crystallization
through denominator units remains future work; explicit reads retain the
selected standard and native calibration.

For count products such as person-hours, an integral count of people does not
by itself imply integral person-hours: one person participating for half an
hour contributes half a person-hour. A chosen coarse basis may retain that
fraction as residue. Product granularity needs its own calibration and read
contract, rather than a claim that one discrete factor makes the product
discrete at the same scale.

## 5. Future Specialized Capability

Gojo should eventually support a specialized scientific-resolution mode that:

- preserves operand bases and current operand values through the read
- preserves explicit quantity bounds, dependencies and measurement evidence
- retains read residue instead of hiding it in rounded output; converts it to
  tension only through an explicit material operation that must preserve change
- distinguishes addressable source cells from crystallized product shells
- supports scale-dependent resolution such as radius-dependent perimeter ticks
- lets callers choose between coarse basis handles and precise measured reads

This should be ordinary relation structure, not a side table of uncertainty
metadata. The important principle is that the source, calibration, uncertainty
and retained resolution of a read remain inspectable. The
[uncertainty reference](../docs/GojoUncertaintyReference.md) separates nominal
stops, tolerance, measurement uncertainty, relevance and shared dependencies.

## 6. Current Code Foothold

Current Gojo already has the basic split:

- `Relation.TryCrystallize(out carrier)` derives a unit shape and creates a
  carrier without projecting a measured value.
- `Relation.TryCrystallize(input, out carrier, out basis, out read)` derives a
  carrier, returns an operation basis rebound to that carrier, and projects the
  current value through an explicit read.
- `UnitShape.AlignedProduct(...)` and `UnitShape.OrthogonalProduct(...)`
  separate same-kind participation from new product-kind participation.
- unbound operation output frames derive their returned basis from exposed
  operand port bases.
- concrete operation output frames are explicit and keep winning over derived
  operand-basis defaults.
- concrete operand target frames act as dominant basis candidates for the first
  ratio/per-style foothold.
- `UnitShape.ResolutionOf(source, ticksPerTick)` creates a stable coarse unit
  shape, and `TryReadResolution(...)` exposes its source and span.

The [explicit quotient reads](../docs/GojoUnitQuotientReads.md) now connect
ordered cancellation witnesses, stable reciprocal/quotient units, and native
numeric division. An authored fixture composes division with a coarser read
while retaining numerator residue and quotient-basis residue separately.

The [numeric milestone](../docs/GojoNumericMilestoneReview-2026-09.md) now includes
explicit nested quotient composition, supplied-slot reads, material refinement
and graph restoration. The next uncertainty step needs jointly constrained
quantities and dependency-preserving reads; basis selection alone does not
provide that behavior.
