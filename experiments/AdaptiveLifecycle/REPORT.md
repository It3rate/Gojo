# Continuous adaptation and local lifecycle verification

## Question and interpretation

The previous `CompositeEmergence` experiment exposed a failure after a missing
probe: a manager could keep choosing to reuse an unavailable publication,
leaving its consumer unable to act. This study separates two possible responses:

1. Keep learning, so a manager can acquire evidence about the new situation.
2. Give participation a finite maintenance reserve, so an unsupported participant
   can withdraw and a funded nursery can admit a replacement.

These are different mechanisms. Retirement creates room; it does not establish
that a replacement knows when to retry. This study tests them separately rather
than claiming an improved evolving population.

## What a frozen manager is

“Frozen” described the evaluation protocol, not a permanently immutable body.
The manager's learned context rows and action evidence stopped changing, and its
exploratory choices were disabled. Its operational feature memory, feature age,
previous outcome, decisions and physical observations continued changing.

The failure path is concrete:

```text
paid probe returns an unavailable publication
  -> retained feature becomes unavailable
  -> manager encounters an untrained availability/age/outcome context
  -> frozen read uses an empty temporary row
  -> both alternatives lack evidence, so the tie selects reuse
  -> reuse cannot provide a feature; the consumer holds
  -> neutral outcome and increasing age do not create learned evidence
  -> the untrained context can persist indefinitely
```

The empty row is not retained during frozen evaluation. Learning mode would
retain it, try both alternatives, and admit resource feedback bound to the
selected decision. It is therefore possible to test whether existing learning
already supplies the missing recovery, before inventing another learning law.

Relevant implementation: `../CompositeEmergence/ServiceConsumer.cs` contains
local row discovery, scores, frozen tie behavior, exploration and feedback;
`../CompositeEmergence/FeatureMemory.cs` preserves unavailable features as open
reads; `AdaptationTrial.cs` controls training and paired evaluation.

## What changes structurally

The current manager has fixed advertised ports and a bounded authored decision
grammar. Learning creates native context rows and changes their carrier evidence;
it does not invent a new port, operation or hierarchy. The native graph therefore
changes locally even while the service interface remains stable.

The lower relation service uses a bounded source grammar. In the preceding
experiment it expanded four initial candidates into 22 after inadequate
prediction. Those alternatives are retained; selecting a useful publication is
different from physically pruning all unused candidates. This diagnostic keeps
that source and its task consumer fixed while the manager is the learning
variable.

The swimming-tank experiment already has additional structural mechanisms:

- `../Phototaxis/Circuit.cs`: bounded proposals add, remove, rebind and compose
  processing terms; native utility comparisons select paid trials.
- `../Phototaxis/Creature.cs`: comfort tension triggers intermittent local
  searches, with witnessed rewrites and finite remembered alternatives.
- `../Phototaxis/Bath.cs`: contact and physical conditions permit breeding;
  parental reserves fund children; starvation or predation removes bodies.
- `../Phototaxis/Circuit.cs` and `../Phototaxis/CognitivePilot.cs`: offspring
  components are selected and rebound to independent child ports.

These existing mechanisms are bounded experiment-specific construction laws.
They are not a universal rule that every Gojo element slowly atrophies or that
new useful structures automatically emerge. The primitive-learning experiments
also grow/refine ordinal landmarks and coarsen redundant ones. Their saturated
evidence counts age when new feedback arrives; inactivity alone does not make
those learners disappear.

## Paired adaptation protocol

Source discovery uses 768 trials. The task consumer then trains for 192 actions.
Two managers each receive 2,048 training decisions: one sees clean observations,
the other sees independent per-channel missingness with probability 0.10. Their
training schedules otherwise share the same apparatus-rule, input, goal and time
streams. The apparatus changes its hidden rule with probability 0.04 per step.

