# Gojo Operation Shape Catalog

This note gives a compact notation for operation-like behavior that falls out
of relation shape. It is not a symbolic operator table. It is a checklist of
structural affordances we have seen or expect to see.

Current Gojo scalar reverse/negative participation is written as signed
locations on the same carrier, for example `L(C,-5)`. Rows that mention
`C_inverse` describe explicit dual-face routes, not the ordinary negative
scalar side of `C`. Product reads now preserve inverse-flow participation:
an opposed operand flips the output carrier face, and each opposed pair
negates the output tick value.

Notation:

- `L(C,n)` means a location on a carrier, not a relation: L(C,3)
- `P(C,n)` means a point, a relation of (L->C): `(C.n -> C)`
- `S(C,a,b)` means a segment: `P(C,a) -> P(C,b)`
- `S0.P0` and `S0.P1` mean the first and second point of segment `S0`
- `A -> B` means an ordinary `Relation(A, B)`
- `A*+` and `A*-` are provisional notation for positive/opposed orthogonal
  pushes from `A`; the current relation-readable surface is
  `A -> S(Unknown,0,0)`
- `A | B` is provisional notation for "A through calibrator B"; the right
  side calibrates the left side
- `^n` means repeated relation shape, not a primitive exponent operator
- "read layer" means the carrier layer inspected after the structure settles

## Basic Shapes

| Shape | Read |
|---|---|
| `L(C0,0) -> L(C0,k)` | forward projection/basis surface; values are interpreted through it, not hardened by it |
| `L(C0,k) -> L(C0,0)` | reverse return surface; crossing `k` wraps local residue back toward origin and leaves crossed tension |
| `L(C0,k) -> L(C0,k)` | same-site stop surface; clamp-like hardening at `k` |
| `P(C0,0) -> P(C1,0)` | origin attachment / shared reference; no automatic accumulation |
| `P(C0,1) -> P(C1,0)` | one-for-one accumulator; every tick crosses upward |
| `P(C0,k) -> P(C1,0)` | compact base/counter absorption; lower hardens, emits carrier tension, and routes that tension through `k:1` into `C1` |
| `P(C0,k) -> P(C1,m)` | ratio projection when `m != 0`; `k` ticks on `C0` read as `m` ticks on `C1`, with no source clearing |
| `P(C0,k) -> P(C1,-m)` | negative ratio projection; positive source participation reads as negative target participation |
| `P(C0,-k) -> P(C1,m)` | negative ratio projection from the other side; same scalar sign as positive-to-negative |
| `P(C0,-k) -> P(C1,-m)` | aligned negative basis; reads as a positive ratio |
| `P(C0,k) -> P(C0,0)` | same-carrier reset boundary; local carrier hardens to `value % k`, unaccounted carrier tension remains as `value - value % k`; also the first partitioned cycle/phase closure shape |
| `P(C0,k) -> P(C0,k)` | same-carrier clamp boundary; local carrier hardens to `min(value,k)`, excess remains as carrier tension |

Same-carrier local accounting is directional. Forward `0 -> k` and reverse
`k -> 0` are different physical shapes even when they mention the same two
locations. Forward `0 -> k` is aligned with carrier flow and does not create
tension by itself.

## Orthogonal Shape-Unit Candidates

These shapes are conceptual footholds for the next unit-shape layer. They are
represented as relation-readable unit-shape provenance by relating the source
to the uncalibrated zero-segment orthogonal outlet:

```text
A*+  ~=  A -> S(Unknown,0,0)
```

`Relation.TryReadUnitShape(...)` and `Relation.TryCrystallize(...)` now
recognize that surface before ordinary port reads. `UnitShape.OrthogonalOf`
emits the same structure, and `UnitShape.TryReadOrthogonalSource(out source)`
or `UnitShape.TryReadOrthogonalSource(surface, out source)` inspects it.
Settlement still needs an explicit carrier/surface to receive the shape; `P*+`
and `S*+` are not yet standalone mutation operators. The goal is to treat
multiplication-like shape formation as a push into an orthogonal supplied by
the source structure, not as a hard-coded two-scalar calculation.

Current official constructor names are intentionally descriptive:

```text
Relation.Point(C,n)                  P(C,n)
Relation.Segment(C,a,b)              S(C,a,b)
Relation.OrthogonalOutlet(A)         A -> S(Unknown,0,0)
UnitShape.CircularFrom(P)            point orthogonal / perimeter carrier
UnitShape.LinearFrom(S(C,a,a))       zero-segment orthogonal / ray carrier
UnitShape.SheetFrom(S(C,a,b))        nonzero segment orthogonal / sheet carrier
```

These names do not add a second representation. They only make the current
relation shape explicit at API boundaries. Circular and linear carriers can
look similar after crystallization because both hold ordinary ticks; their
difference is the source structure preserved in the unit shape.

