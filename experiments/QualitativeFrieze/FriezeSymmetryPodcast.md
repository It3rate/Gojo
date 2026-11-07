# Gojo frieze symmetry and the machines behind patterns

Podcast script for two voices, approximately 24–28 minutes with pauses.

**Production note:** HOST asks questions; GUIDE explains. These are scripted voices, not quotations or interviews with the cited authors. Links are show notes and need not be read aloud. The opening establishes the current Gojo experiments; the later scorecard and learning loop are proposals. No new learning experiment accompanies this script.

**HOST:** Imagine a narrow decorative border. Four angular strands travel across it. Some stretches look like elongated hexagons. Others make little diamonds. The strands cross, disappear underneath one another, and reappear.

There is repetition everywhere. But repetition of what?

The black lines? The white openings? The turns? The order of crossings? Or the little machines that could have drawn all of it?

Today we are using six research sources to think through that question, and what it means for Gojo. The ambition is to understand patterns well enough to judge them, change them, and eventually generate useful new ones.

**GUIDE:** And the useful starting point is to separate three achievements. We can explain a drawing compactly. We can predict how a construction will behave. And we can make something a person likes.

Those achievements can support one another. They need different evidence.

**HOST:** Where is Gojo already doing some of this?

**GUIDE:** In the recent symmetry-action study, a selected piece of a pattern can be passed through a connected transformation and compared with another piece. For some courses, that transformation can be applied repeatedly, with each generated result feeding the next application.

That last detail matters. The system cannot quietly look up the next observed piece and correct itself every time.

There are also checks that distinguish matching coordinates from matching connections. Two sections can meet on screen while remaining separate native sites. A transformation can preserve one relationship and fail the other.

These are bounded experiments on declared reconstructions. They give us inspectable mechanisms and useful failures. They do not yet give us an autonomous judge of ornament or recover the original artist's machine. [Gojo symmetry-action study](https://gojo.inklingai.ca/ornaments/symmetry-actions/)

**HOST:** So let us begin with how to find a symmetry at all.

**GUIDE:** Our first paper for that is *Partial and Approximate Symmetry Detection for 3D Geometry*, by Niloy Mitra, Leonidas Guibas, and Mark Pauly.

Its central move is to collect evidence about transformations. Compatible local features suggest mappings. When many suggestions agree on a similar transformation, that becomes a candidate. A verification stage then checks the corresponding regions and their mismatch.

The symmetry can involve only part of the object, and it can be imperfect. The paper also reduces detected relationships to a compact collection of generators, with other transformations expressed as compositions. Detection still depends on sampling and tolerances; tiny or noisy features are harder to recover.

