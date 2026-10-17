# Conditional, temporal and repetition mechanics

Status: implemented experimental mechanics, 2026-10-16. This follows the
[resolution and schedule checkpoint](GojoResolutionAndScheduleExperiment.md)
and [compute comparison](GojoRefinementComputeComparison.md). The additions live
in `experiments/QualitativeExecution`; no kernel primitive or settlement rule
changes. These are finite, inspectable local contracts, rather than a general
autonomous execution engine.

## The small rule set

| Rule | What it permits | What must remain visible |
| --- | --- | --- |
| Answer at sufficient resolution | Stop requesting detail when every admitted case answers the question the same way | Premises, coverage and the unused supplier ports |
| Agree, or explicitly choose | Uncertain premises may imply one definite action; alternatively select one member of a finite action proposal | Original alternatives, chooser, selected member and supplied sampling evidence |
| Permission needs participation | A decision, an admitted occurrence, a source offer and an available passage can produce an effect | Observation, selection, admissibility and realized transfer remain separate |
| Account at the receiving site | Partition incoming participation into accepted, blocked and explicit remainder | Packet identities, source inventory, allocations and shared capacity |
| One drive, one immediate step | Advance along one uniquely determined immediate phase edge | Source, predecessor, fresh occurrence and before/after witness |
| Repeat a phase, not its event | A reusable phase graph may cycle while its occurrence history grows | Replays, stale predecessors and alternative histories |
| Reuse a template, distinguish its instances | Generate and recognize repeated structure with shared landmarks and fresh selected roles | Template identity, occurrence identity, instance, bindings and role constraints |
| Connect what a read requires | Supply ordered succession and port seams explicitly; add calibration when duration or distance matters | Order alone supplies neither contact, equal spacing, equal intervals nor a metric center |

Certainty is the simplest case, not a universal requirement. A controller can
continue one explicitly sampled possibility without copying participation into
every possible future. Selecting an action does **not** prove that an uncertain
observation was that action's preferred input. The observation remains uncertain.

## Question, selection and realization

[`DemandLaw`](../../experiments/QualitativeExecution/DemandLaw.cs) connects one
existing binary `FiniteLaw`, two advertised publication ports and response
routes. It reads their coarse supports before requesting either supplier. A
unique answer requires complete coverage of all admitted pairs, not merely a
singleton union of the rows that happened to exist. Service requests ports in
the application's exposed order, checks same-context publication refinement,
and asks again after each offer. A sufficient first offer skips the second
supplier, including an unevaluated nested application.

This is local question-directed execution. Suppliers return publications;
independent material maintenance and phase drives still require execution.
There is no general graph scheduler, global cost search or new performance
claim. `TryRead` inspects a retained receipt; the prospective answer reader
checks its answer against the connected application and retained current ports.

[`FiniteChoice`](../../experiments/QualitativeExecution/FiniteChoice.cs)
operates on a closed finite **action proposal**. It accepts one supplied member,
connects the chooser and external sampling evidence, and publishes one command
in the proposal's original context. It retains the complete proposal. It has no
hidden random generator, retry loop, weighting model or implicit branch fork.
Open, empty, foreign or malformed proposals cannot manufacture a choice.

The test fixture supplies each possible sampled result explicitly. This tests
membership, provenance and bounded continuation; it does not establish a
statistical distribution. A future sampler can supply this same surface, with
its own justified distribution and bounded execution budget.

[`ConditionalPassage`](../../experiments/QualitativeExecution/ConditionalPassage.cs)
then reads a command publication against a phase state and explicit opening law.
The decision port exposes controller, target, basis and command-domain identities.
The clock port exposes the recurrence and its source. The offer must use the
current phase occurrence and continue the supplied ledger history.

For an unselected uncertain command, **every** admitted atomic case and consumed
admissibility row must agree on one passage. Different commands can therefore
permit the same passage without first resolving which command holds. A row that
permits and another that denies cannot be collapsed into a permission by taking
their nonempty union. Missing rows remain pending; an explicit empty row is a
known denial. After explicit action selection, the selected command is checked
through the same mechanism; the original uncertain proposal remains evidence.

The passage implements a finite packet ledger. Accepted packet identities leave
the source inventory and occupy declared receiving sites; blocked packets remain
at the source. The same physical site listed under two passages still has one
capacity. Ordered site lists and incoming offers determine local intake order.
Pending permission leaves the original state and a concrete offered remainder.
The calculated accounting reader checks:

```text
offered packet identities = accepted + blocked + remainder
```

This is an experimental discrete participation contract, not automatic carrier
transport. It has no release law or automatic retry. A known denied or blocked
attempt consumes its occurrence in that ledger; pending evidence can be refined
and retried at the same occurrence. Reopening a phase does not itself re-offer a
packet. Raw step readers inspect recorded fields and balance; they are not
authentication of arbitrary caller-authored receipts.

## Explicit time and repeated structure

