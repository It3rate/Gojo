# Skeleton atlas

The [visual atlas](../results/skeleton-atlas/index.html) is an authored reference
for discussing pattern construction and choosing experiment targets. It is not
a set of outputs or conformance proofs from an executable Gojo machine.
Coordinates supply readable illustrations; a target's required relations and
allowed variations define what a future experiment should preserve.

- [Glossary](../results/skeleton-atlas/glossary.html): S01–S10, with three
  diagrams per family, individually addressable as S01.A, S01.B and S01.C.
- [Goal catalogue](../results/skeleton-atlas/goals.html): G01–G12, each with
  requirements, allowed variation, a failure example and source attribution.
- [Experiment brief](../results/skeleton-atlas/brief.html): choose a goal,
  declare experiment choices, and copy or print the expectation and evidence.
- [Glossary contact sheet](../results/skeleton-atlas/glossary-sheet.svg) and
  [goal contact sheet](../results/skeleton-atlas/goal-sheet.svg): vector
  references. PNG versions accompany both sheets.

| ID | Goal |
|---|---|
| G01 | Continuous key |
| G02 | Facing meanders |
| G03 | Glide staircase |
| G04 | Half-drop diamonds |
| G05 | Nested diamond courts |
| G06 | Triangular chambers |
| G07 | Octagon and square field |
| G08 | Counterchanged steps |
| G09 | Angular woven band |
| G10 | Herringbone passages |
| G11 | Sixfold star corridors |
| G12 | One scaffold two readings |

The small controls C01–C03 are intentionally limited examples. They can satisfy
selected mathematical properties while missing an adopted target; they do not
declare simple patterns inherently unattractive.

## Rebuild

From the repository root:

```powershell
python experiments/QualitativeFrieze/SkeletonAtlas/build_atlas.py
```

The Python standard library is sufficient. `atlas.json` supplies the labels,
captions and target contracts; `build_atlas.py` and `extra_patterns.py` draw the
reference plates. The builder writes the HTML and SVG files to
`results/skeleton-atlas`. PNG contact sheets are browser captures of those SVGs;
recapture them after changing the drawings.

Construction guides and one-ink views are optional. Shaded regions denote
membership in the illustrated partition. Unbroken contacts meet and dots
highlight selected junction sites; an interrupted crossing depicts separate
over/under paths. Pale solid lines retain a supporting scaffold. These conventions
state the intended reference, not evidence of a native graph or an accepted
material passage.

## Using a target

Choose an ID and the features that matter before a run. Declare scope, allowed
edits, seam and crossing policy, and any metric interpretation. Afterwards,
record each requirement as met, missed or unresolved with its evidence; judge
the intended visual character separately. Exact image matching, fixed aspect
ratio, equal physical gaps and universal beauty are not implicit requirements.

The [historical research](../../../Gojo/docs/GojoHistoricalOrnamentRules.md)
distinguishes the source rules, present native capabilities and experiments
still needed. References labelled structural adaptations preserve a source
mechanism rather than tracing its plate. Authored syntheses are our proposed
targets. No kernel or experimental execution laws are added by this atlas.
