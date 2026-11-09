# Construction families: what can change, and what stays?

One drawing is one output. A construction family is the set of outputs a connected machine permits under selected bindings. This experiment compares those sets, changes the source reused by a symmetry machine, and distinguishes nearby shapes that need different kinds of structural change.

Open [the interactive gallery](index.html) for the drawings and comparison controls. [results.json](results.json) contains the recorded observations. All programs and tested bindings are supplied; this study does not learn a new generator or autonomously replace a machine.
Connected native evidence is retained in [ranges.gojo](ranges.gojo), [symmetry.gojo](symmetry.gojo) and [neighborhood.gojo](neighborhood.gojo). These archives preserve more than the report's presentation summaries.

## Read this first

- **A relaxed binding can enlarge a range.** Releasing width = height admits rectangles that the tied square family rejects.
- **A different program need not enlarge it.** A free four-step rectangle walk already has independent width and height. Factoring it into a corner and its half-turn can preserve the same finite boundary range.
- **Source choice is part of repetition.** Reuse the original, feed back the last result, or transform the accumulated group: the same action can produce different participation and overlap.
- **A shifted crop is not automatically a different infinite pattern.** Conversely, matching a supplied phase correspondence does not erase finite boundaries or prove every continuation.
- **Nearby pictures can require different edits.** Changing a binding, applying a geometric word substitution, changing connectivity and replacing a boundary construction are separate moves.

## 1. Finite construction ranges

W and H each use 1 through 4 repeated shared reaches (normally half-extents). Six-equal-steps uses W twice per equal phase, giving total width 4W and height 2W; H is unused there. N is a zero-at-resolution publication, or zero/one in notch-range. One common origin, frame and resolution; no translation or rotation quotient.

The inputs count shared reaches in a declared local basis, not pixels or universally calibrated physical lengths. W and H are offered roles, not guaranteed measured dimensions of the resulting shape: a machine can reuse one role or leave another unused. In particular, the six-equal-step walk keeps a fixed 2:1 aspect ratio and does not consume the independent H offering. An admitted input satisfies this program's tested obligations. Several admitted inputs can have the same boundary read; attempts and unique outputs are therefore different counts.

| Family | Retained constraint | Released or variable condition |
| --- | --- | --- |
| Walk with shared width and height | W and H are joined by an actual equality obligation. | Removing that obligation admits independent widths and heights. |
| Four-step independent rectangle walk | Opposite reaches share roles; W and H are independent; return is checked. | Already permits rectangular aspect changes. A new corner factorization alone adds no range. |
| Two-step corner repeated by half-turn | One horizontal/vertical corner, one half-turn copy, and two declared endpoint joins. | Same sampled rectangle range as the free four-step walk; different reusable construction. |
| Two pairs of parallel rails | Both horizontal rails point right; both vertical rails point up; intersections are shared sites. | Aspect changes fit. A concave notch requires additional structure. |
| Two routes sharing their endpoints | Upper and lower three-step caps share start and finish, retaining opposed route flows. | Same rectangle ink range, but a different directed boundary range. |
| Four equal-turn radial markers | Common contour, cyclic adjacency and equal radial turns; selected calibration requires W = H. | Releasing equal turns allows the selected rectangular calibration to stretch. |
| Radial markers with angular spacing open | Common contour and cyclic adjacency remain; this probe supplies a centered rectangular realization. | Matches rectangle ranges here; this is not an enumeration of all angular placements. |
| Six phases with a zero-read notch | Six ordered phases remain; the supplied N binding is zero at this resolution. | Two participating zero-length phases can expose a notch when the offered N domain expands. |
| Six phases with notch optional | Same six-phase construction; both N = 0 and one shared-reach N are offered. | Includes the sampled rectangles and additional concave outlines without deleting the latent phases. |
| Six equal steps around a rectangle | Right, right, up, left, left, down all reuse one extent role. Every phase uses W twice; H is offered but unused. | Total width is 4W and height is 2W. Varying the unused H cannot broaden this fixed-aspect family; independent corner extents can. |