This is a method for finding geometric relationships, rather than judging beauty or establishing how an object was historically made. [Mitra, Guibas and Pauly, 2006](https://graphics.stanford.edu/~niloy/research/approx_symmetry/approx_symm_sig_06.html)

**HOST:** How would that change our approach to the meander?

**GUIDE:** Here is a Gojo proposal. Let a corner suggest a relationship, then ask neighboring sections whether they support it.

One corner says, “Reflect me and move me along the border.” The following section agrees. Then the next bend agrees. Eventually we have an explanation covering a meaningful piece of the course.

A single matching corner is weak evidence because corners are common. A rule that predicts the sequence around it is more informative.

And we keep asking what matched. The occupied line? The order of its endpoints? Its connections? Its directed traversal?

**HOST:** This is where orientation and direction come back in.

**GUIDE:** Yes. In a chosen horizontal and vertical frame, flipping the vertical coordinate changes upward movement into downward movement. It leaves a rightward horizontal movement rightward. Geometrically, that is reflection across a horizontal axis.

Reversing traversal is different: we visit the sections in reverse order and reverse each directed section. A half-turn is different again.

The current Gojo action experiment exposes those distinctions through a small local code: exchange the axes, oppose either basis direction, and optionally reverse traversal. Displacement is additional structure. That code summarizes connected transformation rules; it is not a new universal interpretation of the original ODV bits. [Gojo action encoding](https://gojo.inklingai.ca/ornaments/symmetry-actions/)

This lets the same visible outline support several different relationships. That is valuable information about what can change next.

**HOST:** Is that also the distinction between invariance and equivariance?

**GUIDE:** Invariance means a selected read stays the same. Reflect a closed loop and it remains closed.

Equivariance means the result changes in the corresponding way. Imagine a completion machine: mirror its input, let it complete the pattern, and compare that result with mirroring its original completion. If those agree under the declared mirror action, the completion behaves equivariantly in that test.

That is a test of a machine's behavior, beyond finding symmetry in one picture. One successful test would still leave other inputs and transformations to examine.

**HOST:** And what about the bigger shapes? A corner is one thing. An entire pair of interwoven strands seems like another level.

**GUIDE:** That brings us to *Symmetry Hierarchy of Man-Made Objects*, by Wang and colleagues.

They organize parts using two distinct operations: grouping parts that are symmetric, and assembling parts that are connected. Repeating these operations produces a hierarchy that can support structural editing.

The rules favor certain organizations, including compact representations and preserved symmetries. But those priorities are designed heuristics. The authors do not claim an optimal or uniquely meaningful hierarchy. Initial segmentation matters, and symmetric parts do not automatically reveal their function. Even the semantic labels shown in some illustrations are explanatory labels, not discoveries made by the algorithm. [Wang and colleagues, 2011](https://www.cs.sfu.ca/~haoz/pubs/wang_eg11_symh_reduced.pdf)

**HOST:** So for our weave, we could inspect a strand, a reflected pair, and then two pairs together.

**GUIDE:** Exactly. That is a promising Gojo construction. Each selection could expose useful relationships without making every other reader traverse all its sections.

But our overlapping T-shaped reads reveal an important complication. A useful hierarchy cannot be the only permitted organization.

In the A2 example, neighboring larger outlines share three actual source sections. In B6, the corresponding selections share seven. We are reading the same participation in more than one context. We are not drawing those sections twice. [Gojo overlapping reads](https://gojo.inklingai.ca/ornaments/symmetry-actions/)

One reader may follow the whole winding course. Another sees alternating T-like openings. Another inspects the boundary of a white passage.

I would retain those as overlapping candidate organizations. A chosen hierarchy can make one task fast. It should not erase another useful interpretation.

**HOST:** Does sharing a section make those interpretations redundant?

**GUIDE:** Sometimes partly. They can still answer different questions. The important accounting rule is that shared evidence is not independent evidence.

If three descriptions all rely on the same horizontal section, we have not collected three new observations of that section. We have found three uses for it.

That distinction will matter when we score a pattern.

**HOST:** Let us make white space a full participant. How do we find its shapes mechanically?

**GUIDE:** The CGAL source is useful here. It is documentation for a computational-geometry package, rather than a research paper about aesthetics.

An arrangement describes how curves divide a surface into vertices, edges, and faces. It keeps their incidence relationships separate from the geometry of their placement. Each edge has two directed halfedges; following boundary links lets a reader walk around a face or a hole.

Its history extension can also retain which input curves contributed to the resulting edge pieces, including overlaps. This is correspondence with supplied inputs, not recovered artistic intention.

The package makes boundaries and regions computable. It still needs geometric operations, and it does not decide whether a crossing transfers flow or whether the result is attractive. [CGAL 2D Arrangements manual](https://doc.cgal.org/latest/Arrangement_on_surface_2/index.html)

**HOST:** What would we borrow conceptually for Gojo?

**GUIDE:** A white opening should have a boundary we can follow and a relationship to the strands that bound it.

But we need to specify the question. A region between zero-width centre lines is different from the visible clearance between thick black strokes. Increasing stroke width can close a passage even when none of the centre lines move.

An opening may also connect to the outside. If our image crop cuts it off, we must not invent a closed region at the crop boundary.

For the weave, I would begin with selected strand pairs. Read the openings they produce, then read what changes when the other pair is included. That preserves the larger hexagon-like organization that can become hard to see in the combined drawing.

Equal gap width is a further claim. Topology can tell us that a passage connects two regions. Establishing equal clearances requires an appropriate distance or calibration relationship.

**HOST:** So white space gives us another perspective, but it does not magically add measurement information.

**GUIDE:** Right. It gives us another place to express what is known, unknown, open, or shared.

**HOST:** The crossing itself still seems tricky. It is an intersection in the picture, but the strands should continue through it.

**GUIDE:** That is the strength of the next source, *CelticGraph: Drawing Graphs as Celtic Knots and Links*, by Eades and colleagues.

They start with a planar graph whose vertices each have four incident edge ends. In a selected plane embedding, opposite positions around each vertex are paired. Following those continuations partitions the edges into threaded circuits.

For that setting, they establish a unique circuit partition and the existence of a consistent alternating over-and-under assignment. They then address the drawing geometry, using controlled curves to make the strands readable. They also show why minimizing curvature alone can produce unwanted crossings unless the construction is constrained.

These results apply to their specified graph setting, not arbitrary branching networks, and they do not establish a universal aesthetic metric. [CelticGraph, 2023](https://arxiv.org/html/2309.02852v2)

**HOST:** For Gojo, that sounds like a small local machine with surprisingly large consequences.

**GUIDE:** Yes. Imagine four entrances at a crossing. A continuation relation specifies which exit follows each entrance. A separate relation says which strand passes above. Another reader follows the white boundary around the crossing.

The projected crossing, the strand continuation, and the depth relationship are different structures or reads. Flow transfer between the strands would require yet another explicit connection.

This also helps separate two things that look identical. One black X may be two strands passing over and under. Another may be a four-way junction. A third may be two corners touching in projection.

If we reward all three for being an X without retaining those differences, our generator loses the machinery we wanted to learn.

**HOST:** And we still have to check the repeating seam?

**GUIDE:** Yes. For our proposed frieze machines, a crossing rule needs a declared boundary contract. Following a strand around a repeat must return with compatible continuation and depth requirements. A contradiction should remain visible rather than being repaired by the renderer.

**HOST:** We now have a lot of possible relationships. How do we decide which explanations are important?

**GUIDE:** Peter Grünwald's *A Tutorial Introduction to the Minimum Description Length Principle* gives us a useful framework.

In the simple two-part picture, we pay for describing a model and for describing the observations left unexplained by that model. A flexible explanation may fit closely but cost more; a short explanation may leave many exceptions. Refined MDL handles model-family flexibility more carefully than this introductory picture.

Practical comparisons depend on the available model language and coding choices. Finding the shortest arbitrary program is not generally computable. An economical explanation can be useful for prediction without being the true historical cause of the data. [Grünwald's MDL tutorial](https://arxiv.org/pdf/math/0406077)

**HOST:** Give me a concrete Gojo comparison.

**GUIDE:** Take the opposed hooks in the A2 study. One candidate uses a short hook and repeatedly flips its vertical coordinate while advancing. Another uses the larger pair of hooks and translates that pair.

Both can reproduce the observed course. We should compare their complete executable descriptions: seed, transformation, displacement, controller, seam connections, and any exceptions. Counting only seed sections would miss much of the mechanism.

For the four-strand weave, we would also charge for coordination. Two tiny strand machines can require an elaborate timing arrangement. Their small individual size would not make the complete explanation small.

The current Gojo audit finds short seeds within a declared family. A measured description-cost comparison would be a further experiment, not a new name for the existing seed count. [Gojo bounded engine audit](https://gojo.inklingai.ca/ornaments/symmetry-actions/)

**HOST:** Could this explain why some symmetries feel more important than others?

**GUIDE:** It gives us a hypothesis to test. A relationship may be important because it accounts for many otherwise separate choices.

Suppose one phase relationship between two machines predicts the crossings, the alternation of openings, and the placement of larger motifs. That relationship has substantial explanatory reach.

A matching corner already implied by the same machinery adds less new information. It remains a valid symmetry, but it should not earn unlimited extra credit just because we can enumerate it in many overlapping windows.

And the important relationship need not occupy much ink. A small seam connection may control whether the entire construction continues coherently.

**HOST:** This starts to sound like a quality metric.

**GUIDE:** A structural-quality proposal, yes. We still need the sixth source before calling it a taste metric.

Jon McCormack and Camilo Cruz Gambardella's *Complexity and aesthetics in generative and evolutionary art* compares complexity measures across three generative-art datasets. The most useful measures differ between datasets and between evaluation questions.

The paper also distinguishes an artist's ratings from broader audience preference. In its 201-person survey, it found no significant correlation between perceived complexity and aesthetic value across the three datasets. The authors see a role for selected measures in coarse filtering, while finding them insufficient as complete aesthetic judges.

That result concerns those datasets and participants. It challenges the idea that a single complexity measure can serve as a universal beauty score; it does not show that preference is impossible to learn. [McCormack and Cruz Gambardella, 2022](https://link.springer.com/article/10.1007/s10710-022-09429-9)

**HOST:** So a very regular fence might be easy to explain and still be boring.

**GUIDE:** And a deliberate irregularity might make a border more appealing. Those are possibilities our Gojo experiments should allow.

I would begin with a profile of judgments rather than one winning number.

First, does the construction satisfy its declared commitments? Are required joins present? Are the crossings consistent? Are passages that must remain open actually open?

Second, how economically does its machinery account for the selected evidence?

Third, does that machinery predict something we hid, or predict the consequences of an allowed change?

Then we can ask what people prefer in a particular context, and whether the collection has become repetitive even though every individual sample is coherent.

**HOST:** How would symmetry per stroke fit into that?

**GUIDE:** I would keep it as a diagnostic to investigate, with some controls.

If we split every straight section into ten smaller sections, the physical design has not changed. A useful score should not swing wildly because its storage segmentation changed.

Likewise, adding identical repeats increases the number of pairwise matches. That should not produce unlimited quality just because the border is longer.

And mirrored corners, mirrored openings, and a mirrored whole motif may partly reflect the same underlying relationship. We should record their shared support.

These are concrete ways to test whether the score notices organization or merely counts opportunities to match.

**HOST:** What would be the most revealing experiment with our existing weave?

**GUIDE:** Keep one baseline and change one relationship at a time.

Reverse a selected traversal while retaining its visible support. The geometric reader should still recognize the picture; the directed reader should notice the change.

Change an over-or-under choice. The centre-line geometry can remain identical while the crossing-order check changes.

Thicken the strokes. The native centre-line connections can stay fixed while the white clearances shrink or disappear.

Shift one strand's phase. The individual strand engines may still work perfectly, while their combined crossings and openings change.

Finally, replace one generating construction with another that produces the same initial drawing. Then change an input. That exposes differences in potential behavior that the first image could not reveal.

**HOST:** This gives us failures that are specific enough to learn from.

**GUIDE:** Exactly. “The score got worse” is a thin signal. “The strand remains continuous, but its phase no longer predicts the white openings” tells us where the disagreement lives.

We should also hide evidence before fitting the candidate. For example, learn from part of a border and ask the candidate to continue it without seeing the withheld turns. For broader transfer, hold out whole pattern families rather than another crop of the same repeat.

A candidate that fits everything after seeing everything has passed a reconstruction test. Prediction asks more of it.

**HOST:** How do multiple machines communicate in this picture?

**GUIDE:** Our proposed machines could expose small surfaces: phase, a boundary encounter, an occupied passage, a request for clearance. A receiver would respond through a local law without needing a complete model of its partner.

We could compare an independent pair with a coupled pair. Does the communication help predict or preserve crossings and gaps across perturbations? What does that communication cost? Does it help one pattern family while making another worse?

That would test whether coupling is useful. The existence of a channel alone would not count as a success.

In the current weave analysis, the inner and outer strands can share a recurrence action while retaining different seed words. Sharing a rule is already a meaningful similarity. It does not require the detailed strands to be copies of one another. [Gojo weave action comparison](https://gojo.inklingai.ca/ornaments/symmetry-actions/)

**HOST:** Where does settlement enter? Could a nearly hexagonal opening encourage the strands to become more hexagonal?

**GUIDE:** It could, if we explicitly build the response mechanism.

A reader first identifies a discrepancy under a selected claim. Perhaps two gaps are expected to correspond, and they do not. That discrepancy is evidence; it does not automatically move anything.

A connected response would specify which phase, reach, or site may change, how that change enters the local structure, and what must remain intact. It would retain blocked change when another relation prevents the requested adjustment.

Then the higher-level opening can influence lower-level structure through an inspectable route. The lower-level structure can change what the opening reader sees in return.

This is the difference between a visible feedback mechanism and an evaluator secretly redrawing the ornament to improve its own score. The recent action study sketches this settlement direction but does not claim to have implemented the general mechanism. [Gojo settlement direction](https://gojo.inklingai.ca/ornaments/symmetry-actions/)

**HOST:** And the eventual discriminator and generator?

**GUIDE:** I would let the discriminator return a profile with witnesses: what matched, what failed, which evidence was reused, and which questions remain unresolved.

The generator would propose connected machines and their couplings, then produce drawings by running them. A training model could help propose or select those structures. The deployed structure would need to stand on its own and expose the rules responsible for its output.

We would train with contrasting examples: a coherent pattern, a controlled break, and an intentional variation that should remain acceptable. Preference comparisons could teach which tradeoffs people favor. Structural checks would retain their own meanings instead of changing whenever taste changes.

That gives us room for simple designs, elaborate designs, and several successful construction philosophies.

**HOST:** What can plausibly transfer beyond ornament?

**GUIDE:** The reusable machinery may concern alternating roles, repeating actions, maintaining separation, sharing a resource, or coordinating independent participants.

An over-under alternation could suggest a scheduling alternation, for example. But the transfer needs explicit new bindings and constraints. A visual crossing does not automatically mean two tasks compete for a resource.

What could become broadly useful is the ability to compare connected explanations, retain their unresolved differences, and test what they predict under change. Whether that improves another domain is an experiment to run.

**HOST:** So our central question becomes: which relationships explain why the parts work together?

**GUIDE:** Yes. Then we ask which explanations continue to work when we perturb them, which preserve the qualities we care about, and which can be reused somewhere else.

The ornament is useful because we can see the consequences. A misplaced turn, a closed white passage, or a broken crossing rhythm gives us something concrete to inspect.

And when one small connected mechanism explains several of those relationships together, we have a candidate concept worth keeping and testing.

**HOST:** We can appreciate the pattern while also asking what it knows how to do.

**[End]**

## Production and research notes

The episode's proposed quality profile separates structural validity, explanation cost, predictive utility, response to perturbation, coordination utility, and contextual preference. These are experimental objectives, not a validated combined metric. The generator/discriminator language describes a possible architecture; it does not imply that a GAN is required or that an autonomous learning system was implemented in this pass.

The Gojo behavior described here is grounded in the published [symmetry-action study](https://gojo.inklingai.ca/ornaments/symmetry-actions/), the [centre-line atlas](https://gojo.inklingai.ca/ornaments/centreline/), and the [weave study](https://gojo.inklingai.ca/ornaments/weave/). The corresponding local sources are [SymmetryActionStudy.md](SymmetryActionStudy.md), [CentrelinePatternStudy.md](CentrelinePatternStudy.md), and [AngularWeaveStudy.md](AngularWeaveStudy.md). Current implementation checks include `SymmetryActionStudyTests`, `CentrelineActionTests`, and their associated construction code. The script does not equate an experimental compact action code with the entire Gojo kernel.

The six research sources are linked next to their explanations. Their contributions are complementary: detection, organization, region representation, strand construction, explanation selection, and empirical evaluation of aesthetic proxies. The Gojo proposals are a synthesis to test, not results claimed by those sources.