Each manager is transported with the connected source and consumer roots so
shared physical axes remain shared. Operational memories are reset before each
arm. Every arm then receives the same 2,048-step missing-observation tape.

| Arm | Manager exposure | Updates during measured stream | Purpose |
|---|---|---|---|
| Clean-trained / frozen | Clean training | None | Reproduce missing-input failure |
| Clean-trained / online | Clean training | 2,048 additional updates | Test recovery through ongoing learning |
| Missing-trained / frozen | Missing-input training | None | Test learning recovery before freezing |
| Missing-trained / online | Missing-input training | 2,048 additional updates | Test continued adaptation after exposure |
| Always probe | Clean-trained manager retained | None | Repeated paid acquisition baseline |
| Retry if unavailable | Clean-trained manager plus authored native condition | None | Explicit repair comparator; not learned retry |

The authored retry comparator publishes a connected conditional rule: probe when
the availability publication is zero; otherwise use the ordinary frozen manager
choice. The comparator tests a simple alternative to learning. Its behavior is
not attributed to an acquired policy.

Correct actions produce +8 resource, incorrect actions -12, holds 0. Each probe
costs 2. Gross physical resource, paid probe debits and net resource are saved
separately. Holds count in all-step accuracy, coverage and return. The evaluator
can inspect the private apparatus rule; learner inputs cannot.

Online versus frozen compares continued access to feedback with its absence.
The online arms have more total manager training information. They do not have
different measured physical tapes, but they have different learning permission.
Source training, consumer training and candidate computation remain unpriced in
these adaptation returns.

## Results

Confirmation 01 used eight fresh seeds, 410 through 417, with the fixed protocol
above. It completed 98,304 measured decisions across 48 paired arm/seed rows in
112.3 seconds. No learning-law changes were made after the pilot. Every online
manager admitted 2,048 additional physical-feedback updates; frozen managers
admitted none. The lower source and task consumer stayed fixed in every arm.

Means across seeds:

| Arm | Correct actions / all steps | Action coverage | Paid probes | Net resource / step | Longest hold run, mean |
|---|---:|---:|---:|---:|---:|
| Clean-trained / frozen | 2.40% | 2.51% | 0.30% | 0.173 | 1,995.1 |
| Clean-trained / online | 93.68% | 97.41% | 12.99% | 6.788 | 3.4 |
| Missing-trained / frozen | 95.12% | 99.02% | 4.83% | 7.045 | 3.2 |
| Missing-trained / online | 93.87% | 97.74% | 11.58% | 6.812 | 3.2 |
| Always probe | 81.52% | 81.52% | 100.00% | 4.522 | 4.2 |
| Retry if unavailable | 93.25% | 96.56% | 18.62% | 6.690 | 3.1 |

“Action coverage” counts non-hold actions, regardless of correctness. It therefore
measures whether a policy escaped inactivity; it does not by itself establish
useful action. Net resource supplies the complementary measure of physical
consequences and probe expenditure. Always probing makes every current missing
measurement invalidate the feature, whereas retaining a previously useful
feature can bridge an outage; this explains why constant probing is not the
coverage ceiling for a memory-equipped controller.

Paired differences in net resource per step, with descriptive 95% Student-t
intervals across the eight seeds:

| Comparison | Mean difference | 95% interval |
|---|---:|---:|
| Clean online minus clean frozen | +6.615 | [6.406, 6.825] |
| Missing-trained frozen minus clean frozen | +6.872 | [6.662, 7.082] |
| Missing-trained online minus missing-trained frozen | -0.232 | [-0.257, -0.207] |
| Clean online minus authored retry | +0.098 | [-0.156, 0.352] |
| Missing-trained frozen minus authored retry | +0.355 | [0.113, 0.597] |

Intervals are over independent seed-level means, not over individual correlated
steps, and are not corrected for multiple comparisons. The comparison against
authored retry does not establish a clear advantage for clean-trained online
learning. Missing-input training followed by freezing did better than that
authored comparator in this protocol.

