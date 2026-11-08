# Two claims, one construction

## What this does, in plain language

The previous experiment could make the spacing between two paths correct while
leaving their endpoints apart. This one connects two small controls to the same
shape. One changes the lower path's height to get the requested gap. The other
changes its width to bring the far endpoints together.

Neither control is told the finished width or given a target outline. Each
receives only a coarse correction from its own observation. The interesting
part is that changing height can also move an endpoint: one control changes the
problem that the other control sees.

Both controls settled the six ordinary starting cases. They also exposed three
distinct limits: insufficient capacity, a gap between available increments,
and a change which removes the section needed by the gap reader. The last two
are particularly useful directions for further work.

Open the [recorded visual experiment](results/coupled-claims/index.html).
The [machine glossary](CoupledClaimGlossary.md) explains what is actually
connected. The [prospective protocol](CoupledClaimProtocol.md) records the
predictions and conditions specified before the run.

## Start with this example

Choose **Opposed chevrons → Both requests can be met → Both, on every beat →
Case 1**. The upper cap stays fixed. The orange lower cap starts too shallow
and ends too far to the left.

1. The gap claim asks the height machine to grow.
2. The endpoint claim asks the width machine to grow.
3. Both requests were sampled before either response. Together they move the
   endpoint past its counterpart.
4. The next endpoint observation asks for less width, while height continues
   to grow until the gap is correct.
5. Height then holds; width finishes compensating. Both relations agree.

This small overshoot is not hidden. It shows how two locally reasonable
responses interact through their common construction.

| Recorded beat | Lower width | Lower height | Gap, requested 6 | Lower endpoint minus upper, horizontally | Height request | Width request |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 8 | 1 | 3 | −2 | Not sampled | Not sampled |
| 1 | 9 | 2 | 4 | +1 | +1 | +1 |
| 2 | 8 | 3 | 5 | +2 | +1 | −1 |
| 3 | 7 | 4 | 6 | +3 | +1 | −1 |
| 4 | 6 | 4 | 6 | +2 | 0 | −1 |
| 5 | 5 | 4 | 6 | +1 | 0 | −1 |
| 6 | 4 | 4 | 6 | 0 | 0 | −1 |
| 7 | 4 | 4 | 6 | 0 | 0 | 0 |

Values describe this bounded integral realization; they are not lengths fixed
by the abstract shape. The requests in each row came from the preceding
state. The width 4 was reached by local corrections, not calculated elsewhere
and passed in as a target.

## Exactly what connects to what

```text
fixed upper path section ─┐
lower path section ───────┼─ gap claim ─ coarse support ─ pending gate ─ height actuator
desired-gap value port ──┘                                              │
                                                                        │
fixed upper endpoint ────┐                                               │
lower endpoint ──────────┴─ endpoint claim ─ support ─ pending gate ─ width actuator
                                                                        │
both exposed extent ports ─ captured samples ─ width/height role bindings ┘
                                              │
                                     next native construction read
```

| Encoded part | Actual connection | Consequence |
| --- | --- | --- |
| Upper/lower paths | Ordered direction uses connected to width and height roles | Repetition of a role affects every place it is used |
| Lower reflection | Vertical-frame action applied to the native sections | Changes the vertical read without reversing traversal order |
| Gap observation | Two native horizontal sections, selected downward normal, requested-gap port | Reads separation only where nonzero parallel sections overlap longitudinally |
| Gap response | Height extent port connected to a positive motion word | Increasing height moves the selected lower section downward |
| Endpoint observation | Two `site -> position word` ports and a selected rightward axis | Reduces their difference and retains both along-axis and transverse words |
| Endpoint response | Width extent port connected to a rightward positive motion word | Increasing width moves the lower endpoint right |
| Public comparison | Support from decrease/equal/increase domain to one receiver | No source geometry or requested final extent crosses this surface |
| Pending gate | Comparison support connected to the receiver's advertised tension port | Stops repeated same-direction debt from accumulating indefinitely |
| Actuator | Finite law, effect outlet, unit correspondence and local clamp | Introduces signed change; accepts it or holds the remainder |
| Publication history | Actual live port connected to a distinct captured sample | The drawing remains traceable to the exposed value it observed |

