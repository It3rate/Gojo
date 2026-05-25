# Gojo

Gojo is the active kernel model for this repository. It was extracted from the
older Core4 workspace so the current numeric structure can be read and developed
without historical project noise.

## Quick Start

From the repository root:

```bash
dotnet test Gojo.slnx
```

Or test the project directly:

```bash
dotnet test Gojo.Tests/Gojo.Tests.csproj
```

The core project is:

```text
Gojo/Gojo.csproj
```

The tests are:

```text
Gojo.Tests/Gojo.Tests.csproj
```

## Intent

Gojo represents concepts, structure, and operations in physically meaningful
numeric form. The goal is not to solve equations through external symbolic
dispatch. The goal is to create related carriers whose value and tension settle
through the structure they contain.

The current model favors:

- carriers with stable identity, mutable value, and mutable tension
- carrier locations as signed tick sites on carriers
- relations as the primitive structure builder
- unit shapes as crystallized promises about what one tick means
- tension as the visible remainder when a carrier cannot fully account for
  incoming change
- projection and crystallization as relation-shaped reads, not opaque tags

When a concept is difficult to express, the preferred direction is to extend the
numeric structure rather than add an enum, symbolic operation table, or external
case-specific controller.

## Current Core Pieces

### Carrier

`Carrier` is the physical value site. A carrier has:

- a unique identity
- a flow face, currently represented by `IsInverse`
- one visible unsigned `Value`
- one signed `Tension`
- a stable `UnitShape`
- attached relations that settle the connected local world

Public value input is tension-based:

- `Add(ticks)` feeds positive tension
- `Remove(ticks)` feeds negative tension
- `Set(ticks)` feeds the delta needed to approach a visible value

`Set` is aspirational. It does not bypass the structure. It adds or removes
tension and lets settlement account for as much as possible.

### CarrierLocation

`CarrierLocation` is a signed tick site on a carrier. It is also an `IElement`,
so locations can participate directly in relation structure.

Negative locations are ordinary signed locations on a carrier. They are not the
same thing as an inverse carrier face. Inverse carrier faces are reserved for
opposed-flow participation.

### TickRef

`TickRef` allows multiple locations to share the same tick reference. Moving the
reference moves every location that uses it. This supports physical shared
coordinates without treating matching numeric values as the same point by
coincidence.

### Relation

`Relation` is the primitive ordered structure. It can represent a point,
segment, port, operation surface, route, boundary, nested group, or orthogonal
outlet depending on the shape of its elements.

Official construction helpers:

```csharp
Relation.Point(carrier, ticks)
Relation.Segment(carrier, start, end)
Relation.OrthogonalOutlet(source)
```

These helpers do not create wrapper concepts. They create ordinary relation
structure:

```text
Point(C,n)          = L(C,n) -> C
Segment(C,a,b)     = Point(C,a) -> Point(C,b)
OrthogonalOutlet(A) = A -> Segment(Unknown,0,0)
```

Settlement and reads derive behavior from this relation shape.

### UnitShape

`UnitShape` is the crystallized meaning of one carrier tick. It is stable and
read-only after creation. Carriers can share a unit shape without sharing
identity or value.

Official supported orthogonal unit-shape constructors:

```csharp
UnitShape.CircularFrom(point)
UnitShape.LinearFrom(zeroLengthSegment)
UnitShape.SheetFrom(segment)
```

They all use the same underlying orthogonal outlet shape. The source structure
is what distinguishes them:

```text
CircularFrom(P)       point orthogonal, perimeter or level-line carrier
LinearFrom(S(a,a))    zero-length segment orthogonal, ray or line carrier
SheetFrom(S(a,b))     nonzero segment orthogonal, sheet or rectangle carrier
```

The generic escape hatch remains:

```csharp
UnitShape.OrthogonalOf(element)
```

Prefer the named constructors when the source shape is known. Use
`OrthogonalOf` when the source is intentionally more general.

## Current Settlement Model

Carriers settle through attached relations.

A simple local reset/counter relation:

```text
Point(C,10) -> Point(C,0)
```

means that the local carrier accounts for residue inside the boundary and
leaves crossed value as tension. If that tension has a route to another carrier,
it can be absorbed there.

A compact counter route:

```text
Point(lower,10) -> Point(upper,0)
```

lets `lower` absorb residue in base 10 while routed tension increments `upper`.

This is enough to build ordinary positive decimal-like ladders, counters,
modulo reads, and simple routed carries.

## Current Projection Model

Relations can project values between frames.

Example:

```text
Segment(source,0,20) -> Segment(target,0,-10)
```

Reading `source = 4` through this relation gives `target = -2` with no source
residue. This is the current foothold for signed frame projection. For physical
gear tooth contact, the first demo uses eight circular ticks per tooth and a
one-to-one opposed perimeter-tick read:

```text
Segment(driver,0,1) -> Segment(driven,0,-1)
```

That keeps equal tooth travel visible, so larger gears turn slower than smaller
gears while still allowing in-between visual rotation before the next tooth
position.

Projection is currently a read. It does not automatically mutate the driven
carrier unless code deliberately uses the projected value as input.

For live reads that should continuously feed a concrete carrier,
`anchor.AttachLiveOutput(input, port)` creates ordinary relation structure of
the form `input -> output port`. The projected value settles into the requested
port target. Read residue remains virtual because the source is still present
and can be read again at another available resolution. Residue is preserved as
tension in transfer/import paths where discarding the remainder would lose
incoming change.

