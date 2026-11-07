# The machines behind the ornaments

## What this glossary is for

We can see a diamond, a long hexagon, a winding corridor, or a repeated T in an
ornament. That does not yet tell us what made it. This glossary describes the
actual small mechanisms we can inspect: what enters, what is connected, what
changes, and what comes out. The names are convenient handles for those
structures; they are not new primitive operation tags.

The most useful distinction is between **a shape we can read** and **a machine
whose operation establishes that shape**. A diamond may be the output of one
closed walk, a boundary between two independent strands, or a selected contour
within a larger arrangement. Those interpretations can agree today and respond
differently when an input changes.

This account covers the existing compact primitive experiments and the newer
native ornament action studies. They use related ideas but do not share one
universal encoding or execution contract. Implementation links below identify
which mechanism actually supplies each behavior.

## Start with one concrete machine

The baseline-return machine is a useful example because its purpose, state,
action, and failure are all small enough to see.

```text
request -> rise using A -> across -> descend using B -> inward
                                                        |
                                                        v
                                             read baseline relation
                                             /         |          \
                                          above        at         below
                                            |          |            |
                                       down one tick   finish       hold
                                            |          |
                                            +-- read   +-- actual seam -> next request
```

Its state is the activation's current site and the pen's current position.
Its public observations are above, at, or below the baseline. Its available
correction is a downward tick. The machine cannot correct an overshoot below
the baseline because it has no upward correction outlet.

Another machine can make a fixed second descent instead. At one set of inputs
the two draw identical ink. Change the rise and their responses differ. This is
why the glossary records connections and response routes as well as shapes.
[The actual experiment and encoding](../OrnamentFlowPilot/README.md).

## Does a hexagon machine already exist?

There are several distinct answers, depending on which construction we mean.

| Meaning of the name | What we have | What it does not establish |
| --- | --- | --- |
| Six equal contour parts joined by chords | The earlier regular-polygon construction can retain a circle, six equal parts, ordered landmarks, and neighboring chords. | It does not discover that construction from an image, or produce every possible stretched hexagon. |
| A six-sided closed walk | Ordered sections and shared endpoint identities can encode that walk. Additional direction and extent relations decide its geometry. | Six participating sections alone do not establish convexity, equal sides, or a regular hexagon. |
| A stretched hexagonal opening between strands | The angular weave supplies ordered outer strands whose selected pair exposes such an opening in the declared realization. | That reading is not proof of one autonomous six-sided generator, nor proof that the strands form a material loop. |
| A diamond in the same ornament | A different pair or selected portion can expose a four-sided projected boundary. | It is not necessarily a smaller instance of the same machine that produced the hexagon. |
| One adjustable family covering both outlines | An authored paired-cap family now exercises bounded extent settlement, described below. | A continuous change of the picture does not automatically remove native sites or sections. |

The regular-polygon example is documented in
[repetition and composed shapes](../PrimitiveMeasure/RepetitionExperiments.md).
The strand interpretation is documented in the
[centre-line survey](CentrelinePatternStudy.md) and
[angular weave study](AngularWeaveStudy.md).

The CR4 weave has four continuing strands. Its outer pair and inner pair each
have reflected partners. The outer and inner seeds are **different words**,
although the action audit finds the same reflect-and-advance transformation
repeated on all four strands. Sharing an action is weaker than sharing the
complete construction. No strand closes within the selected three-bay view.

## What counts as a local patch?

A patch is a selected part of an actual construction, together with its
exposed boundary and correspondence to its source. It need not occupy a
separate block of storage or coincide with one pen's work.

Examples include two sections around a corner; the two routes at a crossing;
a run of inward turns; a pair of opposed strands; or an opening bounded by
portions of several routes. A useful patch description records its entry and
exit sites, ordered sections, relevant extent bindings, and any open condition.

```text
source sections --------> selected patch --------> candidate action
       |                        |                        |
       +---- other reads -------+                 new application
                                |                        |
                         exposed boundaries       changed output
                                |                        |
                                +---- comparison witness-+
```