| Family | Attempts | Admitted | Rejected | Unresolved | Unique outlines | Unique directed outlines |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| tied-square | 16 | 4 | 12 | 0 | 4 | 4 |
| free-walk | 16 | 16 | 0 | 0 | 16 | 16 |
| repeated-corner | 16 | 16 | 0 | 0 | 16 | 16 |
| rails | 16 | 16 | 0 | 0 | 16 | 16 |
| caps | 16 | 16 | 0 | 0 | 16 | 16 |
| radial-equal | 16 | 4 | 12 | 0 | 4 | 4 |
| radial-open | 16 | 16 | 0 | 0 | 16 | 16 |
| latent-notch | 16 | 16 | 0 | 0 | 16 | 16 |
| notch-range | 32 | 32 | 0 | 0 | 32 | 32 |
| six-equal-steps | 16 | 16 | 0 | 0 | 4 | 4 |

### What an overlap means

An outline is a multiset of shared-reach line atoms. The read ignores section subdivision; it preserves repeated occurrences. The directed version also retains the direction of each atom. Neither read establishes the same site identities, traversal schedule, topology, history, or future behavior. Set intersections are over admitted outputs, not just over attractive drawings.

The inclusion statements below concern only the tested finite envelope. They are not proofs that one unrestricted construction language contains another. An exclusive sample is an input whose output was not found in the other family's tested range.
The same input envelope can produce different output scales: six equal steps reuse W more times than the four-step walk. Some six-step rectangles fall outside the four-step inventory's largest sampled width. A finite unmatched size therefore does not prove that a free rectangle machine cannot construct that rectangle with a larger input.

