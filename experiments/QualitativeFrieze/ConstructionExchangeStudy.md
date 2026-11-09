# Construction constraints and machine exchange points

## Start here

Several machines can draw the same square. That agreement can be a useful
place to exchange machines, but only for the properties that actually agree.
A closed walk, four intersecting rails, and points on a common circle retain
different dependencies. After a stretch or a notch request they need not
continue to agree.

This experiment supplies eight small native programs, observes four sets of
bindings, and compares every pair at three selected levels. It is an experiment
in correspondence and admissibility, not a learner or an automatic shape solver.
The source programs and earlier observations remain intact when a different
binding or candidate is examined.

Open the [interactive comparison](results/construction-exchange/index.html).
The [recorded Markdown report](results/construction-exchange/README.md) contains
the same explanations, measurements and complete comparison table.

## Recorded findings

The run contains 32 constructions and 112 pair comparisons. Of those pairs,
56 admit the selected boundary exchange and 37 also agree under the coarse
topology contract. These counts include all four input conditions; they are
not ornament quality scores or independent evidence for a general theorem.

The important exchange point is the zero-notch six-step walk: its visible
boundary and directed traversal agree with the four-step walk while its two
additional phases remain available. A later notch binding separates the outputs
without destroying their coarse closed-loop correspondence. Conversely, a
stretched equal-turn radial candidate retains a failed obligation even though
its proposed drawing matches a rectangle. Choosing the angularly open radial
candidate changes the construction promise rather than pretending the failed
obligation was satisfied.

Detached rails demonstrate why matching ink is too weak for a connectivity
claim. The paired caps demonstrate why matching boundary and coarse topology
is still too weak for a flow claim. These distinctions are reproduced from the
native serialized correspondence without looking up machine labels.

## Questions and predictions

These are narrow predictions about supplied constructions, not inferred
historical causes or guarantees about all implementations of a named machine.

| Probe | Prediction | What would contradict the implementation |
| --- | --- | --- |
| Four-step walk versus two three-step caps | Same boundary and coarse topology; different directed flow | Section count alone blocks the boundary read, or matching ink implies matching direction |
| Walk versus detached rails | Same ink, different connectivity | Coordinate equality welds the independent sites |
| Stretch an equal-turn radial quadrilateral | The proposed rectangle fails the retained equal-component calibration | An attractive rendering overrides the retained obligation |
| Leave radial angular spacing open | The centered rectangular realization remains available | Missing equal-turn evidence is treated as either proof of regularity or absence of the entire contour |
| Notch at the selected zero resolution | Six phases remain, with two invisible extents; boundary can match the four-step walk | Zero extent deletes sites or phases |
| Increase the notch binding | Only the program with connected detour uses exposes the notch | A parameter name causes every program to acquire new moves |
| Collapse a radial projection | Distinct geometric marker calibration is unresolved | Several coincident projections prove distinct circular landmarks |
| Add contour markers | Closure and conditional equal-turn structure can survive 3, 4, 5, 6 or 8 markers | Marker count alone is presented as discovered angular equality |

The finite input probes are W/H/N = 2/2/0, 3/2/0, 3/2/1 and 0/2/0.
W and H are half-extents in a supplied centered realization, represented as
chains of a shared reach. N only affects programs whose relations reference it.
They are not pixel dimensions or universal calibrated lengths.

## The encoding that executes

```text
program = frame -> Closed(roles, seeds, advances, drawing edges,
                          equality obligations, retained construction)
seed = actual site -> Closed(direction -> extent role, ...)
advance = (source site -> target site) -> Closed(direction -> extent role, ...)
publication = Closed(role -> Closed(shared reach, ...), ...)
equality = extent role -> extent role
```

The executor expands actual bindings, reduces motion using the existing native
motion frame, and retains the reduction witnesses. A repeated target site is
checked against its already derived position; it is never silently overwritten
to force closure. Drawing edges refer to actual sites. Coincident independent
sites remain independent.

The factories author different programs. Execution contains no square, rail,
notch or other shape-name dispatch. Class names and HTML labels are descriptions
of the connected programs. The radial construction additionally retains the
existing `QualitativeMarkedContour`: center, shared radial participation,
markers, successor cycle, and optionally a common-turn promise. Its supplied
centered realization is justified only in the selected perpendicular drawing
basis. The reader checks the marker/seed correspondence and the sign-reflected
component extents; it is not a general circle detector or radius solver.

## What an exchange promises

The boundary witness connects the actual source and target section scopes to
their declared frame, shared reach and permitted endpoint order. Sections are
subdivided into derived shared-reach atoms. Each atom retains its original
section and its endpoint words. Matched atoms, unmatched occurrences, unresolved
sections and participating zero sections remain explicit native structure.

This read supports cardinal and equal-step diagonal lines. Other slopes,
curves and open inventories remain unresolved. Occurrence multiplicity is kept:
two coincident strokes are not silently replaced by one.

The undirected boundary read deliberately ignores traversal and subdivision.
The directed read keeps each atom's direction; neither read establishes stroke
order or timing. A third, coarse topology read follows site
identity to obtain components and incidence; it ignores degree-two subdivisions
but is not general graph isomorphism. Agreement under one does not establish
agreement under another, nor shared execution order, history or future response.

An exchange candidate must also satisfy its construction obligations. A failed
or unresolved candidate can be illustrated, but its picture cannot authorize
exchange. These are scoped candidate correspondences, not actual port
replacement or material transfer.

## Why zero and open information matter

A participating zero is a zero read at the selected resolution. It supplies no
evidence about finer participation and does not delete the phase or its sites.
An unbound role instead leaves execution unresolved. Neither case establishes
that an observed physical source has changed.

The six-step notch program with N reading zero can share the four-step walk's
boundary. Its two retained detour phases become visible when N participates at
the selected resolution. This is a concrete exchange point: the current outline
agrees while the available next changes differ. The study supplies that richer
program; it does not yet learn to insert those phases.

## Scope and remaining work

- Candidate machines, bindings and comparison contracts are authored. There is
  no autonomous template discovery, search over all programs or learned exchange
  policy in this pass.
- Counts of seeds, advances, equalities and drawing sections are inspectable
  construction costs, not minimum edit distances or performance guarantees.
- A notch is simple and concave only in the intended range
  `0 < N < 2 min(W,H)`. Arbitrary values can reverse sections or self-intersect;
  the machine's name does not certify a simple boundary.
- Finite radial count probes preserve conditional construction without supplying
  general polygon coordinates, a numerical angle solver or dynamic settlement.
- A current match does not establish a match after an untested input. Learning
  when to exchange requires future-response trials, retained failures and a
  connected selection mechanism.
- Full topology-preserving substitution needs richer correspondence than this
  component/incidence summary. The original native sites remain available.

The useful next application is an explicit local choice between candidate
machines: preserve a selected output contract at the exchange point, then
compare how each candidate handles the next request. That should separate
useful flexibility from simply releasing every constraint.

## Run and inspect

```powershell
dotnet run -c Release --project experiments/QualitativeFrieze -- --construction-exchange
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj -c Release --filter "FullyQualifiedName~ConstructionExchange|FullyQualifiedName~BoundaryExchange"
```

The output includes HTML, Markdown, JSON projections and native `.gojo` archives
for each binding condition plus the contour probes. The archives retain the
programs, observations, construction evidence and comparison witnesses rather
than only the scorecard. Their size includes the repeated observations and
atom-by-atom comparison evidence; it is not the storage cost of one primitive
or one runtime machine. They use the existing native numeric format and do not
introduce a new interpretation of historical ODV bits.
