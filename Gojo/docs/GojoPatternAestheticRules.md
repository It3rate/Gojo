# Declared aesthetic profiles for pattern experiments

Research and implementation note, 2026-10-17. The implemented
[profile reader](../../experiments/QualitativeFrieze/PatternAestheticProfile.cs)
evaluates supplied outcome supports against explicit local criteria. It is
not a perceptual model or a learned human-beauty score. The criteria below are
**authored experimental preferences**; their usefulness remains a question for
the generated examples and, eventually, independent viewers.

## What the research warrants

The directly relevant frieze study tested horizontal black-and-white patterns
with flag, comma and texture motifs, then textured variants at three element
sizes. The two types containing mirrors parallel to the strip were preferred
most in both experiments. Symmetry count did not explain the complete ordering.
Larger, fewer texture elements were preferred; those factors were confounded,
and easier recognition of the symmetries remained an explanation to test.
Importantly, filling the pattern region meant distributing patterned texture,
not painting it solid black. These results motivate testing visible motif
correspondences and strip-relative symmetry, not maximizing ink or symmetry
count. [Friedenberg, Martin, Uy and Kvapil (2022)](https://journals.sagepub.com/doi/10.1177/20416695221131112).

Generated-image experiments also found different individual preference maps
over symmetry and complexity. Keep the two dimensions separate rather than
assuming a universal order/complexity ratio.
[Mather et al. (2023)](https://www.nature.com/articles/s41598-023-47835-w).

In tile-production and pattern-parsing experiments, participants produced and
preferred structured arrays over the tested random arrays. Hierarchical
grouping could differ from a simple serial rule despite identical counts of
tile orientations. This motivates reusable motifs and visible relationships;
it does not establish that the shortest program is most attractive.
[Westphal-Fitch et al. (2012)](https://pmc.ncbi.nlm.nih.gov/articles/PMC3367690/).

For simple compositions, measures that best predicted perceived balance were
not identical to those that best predicted preference; homogeneity contributed
separately. Equal ink/gap area, a centered mass and symmetry are different
possible reads. [Hübner and Fillinger (2016)](https://www.frontiersin.org/journals/psychology/articles/10.3389/fpsyg.2016.00335/full).

Contour-detection experiments showed that local alignment affects recognition
of a path among distracting elements. This is evidence about perceptual
grouping, not evidence that all straight continuations are beautiful or that
corners and junctions should be eliminated.
[Field, Hayes and Hess (1993)](https://pubmed.ncbi.nlm.nih.gov/8447091/).
See also the broader [visual/rhythm research note](GojoPatternFitnessResearch.md).

## A practical rule vocabulary

Each observation needs its actual source, selected period/window, focus,
observer basis and any calibration. These suggested categories are outcomes
of explicit feature observers, not hidden pattern-name classifications.

| Question | Useful authored preference | Counterexample or limit |
|---|---|---|
| Does a repeated structure remain readable? | Prefer actual motif reuse together with some organized variation or hierarchy. | A constant line can maximize reuse while supplying no interesting variation. High variation can still come from simple interacting recurrences. |
| Which symmetry is present? | Prefer a selected, verified support correspondence; compare mirror parallel to the strip with half-turn, glide and other maps separately. | More symmetry operations are not automatically better. A failed geometric map cannot be repaired by naming the intended group. |
| Can a path be followed? | Prefer explicit continuation and distinguish ordinary joins, branches, transverse crossings and retraced support. | A designed branch or corner can be useful. Two crossing strokes do not establish a connected material junction. |
| Do neighboring features remain distinct? | Prefer an advertised clearance band or a limited number of crowded junction neighborhoods. | The chosen width, neighborhood and resolution matter. A source without metric calibration cannot establish a physical gap width. |
| Do ink and gap both participate? | Preserve both roles and compare their regions, runs or selected clearances. An intermediate occupancy category can be an authored preference. | Equal counts do not prove equal area or color-exchanging symmetry. No cited study establishes a universal 50% ink optimum. Solid fill erases the contrast being evaluated. |
| Is complexity manageable? | Keep a declared construction/observation capacity, then compare legible variation within it. | Capacity is a hard experiment limit, not an aesthetic law. A tiny description can produce visually confusing structure. |

Three useful **proposed profiles** are a quiet repeat, an articulated band and
an interlocking field. They can share the same observations while preferring
different supports: the first may prefer sparse changes, the second visible
motif changes with clear joins, and the third deliberate branches and connected
gap structure. The generic reader does not implement these observer laws or
select numerical thresholds. Bind the actual observed outcomes and selected
preferences before ranking examples.

Keep hard legality separate: required closure, emission accounting, finite
capacity, forbidden retracing and any chosen crossing policy must be checked
under their own contracts. A branch is not a violation merely because another
profile prefers an unbranched band.

## Native contract and uncertainty

```text
criterion = (feature-surface -> ordered-outcome-domain)
            -> (acceptable-support -> preferred-support)
observation = (actual-criterion -> actual-feature-witness) -> outcome-support
read = (actual-criteria -> observations)
       -> (preferred -> (passed -> (violations -> unknown)))
```

Every verdict retains the actual criterion and input observation. A closed,
nonempty support entirely inside the preferred support proves preference under
that criterion. If all outcomes are acceptable, the coarser acceptable verdict
can settle even when exact preference cannot. Outcomes entirely outside the
acceptable support establish a known violation. Open, missing, empty, foreign,
conflicting, or mixed acceptable/rejected support remains unknown.

The reader checks the supplied support's conformance, not the factual truth of
an arbitrary observer witness. It does not inspect private geometric internals
or invent the missing feature law. Those observations and their own validity
contracts remain separate connected premises.

`Describe` projects counts and distinguishes complete evaluation from an
acceptable result. A complete result may contain known violations. Unknowns
prevent an acceptable aggregate; they are never normalized as favorable zero.
If a presentation later assigns ordinal points or numerical weights, expose
that as a separate authored interpretation, with unavailable ranks left
unavailable. Comparing profiles or asking an AI critic to choose a picture is
useful exploration; neither is independent validation of human preference.

## Connected-pattern profile used in this experiment

The [connected-pattern adapter](../../experiments/QualitativeFrieze/ConnectedPatternScoring.cs)
now supplies four native and four measured criteria. The following ranges are
**authored defaults**, not values fitted by any cited study. Native boundaries
are distinct finite slot scopes linked to the actual observed participants;
the numbers below are a readable cardinality projection.

| Observation | Acceptable | Preferred |
|---|---|---|
| Shared chord-word vocabulary | 2–8 templates | 3–6 templates |
| Endpoint junctions | 0–12 | 0–4 |
| Occupied junction signatures | 1–6 kinds | 2–5 kinds |
| Verified nonidentity graph correspondences | 0–8 | 1–8 |
| Empty probes in the selected measured band | 20–80% | 30–65% |
| Spread of clearance-center layer ordinals | 0–5 | 0–1 |
| Verified measured support isometries | Translation alone suffices | At least one nontranslation map |
| Probes with multiple footprint supports | At most 35% | At most 20% |

Unused coordinate landmarks are excluded from the junction-signature focus.
Graph correspondences are not automatically Euclidean isometries, and a
retained phase map is not included in the graph-correspondence verdict. Probe
fractions and clearance depend on the supplied period, band, quantum, radius
and calibration. Multiple support near ordinary joins is included in the
crowding observation; it is not thereby declared illegal retracing.

The initial gallery profile exposed a useful counterexample: a nearly solid
crossed-brace field ranked 13/14 despite roughly 69% multiple-support probes.
Adding the crowding criterion is a visually motivated revision of that authored
profile, not a newly established perceptual law. The revised adapter has eight
components, with an optional ordinal total of 16: preferred contributes two,
acceptable one, and a known violation zero. Any unknown component makes the
total unavailable. Keep acceptance separate from this descriptive total;
selection can require all criteria to be acceptable instead of letting several
preferences compensate for a known crowded-field violation.
