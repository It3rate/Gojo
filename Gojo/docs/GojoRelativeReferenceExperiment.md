# Relative references before numeric measurements

Status: implemented experimental ordinal comparison fixture, with the averaging
law below retained as a mathematical direction rather than an implemented
average reader. This is a follow-up to the
[qualitative execution experiment](GojoQualitativeExecutionExperiment.md).

## Question

Can one source expose its position relative to a fixed marker, its change from
the previous sample, and its change across a longer window without supplying
numeric positions or distances? Can those reads disagree without being treated
as conflicting claims about the same comparison?

Yes, provided the reference, observation endpoints, order and resolution remain
part of each claim. A reference can act as a local origin without supplying a
numeric zero or becoming a universal origin.

## Three different questions

| Read | Compared participants | What an upward result says |
| --- | --- | --- |
| Fixed reference | Current sample and fixed marker | Current position is higher than that marker |
| Previous reference | Current sample and previous sample | This observed step is upward |
| Longer baseline | Current sample and an earlier retained sample | Net endpoint change across this window is upward |

The third read does not establish monotonic movement, slope, a running average,
variability or statistical significance. Its window and endpoints say exactly
what it does establish. An averaged-trend supplier may expose a different
contract rather than impersonating this endpoint comparison.

For example, all three can coexist:

```text
current > fixed marker
current < previous sample
current > earlier sample
```

There is no contradiction and no distance estimate. A graph retaining these
relations can support order questions without inventing magnitudes. If both
current and previous samples are merely above a marker, their mutual order
remains unknown until a comparator supplies it.

The comparator must have access to an actual retained reference signal, a
physical comparison mechanism, or another advertised source of comparison
evidence. Keeping only the old word/category “above” does not preserve the old
signal. An opaque reference identity records which retained sample is used;
identity alone is not a signal or a proof of its ordering.

## Native comparison contract

The fixture uses an exact ordinal comparison at one declared resolution:
lower, same resolved position, or higher. “Same resolved position” does not
assert sample identity, exact equality of an unobserved physical scalar, or
membership in the earlier detector's threshold band.

```text
frame = (order -> resolution) -> (domain -> composition-law)

publication = ((source -> (subject -> reference)) -> (frame -> context)) ->
              (support -> evidence)
```

Context retains the selected temporal/window structure. The reference can be
the permanent marker or a particular retained observation. No carrier, location,
numeric sample or metric distance is required in these publications.

The authored composition law states how two comparisons in the same resolved
ordered domain compose:

| A relative to B | B relative to C | A relative to C |
| --- | --- | --- |
| Higher | Higher | Higher |
| Lower | Lower | Lower |
| Same | Any supported state | That state |
| Any supported state | Same | That state |
| Higher | Lower | All three remain possible |
| Lower | Higher | All three remain possible |

The inference requires the actual shared pivot B, source, frame and resolution.
A lookalike reference or another detector resolution cannot silently substitute.
Open support remains open; the native law and both supplied comparisons remain
connected to the result. The derived backend reads the same authored rows.

Different contexts should be kept together as evidence, not automatically
intersected as though they were repeated measurements of one proposition.
Different resolutions need an exposed correspondence before order composition;
neither agreement nor disagreement creates that correspondence by itself.

Compatible different windows can still be useful together: previous > current
and current > earlier yields previous > earlier when source and resolved order
agree. Their separate contexts remain in the witness. In contrast, current <
previous and previous > threshold leaves the current/threshold comparison
undetermined; the sizes of the opposed changes are unavailable.

Composition is a local conditional read, not a consistency verdict over all
claims. It does not discover arbitrary paths or solve cycles, correlate every
alternative, detect every conflicting claim, or strengthen a read from
additional unqueried sample identities. The meaning of an alternative support
is a set of admitted states: native and compiled enumeration orders may differ
while ordered comparison roles and proof identities remain intact.

## Running averages and other trend laws

Signs of successive steps do not determine arbitrary weighted averages,
regression trends, variability or rates. An ordinal sequence also leaves its
distances unspecified. Those interpretations require further structure or an
explicit supplier contract.

There is a useful limited case. For adjacent equal-size, equal-weight sliding
windows with the same positive normalization, the retained terms cancel:

```text
old window: outgoing + shared terms
new window: shared terms + incoming

direction(new mean relative to old mean)
  = direction(incoming relative to outgoing)
```

Thus an incoming/outgoing comparator can answer the direction of that mean
without exposing either magnitude. This depends on the additive cancellation
law, the actual shared occurrences, equal positive weights and common
normalization. The law is additional structure; it does not follow merely from
calling an ordinal read a “trend.” Changing those assumptions removes that
inference.

This checkpoint implements the reference comparisons and their ordinal
composition. It does not add a running-average evaluator. An execution fixture
for the cancellation law should explicitly expose each shared occurrence,
positive common normalization and the selected order-preserving law, rather
than hide averaging semantics in a helper name.

An endpoint trend, all-steps-up trend, majority-of-step-signs summary and
running-mean direction are different questions. A short downward step and a
long upward trend can both be valid. Without further evidence, neither says
which is larger, more reliable, or more useful for a decision.

## Verification

The follow-up adds 17 cases: nine atomic composition rows and eight controls.
The controls cover independent reference/resolution contexts, missing pivot,
foreign source/frame/resolution, threshold band versus ordinal equality,
open/empty support, and useful or unresolved composition across windows. One
control also checks all 64 closed support pairs against native and compiled
execution. Pure reference graphs are checked for absence of numeric carriers
and locations.

```powershell
dotnet test experiments/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj
dotnet test Gojo.slnx
```

All 17 new cases passed, bringing the qualitative experiment project to 182
passing cases. The final `dotnet test Gojo.slnx` run passed 2,612 tests across
12 test projects, with zero failures and zero skipped tests. Focused builds
reported zero warnings and zero errors.
