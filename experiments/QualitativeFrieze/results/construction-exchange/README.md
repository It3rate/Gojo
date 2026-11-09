# When can one construction stand in for another?

This experiment starts with several supplied machines that can draw the same outline. It changes their inputs, keeps each machine's connected constraints, and asks which pairs still agree under a selected read. A matching picture is useful evidence, but does not establish the same topology, flow, construction history or future behavior.

Recorded run: **8 machines**, **32 constructions**, **112 pair comparisons**. 56 pairs support the selected boundary exchange; 37 also agree under the coarse topology read. These are finite observations, not a universal interchangeability proof.

Open [the interactive gallery](index.html) to compare drawings, arrows, zero-at-resolution sections and the pair contracts. The complete recorded data is in [results.json](results.json).

Native witness archives: [square.gojo](square.gojo), [stretch.gojo](stretch.gojo), [notch.gojo](notch.gojo), [zero-read.gojo](zero-read.gojo), [contours.gojo](contours.gojo). These retain the connected experiment evidence alongside the presentation reads.

## What the readings mean

- **Outline:** the same multiset of shared-reach line atoms, ignoring direction and section subdivision. Multiplicity is preserved: a doubled line is not one line. The reader supports cardinal and equal-step diagonal sections in the declared basis.
- **Directed outline:** the same atom comparison with traversal direction retained. Matching undirected ink need not match this read.
- **Coarse topology:** the same component count, closed-degree condition and sorted non-degree-two incidence counts. Degree-two subdivisions are ignored. This is not general graph isomorphism or a proof of equivalent execution.
- **Admissibility:** both supplied constructions satisfy the tested obligations. A drawing from a failed candidate is illustrative and cannot authorize exchange.
- **Boundary exchange:** outline agreement and both candidates admissible. Adding the coarse topology contract narrows this set; direction and other contracts remain separate.

## Machines and retained constraints

| Machine | What remains connected | What can change |
| --- | --- | --- |
| Four-step closed walk | Ordered handoffs; closing return; opposing reaches share roles. | Width and height vary independently. A notch needs extra phases. |
| Two pairs of rails | Shared intersections; parallel pairs; common width and height. | Aspect changes directly. Concavity needs a different boundary construction. |
| Four equal radial turns | Common contour; cyclic adjacency; equal-turn promise; equal component reaches. | Uniform scaling fits. Stretching needs release of equal-turn calibration. |
| Four radial points, angular gaps open | Common contour and cyclic adjacency remain; equal-turn obligation stays open. | Rectangles fit the supplied centered realization; general radial interpolation remains open. |
| Two opposed caps | Two three-phase routes share their authored start and end sites. | Same rectangle outline; different section count and traversal. |
| Six-step notched walk | Ordered return survives the inserted left/up detour. | A zero-at-resolution notch retains two phases. Increasing its binding exposes concavity. |
| Three-step open walk | Order and shared adjacent sites; no return edge is supplied. | Closing it requires adding the missing connection. |
| Four detached rails | Matching coordinate expressions, but independent endpoint identities. | The outline can agree with a loop. Joining requires explicit topology. |

## Four input probes

The counts bind repeated shared reaches, rather than pixels or universal physical lengths. Width and height are half-extents in the supplied centered realization. The notch input is consumed only where the native program has that role connected to a move.

| Probe | W / H / notch | What to inspect |
| --- | --- | --- |
| square | 2 / 2 / 0 | Matching initial outlines may hide different traversal, subdivisions and site identities. |
| stretch | 3 / 2 / 0 | Aspect ratio changes. The equal-turn radial candidate must satisfy its retained equality. |
| notch | 3 / 2 / 1 | Only a connected detour consumes the notch input. Other machines do not gain new phases automatically. |
| zero-read | 0 / 2 / 0 | The horizontal extent reads zero. Sites and phases remain; coincident lines retain multiplicity. |

A participating zero remains a zero read at the selected resolution. Zero-length sections retain their phases and sites; a source is not deleted because its rendered line collapses. The model has no evidence about finer physical participation. Replacing a hypothesis also does not mutate the observed source.