| First | Second | Read | First / second sizes | Intersection | First-only / second-only | First subset of second | Second subset of first | Exclusive sample references |
| --- | --- | --- | ---: | ---: | ---: | --- | --- | --- |
| tied-square | free-walk | Undirected | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / free-walk-1-2-0 |
| tied-square | free-walk | Directed | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / free-walk-1-2-0 |
| tied-square | repeated-corner | Undirected | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / repeated-corner-1-2-0 |
| tied-square | repeated-corner | Directed | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / repeated-corner-1-2-0 |
| tied-square | rails | Undirected | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / rails-1-2-0 |
| tied-square | rails | Directed | 4 / 16 | 0 | 4 / 16 | No | No | tied-square-1-1-0 / rails-1-1-0 |
| tied-square | caps | Undirected | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / caps-1-2-0 |
| tied-square | caps | Directed | 4 / 16 | 0 | 4 / 16 | No | No | tied-square-1-1-0 / caps-1-1-0 |
| tied-square | radial-equal | Undirected | 4 / 4 | 4 | 0 / 0 | Yes | Yes | Not read / Not read |
| tied-square | radial-equal | Directed | 4 / 4 | 4 | 0 / 0 | Yes | Yes | Not read / Not read |
| tied-square | radial-open | Undirected | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / radial-open-1-2-0 |
| tied-square | radial-open | Directed | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / radial-open-1-2-0 |
| tied-square | latent-notch | Undirected | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / latent-notch-1-2-0 |
| tied-square | latent-notch | Directed | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / latent-notch-1-2-0 |
| tied-square | notch-range | Undirected | 4 / 32 | 4 | 0 / 28 | Yes | No | Not read / notch-range-1-1-1 |
| tied-square | notch-range | Directed | 4 / 32 | 4 | 0 / 28 | Yes | No | Not read / notch-range-1-1-1 |
| tied-square | six-equal-steps | Undirected | 4 / 4 | 0 | 4 / 4 | No | No | tied-square-1-1-0 / six-equal-steps-1-1-0 |
| tied-square | six-equal-steps | Directed | 4 / 4 | 0 | 4 / 4 | No | No | tied-square-1-1-0 / six-equal-steps-1-1-0 |
| free-walk | repeated-corner | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| free-walk | repeated-corner | Directed | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| free-walk | rails | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| free-walk | rails | Directed | 16 / 16 | 0 | 16 / 16 | No | No | free-walk-1-1-0 / rails-1-1-0 |
| free-walk | caps | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| free-walk | caps | Directed | 16 / 16 | 0 | 16 / 16 | No | No | free-walk-1-1-0 / caps-1-1-0 |
| free-walk | radial-equal | Undirected | 16 / 4 | 4 | 12 / 0 | No | Yes | free-walk-1-2-0 / Not read |
| free-walk | radial-equal | Directed | 16 / 4 | 4 | 12 / 0 | No | Yes | free-walk-1-2-0 / Not read |
| free-walk | radial-open | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| free-walk | radial-open | Directed | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| free-walk | latent-notch | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| free-walk | latent-notch | Directed | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| free-walk | notch-range | Undirected | 16 / 32 | 16 | 0 / 16 | Yes | No | Not read / notch-range-1-1-1 |
| free-walk | notch-range | Directed | 16 / 32 | 16 | 0 / 16 | Yes | No | Not read / notch-range-1-1-1 |
| free-walk | six-equal-steps | Undirected | 16 / 4 | 2 | 14 / 2 | No | No | free-walk-1-1-0 / six-equal-steps-3-1-0 |
| free-walk | six-equal-steps | Directed | 16 / 4 | 2 | 14 / 2 | No | No | free-walk-1-1-0 / six-equal-steps-3-1-0 |
| repeated-corner | rails | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| repeated-corner | rails | Directed | 16 / 16 | 0 | 16 / 16 | No | No | repeated-corner-1-1-0 / rails-1-1-0 |
| repeated-corner | caps | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| repeated-corner | caps | Directed | 16 / 16 | 0 | 16 / 16 | No | No | repeated-corner-1-1-0 / caps-1-1-0 |
| repeated-corner | radial-equal | Undirected | 16 / 4 | 4 | 12 / 0 | No | Yes | repeated-corner-1-2-0 / Not read |
| repeated-corner | radial-equal | Directed | 16 / 4 | 4 | 12 / 0 | No | Yes | repeated-corner-1-2-0 / Not read |
| repeated-corner | radial-open | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| repeated-corner | radial-open | Directed | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| repeated-corner | latent-notch | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| repeated-corner | latent-notch | Directed | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| repeated-corner | notch-range | Undirected | 16 / 32 | 16 | 0 / 16 | Yes | No | Not read / notch-range-1-1-1 |
| repeated-corner | notch-range | Directed | 16 / 32 | 16 | 0 / 16 | Yes | No | Not read / notch-range-1-1-1 |
| repeated-corner | six-equal-steps | Undirected | 16 / 4 | 2 | 14 / 2 | No | No | repeated-corner-1-1-0 / six-equal-steps-3-1-0 |
| repeated-corner | six-equal-steps | Directed | 16 / 4 | 2 | 14 / 2 | No | No | repeated-corner-1-1-0 / six-equal-steps-3-1-0 |
| rails | caps | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| rails | caps | Directed | 16 / 16 | 0 | 16 / 16 | No | No | rails-1-1-0 / caps-1-1-0 |
| rails | radial-equal | Undirected | 16 / 4 | 4 | 12 / 0 | No | Yes | rails-1-2-0 / Not read |
| rails | radial-equal | Directed | 16 / 4 | 0 | 16 / 4 | No | No | rails-1-1-0 / radial-equal-1-1-0 |
| rails | radial-open | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| rails | radial-open | Directed | 16 / 16 | 0 | 16 / 16 | No | No | rails-1-1-0 / radial-open-1-1-0 |
| rails | latent-notch | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| rails | latent-notch | Directed | 16 / 16 | 0 | 16 / 16 | No | No | rails-1-1-0 / latent-notch-1-1-0 |
| rails | notch-range | Undirected | 16 / 32 | 16 | 0 / 16 | Yes | No | Not read / notch-range-1-1-1 |
| rails | notch-range | Directed | 16 / 32 | 0 | 16 / 32 | No | No | rails-1-1-0 / notch-range-1-1-0 |
| rails | six-equal-steps | Undirected | 16 / 4 | 2 | 14 / 2 | No | No | rails-1-1-0 / six-equal-steps-3-1-0 |
| rails | six-equal-steps | Directed | 16 / 4 | 0 | 16 / 4 | No | No | rails-1-1-0 / six-equal-steps-1-1-0 |
| caps | radial-equal | Undirected | 16 / 4 | 4 | 12 / 0 | No | Yes | caps-1-2-0 / Not read |
| caps | radial-equal | Directed | 16 / 4 | 0 | 16 / 4 | No | No | caps-1-1-0 / radial-equal-1-1-0 |
| caps | radial-open | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| caps | radial-open | Directed | 16 / 16 | 0 | 16 / 16 | No | No | caps-1-1-0 / radial-open-1-1-0 |
| caps | latent-notch | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| caps | latent-notch | Directed | 16 / 16 | 0 | 16 / 16 | No | No | caps-1-1-0 / latent-notch-1-1-0 |
| caps | notch-range | Undirected | 16 / 32 | 16 | 0 / 16 | Yes | No | Not read / notch-range-1-1-1 |
| caps | notch-range | Directed | 16 / 32 | 0 | 16 / 32 | No | No | caps-1-1-0 / notch-range-1-1-0 |
| caps | six-equal-steps | Undirected | 16 / 4 | 2 | 14 / 2 | No | No | caps-1-1-0 / six-equal-steps-3-1-0 |
| caps | six-equal-steps | Directed | 16 / 4 | 0 | 16 / 4 | No | No | caps-1-1-0 / six-equal-steps-1-1-0 |
| radial-equal | radial-open | Undirected | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / radial-open-1-2-0 |
| radial-equal | radial-open | Directed | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / radial-open-1-2-0 |
| radial-equal | latent-notch | Undirected | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / latent-notch-1-2-0 |
| radial-equal | latent-notch | Directed | 4 / 16 | 4 | 0 / 12 | Yes | No | Not read / latent-notch-1-2-0 |
| radial-equal | notch-range | Undirected | 4 / 32 | 4 | 0 / 28 | Yes | No | Not read / notch-range-1-1-1 |
| radial-equal | notch-range | Directed | 4 / 32 | 4 | 0 / 28 | Yes | No | Not read / notch-range-1-1-1 |
| radial-equal | six-equal-steps | Undirected | 4 / 4 | 0 | 4 / 4 | No | No | radial-equal-1-1-0 / six-equal-steps-1-1-0 |
| radial-equal | six-equal-steps | Directed | 4 / 4 | 0 | 4 / 4 | No | No | radial-equal-1-1-0 / six-equal-steps-1-1-0 |
| radial-open | latent-notch | Undirected | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| radial-open | latent-notch | Directed | 16 / 16 | 16 | 0 / 0 | Yes | Yes | Not read / Not read |
| radial-open | notch-range | Undirected | 16 / 32 | 16 | 0 / 16 | Yes | No | Not read / notch-range-1-1-1 |
| radial-open | notch-range | Directed | 16 / 32 | 16 | 0 / 16 | Yes | No | Not read / notch-range-1-1-1 |
| radial-open | six-equal-steps | Undirected | 16 / 4 | 2 | 14 / 2 | No | No | radial-open-1-1-0 / six-equal-steps-3-1-0 |
| radial-open | six-equal-steps | Directed | 16 / 4 | 2 | 14 / 2 | No | No | radial-open-1-1-0 / six-equal-steps-3-1-0 |
| latent-notch | notch-range | Undirected | 16 / 32 | 16 | 0 / 16 | Yes | No | Not read / notch-range-1-1-1 |
| latent-notch | notch-range | Directed | 16 / 32 | 16 | 0 / 16 | Yes | No | Not read / notch-range-1-1-1 |
| latent-notch | six-equal-steps | Undirected | 16 / 4 | 2 | 14 / 2 | No | No | latent-notch-1-1-0 / six-equal-steps-3-1-0 |
| latent-notch | six-equal-steps | Directed | 16 / 4 | 2 | 14 / 2 | No | No | latent-notch-1-1-0 / six-equal-steps-3-1-0 |
| notch-range | six-equal-steps | Undirected | 32 / 4 | 2 | 30 / 2 | No | No | notch-range-1-1-0 / six-equal-steps-3-1-0 |
| notch-range | six-equal-steps | Directed | 32 / 4 | 2 | 30 / 2 | No | No | notch-range-1-1-0 / six-equal-steps-3-1-0 |

