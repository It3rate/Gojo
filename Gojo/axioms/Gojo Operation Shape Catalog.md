# Gojo Operational Read Catalog

This note replaces the older shape-only catalog with the current read-centered
view of operation-like behavior in Gojo.

It is still not a symbolic operator table. A row in this catalog is a
structural affordance: ordinary carrier and relation shape that can be read as
projection, accumulation, product, repetition, settlement, routing, filtering,
or crystallization.

The current read contract is:

```text
selected source
read through this surface or basis
produced this result
left this residue or tension
with this provenance
```

The common helpers for that contract are:

| Helper | Role |
| --- | --- |
| `ReadWitness` | General selected/read/result/residue/provenance skeleton. |
| `BasisReadSurface` | Axis, face, lane, metric evidence, residue, provenance. |
| `TimeStepSequence` | Ordered transition evidence, readable as witnesses. |
| `RepeatSurface` | Selected structure, controller, transition, feedback, residue, timeline. |
| `RefinementRead` | Parameter binding attempt plus witness, residue, and timeline/provenance. |
| `OperationClaim` | Equality and inequality claims with readable residual/violation evidence. |
| `OperationFollow` | Claim plus response route plus before/after settlement timeline. |

The older shape notation remains useful, but it should now be read through this
spine. For example, a ratio projection is not just "division." It is a selected
location read through a basis relation, producing a target location plus source
residue, with provenance through the projection surface.

## Notation

- `L(C,n)` means a location on carrier `C`.
- `P(C,n)` means a point relation: `Relation.Point(C,n)`.
- `S(C,a,b)` means a segment relation: `Relation.Segment(C,a,b)`.
- `A -> B` means ordinary `Relation(A, B)`.
- `C*` means an explicit opposed/inverse carrier face, not an ordinary
  negative location on `C`.
- `A*+` and `A*-` remain provisional shorthand for orthogonal outlets.
  Current structure uses `Relation.OrthogonalOutlet(A)`.
- `A | B` remains provisional shorthand for "A read through calibrator B."
  The right side is the read surface.
- `^n` means repeated relation shape. Current executable structure should use
  `RepeatSurface` and usually a `TimeStepSequence`.

Negative locations and opposed faces must stay distinct:

```text
L(C,-5)      ordinary negative signed position on C
L(C*,5)      positive participation on the opposed face of C
L(C*,-5)     negative location on the opposed face
```

## Point And Segment Projection Reads

These are the smallest operation-like reads. They are best treated as
`BasisReadSurface` or `ReadWitness` evidence when another structure needs to
carry the read result forward.

| Shape | Current read |
| --- | --- |
| `L(C0,0) -> L(C0,k)` | Forward projection/basis surface. Values are interpreted through it, not hardened by it. |
| `L(C0,k) -> L(C0,0)` | Reverse return surface. Crossing can wrap local residue toward origin and leave crossed tension. |
| `L(C0,k) -> L(C0,k)` | Same-site stop surface. Clamp-like hardening at `k`. |
| `P(C0,0) -> P(C1,0)` | Origin attachment or shared reference. No automatic accumulation. |
| `P(C0,1) -> P(C1,0)` | One-for-one accumulator. Every tick crosses each point layer. |
| `P(C0,k) -> P(C1,0)` | Compact base/counter absorption. Lower hardens, emits carrier tension, and routes through `k:1` into `C1`. |
| `P(C0,k) -> P(C1,m)` | Ratio projection. `k` ticks on `C0` read as `m` ticks on `C1`; source residue stays visible. |
| `P(C0,k) -> P(C1,-m)` | Negative ratio projection. Positive source participation reads as negative target participation. |
| `P(C0,-k) -> P(C1,m)` | Negative ratio projection from the other side. |
| `P(C0,-k) -> P(C1,-m)` | Aligned negative basis; reads as a positive ratio. |

The ratio projection read returns relation-shaped state:

```text
projected target value -> source residue
```

For example, reading `L(C0,10)` through `P(C0,3) -> P(C1,1)` gives
`L(C1,3) -> L(C0,1)`. The residue is not a hidden decimal. It is source-basis
residue.

## Same-Carrier Accounting Reads

Same-carrier shapes are settlement surfaces as well as reads.

| Shape | Current read |
| --- | --- |
| `P(C,k) -> P(C,0)` | Reset/modulo boundary. Carrier hardens to `value % k`; crossed cycles remain tension unless routed. |
| `P(C,k) -> P(C,k)` | Clamp boundary. Carrier hardens to `min(value,k)`; excess remains tension. |
| `P(C,k) -> P(counter,0)` | Crossing counter. Carrier tension can be routed into another layer. |
| `(P(C,k) -> P(C,0)) -> P(counter,0)` | Explicit counter route: local modulo plus exported cycle count. |

The same reset shape can read as a partitioned cycle in a contour frame:

```text
P(C,12) -> P(C,0)
```

This can mean a 12-part circular phase closure without yet calibrating radius
or tooth geometry. The closure is not a traversable return journey by itself.
A loop with a return path still needs explicit opposed carrier or route
evidence.

