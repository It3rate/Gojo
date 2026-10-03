# Break-driven prediction

This is a light exploration of stable local continuations that recruit help at
breaks. It leaves the earlier predictor experiments and the production kernel
unchanged. The [protocol](PROTOCOL.md) was committed before the first trials;
the first [measurements](results/first-run.md) and [per-arrival reads](results/traces.csv)
are retained without tuning the policy to those outcomes.

Run from the repository root:

```powershell
dotnet test experiments/BreakDrivenPrediction.Tests/BreakDrivenPrediction.Tests.csproj
dotnet run --project experiments/BreakDrivenPrediction/BreakDrivenPrediction.csproj
```

## What exists

The learner is a connected Gojo relation, not an instance storing a semantic
model. Two authored continuations carry native recurrences, exposed parameter
sites and signed sensor coordinates. The current selection persists through
correct directional predictions. A local failure opens inspection of the
available candidates, records the observed directed transition and recruits a
matching continuation for subsequent predictions.

Complete runs propose timing relations. Their native equations compare the
current run age with the observed duration, and their support with a declared
participation floor. A shadow forecast is frozen before its outcome arrives.
Only a prospective improvement over continuing earns support. Two supported
opportunities allow an actual timing connection into the exported forecast;
insufficient support removes that connection. A changed duration cannot inherit
the old duration's support. The first run and runs interrupted by uncovered zero
changes cannot supply a complete duration.

Successful stretches retain the current level and a small evolving trace, not
an appended list of samples. Native reads use temporary bound copies and do not
write their intermediate answers into the retained graph. One latest event is
kept; returned assessments may be saved by an external observer but are not fed
back as an accumulating learner history. Independent numeric checkpoint replay
preserves the live aliases, pending forecast and later behavior.

Direction and magnitude are different reads. A supplied pressure request can
make a directionally satisfied continuation inspect its magnitude. That request
is real incoming tension on an explicit reset boundary, consumed once when
handled. Ordinary comparison residue is not automatically injected as tension.

## First observations

| Input | With timing | Same structure without timing | What happened |
| --- | --- | --- | --- |
| Regular unequal zigzag | 105/112 directional predictions | 85/112 | Anticipated 20 of 27 turns; no false turns. |
| Changed cadence | 98/112 | 85/112 | One false anticipation; old timing connections were removed and replacements earned support. |
| Irregular runs after a regular prefix | 82/107 | 78/107 | Both timing connections were eventually withdrawn. The irregular suffix alone was 49/67 versus 47/67. |
| Variable scale, regular cadence | 105/112 | 85/112 | Direction stayed useful while magnitude error increased. |

The mirrored stream also reached 105/112; its startup differs because the
authored initial selection is positive. Zero changes remained uncovered by the
two supplied primitives rather than being assigned a fictitious flat model.

The stable-input pressure pair is particularly relevant to the proposed design.
Both runs got all 34 directions right. Without a request, the structure continued
exporting its coarse magnitude. With a request at observation 17, it inspected
the current difference and its next prediction changed from one tick to seven.
The already issued prediction for observation 17 stayed one. Mean magnitude
error fell from 4.53 to 1.53 across the full run, including the prefix before the
request. Thus directional success can coexist with a reason for another local
space to request more detail.

On regular input, the reachable learner stayed at 85 carriers and 873 relations
at observations 32, 64 and 96. Structural tests also check bounded retention out
to observation 256. This is bounded memory, not yet efficient computation: native
read construction and returned evidence still allocate temporary structure.
Candidate inspection happens only at local breaks; active prediction, grading
and existing timing checks still run for each arrival.

## Limits and next useful step

This supports the mechanics of the proposed direction, not general discovery.
The two primitives, last-completed-run timing family, support policy and
recruitment scheduler are authored. Gojo carries and calculates their structure;
host code applies the explicitly declared local policy. No least-squares fit or
hidden predictor object owns the result, but the policy does not yet emerge from
ordinary settlement alone.

Timing here means observation count. It does not infer physical time from
irregular arrivals. A wrong duration is replaced directly, so jitter can repeatedly
erase useful partial evidence. The experiment retains no alternative duration
range, resolution ladder or independently supplied leading cue. Bounded support
is not statistical confidence, and local accuracy is not global usefulness.

The next useful step is to expose the remaining transition mismatch to another
small producer through an actual input connection. Compare a genuinely leading
cue, a delayed reversal detector and an irrelevant cue. Let a consumer's useful
improvement pay for that connection. That would test whether independent spaces
can supply the missing timing, before adding general spawning, resolution
adaptation or crystallization into a reusable generating structure.

Verification: the existing solution tests pass with
`dotnet test Gojo.slnx --no-restore` (1,732 tests). The new project's 23 tests check forecast ordering,
pure reads, support saturation and withdrawal, pressure, uncovered input,
incompatible/unrepresentable reads, bounded retention, and checkpoint continuation.

## Independent event supplier extension

The subsequent [event supplier pass](EVENTS.md) connects a separate native
reversal reader through an opaque public port. It tests prospective consumer
utility, redundancy, delayed and unrelated signals, delivery gaps and demand
withdrawal. Run it with `--events`; the original default trials remain intact.
The expanded experiment suite has 42 passing tests.
