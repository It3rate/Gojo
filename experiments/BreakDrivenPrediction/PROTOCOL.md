# Break-driven continuation: first pass

Status: exploratory. Written before running the trials. Earlier experiments
remain unchanged. This is a narrow topology policy interpreted by a host over
Gojo structure, not a new autonomous settlement law.

## Intended structure

```text
stream = group(sensor, arrival-clock, candidates, selected-port -> candidate,
               run-start, observed-onset, proposals, pressure-port, policy,
               latest-event, participating-timing-connections)
candidate = group(signed continuation on sensor basis,
                  native recurrence current-level + continuation,
                  exposed structural parameter port)
proposal = group(source-candidate -> target-candidate,
                 completed-run span, support-carrier,
                 native age-minus-duration, native support-minus-floor)
issue = group(source-clock, issued-at, due-at, input-level,
              selected binding, baseline continuation, exported continuation,
              timing proposal or open slot, shadow continuation or open slot,
              timing comparison, participation comparison)
```

Signed increase/decrease coordinates share one measurement basis; they are not
opposed carrier faces. A completed-run span has an explicitly declared basis:
number of subsequent observations. It does **not** estimate elapsed physical
time or infer a sample rate. Every invocation supplies the next whole sensor
observation. Missing or reordered observations need a richer input contract.

The two supplied primitives initially continue by +1 and -1 sensor ticks.
The first is initially selected. A native signed agreement claim tests each
issued continuation against the *later* observed difference. Direction and
absolute magnitude disagreement are separate evidence; no squared loss or
normalization of uniformly poor candidates decides participation.

On a local break, seek a matching primitive, connect the observed source to
that target, and retain its completed run length. A newly recruited primitive
copies the observed difference through a visible native frame. This selection
cannot receive credit for predicting the break that selected it.

The first run is left-censored and cannot train a duration: its true onset was
not observed. A later complete run proposes a future handoff when the current
run reaches the same observation count. The proposal is issued in shadow
before the next input. At the due observation it earns one support tick only
when it predicts direction better than continuing, and loses one when worse.
Support is bounded at four. Two support ticks are required to add a timing
connection to the exported forecast. Falling below two removes that connection.
Changing the duration clears its old support. These are supplied local policies,
not discovered laws or confidence probabilities.

Out-of-range rewards/draws are declined before becoming physical inputs. Thus
saturation neither banks positive pressure nor accumulates negative debt. The
declined response remains in the returned assessment; the latest retained event
keeps bounded outcome evidence. This is explicit forgetting of support, not
discard of already received tension. A premature observed turn changes the run
duration and clears old support; a late turn first produces a failed prospective
handoff, then proposes the new completed duration.

Only the current level, current onset, candidate continuations, two possible
transition proposals, bounded support, and one latest event remain reachable.
Successive correct values do not append sample history. Issuance and assessment
are separate; only the matching next observation can consume an issue. A repeat
read does not create pressure or count as another observation.

External inspection pressure enters a one-tick reset carrier as real tension.
Its `P(1) -> P(0)` boundary keeps no visible integer value and has no export route.
It can request reconsideration of a stable continuation's magnitude. Handling
that request records a response and consumes that request once. Ordinary read
residue is not injected as new tension. This pass does not discover demand,
adapt resolution, generate a primitive family, or crystallize a reusable word.

## Trials and evidence

Use the same policy without tuning after observing the results:

- regular unequal runs, including an initially decreasing stream;
- a change in cadence;
- irregular run lengths;
- same-direction scale variation;
- injected pressure during a directionally stable run.

Compare exported predictions with continuation of the last observed change,
and a version of the same adaptive structure whose timing connections cannot
participate. Report all arrivals, not only successful or post-warmup points.
Report directional correctness, magnitude error, correctly anticipated turns,
false anticipations, topology additions/removals and remaining pressure. A turn
detector's later reaction must never be called anticipation.

Mechanism checks should cover ten successes then a break, cold/censored timing,
prospective shadow credit, withdrawal after a false turn, pressure during
success, stale/foreign issues, invalid/native unresolved input, bounded reachable
structure, and continuation after native graph transport. Host-generated trial
inputs, counters, CSVs and summaries are measurement apparatus, not learner state.
