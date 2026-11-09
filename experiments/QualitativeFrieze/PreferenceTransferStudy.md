# Reusing a learned preference in a different situation

## What this experiment does

A small machine learns which of two possible responses is useful. We then
give that preference to a fresh machine facing a different situation. Does it
save a bad first pass? When the old analogy becomes wrong, can the new machine
recover by evaluating more complete passes?

The answer in this supplied apparatus is **both**. Reuse often saves one or two
passes. A reversed control makes the same prior actively harmful: it takes
five passes to settle instead of two from a cold start. Keeping that imported
preference fixed never settles within the six-pass budget.

Open the [interactive comparison](results/preference-transfer/index.html).
Start with **Reversed response**, then compare **Cold start**, **Transferred
preference**, and **Fixed transferred preference**. The three small drawings
share the selected beat. The pass cards explain the local revisions.
The [glossary](PreferenceTransferGlossary.md) connects these reads to the actual
machines, and the [prospective protocol](PreferenceTransferProtocol.md) records
the questions and limits set before running the campaign.

## What is reused

The source has two controls, height and width. Each keeps two motion candidates.
Its six full passes produce a preference through the existing native
keep-or-advance law. We export a preference only when its last retained pass
is complete and useful: it improved the discrepancy or reached the goal with
that selected option. An untried option reached by advancing after failure is
not exported as learned success. An unresolved last evaluation also blocks export.

Later source passes are already at the goal and issue hold responses. The
export rule admits this maintained goal as local utility; these repeated holds
are not independent confirmations of the option's motion prediction. The full
source replay shows its earlier revision, but only two recent evaluations
remain in its active pass memory.

The target receives this connected structure:

```text
source context + captured preferred option + useful pass witness
    -> supplied source-option / target-option correspondence
    -> target preference position
    -> provisional response during the target pass
    -> target-local evaluation and next preference
```

Only the starting preference changes. The target begins with its own actuator
state, empty local pass history, and two unsupported motion candidates per
control. Source utility is not installed as evidence that either target motion
candidate is true. The source's qualifying evidence remains attached as
provenance; it is not counted as one of the target's own evaluations.

The correspondence is supplied for this experiment. It is a mechanically
readable analogy, not an automatically discovered or verified equivalence.
This matters especially when it is wrong.

## The comparison

We train two sources: an inset cap and a chevron cap. Each supplies nine new
situations, tested with three policies: cold start with revision, imported
preference with revision, and imported preference with a fixed keep law.
That gives 54 target runs, each with six passes of six clock opportunities.

The table gives the first jointly settled pass. Both source families produced
the same pass counts. A dash means no joint settlement within the budget.

| Target situation | Cold start | Reuse, then revise | Reuse, keep fixed |
| --- | ---: | ---: | ---: |
| Different starting extents | 2 | 1 | 1 |
| Changed goal | 3 | 1 | 1 |
| Other construction family | 2 | 1 | 1 |
| Reordered candidate bank | 1 | 1 | 1 |
| Reversed response, unchanged analogy | 2 | 5 | — |
| Reversed response, corrected correspondence | 2 | 1 | 1 |
| Capacity too small | — | — | — |
| First pass observation withheld | 3 | 2 | 2 |
| Zero-width start / lost section | 3 | 2 | 2 |

There are 46 final joint matches out of 54 supplied conditions. This is not
learning accuracy or an ornament-quality score. The candidate banks deliberately
contain a useful description, the analogies are authored, and several paired
conditions share the same mechanics. The first-pass damage and recovery are
more informative than the final count.

## A useful analogy does not depend on storage position

The source learns its second option. In the reordered target, that option's
counterpart is first. The correspondence imports the first target option;
copying the source's cursor would select the wrong one. This case reaches the
goal in one pass. The cold start also succeeds because the reordered first
option is already useful: transfer has no measured advantage there.

Inset-to-chevron and chevron-to-inset use a supplied role correspondence across
different motion words and reach counts. They show reuse of the controller's
preference structure, not automatic recognition of a metaphor between domains.

## The bad analogy and its five-pass recovery

The reversed target has a real affine publication:

```text
actuator height 0 .. 12 -> drawing height 12 .. 0
```

Increasing the actuator now decreases the visible height. The uncorrected
analogy preserves the source's old signed-motion assumption. This is not a
hidden change in the controller; the reversal exists in the connected read.

