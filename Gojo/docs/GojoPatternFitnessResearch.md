# Pattern fitness: evidence and a bounded experiment

Research note, 2026-10-16. The papers below are primary empirical studies.
The proposed Gojo feature vector and experiment are **exploratory**. They are
not an implemented human-preference predictor or a validated aesthetic score.

Research supports examining recurrence, organized variation, symmetry and
expectation together. It does not supply one law saying that more symmetry,
less complexity, more regularity, or a particular amount of surprise always
makes a pattern better. A useful first experiment should expose several
features and their counterexamples, then retain the selected preference as
an explicit premise.

## Six useful primary studies

| Study | What was actually tested and found | Boundary for this experiment |
|---|---|---|
| [Witek et al. (2014), groove, movement and pleasure](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0094446), DOI `10.1371/journal.pone.0094446` | Sixty-six participants rated fifty synthesized drum loops. The loops used a fixed tempo and contained no performance microtiming. Moderate syncopation received higher average pleasure and movement-desire ratings than the extremes. A syncopation model explained the ratings better than the tested joint audio-entropy measure. | This concerns these groove stimuli and listeners. Syncopation is organized conflict with a metrical reference; it is not arbitrary timing noise. The result supplies neither a universal complexity optimum nor a formula for visual beauty. |
| [Sioros et al. (2014), controlled syncopation transformations](https://www.frontiersin.org/journals/psychology/articles/10.3389/fpsyg.2014.01036/full), DOI `10.3389/fpsyg.2014.01036` | Two experiments, with 28 and 22 participants, manipulated piano-melody event positions. Moderate transformations produced more groove than unmodified or maximally displaced versions; density controls had weaker effects. The placement of salient syncopations mattered beyond their total amount. | A phase offset against another layer differs from a local exception within an established phrase. An offbeat count alone cannot reproduce the study's result. The experiments do not show that introducing random jitter improves groove. |
| [Mather et al. (2023), joint visual symmetry and complexity](https://pmc.ncbi.nlm.nih.gov/articles/PMC10700581/), DOI `10.1038/s41598-023-47835-w` | In the analyzed sample of 106 participants, people rated five sets of 49 generated images spanning symmetry and complexity. Individual preference maps differed. Many, but not all, participants grouped around symmetry-oriented or simplicity-oriented preferences; dislikes also differed. | These were generated colored images, not Gojo ribbons or every kind of art. Keep symmetry and complexity as separate features. A single fixed ratio of order to complexity cannot be assumed to represent every observer. |
| [Cheung et al. (2019), chord expectation and pleasure](https://www.marcus-pearce.com/assets/papers/CheungEtAl2019.pdf), DOI `10.1016/j.cub.2019.09.067` | A behavioral experiment with 39 listeners used 1,039 chords from thirty progressions. A statistical model trained on a Western pop corpus estimated uncertainty before a chord and surprise afterward. Pleasure depended on their interaction: low uncertainty with high surprise, or high uncertainty with low surprise, could be preferred. | This is conditional harmonic expectation in a particular corpus and stimulus setting. Global symbol entropy is not that model, and random novelty is not automatically pleasurable. Its findings do not establish a universal inverted-U target for all sequences. |
| [McDermott et al. (2016), cultural variation in consonance preference](https://mcdermottlab.mit.edu/papers/McDermott_etal_2016_consonance.pdf), DOI `10.1038/nature18635` | Tsimane' listeners with limited exposure to Western music did not show the consonance preference found in the US comparison groups. Controls distinguished this from task failure and from aversion to acoustic roughness, which was present across the tested populations. | Detecting an acoustic difference and preferring it are distinct. Do not encode Western consonance as innate universal goodness, or collapse roughness, harmonicity, cultural familiarity and chord-progression expectation into one property. |
| [Sievers et al. (2013), shared controls for music and movement](https://www.pnas.org/doi/10.1073/pnas.1209023110), DOI `10.1073/pnas.1209023110` | Participants in US and Kreung Cambodian groups manipulated a generator using rate, regularity, direction, step extent and an auditory-dissonance/visual-spikiness correspondence. Emotion-specific configurations showed common structure across the tested auditory and movement tasks. The authors explicitly did not claim that their five controls optimally span the space. | This supports testing declared cross-modal correspondences. It measured expression of specified emotions, not a universal beauty score, and does not establish that an arbitrary picture has one correct drumbeat or chord progression. |

The papers justify a multivariable investigation with contextual and observer
differences. They do not establish that Gojo duplicates a brain mechanism, that
a visual preference transfers unchanged into music, or that any current Gojo
utility has been validated against listeners or viewers.

The Witek paper has a [2015 correction](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0139409)
covering stimulus indexing and supporting transcripts. The authors state that
the findings and their interpretation are unchanged. Any future reproduction
of its stimuli should use those corrected supplements; this checkpoint's pulse
programs are independently authored examples.

## A small descriptive vector

The following seven-part vector is an **engineering proposal inferred from the
research**, not a fitted model from those studies:

```text
F = (recurrence, reuse, local-change, coordination,
     symmetry, gap-structure, metrical-conflict)
```

Each component should first be a native read with actual participants,
correspondences and unresolved obligations. Counts, ratios and chart values
may then project those reads. The vector need not itself be a stored numeric
object or a scalar reward.

| Feature | Small native question | Qualification |
|---|---|---|
| Recurrence | Which actual controller configuration returns, and which events, words and samples lie between the two occurrences? | A control return, a repeated event category, and a geometric translation period are different reads. A finite observed prefix does not prove unbounded recurrence. |
| Reuse | Which word/template participates again, and which distinct occurrences correspond to it? | Shared source identity, explicitly corresponding patterns and merely equal readings remain separate. A short description can generate an uninteresting constant pattern. |
| Local change | Do consecutive moves preserve, oppose or change their declared face/axis/reach? Where do repeated changes, pauses and reversals occur? | This describes structure at a selected sampling boundary. Geometric angle, speed and acceleration require an appropriate measured interpretation; changing axis alone does not supply them. |
| Coordination | Which contributing streams coincide, interleave, alternate or return to the same phase relationship? | Simultaneous contributions and sequential commits can share a net displacement while drawing and sounding differently. No statistical independence follows from different stream identities. |
| Symmetry | Which supplied maps preserve directed support, and which exchange the two field roles? | Keep geometric isometry, topology-preserving correspondence, role exchange, traversal reversal and Gojo duality distinct. Failed map obligations remain part of the feature. |
| Gap structure | What are the regions, interfaces, runs and ordinal clearance plateaus of each explicit role? | White is participating structure. Equal cell counts are not equal measured area without the relevant calibration. A finer observer can distinguish gaps that a coarse observer merges. |
| Metrical conflict | Under an explicit recurring accent/slot hierarchy, which actual weak-position onsets correspond to omitted or delayed stronger-position onsets? | A declared meter is necessary. An unknown onset is not a rest. Being on a different clock does not by itself establish syncopation or a listener's perceived beat. |

For a later contextual prediction overlay, expose the actual model, context,
candidate support and observation that supplied an expectation. Entropy and
surprisal require probabilities and a chosen model; an unweighted finite
alternative set does not provide them. Deterministic law mismatch can already
be read qualitatively, but should be called mismatch under that law rather
than human surprise.

## Native mechanisms available to compose

The current experiment supplies useful building blocks rather than an aesthetic
theory: [drive analysis](../../experiments/QualitativeFrieze/FriezeDriveAnalysis.cs)
retains actual contributing applications and sampling boundaries;
[motion frames](../../experiments/QualitativeExecution/QualitativeMotionFrame.cs)
retain direction/reach identities and cancellation;
[weave construction](../../experiments/QualitativeFrieze/QualitativeWeave.cs)
and [partitions](../../experiments/QualitativeFrieze/QualitativePartition.cs)
expose both roles and their adjacency;
[field analysis](../../experiments/QualitativeFrieze/QualitativeFieldAnalysis.cs)
supplies selected-role support and ordinal clearance; and
[motif correspondence](../../experiments/QualitativeFrieze/MotifFieldCorrespondence.cs)
retains competing maps, actual label/support evidence and unselected
complements. The existing
[layer experiment](GojoPatternLayerExperiment.md) demonstrates why adding at
every equally quiet location can erase the original pattern.

A complete seven-part fitness observer, a perceptual meter detector, an
empirical harmonic-expectation model and validated aesthetic preference
learning are **not established by these building blocks**. Combining their
reads into the vector above is proposed follow-on work. No domain-specific
kernel primitive is needed for the initial experiment.

## Legality, preference and judgment

Use three separate stages:

1. **Declared legality and completeness.** Check actual event/accounting
   provenance, field labels and topology, selected closure/seam obligations,
   complexity capacity, and any requested no-retracing or crossing policy.
   Measured intersection rules apply only under their selected calibration.
   An unresolved legality question is not a favorable preference score.
2. **Descriptive and exploratory preference reads.** Publish the vector and
   the actual focus, alphabet, period window, observer adjacency and
   calibration used. A selected profile may prefer repetition with occasional
   exceptions, another a quiet regular band, another interlocking complexity.
   Any weights or preferred ranges are authored hypotheses unless fitted and
   evaluated against appropriate judgments.
3. **External assessment.** Keep the judge, criterion, candidate comparison,
   exposure conditions and response connected to the actual proposal. Human
   choice, an AI critic's choice and the system's own declared objective are
   different observations. An AI preference or internal score is not a
   validated human-pleasure measure.

A profile can be useful before human validation, provided its claim is precise:
it selects for its declared structure. Optimization success then means a
proposal better satisfies that profile. It does not independently prove that
the result is more beautiful.

## Counterexamples the experiment should retain

These are **proposed diagnostic cases**, not additional empirical findings:

- A constant pulse or straight band has excellent recurrence and a short
  description. It can suit one purpose and be monotonous for another. Maximum
  simplicity must not win by construction.
- `A B A B A B` and `A A A B B B` have the same symbol counts but different
  transitions, phrases and boundary behavior. Histogram entropy misses that
  distinction.
- A deliberately irregular sequence can be expressive. A complicated-looking
  trace can also be the orderly interaction of two simple recurrences.
  High local variation is not automatically error, ugliness or random noise.
- An exact repeating offbeat against a declared reference is not the same
  structure as continuously changing timing. Conversely, adding many events
  does not establish meaningful syncopation.
- A perfectly symmetric filled field loses role contrast and useful gaps.
  A small local exception may preserve a recognizable motif better than an
  edit applied to every equal-resolution plateau.
- Equal ink/gap counts can coexist with a failed role-exchange correspondence.
  A pretty rendering cannot repair that failed structural claim.
- The same event applications can produce zigzag or staircase paths when the
  commit schedule changes. A static field cannot uniquely recover the clocks,
  application order or original drawing process.
- The same qualitative source can look crowded, sparse or differently balanced
  under different ribbon widths and aspect calibrations. Record those view
  choices instead of silently changing the source preference.

## A bounded visual/rhythm comparison

An initial experiment can hold the native event identities and source laws
fixed while exposing several declared interpretations:

```text
phase/event network -> actual contributing applications
                    -> selected sampling / phrase boundary
                    -> visual path or two-role field
                    -> rhythmic onset / instrument-role observation
```

**Proposed comparisons:** a steady repeated word; a phrase with one displaced
event; two interleaved recurrences; the same contributions committed jointly
or separately; and a controlled pattern edit against an equally sized
unstructured edit. Compare matched windows, keep actual causes and the
unselected complement, and vary one declared connection at a time. Retain
counterexamples alongside selected results. Report descriptive differences
before introducing a weighted rank.

For sound, bind native event/voice participants to an explicit instrument and
time interpretation. Native recurrence need not contain seconds; tempo in
beats per minute belongs to the chosen clock calibration. A chord progression
additionally needs declared simultaneous voices, pitch relations and a tuning
or frequency interpretation. A visual direction or color does not acquire
those meanings by itself. The useful common source is connected event/order/
participation structure with explicit adapters, not a claim that the modalities
are identical or mutually invertible.

Later, randomized paired comparisons could ask separately about liking,
regularity, tension, recognition and desire to move. Those are distinct
questions. Repeated comparisons and held-out patterns would help distinguish
stable personal preference from a renderer artifact or a profile that only
rewards its own examples. No such participant study or preference learning is
claimed by this research note.