The central result is that the existing native choice market can acquire retry
without changing its learning law. Continued learning repaired the previously
unseen context, and exposure during training produced a frozen policy that
handled later missing observations. The original long holds nearly disappeared.

There is also a useful negative result: continued exploration cost resource once
the missing-input policy had already been trained. Its measured mean net return
was 0.232 lower than the same trained manager held fixed. The implementation's
periodic alternatives remain active whenever learning is enabled. This supports
testing a local gate that resumes exploration when evidence becomes inadequate
and pauses it when sufficient. It does not establish a generally optimal rule
for deciding when to stop learning.

### A concrete new-context trace

In seed 410, the clean-trained frozen and online managers both made a failed
probe at step 17. They then faced the previously unseen context:

```text
availability = 0, coarse age = 3, previous gross outcome = 0
```

The frozen manager kept skipping at steps 18 onward, retaining no row for that
context. It held for the remaining 2,031 steps. The online manager retained the
new row, tried reuse at steps 18 and 19, then probed at step 20. That successful
publication supported a correct action and a net resource receipt of +6.

Its final native evidence for that context had three reuse observations with
zero resource, versus 25 probe observations with 132 positive and 6 negative
resource units. The resulting scaled probe estimate was 1,290/256, while reuse
remained zero. This is actual bound physical evidence in connected ledgers, not
a host-written instruction that unavailable information should cause retry.

### Recovery metric boundary

The analysis also records time from a failed probe to the next available
publication. Unresolved failures remain in the denominator; a second failed
probe supersedes the previous interval as unresolved. This is a conditional
first-publication measure, not proof of sustained correct action after recovery.
All-step accuracy, return and longest hold runs carry the main conclusions.

## Local maintenance and replacement protocol

The separate deterministic lifecycle fixture supplies one native body and one
exposed participation relation. A local reserve, elapsed-time maintenance
equation, support decision, receipts and withdrawal witness remain connected
Gojo structure.

Elapsed maintenance is charged even without successful feedback or decisions.
If demand exceeds the reserve, the unmet draw remains negative carrier tension.
A depleted participant withdraws its supplied attachment; its body, evidence,
retained route and debt remain inspectable. A later physical credit first pays
the debt. Reattachment requires an explicit viable resume operation.

A bounded nursery admits a supplied native seed only when a slot and its reserve
endowment can be funded. Numeric transport creates a fresh connected graph,
preserving its internal aliases and giving it independent mutable identity.
Retirement releases a slot and accumulates the unpaid maintenance in a native
aggregate before older individual event records are forgotten.

The deterministic demonstration starts with 40 nursery resource units and one
slot. It funds two 16-unit participants over time, admits 8 external credit units,
and leaves 8 nursery units. Across the two participants, requested maintenance is
42, paid maintenance is 40 and retained unpaid demand is 2:

```text
initial funding 40 + external credits 8
  = remaining funding 8 + paid maintenance 40
requested maintenance 42 = paid maintenance 40 + retired shortfall 2
```

Tests additionally check unavailable credits, recent duplicate exclusion,
capacity clipping with retained tension, independent children, blocked births,
resumption, archive continuation, historical coordinate stability and bounded
retained nursery graphs over 100 replacements. Replacing old individual records
does not erase their aggregate shortfall.

This is a local participation law. Withdrawal disables the one supplied
attachment; it is not physical destruction or proof that all body computation
has stopped. Seed reconstruction, material complexity and construction CPU are
unpriced. The paid birth amount is the reserve endowment, not a complete price of
creating an organism. The fixture contains no learned inheritance or selection
of better descendants.

## Evidence and reproducibility

Confirmation records live in `results/confirmation-01/`: settings and source
hashes, per-seed traces and native evidence, actual first-seed execution archives,
lifecycle archives, source/build snapshot, archive replay results and analysis.
JSON is an inspection projection; `.gojo` files preserve connected execution
structure and mutable aliases. They are different artifact contracts.