The same section may belong to two overlapping patches. In A2, neighboring
seven-section T-like readings share three actual sections. In B6, neighboring
fifteen-section readings share seven. The sections are observed twice, not
drawn twice. The larger interpretation need not have been a single generation
step. [The exact overlapping readings](SymmetryActionStudy.md).

## Implemented reusable actions

These actions transform selected structure. Their existence does not imply a
live controller repeatedly invokes them, or that an output physically settles
from an input. The action study retains finite sequences of applications and
checks the predicted next instance against the observed construction.

### 1. Advance a retained word

**Short description:** place another instance of this ordered construction at
an offset.

The inputs are a selected section scope, a basis, and a displacement word. The
action maps the selected positions and retains source-to-output section and
site correspondences. Internal shared endpoint patterns survive in the new
instance. Its output can become the input to the next application.

The instruction itself has no changing phase. The application chain records
which instance came next. New instances have new site identities; coincident
coordinates at a repeat boundary do not automatically weld neighboring chunks.
An executable continuing path needs the seam connection as well.

This is useful for repeated winding words, inserts, and complete motifs.
[Native action implementation](Composition/CentrelineAction.cs) and
[recurrence checks](../../Tests/QualitativeExecution.Tests/SymmetryActionStudyTests.cs).

### 2. Reflect vertically and advance

**Short description:** move ahead while taking the next instance in the
opposed vertical frame.

The inputs are the same as in entry 1, but the connected substitution maps a
vertical move to its opposite and preserves a horizontal move. The displacement
locates the reflection axis and the next instance. Ordered traversal is retained
unless a separate reversal is supplied.

For A2 the selected realization reads as `F(x,y) = (x + 2, 2 - y)`. Two
applications restore the vertical frame and translate by four. The constants
are the declared realization's common-reach counts, not measurements recovered
from the photograph.

A2 uses a four-section seed, B6 an eight-section seed, and the simple zigzag
CL5 a one-section seed under this family. These are different amounts of
retained detail behind the same reusable action.
[Action study](SymmetryActionStudy.md).

### 3. Place a second copy at another phase

**Short description:** use one word twice, with a relative placement that
interleaves the copies.

The source is one ordered word. Two applications provide separately owned
instances, and the selected displacement records their relative phase. A3,
A4, and B3 have declared through-routing interpretations using translated
copies in the same vertical frame. B8 adds a vertical displacement and a
separate spot stream.

This must not be relabeled a vertical reflection merely because the combined
picture has opposed-looking parts. Changing the relative phase changes where
the partners encounter each other; a crossing still needs its own read or law.
[Survey of pair constructions](CentrelinePatternStudy.md).

### 4. Repeat around a quarter-turn orbit

**Short description:** reuse a selected part through successive quarter-turn
correspondences.

The action maps the connected basis and ordered section controls. Repeating it
can account for the small square inserts in A3 and A6. The existing action
grammar distinguishes the spatial transformation from reversal of traversal.

A square guarantee needs the relevant reach, quarter-turn, and endpoint
relationships. Four applications that merely look connected do not supply a
native closed cycle. The separate primitive square walk explicitly reuses one
reach and an established turning cycle.
[Action tests](../../Tests/QualitativeExecution.Tests/CentrelineActionTests.cs)
and [square controller](../PrimitiveMeasure/RepetitionExperiments.md).

## Implemented controllers and local responses

These entries have a changing state, a transition law, or a local response
mechanism. Their scopes differ. A passage in the early packed fixture and a
native finite-law transition are not assumed to be interchangeable without a
correspondence.

### 5. Gated handoff and complementary branch

**Short description:** move the existing activation through the selected
available outlet.

The connected roles are source, control, and receiver. The historical
eight-byte passage law lists allowed whole before-and-after states. A resolved
open control admits the handoff only when the receiver can accept it. Source
and receiver change together; the same activation is not copied down both arms.

Two complementary guards form an if/else. Their continuations can join at a
shared receiver. That receiver's presence does not retain which arm was taken;
route history needs its own evidence. An unknown control preserves alternatives
and holds under the fixture's execution policy.
[Conditional encoding](../PrimitiveMeasure/ConditionalExperiments.md) and
[passage tests](../../Tests/PrimitiveMeasure.Tests/PassageTests.cs).

