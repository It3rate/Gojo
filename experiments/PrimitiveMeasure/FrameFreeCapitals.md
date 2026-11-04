# Capitals without a construction box

Could you tell an N from a Z if all you knew was which strokes met? In these
block letters a quarter-turn takes one drawing into the other. With the
ground unbound, both names fit. Attach a local ground and the reader can tell
whether the outer strokes run along it or across it. That one clue separates
the letters even if you turn the whole illustration sideways. [Try N and Z](https://gojo.inklingai.ca/encoding/explorer/?layer=23)

E poses a different question. A writer can draw its arms from bottom to top
without changing where they join the spine. Switching writers lets you see
which facts come from stroke order and which need a spatial comparison.
[Try the two orders of E](https://gojo.inklingai.ca/encoding/explorer/?layer=24)

The five constructions supply ordered straight runs and shared sites. Local
site numbers name references. Each listed run participates and its successive
landmarks are distinct positions along it. Distances and screen coordinates
are unspecified.

## The five inputs

Each bracket is a directed read along one run. An interior landmark is both
the end of one span and the start of the next. The crossbar of H uses the
same sites that its two stems encounter internally. A is obtained by sharing
the two initial stem sites at an apex and renumbering the remaining sites.

| Drawing label | Ink runs | Sites shared by runs | Independent cycles | Compatible among these five, without ground |
| --- | --- | ---: | ---: | --- |
| H | [0,1,2] [3,4,5] [1,4] | 2 | 0 | H |
| A | [0,1,2] [0,3,4] [1,3] | 3 | 1 | A |
| E | [0,1,2] [0,3] [1,4] [2,5] | 3 | 0 | E |
| N | [0,1] [1,2] [2,3] | 2 | 0 | N, Z |
| Z | [0,1] [1,2] [2,3] | 2 | 0 | N, Z |

“Shared” counts references used by more than one run. The apex of A counts
even though only two incident spans meet there. The two crossbar sites each
have three incident spans. The cycle read counts independent undirected
cycles of the ink graph. In A, one can go from the apex down to one bar end,
across and back to the apex.

The reader receives the packed graph and compares it to an authored roster.
It looks for a one-to-one correspondence of sites and runs, preserving
membership and the order of landmarks up to reversal on each run. It first
checks incidence counts to reject impossible matches. Counts alone cannot
accept a model. Moving H's bar to the tip of one stem preserves those counts
and removes its H correspondence. Rotating the drawing, assigning new local
site numbers or storing runs in another catalogue order preserves a match.

These are compatibility results for the supplied straight-run decompositions.
They are weaker than a unique identification among all capitals or handwriting
styles. For example, H's crossbar can slide and its stems can splay. Splitting
a run arbitrarily during image extraction changes the input to this reader.
The existing letter studies and junction experiments address other parts of
that wider problem. [Earlier junction study](https://github.com/It3rate/Gojo/blob/0caa847c/Gojo/docs/GojoJunctionInterpretationExperiments.md)

## Ground as optional construction

With the ground unbound, N and Z have identical run bytes and identical
pen-event bytes. Rotating the sample N a quarter turn gives the sample Z's
rendered ink exactly. A reader of those bytes alone must keep both candidates.

The optional ground is a selected local basis with along and perpendicular
roles. A packed pair [run,axis] supplies a known alignment to that basis.
For N the two outer runs bind to the perpendicular role. For Z they bind to
the along role. The middle connector has no such binding. A qualitative
alignment suffices for this comparison. Ground contributes zero ink sites,
spans or cycles. The dashed construction arrow rotates with the displayed
glyph, so a grounded N can still be drawn sideways. The renderer chooses
coordinates only to illustrate one possibility.

Choosing this basis is an authored premise. Inferring a baseline from a page
or establishing a shared ground between neighbours would need an additional
correspondence. The experiment measures neither angles from pixels nor
distances between landmarks.

## The two orders of E

Call the three arm attachments P=0, Q=1 and R=2. The complete spine stores
[P,Q,R] as a chain. The row position supplies the order. A second layout
stores members [R,P,Q] and two strict pairs [P,Q] and [P,R]. Here the
membership table's field positions have no ordering force. In the selected
spine direction, P precedes Q and R. Comparing Q with R yields unresolved.
[Inspect E's two orders](https://gojo.inklingai.ca/encoding/explorer/?layer=24)

| Evidence | Q compared to R | Possible spine traversals in the authored direction |
| --- | --- | --- |
| Chain [P,Q,R] | Before | P Q R |
| P before Q and P before R | Unresolved | P Q R or P R Q |
| P before Q and Q before P, also P before R | Conflict | None |

The comparison follows strict pairs transitively within one carrier. Reversing
the traversal swaps before and after. The same reference reads as the same
site. A reference outside the carrier returns outside. Contradictory claims
in the selected carrier yield conflict. Possible orders assume separate
landmark positions along this run. Coincident positions would require their
own evidence and read.

The writing tape records signed *run visits* and pen lifts. It can record the
arms in order R, Q, P while leaving Q versus R spatially unresolved. It
does not contain sampled positions along an uncertain run. In both allowed
completions the graph still matches the E reference with appropriate arm
correspondence. The reader can thus answer a question about the whole glyph
while one attachment comparison stays open.

Strict comparison uses the chain positions or the reachability of explicit
claims. Only the small five-example matching and illustration apparatus
explores linear completions and possible correspondences. Larger ambiguous
graphs can have many such completions. These experiments establish no
general performance bound for recognition.

## Exact encoding examples

Each run's containing table specifies whether its unary site fields form a
chain or an unordered membership list. Reference width is local to the layout.
Fields begin at the low bit and each table pads only its final byte.

| Input | Bytes for the separate run arrays, in catalogue order | Standard pen-event bytes |
| --- | --- | --- |
| H, 3-bit site references | `88 00` / `63 01` / `21` | `62 0A` |
| A, 3-bit site references | `88 00` / `18 01` / `19` | `62 0A` |
| E, 3-bit site references | `88 00` / `18` / `21` / `2A` | `62 EA` |
| N and Z, 2-bit site references | `04` / `09` / `0E` | `42 08` |

For partial E, the spine membership bytes become `42 00` and the two
precedence rows occupy `08 04`. Its reverse writing tape is `BF 37`.
For grounded N and Z, the optional axis table respectively contains `64`
([0,1],[2,1]) and `20` ([0,0],[2,0]). Removing those rows restores the
unbound comparison.

A pen event uses the high bits for run position, bit 1 to begin a pen stroke,
and bit 0 for reversed traversal. N and Z each have three straight runs and
one authored pen stroke. H and A have three independent pen strokes in these
examples. A different writer can supply another history. The spatial matcher
has no input from the pen tape or display coordinates. The ordinary V bit
can still describe participation in a containing value system. This
structural fixture supplies participating runs directly.

## Implication for later segment reads

An interval read can retain a carrier, ordered endpoints, traversal sense and
open or closed ends. On the same carrier, suppose B precedes both C and D.
The intersection of (B,C) and (B,D) then runs from B to whichever of C and
D is earlier, excluding both bounds. If C versus D is unresolved, an upper
endpoint choice can remain open. A claim that an actually occupied landmark
lies inside needs occupancy evidence as well. This chapter implements the
local comparison used by such a read. Boolean segment operations remain a
later experiment.

~~~powershell
dotnet run --project experiments/PrimitiveMeasure -c Release -- --capitals
dotnet test Tests/PrimitiveMeasure.Tests -c Release --filter FullyQualifiedName~StrokeGraphTests
~~~