### Range limits

- Range inclusion and equality refer only to these explicitly enumerated publications, not all possible machine outputs.
- Undirected ink ignores traversal and subdivision; directed ink retains segment direction but still ignores execution order. Both preserve overlapping occurrence multiplicity.
- Boundary agreement does not establish identical topology, machine, potential, construction history, area prediction or equal probability of outputs.
- The radial-open family samples one supplied rectangular realization. Open angular spacing has many other possible realizations that are not sampled.
- Six-equal-steps has only one used extent role. Its unused H publications duplicate outputs; finite range overlap is also limited by the offered scale range.
- Zero-at-resolution notch phases remain in structure and evidence. Their lack of visible length is not removal of participation.

## 2. Symmetry machines: what becomes the next source?

A repeat consists of a selected source, a connected action, a controller and a next-source relation. Reapplying a fixed transform to the original can repeatedly stamp the same location. Feeding back the previous result accumulates the transform. Reusing the whole group transforms all retained participation, so group growth and overlap matter. An advancing placement clock is another input; it must not be smuggled into 'reuse the original'.
These samples copy observations; they do not divide or duplicate conserved material flow. A phase shift step is a placement slot in the supplied rule, not necessarily one native reach or one line section.

| Sample | Motif | Construction | Rule | Steps | Retained sections |
| --- | --- | --- | --- | ---: | ---: |
| original-fixed | angular wing | Reuse the original, fixed placement | Every request reads the original wing through the same reflection-and-advance action; repeated copies overlap. | 3 | 20 |
| feedback | angular wing | Reuse the preceding result | Each output becomes the next action's input. Advance accumulates and vertical orientation alternates. | 3 | 20 |
| whole-group | angular wing | Reuse the entire accumulated group | Keep the current group and add its transformed copy. Section participation doubles per round; coincident copies are not merged. | 3 | 40 |
| original-clock | angular wing | Reuse the original with advancing placement | The original is unchanged; supplied placement steps advance each copy. A fixed reflection does not create alternating orientation. | 3 | 20 |
| mirror-y-advance | asymmetric corner | Reflect vertical, then advance | The horizontal displacement is unchanged by this vertical reflection. | 2 | 2 |
| advance-mirror-y | asymmetric corner | Advance, then reflect vertical | The same two native actions in the other order; this selected perpendicular displacement commutes. | 2 | 2 |
| mirror-x-advance | asymmetric corner | Reflect horizontal, then advance | The mirror opposes horizontal coordinates before the positive displacement is added. | 2 | 2 |
| advance-mirror-x | asymmetric corner | Advance, then reflect horizontal | The final mirror also reverses the displacement; these actions do not commute. | 2 | 2 |
| phase-left | alternating angular wings | Original / mirror cell | Two ordered motif positions form a supplied cell. Two placement steps supply its period; a separate cyclic read permission allows wrapping. | 2 | 10 |
| phase-right | alternating angular wings | Mirror / original cell | Two ordered motif positions form a supplied cell. Two placement steps supply its period; a separate cyclic read permission allows wrapping. | 2 | 10 |
| phase-broken | alternating angular wings | Opposite phase with an added mark | Two ordered motif positions form a supplied cell. Two placement steps supply its period; a separate cyclic read permission allows wrapping. | 2 | 11 |
| zigzag-feedback | zigzag | The same feedback action on a zigzag | The source shape changes; the connected reflection-and-advance action is reused. | 3 | 16 |

