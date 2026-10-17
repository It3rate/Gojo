# Pre-numeric computation assessment

Status: experimental local interpretation law implemented and exercised on
2026-10-15. This extends the initial
[primitive sufficiency experiments](GojoPrimitiveSufficiencyExperiments.md).
The separate reports describe
[structural constraints](GojoStructuralConstraintExperiments.md) and
[qualitative processes](GojoQualitativeProcessExperiments.md).

The subsequent [qualitative execution experiment](GojoQualitativeExecutionExperiment.md)
adds direct categorical detector publications, reversed observer views, local
demand/refinement gates and a derived compact backend. Its initial observation
graph has no numeric samples. The verification counts below describe this
assessment's earlier stage; the follow-up reports the enlarged solution run.

The [structural algebra readiness review](GojoStructuralAlgebraReadiness.md)
consolidates this track with existing signal memory, motif generation,
phototaxis and learning evidence, and proposes the next bounded experiments.

## Finding

The experiment supports a limited but substantive result: existing elements,
relations and placeholders can carry useful computation before quantities,
coordinates or units are introduced. The earlier implementation lacked a local
interpretation law for that computation. Adding a shared selected-slot matcher,
branchwise composition and guarded application was enough for these finite
experiments. No new primitive or numeric settlement rule was needed.

This is evidence for sufficiency in the tested fragment, not proof that the
primitives suffice for every form of pre-numeric reasoning. The implementation
does not establish arithmetic emerging automatically from distinction or imply
that every relation is a constraint.

## Existing behavior and the repair

The earlier tests remain unchanged. They reproduce that `Bind`/`Refine` can
insert a nonmember and that `Advance` does not follow authored state edges.
The process fixture also tests that ordinary substitution alone does not infer
a direction from an influence or dominance relation.

The production addition is
[`StructuralConstraint.cs`](../Patterns/StructuralConstraint.cs), above the
kernel in `Gojo/Patterns`. Its operations are local reads over ordinary connected
structure; no domain name, state enum, numeric sign code, hidden rule registry or
physics dispatcher participates in their implementation.

The added scope grammar is:

```text
scope = seam -> (entry -> (... -> tail))
closed: tail shares seam identity and face
open:   tail is an independent placeholder

request = ((pattern -> selectedSlots) -> initialBindings) -> support
read = (request -> selectedSlots) -> (candidates -> (blocked -> unresolved))
candidate = offeredParticipant -> (instantiatedPattern -> bindingScope)
```

The seam provides closure topology, not a hidden Boolean flag. An exact spine
reader exposes only its entries, preserving arbitrary relations as participants.
An open enumeration never becomes closed merely because a known list ends.

Only explicitly selected slot faces are variables. Other placeholders remain
identity-bearing participants at this read's resolution; their finer content
does not have to be numerically grounded. Concrete relation participants compare
by identity. A relation pattern containing selected slots decomposes in order.
Repeated selected slots preserve participant identity, and repeated relation
nodes preserve graph aliases. Coordinate equality is not shared-site identity.

Each offered match has its own bindings and correspondence. Initial binding
conflicts, unsupported recursive substitutions and malformed structures preserve
unresolved evidence. Observation does not create tension. The runtime's temporary
maps are execution scratch; the resulting graph retains the bindings and offered
participants needed to inspect the read.

`Project` consumes each candidate separately. `Join` passes its frame through
another exposed constraint and retains the originating branch, including blocked
and unresolved continuations. There is no global search or automatic recursive
constraint propagation. `ApplyUnique` applies a target only when one complete
supported route remains; otherwise it returns that target unchanged.

## What the experiments establish

| Concern | Representation | Inference | Enforcement / composition |
| --- | --- | --- | --- |
| Finite domain | Relations distinguish three states and a fourth outside state. | Known membership, missing closed-domain membership, unknown candidates and open enumeration produce different evidence. | An explicit guarded application admits a unique supported refinement and preserves blocked/open targets. |
| Directed transitions | Actual ordered edges are exposed as support. | A -> B can supply B; missing A -> C and reversed B -> A do not become permitted. | Alternatives remain separate, and downstream constraints can filter them through shared participants. |
| Relations between relations | An actual offered edge can be a participant in another relation. | The same matcher reads its higher-order correspondence. | A lookalike reconstructed edge cannot impersonate the actual participant. |
| Qualitative processes | Direction, influences, active scopes, dominance and landmarks use relations/placeholders with no carriers. | Explicit closure and process schemas support direction inference, reversal under dominance and conditional activation. | The same matching/join/projection law composes the results; it does not mutate a physical tank. |
| Mixed measured model | Measurements attach to existing influence identities. | Existing numeric support readers can publish the same direction alternatives. | Structural joins refine the existing model and retain conflicts instead of overwriting a premise. |

