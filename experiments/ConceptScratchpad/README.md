# Gojo as a conceptual scratchpad

The original experiment was performed on 2026-09-26. Its findings below are
historical; they motivated subsequent kernel and encoding corrections. The
executable is [Program.cs](Program.cs), now updated to check those corrections.

## Current status (2026-10-01)

The version-2 graph archive preserves directed endpoint order, inverse-face
identity, shared coordinates, unit descriptors, carrier value and tension, and
attached settlement structure. Legacy imports expose missing evidence instead
of claiming a complete executable restoration. Incomplete predictions have no
numeric answer, and a resolved-violation read rejects unaligned comparisons.

`QualifiedReadSurface` retains executable relations and reads current connected
inputs on demand. Pending addition and subtraction retain their distinct
equations; supplying a missing basis can make the same circuit readable later.
Variation, coverage, resolution, and excluded context remain separate connected
evidence. Missing required archive fields produce an unresolved import witness.

The current harness passes 2,260 checks, the information-preservation audit
passes 24 cases, and the read-boundary audit passes four cases. The solution
suite passes 941 tests with cached restoration. A fresh solution restore is
blocked here by the browser project's missing `wasm-tools` workload.

These are demand-driven calculations. Continuous dynamics, fading retention,
automatic refinement under pressure, and learned discard policies are not yet
implemented. Archives preserve an explicitly captured world; saved historical
read witnesses are observations, not a substitute for executing current reads.

Compatibility checks on this date: the desktop Blocks, visualizer, and sketch
viewer build without warnings; the visualizer smoke run succeeds; Blocks exports
the scene-mechanics and resolution/tension pages; and the sketch viewer renders
102 scene/rung surfaces. Its stricter `--smoke-scenes` check fails on the radial
YT fill assertion, identically on baseline commit `123b5a4` and with these
changes. This is an existing demo defect, not a newly passing check. Browser
compilation and interactive UI behavior remain unverified. New version-2
archives require the updated reader; old readers cannot consume them safely.

## Original experiment and findings

**The height answer is yes under the ordinary shared-scale interpretation:**
Mary's gap above Sue exceeds her gap above John, so John is above Sue.
Gojo successfully executes that interpretation. It does not currently turn the
English sentence into that interpretation or discover the deduction unaided.

This is a useful executable scratchpad already. Its strongest feature is that
calculations, premises, selected bases, and unresolved information can remain
connected. The most urgent weaknesses found here concern incomplete reads and
saving executable structure.

## How the system builds meaning

The inspected path through the current repository was:

1. **A place where change can be accounted for.** A carrier receives change as
   tension. Its connected relations determine what can settle into visible
   value and what remains unaccounted. The 234-tick example in this experiment
   uses a carry relation to leave 4 ticks below and 23 above; no host-language
   division or remainder computes that result.
2. **A location and a direction.** Carrier locations put signed coordinates on
   a particular carrier. Relations connect locations into points, directed
   spans, routes, and larger structures. Negative locations are useful for
   John and Sue below Mary's chosen origin; they do not require inverse flow
   faces. Shared coordinates, identity, and compatible units are different
   kinds of evidence.
3. **A stable meaning for a tick.** `UnitShape` preserves the construction of
   a unit. Resolution and calibration determine how that unit is read in a
   particular use. A shared unit shape alone does not establish a shared
   comparison frame.
4. **An executable equation.** Input and output segments form affine maps;
   operand ports and output structure form sums and products. The difference
   operation used here is an additive surface with a negative port and a
   positive port. Its meaning remains in those relations after the helper
   method has returned.
5. **A read with evidence.** `ReadWitness` connects selected input, read
   surface, result, residue, and provenance. An open slot, a coarse read,
   incompatible bases, and a violated claim need distinct interpretations.
6. **A structure read as an idea.** Concept and scene surfaces bind a selected
   structure to focus, basis, transformation, goal, and residue. A word becomes
   a contextual read over that structure. In the current encoding project,
   sentence-like examples are still fixtures. The older preposition catalog
   is explicitly superseded; this experiment does not revive its word tags.

Useful source entry points are [the kernel overview](../../Gojo/README.md),
[unit shape and crystallization](../../Gojo/axioms/Gojo%20Unit%20Shape%20And%20Crystallization.md),
[equations](../../Gojo/Equations/Equation.cs),
[the common read spine](../../Gojo/docs/GojoReadSpine.md),
[concept surfaces](../../Gojo/ConceptSurface/README.md), and
[current encoding scope](../../Gojo.Encoding/README.md).
The tension-driven emergence described in the axioms is partly a research
direction, not a capability demonstrated by this experiment.

## What "only Gojo" meant in this experiment

All domain arithmetic, comparisons, extrema, projection, and accounting used
existing Gojo APIs. C# constructed relations, enumerated finite input domains,
invoked reads, inspected their output, asserted expectations, and wrote files.
There was no external symbolic solver, arithmetic evaluator, truth table
engine, person class, or fact database. English names were display attachments;
the equation readers never examined them.

