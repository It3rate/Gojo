# Composing motif occurrences into structural questions

Status: implemented exploratory fixture above the existing kernel. The
[interactive gallery](../../experiments/QualitativeFrieze/results/occurrence-composition/gallery.html)
contains five independently executed cases, two budget controls and a small
scaling diagnostic. The existing structural-evidence experiment remains intact.

The earlier questions establish existential motif presence: a closed contour,
a junction, or a short connected walk exists. This vocabulary can treat one,
two and three disconnected copies of the same triangle as equivalent. That is
a useful coarse read, but it cannot establish multiplicity or relationships
between copies. This fixture tests whether native role bindings and explicit
local obligations can supply that next layer without new kernel primitives.

## Five cases and their results

| Case | Question | Result |
| --- | --- | --- |
| C01 | Does a triangle loop exist; are there at least two; at least three? | All three libraries initially agree at the old vocabulary. Successive compound questions retain 3, 2, then 1 alternatives for the two-loop source. |
| C02 | Do two different four-edge contours share a site but no edge? | Selects the joined-corner construction over separate contours and a shared boundary. |
| C03 | Do two different four-edge contours share an actual edge? | Selects the construction that reuses one boundary edge, drawn once. |
| C04 | Does a separate two-edge path share a site with each of two site-disjoint loops? | The three-role question rejects the visibly touching but unjoined sample. It tests whole-motif contact, not endpoint-specific attachment. |
| C05 | Do two different triangle loops have disjoint site sets? | Both near and far arrangements satisfy this question. Their spacing stays ambiguous. |

Sources and candidate examples execute freshly through `UnitArrangementNode`.
The shared-edge sample contains seven actual edges and six sites: the second
contour adds three sides to the first contour's boundary. The bridge alternatives
both contain ten edges, but the accepted endpoint joins reduce eleven distinct
sites to nine. These authored examples vary endpoint joins, but the question
does not select endpoints: a path's middle vertex sharing a loop site could
also satisfy contact. Endpoint-specific obligations remain a reader gap.
Equal coordinates never establish those joins. Source-free
pattern definitions come from separately executed exemplars.

`QualitativeRecognitionPublisher` exposes fresh site and edge identities,
incidence and coarse heading participation. It omits placement words, reaches,
recipe and construction ancestry. Matching reads only this published structure;
the SVG renderer uses private placement after the reads have completed.

This round is a direct local analysis of an explicitly exposed inventory. It
does not extend the earlier authenticated supplier request/reply protocol.
The complete assessment and archive contain the advertised observation and
audit evidence. They are not sparse private-source reply payloads.

## The small set of rules

1. A role requests an actual occurrence of a native motif definition. A caption
   such as “square” does not establish it.
2. Two role slots may bind the same physical footprint unless a difference
   obligation forbids that. Repeating an existential clause does not count copies.
3. Difference compares actual edge sets. Cyclic starting points, reverse reads
   and additional match wrappers cannot manufacture another physical copy.
4. Shared/disjoint site and edge obligations compare the selected actual
   identities. They do not compare drawing coordinates or imply global isolation.
5. One certified assignment establishes a positive existential answer.
   Absence requires sufficient completed motif search and exhaustive relevant
   assignments; a proven missing component can also refute the compound.
6. An unfinished inventory, unsupported motif grammar or unfinished assignment
   search remains Open. Malformed role/constraint participation is Blocked.
7. A candidate remains compatible when its answer agrees with the source,
   including No/No. Compatibility is not an affirmative claim about the shape.

These are local reader contracts over existing relations and placeholders.
They are not proposed universal Gojo axioms. In particular, “different edge
sets” is deliberately narrower than general conceptual occurrence identity:
two endpoint-centred stars over the same line still use one edge footprint.

## Native contract and inspectable evidence

The implementation is
[OccurrenceComposition.cs](../../experiments/QualitativeFrieze/OccurrenceComposition.cs).

```text
role = actual slot -> source-free motif pattern
question = Closed(roles) ->
           (Closed(different-edge-set role pairs) ->
           (Closed(disjoint-site role pairs) ->
           (Closed(shared-site role pairs) ->
           (Closed(shared-edge role pairs) -> Closed(disjoint-edge role pairs)))))

assignment = actual slot/occurrence bindings ->
             (actual site/edge footprints ->
             (native pattern correspondences -> constraint checks))

constraint check = actual constraint ->
                   ((left footprint -> right footprint) ->
                    (shared identities -> (left-only -> right-only)))
```

Each pattern remains the existing star/walk/cycle role graph under a heading
frame. The contour questions request the illustrated heading patterns; they
are not wildcard queries for every polygon under arbitrary rotation. The
question is reconstructed and validated from its native surface;
missing contract sections do not silently become empty constraints. Pair
constraints must refer to actual declared role slots. A third role can bridge
the first two without seeing their generating programs.

