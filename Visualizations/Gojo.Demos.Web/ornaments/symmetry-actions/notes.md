# Symmetry as a reusable action

## What this experiment is for

A pattern can contain a familiar shape without having been drawn one whole
shape at a time. Two neighboring readings may share several of the same
sections. A small repeated action can also generate a larger shape that crosses
the action's repeat boundaries.

This study brings those relationships into view. It asks what small action can
repeat a piece of an existing ornament, what the action preserves, and what it
changes. It also examines two different meanings of a T in a meander: an open
T-shaped boundary and a literal projected three-arm contact. These are useful
reads, but they are not the same native structure.

Start with A2. Its short hook word can be repeated in opposed vertical frames.
Then select a larger seven-section boundary. That boundary resembles an open T,
and its neighboring copy shares three actual sections with it. The shared
sections are read twice; no new material is drawn.

B6 extends this idea with a deeper inward return. Its larger boundary reads
overlap over seven sections. Its framing also produces actual T-shaped
*projected contacts*, whose material routing remains a separate question.

The examples use the declared centre-line reconstructions from the
[source survey](CentrelinePatternStudy.md). They test those connected
realizations. They do not automatically trace the photographs, recover an
original drawing history, or establish that one explanation is the uniquely
correct engine.

## Three things that must remain distinct

**A match** says that a selected transformation takes one observed part to
another part under a stated correspondence. It does not change either part.

**An action** supplies an input surface, transformation structure, displacement,
and output. Repeating that action can produce a new course. An action that
matches one pair of pieces may still fail when repeated again.

**Settlement** would introduce change into a connected local system and let
its relations account for that change. A newly drawn or transformed path does
not establish that such a response mechanism ran.

The action audit checks repeated generation rather than fitting a new
correction at every step: the previous generated output becomes the next input.
If an action drifts away from the observed course, that mismatch remains visible.
There is no hidden adjustment to the target after each application.

Each application creates new sites while preserving the input's internal
shared-site pattern. Equal coordinates at the seam between two generated chunks
do not weld those chunks together. The observed course already has its own
connected handoffs; a generating machine must explicitly retain or establish
the corresponding seam. The present repetition controller is a finite chain of
applications, not an independently running or self-modifying loop.

## A2: a small engine and a larger overlapping read

The authored A2 course begins with these points in its own common-reach frame:

```text
p0 (0,2) -> p1 (3,2) -> p2 (3,1) -> p3 (2,1) -> p4 (2,0)
         -> p5 (5,0) -> p6 (5,1) -> p7 (4,1) -> p8 (4,2)
```

It then repeats with a horizontal displacement of four common-reach units.
These are coordinates of the declared realization, not recovered physical
measurements of the source image. Section `e0` joins `p0` to `p1`, section `e1`
joins `p1` to `p2`, and so on. The section numbering continues through the
whole course.

The first four sections and the next four sections correspond under:

```text
F(x,y) = (x + 2, 2 - y)
```

This is reflection about the local horizontal axis `y = 1`, followed by a
horizontal displacement. The order of traversal is preserved. Applying it
twice gives a translation by four; the reflected frame returns to its original
orientation after two applications.

The larger T-shaped reads use a different cut through the same course:

| Read | Actual course sections | Ordered points |
| --- | --- | --- |
| First open T boundary | `e1` through `e7` | `(3,2), (3,1), (2,1), (2,0), (5,0), (5,1), (4,1), (4,2)` |
| Next opposed boundary | `e5` through `e11` | `(5,0), (5,1), (4,1), (4,2), (7,2), (7,1), (6,1), (6,0)` |
| Their common participation | `e5`, `e6`, `e7` | The same three source sections in both selections |

The same action `F` maps the first ordered boundary to the second. A seven-edge
read therefore advances by four edges and overlaps its neighbor by three.
There is no requirement that a useful concept occupy one exclusive block of
storage, one whole pen, or one generating step.

This is an **open outline** with a mouth between its two ends. Naming its
silhouette a T does not add a closing segment, fill its interior, or create a
three-way junction. A white-region read would also need to retain its boundary,
mouth, and selected outside context.

