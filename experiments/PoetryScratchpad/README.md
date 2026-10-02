# Poetry as connected local readings in Gojo

This experiment relates the supplied poems *Upon leaving the clockworks* and
*Snagged*, and the reader's older letter about them, to current Gojo. It builds
six indicative examples using the existing kernel. The strongest demonstrated
capability is preserving different contexts while executing local substitutions,
arithmetic, and re-reads. The central missing capability is allowing unresolved
interpretations to exert useful pressure on one another without requiring one
final, globally settled meaning.

The poems were inspected in full, including their page images: five pages of
*Clockworks* and one of *Snagged*. Page references below refer to those supplied
PDFs. The examples contain selected excerpts, not copies of the source PDFs.
No outside literary or historical sources were used. No kernel change was made.

## What the experiment actually does

The host supplies candidate interpretations, local units, correspondence rules,
and scheduling. Gojo performs the arithmetic, interval reads, binding
substitution, feedback transitions, and archive replay. The host formats their
results and checks the execution. Labels never select an operation or establish
a semantic relationship on their own.

This is a constructed reading notebook. It does not parse the poems, discover
their metaphors, infer authorial intent, or measure their emotional content.
Except for quantities explicitly in the poems, all numbers are declared
experimental controls. A twelve-tick basis is a chosen local reference, not
twelve readers, a probability, or a calibrated unit of emotion. A zero-to-twelve
basis does not cap an amount at twelve.

The current code and generated observations are [Program.cs](Program.cs),
[PoetryPatterns.cs](PoetryPatterns.cs), and [the run report](out/observations.md).
The report distinguishes historical observations from current circuit reads.

## Reading the poems before fitting equations

The older letter's fish interpretation goes further than the attached *Snagged*.
The poem describes bait descending, resistance, imagined blood, the speaker's
reflection, and finally being snagged. It never establishes a fish brought to the
surface or dying in air. That is a possible reader extension, or a difference
between versions, not an event this PDF establishes. Gojo should preserve that
extension as a separate reading, with its extra premises visible.

This makes the poem especially useful here. The same line connects the reader
who expects to catch something with the speaker who discovers being caught.
Line, mouth, bait, words, blood, umbilical, and reflected face establish related
but nonidentical spaces. The turn need not identify the object below the water
to change the speaker's place in the relationship.

In *Clockworks*, my reading is that attempts to domesticate the world into
measurement repeatedly meet things that exceed or alter the measure. Clockwork,
precision, droplets, the drawing-room, arithmetic, and the abacus coexist with
wildfire, flesh, violent flowering, cosmic scale, and the final messy snow.
This is an interpretive proposal, not a result discovered by the executable.
It makes an entirely settled reading a poor objective: the relations between
measurement, experience, and what escapes measurement are part of the subject.

## Six small constructions

### 1 The catch and the catcher

For *Snagged*, the native structure connects speaker to line to an open target.
A separate open slot represents the additional landing event requested by the
older reading. The two slots are our decomposition of the question; Gojo did
not discover them in the English. Supplying a fish identity binds one slot, but
does not create the event of bringing it up. A bottom-object hypothesis occupies
a separate branch sharing the same source.

The literal depth has its own carrier: traversing `0 -> 27` reads `27` feet;
traversing `27 -> 0` reads `-27`. Neither operation identifies the target or
turns the angler into a fish. Direction of a measured path and reversal of a
participant's role require different structural evidence.

The conceptual local form is `encounter = (speaker -> line -> ?target)`.
The extra claim `landed(?target)` requires its own event binding. This notation
describes connected relations; it is not a second symbolic solver.

One weakness appeared directly: two bindings for the same slot in one ordinary
binding frame use the first binding. Reversing their order changes the selected
identity. This is substitution priority, not an evidential choice. The current
experiment therefore keeps competing interpretations in separate branches.

### 2 Unsettling to fly

*Clockworks*, page 3, connects releasing a branch with readiness to act, while
the word unsettling also invites an affective reading. That affective reading
is not a claim that the bird feels a measured amount of anxiety.

The mechanical illustration uses native qualified subtraction:

```text
remaining attachment = held attachment - released attachment
held = 8/12, supplied alternatives [7/12, 9/12]
released = 6/12, supplied alternatives [5/12, 7/12]
result = 2/12, propagated alternatives [0/12, 4/12]
```

These controls demonstrate declining attachment. Flight conditions remain open;
less clinging alone does not establish airborne motion. A separate contextual
reading can connect the transition with vulnerability, agency, or both.

Native signed negation gives `4 -> -4 -> 4` on one face. Positive four on the
inverse face remains a different contextual location. Consequently, the older
four-stage happy/sad loop needs a supplied transition system with additional
context or phase. It does not follow from numeric negation alone.

### 3 Hands and planets

For the hands-as-planets passage on *Clockworks* page 4, the experiment supplies
a partial correspondence between two peripheral roles and a center. Native
focus projection substitutes those roles inside their relations. Hand, body,
skin, and planetary physical carriers remain distinct. Skin/body participation
and an open physical calibration survive the mapping.

