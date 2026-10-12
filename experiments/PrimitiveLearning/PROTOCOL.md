# Primitive learning study: protocol and working plan

## Questions fixed before the first campaign

1. Can a local opaque-signal service learn useful ordinal distinctions from later consequences, without receiving a magnitude, zero, or unit calibration?
2. Can a small local model learn next-comparison predictions, and preserve conflicting observations as alternatives rather than claiming guaranteed support?
3. Can an empirical query policy learn when additional observation earns its declared cost, including cases where it should stop?
4. Can bounded edits to native condition/response graphs learn a two-port composition that neither port can solve alone?
5. Does the integrated system retain improvements on unseen event tapes with learning disabled? Which connections and learners cause the differences?

These are exploratory local learning laws. Existing sampling, arithmetic, and
settlement do not establish autonomous learning merely by being exercised.

## Plan

- [x] Read canonical instructions and current primitive surfaces; preserve existing work.
- [x] Review primary research and define information boundaries.
- [x] Implement four small native learners with inspectable updates and non-read cases.
- [x] Run isolated smoke trials and correct structural/measurement errors.
- [x] Freeze a campaign manifest after pilot findings, before confirmatory held-out runs.
- [x] Run neutral, frozen, shuffled-feedback, individual, factorial and communication controls.
- [x] Investigate failures and graph complexity; verify native transport and retained behavior.
- [x] Publish machine-readable traces, a readable report, local demonstrations and reproducible commands.

## Native boundary

Canonical meaning lives in connected carriers, ordered relations, sample identities,
condition/response rules, calibration frames, counters, prediction claims, query
choices, candidate parents, and update witnesses. Classes build/read that graph.
Host code supplies an external world, event scheduling, bounded grammar construction,
artifact output and diagnostics. It must not keep the only learned table or policy.
Sensor observations are published through explicit ports; no world operation writes
learner memory or supplies its hidden numeric inputs.

Ordinal sample ranks never become distances. Presence, missing acquisition, equal
comparison and unknown magnitude remain distinct. Prediction counts describe
observed support for a hypothesis; they do not tighten an instrument's guaranteed
support. A mismatch is evidence, not automatically reward, tension, or fault.

## Diagnostic world: two-lane relay

A parcel has an opaque load. A narrow-lane probe produces an independently observed
fit/no-fit consequence. The learner can request load-versus-retained-sample ordering;
the private load coordinate and narrow-lane threshold are unavailable to it.

A flow has a last witnessed direction and a public binary continuity cue. In one
context, subsequent direction has a reproducible relationship to its predecessor;
in the other it is unreliable. Primary training uses95% reversal; a separate
persistent-flow comparison uses95% repetition. The cue identifies context, not its law.
A queried current-direction service is available for a declared resource fee.
The forecast predicts a requested ordinal direction in that context, not light
amplitude. Arriving direction is published after the action even on skipped-query
episodes so prediction accuracy is not selectively labeled.

A fitting parcel routes with the arriving flow; a non-fitting parcel needs the
opposite lane. The actuator publishes its actual resource consequence afterwards.
Balanced fit classes and flow directions make either isolated port inadequate for
the complete routing task. This gives a concrete composition test, not a claim
that its interaction is mathematically unpredictable.

Training query audits shadow only the selected direction response on a
known-fitting parcel on the same exogenous episode. Sparse alternative probes
provide comparison evidence; both counterfactual benefits are never supplied.
Those are explicitly experimental counterfactual trials, with their own costs;
they are not free runtime sensor knowledge. All training work is reported
separately from deployment work. Missing publications remain open.

An explicit native actuator fallback holds when the program's intent is open.
The open intent remains in the witness; paid query fees are still charged.
A whole-route-credit control removes the auxiliary shadow parcel and credits
the query service from actual route returns. This tests a changed feedback
contract and credit assignment, rather than observation availability alone.

## Pilot changes recorded before confirmation

