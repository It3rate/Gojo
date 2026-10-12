# Continuous adaptation and local lifecycle verification

Status: complete. Implementation, two-seed pilot, frozen eight-seed confirmation,
independent review and the full repository gate are finished. Confirmation
recorded 98,304 evaluation decisions in 112.3 seconds. All 2,200 repository tests
passed. See [REPORT.md](REPORT.md) and [SUMMARY.md](SUMMARY.md). The settings below
record the protocol frozen before confirmation.

## Questions

1. Does the observed missing-input trap require a changed learning law, or does
   the existing native choice market recover when its learning remains enabled?
2. Does training with unavailable publications yield a useful frozen policy?
3. Can elapsed maintenance consume native reserves even without decisions or
   successful feedback, withdraw starved participation, and preserve its debt?
4. Can a bounded, funded nursery create genuinely new native participants while
   leaving the failed participant and its explanation inspectable?

## Structure before experiment

The adaptive comparison reuses the existing native source, consumer, manager
and feature-memory surfaces. External tapes publish observations and resource
consequences. The source and task consumer remain fixed in this diagnostic;
manager learning is the controlled difference. Initial exploration and count
aging are the existing local learning law, not an added command to retry.

Lifecycle is a separate local structure:

```text
participant = G(native service root, reserve, maintenance basis,
                participation relation, elapsed draw witness,
                last resource receipt, withdrawal witness)
nursery = G(funding reserve, shared capacity, seed structure,
            birth transfer witness, admitted participants)
```

Elapsed maintenance is an authored physical cost. Positive resource can fund a
reserve; unmet draws remain negative tension. Withdrawal changes attachment
participation, preserving the relation and its previously delivered state.
Birth uses declared native seed structure and an explicitly funded reserve.
Seed reconstruction work is unpriced in this diagnostic; it does not discover
a construction grammar or establish complete construction-cost accounting.

## Execution and controls

- Preserve all `CompositeEmergence` confirmation artifacts and source files.
- Compare baseline-trained frozen, baseline-trained online, and missing-trained
  frozen managers on paired tapes. Account for trials with no action rather
  than dropping them from accuracy or return.
- Separate held-out frozen evaluation from online adaptation. Report extra
  observations and updates available to the latter.
- Retain missing-event/retry traces and native learned evidence.
- Test dormancy, ongoing useful funding, underfunded birth, independent identity,
  bounded nursery capacity, withdrawal and transport behavior.
- Killing a bad participant is not evidence that a replacement learned a better
  policy. Verify adaptation and lifecycle separately before combining them in
  a larger population experiment.
- Run focused tests, the repository test gate, then report results and remaining
  gates for growth, pruning, composite credit and bigger environments.

No paid Jev calls are required.

## Pilot and frozen confirmation protocol

Pilot 01 used seeds 400 and 401, with the same native source, consumer and
manager laws as `CompositeEmergence`. Online manager action coverage was 96.9%;
the clean-trained frozen comparison was 2.2%. Missing-trained frozen coverage
was 98.8%. These are exploratory results, retained in their original directory.
No learning-law changes were made based on them.

Before confirmation, native lifecycle accounting was strengthened: retirement
coordinates are captured, aggregate unpaid maintenance is retained even as
older individual receipts are forgotten, and numeric overflow remains open
without partial admission. Actual saved native pipeline replay was added.
Pilot manifests retain the historical source hashes; drift is disclosed.

Confirmation uses fresh seeds 410..417. Fixed settings: source discovery 768,
consumer training 192, manager training 2,048 for each of clean/missing exposure,
then 2,048 decisions per arm on one paired missing-input tape. Rule-switch
probability 0.04, per-channel missing probability 0.10, probe price 2. All six
arms and all holds count. The online arms each admit 2,048 extra manager updates
during the measured stream; other learned parameters remain unchanged.

Primary measures are all-step net resource, action coverage and paid query rate.
Longest holds expose dormancy. Conditional first-publication recovery retains
unresolved/superseded failures; it is not sustained useful recovery. Compare
paired seed means using descriptive 95% Student-t intervals, without correction
for multiple comparisons. No learning-law or settings changes are permitted
within confirmation 01. Any later repair requires a new run directory.

The deterministic lifecycle checks are separate from the adaptive trial.
They test reserve funding, elapsed maintenance, local route withdrawal, bounded
active slots, new identities, aggregate shortfall and transport. They do not
estimate a population-fitness advantage from decay/replacement.