All six saved adaptive pipeline archives are reloaded during execution and
checked for native signature agreement and the next frozen manager decision.
The lower source and consumer are guarded against learned-parameter drift, and
frozen managers are similarly guarded. Tests exercise connected lifecycle
transport, including resumed and withdrawn states.

The source/build snapshot covers the experiment and its production dependencies.
It is not an archive of the full repository test solution or every historical
result. Pilot artifacts are preserved under their original source manifests;
later lifecycle-accounting and replay improvements do not retroactively change
what those pilots verified.

The solution verification gate passed all 2,200 tests with zero failures or
skips, including 25 tests in this new experiment. Analysis checked 48 rows,
98,304 trace steps, 127 current source hashes and six actual saved archive
replays with no warnings. The source/build snapshot contains 132 files. No paid
Jev calls were made. These are current confirmation checks; the original
two-seed pilot is preserved as exploratory evidence.

## Remaining gates before larger training

| Capability | Verified here or already present | Further evidence needed |
|---|---|---|
| Missing-information recovery | Paired frozen/online/exposure/repair comparison | Longer repeated outages; recovery to sustained correct action |
| Local learning | New context rows, bound physical credit, bounded evidence aging | Several coupled learners adapting simultaneously |
| Maintenance without action | Elapsed reserve draw and local withdrawal | Complete accounting of every active processing and communication route |
| Replacement | Funded reserve, finite slots, fresh connected identity | Construction/material/query/selection costs priced together |
| Graph and memory bounds | Bounded rows/receipts and 100 fixed-seed replacements | Evolving candidate grammar, attached-world and execution-work budgets |
| Structural growth and pruning | Existing bounded Phototaxis proposals and ordinal landmarks | Utility-directed lifetime for parts of the composite in this task |
| Inheritance | Existing independent child rebinding in Phototaxis | Learned service inheritance with preserved calibration and child-local memory |
| Credit across levels | Manager receives actual bound net outcomes | Distinguish joint success from each component's causal contribution |
| Retention versus adaptation | Saturated local counts forget older evidence | Repeated rule changes and returns; useful rare-service preservation |
| Population benefit | Not tested | Learning/maintenance/birth controls on identical tapes and supplied budgets |
| Interface refinement | Fixed advertised ports | New coarse/fine service requests justified by a receiver's unresolved need |
| Transport | Actual connected saved-file replay | Resume a long adapting population with debt, clocks and capacity intact |

Other current boundaries matter for the next study. Physical net resource is
credited to the manager immediately, even though the previous gross outcome is
available as its operational context only on the next step. This is not learning
through a long delayed consequence. Evidence aging happens only when a ledger
receives new feedback and reaches its bound; merely waiting does not decay it.
The lifecycle fixture supplies the elapsed-time law that the manager itself
lacks, but it has not been attached to these six adaptive arms.

The next integrated population test should distinguish ongoing learning alone,
maintenance without birth, funded replacement without child learning, and the
full combination. It should include the original frozen control. All time,
including starvation, empty slots and retraining, must count; survivor-only
accuracy can conceal failure. Rarely used services should be judged by useful
consequences and their maintenance budget rather than frequency alone.

## Relation to other learning work

[Dohare and colleagues' continual-learning study](https://arxiv.org/abs/2306.13812)
tests continued replacement of some less-used neural units to maintain
plasticity. This suggests a selective-renewal hypothesis for Gojo; it does not
validate indiscriminate decay or establish a native Gojo lifetime law.

[Non-stationary bandit research](https://www.jmlr.org/papers/v24/22-0387.html)
evaluates cumulative performance as the best choice changes. Our use of measured
stream utility and explicit recovery failures follows that evaluation concern.
This experiment does not implement that paper's algorithm or inherit its regret
guarantees.