Pilot01 was invalidated by an equation operand-tree bug: query/program scores
were open. Pilot02 fixed that bug and showed learned fit boundaries and routing,
but also that a fixed prior-direction fallback made prediction redundant in a
persistent flow. Primary training therefore uses predictable reversal; persistent
flow remains a separately labeled comparison. Training grows from640 to1536
episodes; test length grows from256 to768. The query-price shift5 crosses the
decision boundary; price3 would only increase expense. Learned model slots are
checked unchanged before/after frozen tests. No campaign feedback is used to
redesign the learning law.

Pilot03 exposed another composition issue: lifetime query estimates pooled early
bad forecasts with later improved forecasts. Pilot04 tests a128-episode A/P
warmup before Q/S updates. The primary campaign keeps this staged schedule;
`simultaneous-query` retains Q-from-zero under the same total episode budget.
This changes exposure/scheduling, not learner laws. No claim is made that128
establishes convergence. Query evidence is saved with its actual counts,
positive/negative benefit, fees and native mean-score read.

The first longer runs initially reused pilot seed identities0/1. They were
stopped and retained as incomplete development data. Confirmation uses distinct
seed identities100..107; the persistent comparison uses100..103. `--seed-start`
makes this split explicit in the manifest. The default campaign starts at100;
pilots start at0. The final reported studies are `campaign-fresh` and
`persistent-fresh`; development directories are excluded from confirmation.

## Design and controls

- TRAIN updates native structures; held-out TEST freezes them. Validation/pilot
  observations used to change the design are never called untouched test evidence.
- Independent tapes are pre-generated, so divergent choices cannot consume random
  values in different orders. Record seeds, lengths, conditions, fees and hashes.
- Isolated primitives: learned, initial frozen, neutral/fixed reference, contradictory
  or shuffled feedback, and informative versus unpredictable contexts.
- Integration: full 2^4 on/off factorial, plus fixed initialization, neutral actuator,
  no paid query, always query, missing/miswired communication and trained-frozen replay.
- Test new seeds and separate shifts in continuity, query price, threshold, unavailable
  sensors and strictly increasing private signal encodings.
- Report coverage and error separately for abstaining predictors. An unknown answer
  is not a correct prediction. Report every comparison, paid query, audit, candidate
  trial, accepted edit, retained landmark, support alternative and native graph size.
- Evaluate action success and physical resource consequence separately from local
  prediction agreement. Report per-seed paired differences, not only pooled totals.
- Interpret factorial interactions as measured complementarity under the specified
  task. Information erased by an ablation can impose a ceiling; that is not evidence
  that the remaining learning algorithm is intrinsically incapable.

## Research foundations and limits

- Li, Walsh & Littman (2006), [state abstraction](https://thomasjwalsh.net/pub/aima06Towards.pdf): relevant distinctions and different preservation criteria. Immediate action agreement is weaker than future model equivalence.
- Abel, Hershkowitz & Littman (2016), [approximate abstraction](https://proceedings.mlr.press/v48/abel16.html): task compression with bounded loss under specific assumptions.
- Hay, Russell, Tolpin & Shimony (2012), [metareasoning](https://proceedings.mlr.press/r10/hay12a.html): selecting computation by decision improvement and cost. Applying it to sensor query services here is an adaptation.
- Ellis et al. (2021), [DreamCoder](https://people.csail.mit.edu/asolar/papers/EllisWNSMHCST21.pdf): bounded program search and reusable libraries. This experiment does not implement its Bayesian/neural wake-sleep learner.
- Dietterich (2000), [MAXQ](https://arxiv.org/abs/cs/9905014): hierarchical learning and local/global optimality distinctions. Its scalar-MDP convergence result does not transfer automatically to these structures.
- Ross, Gordon & Bagnell (2011), [DAgger](https://proceedings.mlr.press/v15/ross11a.html): learner actions change encountered observations; an episodic relay result needs later closed-loop transfer testing.

TypeSafe guidance is applied to the optional teacher boundary: Jev may make bounded
judgments over explicit candidates and evidence. These four initial learners use
no paid AI inference. AI authors the grammar and experimental setup; it does not
choose the measured learned result after seeing held-out truth.
