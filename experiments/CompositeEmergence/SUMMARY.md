# Composite emergence: short results

We completed eight fresh runs, with about 1.19 million evaluation decisions
across controls. The experiment took 12 minutes; no paid Jev calls were used.

**The main idea worked.** Two isolated direction sensors each receive no
information about the hidden rule. A Gojo learner discovered their relation,
`A * B`, on all eight runs. It predicted correctly on every held-out trial.
The resulting small publication was useful to another learner, whose boundary
could not access either raw sensor's ancestry.

**That learned service transferred.** A fresh action learner facing the reversed
task reached 91% after 16 training actions and 100% after 32. A flat learner
reached 62% and 87%; a fresh source/action pair stayed around 50% at those
checkpoints. All capable controls eventually reached 100%. Reuse adds 768
earlier source-training trials, so this is a target-learning advantage, not
a total-training-budget win.

**A third learner learned when to investigate, with limits.** With immediate
feedback history it earned 7.05 resource units per step versus 6.00 for probing
every time. But a simple free outcome-memory controller earned more: 7.17.
With delayed history, the learned manager earned 6.12 versus outcome memory's
4.61; it did not reliably beat always probing.

**The biggest failure was missing information.** Frozen managers often failed
to retry after a missing sensor pair, leaving them inactive. Action coverage
fell to 5.2% with immediate history and 0.5% with delayed history. This should
be the next training problem, before adding more complexity.

This supports learned composition and service reuse inside an authored
hierarchy. It does not establish invented hierarchies or subjective
comprehension. All behavior remains explainable through retained native
equations, evidence, utility offers and declared memory.

Verification passed: 2,175 repository tests, six actual native archive replay
checks, 848 resource accounting checks and 123 matching source hashes.

See [REPORT.md](REPORT.md) for complete results and limits,
[the offline dashboard](results/confirmation-01/index.html) for interactive
inspection, and [README.md](README.md) to run it.