For the inset target, the gap goal is 12:

| Pass | Gap before → after | Endpoint discrepancy before → after | Local response to the evaluation |
| --- | --- | --- | --- |
| 1 | 9 → 3 | 0 → 0 | Height rejects its old option; width retains its option. |
| 2 | 3 → 12 | 0 → 2 | Height reaches its goal; width treats the worsening as a reason to switch. |
| 3 | 12 → 12 | 2 → 8 | Width's new option makes things worse, so it switches back. |
| 4 | 12 → 12 | 8 → 2 | Width improves and retains its option. |
| 5 | 12 → 12 | 2 → 0 | Both goals settle. |
| 6 | 12 → 12 | 0 → 0 | Both remain settled. |

The first pass reaches actuator height 12 with +1 pending tension. The first
negative input of pass two repays that tension; it does not yet change the
visible actuator value. Later steps reduce the actuator and grow the drawing.
Nothing resets between passes.

This exposes a real weakness in the small policy: **a local worsening does not
identify which participant caused it**. Height's useful recovery shifts the
endpoint that width is trying to match. Width's whole-pass rule switches away
from a useful response because it cannot distinguish moving context from its
own bad choice. Recovery still occurs, but with an unnecessary detour.

We deliberately leave that failure visible. The corrected-correspondence case
includes the known reversal explicitly and settles in one pass; it demonstrates
what a better mapping could supply, not that the machine learned that mapping.

## What the other failures tell us

Capacity remains a physical limit. Transferred preference cannot create height
that the passage cannot accept. All three policies fail the joint goal there,
and blocked movement remains visible as pending tension. Revision can change
the preferred option, but cannot solve the missing capacity.

During the withheld first pass, the viewer can still show diagnostic geometry.
The controller cannot use it: no response is selected, no local utility is
invented, and the imported preference survives until a readable pass occurs.

A zero-width start can remove the primary section read. The existing landmark
alternative can remain available without pretending that the missing section
exists. Transfer helps restore the construction, but it does not redefine
absence as zero or establish closure by assertion.

## Memory, encoding and remaining gaps

Each target policy retains at most two own pass evaluations, with an explicit
discard count and open lost-history surface. The report retains a longer
inspection history that the controller does not consult. A captured source
preference does not change when the source later revises itself; target
revision also does not rewrite the source.

The native export includes source provenance and its reachable graph. It is
not yet a tiny compiled transferable packet. This is evidence-preserving
experimental storage, distinct from the size of the preference itself.
Target operational exports span 465,477–683,120 bytes in this run; four full
inspection exports span roughly 2.85–4.12 MB. These are archive sizes, not
minimal machine sizes or timing measurements.

The next useful extension is better local credit assignment: preserve the
possibility that another connected machine moved the target during a pass.
Repeated failure of a particular correspondence could also become evidence
about that analogy, separate from evidence about the target's motion. We do
not yet learn mappings, select among several source memories, invent new
options, rewrite topology, or transfer safely between unrelated domains.

All target motion candidates remain possible throughout this campaign. Useful
action and preference revision proceed without upgrading either explanation
to established truth.

## Verification and reproduction

Four representative full traces agree between native and compiled finite
laws, and their exported graphs decode. All 75 focused checks pass, covering
transfer refusal, historical snapshots, reordered options, bad analogies,
local recovery, restoration and the preceding pass behavior. The full solution
passes 6,808 tests across 18 projects, with no failures or skips. Browser checks
cover all 54 target conditions, synchronized comparisons, keyboard focus and
narrow layouts; all local links in the four HTML pages resolve.

```powershell
dotnet run -c Release --project experiments/QualitativeFrieze -- --preference-transfer
python experiments/QualitativeFrieze/RenderPreferenceTransferNotes.py
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter "FullyQualifiedName~PreferenceTransferTests|FullyQualifiedName~PreferenceTransferStudyTests|FullyQualifiedName~PassPolicyMemoryTests|FullyQualifiedName~PartialDecisionStudyTests"
dotnet test Gojo.slnx
```

Source: [transfer apparatus](Composition/PreferenceTransferStudy.cs),
[pass policy and transfer surfaces](Composition/PassPolicyMemory.cs), and
[shared whole-pass runner](Composition/PartialDecisionStudy.cs).
