# Gojo Ports, Fusion, And Operational Reads

This note records the current direction for ports and fused objects in Gojo.
It is intentionally relation-shaped: ports, grouped ports, fusion surfaces, and
operational reads should all be expressed with the same carrier/relation
machinery already used by settlement and projection.

For unit-shape and crystallization rules, see
`Gojo Unit Shape And Crystallization.md`.

## 1. Ports

A port is an exposed relation surface.

The smallest carrier output port is:

```text
ValueSlot(C) = L(C, C.Value)
```

The smallest carrier input port is a relation from an exposed unit surface to a
carrier surface:

```text
P(U,1) -> P(C,1)
```

or a calibrated/grouping surface:

```text
P(U,1) -> S(C,0,10)
```

Ports do not bypass carrier settlement. A port write projects the incoming
value through the port, then feeds the resulting carrier target through
slot-style `Set`, which means:

```text
receive(target - current visible Value)
```

Any projection residue remains as tension on the residue carrier.

## 1A. Relation, Port, Boundary, Crystal

`Relation` is the primitive structure:

```text
A -> B
```

It may be private internal structure, a route, a point, a segment, a grouped
surface, or an operation. A port is not a second primitive. A port is a
relation surface that remains exposed across an object boundary.

The boundary is not merely the place where two structures touch. When two
structures connect, settlement may treat them as one connected world. A
boundary persists only when some surface remains deliberately separable:

```text
private relation      ordinary internal structure
open port             exposed relation surface available for connection
connected port        exposed relation surface with an active connection
sealed port           exposed relation surface no longer accepting more input
absorbed boundary     former interface now treated as ordinary internal shape
crystallized boundary exposed operation hardened as a new carrier tick
```

These are roles of relation surfaces, not different mathematical kinds. A
concrete exact-carrier port:

```text
P(Heat,1) -> S(Display,0,10)
```

and an internal relation of the same shape use the same numeric machinery. The
difference is whether the surface is preserved as an interface. `Unknown`
makes a port discovery-friendly, but `Unknown` is not what makes it a port.

## 1B. Unknown Discovery Ports

A concrete carrier in a port frame means exact physical carrier identity:

```text
P(Heat,1) -> S(Display,0,10)
```

accepts `L(Heat,n)`, but not `L(Light,n)`.

Discovery often needs a looser surface. `Carrier.Unknown` is the current
wildcard frame:

```text
P(Unknown,1) -> S(Heat,0,10)
```

accepts any incoming carrier. If `L(Light,2)` arrives, the port treats the
source frame as `Light` for this projection and maps it into `Heat`:

```text
L(Light,2) -> L(Heat,20)
```

Residue stays on the actual incoming carrier:

```text
P(Unknown,3) -> S(Heat,0,10)
L(Light,1) imports as L(Heat,3) with L(Light,1) tension
```

Unknown can also be used on an output/read side:

```text
S(C,0,10) -> P(Unknown,1)
```

so any reader carrier can try the output perspective. This is a discovery
surface, not a category proof. A stabilized structure can later replace
`Unknown` with a concrete carrier or a richer category/provenance mechanism.

## 2. Port Groups

A port group is a relation-shaped collection of ports:

```text
(P(Sx,1) -> P(Sy,1)) -> (S(Cx,0,10) -> S(Cy,0,4))
```

This is already executable for carrier value slots. An input shaped as:

```text
L(Sx,3) -> L(Sy,2)
```

imports each paired leaf slot:

```text
Cx receives target 30
Cy receives target 8
```

The whole shape must validate before any target slot changes. A grouped port is
therefore the first object boundary: it lets several slots be handled as one
interface without hiding their individual carriers.

## 2A. Structural Ports

Value ports feed carrier state. Structural ports retarget exposed structure.

The target side of a structural port must be an existing structural site:

```text
P(C,k)
S(C,a,b)
shared TickRef grounded on one or more carrier faces
```

The import still uses the same source-to-target projection surface:

```text
P(U,10) -> P(C,20)
L(U,15) imports as structural retarget to L(C,30)
```

but it commits by moving the target `TickRef` instead of calling carrier
`Set`. Carrier value is not directly changed:

```text
P(U,10) -> P(C,x)
L(U,12) moves x to 12
C.Value is unchanged
```

If the target `TickRef` is shared across carrier faces, the coordinate moves
everywhere that reference is grounded:

```text
x on C and C* moves as one coordinate reference
```

For a segment target, the current foothold retargets the existing endpoint
exposed by the target surface. It does not create a new segment, a new basis,
or a hidden interpretation. Residue from inexact projection is preserved as
tension on the actual residue carrier, just as value-port imports do.

