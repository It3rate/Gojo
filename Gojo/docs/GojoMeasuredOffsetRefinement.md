# Measured offsets with an open scale

This follow-up to the [metric construction fixtures](GojoMetricConstructionFixtures.md)
separates a known normal direction, the equation fixing its magnitude, and an
observation at a selected resolution. A missing rational endpoint need not erase
the construction that determines it.

Status: implemented selected reads. See the
[walkthrough](examples/measured-offsets.html) and
[authored syntax](../../Gojo.Encoding.Tests/Fixtures/Syntax/measured-offset-refinement.gojo).

## Structural contract

Preparation consumes `group(offset request, scale whole)`. The offset request
is the existing edge, metric, edge outlet/reach calibration and relative side
guide. The new `S(scale,0,1)` is a parameter frame: one whole scales the
calculated, unnormalized normal once. It is not a physical length unit.

The prepared result retains four connected parts:

```text
group(
  normal direction,
  group(scale whole, shared scale slot -> same scale slot,
        normal squared length -> requested squared distance),
  group(first endpoint interpolation, second endpoint interpolation),
  first output slot -> second output slot
)
```

The selected normalization law is `normal² × scale² = distance²`, with a
nonnegative scale. The repeated operand is the same slot. Its sign is not a
second side decision: the original guide already selected the normal's side.
Both endpoint requests are existing affine interpolations from an endpoint to
that endpoint plus the unnormalized normal. They share the scale slot, unit
frame, nonnegative support and coordinate basis. Neither endpoint is authored
as the answer. The result's output slots are the actual targets of these
requests.

The support is constructed as `S(scale,0,ceil(sqrt(distance²/normal²)))` using
exact integer comparisons. This is a mathematical enclosure in the chosen
parameter frame, not an assumed physical tolerance. It makes the branch choice
visible: the existing interpolation reader refuses a negative scale. In the
diagonal example the allowed coarse support is `[0,2]`, and the attached
square equation further restricts which value in that support can satisfy the
requested distance.

A resolution request then connects the prepared witness to a subdivision:

```gojo
root group(?prepared,
    span($fine_scale, 0, 100) -> ?prepared_whole);
```

The correspondence must target the actual prepared whole, and the fine carrier
must have its subdivision unit promise. It does not mean “about two units” or
authorize a tolerance around the source measurement.

`SegmentOffset.TryPrepare` returns the preparation witness. Its captured scale
whole is bound into `?prepared_whole`; the original live whole and an independently
equal-looking copy are not that same captured relation. The observation request
is handled by `SegmentOffset.TryObserveAtResolution`.

Before consuming a transported preparation, the reader checks its normal,
square equation, actual source endpoints, calculated interpolation probes,
shared scale/grade and output-slot correspondence against its captured input.
A witness is not treated as a global correctness certificate.

## A diagonal example

The edge `(0,0) -> (1,1)` has a selected normal `(-1,1)` in the identity metric.
Its squared length is two. An offset distance of two therefore requires:

```text
2 × scale² = 4
scale >= 0
```

The magnitude is exactly constrained even though the rational reader cannot
return √2 as one rational scalar. At tenths of the scale whole, its fine count
lies between 14 and 15. At hundredths it lies between 141 and 142. These are
guaranteed closed enclosures, obtained by exact integer square comparisons.
The equation remains attached and is stronger than either enclosure.

Both output endpoints depend on one shared bounded sample. Their coordinate
expressions use the existing `BoundedQuantity` affine structures and readers.
The endpoint enclosures are marginal views of that connected construction:
they are not permission to choose unrelated points anywhere in two boxes.
Subtracting the two endpoint expressions cancels the shared movement and
returns the exact original displacement `(1,1)` at every selected resolution.

A fine read does not falsify an earlier coarse read. Each retains the captured
prepared construction and its scale correspondence. Different read samples
are linked to the same original scale slot; the quantity readers do not yet
automatically reconcile separately produced resolution samples by ancestry.

The observation result is `group(shared sample, endpoint expression edge,
endpoint bound-witness edge)`. Its provenance connects the prepared scale slot
to this sample through the actual subdivision, retains the exact square
equation, and connects each original output slot to its bounded point expression.
These are contextual read correspondences; they do not replace the original
exact constraint with independent intervals or change material state.

## Boundaries

This remains a selected flat metric construction with complete source geometry,
distance calibration and side evidence. Missing metric or side discovery,
uncertain physical distances, curve normals and material movement are separate.
The exact offset reader and bounded miter retain their previous contracts.
Preparation and bounded observation are explicit additional stages.

The open normalization does not become an approximate exact solution. No
midpoint is silently chosen. If the scale lies exactly on the selected grid,
the sample is concrete and its enclosure collapses. Otherwise the sample stays
open, even when a finer or exact read could retain more information.

No new parser tokens or geometry opcodes are introduced. The host selects
preparation and observation, and binds their actual surfaces into subsequent
requests. A future compiler must preserve those selected laws and the
correspondence between the exact constraint, coarse sample and output slots.

## Verification

The [focused tests](../../Gojo.Encoding.Tests/GojoSyntaxMeasuredOffsetTests.cs)
cover shared-scale cancellation, native and numeric-restored requests, exact
grid sites, zero distance, a skew chart, opposite guides, rejected negative
domains and forged preparations, calibration history and storage limits.
The walkthrough's syntax and all displayed bounds are checked against the
executed fixture.

```powershell
dotnet test Gojo.Encoding.Tests/Gojo.Encoding.Tests.csproj --filter 'FullyQualifiedName~GojoSyntaxMeasuredOffsetTests'
dotnet test Gojo.slnx
```

At this checkpoint all 1,586 solution tests pass: 782 kernel, 628 encoding,
155 CAD and 21 gear-ladder cases. There are 28 dedicated measured-offset
refinement cases. Browser checks cover the five walkthrough stages, four scale
subdivisions, horizontal back-view reflection, syntax panels, links and direct
step navigation on desktop and phone; representative layouts were inspected.