The bounded search binds roles incrementally, checks obligations once their
participants are bound, and stops after one successful assignment. Rejected
partial assignments, successful matching correspondences, budget slots and
unfinished reads remain connected. These are non-draining observations. A
failed pattern claim does not automatically become carrier tension.

The study in
[OccurrenceCompositionStudy.cs](../../experiments/QualitativeFrieze/OccurrenceCompositionStudy.cs)
compares each candidate against successive native question answers. The earlier
both-direction existential vocabulary comparison is retained separately to
expose its limit. It is not a graph-isomorphism check or independent ground truth
for multiplicity. No training or learned question-selection policy runs here.

## Bounded work and local measurements

The first checkpoint built the cases, reads and scaling diagnostics in approximately
1.19 seconds. Updated timings are retained in the gallery's diagnostic JSON;
these figures include generation and comparative reads, but exclude SVG
generation, JSON serialization, compression and archive verification. This is
one local run, not a throughput or training-infrastructure forecast.

The scaling diagnostic uses 1, 2, 4, 8 and 16 disconnected identical loops:

| Input | Query | Attempted role extensions | Answer |
| --- | --- | ---: | --- |
| 16 loops | Two site-disjoint loops, budget 8 | 3 | Yes |
| 16 loops | Two loops sharing a site, budget 8 | 8 | Open |
| 16 loops | Two loops sharing a site, budget 4096 | 272 | No |

The positive query can stop after selecting two compatible actual footprints.
The negative pair query tries `n + n²` partial extensions in this enumeration.
On the 16-loop input the saved joint-read times were about 1–5 ms, with individual
timing noise. These are diagnostic points; the invariant work counts and correct
Open result under the smaller budget are the stronger evidence.

The budget bounds attempted role extensions, not all preparation. Per-role
motif matching and option construction are eager. Inventory induction,
allocation and proof retention also consume work outside that budget. Pairwise
identity scans and retaining every failed assignment are simple experimental
implementations, not optimized indexing or streaming. More roles can create a
combinatorial search; budgets preserve an honest unfinished answer rather than
solving that scaling problem.

## What the approach now supports

The demonstrated reusable elements are native motif templates, independent role
slots, explicit footprint difference, shared/disjoint incidence constraints,
incremental assignment, and scoped proof or open search. They provide one
compositional layer between individual motifs and a larger structure. The
bridge case is a small example of describing a relation between recognizable
parts instead of flattening everything into one image-specific equation.

The limits remain substantial:

- At least two/three is a lower bound, not an exact global count.
- A selected pair being disjoint does not prove that the surrounding graph is
  disconnected. A shared site does not establish a specific empty interior.
- Spacing, relative placement order, translation cadence, lattice generators
  and affine correspondence need additional exposed services. They cannot be
  recovered from incidence and headings alone.
- This is bounded composition over supplied motifs, not arbitrary recursive
  concept discovery, unknown-candidate invention, image recognition or learned
  generative equivalence.
- The publication discards ancestry, so the construction recipe remains
  unrecoverable without a separate provenance surface.

A useful next experiment is selection of named pattern landmarks, including
path endpoints and distinguished centres, followed by optional unmeasured
order publication over the same occurrence identities. It could ask whether
corresponding roles alternate or recur along a declared order while preserving
the topology-only read. That would test disposition without introducing calibrated coordinates.
After that, perturb one join and inspect which higher-level assignments survive;
this fixture has not yet demonstrated incremental invalidation or surprise.

## Reproduction and verification

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --occurrence-composition
dotnet test Tests/QualitativeExecution.Tests --filter "FullyQualifiedName~OccurrenceComposition"
node experiments/QualitativeFrieze/tools/check-occurrence-composition.cjs
dotnet test Gojo.slnx
```

The 29 new focused tests cover the native contract, actual execution, traversal
aliases, copied/coincident sites, shared edge reuse, three-role bridges,
incomplete inventories, unsupported grammar, malformed constraints, positive
evidence under unfinished search, bounded assignment and candidate comparison.

The current archive restores 841,606 native participants with zero measured
numeric participants. Its 7,226,654-byte gzip retains complete case executions,
matching proofs and comparison histories. It is an audit artifact, not the
minimal size of a concept or a recommended large training-data representation.

For the first checkpoint, solution verification passed all 4,538 tests across 12 test projects, including
the 29 new focused checks. Browser verification covered five cases, all 12
stages, two controls, 20 scaling rows and 22 local references, with no browser
errors or horizontal overflow at desktop and mobile widths.

The [landmark follow-up](GojoLandmarkCompositionExperiments.md) implements the
selected-site refinement above. Whole-footprint questions retain their prior
answers and assignment counts. Both galleries now use explicit retention badges
and answer-history tables; the earlier red/green candidate fills are removed.