The endpoint law has six rows: three discrepancy signs and two permitted
motion polarities. The input support can be `001`, `010`, `100`, open `000`,
or a set such as `111`. These are alternatives, not a magnitude and not three
simultaneous flows. A `010` output from the pending gate means **hold**; it may
be holding an unsatisfied request. It does not prove measured equality.

Observed geometric discrepancy is claim residue. It is not automatically
material tension. Only an admitted actuator response introduces a signed
adjustment, and each actuator accounts independently for:

```text
introduced adjustment = visible extent change + change in retained tension
```

Neither actuator reads the other's private state. The claims observe their
advertised path surfaces, and the next construction read observes both extent
ports. This is a small example of coordination through shared consequences.

## Why three constructions matter

| Construction | Effect of lower height | Effect of lower width | What the experiment demonstrates |
| --- | --- | --- | --- |
| Cardinal cap | Moves the horizontal section; endpoint horizontal position is unchanged | Moves endpoint horizontally | The two controls can largely operate independently |
| Chevron cap | Moves section vertically and endpoint horizontally | Moves endpoint horizontally | Width compensates for height's side effect |
| Inset cap | Moves selected section by three reaches per height step and also moves endpoint | Moves endpoint horizontally | Repeated role uses change both response magnitude and reachable gaps |

These consequences are present in the native phase construction. The local
response correspondences are **supplied**, not inferred: one positive height
step moves the observed section downward by one shared reach for cardinal and
chevron, and by three for the inset. The controls receive only direction
support, so the stronger inset effect can step over a goal.

An after-the-fact algebraic check explains the reachable chevron example:
upper endpoint is at `8 + 2×2 = 12`; gap 6 requires lower height 4; the lower
endpoint is `width + 2×4`, so width 4 would match. This equation is an audit of
the result. It is not an input or a solver used by the control loop.

## Recorded results

The campaign ran 176 conditions for 36 beats each, preserving the initial
state as well: 6,512 recorded frames. Three native/compiled comparisons agreed
over their complete traces, and all three exported native archives decoded.

The following counts require the requested gap, full endpoint coordinate
agreement, and zero held tension in both extent actuators at the final beat.
They are not an aesthetic score or a percentage of learned success.

| Situation | Gap only | Endpoint only | Both every beat | Both alternating |
| --- | ---: | ---: | ---: | ---: |
| Ordinary reachable request | 1/6 | 0/6 | 6/6 | 6/6 |
| Height capacity too small | 0/6 | 0/6 | 0/6 | 0/6 |
| Width capacity too small | 0/6 | 0/6 | 0/6 | 0/6 |
| Gap observation withheld for eight beats | 1/6 | 0/6 | 6/6 | 6/6 |
| Endpoint observation withheld for eight beats | 1/6 | 0/6 | 6/6 | 6/6 |
| Endpoint observation ambiguous for eight beats | 1/6 | 0/6 | 6/6 | 6/6 |
| Larger gap / compatibility probe | 1/6 | 0/6 | 2/6 | 2/6 |
| Inset gap between available steps | 0/2 | 0/2 | 0/2 | 0/2 |

In total, 57 of 176 runs ended in joint agreement without held extent tension.
The gap-only success is the cardinal starting case whose endpoint already
agrees. The two successes in the larger-gap probe are also cardinal cases:
their height change does not move the endpoint horizontally. Keeping that
contrast is more useful than making every difficult condition fail.

The diagnostics can observe agreement while a control's message is withheld.
That establishes a property of the recorded geometry, not that the receiver
has learned or certified that property. A first observed match also does not
by itself prove convergence beyond the recorded horizon.

## The failures teach different things

**Capacity leaves debt.** With the chevron width limited to 3, the gap reaches
6 but the lower endpoint stays one reach short. One positive width adjustment
remains pending. The gate holds repeated requests; it does not relabel the
endpoint as equal. Limiting height instead leaves a positive height debt and
an unmet gap even after width brings the endpoints together.

**Resolution causes continuing correction.** For the inset's requested gap
10, the simultaneous controls alternate late in the run between gap 9 with
width/height 5/2 and gap 12 with width/height 6/3. The endpoints alternate between
one reach short and two reaches too far. Neither extent carries debt: every
step is legal, but the goal lies between available readings. This is different
from a blocked actuator and would need a different remedy.