| Shape | Read |
|---|---|
| `P*+` | point pushed into positive orthogonal; unconstrained level-line/circle-like shape candidate |
| `P*-` | point pushed into opposed orthogonal; inward/opposed circle-like candidate |
| `S*+` | segment pushed into positive orthogonal; uncalibrated sheet/rectangle candidate |
| `S*-` | segment pushed into opposed orthogonal; opposite-side sheet/rectangle candidate |
| `S(C,a,a)*+` | zero-length segment orthogonal; ray/line seed constrained by the segment's carrier/basis |
| `S*+ | S` | segment orthogonal calibrated by the source segment; square-like shape unit candidate |
| `P*+ | R` | point contour calibrated by a radius/ray-like structure |
| `Z*+ | E` | zero-segment ray seed calibrated by endpoint `E` |

A point and a zero-length segment can be coincident while exposing different
orthogonals. A point supplies place but no rotational orientation constraint;
its unconstrained orthogonal can close as a circle-like level line. A
zero-length segment supplies a collapsed basis; its orthogonal can become a
ray or line seed. This distinction should be structural, not a symbolic mode.

For gear-style structures, a circular carrier created from
`UnitShape.CircularFrom(P(center,0))` can be partitioned directly with ordinary
ticks. Twelve teeth are just a reset/counter relation at tick `12`; higher
resolution tooth geometry can later subdivide the same perimeter so a tooth tip
and tooth valley differ by half a pitch. A simple external gear mesh can be
read as opposed perimeter projection, for example
`S(driver,0,20) -> S(driven,0,-10)`.

The `|` notation is intentionally right-calibrating: `shape | calibrator` means
the left structure is read through the right structure. This matches ordinary
Gojo projection habits where the right side is the target/read surface. The
notation is provisional; code should not add a parser for it until the relation
encoding is clear.

Inverse syntax remains open for notation, but the core product behavior is no
longer deferred. An inverse-flow operand acts by inverting the carrier face of
what it multiplies. Repeating that action gives the ordinary `1, i, -1, -i`
cycle without a symbolic operation table:

```text
i * 1   ->  i
i * i   -> -1
i * -1  -> -i
i * -i  ->  1
```

This is carrier-face behavior. It does not mean every reverse segment is an
inverse carrier, and it does not collapse positive and inverse components
during addition.

There are two related segment/fused-value shapes here:

- `S(C,a,b)` is an ordered same-face frame; its direction is the frame extent
  `b-a`.
- any binary fused pair containing a carrier and its opposed face, in either
  order, is a fused carrier-face value for math reads:

```text
P(C*,a) -> P(C,b)
P(C,b)  -> P(C*,a)
L(C*,a) -> L(C,b)
L(C,b)  -> L(C*,a)
```

- `DS(C,a,b)` is only a convenient directed endpoint constructor for one such
  fused pair:

```text
DS(C,a,b) = P(C*,a) -> P(C,b)
```

The order can carry other interpretation, but multiplication, addition-like
folding, and scalar collapse should read the fused faces structurally rather
than checking whether the relation was created by the directed segment helper.

Product operation surfaces inspect these fused carrier-face inputs before
collapsing to a scalar product. The directed segment helper is therefore only
a convenient way to build one fused shape; manually constructed positive/opposed
pairs read the same way.

Aligned point-output operation surfaces also preserve opposed-face
participation during addition. Adding `L(X*,2)` and `L(Y,5)` through ordinary
aligned ports produces a fused result like `L(Result*,2) -> L(Result,5)`,
not the scalar `Result:7` and not the folded scalar `Result:3`.

Same-face segment inputs can already participate in product reads through
their extent. A segment on an inverse face carries inverse-flow participation:

```text
S(X*,0,2) * S(Y,0,5) -> L(Area*,10)
```

For a two-sided directed segment product such as:

```text
(2 inverse + 3 forward) on X
*
(4 inverse + 5 forward) on Y
```

the product read distributes the four endpoint terms as:

```text
AC: P(X*,2) * P(Y*,4) -> L(Area*,8)
AD: P(X*,2) * P(Y, 5) -> L(Area*,10)
BC: P(X, 3) * P(Y*,4) -> L(Area*,12)
BD: P(X, 3) * P(Y, 5) -> L(Area,15)
```

So `AC` is not a second subtraction of a positive area. It is already an
opposed-area term: `Area*:8`, with provenance through inverse/inverse
endpoint participation. A scalar collapse can freely read that as negative
area and cancel against `BD`, but the structural product first preserves the
carrier face.

Folding positive and opposed values is a separate scalar-face read:

```text
Fold(Area:15, Area*:8)  -> Area:7
Fold(Area:3,  Area*:10) -> Area*:7
```