Relation-shaped structural input is decomposed only when the receiving surface
is group-shaped. A frame-readable input remains atomic:

```text
S(U,0,12) -> S(C,0,x)
```

means "retarget this exposed basis/span from the incoming span." It does not
mean "set the left endpoint, then set the right endpoint" unless the receiving
port explicitly exposes those endpoints as separate slots.

A port also references existing surfaces. A same-shaped lookalike is not a
reference to hidden internal structure:

```text
internal Bx = S(X,0,u)
lookalike  = S(X,0,u')
port uses lookalike
```

Retargeting the port moves `u'`, not `u`. A reference is allowed to expose or
retarget structure only when that structure is actually the referenced surface.
It must not manufacture a new basis by externally interpreting an object's
internals.

## 3. Port Operations Act On Ports

Operational reads should operate on the exposed port surfaces, not raw carrier
internals.

If a port reads through a basis, the operation uses that basis:

```text
S(C,0,10) -> P(U,1)
```

means the operation sees units of `U`, with residue in `C`, not naked native
ticks unless the port exposes naked ticks.

This matters for physical interpretation. A value of `27` on `C` may be read
through the port as:

```text
L(U,2) -> L(C,7)
```

An operation over that port operates on the exposed `2 7|10` surface. It does
not silently reach underneath and operate on `27` unless the port is the raw
tick port.

## 4. Operational Ports

An operational port is a single relation over one or more port surfaces.

The operation is not an enum such as `Multiply` or `Add`. It is the relation
shape and its attachment values. This matches existing cross-carrier reads:

```text
P(A,2) -> P(B,3)
```

reads `A` through a `3:2` relation. Adjusting the relation values adjusts the
operation.

For a fused object with two exposed component ports:

```text
Object = A -> B
```

possible operational ports include:

```text
add-like relation       = exposed as one unit-compatible read
multiply-like relation  = exposed as a higher-grade unit read
subtract-like relation  = opposite directed relation
divide-like relation    = reverse traversal / inverse read
```

The names are explanatory only. The kernel should prefer the numeric relation
surface itself over symbolic operation tags.

The current executable foothold reads operation shape from the exposed output
surface:

```text
(portA -> portB) -> P(Result,1)      point output: additive collapse
(portA -> portB) -> S(Result,0,1)    segment output: multiplicative composition
```

Each operand is first projected through its own exposed port surface into the
reader/output carrier. The operation then folds those projected visible values.
Residues remain relation-shaped in the same operand order:

```text
operation read = L(Result,n) -> (residueA -> residueB)
```

So an operation over:

```text
S(X,0,10) -> P(Unknown,1)
S(Y,0,10) -> P(Unknown,1)
```

sees whole exposed units first. If `X=17` and `Y=28`, an additive point-output
operation reads:

```text
L(Result,3) -> (L(X,7) -> L(Y,8))
```

It does not silently reach beneath the exposed ports to add `17 + 28`.

An operational port should expose an operation surface that is present in, or
constructible from, the object's relation structure. It should not mean "any
external observer may request any operation over raw internals." Exploration
may still happen outside an object: a probing structure can read two exposed
values, combine them, and discover that the combination lowers tension or
becomes useful. If that combination stabilizes, it may be promoted back into
the source structure as an internal operation surface and then exposed as a
port.

So:

```text
external exploratory read -> useful relation -> promoted operation surface -> exposed operation port
```

This keeps experimentation possible without making every conceivable operation
part of every object boundary.

## 5. Directed Segment

A directed segment can be exposed as a two-slot port group:

```text
SegmentPort = Start -> End
```

As an unfused grouped port, this is just two exposed slots. As a fused object,
the port group becomes one object boundary:

```text
D = P(C,start) -> P(C,end)
```

Operational ports over `D` may expose:

```text
flow / signed length    = End read from Start through D
translation             = same perturbation applied to both endpoint ports
endpoint edit           = one endpoint changes, flow/length changes
```

The operation acts on the endpoint ports. If those endpoint ports are
calibrated, length is read in the calibrated basis. If they are raw tick ports,
length is read in native ticks.

## 6. XY Objects

An XY object is also a two-slot port group:

```text
XY = X -> Y
```

The component shapes determine the physical object:

```text
P(X,x) -> P(Y,y)             coordinate point
S(X,x0,x1) -> S(Y,y0,y1)     paired X/Y extents
P(X,x) -> S(Y,y0,y1)         vertical segment
S(X,x0,x1) -> P(Y,y)         horizontal segment
```

Fusion says this is one object boundary, not merely two unrelated exposed
slots. It does not decide which operation will later be read from those slots.