The local construction is `project(selected body relationships, supplied
role correspondence)`. It transfers a relationship that the reader considers
relevant. It does not assert that a hand has planetary diameter or that the poem
describes an actual orbital system. An attempted literal physical comparison
and dimensional projection remain unresolved.

This suggests a way to preserve the scale contrast you value: allow selected
relationships to communicate while retaining the source scales. The result can
hold bodily intimacy and cosmic distance together. A single normalization that
makes both objects equally sized would remove a relationship the reading needs.
Gojo did not calculate awe; it retained conditions under which that response
could remain available to a reader.

### 4 Repetition with consequences

The repeated sun on *Clockworks* page 4 is paired with real bullets rather than
blanks. The experiment stipulates three recurrences and a consequence per
recurrence of either zero or two ticks. The local equation is:

```text
next consequence = current consequence + this recurrence's contribution
next input <- next consequence
```

`EquationPattern` executes each addition. `RepeatSurface` performs the feedback
substitution and retains template, controller, transition, and timeline. The
two runs produce zero and six. With the contribution unbound, no numeric result
is established.

Three calls are scheduled by the host to match the declared controller. These
are witnessed executions, not an autonomous scheduler or a continuously live
timeline. The values are mechanism controls, not quantities extracted from the
poem. Repetition, irreversible consequence, and a changing interpretation of
the repeated event remain distinguishable.

The drops-to-rivulet passage offers a further boundary experiment. Counting to
eleven does not by itself define a universal rivulet threshold. Coalescence,
connectedness, flow, and the observer's scale could supply different transitions.
That extension is proposed here, not implemented in these six examples.

### 5 Connecting a line reading to a theme

The most numerical example uses the strained line in *Snagged*. It deliberately
separates a toy load model from a reader's hypothesis about felt entanglement.
Let `L` be the local load, `C` its supplied capacity, and `W` a contribution from
the speaker's recollection of having caught his own words. These names describe
native carrier-grounded reads, not code-side concept objects.

```text
reserve = C - L
felt load = supplied correspondence(L)
constructed entanglement = felt load + W
```

Without the correspondence, force plus interpretation remains unreadable. The
chosen bridge maps a twelve-tick force reference to a twelve-tick interpretive
reference. Its additive coupling is a reader hypothesis, not a discovered law.

| Current source | Reserve | Constructed entanglement |
| --- | --- | --- |
| Load 8/12 with alternatives 6/12 to 10/12 | 2/12, alternatives 0/12 to 4/12 | 12/12, alternatives 8/12 to 16/12 |
| Same source edited to 11/12, alternatives 9/12 to 13/12 | -1/12, alternatives -3/12 to 1/12 | 15/12, alternatives 11/12 to 19/12 |
| Reloaded circuit edited to 5/12, alternatives 3/12 to 7/12 | 5/12, alternatives 3/12 to 7/12 | 9/12, alternatives 5/12 to 13/12 |

Capacity is held at 10/12 under the toy model. The words contribution is held at
4/12 with alternatives 2/12 to 6/12. All bounds are supplied alternatives and
their affine propagation; they are not empirical confidence intervals. Coverage
and excluded context remain open. Reading a nominal amount does not settle them.

The same live equation responds to changed source coordinates. The saved and
reloaded circuit also responds, rather than looking up its earlier answer.
`StructuralLadderSurface` connects the source, selected load reading, and theme
read. The rung records that supplied connection; it does not discover the theme.

A second real limitation appears here. `(C - L) + L` returns nominal `10/12`,
but its alternatives are `6/12` to `14/12`, even though the identical shared
quantity should cancel under these assumptions. The current interval reader
does not track that dependency through distinct calculation paths. Its bound
is conservative but unnecessarily wide. Several interpretations using the same
phrase can therefore acquire artificial variation, or be mistaken for separate
evidence by an incautious caller.

The separate settlement example receives thirteen ticks into a carrier clamped
at ten, producing **value 10 | tension 3**. That is actual carrier accounting.
It is not interchangeable with an open referent, a variation interval, or a
literary contradiction. Those need an explicit coupling before they can cause
this form of settlement pressure.

### 6 An open historical allusion

The explicitly biblical sparrow on *Clockworks* page 2 connects to an unbound
historical-source slot. The cue supports considering an allusion. It does not
uniquely supply a verse, theology, historical event, or authorial intention.
The example represents that missing correspondence; it executes no historical
inference. This is a useful upper level that can remain attached without being
resolved by the lower-level calculations.

## Revisions to the older interpretation

**Focus is more than cropping.** It can alter which parts become comparable,
which relationships become active, and which details remain outside a local
read. Gojo's focus surfaces preserve much of this distinction. The reader still
supplies the selection and correspondence.