## Current Crystallization Model

Crystallization hardens unit shape, not mutable value.

For example, an operation or orthogonal surface can crystallize a new carrier
whose tick shape is known. That carrier may then hold independent value and
tension. It does not remain live-bound to the structure that created it.

This is intentional. A crystallized carrier is a promise that one tick means a
stable thing. If a later structure wants to convert, project, or adapt against
that carrier, it should do so through explicit relation structure.

## Orthogonal Shapes

The current supported orthogonal shapes are official but conservative.

### Circular / Perimeter

A point orthogonal creates a circular or level-line carrier:

```csharp
var center = new Carrier();
var perimeter = new Carrier(UnitShape.CircularFrom(Relation.Point(center, 0)));
```

After crystallization, the perimeter carrier holds ordinary ticks. For a gear,
one tick may represent one tooth position:

```csharp
var rotations = new Carrier();
perimeter.Attach(new Relation(
    Relation.Point(perimeter, 12),
    Relation.Point(rotations, 0)));
```

Adding `27` ticks to a 12-tooth perimeter leaves visible tooth position `3`
and records `2` completed rotations on the rotation carrier.

This does not yet require radius calibration. Radius, diameter, tooth tips, and
tooth valleys can be represented by additional relation structure later.

The carrier itself can be inspected for its structural source:
`perimeter.TryReadCircularOrigin(out origin)` succeeds when its unit shape came
from a point orthogonal. A radial line carrier can likewise be inspected with
`radius.TryReadLinearOrigin(out origin)` when it came from a zero-length segment
orthogonal. A radius calibration is not stored as a separate circle object; it
is ordinary connected structure such as
`Point(radius, r) -> Point(perimeter, p)` between a radial endpoint and a
perimeter marker whose source origins already align. Forward perimeter faces are
the normal contour face, and inverse perimeter faces are the opposed contour
face, but richer handedness still needs explicit structure.

### Linear / Ray

A zero-length segment orthogonal creates a ray or line carrier:

```csharp
var raySeed = Relation.Segment(carrier, 0, 0);
var rayShape = UnitShape.LinearFrom(raySeed);
```

A point and a zero-length segment can be coincident while still producing
different orthogonal shapes. The point has place but no collapsed basis. The
zero-length segment has two related points, so it carries a different structural
source.

### Sheet / Rectangle

A nonzero segment orthogonal creates a sheet or rectangle-like carrier:

```csharp
var basis = Relation.Segment(carrier, 0, 10);
var sheetShape = UnitShape.SheetFrom(basis);
```

This is currently a unit-shape foothold. It does not yet automatically generate
an area-filling settlement process from incoming tension.

## Gear UI Readiness

Gojo is ready for a simple gear UI demo if the first version is framed as a
structural read/projection demo rather than a full physical mesh simulation.

Supported now:

- create circular perimeter carriers
- partition perimeter carriers by tooth count
- count completed rotations through ordinary reset/counter relations
- read opposed gear ratios through signed tooth-contact projection
- represent radius or diameter as separate linear carriers
- keep gear values inspectable as carrier value plus tension

Still future:

- automatic gear mesh settlement where driver tension physically mutates the
  driven gear
- full contour direction calibration independent of declared relations
- tooth tip and tooth valley geometry
- half-pitch offsets as first-class circular reads
- radius/diameter fusion into a full gear object
- hardening new structures from unresolved tension over time

## Important Axioms And Notes

The active Gojo notes live in:

```text
Gojo/axioms/
```

Most important current files:

- `Gojo Unit Shape And Crystallization.md`
- `Gojo Crystallized Resolution.md`
- `Gojo Potential Shape Fact Sheet.md`
- `Gojo Groups Attributes And Lenses.md`
- `Gojo Operation Shape Catalog.md`
- `Gojo Placeholders And Repetition.md`
- `Gojo Structure Through Tension.md`
- `Gojo Carrier Settlement Algorithm.md`
- `Gojo Duality And Relation Settlement Table.md`

Some older Core4-derived notes remain in `Gojo/axioms` and `docs` for context.
They are useful, but current Gojo docs and code should win when there is a
conflict.

## Project Layout

```text
Gojo/
  Core/             carrier, location, relation, tick reference, unit shape
  Equations/        relation-shaped equation surfaces, maps, comparisons
  Patterns/         placeholder binding and repeated structure helpers
  Reads/            read helpers and legacy read footholds
  Diagnostics/      diagnostics for traversal-oriented inspection
  Interpretation/   boolean-style read experiments
  Workspace/        scoped/proxy workspace design notes and future mechanics
  axioms/           active model notes and transition notes

Gojo.Tests/
  Core/             tests for carrier settlement, relations, duality, ports,
                    structural slots, tick refs, and unit shapes
  Equations/        tests for equation helpers and equation catalog cases
```

The root `Gojo.slnx` includes only `Gojo` and `Gojo.Tests`.

## Development Guidance

Use the Gojo numeric system as the canonical internal representation.

Prefer:

- relation shape over operation tags
- calculated reads over stored metadata
- carrier value and tension over hidden contribution tables
- named helpers only when they create ordinary Gojo structure
- tests that compare Gojo behavior to known math or physical expectations

Avoid:

- enums for operation meaning
- symbolic case lists as the primary engine
- global all-knowing interpreter objects
- wrappers that hide meaning outside the numeric structure
- direct mutation of carrier value that bypasses tension settlement

## Test Command

```bash
dotnet test Gojo.slnx
```
