# Rhythm as a grammar for friezes

This checkpoint extends the pulse interpretation with authored correspondences
from rhythmic roles to whole movement phrases. A selected phrase can go forward,
turn, reach backward, nest a turn and return to neutral vertical displacement.
It produces fret bands through the existing qualitative phase machinery.
No kernel primitive or stochastic search is added.

The source rhythm does not determine a unique drawing. A finite local law
selects the interpretation explicitly. Several examples share the same onset
source with different laws; another comparison shares a law across different
onset sources. A separate phrase-state cycle can change neighboring motifs.
These are exploratory authored choices, not discovered musical or aesthetic
laws.

## Structural contract

```text
onset binding = actual source slot -> actual hit/rest participant
state schedule = Closed(actual source slot -> actual phrase state)
phrase law = finite law over (onset role, phrase state) -> phrase support
phrase = Closed(ordered stroke words)
stroke word = Closed(actual motion occurrences)

source slot + scheduled state
  -> witnessed finite phrase-law read
  -> selected actual phrase
  -> individual output phases and actual emission-law rows
  -> execution receipts and applications
  -> separately committed stroke samples
```

The source-state schedule is checked against actual recurrence edges including
its closing seam. Every phrase word becomes a separate output phase; summing
the entire phrase into one sample would remove its corners. Reusing a template
retains its word and reach identities while actual output phases, execution
receipts and samples remain separate occurrences.

The builder compiles an offered finite source into a `FriezeFixture.Sequence`.
Its native correspondence retains source, selection and output evidence.
This is a captured compilation, not a general live dependency engine. Changing
the source requires a new witnessed application; the old result remains its
old interpretation. There is no automatic audio input, streaming beat tracker
or adaptive choice of grammar.

## Geometry and fill

The curated grammar uses shared unmeasured horizontal and vertical reaches.
Repeated participation expresses wider reaches without assigning source
coordinates. Phrases advance horizontally and return to neutral vertical
displacement. This does not assert shared physical site identity. Backward
motion remains useful inside a phrase: a corner separates
it from the preceding forward stroke.

Validation separates the 64-stroke phase cap, positive horizontal/neutral
vertical periodic displacement and immediate retracing from exact measured
overlap/crossing checks across periodic neighboring copies. A local reversal
certificate alone cannot certify the complete band.

The gallery supplies H4/V4 and a square brush of width 2. A separate
`RibbonPartitionSupplier` supplies exact dark/light cell occupancy in an
aligned rectangular strip, including periodic boundary copies. Every source
pitch is the sum of a shared band and gap extent, both interpreted as 2.
Its strip uses four observer cells of padding from the centerline extrema,
leaving two white cells beyond the brush on each side. The
diagnostics record all bounds and both role counts. Strip heights differ
between patterns, so percentages describe the declared view rather than one
common optimization objective. Equal band/gap extent does not imply equal
black/white areas or a role-exchanging symmetry. Rails only frame the display
and are excluded from fill and legality reads.

Phrase colours are a local palette over actual selected phrase identities.
Every sample is attributed through its actual accepted phase receipt and
compiled phase to its source selection. Display inversion changes the view; it does
not establish native duality or a color-exchange correspondence.

## Recorded comparison

| Interpretation | Source slots | Stroke phases | Reach participations | Phrase identities | Dark fraction |
| --- | ---: | ---: | ---: | ---: | ---: |
| 3–3–2, folded keys and connectors | 8 | 26 | 32 | 2 | 38.1% |
| Same onsets, nested keys | 8 | 35 | 80 | 2 | 40.0% |
| Same onsets, square rest phrases | 8 | 41 | 47 | 2 | 41.2% |
| Same onsets, alternating phrase state | 8 | 26 | 36 | 3 | 37.5% |
| Alternating onsets through that same law | 8 | 32 | 40 | 2 | 41.7% |
| Four slots, nested and folded phrases | 4 | 34 | 68 | 2 | 42.5% |

All six settle the structural and selected measured reads with zero periodic
centerline overlaps and crossings. In the alternating-onset example every hit
meets the same phrase-state face, so the broader key available in the law is
never selected. That is a useful phase/source interaction, rather than an
instruction to draw a different named pattern.

All six currently certify translation only under the measured symmetry read.
The fifth has a 16-source-tick fundamental ink period inside its 64-tick control
translation; the sixth has a 32-tick ink period inside 64. Repeated appearance
does not collapse their distinct phase occurrences or source slots.

The combined unmeasured source and retained execution/selection proofs contain
69,693 elements, zero carriers and zero numeric locations. Per-example source
proofs contain 9,037–15,519 elements; the complete measured cell/geometry views
contain 669,400–1,559,788. These are identity-visited graph counts, not packed
storage sizes or benchmark results. The large field proof is optional; it is
not required to choose or execute a phrase.

## What this establishes

The existing pre-numeric machinery can carry a reusable turn grammar, choose
phrases from source roles and phase, and execute the resulting complex band
while retaining correspondence. Rhythm can control structural grouping and
motif identity rather than only vertical excursions. Numeric geometry remains
an optional interpretation used here for display, fill and stronger legality
checks.

An onset slot can expand into many strokes while a rest expands into one.
That preserves source order, not duration, acoustic meter or proportional
3–3–2 spatial spacing. A duration-preserving interpretation needs a separately
declared time or reach allocation. The grammar's geometry is authored; it is
not implied by hit/rest identity alone. This checkpoint supplies no universal
fitness score or preference learning.

## Reproduction

Verification passed all 3,187 tests across 12 test projects, including 757
qualitative-execution tests and 10 new phrase-compiler tests. The focused tests
cover actual source/read/output correspondence, phase wrap, separate corner
samples, reused reaches, open and foreign inputs, missing or competing laws,
duplicate bindings and rejection of copied member receipts. All six measured
gallery applications pass their selected geometry and fill checks. Both
monochrome and phrase-colour previews were visually checked at 1460×1930.

The fixture still admits at most 64 emitted stroke phases and the selected
orthogonal cell reader at most 256 cells along either axis. Exceeding the
compiler's phase capacity raises its documented API precondition; it does not
truncate a phrase or turn the remainder into a rest. The public compiler also
permits explicit empty or multi-axis words; this curated orthogonal gallery
does not claim its cell renderer applies to every such custom phrase.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --rhythm-friezes
dotnet test experiments/QualitativeExecution.Tests -c Release --filter FullyQualifiedName~RhythmFriezeLibraryTests
dotnet test Gojo.slnx -c Release --no-restore --verbosity minimal
```

The [gallery](../../experiments/QualitativeFrieze/results/rhythm-friezes/gallery.html),
[standalone SVG](../../experiments/QualitativeFrieze/results/rhythm-friezes/gallery.svg)
and [phrase-colour SVG](../../experiments/QualitativeFrieze/results/rhythm-friezes/phrases.svg),
plus [diagnostics](../../experiments/QualitativeFrieze/results/rhythm-friezes/diagnostics.json),
retain the exploratory comparison. The earlier
[rhythm/diagonal experiment](GojoRhythmPatternExperiment.md) remains a separate
checkpoint for additive drives and pulse mappings.