**Negation needs scope.** Not very happy does not uniquely mean slightly happy;
it can accommodate neutrality, sadness, understatement, or a correction to an
expectation. Not slightly happy can reject happiness or reject the word slightly
in favor of very. Coordinate sign, inverse face, logical denial, degree
complement, and contextual reinterpretation should not share one automatic
operator. A region complement may require several disjoint regions, exceeding
the current single-interval qualified reader.

**Scale is not always magnitude on one axis.** Pinch, grip, hold, and haul can
change contact, duration, intention, and movement as well as intensity. Treating
them as points on one force line would discard valuable structure. Likewise,
hands and planets can share a role while their physical scales remain unrelated.

**Repetition does not require identical effect.** A plural is a collection; a
rhythm is a temporal relationship; recurrence can change its own conditions.
An additive loop is a useful local special case. The first sunrise and a later
one can carry different consequences or expectations despite a shared template.

**Finite execution does not disprove mathematical infinity.** Your operational
point fits Gojo: represent what can be read at the present scope, and retain an
open boundary where further distinctions are unavailable. That is different
from claiming all mathematical uses of infinity are invalid. The poem's
suspended time can loosen the relationship between event order and a clock
without asserting that time has ceased or acquired a particular numeric value.

**Exactness is local and conditional.** This agrees with the current axiom on
crystallized resolution. A count depends on admitted units and category scope.
The kernel can do exact arithmetic within that agreement while preserving
evidence that a finer or different read needs additional structure.

**Some ambiguity is productive participation.** Unknown evidence, resolution
residue, model mismatch, and several simultaneously useful meanings should
remain distinguishable. Averaging incompatible readings can invent a bland
meaning that neither reading supports. More precision may leave the poem's
ambiguity intact, or reveal additional relations instead of selecting a winner.

## Missing patterns worth pursuing

The immediate priority is connected alternatives with local scope and shared
evidence. The first-binding behavior should remain a substitution rule, while
a separate native construction preserves incompatible candidate bindings and
their premises. Reordering candidates must not decide their evidential status.

The second priority is dependency-sensitive propagation through shared paths.
The cancellation example provides a small concrete probe. At the interpretive
level, the same phrase may support several readings without becoming several
independent observations. Unknown correlation should remain distinguishable
from known shared dependence.

Next comes partial correspondence with consequences for unmet requests. Current
focus projection can preserve unmatched structure, but its success does not
mean every requested transfer is supported. A local read asking for a physical
dimension should encounter the missing calibration; a role-only read may not
care about it. The read's needs should determine whether a mismatch matters.

Then we need explicit ways to route that unmet need into adjacent structure.
An open slot alone does not push a neighboring reading to refine itself.
`QualifiedReadSurface` supports demand-driven affine calculations; it does not
yet provide a general cycle of semantic influence, nonlinear qualified effects,
saturation, or gradual forgetting. A future local loop could seek enough support
for its current question while retaining several useful interpretations.

Finally, preserving temporal reading order matters. The final snag can change
how earlier agency is read; it does not change what the earlier words literally
said. A phrase should be reusable across successive contexts with evidence of
how the read changed. Existing focus timelines and transform replay provide
pieces of this, but no autonomous cross-level interpretation process has been
demonstrated here.

A promising next test is to ask two different questions of the same unresolved
structure: what is physically at the end of the line, and how does the speaker's
agency change? The second may acquire useful support while the first remains
open. The goal is adequate local reading, not eliminating every tension in the
poem or maximizing a single interpretation score.

## Running and inspecting the examples

From the repository root:

```powershell
dotnet run --project experiments/PoetryScratchpad/PoetryScratchpad.csproj
dotnet run --project experiments/PoetryScratchpad/PoetryScratchpad.csproj -- --pull 7 --low 4 --high 10
```

The controls change the second load reading. They are separate amount and
alternative bounds, all expressed on the declared twelve-tick basis. The host
sets those inputs; native Gojo reads propagate their effects. The fixed first
reading and independent reload probe make the same sequence easy to inspect.

Each run writes to ignored `out/`, replacing the previous run's outputs:

- `observations.md`: the executed reads and their limitations.
- `notebook.gojo.json`: connected examples, witnesses, alternatives, and labels.
- `before-poke.gojo.json`: the explicitly retained first circuit state.
- `live-circuit.gojo.json`: the circuit after the requested source edit, before
  the independent reload probe.

Earlier numeric captions are historical local observations, saved explicitly
alongside the live circuits. They are not used as inputs to subsequent reads.
The generated full notebook contains those circuits at their later input state;
use the before-poke archive to replay the earlier state. Inspection exports and
historical archives are deliberate artifacts of this experiment, not a default
theory of memory as answer lookup.

The harness passed fourteen main checks plus the partial-mapping and feedback
checks in `PoetryPatterns`. Those checks validate encoding behavior, not a correct
interpretation of either poem. Broader literary validity and empirical reader
responses remain outside what this run establishes.

Validation on 2026-10-02 also passed the alternate-input run above and all 941
existing solution tests with `--no-restore`. A fresh `dotnet test Gojo.slnx`
stops with `NETSDK1147` because the browser's `wasm-tools` workload is missing.
