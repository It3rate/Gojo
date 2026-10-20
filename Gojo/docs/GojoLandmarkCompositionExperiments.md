# Connections at selected motif landmarks

Status: implemented experimental reader above the kernel. The
[landmark gallery](../../experiments/QualitativeFrieze/results/landmark-composition/gallery.html)
refines the earlier
[whole-motif composition fixture](GojoOccurrenceCompositionExperiments.md).
No kernel primitives or measured geometric values are introduced.

The earlier path-contact example exposed a real limitation: sharing any site
between a path and a contour does not establish that the shared site is a path
endpoint. A middle vertex can satisfy the same claim. This round adds selection
of actual site roles inside a reusable motif, then tests those selected sites
after matching the motif to an observation.

## Cases and observed outcomes

| Case | Coarse question | Refinement | Result |
| --- | --- | --- | --- |
| L01 | Does a separate path touch a loop? | Does at least one path endpoint touch it? | Retains internal and endpoint contact initially, then distinguishes them. Coincident unjoined sites fail both questions. |
| L02 | Does a path contact two separate loops? | Does each different path endpoint join a different loop? | Rejects the middle-plus-one-end alternative while preserving the coarse contact read. |
| L03 | Does a path touch a four-edge loop? | Does its endpoint join the corner whose sides extend rightward and upward? | Distinguishes two joined corners using incidence and qualitative heading faces. |
| L04 | Does a path contact two loops through its endpoints? | Ask the same landmark questions for different internal subdivisions. | Both subdivisions remain compatible; endpoint topology does not establish equal subdivision. |

Each source and candidate is an independently executed `UnitArrangementNode`
construction. Joins are admitted through actual exposed site ports. The
history-blind publisher supplies fresh site/edge identities, incidence and
qualitative heading participation. Pattern exemplars execute separately from
the source and alternatives. Positions and repeated reach words stay outside
the observation used for matching.

L04 deliberately retains a finer private construction difference. One bridge
uses equal formal reach words; the other divides the same total formal reach
unequally. Their public endpoint evidence agrees. The internal vertex is a
topological landmark, not evidence of a halfway point. A separate equal-reach
or equal-subdivision relation could support an unmeasured midpoint; a numeric
length or calibration is not intrinsically required. This fixture does not yet
publish or query that relation.

## Native selection and correspondence

The implementation extends
[OccurrenceComposition.cs](../../experiments/QualitativeFrieze/OccurrenceComposition.cs).
Legacy whole-footprint questions retain their exact question shape and prior
answers and assignment counts. An extended question adds explicit scopes:

```text
selection = actual occurrence-role slot -> Closed(selected pattern site roles)

extended question = legacy whole-motif question ->
                    (Closed(shared selection pairs) ->
                     Closed(disjoint selection pairs))

selection pair = first selection -> second selection
selected-site witness = actual selection -> Closed(actual observed sites)
```

A selector contains actual site placeholders from that role's connected native
pattern. Endpoint and internal-site selections are authored by traversing the
pattern's ordered path. The corner selection reads its incident heading faces.
There is no endpoint enum, corner identifier or numeric coordinate in the
canonical question. The exact native question can be transported and read
without its builder or original caption.

Matching retains the pattern-to-family correspondence and the family's actual
occurrence bindings. The selected placeholders project through these bindings
to actual observation sites. Sharing requires actual common identity; equal
screen positions never create a join. Witnesses retain selected-site mappings,
full matching correspondences and the shared/disjoint checks.

An important complication is symmetry. The older whole-footprint reader can
quotient traversal aliases using their actual edge set. A selected landmark
may distinguish those aliases. The extension therefore enumerates relevant
path/cycle alignments and equal-profile star leaf correspondences, preserving
different actual site-role assignments. Parallel-edge permutations with the
same site bindings cannot affect this selected-site vocabulary and need not be
enumerated separately. Different mappings never manufacture different physical
edge footprints.

## Quantifiers and identity boundaries

These distinctions are part of the contract, not presentation details:

- Sharing selected sets means **at least one common site**. Selecting both
  endpoints in one set asks whether either endpoint participates. L02 uses two
  separate singleton constraints in one joint assignment to require both ends.
- An endpoint belongs to the selected path motif. After attachment, that site
  need not have degree one in the larger observation.
- An internal vertex is between selected path endpoints topologically. It is
  not automatically a geometric centre or equal-distance midpoint.
- L03's corner is relative to the advertised heading frame. It does not
  demonstrate arbitrary rotation-invariant corner naming.
- Each question independently searches its assignments. Reusing a role slot
  across questions does not retain the previous occurrence binding. The
  gallery accumulates answer facts; it does not pin a previous witness as a
  future input. Simultaneous identity requirements belong in one compound
  question. The present examples have one relevant path, and their refined
  questions imply the earlier contact requirements.

A witness-continuation surface would be needed when a later question must
refer to one particular previously selected occurrence. That is a useful next
experiment; it should preserve explicit selection and allow alternative
bindings rather than introduce a hidden first-match commitment.

## Search, incomplete evidence and work

The extension separates the native role-assignment budget from a native
correspondence-enumeration budget. A successful certified joint assignment
supports Yes. No requires sufficient completed motif search, relevant mapping
enumeration and joint assignments, or a separately proven missing component.
An exhausted search cannot establish absence.

Four controls remain Open:

1. Complete motif inventory but zero joint-assignment capacity.
2. Unsearched motif inventory.
3. Complete motifs but zero correspondence-enumeration capacity.
4. An open, unpublished landmark selection.

An explicitly closed empty selection is different from an open selection. The
empty set has no common site; it does not stand for unknown endpoints. Foreign
pattern-site identities and malformed role participation are Blocked.

Correspondence preparation is eager before joint assignment. The budgets bound
the stated enumeration stages, not motif induction, every allocation or total
runtime. This run supplies no training or throughput claim. The large native
archive retains exact case executions and proof histories; it is not a compact
concept payload or a recommended large training-data format.

## Visual presentation

Both composition galleries now separate answers from candidate retention:

- All drawings remain full opacity on the same paper background.
- Plain Yes/No/Open text gives the answer to the current question.
- Blue/slate badges explicitly say Untested, Retained, Only retained candidate,
  Unresolved or Excluded at step N.
- Each card compares observed and candidate answers for every step. A current
  agreement does not erase an earlier difference; the exclusion explanation
  points to that earlier step.

Red/green card fills are removed. A No answer can support retention just as a
Yes answer can; colour no longer suggests otherwise.

## Reproduction

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --occurrence-composition
dotnet run --project experiments/QualitativeFrieze -c Release -- --landmark-composition
dotnet test Tests/QualitativeExecution.Tests --filter "FullyQualifiedName~OccurrenceComposition|FullyQualifiedName~OccurrenceLandmark|FullyQualifiedName~LandmarkComposition"
node experiments/QualitativeFrieze/tools/check-occurrence-composition.cjs
node experiments/QualitativeFrieze/tools/check-occurrence-composition.cjs experiments/QualitativeFrieze/results/landmark-composition/gallery.html
dotnet test Gojo.slnx
```

The 15 new checks cover selected endpoints, actual middle contact, oriented
corners, shared two-end obligations, native transport, foreign/open/empty
selection, symmetric leaf alternatives, footprint identity, separate budget
accounting and retained midpoint ambiguity. Together with the preceding round,
44 focused composition checks pass.

The saved landmark archive restores 928,776 native participants with zero
measured numeric participants. Its 8,586,812-byte gzip retains full generation,
matching and comparison evidence. Browser checks cover all four cases, 12
stages, four Open controls and 17 local references at desktop and mobile
widths, with no console errors or horizontal overflow. The updated original
gallery separately passes its five-case, 12-stage checks.

Full solution verification passed all 4,553 tests across 12 projects, including
the 15 new checks. The complete qualitative test project passed 2,123 tests;
the focused composition subset passed 44. All generated artifacts are local.
