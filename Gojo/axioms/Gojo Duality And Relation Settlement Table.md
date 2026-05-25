# Gojo Duality And Relation Settlement Table

This note names the current distinction between orientation duality, traversal
reversal, and contextual inversion. It also starts the table we need for the
single carrier/relation settlement algorithm.

## 1. Three Moves, Not One Flag

Gojo should not hide sign, inverse flow, subtraction, division, or reverse
travel in separate flags.

The smaller structural moves are:

```text
Dual(element)      = element-local mirror
Reverse(relation)  = same relation surface traversed in the opposite order
Invert(read/use)   = contextual use of Dual, Reverse, or both
```

`Dual` is part of the `IElement` contract. Each element constructs its own
dual; no outside helper owns that meaning. Carrier duality and location
duality are different grades of the same mirror:

```text
Dual(C)      = C*          opposed-flow / orthogonal carrier face
Dual(L(C,k)) = L(C,-k)     ordinary scalar negation on the same carrier
```

`Reverse` is ordered and should always be constructible for a relation. A
contextual algebraic inverse is not guaranteed to settle cleanly. It may leave
tension.

This keeps `-5` from being a primitive host-language sign:

- value claim: `L(C,-5)` on the same scalar carrier `C`
- operation: a transform slot using a negative carrier location
- trolley movement: reverse traversal by signed magnitude `-5`
- measurement: a carrier-local read whose origin/basis decides whether the
  opposite site is a negative same-carrier location, a backward same-carrier
  segment, a dual carrier face, or unresolved tension

Those are perspectives over the same small structure, not separate operator
taxonomies.

## 2. Element Duality Table

| Element | Dual | Reverse | Notes |
|---|---|---|---|
| `C` | `C*` | none | Opposed flow carrier. This is not the ordinary negative scalar side of `C`; it is a different carrier face/grade. |
| `L(C,k)` | `L(C,-k)` | none | Location dual negates the value on the same carrier. This is the ordinary `-k` scalar read on `C`. |
| `P(C,k) = L(C,k) -> C` | `P(C,-k)` | `C -> L(C,k)` | Point dual negates its carrier location but remains a point on `C`. Point reverse is constructible but not usually a local point. |
| `S(C,a,b) = P(C,a) -> P(C,b)` | `S(C,-a,-b)` | `S(C,b,a)` | Dual negates locations; reverse changes traversal/order. |
| `A -> B` | `Dual(A) -> Dual(B)` | `B -> A` | These commute structurally, but a read through them may settle differently. |

`C*` means the stable dual carrier returned by `C.Dual()`. It is a physical
carrier face, not a negative flag on `C`. A scalar read from `C` does not see
`C*` merely because the carriers are dual. It sees ordinary negative values as
negative signed locations on `C`. The dual face has an inspectable opposed
unit shape that remains compatible with the forward unit shape.

Multiplication is where the carrier face acts. An inverse-flow operand
inverts the carrier face of the product target. Each pair of inverse-flow
operands returns the product to the original face and negates the output value,
giving `1, i, -1, -i` as repeated physical inversion rather than as a symbolic
operator table. Addition does not perform that flow interaction; positive and
dual-face components remain separate unless another relation explicitly
merges them.

The current executable support and traversal reads now use signed locations on
the reader carrier for reverse participation:

```text
support state = L(C,-reverse) -> L(C,forward)
reverse flow  = L(C,-distance)
```

Dual carrier faces remain valid structures, but they require an explicit
relation or later crystallized read before they participate in the scalar
carrier read.

## 3. Site Magnitude Table

For point-like source shapes:

| Source | Meaning |
|---|---|
| `P(C,0)` | origin/reference site; attachment or zero-site read, no material unit by itself |
| `P(C,1)` | one tick boundary; one-for-one carry/accumulation when routed |
| `P(C,k)` | grouped boundary; local accounting emits signed tension in groups of `k` |

For segment-like source shapes:

| Source | Meaning |
|---|---|
| `S(C,a,a)` | zero extent; point-like now, but still two coincident points that may separate later |
| `S(C,0,1)` | unit basis segment |
| `S(C,a,b)` | ordered extent; `a < b` follows native carrier order, `a > b` traverses backward on the same face |

A backward same-carrier segment is not the same thing as a dual carrier. It is
reverse traversal inside one carrier face.

For inspection, segment order defines the local frame:

```text
S(C,a,b) reads L(C,x) as (x - a) / (b - a)
```

For settlement, the same ordered sites are touched by carrier flow. A return
surface such as `P(C,10) -> P(C,0)` wraps positive carrier flow back to `0`
and emits positive tension; it is not reinterpreted as a decrementing carrier
merely because the inspection frame is reversed.

## 4. Current Settlement Table

This table names what Gojo currently executes and what is still only a target.

| Shape | Current settlement |
|---|---|
| bare `C` | input tension is absorbed into `C.Value`; no boundary tension remains |
| `L(C,0) -> L(C,k)` | forward projection/basis surface: no carrier mutation or tension by itself |
| `L(C,k) -> L(C,0)` | reverse local return: crossing `k` wraps local value back toward origin and leaves the crossed amount as positive `Tension` |
| `L(C,k) -> L(C,k)` | same-site local stop: value hardens to `k` and excess remains as positive `Tension` |
| `P(C,k) -> P(C,0)` | reset/carry boundary: value hardens to residue and grouped excess remains as positive `Tension` |
| `P(C,k) -> P(C,k)` | clamp boundary: value hardens to `k` and excess remains as positive `Tension` |
| `P(C0,k) -> P(C1,0)` | same-orientation route: positive source tension routes into `C1` by `k:1` |
| `P(C0,k) -> P(C1,m)` where `m != 0` | signed ratio projection surface: readable as `m:k`, no carrier mutation |
| `P(C0,k) -> P(C1*,0)` | explicit dual-face route: negative source tension can route into `C1*` by `k:1`; this is not an ordinary scalar-negative read |
| explicitly related dual-face values | shared visible value cancels before residue remains |

The projected value for nonzero cross-carrier receiver sites is executable as
a relation-shaped read:

```text
projected participation -> unresolved source residue
```

For example, `P(C0,2) -> P(C1,3)` reads `6` on `C0` as
`L(C1,9) -> L(C0,0)`. Reading the same relation backward gives division.
Non-exact projection keeps unresolved source-basis residue on the original
carrier.
The read does not store the projected value on `C1` or clear `C0`.

The important current correction is that wrap/reset is not caused by the side
being a point instead of a location. It comes from same-carrier relation order.
`0 -> k` is a forward projection surface. `k -> 0` is a return surface.
`k -> k` is a stop surface.

## 5. Target Relation Shape Table

The likely general algorithm should not ask "which operation is this?" It
should inspect the relation shape:

| Left shape | Right shape | Same face | Different same-oriented face | Opposed face |
|---|---|---|---|---|
| point | point | local boundary/reset/clamp depending on right site | origin receiver routes positive tension; nonzero receiver is ratio projection | origin receiver routes negative tension; nonzero receiver is opposed projection |
| point | segment | move or seed receiver endpoint if the segment exposes that point | shift/stretch target surface from a site | opposed shift/refill target surface |
| segment | point | source extent moves receiver point; if receiver point anchors a segment, that segment scales | multiplication-like endpoint movement | division/refill-like endpoint movement, possibly tension |
| segment | segment | preserve local extent relation; do not collapse to point carry | basis projection, copy, ratio scale | inverse basis projection, division/root-like read |
| relation surface | relation surface | internal structure preserved while sites move | coupled fanout/merge by visible sites | opposed fanout/merge with tension if not reversible |

The target behavior should fall out from:

- carrier-local accounting capacity
- signed carrier tension
- relation order
- site duality
- traversal reversal
- preservation of relation structure while points move

If a row needs a named helper forever, the shape is probably not primitive
enough yet.

## 6. Crystallized Reads

A crystallized carrier is a local interface over a settled relation assembly.
It may expose a single usable tick while hiding lower construction from local
control.

Examples:

- signed locations on one carrier crystallize into an ordinary signed-axis read
- positive and dual carrier faces may crystallize into a higher-grade/opposed-axis read
- ones/tens/hundreds crystallize into an integer read
- whole plus partition layers crystallize into a mixed-resolution read
- length and width can crystallize into an area carrier
- distance and time can crystallize into a speed carrier

Crystallization should be one general operation: if a relation assembly has a
stable repeatable unit, it can expose that unit as a new carrier tick. The
lower assembly remains provenance and may still affect settlement, but the
local crystal read does not inspect every lower carrier by hand.