Pure structural and qualitative tests inspect the graph and assert that it
contains no carriers or carrier locations. Host iteration schedules the reads
and constructs exposed correspondences; it does not choose a state, dominant
influence or valid transition. Expected numeric signs occur only in independent
test assertions. Numeric sign interpretation in the bridge is explicitly encoded
in ordinary support-decision frames using the existing numeric law.

## Two false reads found and corrected during the experiments

The first dominance construction checked that both endpoints occurred in the
active support but could match both to the same influence. Self-dominance then
ignored the other active influence. A coverage correspondence now requires the
dominance endpoints to account for the two active occurrences. The active scope
keeps the same identity and ordering when dominance reverses. Nominal membership
also prevents a cloned influence with equal shape from impersonating an active
one. These are executable negative controls, not comments asserting validity.

An early matcher also allowed a shared relation pattern node to match two
distinct lookalike nodes. Retaining a per-match relation correspondence now
preserves the requested alias topology. Safety cases additionally protect exact
placeholder faces, shared tick sites, incomplete projections, malformed scopes
and inconsistent candidate frames.

## Qualitative and quantitative refinement

The process track represents forward as `a -> b`, reverse as `b -> a` and stable
as `a -> a` in an authored local orientation. These are neither integer sign
codes nor automatic physical meanings of arbitrary relations. An influence is
an exposed process-to-direction relation. A closed active single-influence scope
supports that influence's direction under the connected local schema. An empty
closed scope supports stability only under an explicit no-spontaneous-change
schema. Opposing active influences do not cancel without magnitude evidence.

The dominance experiment declares a strict net-dominance premise for an
exhaustive pair. The shared reader checks the premise's aliases, coverage and
active membership, then projects the dominant direction. It does not discover
dominance or pretend that pairwise dominance accounts for an unrepresented third
influence. Missing premises preserve the direction alternatives.

The measured bridge introduces additional structure rather than replacing those
alternatives. Existing `FiniteReadSurface.Subtract` and `SupportDecisionSurface`
read a measured difference through positive, negative and zero frames. Their
responses reference the original qualitative direction identities. A structural
join can then narrow three possibilities to one. Coarse ambiguous publication
remains coarse until explicitly refined. An incompatible measured result leaves
an empty supported intersection with blocked evidence; it does not identify
which source is wrong or silently revise either source.

Thus a qualitative-to-quantitative extension is demonstrated at a selected
calibration. Deriving units, number systems or arithmetic laws from these finite
relations remains an exploratory research question.

## Architectural assessment and remaining limits

The tested results support the hypotheses that the primitives are sufficient for
this finite fragment and that structural and numeric computation can use
different local laws over the same substrate. Numeric tension settlement was
too specialized to perform this structural inference by itself; it did not need
to be replaced or made into a universal dispatcher. No experiment demonstrated
a need for another primitive.

The remaining boundaries are explicit:

- Guarding applies at `ApplyUnique`. Raw construction and ordinary substitution
  can still express incompatible structure; the kernel has no universal domain
  ownership lock.
- Reads and requests retain their exposed premises. New support requires a new
  local read; autonomous subscriptions, causal scheduling and qualitative
  settlement are not implemented here.
- One known match in an open enumeration has positive evidence, but the unique
  application reader conservatively keeps its open remainder. Multiple routes
  with the same visible result remain distinct and can prevent unique application.
- Selected slots are one-sided query variables over finite exposed offers. This
  is not unrestricted unification, theorem proving, transitive closure, negation,
  arbitrary cycle solving or automatic discovery of rules.
- The qualitative laws are authored, conditional local models. Unknown direction
  support is retained explicitly; no general law infers all possible physics from
  an arbitrary collection of relations.
- The measured bridge is intentionally conservative on ambiguous support. It
  neither computes the tightest possible qualitative subset nor infers a missing
  calibration, dominance claim or unmeasured magnitude.
- Historical reads retain structural references and sampled correspondences.
  They are not deep immutable archives of every mutable numeric site. A witness
  is inspectable evidence, not an authenticated global truth verdict.

Further experiments should extend these local laws only when another concrete
failure requires it. Useful next questions include explicit rule revision from
conflicting observations, stronger refinement of open support, and deliberate
local subscription/transition timing. Those are documented directions rather
than behaviors silently supplied by these fixtures.

## Verification

The new focused cases are:

- 18 structural constraint cases;
- 13 qualitative process cases;
- 7 structural read-safety cases;
- 5 measured-refinement cases.

Run the new cases and the repository entry point:

```powershell
dotnet test Tests/Gojo.Tests/Gojo.Tests.csproj --filter 'FullyQualifiedName~StructuralConstraint|FullyQualifiedName~QualitativeProcess|FullyQualifiedName~QualitativeNumeric'
dotnet test Gojo.slnx
```

All 43 new focused cases passed. The final `dotnet test Gojo.slnx` run passed
2,430 tests across 11 test projects, with zero failures and zero skipped tests.