## Recorded construction states

| Probe | Machine | Admissible | Failed / unresolved | Sections / zero reads | Seeds / advances / equalities | Components | Closed degree condition | Equal-turn evidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| square | Four-step closed walk | Yes | 0 / 0 | 4 / 0 | 1 / 4 / 0 | 1 | Yes | Not selected |
| square | Two pairs of rails | Yes | 0 / 0 | 4 / 0 | 4 / 0 / 0 | 1 | Yes | Not selected |
| square | Four equal radial turns | Yes | 0 / 0 | 4 / 0 | 4 / 0 / 1 | 1 | Yes | Present |
| square | Four radial points, angular gaps open | Yes | 0 / 0 | 4 / 0 | 4 / 0 / 0 | 1 | Yes | Open |
| square | Two opposed caps | Yes | 0 / 0 | 6 / 0 | 1 / 6 / 0 | 1 | Yes | Not selected |
| square | Six-step notched walk | Yes | 0 / 0 | 6 / 2 | 1 / 6 / 0 | 1 | Yes | Not selected |
| square | Three-step open walk | Yes | 0 / 0 | 3 / 0 | 1 / 3 / 0 | 1 | No | Not selected |
| square | Four detached rails | Yes | 0 / 0 | 4 / 0 | 8 / 0 / 0 | 4 | No | Not selected |
| stretch | Four-step closed walk | Yes | 0 / 0 | 4 / 0 | 1 / 4 / 0 | 1 | Yes | Not selected |
| stretch | Two pairs of rails | Yes | 0 / 0 | 4 / 0 | 4 / 0 / 0 | 1 | Yes | Not selected |
| stretch | Four equal radial turns | No | 1 / 0 | 4 / 0 | 4 / 0 / 1 | 1 | Yes | Present |
| stretch | Four radial points, angular gaps open | Yes | 0 / 0 | 4 / 0 | 4 / 0 / 0 | 1 | Yes | Open |
| stretch | Two opposed caps | Yes | 0 / 0 | 6 / 0 | 1 / 6 / 0 | 1 | Yes | Not selected |
| stretch | Six-step notched walk | Yes | 0 / 0 | 6 / 2 | 1 / 6 / 0 | 1 | Yes | Not selected |
| stretch | Three-step open walk | Yes | 0 / 0 | 3 / 0 | 1 / 3 / 0 | 1 | No | Not selected |
| stretch | Four detached rails | Yes | 0 / 0 | 4 / 0 | 8 / 0 / 0 | 4 | No | Not selected |
| notch | Four-step closed walk | Yes | 0 / 0 | 4 / 0 | 1 / 4 / 0 | 1 | Yes | Not selected |
| notch | Two pairs of rails | Yes | 0 / 0 | 4 / 0 | 4 / 0 / 0 | 1 | Yes | Not selected |
| notch | Four equal radial turns | No | 1 / 0 | 4 / 0 | 4 / 0 / 1 | 1 | Yes | Present |
| notch | Four radial points, angular gaps open | Yes | 0 / 0 | 4 / 0 | 4 / 0 / 0 | 1 | Yes | Open |
| notch | Two opposed caps | Yes | 0 / 0 | 6 / 0 | 1 / 6 / 0 | 1 | Yes | Not selected |
| notch | Six-step notched walk | Yes | 0 / 0 | 6 / 0 | 1 / 6 / 0 | 1 | Yes | Not selected |
| notch | Three-step open walk | Yes | 0 / 0 | 3 / 0 | 1 / 3 / 0 | 1 | No | Not selected |
| notch | Four detached rails | Yes | 0 / 0 | 4 / 0 | 8 / 0 / 0 | 4 | No | Not selected |
| zero-read | Four-step closed walk | Yes | 0 / 0 | 4 / 2 | 1 / 4 / 0 | 1 | Yes | Not selected |
| zero-read | Two pairs of rails | Yes | 0 / 0 | 4 / 2 | 4 / 0 / 0 | 1 | Yes | Not selected |
| zero-read | Four equal radial turns | No | 1 / 1 | 4 / 2 | 4 / 0 / 1 | 1 | Yes | Present |
| zero-read | Four radial points, angular gaps open | No | 0 / 1 | 4 / 2 | 4 / 0 / 0 | 1 | Yes | Open |
| zero-read | Two opposed caps | Yes | 0 / 0 | 6 / 2 | 1 / 6 / 0 | 1 | Yes | Not selected |
| zero-read | Six-step notched walk | Yes | 0 / 0 | 6 / 4 | 1 / 6 / 0 | 1 | Yes | Not selected |
| zero-read | Three-step open walk | Yes | 0 / 0 | 3 / 2 | 1 / 3 / 0 | 1 | No | Not selected |
| zero-read | Four detached rails | Yes | 0 / 0 | 4 / 2 | 8 / 0 / 0 | 4 | No | Not selected |