That fold is useful when the structure asks for one visible scalar face. It
should not erase product-term provenance when the multi-component/fused value
is still being inspected.

## Segment And Scaling Shapes

| Shape | Read |
|---|---|
| `S(C0,0,1) -> S(C1,0,1)` | segment-to-segment projection; unit duplicate/copy basis shape |
| `S(C0,a,b) -> S(C1,c,d)` | ordered frame projection; `x` on `C0` reads as `c + (x-a)*(d-c)/(b-a)` on `C1`; residue remains unreduced |
| `S(C0,0,k) -> S(C1,0,m)` | origin-based ratio stretch; `x` on `C0` reads as `x*m/k` on `C1` |
| `S(C0,k,0) -> S(C1,0,m)` | reversed source frame; `x` reads as `(x-k)*m/(0-k)`, not as a sorted span |
| `S0 -> S1.P1` | multiplication-like basis scaling; source segment flow moves the endpoint of `S1` while `S1.P0 -> S1.P1` remains intact |
| `S0 -> P(C1,n)` | move a point from source segment flow; if that point is an endpoint of a segment, the segment stretches |
| `S(C,5,5)` | zero-length segment; point-like now, but still two related points that can later separate |

The current ratio projection read returns relation-shaped state:

```text
projected value -> unresolved source residue
```

For example, reading `L(C0,10)` through `P(C0,3) -> P(C1,1)` gives
`L(C1,3) -> L(C0,1)`. This is division without decimal collapse. The residue
is source-basis residue, not a hidden decimal. Reading backward through
`P(C0,2) -> P(C1,3)` gives the inverse ratio.

Segment projection is frame-aware. `S(C,5,15) -> P(U,1)` reading `L(C,28)`
gives `L(U,2) -> L(C,3)`, or `2 3|10`. `S(C,10,0) -> P(U,1)` reading
`L(C,23)` gives `L(U,-1) -> L(C,3)`. That is perspective observation; it does
not change how `P(C,10) -> P(C,0)` settles carrier flow as wrap/carry tension.

A same-carrier segment such as `S(C,2,5)` remains segment structure. It is not
read as `P(C,2) -> P(C,5)` point carry, because that would collapse local
extent into overflow behavior and make ordinary segments unsafe to carry in a
relation surface.

## Repetition And Powers

| Shape | Read |
|---|---|
| `(P(C0,1) -> P(C1,0))^n` | additive repetition; every tick crosses each point layer without scaling the amount |
| `(P(C0,k) -> P(C1,0))^n` | repeated base/counter carry; positional positive integer stack with `n + 1` carrier layers |
| `(P(C0,10) -> P(C1,0))^7` | eight decimal digit carrier stack, such as a fixed-width positive calculator read |
| `(S0 -> S1.P1)^2` | square-like repeated basis scaling |
| `(S0 -> S1.P1)^3` | cube-like repeated basis scaling |
| `S0 -> S1`, `S1 -> S2`, `S2 -> S3` | repeated segment-to-segment projection; copies/scales by each layer's explicit segment lengths |

The point-chain and segment-chain repeats are both repeated relation shapes,
but they are not the same operation. Repeated point layers preserve count
through carry/reset. Repeated segment-to-segment layers copy or scale by the
explicit layer lengths and are tested against ordinary multiplication by those
lengths.

The current power foothold is an explicit repeat surface:

```text
selected:   S(input,0,1) -> S(Unknown,0,1)
controller: S(repeat,0,n)
transition: Product(output, repeated unit ports)
feedback:   witnessed repeated factors
residue:    product read residue
timeline:   per-repeat partial products
```

Literal controller spans can be read as `x^n` today. Non-literal controllers
remain visible as repeat structure rather than being treated as a hidden
exponent. Polynomial term fanout now uses this same surface for `1, x, x^2,
x^3, ...`, so repeated product is no longer private polynomial machinery.

The count controller may also be derived from another readable surface. Current
power reads accept a visible count location, an equation read that produces a
count, the selected input of a resolution search, or an operation-claim
violation/residual magnitude. These are still structural counts: the source
equation, search sample, or claim remains attached as evidence in the repeat
timeline initial frame alongside the product identity.

An open equation-derived controller can use the power input as its count input,
which gives a structural whole-tick read for shapes like `x^x`.

## Residue, Modulo, And Shift Shapes