I supplied the interpretation and the sequence of reads. In particular, I
selected a common height axis, translated the premises into gaps, and selected
the subtraction that answers the question. This evaluates Gojo as an agent's
scratchpad, not as an autonomous language reasoner.

## The Mary, John, and Sue construction

Let the Mary-to-John gap be `a`, and the Mary-to-Sue gap be `b`. These are names
for carrier locations in the explanation, not symbolic variables in a separate
solver. Mary's coordinate is the shared origin. The executable builds:

```text
Mary: 0
John: read (0 minus a) through a Gojo difference surface
Sue:  read (0 minus b) through a Gojo difference surface

question: read (John minus Sue) through another difference surface
```

Each person is connected to their height span. Each computed position retains
its equation witness. The experiment re-reads both original gaps from the
constructed endpoints to verify the premises survived the construction.
Moving the origin from 0 to 137 leaves the answer unchanged.

For an exact illustration, `a=2` and `b=10` produce a John-to-Sue gap of 8.
Those numbers are an illustration, not measurements inferred from the sentence.

The less committal interpretation of "a bit" and "much" is that `b` contains
`a` plus a positive excess. Sixteen generated Gojo witnesses recover precisely
that excess as John's height above Sue. This avoids choosing a universal
numerical threshold for either phrase. It still relies on an explicitly supplied
shared-scale ordering of the two gaps.

To explore uncertainty rather than silently pick one exact assignment, the
experiment also represents possible gaps as finite segments:

| Supplied interpretation | Worlds evaluated | Gojo range for John minus Sue | Consequence within this domain |
| --- | ---: | --- | --- |
| Small gap 1..3; large gap 8..12 | 15 | 5..11 | John is taller in every world |
| First gap 1..6; second gap 4..9 | 36 | -2..8 | Both orderings and equality remain possible |
| Only both gaps positive, each 1..6 | 36 | -5..5 | Mary's being taller than both does not order John and Sue |

These are exhaustive reads of the stated finite integer domains. They are not
proofs over all real values or all meanings of vague adjectives. In particular,
the overlapping example probes what happens when context fails to establish
separated categories; it is not proposed as the intended meaning of the original
sentence. The smallest positive claim is one tick at this chosen resolution.
The possible worlds are explicit alternatives, not probabilities.

## Original observed results (before the corrections)

| Probe | Observed Gojo behavior | Interpretation |
| --- | --- | --- |
| Coarse read | Positive gap 1 becomes coarse value 0 plus fine residue 1 | A coarse tie is not exact equality |
| Unaligned bases | Two same-unit carriers give a structural residual and zero violation | Zero violation alone does not establish truth |
| Explicit alignment | A supplied 1:1 frame map enables the comparison | The basis relationship is a premise |
| Missing second gap | `complete=false`, one unbound descriptor slot, numeric-looking output 1 | The number is not an established answer |
| Supply the missing gap | Binding it to 10 gives `complete=true`, answer 8 | An unresolved read can be refined |
| Contradictory cycle | A-B=2, B-C=3, C-A=1 leaves closure displacement and violation 6 | Inconsistency is visible without inventing a consistent ordering |
| Shared capacity | Requests 7 and 6 each fit capacity 10; together total 13 | Actual settlement retains value 10 and tension 3 |

The cycle result is a claim violation retained in relation structure. It is not
automatically the same thing as mutable carrier tension. The capacity example
does produce actual carrier tension by feeding the clamped carrier.

## What worked well

- **The result can explain itself.** A height answer can be traversed back to
  the two endpoint reads, their premises, and the selected axis. Tournament
  extrema retain the input world that witnessed each bound.
- **The same lower operations support different ideas.** Ordering people,
  checking a closed chain, and testing combined demand against capacity use
  existing numeric structure without adding named concept classes.
- **Resolution remains honest when inspected.** A sub-tick distinction can
  survive a displayed zero. The source is still available for a sharper read.
- **Unresolved information has somewhere to live.** Unbound slots,
  incompatible carriers, claim violations, and actual overflow can be retained
  as inspectable evidence instead of being hidden by a Boolean answer.

## Original friction and failures (before the corrections)

1. **Proof construction is still manual.** I chose the cancellation path and
   enumerated alternatives. The experiment did not find a general mechanism
   that takes these qualitative premises and automatically discovers an
   unbounded order proof. Putting premises in a group does not itself make
   such a deduction run.
2. **An incomplete pattern can still execute as numeric structure.** In this
   reproduction, the unresolved descriptor slot is consumed by the numeric
   read and the prediction's residue has no remaining slot. The separate
   `unboundSlots` output correctly retains it, and `complete` is false. The
   notebook explicitly reconnects that evidence to its witness. A caller
   looking only at the prediction would see the spurious-looking value 1.
   `EquationPattern.TryPredict` invokes `Equation.Predict` even when binding
   is incomplete. This needs a structural admission boundary, not a UI patch.