### Order and phase comparisons

Raw equality compares the recorded finite outputs. Aligned equality compares the explicitly supplied correspondence. The cyclic comparison validates actual cell slots, the connected placement action, its doubled period and inverse wrap, plus an explicit permission to read modulo that period. Forward continuation alone does not grant that permission. No arbitrary best-looking crop is a proof of universal equivalence. A directed match is still weaker than identical timing or producer history.

| Comparison | From → to | Raw | Under supplied alignment | Directed | Shift steps | Qualification |
| --- | --- | --- | --- | --- | ---: | --- |
| Changing which structure is reused | original-fixed → feedback | No | No | No | 0 | The action is shared, but source dependencies differ. A fixed original accumulates coincident copies, not an advancing strip. |
| Grouping changes participation | feedback → whole-group | No | No | No | 0 | The occupied locations can resemble one another, but the growing group has overlapping multiplicity. Ink is not silently unioned. |
| Placement is separate from feedback | feedback → original-clock | No | No | No | 0 | Both advance across the page. Only output feedback alternates the previous result's orientation. |
| These two actions commute in this basis | mirror-y-advance → advance-mirror-y | Yes | Yes | Yes | 0 | Vertical reflection and horizontal translation agree in either order for this connected frame. |
| The displacement is reflected too | mirror-x-advance → advance-mirror-x | No | No | No | 0 | Horizontal reflection and horizontal advance disagree. Relaxing phase is not permission to discard this changed displacement. |
| A finite translated crop still has different ends | phase-left → phase-right | No | No | No | 1 | A one-slot translation aligns the interior cadence but moves the finite cell's end. No crop is silently discarded. |
| Relax phase under a supplied periodic continuation | phase-left → phase-right | No | Yes | Yes | 1 | One connected placement action shifts the selected phase; its two-step period supplies the validated inverse wrap. This needs the explicit cyclic-cell quotient permission, not forward continuation alone, and preserves every section occurrence. |
| A changed motif cannot be explained by phase alone | phase-left → phase-broken | No | No | No | 1 | One connected placement action shifts the selected phase; its two-step period supplies the validated inverse wrap. This needs the explicit cyclic-cell quotient permission, not forward continuation alone, and preserves every section occurrence. |