## Segment And Frame Reads

Segments preserve frame direction. A same-carrier segment is not the same as a
point carry relation.

| Shape | Current read |
| --- | --- |
| `S(C0,0,1) -> S(C1,0,1)` | Segment-to-segment projection; unit duplicate/copy basis. |
| `S(C0,a,b) -> S(C1,c,d)` | Ordered frame projection. `x` on `C0` reads as `c + (x-a)*(d-c)/(b-a)`. |
| `S(C0,0,k) -> S(C1,0,m)` | Origin-based ratio stretch. |
| `S(C0,k,0) -> S(C1,0,m)` | Reversed source frame. Reads from perspective, not sorted span. |
| `S0 -> S1.P1` | Basis scaling. Source segment flow moves endpoint of `S1` while `S1` remains a segment. |
| `S0 -> P(C1,n)` | Move a point from source segment flow. If that point is a segment endpoint, the segment stretches. |
| `S(C,5,5)` | Zero-length segment. Coincident with a point, but still exposes collapsed basis. |

Segment projection should normally be witnessable as a basis read:

```text
selected source location/span
axis or face surface = segment/frame relation
lane or unit basis = projected frame
metric evidence = projected coordinate or span
residue = unreduced source remainder
provenance = projection surface
```

## Orthogonal And Product Reads

Orthogonal shape formation is currently represented by relation-readable unit
shape provenance:

```text
Relation.OrthogonalOutlet(A)
```

Official construction helpers include:

```text
UnitShape.CircularFrom(P)            point orthogonal / perimeter carrier
UnitShape.LinearFrom(S(C,a,a))       zero-segment orthogonal / ray carrier
UnitShape.SheetFrom(S(C,a,b))        nonzero segment orthogonal / sheet carrier
```

These helpers do not add a second representation. They expose the current
relation shape at API boundaries.

| Shape | Current read |
| --- | --- |
| `P*+` | Point pushed into a positive orthogonal; circle-like or level-line candidate. |
| `P*-` | Point pushed into opposed orthogonal; inward/opposed circle-like candidate. |
| `S*+` | Segment pushed into positive orthogonal; sheet/rectangle candidate. |
| `S*-` | Segment pushed into opposed orthogonal. |
| `S(C,a,a)*+` | Zero-length segment orthogonal; ray/line seed constrained by collapsed segment basis. |
| `S*+ | S` | Segment orthogonal calibrated by source segment; square-like unit candidate. |
| `P*+ | R` | Point contour calibrated by radius/ray-like structure. |

Product operation surfaces inspect carrier-face participation before scalar
collapse. Opposed-face participation is preserved structurally:

```text
S(X*,0,2) * S(Y,0,5) -> L(Area*,10)
```

For two-sided directed segment product:

```text
(2 inverse + 3 forward) on X
*
(4 inverse + 5 forward) on Y
```

the product read distributes visible terms:

```text
AC: P(X*,2) * P(Y*,4) -> L(Area*,8)
AD: P(X*,2) * P(Y, 5) -> L(Area*,10)
BC: P(X, 3) * P(Y*,4) -> L(Area*,12)
BD: P(X, 3) * P(Y, 5) -> L(Area,15)
```

A later scalar-face read may fold positive and opposed values:

```text
Fold(Area:15, Area*:8)  -> Area:7
Fold(Area:3,  Area*:10) -> Area*:7
```

That fold should not erase product-term provenance when the fused value is
still under inspection.

## Operational Port Reads

An operational port is a relation over exposed port surfaces. The operation is
not an enum such as `Add` or `Multiply`; the output surface decides the read
shape.

```text
(portA -> portB) -> P(Result,1)
  point output: additive collapse

(portA -> portB) -> S(Result,0,1)
  segment output: multiplicative or higher-grade composition
```

Each operand is first projected through its own exposed port surface into the
reader/output carrier. The operation then folds projected visible values.
Residues remain relation-shaped in operand order:

```text
operation read = L(Result,n) -> (residueA -> residueB)
```

This is why a read over:

```text
S(X,0,10) -> P(Unknown,1)
S(Y,0,10) -> P(Unknown,1)
```

uses exposed whole units first. If `X=17` and `Y=28`, a point-output additive
operation reads:

```text
L(Result,3) -> (L(X,7) -> L(Y,8))
```

It does not silently reach beneath the exposed ports to add `17 + 28`.

Current executable footholds:

- `Relation.TryReadOperationFrom(input, reader, out read)`
- `Relation.TryReadOperationBasisOn(carrier, out basis)`
- `Relation.TryCrystallize(out carrier)`
- `Relation.TryCrystallize(input, out carrier, out read)`
- `Relation.TryProjectCrystallizedValueFrom(input, carrier, out read)`

## Repetition And Power Reads

Do not add a magic repeat operator. Repetition should remain visible as
selected structure, controller, transition, feedback, residue, and timeline.
When re-entry matters, the selected structure can expose input ports and output
ports, with feedback relations connecting output ports back into later input
ports. A non-repeated operation is the same shape with one frame and no
meaningful feedback traversal.