The same outline has additional support symmetries. A suitable half-turn can
match it when the traversal correspondence is reversed. That is a different
action from the direct vertical reflection used above, even if a renderer
without arrows makes their outputs look identical.

## B6: the same relation with more internal detail

The B6 inner course begins with a sixteen-section word. Its first eight sections
map to the next eight under:

```text
G(x,y) = (x + 4, -4 - y)
```

This reflects about the local horizontal axis `y = -2` and advances by four.
Applying it twice translates by eight. The outer framing is another selected
participation; it is not silently included in the inner course's word.

| Read | Actual course sections | Relation |
| --- | --- | --- |
| First larger boundary | `e1` through `e15` | Starts at `(7,0)` and ends at `(8,0)` |
| Next opposed boundary | `e9` through `e23` | Starts at `(11,-4)` and ends at `(12,-4)` |
| Their common participation | `e9` through `e15` | Seven identical source sections, shared by reference |
| Ordered correspondence | First boundary to next boundary | Apply `G` without reversing traversal |

The B6 boundary has inward returns on its arms. At full detail it is not the
straight-edged T outline of A2. A coarser description can retain the open mouth
and paired arms while exposing the extra return structure as detail. Calling
both examples T-like is a candidate abstraction, not proof that their complete
section words are equal.

The exact overlapping-read relation is stronger and simpler to verify than a
general T classification: the selected source sections are known, the transform
is explicit, the image matches in order, and the overlap is actual shared
participation. That evidence can remain useful even if we later choose a
different verbal name for the shape.

## B6 also has literal projected T contacts

At `(7,0)`, the inner course arrives from the left along `e0` and turns downward
along `e1`. A framing contribution extends from `(7,0)` to `(8,0)`. Their
projection has left, right, and downward arms, but the frame's coincident
endpoint is not the course's native shared endpoint.

At `(4,-4)`, the inner course arrives from above along `e7` and turns right along
`e8`. A framing contribution arrives from the left. Its projection has left,
right, and upward arms.

| Contact | Inner course participation | Additional framing participation |
| --- | --- | --- |
| `(7,0)` | Path 0: `e0` then `e1`; left-to-down turn | Path 4: its section 0, `(7,0) -> (8,0)` |
| `(4,-4)` | Path 0: `e7` then `e8`; above-to-right turn | Path 1: its section 0, `(0,-4) -> (4,-4)` |

Select one common-reach unit of each outward arm. Reflection and translation
`(x,y) -> (x + 3, -4 - y)` take the second contact's projected T support to the
first contact's support. However, the ownership and directed handoff do not
match under that support-only correspondence: one native course uses the upper
and right arms, while the other uses the left and lower arms. The third arm is
provided by a different route in each case.

This is an example of a true coarse match with a meaningful finer mismatch.
The image can support a common T-shaped contact, while the native route reads
still distinguish the possible flow behavior. A switching or merging law would
require a further explicit connection. Coordinate coincidence supplies neither
that law nor a material weld.

## A small code for a selected action grammar

For this experiment, an action can be summarized by four local bits. Displayed
as `b3 b2 b1 b0`, they select:

| Bit | Meaning in this local grammar |
| --- | --- |
| `b0` | Exchange the two basis axes. |
| `b1` | Oppose the image of the source horizontal basis. |
| `b2` | Oppose the image of the source vertical basis. |
| `b3` | Reverse the ordered traversal correspondence. |

Together, the first three bits describe eight signed basis permutations; the
last bit distinguishes the two traversal orders. The displacement is additional
connected structure. These four bits are a compact read of this grammar, not
the complete encoding of a path or a universal replacement for ODV.

The canonical action keeps its native map rows, displacement word, and ordered
marker correspondence connected. A reader can derive the compact code from that
surface. The code must not become the only place the transformation means
anything.