Program counts expose the supplied construction's size in several ways. They are not a weighted complexity score, an algorithmic minimum or a measured search cost.

## Pair comparisons

Each row is one recorded comparison, under that input only. A rejected stronger read is useful evidence about a construction difference, not a bad ornament score.

| Probe | Pair | Outline | Directed | Coarse topology | Both admissible | Boundary exchange | + topology | Matched / source-only / target-only atoms |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| square | walk → rails | Yes | No | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | walk → radial-equal | Yes | Yes | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | walk → radial-open | Yes | Yes | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | walk → caps | Yes | No | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | walk → notch | Yes | Yes | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | walk → open | No | No | No | Yes | No | No | 12 / 4 / 0 |
| square | walk → detached | Yes | No | No | Yes | Yes | No | 16 / 0 / 0 |
| square | rails → radial-equal | Yes | No | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | rails → radial-open | Yes | No | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | rails → caps | Yes | No | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | rails → notch | Yes | No | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | rails → open | No | No | No | Yes | No | No | 12 / 4 / 0 |
| square | rails → detached | Yes | Yes | No | Yes | Yes | No | 16 / 0 / 0 |
| square | radial-equal → radial-open | Yes | Yes | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | radial-equal → caps | Yes | No | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | radial-equal → notch | Yes | Yes | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | radial-equal → open | No | No | No | Yes | No | No | 12 / 4 / 0 |
| square | radial-equal → detached | Yes | No | No | Yes | Yes | No | 16 / 0 / 0 |
| square | radial-open → caps | Yes | No | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | radial-open → notch | Yes | Yes | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | radial-open → open | No | No | No | Yes | No | No | 12 / 4 / 0 |
| square | radial-open → detached | Yes | No | No | Yes | Yes | No | 16 / 0 / 0 |
| square | caps → notch | Yes | No | Yes | Yes | Yes | Yes | 16 / 0 / 0 |
| square | caps → open | No | No | No | Yes | No | No | 12 / 4 / 0 |
| square | caps → detached | Yes | No | No | Yes | Yes | No | 16 / 0 / 0 |
| square | notch → open | No | No | No | Yes | No | No | 12 / 4 / 0 |
| square | notch → detached | Yes | No | No | Yes | Yes | No | 16 / 0 / 0 |
| square | open → detached | No | No | No | Yes | No | No | 12 / 0 / 4 |
| stretch | walk → rails | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| stretch | walk → radial-equal | Yes | Yes | Yes | No | No | No | 20 / 0 / 0 |
| stretch | walk → radial-open | Yes | Yes | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| stretch | walk → caps | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| stretch | walk → notch | Yes | Yes | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| stretch | walk → open | No | No | No | Yes | No | No | 16 / 4 / 0 |
| stretch | walk → detached | Yes | No | No | Yes | Yes | No | 20 / 0 / 0 |
| stretch | rails → radial-equal | Yes | No | Yes | No | No | No | 20 / 0 / 0 |
| stretch | rails → radial-open | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| stretch | rails → caps | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| stretch | rails → notch | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| stretch | rails → open | No | No | No | Yes | No | No | 16 / 4 / 0 |
| stretch | rails → detached | Yes | Yes | No | Yes | Yes | No | 20 / 0 / 0 |
| stretch | radial-equal → radial-open | Yes | Yes | Yes | No | No | No | 20 / 0 / 0 |
| stretch | radial-equal → caps | Yes | No | Yes | No | No | No | 20 / 0 / 0 |
| stretch | radial-equal → notch | Yes | Yes | Yes | No | No | No | 20 / 0 / 0 |
| stretch | radial-equal → open | No | No | No | No | No | No | 16 / 4 / 0 |
| stretch | radial-equal → detached | Yes | No | No | No | No | No | 20 / 0 / 0 |
| stretch | radial-open → caps | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| stretch | radial-open → notch | Yes | Yes | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| stretch | radial-open → open | No | No | No | Yes | No | No | 16 / 4 / 0 |
| stretch | radial-open → detached | Yes | No | No | Yes | Yes | No | 20 / 0 / 0 |
| stretch | caps → notch | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| stretch | caps → open | No | No | No | Yes | No | No | 16 / 4 / 0 |
| stretch | caps → detached | Yes | No | No | Yes | Yes | No | 20 / 0 / 0 |
| stretch | notch → open | No | No | No | Yes | No | No | 16 / 4 / 0 |
| stretch | notch → detached | Yes | No | No | Yes | Yes | No | 20 / 0 / 0 |
| stretch | open → detached | No | No | No | Yes | No | No | 16 / 0 / 4 |
| notch | walk → rails | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| notch | walk → radial-equal | Yes | Yes | Yes | No | No | No | 20 / 0 / 0 |
| notch | walk → radial-open | Yes | Yes | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| notch | walk → caps | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| notch | walk → notch | No | No | Yes | Yes | No | No | 18 / 2 / 2 |
| notch | walk → open | No | No | No | Yes | No | No | 16 / 4 / 0 |
| notch | walk → detached | Yes | No | No | Yes | Yes | No | 20 / 0 / 0 |
| notch | rails → radial-equal | Yes | No | Yes | No | No | No | 20 / 0 / 0 |
| notch | rails → radial-open | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| notch | rails → caps | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| notch | rails → notch | No | No | Yes | Yes | No | No | 18 / 2 / 2 |
| notch | rails → open | No | No | No | Yes | No | No | 16 / 4 / 0 |
| notch | rails → detached | Yes | Yes | No | Yes | Yes | No | 20 / 0 / 0 |
| notch | radial-equal → radial-open | Yes | Yes | Yes | No | No | No | 20 / 0 / 0 |
| notch | radial-equal → caps | Yes | No | Yes | No | No | No | 20 / 0 / 0 |
| notch | radial-equal → notch | No | No | Yes | No | No | No | 18 / 2 / 2 |
| notch | radial-equal → open | No | No | No | No | No | No | 16 / 4 / 0 |
| notch | radial-equal → detached | Yes | No | No | No | No | No | 20 / 0 / 0 |
| notch | radial-open → caps | Yes | No | Yes | Yes | Yes | Yes | 20 / 0 / 0 |
| notch | radial-open → notch | No | No | Yes | Yes | No | No | 18 / 2 / 2 |
| notch | radial-open → open | No | No | No | Yes | No | No | 16 / 4 / 0 |
| notch | radial-open → detached | Yes | No | No | Yes | Yes | No | 20 / 0 / 0 |
| notch | caps → notch | No | No | Yes | Yes | No | No | 18 / 2 / 2 |
| notch | caps → open | No | No | No | Yes | No | No | 16 / 4 / 0 |
| notch | caps → detached | Yes | No | No | Yes | Yes | No | 20 / 0 / 0 |
| notch | notch → open | No | No | No | Yes | No | No | 14 / 6 / 2 |
| notch | notch → detached | No | No | No | Yes | No | No | 18 / 2 / 2 |
| notch | open → detached | No | No | No | Yes | No | No | 16 / 0 / 4 |
| zero-read | walk → rails | Yes | No | Yes | Yes | Yes | Yes | 8 / 0 / 0 |
| zero-read | walk → radial-equal | Yes | Yes | Yes | No | No | No | 8 / 0 / 0 |
| zero-read | walk → radial-open | Yes | Yes | Yes | No | No | No | 8 / 0 / 0 |
| zero-read | walk → caps | Yes | Yes | Yes | Yes | Yes | Yes | 8 / 0 / 0 |
| zero-read | walk → notch | Yes | Yes | Yes | Yes | Yes | Yes | 8 / 0 / 0 |
| zero-read | walk → open | No | No | No | Yes | No | No | 4 / 4 / 0 |
| zero-read | walk → detached | Yes | No | No | Yes | Yes | No | 8 / 0 / 0 |
| zero-read | rails → radial-equal | Yes | No | Yes | No | No | No | 8 / 0 / 0 |
| zero-read | rails → radial-open | Yes | No | Yes | No | No | No | 8 / 0 / 0 |
| zero-read | rails → caps | Yes | No | Yes | Yes | Yes | Yes | 8 / 0 / 0 |
| zero-read | rails → notch | Yes | No | Yes | Yes | Yes | Yes | 8 / 0 / 0 |
| zero-read | rails → open | No | No | No | Yes | No | No | 4 / 4 / 0 |
| zero-read | rails → detached | Yes | Yes | No | Yes | Yes | No | 8 / 0 / 0 |
| zero-read | radial-equal → radial-open | Yes | Yes | Yes | No | No | No | 8 / 0 / 0 |
| zero-read | radial-equal → caps | Yes | Yes | Yes | No | No | No | 8 / 0 / 0 |
| zero-read | radial-equal → notch | Yes | Yes | Yes | No | No | No | 8 / 0 / 0 |
| zero-read | radial-equal → open | No | No | No | No | No | No | 4 / 4 / 0 |
| zero-read | radial-equal → detached | Yes | No | No | No | No | No | 8 / 0 / 0 |
| zero-read | radial-open → caps | Yes | Yes | Yes | No | No | No | 8 / 0 / 0 |
| zero-read | radial-open → notch | Yes | Yes | Yes | No | No | No | 8 / 0 / 0 |
| zero-read | radial-open → open | No | No | No | No | No | No | 4 / 4 / 0 |
| zero-read | radial-open → detached | Yes | No | No | No | No | No | 8 / 0 / 0 |
| zero-read | caps → notch | Yes | Yes | Yes | Yes | Yes | Yes | 8 / 0 / 0 |
| zero-read | caps → open | No | No | No | Yes | No | No | 4 / 4 / 0 |
| zero-read | caps → detached | Yes | No | No | Yes | Yes | No | 8 / 0 / 0 |
| zero-read | notch → open | No | No | No | Yes | No | No | 4 / 4 / 0 |
| zero-read | notch → detached | Yes | No | No | Yes | Yes | No | 8 / 0 / 0 |
| zero-read | open → detached | No | No | No | Yes | No | No | 4 / 0 / 4 |