**A response can remove an observation's premise.** In the large-gap chevron
case, width reaches zero. Its selected horizontal section is then zero length,
so this gap reader cannot measure the requested relation. The recorded gap
becomes open, not zero. The endpoint is still four reaches too far right and
the width actuator holds a negative adjustment it cannot absorb. The inset
case differs: repeated height uses keep its selected section nonzero even
when the width role is zero. Its gap can remain measurable at 24 while its
endpoint still fails. The role value alone cannot determine observability;
the connected construction matters.

## Saved structure now supports the next read

The preceding pass exported and decoded geometric graphs but still needed the
original in-memory frame wrapper for an executable geometric read. This pass
adds exact frame rebinds. They inspect the decoded two-axis grammar and retain
its actual directions, opposite pairs, cancellation/exchange laws and shared
reach. They do not invent fresh axes and substitute them for the old ones.

A composed test saves both claims, both actuator archives, their gates and
the observed geometry in one graph. After decoding, both claims read the same
differences and drive the decoded actuators. The original world is unchanged.
Malformed or incomplete frame structures are rejected. Reboxed locations
retain shared tick identity; merely equal coordinates do not become aliases.

This is executable continuation of retained observations and local controls.
It is not yet an archive of the whole experiment scheduler or a persistent
material geometry engine.

## Size and verification

The measured live native apparatus occupies 15,217–15,741 bytes in the current
numeric graph format. This includes the supplied frame, constructions, claims,
laws, gates and ports. It is not a count of primitive participation bits.

The selected full witness archives are much larger: 1,344,219 bytes for the
reachable chevron, 2,323,183 for the off-grid inset, and 1,350,525 for the
incompatible chevron. They retain repeated native observations and histories.
Those byte counts should not be confused with the size of a minimal reusable
machine. History compaction, lifetime policy and low-level optimization remain
future work; this campaign is not a speed benchmark.

All 84 focused checks passed. They covered endpoint polarity and transverse residue, open
and ambiguous observations, frame restoration and identity, both-control
settlement, per-actuator accounting, capacity, resolution failure, and matching
native/compiled traces. Browser verification exercised all 176 selections with
no script errors, checked mobile overflow and keyboard focus, and verified
local document links. `dotnet test Gojo.slnx` passed all 18 test projects,
including 3,189 tests in the qualitative suite.

## What remains supplied, and what comes next

We supply the phase constructions, choice of section/endpoint, response
correspondences, goals, capacities, native finite-law rows and pulse schedule.
These mechanisms do not choose where to attach themselves, learn their own
motion effects, or judge ornament quality. Each path is a new read of the
role-bound construction, rather than a persistent physical path deforming.
The renderer receives recorded points and contributes no control decisions.

The most concrete next experiments are:

1. **Retain a bounded alternative when a read fails.** A zero section can leave
   an endpoint relation or a broader envelope available. Switching observation
   must be an explicit connected choice, with the failed claim still visible.
2. **Refine only where the legal steps cannot settle.** The inset gap conflict
   is a small place to test a finer rung or tolerance read, preserving the
   original coarse evidence rather than quietly changing the goal.
3. **Observe response correspondence.** Compare a published extent adjustment
   with the resulting port motion, then retain competing local effect claims.
   That would begin replacing the supplied motion word with learned evidence.

These are tests of local mechanisms. They would move us closer to a useful
interpolation system while making the necessary guidance and remaining
uncertainty inspectable.

## Reproduce

```powershell
dotnet run -c Release --project experiments/QualitativeFrieze -- --coupled-claims
python experiments/QualitativeFrieze/RenderCoupledClaimNotes.py
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter "FullyQualifiedName~CoupledClaimStudyTests|FullyQualifiedName~GeometricEndpointClaimTests|FullyQualifiedName~QualitativeFrameRestoreTests"
dotnet test Gojo.slnx
```

Source: [study runner](Composition/CoupledClaimStudy.cs),
[endpoint claim](Composition/GeometricEndpointClaim.cs),
[existing gap claim](Composition/GeometricGapClaim.cs),
[path construction](Composition/CoupledShapeTemplate.cs),
[frame reader](../QualitativeExecution/QualitativeMotionFrame.cs).