### 6. Phase walk with request and completion

**Short description:** visit a connected body until its explicit completion
condition permits the return or stop.

An activation occupies one body site. A clock allows a handoff. Parameter
attachments select the shared reach used at each participation, while a phase
cycle supplies turn or return evidence. A requested motif also exposes a
request and completion handshake, preventing one held request from silently
retriggering on every beat.

Counting can be the number of visited stages. A five-stage wait needs no hidden
integer countdown. Returning to controller phase is different from returning
the pen to its starting position or to the same native site.
[Repetition mechanism](../PrimitiveMeasure/RepetitionExperiments.md) and
[tests](../../Tests/PrimitiveMeasure.Tests/RepetitionTests.cs).

### 7. Return to a baseline

**Short description:** keep taking the available corrective step until the
published baseline relation says the return is complete.

The local sensor supplies above/at/below evidence. Above opens a downward-tick
outlet, which returns to the sensor. At opens completion. Below leaves this
particular machine blocked. The goal does not grant an upward outlet that was
never connected.

At rise A = 4 and drop B = 2, this machine and a fixed second-drop machine draw
the same completed line. At A = 5 the feedback version returns to baseline and
the fixed version drifts. At A = 1, B = 2 the feedback version cannot repair
its earlier overshoot. These exact ticks are the experiment's realization.
[Flow pilot](../OrnamentFlowPilot/README.md) and
[tests](../../Tests/OrnamentTension.Tests/FlowPilotTests.cs).

### 8. Exchange frames at a seam

**Short description:** a completed bay changes the perspective used for the
next bay.

A public seam event drives a two-state exchange. The selected state binds the
following motif to a normal or reflected frame. The phase persists between
events; it does not flip merely because another reader asks for a picture.
The next motif starts at the actual preceding endpoint in the live example.

This is an executable controller for alternation. It is distinct from entry 2,
which describes the spatial action itself. A controller can request that
action; the action does not supply its own clock.
[Live alternating frame](../OrnamentFlowPilot/README.md).

### 9. Combine sibling drive streams

**Short description:** separately advancing streams contribute to one sampled
movement under a shared clock selection.

One stream may alternate up/down while another advances right. The clock can
choose the first, the second, both, or neither. Each selected stream advances
its own head. Alternating produces an orthogonal step pattern; simultaneous
selection can produce diagonal sampled movement. Both retain the contributing
source and phase.

The same endpoint displacement can accompany a different swept area. A shared
reach binding can justify cancellation of opposed uses; equal-looking but
independent reaches cannot. This fixture's sibling arrays belong to one score.
Separate owners need an explicit interface and shared-basis correspondence.
[Clock fields, bytes, and predictions](../PrimitiveMeasure/BorderScore.md).

### 10. Trigger another pen from an event

**Short description:** let one owner publish a small event that activates an
independent pen.

In the flow pilot, the prior beat's ink publication opens the second pen's
waiting passage. That pen activates a point contact, deposits a dot at its own
bound location, and enters a terminal state. Its cursor is separate, and its
operation leaves the primary pen's edge and beat sequence unchanged.

The receiver learns that a deposit happened, not the full originating shape.
The event is observation; it is not a second copy of conserved ink substance.
This is a small example of a useful thin communication surface, with an
authored receiver action and location.
[Pen coupling and participation](../OrnamentFlowPilot/README.md).

### 11. Balance connected gap inventories

**Short description:** move one shared-unit step from a locally longer gap to
a shorter gap when the permitted comparison shows enough excess.

Each gap owns a prefix chain of occupied step slots. A pair relation authorizes
exchange. Shifted slot comparisons detect at least two excess steps, and a
whole-step passage hands the donor's last occupied step to the receiver's first
empty slot. The scan revisits pairs until a whole pass makes no transfer.

