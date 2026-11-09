# Gojo Workspace

Status: **documented direction**. There is no `Gojo.Workspace` code yet.
Narrow implemented relatives are listed under [Existing footholds](#existing-footholds).
The November 2026 selection decision is in canonical prompt §7.1. The
exploratory bit-level form is in §5.14 of the
[encoding consolidation note](../docs/GojoEncodingConsolidation-2026-11.md).

`Gojo.Workspace` is reserved for bounded, experimental Gojo structures. A
workspace is not the whole world and not a detached DTO snapshot. It is a
selected, inspectable scope that can run local structure with controlled access
to live world structure.

The working proposal is that **every use of structure runs in a workspace**.
An already active local structure can be its own workspace: the owner's own
settlement is a workspace whose selection is everything it owns. Other
computations select what they need, work locally, and retain, publish or
propose what they found.

## Core Shape

```text
WorkspaceRoot
  + quoted membership          what is selected: the focus
  + active relation group      what may settle or compute here
  + local introductions        candidates created here, e.g. a closing edge
  + proxy carriers             referenced, captured or diverged participants
  + boundary ports
  + supplied input witnesses
  + provenance links
  + residue / unresolved frontier
```

Quoted membership says what the workspace can inspect. Active relations say
what may settle or compute inside the workspace. These are deliberately
separate: including a relation for context should not automatically reconnect
the whole world into the local settlement problem.

## Selection Is Membership

In the sense used to choose points, strokes or inputs, participation is
selection. What enters a workspace is what this computation uses. Selection is
not stored in the selected element or in its value. The ODV word is
unchanged: `V = 0` reads "no magnitude detectable at this resolution".

| Situation | What it tells this workspace |
| --- | --- |
| Not selected | Nothing. No claim about value or existence. |
| Selected, nothing supplied | The question is open. Do not manufacture `V = 0`. |
| Selected, `V = 0` | No magnitude is detectable at this resolution. It is still an operand. |
| Selected, `V = 1` | Magnitude is detectable. It is not the quantity one. |

Within one focus, an omitted participant and a nonexistent one read the same.
Telling them apart needs another inspection, or a **closed** selection, one
declared exhaustive over a scope. That is the only kind of selection that can
support an absence claim, and only within its declared scope. An **open**
selection ("these") says nothing about what else exists. This matches the
open and closed scopes the finite-law reader already uses.

Six equal contour markers make the idea concrete:

```text
source:     A B C D E F
selection:  1 0 1 1 0 1      an opposite pair omitted
read:       A   C D   F      joined in contour order: a rectangle
```

No marker's value changes, and another selection can still use all six.
Omitting an arbitrary pair would not guarantee a rectangle. The selection
holds only because the contour supplies equal partitions.

A retained selection is ordinary structure. It can be reused, transformed or
itself selected. A selection is produced by a small machine rather than
written as a bag of indexes:

| Selection | Mechanism it needs |
| --- | --- |
| None | An explicitly empty selection |
| All | A declared source scope (a closed selection) |
| Every second element | An ordered traversal and an alternating phase |
| Three connections deep | Adjacency and a bounded traversal |
| Everything in an area | A spatial read in a chosen frame |

Running a structure to produce a selection, by following its adjacency outward
from seeds, is the most primitive execution. It only grows the view, so it does
not depend on order. Narrowing what is possible inside the view is its
counterpart, and it also does not depend on order. Moving material comes after
both and needs accounting.

## Observation And Action

Looking away from a flowing branch does not stop it. The source keeps
settling whatever any workspace selects. A selection controls execution only
through an explicit connection to a gate, binding or execution input.

A workspace may observe through any selection. It may commit a material
change only if its selection covers the whole accounting scope of the law
being applied. Otherwise the commit is refused. Partial attention can
therefore never lose or duplicate material.

## Proxy Structure

Workspace computation should usually happen through proxy carriers and copied
or quoted relations. The proxy keeps local experiments from mutating world
state, while provenance links keep the proxy from becoming an amnesiac copy.

```text
world heat carrier
  -> workspace heat proxy
  -> provenance link back to world heat
```

Copying is not required up front. A workspace participant has one of four
provenance classes:

| Class | Meaning |
| --- | --- |
| Referenced, live | Reads the source as it currently is |
| Referenced, captured | A sample taken at a recorded source epoch |
| Introduced | Created here, with no source counterpart, e.g. a proposed closing edge |
| Diverged | Was referenced, then changed locally (copy on write), with its correspondence kept |

Boundary ports control when a workspace reads from or writes toward the world.
They also preserve residue when the workspace imports only a coarse or partial
view.

## Commit And Writeback

Results leave a workspace in one of five ways:

1. **Retain the interpretation** as a rung or branch beside the source.
2. **Retain the rule** that produced it, which is often the more compact
   memory.
3. **Propose a structural edit** through the source's exposed operations. The
   commit checks that the source epoch still matches what was read. If not,
   the proposal is reconsidered and the mismatch stays visible as residue; it
   is never written over the newer state.
4. **Send value changes as tension** (Axiom 5), never as direct assignment.
5. **Replace the source** only as an explicit, witnessed edit. Keeping the open
   source beside the completed rung is usually better. The construction
   exchange study showed that constructions agreeing today can diverge on the
   next input.

**Self-confirmation guard.** Introduced structure never becomes evidence about
the source. If a proposed closure makes a computation succeed, that shows the
closure was useful under those conditions, not that the source already
contained it. Utility accrues to the rule that introduced the closure. Adopting
it into the source is an edit whose provenance says "introduced by rule R,
adopted after N uses", never "observed".

## Siblings, Rungs And Nesting

Workspaces form a graph, not only a ladder. Two workspaces can be sibling
perspectives on the same source:

```text
source edge  A -> B
roof workspace  uses it as A -> B
body workspace  uses it as B -> A
```

Neither reverses the edge or copies material. A change to the shared seam
reaches both. Other workspace relations include candidate interpretations
(a completed contour), refinements (a finer rung), trials, predictions (a
workspace that never commits) and metaphors. They share correspondence
machinery but make different promises. Going back through a correspondence
cannot recover detail that an abstraction discarded.

A workspace can select from another workspace. Its writeback then goes to
that parent, which decides what reaches the source.

## Resolution And Witnesses

Workspace inputs must record what was admitted, not merely what value a carrier
currently holds. This is how Gojo distinguishes:

```text
hundreds = 3
```

from:

```text
hundreds = 3, tens = 0, ones = 0
```

The first is a coarse supplied witness: about `300`, with lower detail outside
the workspace boundary. The second is exact at ones resolution because the lower
slots participated explicitly.

Coarse participation can use `UnitShape.ResolutionOf(source, ticksPerTick)`.
For example, a hundreds proxy can have a unit shape whose one tick represents
100 ticks of the source unit. In a workspace this may be temporary and dynamic;
in core/world structure it may harden into a stable coarse carrier when that is
the concept being represented.

## Flexible Boundaries

Workspace boundaries should be soft in the practical sense: they are explicit
but adjustable. Boundary residue and unresolved frontier structure are signals
that the workspace may need to grow, shrink, or change resolution.

```text
too much residue at lower value slots -> import finer resolution
unused detail                         -> collapse to a coarser proxy
unresolved relation support           -> include nearby support structure
poor metaphor fit                     -> remove or remap source structure
```

## Learning And Hardening

A workspace is a place to try structure safely:

```text
select / proxy / simulate / inspect residue / adjust / harden
```

If a workspace structure repeatedly works, parts of it can be hardened back
into world structure. That hardening path is the likely core of learning, but
it should remain explicit Gojo structure: selected relations, provenance,
boundary residue, and the reason a local pattern became durable should stay
inspectable. The self-confirmation guard above applies to every hardening
step.

## Metaphor

Metaphor can use the same mechanism. A source structure is selected at low
resolution, proxied into a target workspace, and allowed to fit or fail.

```text
source rhythm / turn-taking / recovery
  -> target conversation workspace
  -> residue shows where the metaphor stops fitting
```

Useful residue is not an error. It is information about the boundary between
the sampled structure and the target context.

## Physical Form

A workspace is how hardware already computes: gather the selected state into
local memory, compute there, then commit with a validity check. Registers, GPU
shared memory and CPU-cache-resident scratch are workspaces. Sibling workspaces
over one source behave like concurrent transactions. A commit validates the
epochs it read, and a stale read becomes residue rather than an overwrite. A
selection over a 64-site scope is one 64-bit plane. It is admissible as Gojo
structure only while it stays connected to its source scope and to the machine
that produced it.

## Existing Footholds

| Mechanism | What it already does |
| --- | --- |
| `Gojo/Structures/FocusSurface.cs` | Source, focus basis, selected participation, carried complement, correspondence, residue and provenance |
| House seam experiment ([SeamLadders](../../experiments/PrimitiveMeasure/SeamLadders.md)) | Face ranges over a shared edge catalogue; open and completed views as windows over signed uses |
| `NativePreviewWorkspace` ([negotiation](../../experiments/ConstructionMachines/Negotiation.md)) | Tries candidates in a private preview before the real machine changes; uses an isolated preview for attached worlds, because reversing a transfer is not an undo |
| `LocalTrialMachine` ([partial decisions](../../experiments/QualitativeFrieze/PartialDecisionStudy.md)) | A trial acts on the world only after an explicit quiet grant |

## First Test: The House

A small fixture would exercise the whole contract:

1. Roof and body workspaces select the shared seam with opposite traversal.
   The source edge is unchanged, and a landmark added through the source
   reaches both.
2. A third workspace introduces a closing edge for an open stroke. The edge is
   marked introduced, and the source's closure read stays open.
3. Adopting the closure is an explicit edit with adoption provenance. A source
   change between read and commit makes validation fail and leaves residue.
4. Selecting four of six contour markers yields a rectangle read without
   changing any marker. The retained selection can itself be selected.
5. An observation workspace that omits a passage does not stop it. A material
   commit whose selection misses part of the law's accounting scope is
   refused.