## 3. A neighborhood of constructions

This is a small set of supplied neighboring constructions, not an exhaustive map of shape space. The labels name useful reads; they are not new primitive shape opcodes. A rotated square, a rhombus, a pentagon, an open walk and a contour boundary do not all arise by relaxing the same condition.

| Example | Kind of change | What changed | What remains | Limits |
| --- | --- | --- | --- | --- |
| Square with a shared extent | binding retained | Both extents are joined by an equality obligation. | Ordered loop, four phases, right corners, equal sides. | A rectangular offer is rejected until the equality obligation is released. |
| Rectangle | release binding | Release W = H; the two opposing pairs still share their respective reach. | Four phases, closure, right corners, opposing side correspondence. | Releasing equality does not remove the turn or closure obligations. |
| Diagonally transformed square | word substitution | Map each H step to H+V and each V step to -H+V. | Equal sides, right corners, ordered closed loop. | This integer-word map turns 45 degrees AND scales by sqrt(2); it is not a unit-length rotation. |
| Rhombus: turn, then stretch | word substitution | Apply the diagonal map, then double only the horizontal component. | Equal sides, closure, opposing sides. | Right corners no longer survive. Equal sides alone do not imply a square. |
| Rectangle: stretch, then turn | word substitution | Double the original horizontal component, then apply the diagonal map. | Right corners, closure, opposing sides. | Equal adjacent sides no longer survive. The same two actions in the other order give a rhombus. |
| Sheared loop | word substitution | Add one horizontal step to every upward step, and its inverse to every downward step. | Closure, order, parallel opposing sides. | Neither equal adjacent sides nor right corners is preserved here. |
| Extended supporting lines | change connectivity/focus | Select the extended supports instead of the bounded perimeter. | Four support directions and correspondence to the source edges. | The four strokes cross in projection; they are not welded. This is not a closed boundary. |
| Diagonal supporting lines | word substitution | Apply the same diagonal map to the four extended supports. | Four separate strokes; projected crossings. | Joining crossings would be an additional topology change. |
| Notched loop | authored structural neighbor | Supply a six-phase candidate with a nonzero detour and a checked return. | Proposed width, height and notch role correspondences link the candidate to the source program. | This is an authored candidate association, not an executed edit or a claim of shared site identity. Adding or removing edges needs a structural edit law. |
| Open three-step path | authored structural neighbor | Supply a three-phase candidate with no return edge. | Proposed width, height and notch role correspondences link the candidate to the source program. | This is an authored candidate association, not an executed edit or a claim of shared site identity. Adding or removing edges needs a structural edit law. |
| 4 equal contour partitions, joined | replace boundary construction | Subdivide one native circular whole, locate its marks, and join successive marks; return to the first mark. | Shared contour and radial reach; ordered subdivision; explicit adjacency and closing seam. | The regular picture illustrates the declared contour contract. No screen coordinates prove side lengths; the chord-to-planar metric reader remains open. |
| 5 equal contour partitions, joined | replace boundary construction | Subdivide one native circular whole, locate its marks, and join successive marks; return to the first mark. | Shared contour and radial reach; ordered subdivision; explicit adjacency and closing seam. | The regular picture illustrates the declared contour contract. No screen coordinates prove side lengths; the chord-to-planar metric reader remains open. |
| 6 equal contour partitions, joined | replace boundary construction | Subdivide one native circular whole, locate its marks, and join successive marks; return to the first mark. | Shared contour and radial reach; ordered subdivision; explicit adjacency and closing seam. | The regular picture illustrates the declared contour contract. No screen coordinates prove side lengths; the chord-to-planar metric reader remains open. |
| 8 equal contour partitions, joined | replace boundary construction | Subdivide one native circular whole, locate its marks, and join successive marks; return to the first mark. | Shared contour and radial reach; ordered subdivision; explicit adjacency and closing seam. | The regular picture illustrates the declared contour contract. No screen coordinates prove side lengths; the chord-to-planar metric reader remains open. |
| Whole circular contour | replace boundary construction | Select the circular whole itself rather than a list of chords between selected marks. | Point-derived common offset, radial attachment, whole traversal and closing site. | A native contour is present; this does not claim that dropping a polygon's side count generates a circle. |