This conserves the total declared spacing inventory. It does not seek a global
mean. With only neighbor connections, local quiet can remain globally uneven.
Different unit references block comparison, and hiding a gap's observation is
different from physically disconnecting it.
[Gap mechanism](../PrimitiveMeasure/WhitespaceSettlement.md) and
[tests](../../Tests/PrimitiveMeasure.Tests/GapSettlementTests.cs).

### 12. Walk inward through whitespace strips

**Short description:** reserve a gap, construct the next side, and repeat while
the declared inner clearance still admits another pass.

A direction cycle chooses supporting-line normals. Adjacent depth sites and
one shared gap establish parallel strips. Corners connect the selected
supporting lines. Depth participation moves through available, pending, and
retained banks. A hidden gap or open corner holds the current partial pass.

The seam decides between one inward spiral and separately closed perimeters.
The current sufficient corner rule covers regular convex triangles, squares,
and pentagons and preserves a positive core. It is not a general collision or
offset solver, and the finite output's center is not forced to become an inked
endpoint.
[Strip walker](../PrimitiveMeasure/WhitespaceStrips.md) and
[tests](../../Tests/PrimitiveMeasure.Tests/StripWalkTests.cs).

### 13. Alternate depth by crossing encounter

**Short description:** offer front/back roles in the order a strand visits its
crossings.

Each strand has an ordered encounter sequence and a two-state exchange law.
Its state offers an over or under role and changes at the next encounter.
The actual local order remains attached to the actual crossing and its two
sections; it can agree with the offer, disagree, or remain open.

This is not one permanent height layer per strand. Planar reflection, path
reversal, and exchanging front/back are separate operations. The three-bay
baseline has four strands, 24 crossings, and 44 adjacent encounter comparisons,
all alternating. Those observations do not prove closure or collision-free
three-dimensional ribbon geometry.
[Weave phase and depth](AngularWeaveStudy.md) and
[tests](../../Tests/QualitativeExecution.Tests/AngularWeaveStudyTests.cs).

## Candidate engines and shape readings

### 14. Turn inward, reverse the progression, and emerge

**Short description:** a course winds toward an interior and then continues
outward through a retained turnaround.

A5's declared course supports a fourteen-section retained seed repeated by
translation. The finite coils in CL2 instead end internally and are related as
separate routes. These are useful contrasting candidates for a winding family.

The action audit verifies repetition of the whole A5 seed. It has not extracted
a general smaller program that decides how long each inward or outward reach
should be, nor learned a tension law that creates the turnaround. Calling both
examples spirals would conceal that missing controller.
[Winding examples and the bounded search](SymmetryActionStudy.md).

### 15. Read a larger shape across smaller operations

**Short description:** select participating boundaries and ask what their
combined arrangement supports.

The T-like windows in A2 and B6 overlap. The weave's inner and outer pairs expose
different openings. A crossing can have an X-shaped projection while retaining
two independent routes, or four arms can share an actual central site. Those
are meaningful shape and topology readings, but they are not automatically
generating machines with matching names.

A white-region read needs its selected boundary, mouth or closure, and outside
context. A convincing silhouette does not supply a missing closing edge. A
coarse common shape can remain useful while finer route, ownership, or depth
evidence disagrees.
[Overlapping T readings](SymmetryActionStudy.md),
[pair selections](CentrelinePatternStudy.md), and
[crossing versus junction](AngularWeaveStudy.md).

## Which machines recur most often?

The latest bounded action audit gives a useful starting answer. It is an audit
of eighteen declared source readings, not a natural-frequency sample or a
ranking of artistic quality.

| Mechanism witnessed | Where it appears in the recorded corpus | Caution |
| --- | --- | --- |
| Translation recurrence | Thirteen of eighteen source readings | One reading can contain several routes and several explanations. |
| Vertical reflection plus advance | Nine of eighteen | It shares an action across different seed sizes; it does not make all seeds identical. |
| Quarter-turn recurrence | Small square inserts in A3 and A6 | Repeated inserts dominate candidate totals and must not become independent votes. |
| Opposed partner correspondence | Outer and inner pairs of CR4 | Pairing is a read over existing strands, not additional material. |
| Same-frame translated partner | Selected interpretations of A3, A4, B3, and B8 | Their phased placement should not be conflated with vertical reflection. |

