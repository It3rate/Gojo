# Independent event-producing structure

The first pass supports a narrow but useful behavior: an independent structure
can earn a connection by improving another structure's next prediction, then
lose that connection when its information becomes late, redundant or unused.
It does not yet discover the event detector or its possible interpretation.

The [protocol](EVENTS_PROTOCOL.md) was committed before the trials. The
[complete measurements](results/events/first-run.md) and
[per-arrival native verdicts](results/events/traces.csv) include startup,
negative controls and unsuccessful connections. No support parameters were
changed after seeing the results.

## What is connected

`EventSource` builds a private sensor, a changing previous-difference trace,
native equations and a public event port. Two adjacent nonzero differences
with opposite signs produce an event. This is an authored strict reversal
read; a flat interval between directions is not covered by that predicate.
Initial insufficient input stays open. A complete zero means this selected
predicate did not fire, not that the input has no other interesting structure.

The port carries an event basis, a source-coordinate axis and a publication
sequence axis. Each packet contains a copied reading, a directed support span
and exposed residue. The consumer cannot traverse the packet to the producer's
private sensor, trace or calculation. Meaning is in those connected structures
and their interaction; no transmitted string or operation label supplies it.

`EventCoupling` connects that port through explicit coordinate correspondences
to a continuation. Its first prediction failure opens a proposed route. Later
events can test the native conditional equation `delta * (1 - 2*event)`.
The candidate reverses the primitive delta; credit compares it against the
consumer's existing exported prediction, including its learned timing help.
An event cannot earn credit for the observation that recruited the route.

The candidate and existing prediction freeze before the next input. Native
claims judge direction separately from magnitude. A useful directional
improvement earns two support ticks and each examination costs one. Support
stays in [0,4]; two ticks permit a participation relation. A tied or wrong
candidate loses one. These are supplied local utility rules, not statistical
confidence or a learned economy. Rejected saturation responses remain in the
latest evidence without accumulating hidden support or debt.

The host constructs the structures, supplies observations, schedules reads and
applies that declared policy. Gojo equations and claims calculate the readings
and comparison evidence; the host does not fit a separate numeric predictor.
This is still an authored local interpreter, not autonomous growth through
settlement alone. All retained learner state is connected Gojo structure.

## What the fixed trials showed

Each irregular condition uses three 240-arrival streams with the same targets
and policy. The regular control is one deterministic stream repeated under
three seed labels; it is not three independent replications.

| Supplier condition | Existing / combined correct directions | Connection behavior |
| --- | --- | --- |
| Leads irregular turns | 600/720 / 710/720 | Earned and retained in all three streams. |
| Detects turns too late | 600/720 / 600/720 | No participation earned. |
| Unrelated process | 600/720 / 599/720 | One accidental connection caused one extra error, then withdrew. |
| Leading becomes late | 600/720 / 642/720 | Withdrew in every stream, after three harmful forecasts each. |
| Delivery gaps | 600/720 / 679/720 | Useful connection survived; missing deliveries stayed unavailable. |
| Demand removed, then restored | 600/720 / 664/720 | Export stopped immediately, connection withdrew, then earned participation again. |
| Regular, already learnable cadence | 233/240 / 237/240 | Helped early, then withdrew when internal timing made it redundant. |

The regular control is particularly informative. The supplier's candidate was
useful six times and redundant 52 times. Its connection appeared after arrival
12 and withdrew after arrival 41. An accurate supplier therefore need not keep
its connection when the consumer already has the answer.

The changing-relationship condition exposes a cost of stability. Previously
useful support allowed three harmful event-assisted forecasts before withdrawal,
at arrivals 138, 140 and 146 following the change at 120. The unrelated condition
also shows that two useful coincidences can recruit a false connection. Neither
behavior was tuned away. Better contextual evidence or different support
resolution may eventually improve that tradeoff.

Demand was removed for arrivals 81-160. Event-assisted exports stopped at 81;
the retained edges withdrew at 91, 98 and 100. After demand returned, connections
earned participation again at 173, 170 and 171. Missing packets incur neither
reward nor this event cost, so permanent silence does not itself remove a route.

The source retained 74 carriers and 661 relations at each sampled prefix. The
consumer varied with its current evidence, from 110-148 carriers and 1,120-1,465
relations. Longer-prefix tests also check bounded retention. These are not
efficient minimal structures: temporary read allocation and repeated examination
of unhelpful proposed routes remain substantial. Withdrawing a participation
edge does not yet prune the proposal or its shadow evaluation.

## Compatibility with structural observers

The consumer depends on the public port and its coordinate correspondences,
not on how the producer encounters the event. An observer traversing a structure
could expose an event through this same boundary. Its direction, source span
and availability would need an appropriate correspondence; the consumer does
not require a trolley or inspect a traversal mechanism.

This fixture calibrates one source arrival to one receiver arrival, with zero
offset. It does not yet learn variable delay, physical sampling time or a
perspective-dependent mapping. The support span identifies the observations
used, not an exact inflection instant. Receipt checks preserve that directed
span and reject support mapped beyond present availability. Missing, stale,
future and foreign packets remain unresolved, while the base forecast can run.

## Boundaries and next step

Both the reversal detector and its reversal response are supplied. The leading
environment deliberately has an exact one-step coupling, so its large gain
establishes functioning communication and prospective credit, not general
discovery. The negative controls test whether that connection can fail and
withdraw. Broad primitive competition, response discovery, scale refinement,
multiple-consumer utility and reusable generating structures are still absent.

A useful next small step is to make examination itself conditional: withdraw
repeated shadow work for an unhelpful or unneeded route, then let new local
tension reopen a bounded probe. Measure both missed opportunities and saved
work. After that, compare several response structures behind the same port,
instead of supplying only reversal. Neither requires implementing a trolley.

The experimental APIs assume roots from their builders, unmodified issued
forecasts, sequential admissions and free private input carriers. They do not
authenticate arbitrary forged issue graphs or provide transactional rollback
for arbitrary external settlement feedback. Native checkpoint replay preserves
the public-port aliases, private separation and pending forecasts.

## Run and verification

```powershell
dotnet test experiments/BreakDrivenPrediction.Tests/BreakDrivenPrediction.Tests.csproj --no-restore
dotnet run --project experiments/BreakDrivenPrediction/BreakDrivenPrediction.csproj -- --events
dotnet test Gojo.slnx --no-restore
```

The 42 experiment tests pass, including 19 new cases for public/private
separation, open versus zero, prospective recruitment, frozen forecasts,
duplicate delivery, demand withdrawal, incompatible coordinates, future support,
exhausted publication sequences, bounded retention and checkpoint continuation.
The existing solution's 1,732 tests pass. Production core code was unchanged.