The current executable foothold is `RepeatSurface`:

```text
selected:   S(input,0,1) -> S(Unknown,0,1)
controller: S(repeat,0,n)
transition: Product(output, repeated unit ports)
feedback:   witnessed repeated factors
residue:    product read residue
timeline:   per-repeat partial products
```

The general port form is:

```text
template:   inputs, selected structure, outputs
frame:      port bindings
transition: next output bindings read through the current frame
feedback:   output port -> next input port
```

Readable examples:

| Shape | Current read |
| --- | --- |
| `(P(C0,1) -> P(C1,0))^n` | Additive repetition. Every tick crosses each point layer. |
| `(P(C0,k) -> P(C1,0))^n` | Repeated base/counter carry; positional positive-integer stack. |
| `(S0 -> S1.P1)^2` | Square-like repeated basis scaling. |
| `(S0 -> S1.P1)^3` | Cube-like repeated basis scaling. |
| `S0 -> S1 -> S2 -> S3` | Repeated segment projection, each layer using explicit frame lengths. |

Polynomial term fanout uses the same repeated product structure for
`1, x, x^2, x^3, ...`. Literal controller spans can be read as `x^n`; open or
derived controllers remain visible as repeat structure.

## Claims, Feedback, And Settlement Reads

Operation-like behavior also includes claims and response routes.

`OperationClaim` builds relation-shaped claims:

```text
Equality(actual, expected)
AtMost(value, ceiling)
AtLeast(value, floor)
Between(value, lower, upper)
```

Each claim exposes:

```text
claim surface
residual
violation
residual magnitude
violation magnitude
residue
```

This is not validation metadata. The residual and violation are ordinary Gojo
evidence that can feed later reads.

`OperationFollow` adds a response route:

```text
claim
response route
applied adjustment
next value
remaining residual
route residue
```

Repeated follow settlement is recorded as `TimeStepSequence`, with each step
carrying before state, follow surface, after state, and residue evidence.

## Route, Transfer, Filter, And Capacity Reads

Pass/block/filter behavior now belongs primarily in the boundary-flow stack,
because it needs axis/domain, flow exposure, legal lanes, occupied lanes,
capacity, and residue to remain inspectable.

The current chain is:

```text
BoundaryFlowValueRoute
  source value
  projection route
  target lane basis
  target value
  residue or tension

BoundaryFlowValueTransfer
  route surface
  accepted value
  blocked value
  capacity or gate
  accepted witness
  blocked witness

BoundaryFlowTargetSettlement
  target before
  transfer surface
  target after
  residue or tension
  timeline

BoundaryFlowSharedCapacityTransfer
  capacity
  route group
  transfer group
  residue group
  unused capacity
  allocation witness
  timeline
```

This is the better home for rows that used to be listed as loose
boolean/filter footholds:

| Older sketch | Current direction |
| --- | --- |
| travel segment -> support segment | route projection plus target basis read. |
| travel segment -> gap in support surface | blocked transfer witness plus residue/tension. |
| support A + support B | repeated or grouped lane/capacity read. |
| support A opposed support B | opposed-face evidence plus claim/settlement residue. |
| selected basis passes | accepted witness. |
| nonmatching basis remains | blocked witness or lane tension. |

## Crystallized Operation Reads

Crystallization hardens an operation read as a new carrier tick without making
the helper the source of meaning.

Before crystallization:

```text
raw slots -> grouped port -> operational port
```

After crystallization:

```text
operational port result = one tick on a new carrier
```

The construction below the crystal may still exist and may still drive the
value. The crystallized carrier gives a lower-resolution local view with its
own unit shape and projection basis.

Readable examples:

```text
paired width/height extents -> product operational port -> area carrier tick
directed segment endpoints -> length operational port -> length carrier tick
```

Incompatible additive operation shapes can still be shape-crystallizable even
when they are not value-projectable into one scalar carrier. That case wants a
multi-component crystal surface, not forced scalar collapse.

## Rewrite Rules For New Operation Shapes

When adding a new operation-like behavior, use this checklist:

1. Identify the selected structure.
2. Identify the read surface or basis.
3. Decide whether the read is general (`ReadWitness`) or basis-bearing
   (`BasisReadSurface`).
4. Preserve result, residue/tension, and provenance.
5. If the operation repeats, use `RepeatSurface` and `TimeStepSequence`.
6. If it binds a parameter, use `RefinementRead`.
7. If it asserts a target relation, use `OperationClaim`.
8. If it routes feedback, use `OperationFollow` or an explicit route witness.
9. If it passes or blocks material/value, use boundary-flow route and transfer
   witnesses.
10. Only crystallize after the operation read is already visible as structure.

The guardrail remains simple:

```text
if a behavior needs a new action name,
first try to express it as carriers, points, segments, relations,
unit-shape descriptors, witnesses, residue, tension, and timelines
```

No separate symbolic operation table should be introduced unless the numeric
and structural relation surface truly cannot carry the meaning.
