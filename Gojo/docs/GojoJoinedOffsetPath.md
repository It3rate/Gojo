# Joined straight offsets

Status: September 2026. A selected open-path construction extending the
[measured offsets](GojoMeasuredOffsetRefinement.md) and the existing two-edge
miter. This is a local geometry read, with no material movement or region claim.

## The missing check between corners

Consider an open U-shaped route `A(0,0) -> B(8,0) -> C(8,6) -> D(0,6)`.
An inward offset of two constructs three straight offsets. Their two local
miters are `J1(6,2)` and `J2(6,4)`. The resulting path is
`(0,2) -> J1 -> J2 -> (0,4)`, with one actual shared site at each join.

At distance four, both corner intersections still fall inside their respective
source supports. But `J1(4,4)` lies beyond `J2(4,2)` on the middle edge's
ordered support. Joining these blindly would reverse that edge. A successful
corner read therefore does not establish a successful path read.

At distance three the middle span collapses. This first path contract requires
strictly positive remaining traversal in the source direction. It retains that
collapse as an unresolved topology change; it neither merges two sites based
on coordinate equality nor silently deletes the middle edge.

## Connected syntax

The input uses the existing preparation request for each edge, in source order,
and one explicit parameter-frame/support relation per edge:

```gojo
root group(
    group(group($offset_ab, $scale_ab),
          group($offset_bc, $scale_bc),
          group($offset_cd, $scale_cd)),
    group($whole_ab -> $allowed_ab,
          $whole_bc -> $allowed_bc,
          $whole_cd -> $allowed_cd));
```

Each `$scale` is a native whole `span(carrier(),0,1)`. Each `$offset` retains its
actual edge, selected metric, orthogonal outlet, calibrated reach and side.
Adjacent requests must share the actual source corner, metric and reach
calibration before the existing miter read accepts their connection. Equal
coordinates or numerically equal calibrations do not supply that identity.
Separate scale wholes do not assert independent physical uncertainties.

The retained offset and corner witnesses can contain repeated calculations
from the same inputs. These are construction evidence, not independent
observations that strengthen confidence by being counted more than once.

`SegmentOffset.TryConstructPath` reads this exact two-field surface. Its host
selects this procedure; neither variable names nor a new syntax opcode select
the law. The
[syntax fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/joined-offset-path.gojo)
supplies inputs only, never calculated joints. Its first root is the common
request with an open calibration; the next three roots bind that slot to
distances two, three and four. The driver applies the chosen binding before
requesting a read.

## Construction and result

1. Capture the request, preserving its shared sites and numeric inputs.
2. Retain each edge's normal, square-constrained scale and endpoint recipes
   through the existing preparation. Also attempt its exact rational offset.
3. Apply the existing bounded miter to each neighbouring pair. Use the actual
   result joint at both sides of that connection. Failed corners supply one
   shared open joint slot and retain their local evidence.
4. Build candidate spans between these sites. For each span, read its start and
   end through its own source parameter frame and allowed support. Admit it
   only if both endpoints are known, allowed, and ordered strictly forwards.
5. Return the ordered admitted edges or open edge slots, with per-edge
   witnesses, the candidate topology and source/result correspondences.

The outer witness follows the usual five fields. Its result is an ordered
group of edges or placeholders. Its remainder contains the unresolved edge
witnesses. Its provenance is:

```text
group(preparation witnesses,
      exact offset witnesses,
      corner witnesses,
      edge admission witnesses,
      source-edge -> result-edge correspondences)
```

Each admission witness selects the corresponding preparation request. Its
read surface contains the exact offset witness, incident corner witnesses and
parameter support. Its provenance contains the candidate edge, parameter
traversal, and preparation witness. A traversal connects the start/end samples
to the selected frame/support; a missing sample stays open. Successful edge
results retain the actual candidate edge. Rejected results retain an open edge
slot, with the candidate and traversal in their remainder. Thus rejection does
not discard a corner that was individually valid, or the partial path's shared
topology. Inspect the witness rather than treating every candidate as admitted.

Success means every span was admitted under this selected contract. Failure
can still contain valid adjacent spans. A malformed outer request returns the
ordinary unresolved witness. An explicitly closed source seam is outside this
open-path contract and remains unresolved.

Parameter frames may be reversed or use different tick counts. Forward means
forward relative to each source frame, not increasing raw ticks. The two open
endpoints retain their displaced source positions; those positions must also
be permitted by their supports. No cap is inferred.

## Partial geometry and limits

If a diagonal needs an irrational scale, its normal, square equation and shared
endpoint recipes survive in the preparation witness. An unresolved corner's
one joint slot is shared by both candidate spans. The current path reader does
not intersect interval endpoint boxes or choose their midpoints. Bounded joined
coordinates need a further correlated construction law; individual offset
resolution reads remain available from the retained preparations.

Parallel/collinear corners, turnbacks, caps, bevels, round joins, curved offsets,
closed contours, non-neighbour intersections and region topology are still
separate work. Local admission does not certify a simple or manufacturable
outline. The selected metric and reach are exact premises here, not discovered
physical truth or a probability distribution.

## Walkthrough and verification

The [joined-offset walkthrough](examples/joined-offsets.html) shows the three
source edges, their offsets, calculated joints, accepted path, collapsed middle
span and reversed middle span. Its syntax and displayed numeric traces are
checked against the executable fixture. The browser presents those traces;
it does not execute Gojo. The back view mirrors horizontally without changing
the mathematical structure.

The [30 dedicated tests](../../Gojo.Encoding.Tests/GojoSyntaxJoinedOffsetTests.cs)
cover shared joint identity, per-edge support and order, open corners,
irrational preparations, reversed frames, sheared/reflected charts, longer
paths, integers beyond floating-point precision, failed premises, numeric
restoration and captured calibration history. Run:

```powershell
dotnet test Gojo.Encoding.Tests/Gojo.Encoding.Tests.csproj --filter 'FullyQualifiedName~GojoSyntaxJoinedOffsetTests'
dotnet test Gojo.slnx
```

At this checkpoint the full solution passes 1,617 tests: 782 kernel, 659
encoding, 155 CAD and 21 gear-ladder cases. Browser checks cover all six stages,
both view directions, desktop/phone layouts, distance selection, syntax,
local links and direct step URLs. Representative layouts were inspected.
