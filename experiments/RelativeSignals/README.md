# Relative signals

A comparison-only experiment using native Gojo fast/slow attention banks and
hand-designed rule surfaces. No amplitude calibration, absolute light threshold,
metric average, learning, or Jev call is used.

## Run

From the repository root:

```powershell
dotnet run --project experiments/RelativeSignals -c Release -- --output artifacts/relative-signals
```

Open `artifacts/relative-signals/index.html` directly in a browser. The replay
embeds its results and needs no server or account. `--script-only` omits the
optional pursuit trials. The default run is bounded: nine 224-acquisition
scripted trials and twelve 384-tick pursuit trials. It writes results, a report,
an embedded replay, and one final native `.gojo` archive per trial.

## What is measured

The supplier publishes a `SamplingSurface.Sample`: known presence and an open
magnitude slot. A private fixture dictionary answers requested comparisons with
`SamplingSurface.Comparison(candidate, anchor, orderAxis, sign)`. The learner
never receives the dictionary, private values, world position, or peak location.
Private plotting coordinates are hidden by default in the replay.

Each native bank is a retained decay calibration plus acquisition→attention
carrier relations. Initial attention is 64; fast attention decays by 3/4 and
slow by 31/32, with at most 64 references. Reads compare the current event to
each reference before decay/admission. The native equation combines comparison
signs with their attention weights and normalizes the result to −16…16:

```text
score = truncate(16 × signed comparison attention / compared attention)
```

This is relative ordinal evidence. It is **not a brightness measurement or a
metric mean**. A zero score is marked `all_same` only when every native query
reports equality. Balanced signs and rounded small votes remain
`balanced_or_rounded`; missing/warmup/unavailable reads remain `open`.
Post-update reference counts/weights are displayed separately from the weights
used by the current read. Original comparison witnesses and normalization
residue remain connected to the read.

Exact reuse of the latest event is idempotent: no new query, decay, or admission.
Reuse of another still-retained event produces open reads without adding
independent evidence. Once an identity has been forgotten, this finite memory
cannot recognize it without an additional history contract. Distinct acquisitions
with the same value are still distinct events. Missing signals are not zero
brightness and are never compared.

## Native controller

```text
controller = G(frames, roles, G(heading memory, refractory memory),
               policy rules, motion rules, heading rules, refractory rules,
               refractory leak calibration)
condition  = G(evidence role, closed native frame)
rule       = G(G(conditions), response coordinate, affine calibration)
```

A local reader follows native rule order and publishes the selected response
with its admitted evidence and projection residue. The score-focus witness
retains the whole original bank read as provenance. The reusable rules, response
coordinates, frames, and calibration define the controller; display labels and
records do not own its semantics.

Previous-only and fast-only policies reverse on fast score ≤−4, then continue
otherwise. The combined policy distinguishes a supported decline in both
timescales, a short dip against a longer baseline, a fast rise, and weak fast
evidence with strong slow evidence. Its ±4 thresholds concern comparison votes,
never absolute brightness.

Intent enters native motion rules. Continue/reverse projects the remembered
heading through forward/reversed calibrations; heading updates use native
responses and `Set` delta settlement. Reversal resets a native refractory
carrier to 4. Its retained 3/4 projection reads 4→3→2→1→0; reversal rules require
zero, so persistent negative evidence cannot flip direction every tick.
Missing/open inputs follow an explicit native continue rule and remain visible.

Previous-only retains one reference per bank. Fast-only consumes only the fast
score **but still maintains the same two-bank observer**. Its cost includes slow
memory, and identical candidate/reference pairs are shared between bank reads.
This is an honest controller comparison, not a claim that the fast-only
implementation minimizes work.

## Trial and measured results

The script contains plateaus, slow rises/falls, abrupt shifts, single-tick
pulses, missing events, reused identities, and a final lower plateau from ticks
160–223. In the two-bank runs:

- Fast and slow scores have opposite signs on 14 ticks. For example, tick 99
  reads +3/−11, and tick 126 reads −3/+2.
- Both native reads first report all-same on the final plateau at tick 207,
  47 ticks after its start. The old normal is no longer represented in their
  active comparison evidence.
- The scripted previous-only run uses 220 comparison requests; fast-only and
  combined use 9,289 each, about 41.5 per acquisition, because both banks remain
  active. Shared queries prevent duplicate requests for the same pair.

The optional 31-cell hill moves every 96 ticks. The world provides one opaque
center sample per tick; it does not provide a direction sensor or write memory.
All trials start at cell 15 heading right. External observer metrics were:

| Controller | Mean distance from peak | Ticks within two cells | Queries |
|---|---:|---:|---:|
| Always right | 13.88 | 5 | 17,256 |
| Previous-only | 2.73 | 285 | 383 |
| Fast-only | 2.37 | 328 | 17,256 |
| Fast + slow | 3.10 | 301 | 17,256 |

The fast-only policy performs better than the combined policy in this particular
world. Extra temporal information does not automatically improve a hand-designed
response. The neutral policy also maintains the observer for common instrumentation;
it is not an optimized no-observation baseline. The two-bank pursuit cost is
about 44.9 order-service requests per tick. No tuning followed these results.

## Invariance and limits

Every scenario is repeated under private day `8x+1000`, dusk `x+20`, and nonlinear
`x²+20` transforms, all strictly increasing on the nonnegative fixture domain.
All 14 full-signature comparisons agree: native scores/statuses, retained
reference ages/weights, query counts, controller intents/actions, heading,
refractory state, and pursuit positions. Private display values and runtime
timings are excluded. This verifies these deterministic fixtures; it does not
establish robustness to noise or arbitrary comparison services.

Final archives are decoded to verify opaque samples, reference weights, ordinal
reads, native rule/calibration correspondence, and both memory cells. Only final
graphs are retained, rather than an exact native archive of every historical
operational state; replay steps are display projections. The native observer
maintains finite active memory but allocates witnesses and temporary native
equations. Reported query counts are service requests, not CPU instructions or
paid API calls. External proximity outcomes are never internal rewards.