| Shape | Read |
|---|---|
| `P(C0,k) -> P(C0,0)`, then read `C0` | modulo/residue read; carrier reports the settled `value % k` layer |
| `(P(C0,k) -> P(C0,0)) -> P(C1,0)` | explicit counter route; carrier tension on `C0` is absorbed through `k:1` into `C1` |
| `P(C0,k) -> P(C1,0)`, then read final carried layer | compact distributed quotient/counter read; executable as carrier-tension emission plus immediate route |
| current `P(C1,n)` plus `P(C0,1) -> P(C1,0)` | additive shift; incoming ticks accumulate from an offset carrier position |
| `S(C,a,b) -> S(C,a+n,b+n)` | segment shift/translation candidate; endpoints move together while segment length is preserved |
| `S0 -> S1`, then read `S1` | duplicate/projection read of one basis through another |

The current executable settling supports modulo through active same-carrier
reset boundaries. The carrier value is not left as an uninterpreted larger
number; it hardens to the local residue and leaves unabsorbed carrier tension.
The same shape is also the first executable level-line cycle: `P(C,12) ->
P(C,0)` can be read as a 12-part circular phase surface without calibrating a
radius. The closure declares one marker as both start and end of the local
cycle; crossed cycles remain tension unless routed into a counter carrier.
General same-carrier segment shift still needs point identity so coincident
points can move without accidentally moving every matching point in the
surface.

Relation identity now provides that foothold. Structural perturbation follows:

```text
target.Perturb(L(C,+n))              translates matching carrier sites
target.Perturb(P(C,a) -> P(C,b))     scales participating sites by b:a
```

The target chooses participants. If the target is a point, only that point
moves. If the target is a segment/relation surface, its participating child
points move. A coincident but unshared point does not move.

## Routing-Like Shapes

| Shape | Read |
|---|---|
| `L(C0,k) -> L(C1,n)` | structural transfer route; moves `min(value,k)` from `C0` into `C1+n`, leaves `max(value-k,0)` on `C0` |
| `L(C0,k) -> receiver surface` | coupled structural transfer; same accounted source updates each carrier location in the receiver surface |
| `L(C0,m) -> L(unknown,0)` | structural open gather; raises `m` to cover visible `C0` tension without draining `C0` |
| `(L(C0,a) -> L(C0,b)) -> L(C1,n)` | structural observation window; reads up to `|b-a|` from `C0` into `C1+n` without draining `C0` |
| `P(C0,k) -> P(counter,0)` | crossing counter |
| `P(C0,k) -> P(C0_inverse,0)` | deferred dual-face/reflection foothold; crossing settles as positive ticks on the opposed carrier |
| `P(C0,k) -> P(C0_inverse,0)` plus `P(C0_inverse,k) -> P(C0,0)` | deferred opposed return loop/ping-pong candidate; lossy or thresholded return can settle without signed storage |
| `P(C,k) -> P(C,0)` | same-oriented reset boundary; modulo locally, tension externally |
| `S(C,a,b).P1 -> S(C,a,b).P0` | segment end-to-start cycle candidate; restart without traversing a return path |
| `S(C,a,b).P1 -> S(C_inverse,0)` | deferred loop/ping-pong candidate; flow traverses an opposed return path before it can come forward again |
| `S0 -> (P(C1,n) -> P(C2,m))` | segment flow into a relation surface; possible coupled fanout/general receiver surface |
| `(source relation) -> (open 0/0 site)` | unresolved gather / attachment potential |

Cycling and looping are intentionally separated here. A cycle restarts at a
related origin or point. A loop has a return journey, usually through an
opposed carrier or explicit return relation, so the return path can carry its
own tension, residue, or blockage.

Same-carrier reset is executable as local accounting. In a line frame,
`P(C,k) -> P(C,0)` hardens the carrier and exposes crossing tension; it does
not imply a return journey through space. In a contour frame, the same shape is
a partitioned cycle closure because the start and end marker are the same local
site under different counts. A loop with a traversable return path still needs
an absorber, counter, opposed carrier, or other relation to account for that
tension.

Lossless point loops are allowed as structures, but settling stops when a full
pass returns to the same carrier positions. The remaining load is visible
participation for a later frame or a different relation surface, not an error.

## Boolean And Filter Footholds

These are not yet structural-settle operations, but the expected shapes are:

| Shape | Read |
|---|---|
| `travel segment -> support segment` | pass where spans overlap |
| `travel segment -> gap in support surface` | block / unpassed tension |
| `support A + support B` | support union/readable coverage |
| `support A opposed support B` | tension/shared opposed participation |

## Possible But Not Fully Listed Yet

The same notation should extend to:

- relation-to-relation transforms where both sides are multi-carrier structures
- segment-to-surface fanout
- point-to-surface carry into several related carrier points
- looped point carry, modulo, and periodic counters
- inverse point and segment relations for subtraction/division/refill
- orthogonal relation surfaces for area, complex rays, and multi-axis support
- filter surfaces that split travel into passed and blocked spans

The guardrail is simple: if a behavior needs a new action name, first try to
express it as points, segments, and relation surfaces whose points move while
their relations are preserved.
