# Relative signals with two memory timescales

This exploratory fixture asks whether comparison-only observations can support
different short and long views of the same signal. No signal-amplitude unit,
absolute origin, minimum, maximum, or arithmetic brightness average is supplied
to the controller. Private fixture readings belong to the external instrument;
the replay can reveal them to the human observer.

## Structure before interpretation

```text
state = G(source, policy, fast bank, slow bank, latest witness)
bank = G(leak law, G(acquisition -> attention carrier))
leak law = S(attention, 0, denominator) -> S(attention, 0, numerator)
bank read = witness(current acquisition, comparison/aggregation law,
                    G(score, signed evidence, total attention, reference count),
                    rounding or unresolved evidence,
                    G(exact comparisons, captured attention coordinates))
```

An acquisition is the existing `SamplingSurface.Sample`: presence is exposed,
amplitude stays open. Each requested comparison names the current and remembered
acquisitions and supplies below, indistinguishable, or above in a local order
frame. These answers do not establish distance or ratios between amplitudes.

One attention tick is a locally defined memory weight. The weighted comparison
read averages signs in this attention basis. Its bounded score is a relative
ordering summary, not a brightness value, change in brightness, or probability.
Zero can arise from opposing comparisons as well as all comparisons being
indistinguishable. Missing observations and empty memories remain open.

## Deliberately finite decay

The fast bank retains 3/4 of each remembered weight per new acquisition step;
the slow bank retains 31/32. Integer projection rounds toward zero and retains
its remainder in the update witness. A new present acquisition starts with 64
attention ticks. References whose weight reaches zero leave active memory.
Each bank has an explicit capacity of 64 references.

The comparison read uses memory before the new acquisition is admitted. Earlier
read witnesses capture the weights actually used, so subsequent decay does not
rewrite their evidence. Absence can age memory without becoming amplitude zero.
Immediate repeated delivery of the latest acquisition is idempotent. Reusing
an older still-retained acquisition exposes an open refusal without replenishing
or decaying memory. An already forgotten identity cannot be deduplicated without
an additional retention law.

This is a designed forgetting law, not a biological model or learned decay
rate. Its clock is an acquisition step, not elapsed physical time. The finite
integer law can reach zero; it does not retain an immortal one-tick tail.

## Interpretation boundaries

The two banks can disagree usefully: a signal may be falling relative to recent
observations while remaining above its longer-lived references. This requires
no fixed brightness origin. A sustained new level can become ordinary as older
references lose their influence.

More positive comparison weight means that the current acquisition exceeds more
of the retained attention. It does not prove a larger physical difference. A
monotonic private re-encoding should preserve this interpretation. Two small
increases need not compensate for one large decrease.

Memory is adaptive operational structure. The fixture does not promise an
eternal archive or reconstruction of forgotten observations. Retained Gojo
witnesses and exported archives are inspectable evidence at their stated scope.
The external world may use ordinary integer calculations; controller memories,
aggregation laws, and response rules remain Gojo structure.

## Trial and results

The companion experiment is `experiments/RelativeSignals`. It includes scripted
changes, short disturbances, long plateaus, missing acquisitions, and matched
private brightness re-encodings, plus a small closed-loop movement check.
Controllers are authored for the trial; this fixture does not implement learning
or evolution. The generated `report.md` supplies measured results and the replay
shows the exact comparisons available for decisions.

The default trial records 21 stories: three private brightness encodings for
each scripted or movement controller. All 14 matched-encoding checks agree on
scores, statuses, weights, queries, controller memory, actions, and positions.
The scripted two-bank readings have opposite signs on 14 steps. After the new
lower plateau starts at tick 160, the fast bank settles first; both banks expose
only indistinguishable comparisons by tick 207.

In the 384-step movement trial, the number of observations within two cells of
the moving peak is 5 for always-right, 285 for previous-only, 328 for fast-only,
and 301 for fast-plus-slow. This authored combined policy therefore does not
improve pursuit over fast-only. Its longer context is useful information, but
the response law still needs work.

The two-bank observer makes 17,256 shared comparison-service requests during
that trial, versus 383 for previous-only. Fast-only and the neutral policy keep
the slow bank for the matched inspection experiment; their reported observer
cost includes it. These are local order queries, not paid model requests.
The prototype retains detailed native witnesses and is not a packed-bit or
minimal-computation implementation.

```powershell
dotnet run --project experiments/RelativeSignals -c Release
python -m http.server 4325 --bind 127.0.0.1 --directory artifacts/relative-signals
```

Open `http://127.0.0.1:4325/`. The source plot is hidden until requested. Each
story has a final native `.gojo` archive; replay steps are inspection projections,
not a promise of exact native reconstruction at every historical step.
