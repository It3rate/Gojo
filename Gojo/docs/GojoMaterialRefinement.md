# Explicit material refinement

Status: implemented bounded local transfer, September 6, 2026.

`ResolutionOf(u,n)` defines a coarse tick containing `n` source ticks.
`SubdivisionOf(u,n)` defines a finer tick, `n` of which correspond to one
source tick. Both require positive factors, preserve opposed flow and unit
descriptors, and check stored factor overflow. Exact divisible inverse scales
reduce; general rational unit normalization is not implied.

The finer unit's algebra is ordinary relation structure:

```text
S(Unknown,0,n) -> source unit surface
```

It differs from the coarse `source -> S(Unknown,0,n)` and from a quotient's
denominator factor group. Its native value still counts whole ticks.

## Selected transfer

[`RefinementTransfer`](../Structures/RefinementTransfer.cs) reads:

```text
request = group(calibration, L(source,requested))
calibration = S(source,0,A) -> S(target,0,B)
```

The two carriers must be concrete, distinct, on the same flow face, and have
exactly equal native promises after the indicated resolution:
`ResolutionOf(source.UnitShape,A) == ResolutionOf(target.UnitShape,B)`.
No unit-name convention, affine offset, or implicit dimensional conversion
supplies missing correspondence.

With `g = gcd(A,B)`, one material packet contains `A/g` source ticks and
`B/g` target ticks. The operation accepts the largest whole number of packets
supported by the nonnegative request and source value. In source ticks:

```text
request = accepted + unavailable + remainder
accepted * B = delivered * A
```

Unavailable is request beyond available source value; remainder is available
participation below one representable packet. Neither is invented on the
receiver. Unused source value remains in place.

A witness connects the request, calibration, negative source/positive target
deltas, both residual components and before/after local state. The direct
deltas are range-checked before either is injected. Both enter before ordinary
local settlement. A receiver clamp may retain part of the injection as tension;
the transfer witness does not falsely equate delivered ticks with final value.

This is not an atomic transaction over arbitrary connected worlds. A later
attached operation can still fail its own range check after the direct
injections. Capture and apply while the local world is not concurrently changing.

## Borrow

`TryBorrow(calibration,out witness)` observes negative receiver tension and
requests the smallest whole calibration packets that can pay it. Available
source value limits acceptance. For a ten-part fine basis:

```text
coarse = 2; fine value = 0; fine tension = -3
borrow one coarse tick
coarse = 1; fine value = 7; fine tension = 0
```

With no coarse value the unpaid fine draw remains tension. This is an explicit
selected passage, not automatic borrowing across every nearby ladder.
Arbitrary signed/offset material conversion and general fractional tension
storage remain outside this operation.

## Syntax and evidence

The [authored fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/material-refinement.gojo)
uses `subdivision(unit,parts)`, ordinary spans and groups. Parsing builds the
request. A caller explicitly applies it or asks for borrowing; parsing never
drains a carrier.

[Kernel tests](../../Gojo.Tests/Core/RefinementTransferTests.cs) reconstruct
coarse/fine participation over radices 2 through 7, available values, fine
remainders, paid draws and exhaustion. They also cover receiver clamps, open
requests, incompatible calibration and pre-transfer overflow refusal.
[Syntax tests](../../Gojo.Encoding.Tests/GojoSyntaxMaterialRefinementTests.cs)
run the same refine/coarsen/borrow sequence both directly and after numeric
restoration. The legacy radix ladder now also checks exact composite and scale
overflow instead of silently saturating.