Of 79 retained route/action recurrences, A3 and A6 contribute 50. The smallest
seed in those rows can describe a tiny insert rather than the whole ornament.
The search also excludes several kinds of collection seed, arbitrary partial
window, and scale change. A missing candidate can therefore describe the
search boundary rather than an irregular pattern.

For a useful glossary, track at least three kinds of reuse separately: repeated
applications within one object, use in independent source examples, and success
after transfer to a new construction. Only the third begins to test the
generality we want from a reusable concept.

## From a glossary entry to a retained machine

A machine card should let us trace:

```text
exposed input -> connected body and laws -> output/read surface
                       ^       |
                       |       v
                    feedback and state

selected requirement -> local observation -> permitted response route
                                                |
                                                v
                                     accepted change or blocked remainder
```

The card should name its source structure, inputs and available observations,
changing state, permitted actions, shared parameters, stopping conditions, and
remaining failures. These descriptions are for inspection. The corresponding
relationships must exist in the encoded construction.

We should also say how it entered the glossary: authored and tested, recognized
within a declared action grammar, or retained after a local learning trial.
The [timing socket experiment](../PrimitiveMeasure/TimingSocket.md) supplies a
small example of the last category: a nearby pulse edit is retained only after
repeatable distinguishing observations, within the measured candidate scope.
It does not yet earn arbitrary geometric correction routes.

## A bounded family now exercising settlement

The [machine settlement study](MachineSettlementStudy.md), following its
[prospective protocol](MachineSettlementProtocol.md), now exercises a narrow
family of paired caps. One of its authored templates has this construction:

```text
upper:  rise-and-advance -> optional plateau -> fall-and-advance
lower:  fall-and-advance -> optional plateau -> rise-and-advance
```

The local basis, corresponding extents, and endpoint relations are explicit.
With matching positive plateaus, one realization can outline a stretched
hexagon. Reducing the plateaus to zero can yield a diamond-shaped projection.
The [native template](Composition/CoupledShapeTemplate.cs) retains three phases
per cap, including each zero-extent plateau. Its two endpoints remain distinct
sites even when their projected positions agree. The paired drawing therefore
retains six sections; it does not silently acquire a four-edge topology.

This is an authored **paired-cap construction**, with hexagon and diamond as
possible reads under declared conditions. Its template expands supplied extent
publications; the cooperating extent controllers are separate participants. It
does not establish that the historical weave was generated this way. Changing
only one cap can also produce a mismatched seam rather than a closed
intermediate polygon. The study separately declares two proposed seam joins.
A formal position read admits or blocks each declaration; admitted joins create
a connected derived view while preserving both source paths. This is neither
an automatic material weld nor permission to merge every coincident site.

Four local regulators control two extents for each cap. Explicit comparison
links read public ports: one regulator follows a supplied desired extent, and
its partner follows the first regulator's published extent. Each receiver gets
only a coarse comparison, then its native response law can request one signed
step. Local capacity retains unabsorbed change as tension. Missing information
and competing alternatives remain unresolved. The selected sensor correspondence
and response outlets make the relationship effective; neither a shape label nor
the target picture tells a controller what to do.

The display reconstructs each drawing from the resulting public extents through
explicit unit-to-reach correspondences. These are fresh realizations of native
templates, not persistent geometric sites moving autonomously. The trace shows
actual admitted extent responses, intermediate state, unresolved requests and
retained zero-length phases. Withheld observations, delays and constrained
capacity expose the limits of this mechanism.

The accompanying retention probe supplies three response laws and uses two
directional challenges to retain an actual native controller archive for reuse.
This is selection among authored candidates, not discovery of a new feedback
law or of the drawing templates. Likewise, the three drawing families are views
of the same controller experiment, not independent discoveries of settlement.

This implemented family is deliberately narrower than arbitrary shape
deformation. A next proposed step is to let a geometric read, such as a seam
or clearance mismatch, publish pressure through an explicit local response
route. That would exercise which lower-level extents can relieve the mismatch;
it would still need to preserve incompatible goals and unrelieved pressure.