## Radial count probe

These are supplied native contour attachments and successor relations with a conditional equal-turn promise. They test the structural count and closure read without pretending to solve a general polygon's geometry.

| Markers | Closed contour | Equal-turn evidence | Open obligations |
| --- | --- | --- | --- |
| 3 | Yes | Yes | 0 |
| 4 | Yes | Yes | 0 |
| 5 | Yes | Yes | 0 |
| 6 | Yes | Yes | 0 |
| 8 | Yes | Yes | 0 |

## Limits and next use

The circular examples use an authored four-point centered-rectangle realization connected to contour evidence. Equal turns impose the extra equal-component obligation; open angular spacing is a weaker construction. This is not a general radial settlement solver, automatic inference from a picture, or a discovered construction history.

The shown positive notch lies in the bounded domain `1 <= notch < 2 * min(width, height)`, which gives the intended simple concave outline. This run tests a bounded subset of bindings; it does not certify that every value accepted by the general input grammar creates a simple polygon.

Candidate programs, extent bindings and tested contracts are supplied. This pass does not learn a new machine, choose an autonomous replacement, perform a material transfer or mutate an observed source. It records where a future exchange could be justified under a specified read, and where additional correspondence or relaxed constraints would be necessary. No agreement here guarantees agreement after an untested change.