3. **A quiet claim can be unresolved.** `OperationClaim` preserves unmatched
   basis evidence as a relation, but its violation magnitude is zero. The
   unresolved residual magnitude also uses `long.MaxValue` as a sentinel.
   A scalar violation test is therefore an unsafe truth reader. Both the
   residual shape and the unresolved evidence matter.
4. **Compact serialization changes directed meaning.** Saving the exact
   height witness retains its cached answer 8, but replay produces -13.
   A minimal reproduction is `Segment(axis,0,-1)`: its flow is -1 before
   serialization and +1 after reload. The writer's `TryReadSegment` uses
   `Math.Min` and `Math.Max`, discarding endpoint order. A snapshot that still
   displays the old answer can conceal this failure.
5. **Compact reload does not restore a live world.** The capacity carrier has
   value/tension 10/3 before reload and 0/0 afterward. The writer records those
   numbers, but the reader creates fresh carriers. Unit shapes, shared tick
   references, and attached settlement behavior also need explicit persistence
   treatment before this format is used as an executable notebook. Their full
   restoration was not tested here.
6. **Evidence is verbose.** This deliberately explicit suite writes about
   16 MB of indented diagnostic snapshot data, while the single compact height
   witness is about 16 KB. Repeated worlds, text attachments, and complete
   traces contribute heavily; these sizes are not an inherent lower bound on
   Gojo representation. A user-facing scratchpad needs focused inspection and
   structural reuse instead of exposing every row at once.

The source locations behind those findings are
[pattern execution](../../Gojo/Equations/EquationPattern.cs),
[unresolved claims](../../Gojo/Structures/OperationClaim.cs), and
[compact writing and rehydration](../../Gojo.Encoding/GojoStructureCompactJson.cs).

## Original recommendations

1. **Make executable persistence preserve direction and identity.** Preserve
   start/end order, distinguish inspection exports from resumable worlds, and
   test replay of negative-weight equations. Comparing cached outputs alone
   will miss the demonstrated failure.
2. **Keep unresolved slots outside ordinary numeric admission until bound.**
   Retain the attempted read, available contributions, and unresolved input as
   connected structure. Ensure every public witness carries the completeness
   evidence so callers cannot accidentally discard it.
3. **Add a small local claim inspector grounded in evidence.** It should expose
   the scalar comparison, basis alignment, unresolved participants, and
   violation witness. Avoid a new global classifier or stored truth enum.
4. **Build one reusable order proof from this example.** Keep two directed
   spans with a shared endpoint; explicitly bind their gap-order evidence;
   derive the remaining span and its positive support. Retain the cancellation
   steps as Gojo witnesses. This would remove the need to enumerate example
   worlds for this specific inference without introducing an outside solver.
5. **Bind vague language through context.** Represent "a bit" and "much" as
   reads over shared gap/basis structure, with separated, overlapping, or
   unbound support as appropriate. Leave exact magnitudes open when ordering
   alone is enough. Do not bake arbitrary universal thresholds into words.
6. **Grow the reasoning set gradually.** Next use the same scratchpad for
   chained comparisons, distance versus direction, containment, shared
   capacity, and temporal order. For each problem keep a supporting witness,
   a counterexample or unresolved alternative, and a refinement case. Broader
   quantified logic needs additional demonstrated structure; this run did not
   establish that it already follows from the current helpers.

## Reproduce and inspect

From the repository root:

```powershell
dotnet run --project experiments/ConceptScratchpad/ConceptScratchpad.csproj
dotnet run --project experiments/ConceptScratchpad/ConceptScratchpad.csproj -- audit-information
dotnet run --project experiments/ConceptScratchpad/ConceptScratchpad.csproj -- audit-read-boundaries
dotnet test Gojo.slnx --no-restore --verbosity minimal
```

The updated experiment completes 2,260 harness checks, including corrected
read and persistence behavior. The finite height cases
include 15 + 36 + 36 domain worlds, 16 positive-excess witnesses, and exact
origin checks.

Current solution totals are 645 kernel, 150 encoding, and 146 CAD tests.
`dotnet test Gojo.slnx` stops at restoration with `NETSDK1147` in this environment;
no browser workload was installed for this experiment.

Generated inspection files live in the ignored `out` directory:

- `results.md`: observed results from the actual run.
- `exact-height.compact.gojo.json`: the current version-2 witness; replay now
  preserves the height result and its directed calculation.
- `notebook.snapshot.gojo.json`: a diagnostic capture of the connected
  notebook, including unit-shape surfaces and carrier state. It is an
  inspection export, distinct from the executable compact archive.
- `information-preservation/`: the current 24-case preservation audit.
- `read-boundaries-corrected/`: the current four-case read-boundary audit.

Earlier `information-audit/` and `read-boundaries/` outputs, when present, are
historical evidence and are not overwritten by the updated audit commands.

An optional first argument selects another output directory. The source is
self-contained and intentionally separate from the production solution.