| Example | Equal section lengths | Perpendicular joins | Components | Sections | Contour markers | Whole-contour read |
| --- | --- | --- | ---: | ---: | ---: | --- |
| Square with a shared extent | Yes | Yes | 1 | 4 | 0 | No |
| Rectangle | No | Yes | 1 | 4 | 0 | No |
| Diagonally transformed square | Yes | Yes | 1 | 4 | 0 | No |
| Rhombus: turn, then stretch | Yes | No | 1 | 4 | 0 | No |
| Rectangle: stretch, then turn | No | Yes | 1 | 4 | 0 | No |
| Sheared loop | No | No | 1 | 4 | 0 | No |
| Extended supporting lines | Not read | Not read | 4 | 4 | 0 | No |
| Diagonal supporting lines | Not read | Not read | 4 | 4 | 0 | No |
| Notched loop | No | Yes | 1 | 6 | 0 | No |
| Open three-step path | Not read | Not read | 1 | 3 | 0 | No |
| 4 equal contour partitions, joined | Not read | Not read | 1 | 4 | 4 | No |
| 5 equal contour partitions, joined | Not read | Not read | 1 | 5 | 5 | No |
| 6 equal contour partitions, joined | Not read | Not read | 1 | 6 | 6 | No |
| 8 equal contour partitions, joined | Not read | Not read | 1 | 8 | 8 | No |
| Whole circular contour | Not read | Not read | 1 | Not read | 0 | Yes |

Equal section lengths compares the retained sections, which need not be maximal geometric sides. Perpendicular joins checks adjacent section directions; it admits both a convex 90-degree corner and a concave 270-degree interior corner. It does not certify convexity.

- A weaker equality obligation broadens a range; a different view need not broaden it.
- Stretch and diagonal reorientation preserve different constraints when their order is exchanged.
- Four equal contour partitions give the square candidate. A rectangle can use unequal alternating angular gaps on the same circle; an ellipse is not required.
- A five-sided boundary requires another marker and edge plus a different turn partition. It is not merely a four-side binding made unknown.
- The whole circle is an intrinsic construction, not an increasingly dense polygon.
- Coordinates are a selected rendering/read basis. Copying a candidate does not transfer material or alter the source.

The finite diagonal substitution is (x,y) → (x−y,x+y). It actively copies and transforms the coordinate words into a new output, leaving the source unchanged; it is not merely a different observer's view. In the displayed Euclidean basis it includes both a 45-degree rotation and a √2 scale, so it is not a length-preserving rotation. Stretching before this substitution can retain perpendicular joins; stretching after it can retain equal section lengths while changing the joins. The order of actions matters.

Radial illustrations are regular display layouts of the supplied native contour, marker count and successor structure. Pixel locations, drawn straight chords and the smooth circle are presentation choices. They do not establish missing metric settlement, chord lengths, area or a general regular-polygon solver. 'Not read' in a property field remains unavailable evidence, not a negative result.

## What this prepares, and what remains open

The useful output is a collection of bounded correspondences and counterexamples: where two machines overlap, what an extra constraint costs, and what changes when the next source changes. These are candidates for later machine exchange, prediction and learned preferences. The present study supplies the candidate machines, input envelope, correspondences and transformations; it does not discover them from drawings or learn a new vocabulary autonomously.

Changing a candidate is not permission to mutate an observed source. Participating zero is zero at the selected resolution; it does not remove the participating section or prove absolute absence. Geometric equality does not establish topology, identical flow, quality, taste or interoperability under every future input. More general relaxation, settlement, interpolation and machine selection need their own connected laws and evidence.