For A2, the compact code is `0100`. Its actual substitution rows are
`H+ -> H+`, `H- -> H-`, `V+ -> V-`, and `V- -> V+`. The displacement word is
`H+ H+ V+ V+`, and the ordered marker pair remains in its original order. B6
uses the same four substitution rows with a different displacement word:
four `H+` reaches and four `V-` reaches. The different offsets place the
reflection axes and the next repeated piece in their respective local frames.

Adding traversal reversal changes this local code to `1100`. It also changes
the connected marker correspondence; it is not a label that can be changed
without changing what the action does.

For an unswapped basis, reversing only the vertical basis changes upward motion
to downward motion and leaves horizontal motion unchanged. It does not turn a
rightward horizontal move into a leftward one. Reversing the path's traversal
does reverse every directed step and reverses the step order. A half-turn
opposes both spatial axes while retaining the traversal order unless reversal
is separately selected.

Turn handedness alone cannot distinguish all these cases. Reflection exchanges
left and right turns; traversal reversal also exchanges them. Combining both
can preserve the turn word while changing the directed correspondence. Endpoint,
basis, and ownership evidence therefore remain relevant.

## What the bounded engine audit can say

The audit searches a declared, finite family. Within each eligible course it
considers chunk widths that divide the section count, eight basis permutations,
both traversal orders, and a constant displacement fitted from the first
correspondence. Each proposed action is then applied repeatedly to its previous
generated output and checked against the observed course.

For each action, the report retains its shortest successful seed. Whole-route
pair checks require at least two sections. Both the ordered geometry and the
pattern of shared endpoint identities must match. The search does not cover
every partial window, cyclic starting phase, multi-route group, or change of
scale. An empty result therefore means no engine was found in the tested family,
not that the pattern has no compact explanation.

A shortest seed found by this procedure means **shortest within this grammar
and this observed course**. It is not a globally shortest program, an exhaustive
search over all Gojo structures, or a probability that the original artist used
that engine. Whole-route pair comparisons likewise check selected transformed
correspondences; they do not infer material joins or hidden histories.

Seed-section counts also are not encoded byte costs. An executable explanation
has a frame, a displacement, a controller, correspondence, and retained checking
evidence in addition to its seed. The study makes those connections inspectable;
it does not claim that the current diagnostic representation is maximally packed.

Several descriptions may fit. For example, a smaller hook with a reflecting
action and a larger two-hook word with a translating action can reproduce the
same finite support. Their different controllers and exposed parameters matter
when we ask what can change next.

The corpus contains related examples from the same source sheets, including
repeated views of the same family. A construction that appears often here is
common in this selected collection. It is not thereby common in all ornament,
human drawing, or other domains. Reuse across independent examples and success
on unseen variations would supply stronger evidence.

## What the recorded run found

The recorded run covers all **18 authored pattern readings**. It retains
**79 route/action recurrences**, and **147 exact action witnesses over 117
distinct whole-route pairs**. More than one action can explain the same pair,
so those last two counts answer different questions. The action laboratory
also validates **240 applications**: five templates, sixteen local action
codes, and three displacements.

Some useful examples concern the main course, rather than the smallest detail
anywhere in the ornament:

| Selected course | Seed sections | Occurrences, including the seed | Repeated action |
| --- | ---: | ---: | --- |
| A2 opposed hooks | 4 | 10 | Reflect vertically and advance. |
| B6 deep interlock | 8 | 6 | Reflect vertically and advance. |
| A5 inward-outward winding | 14 | 4 | Translate the complete winding, including its central turnaround. |
| B5 battlement | 2 | 10 | Reflect vertically and advance. |
| CL5 diagonal zigzag | 1 | 8 | Reflect vertically and advance. |
| CR4, each outer strand | 2 | 3 | Reflect vertically and advance. |
| CR4, each inner strand | 5 | 3 | Reflect vertically and advance. |

Every subsequent occurrence in these rows was produced from the previous
generated one and checked natively. The occurrences include the initial seed:
A2 therefore has nine validated applications after its first four-section
piece, not ten applications after it.

CR4 gives a particularly useful shared mechanism. All four strands use the same
repeating action, `(x,y) -> (x + 28, 12 - y)`, but the inner and outer seed words
are different. A separate reflection, `(x,y) -> (x, 12 - y)`, matches each strand
to its partner. This preserves the distinction between sharing an action and
being copies of the same detailed path. The planar action audit does not by
itself validate an over/under schedule.