[`PhaseRecurrence`](../../experiments/QualitativeExecution/PhaseRecurrence.cs)
uses a native domain and immediate transition relations. Its reusable graph can
contain `sense -> act -> decay -> sense`. Each admitted drive appends one fresh
event to the supplied history. It never takes transitive closure to skip a phase.
Missing successors, competing distinct successors, unknown phase support and
open templates preserve the prior state with obligations. Multiple justifications
for the same successor do not create multiple phases.

`TryReadAccepted` rechecks the transition premises and exact history extension
before a downstream supplier acts. Reading permission does not consume an
effect globally. Historical before-states can still be used to explore explicit
alternative histories; jointly realized effects require shared admission.

This gives repetition an explicit driver, transition, feedback opportunity and
timeline. The fixture provides the drives. A static cycle, repeated reads and
settlement iterations do not become a clock. Unequal measured intervals can
annotate the same qualitative events without changing their phase order; those
annotations alone do not validate elapsed-time scheduling.

[`RepeatedMotif`](../../experiments/QualitativeExecution/RepeatedMotif.cs)
uses selected placeholder roles, an actual relation pattern, exposed entry/exit
roles, stable bindings and native role-separation obligations. Generation and
recognition use that same exact template surface. Selected local roles are fresh
unless explicitly bound; unselected landmarks remain the same participants.
Distinctness is a declared constraint, so a separately authored loop can be valid.

A strip separately records occurrence order, successor relations and seams
connecting each predecessor exit to the following entry. A copied, equal-looking
successor is not the successor to which the seam was attached. A seam connects
sites without identifying them. Closed missing joins are failures; potentially
valid connections into an open tail remain unresolved. Already known wrong
adjacency or repeated occurrence identity remains a failure even in an open strip.

Reversing the sequence requires new ordered successors and joins. It does not
reverse the local motif or prove a geometric reflection. These are useful
frieze-like sequencing mechanics, not a complete frieze symmetry algebra,
equally spaced translation rule or general glyph recognizer.

## Small compositional experiments

The executable fixtures cover these traces:

| Experiment | Demonstration | Boundary |
| --- | --- | --- |
| Demand to phase gate | One input refinement selects a command, skips the second supplier, then an explicit packet offer enters the receiver | A sufficient answer alone leaves inventory unchanged |
| Uncertain action choice | Two proposed commands remain visible; either supplied choice continues one path through the same gate | No assertion about which uncertain sensor alternative was true |
| Independent maintenance | A settled question skips suppliers while an explicit draw and phase drive still execute | Query pruning is not permission to skip dynamics |
| Real material route | A settled decision is explicitly interpreted as attaching a numeric route; existing source participation then settles into the target | The fixture supplies the interpretation; the kernel does not dispatch qualitative commands |
| Alternating motif phases | `A -> B -> A` reuses two actual template identities and generates three fresh occurrences and local port pairs | Returning to A does not close the occurrence strip back to its beginning |
| Measured occurrence annotation | Unequal clock intervals attach to those same motif events | Qualitative recurrence does not imply a fixed period |

The material-route example uses the existing kernel's point route: two source
quanta settle into two target participations when the route is attached.
Detachment stops new transfer and retains the target state. Later reattachment
can settle retained source material. This is intentionally distinguished from
the finite ledger's explicit re-offer policy, rather than claimed as universal
equivalence between different retention laws.

Important counterexamples include permission mixed with denial, missing law
coverage, replayed events, an unrelated clock branch reusing only a ledger's last
event, shared receiver capacity, foreign publications, copied seams, collapsed
roles, duplicate occurrences and unfinished strip tails. They exercise false
reads and unresolved structure as well as successful results.

## What remains

These helpers interpret declared native structure; they do not discover laws or
derive a physical conditional from a bare arrow. No result requires adding a
domain-specific kernel primitive. The main remaining work is composing larger
networks under explicit execution budgets, with independently supplied clocks,
material retention/release policies and measured correspondences.

Current witnesses keep full histories. History validation and retained provenance
grow with the run; the new mechanics are not compiled into the earlier fast
benchmark loop. Shared effect consumption, history compaction and a general
question-directed circuit compiler need their own contracts and comparisons.
Probabilistic calibration, learning, autonomous scheduling, spatial frieze groups
and proof of stability remain separate experiments.

Run the mechanics and compositional cases with:

```powershell
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj -c Release --filter "FullyQualifiedName~DemandLawTests|FullyQualifiedName~FiniteChoiceTests|FullyQualifiedName~ConditionalPassageTests|FullyQualifiedName~PhaseRecurrenceTests|FullyQualifiedName~RepeatedMotifTests|FullyQualifiedName~TemporalMotifCompositionTests|FullyQualifiedName~ConditionalTemporalCompositionTests"
```

The full qualitative suite also retains the earlier law, resolution, schedule,
measured-supplier and numeric-counterexample checks.

Verification at this checkpoint: 124 new cases; all 474 qualitative cases passed.
Solution-wide Release verification passed all 2,904 cases across 12 test projects,
with no failures or skipped cases:

```powershell
dotnet test Gojo.slnx -c Release --no-restore --verbosity minimal
```