Operational ports over the fused boundary may expose:

```text
add-like read       X + Y      length of an L, half perimeter of a rectangle
multiply-like read  X * Y      area-like read when both exposed components are extents
subtract-like read  X - Y      directed offset read
ratio-like read     X / Y      comparison or slope-like read
```

The result grade depends on the operational port:

```text
add/subtract        unit-compatible grade, such as u
multiply            higher grade, such as u^2 or u*v
division            inverse/read grade, such as u/v
```

The units come from the port surfaces, not from raw carrier storage.

## 7. Repetition And Powers

Do not add a magic repeat operator yet.

If a structure needs something like:

```text
a^3 + b
```

represent it as repeated port participation:

```text
A1, A2, A3, B
```

where `A1`, `A2`, and `A3` may share the same source port or reference. The
relations between those ports can then express the repeated operation. This
keeps the repeated structure visible and inspectable.

## 8. Crystallization

Crystallization is hardening one operational port as a new carrier tick.

Before crystallization, a grouped or operational port may still expose its
internal slots:

```text
width port -> height port -> area-like operational port
```

The operational port is readable because the exposed surfaces remain visible.
It may be connected, disconnected, or absorbed into larger structure, depending
on whether the boundary is preserved.

Before crystallization:

```text
raw slots -> grouped port -> operational port
```

After crystallization:

```text
operational port result = one tick on a new carrier
```

This does not require destroying the internal structure. It creates a new
surface that treats the operational read as one carrier unit.

After crystallization, a local reader works with the new carrier surface. The
construction below the crystal may still exist and may still drive the value,
but the local view no longer has ordinary access to the internal slots unless
another port explicitly exposes them.

So:

```text
paired width/height extents -> multiply-like operational port -> area carrier tick
directed segment endpoints -> length operational port -> length carrier tick
```

Crystallization is therefore the strongest preserved boundary: it keeps a
separation even while the underlying structure may remain physically connected.

The current code implements an initial crystallization foothold: an operation
surface can derive a reusable unit shape, create a new carrier with that
shape, and project a compatible current operational read onto that carrier.
Projection feeds the crystal by delta through carrier settlement and preserves
operand residue as visible relation-shaped residue.

Incompatible additive operation shapes remain shape-crystallizable but are not
yet value-projectable into one scalar carrier. That needs a multi-component
crystal surface rather than a forced collapse.

## 9. Current Code Foothold

In current Gojo code:

- `Carrier.ValueSlot` exposes `L(C, Value)`.
- `Carrier.TensionSlot` explicitly exposes unresolved tension.
- `Carrier.TrySetFrom(port, input)` imports one slot through a relation frame.
- `Relation.TrySetFrom(input)` imports matching relation-shaped groups.
- `Relation.TrySetStructureFrom(input)` imports matching relation-shaped
  groups into existing structural sites by retargeting exposed `TickRef`
  coordinates rather than carrier values.
- Structural imports decompose relation-shaped input only through group-shaped
  receiving surfaces; frame-readable inputs are treated as atomic basis/span
  values.
- `Carrier.Unknown` acts as a wildcard port frame for discovery imports and
  reads; concrete carriers still require identity match.
- `Relation.Project(location, reader)` provides the current single-location
  projection through point/segment frames.
- `Relation.TryProject(location, reader, out projected)` reports whether a
  port actually accepted the projection. Imports use this so an observation
  fallback of zero-plus-residue cannot accidentally write a target.
- `Relation.TryReadOperationFrom(input, reader, out read)` reads the first
  operation-surface foothold over port groups: point output folds projected
  operands additively, segment output folds them multiplicatively, and residues
  remain visible as a relation-shaped tail.
- `Relation.TryReadUnitShape(out unitShape)` derives the unit shape exposed by
  frame, port, and operation surfaces.
- `Relation.TryCrystallize(out carrier)` creates a new carrier from the
  derived unit shape without projecting current value.
- `Relation.TryCrystallize(input, out carrier, out read)` creates a crystal
  carrier and projects the compatible current operation read onto it.
- `Relation.TryProjectCrystallizedValueFrom(input, carrier, out read)` projects
  a compatible current operation read onto an existing crystal carrier.

This supports carrier-value multi-slot ports and the first structural-port
foothold. The important guardrail is the same in both cases:

```text
validate whole port shape first
then commit every slot
preserve residue as tension or explicit residue slot
```

The next missing step is fused operational ports over grouped object
boundaries. Those should operate over exposed port surfaces, including
structural ports, rather than raw carrier internals.

No separate symbolic operation table should be introduced unless the numeric
relation surface cannot carry the meaning.