Simple translation recurrences appear in thirteen of the eighteen readings;
vertical reflection with advance appears in nine. Quarter-turn recurrences
appear in the small squares of A3 and A6. Those repeated inserts strongly affect
the totals: A3 and A6 alone contribute fifty of the seventy-nine retained
recurrences. Their reported minimum seed of one section describes a small square
component, not their whole ornament. These are counts of witnessed candidates,
not seventy-nine independent concepts or a ranking of aesthetic quality.

The missing results are informative about the search boundary. B4 and CL6 are
collections of independent one-section pens, and the current search does not
take a collection of those pens as its repeating seed. Their zero results do
not count against their regularity or compactness. CL2 supplies another case:
no repeated action was retained within one finite coil, but the whole-route
comparison finds correspondences between the separate coils. A collection-level
controller is a natural next search scope for these examples.

The five comparison controls also separate the claims as intended. Changing a
reach breaks the exact geometric match while retaining the shared-site pattern.
Breaking the handoffs preserves the ordered coordinates while failing the
shared-site check. A reversed target fails the reflection-only comparison and
matches once traversal reversal is explicitly included.

## Reusable patterns and informative exceptions

The current collection suggests several compact engine families worth retaining
as candidates:

- **Advance with alternating frame:** A2 and the B6 inner course use an opposed
  vertical read on successive chunks, then return to the original frame.
- **One word, several placements:** A3, A4, and B3 retain translated copies of
  the same word with a relative phase. B8 adds a vertical displacement and a
  separate spot stream.
- **Repeat a mixed group:** A3 combines paths with divided-square inserts; A6
  combines nested squares with four turned hooks. Each component can remain
  separately readable.
- **Turn inward, then return:** A5 retains a central turnaround and continues
  outward. CL2 instead retains finite inner ends and half-turned cell placement.
- **Several participants define a crossing or a space:** B4, CL6, and CR4 make
  useful structures available only when more than one path is selected.

The less frequent cases are useful because they expose what a simpler engine
omits: finite ends, an extra spot stream, a quarter-turn orbit, an insert, a
frame contact, or crossing order. Their cost is not automatically a defect.
Their utility must be judged against the question the structure is answering.

## Deformation and the next settlement step

A path can be transformed, reparameterized, or edited while retaining a
correspondence to its source. Keeping the same native endpoint identities can
preserve connectedness and closure. It does not automatically preserve the
projected arrangement: moving sections can create or remove contacts, crossings,
overlaps, and white openings. Smooth animation alone is not a guarantee that
these events cannot occur.

Likewise, matching two patterns after a permitted deformation does not show
that either pattern physically settled into the other. This pass supplies
actions and checks. Autonomous geometric settlement is not claimed.

A concrete next settlement experiment would need to connect:

```text
exposed path or pair
  -> a measured or qualitative gap/contact read
  -> a connected desired relation and its mismatch
  -> a permitted local response route
  -> signed tension or an explicit structural retargeting operation
  -> changed local state
  -> a new read, with blocked change and remaining tension visible
```

The response route must say which extent, phase, or site can move, what it must
preserve, and what happens when two demands conflict. If the material may not
cross another strand, that admissibility must be enforced locally or a failed
claim must remain visible. If a boundary is open, the reader must not invent a
closing segment simply to improve the score.

A desired rectangle would therefore need connected claims about its ordered
sides, opposite relationships, corners, and closure. The word "rectangle" would
not select a hidden fitting routine. Adding a corner, removing a section,
splitting a branch, or joining two sites would be an explicit structural change,
with a new correspondence; it would not be disguised as an adjustment of the
old path's values. The original read can remain available alongside the changed
candidate when both have utility.

This would turn an observed relationship into an active local mechanism. The
deeper opportunity is that the same connected word and transform can serve as
a recognizer, a predictor, and a generator, while its selected response laws
determine whether it can also adapt.
