# Glyph joints: one drawing, several constructions and uses

This study keeps four questions separate: what is drawn, which sites are actually shared, how the pen visits the sections, and which routes a later use permits. It supplies capital letters and digits, alternative constructions, small route machines, and conditional support reads. It does not learn handwriting or infer the intended use from a picture.

An L can be a continuous bend or two attached pieces. Both can have the same ink and final connectivity. An H can be drawn left to right while its later bridge use permits travel both ways. The extra history or route law must be present; the glyph name does not supply it.

Open [the visual atlas](index.html). [Recorded data](results.json), [native glyph constructions](corpus.gojo), [native construction landmarks](construction.gojo), [native route machines](usage.gojo), and [native contextual reads](contexts.gojo) retain the experiment's different evidence surfaces.

## How to read the atlas

1. Choose a glyph and compare its construction variants. Colored sections show pen strokes. Select one stroke to see its green start and red stop. A dashed relocation moves the pen to its next landmark; it does not draw a connection.
2. Examine a joint machine. Its lines show available structure; arrows show explicitly supplied routes. A probe records a move, a block, an unresolved choice, or incomplete evidence.
3. Change the contextual premises. Ground contacts and a load-position projection onto the chosen ground/reference are supplied relations. A projection inside their span is a geometric read, not a proof of physical stability.

## The small structural grammar

| Surface | What is connected | What it does not imply |
| --- | --- | --- |
| Section | Actual start and end sites, with the selected geometric evidence | A drawn crossing is not automatically a shared site. |
| Pen stroke | Ordered visits to actual sections, with a selected traversal direction | Drawing direction is not a one-way traffic law. |
| Construction landmark | A stroke start or stop refers to a site already exposed by earlier drawing, or to an explicitly supplied placement relation | A future section cannot provide evidence for an earlier move. |
| Joint | Several section ends refer to one actual site | Shared attachment alone does not choose a turn or split conserved flow. |
| Route | Exposed input, connected passage and destination | A route is not available merely because two lines touch in the image. |
| Conditional law | Input and control evidence select permitted route candidates | Several candidates are not several executed copies of one packet. |
| Contextual read | Shape, selected ground, contacts, load and projection basis | A positive geometric result does not supply stiffness, friction or mass distribution. |
| Selection | A read chooses which existing sections or evidence to inspect | Unselected is not zero magnitude, nonexistence, or a stopped physical flow. |

ODV remains a low-resolution magnitude read: V = 0 means undetectable at its selected resolution, not removal. The atlas uses native connected relations for sites, history and route laws; it does not claim that one ODV word encodes an entire joint or introduces a new universal joint opcode.

## Glyph catalogue

The baseline constructions follow the supplied teaching cards and the cited school-print guides, with noted choices where conventions differ. Variants distinguish other teaching conventions from authored construction experiments. Curve controls provide one illustrative geometry. Counts concern retained sections and shared endpoints, not every apparent shape in a drawing. Pen-stroke groups record drawing history; they do not supply material stiffness or bolt, hinge or weld laws.

The primary joint reading now follows construction history. A continuous corner in L differs from returning to an existing endpoint and attaching F's top bar. Starting at a known interior point and drawing outward differs from arriving at that point and stopping. The geometric tangent reading remains available alongside that history; it does not replace it.

### A — School-print construction

Start at the apex and descend the left leg. Lift, return to that top endpoint and descend the right leg. Return to an interior landmark on the left leg and join across to the right leg.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 2; independent cycles: 1; pen strokes: 3; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 → s3 | True | False |
| 3 | s4 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p0: Lift and return to an existing endpoint from stroke 1. | 1 | p4: Stop at a newly reached endpoint. | none |
| 3 | p1: Lift and return to an existing interior landmark from stroke 1. | 1 | p3: Arrive at the existing interior landmark from stroke 2 and stop. This stroke joins two landmarks that already existed before it began. | 2 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p3: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 3, p1: Construct a secondary route outward from an earlier interior landmark. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 3, p3: Arrive at the existing interior landmark from stroke 2 and stop. Earlier incidence count: 2; earlier source strokes: 2.
- Stroke 3, p3: Connect two previously available landmarks with this new stroke. Earlier incidence count: 2; earlier source strokes: 2.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (4, 10) → (2, 5) | True |
| s1 | p1 → p2 | (2, 5) → (0, 0) | True |
| s2 | p0 → p3 | (4, 10) → (6, 5) | True |
| s3 | p3 → p4 | (6, 5) → (8, 0) | True |
| s4 | p1 → p3 | (2, 5) → (6, 5) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Construct an attachment by leaving an earlier endpoint after a pen lift. | (4, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Construct a secondary route outward from an earlier interior landmark. | (2, 5) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |
| p3 | Continue through this site without lifting the pen., Arrive at the existing interior landmark from stroke 2 and stop., Connect two previously available landmarks with this new stroke. | (6, 5) | 3 | T-like | 1 | 0 |
| p4 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### B — School-print construction

Draw the stem from top to bottom. Lift and return to its top endpoint; draw the upper bowl back to the middle landmark, then continue the lower bowl to the bottom endpoint.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 0; branch sites: 1; independent cycles: 2; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 → s3 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p0: Lift and return to an existing endpoint from stroke 1. | 1 | p2: Arrive at the existing endpoint from stroke 1 and stop. This stroke joins two landmarks that already existed before it began. | 1 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p1: Reach an earlier interior landmark, then continue this stroke from it. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p2: Arrive at the existing endpoint from stroke 1 and stop. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p2: Connect two previously available landmarks with this new stroke. Earlier incidence count: 1; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 5) | True |
| s1 | p1 → p2 | (0, 5) → (0, 0) | True |
| s2 | p0 → p1 | (0, 10) → (7, 10) → (7, 5) → (0, 5) | False |
| s3 | p1 → p2 | (0, 5) → (8, 5) → (8, 0) → (0, 0) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Construct an attachment by leaving an earlier endpoint after a pen lift. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Reach an earlier interior landmark, then continue this stroke from it. | (0, 5) | 4 | multi-arm | 1 | 1 |
| p2 | Stop at a newly reached endpoint., Arrive at the existing endpoint from stroke 1 and stop., Connect two previously available landmarks with this new stroke. | (0, 0) | 2 | corner geometry | 0 | 0 |

### C — School-print construction

Make one open curved return from the upper right to the lower right; the two ends remain distinct.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p1: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (7, 9) → (-2, 12) → (-2, -2) → (7, 1) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (7, 9) | 1 | endpoint | 0 | 0 |
| p1 | Stop at a newly reached endpoint. | (7, 1) | 1 | endpoint | 0 | 0 |

### D — School-print construction

Draw the stem downwards. Lift and return to its top endpoint, then curve around to stop at the existing bottom endpoint.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 0; branch sites: 0; independent cycles: 1; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 | True | False |
| 2 | s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p1: Stop at a newly reached endpoint. | none |
| 2 | p0: Lift and return to an existing endpoint from stroke 1. | 1 | p1: Arrive at the existing endpoint from stroke 1 and stop. This stroke joins two landmarks that already existed before it began. | 1 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p1: Arrive at the existing endpoint from stroke 1 and stop. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p1: Connect two previously available landmarks with this new stroke. Earlier incidence count: 1; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 0) | True |
| s1 | p0 → p1 | (0, 10) → (9, 10) → (9, 0) → (0, 0) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Construct an attachment by leaving an earlier endpoint after a pen lift. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Stop at a newly reached endpoint., Arrive at the existing endpoint from stroke 1 and stop., Connect two previously available landmarks with this new stroke. | (0, 0) | 2 | corner geometry | 0 | 0 |

### E — School-print construction

Draw the stem downwards. Return to its top endpoint for the top bar, to an interior landmark for the middle bar, and to its bottom endpoint for the bottom bar. Each bar leaves toward the right.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 4; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 | True | False |
| 3 | s3 | True | False |
| 4 | s4 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p0: Lift and return to an existing endpoint from stroke 1. | 1 | p3: Stop at a newly reached endpoint. | none |
| 3 | p1: Lift and return to an existing interior landmark from stroke 1. | 1 | p4: Stop at a newly reached endpoint. | none |
| 4 | p2: Lift and return to an existing endpoint from stroke 1. | 1 | p5: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 3, p1: Construct a secondary route outward from an earlier interior landmark. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 3, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 4, p2: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 4, p5: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 5) | True |
| s1 | p1 → p2 | (0, 5) → (0, 0) | True |
| s2 | p0 → p3 | (0, 10) → (8, 10) | True |
| s3 | p1 → p4 | (0, 5) → (6, 5) | True |
| s4 | p2 → p5 | (0, 0) → (8, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Construct an attachment by leaving an earlier endpoint after a pen lift. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Construct a secondary route outward from an earlier interior landmark. | (0, 5) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint., Construct an attachment by leaving an earlier endpoint after a pen lift. | (0, 0) | 2 | corner geometry | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |
| p4 | Stop at a newly reached endpoint. | (6, 5) | 1 | endpoint | 0 | 0 |
| p5 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### F — School-print construction

Draw the stem downwards. Lift and return to its top endpoint to attach a rightward bar. Lift again and return to an interior landmark on the existing stem to begin a second rightward branch.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 3; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 | True | False |
| 3 | s3 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p0: Lift and return to an existing endpoint from stroke 1. | 1 | p3: Stop at a newly reached endpoint. | none |
| 3 | p1: Lift and return to an existing interior landmark from stroke 1. | 1 | p4: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 3, p1: Construct a secondary route outward from an earlier interior landmark. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 3, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 5) | True |
| s1 | p1 → p2 | (0, 5) → (0, 0) | True |
| s2 | p0 → p3 | (0, 10) → (8, 10) | True |
| s3 | p1 → p4 | (0, 5) → (6, 5) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Construct an attachment by leaving an earlier endpoint after a pen lift. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Construct a secondary route outward from an earlier interior landmark. | (0, 5) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |
| p4 | Stop at a newly reached endpoint. | (6, 5) | 1 | endpoint | 0 | 0 |

### G — School-print construction

Follow an open curved return, rise a short distance at its lower end, then turn inward.



Teaching reference: [Zaner-Bloser manuscript formation guide](https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p3: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (7, 9) → (-2, 12) → (-2, -2) → (7, 1) | False |
| s1 | p1 → p2 | (7, 1) → (7, 5) | True |
| s2 | p2 → p3 | (7, 5) → (4, 5) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (7, 9) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (7, 1) | 2 | corner geometry | 0 | 0 |
| p2 | Change direction here during one continuous pen stroke. | (7, 5) | 2 | corner geometry | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (4, 5) | 1 | endpoint | 0 | 0 |

### H — School-print construction

Draw the left stem downwards. Use its top as a reference, move to an estimated rightward offset, and draw the parallel right stem downwards. Return to an interior landmark on the left stem and draw across to stop at the corresponding landmark on the right stem.

The offset is one supplied realization of an estimated placement. The bridge is drawn left-to-right; this history alone does not make later traffic one-way.

Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 4; branch sites: 2; independent cycles: 0; pen strokes: 3; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 → s3 | True | False |
| 3 | s4 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p3: Locate a new start by the supplied offset from landmark p0 on stroke 1; the new stroke runs parallel to it. | none | p5: Stop at a newly reached endpoint. | none |
| 3 | p1: Lift and return to an existing interior landmark from stroke 1. | 1 | p4: Arrive at the existing interior landmark from stroke 2 and stop. This stroke joins two landmarks that already existed before it began. | 2 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2: declared placement relative to p0 on stroke 1, local offset (8, 0), parallel read True.
- Stroke 2, p3: Locate a new start by the supplied offset from landmark p0 on stroke 1; the new stroke runs parallel to it. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p4: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p5: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 3, p1: Construct a secondary route outward from an earlier interior landmark. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 3, p4: Arrive at the existing interior landmark from stroke 2 and stop. Earlier incidence count: 2; earlier source strokes: 2.
- Stroke 3, p4: Connect two previously available landmarks with this new stroke. Earlier incidence count: 2; earlier source strokes: 2.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 5) | True |
| s1 | p1 → p2 | (0, 5) → (0, 0) | True |
| s2 | p3 → p4 | (8, 10) → (8, 5) | True |
| s3 | p4 → p5 | (8, 5) → (8, 0) | True |
| s4 | p1 → p4 | (0, 5) → (8, 5) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Construct a secondary route outward from an earlier interior landmark. | (0, 5) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |
| p3 | Locate a new start by the supplied offset from landmark p0 on stroke 1; the new stroke runs parallel to it. | (8, 10) | 1 | endpoint | 0 | 0 |
| p4 | Continue through this site without lifting the pen., Arrive at the existing interior landmark from stroke 2 and stop., Connect two previously available landmarks with this new stroke. | (8, 5) | 3 | T-like | 1 | 0 |
| p5 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### I — School-print construction

Draw the central stem downwards. Offset left from its top endpoint and draw the top bar through it; then offset left from the bottom endpoint and draw the bottom bar through that endpoint.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 4; branch sites: 2; independent cycles: 0; pen strokes: 3; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s2 | True | False |
| 2 | s0 → s1 | True | False |
| 3 | s3 → s4 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p1: Start at a new site; no earlier landmark is supplied. | none | p3: Stop at a newly reached endpoint. | none |
| 2 | p0: Locate a new start by the supplied offset from landmark p1 on stroke 1. | none | p2: Stop at a newly reached endpoint. | none |
| 3 | p4: Locate a new start by the supplied offset from landmark p3 on stroke 1. | none | p5: Stop at a newly reached endpoint. | none |

- Stroke 1, p1: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2: declared placement relative to p1 on stroke 1, local offset (-4, 0), parallel read False.
- Stroke 2, p0: Locate a new start by the supplied offset from landmark p1 on stroke 1. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Reach an earlier endpoint, then continue this stroke from it. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 3: declared placement relative to p3 on stroke 1, local offset (-4, 0), parallel read False.
- Stroke 3, p4: Locate a new start by the supplied offset from landmark p3 on stroke 1. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 3, p3: Reach an earlier endpoint, then continue this stroke from it. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 3, p5: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (4, 10) | True |
| s1 | p1 → p2 | (4, 10) → (8, 10) | True |
| s2 | p1 → p3 | (4, 10) → (4, 0) | True |
| s3 | p4 → p3 | (0, 0) → (4, 0) | True |
| s4 | p3 → p5 | (4, 0) → (8, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Locate a new start by the supplied offset from landmark p1 on stroke 1. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Start at a new site; no earlier landmark is supplied., Reach an earlier endpoint, then continue this stroke from it. | (4, 10) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |
| p3 | Stop at a newly reached endpoint., Reach an earlier endpoint, then continue this stroke from it. | (4, 0) | 3 | T-like | 1 | 0 |
| p4 | Locate a new start by the supplied offset from landmark p3 on stroke 1. | (0, 0) | 1 | endpoint | 0 | 0 |
| p5 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### J — School-print construction

Draw the top bar left to right. Lift and return to a landmark inside that bar, descend the stem, then curve left into the hook.

Top bar first follows the supplied card. The publisher chart also teaches a stem-and-hook-first variant, retained below.

Teaching reference: [Supplied teaching card; compared with publisher guides](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 → s3 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p1: Lift and return to an existing interior landmark from stroke 1. | 1 | p4: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Construct a secondary route outward from an earlier interior landmark. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p3: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (6, 10) | True |
| s1 | p1 → p2 | (6, 10) → (8, 10) | True |
| s2 | p1 → p3 | (6, 10) → (6, 3) | True |
| s3 | p3 → p4 | (6, 3) → (6, -1) → (0, -1) → (0, 3) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Construct a secondary route outward from an earlier interior landmark. | (6, 10) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |
| p3 | Continue through this site without lifting the pen. | (6, 3) | 2 | tangent-continuation | 1 | 0 |
| p4 | Stop at a newly reached endpoint. | (0, 3) | 1 | endpoint | 0 | 0 |

### K — School-print construction

Draw the stem downwards. Offset right from its top endpoint, then draw diagonally into the existing middle landmark and continue toward the lower right.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 4; branch sites: 1; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 → s3 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p3: Locate a new start by the supplied offset from landmark p0 on stroke 1. | none | p4: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2: declared placement relative to p0 on stroke 1, local offset (8, 0), parallel read unresolved.
- Stroke 2, p3: Locate a new start by the supplied offset from landmark p0 on stroke 1. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Reach an earlier interior landmark, then continue this stroke from it. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 5) | True |
| s1 | p1 → p2 | (0, 5) → (0, 0) | True |
| s2 | p3 → p1 | (8, 10) → (0, 5) | True |
| s3 | p1 → p4 | (0, 5) → (8, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Reach an earlier interior landmark, then continue this stroke from it. | (0, 5) | 4 | multi-arm | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |
| p3 | Locate a new start by the supplied offset from landmark p0 on stroke 1. | (8, 10) | 1 | endpoint | 0 | 0 |
| p4 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### L — School-print construction

Descend the stem and continue around the bottom corner without lifting the pen.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 0) | True |
| s1 | p1 → p2 | (0, 0) → (8, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (0, 0) | 2 | corner geometry | 0 | 0 |
| p2 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### M — School-print construction

Draw the left stem downwards. Lift and return to its top, descend diagonally to the valley, rise to the upper right, then descend the right stem.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 | True | False |
| 2 | s1 → s2 → s3 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p1: Stop at a newly reached endpoint. | none |
| 2 | p0: Lift and return to an existing endpoint from stroke 1. | 1 | p4: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p2: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p3: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 0) | True |
| s1 | p0 → p2 | (0, 10) → (4, 5) | True |
| s2 | p2 → p3 | (4, 5) → (8, 10) | True |
| s3 | p3 → p4 | (8, 10) → (8, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Construct an attachment by leaving an earlier endpoint after a pen lift. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |
| p2 | Change direction here during one continuous pen stroke. | (4, 5) | 2 | corner geometry | 0 | 0 |
| p3 | Change direction here during one continuous pen stroke. | (8, 10) | 2 | corner geometry | 0 | 0 |
| p4 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### N — School-print construction

Draw the left stem downwards. Lift and return to its top endpoint, descend diagonally to the lower right, then continue up the right stem.

The first stem descends; the later right stem rises in this taught construction. Its distinction from Z also depends on the selected frame.

Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 | True | False |
| 2 | s1 → s2 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p1: Stop at a newly reached endpoint. | none |
| 2 | p0: Lift and return to an existing endpoint from stroke 1. | 1 | p3: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p2: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 0) | True |
| s1 | p0 → p2 | (0, 10) → (8, 0) | True |
| s2 | p2 → p3 | (8, 0) → (8, 10) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Construct an attachment by leaving an earlier endpoint after a pen lift. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |
| p2 | Change direction here during one continuous pen stroke. | (8, 0) | 2 | corner geometry | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |

### O — School-print construction

Start at the top, travel leftwards around the closed contour, and return counterclockwise to the starting site.

Four native quadratic sections provide one rounded outline; this is not an exact metric-circle claim.

Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 0; branch sites: 0; independent cycles: 1; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 → s3 | True | True |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p0: Return to this stroke's actual starting site and stop. | 1 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p0: Return to this stroke's actual starting site and stop. Earlier incidence count: 1; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (4, 10) → (0, 10) → (0, 5) | False |
| s1 | p1 → p2 | (0, 5) → (0, 0) → (4, 0) | False |
| s2 | p2 → p3 | (4, 0) → (8, 0) → (8, 5) | False |
| s3 | p3 → p0 | (8, 5) → (8, 10) → (4, 10) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Return to this stroke's actual starting site and stop. | (4, 10) | 2 | tangent-continuation | 1 | 0 |
| p1 | Continue through this site without lifting the pen. | (0, 5) | 2 | tangent-continuation | 1 | 0 |
| p2 | Continue through this site without lifting the pen. | (4, 0) | 2 | tangent-continuation | 1 | 0 |
| p3 | Continue through this site without lifting the pen. | (8, 5) | 2 | tangent-continuation | 1 | 0 |

### P — School-print construction

Draw the stem downwards. Lift and return to its top endpoint, then draw a bowl that stops at an interior landmark on the existing stem.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 1; branch sites: 1; independent cycles: 1; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p0: Lift and return to an existing endpoint from stroke 1. | 1 | p1: Arrive at the existing interior landmark from stroke 1 and stop. This stroke joins two landmarks that already existed before it began. | 1 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p1: Arrive at the existing interior landmark from stroke 1 and stop. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p1: Connect two previously available landmarks with this new stroke. Earlier incidence count: 2; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 5) | True |
| s1 | p1 → p2 | (0, 5) → (0, 0) | True |
| s2 | p0 → p1 | (0, 10) → (8, 10) → (8, 5) → (0, 5) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Construct an attachment by leaving an earlier endpoint after a pen lift. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Arrive at the existing interior landmark from stroke 1 and stop., Connect two previously available landmarks with this new stroke. | (0, 5) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |

### Q — School-print construction

Draw a closed rounded contour, lift, then draw a short diagonal across its lower right area.

The tail is a separate stroke with separate sites. A visible intersection does not silently weld it to the contour.

Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 2; free ends: 2; branch sites: 0; independent cycles: 1; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 → s3 | True | True |
| 2 | s4 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p0: Return to this stroke's actual starting site and stop. | 1 |
| 2 | p4: Locate a new start by the supplied offset from landmark p2 on stroke 1. | none | p5: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p0: Return to this stroke's actual starting site and stop. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2: declared placement relative to p2 on stroke 1, local offset (1, 3), parallel read unresolved.
- Stroke 2, p4: Locate a new start by the supplied offset from landmark p2 on stroke 1. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p5: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (4, 10) → (0, 10) → (0, 5) | False |
| s1 | p1 → p2 | (0, 5) → (0, 0) → (4, 0) | False |
| s2 | p2 → p3 | (4, 0) → (8, 0) → (8, 5) | False |
| s3 | p3 → p0 | (8, 5) → (8, 10) → (4, 10) | False |
| s4 | p4 → p5 | (5, 3) → (9, -1) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Return to this stroke's actual starting site and stop. | (4, 10) | 2 | tangent-continuation | 1 | 0 |
| p1 | Continue through this site without lifting the pen. | (0, 5) | 2 | tangent-continuation | 1 | 0 |
| p2 | Continue through this site without lifting the pen. | (4, 0) | 2 | tangent-continuation | 1 | 0 |
| p3 | Continue through this site without lifting the pen. | (8, 5) | 2 | tangent-continuation | 1 | 0 |
| p4 | Locate a new start by the supplied offset from landmark p2 on stroke 1. | (5, 3) | 1 | endpoint | 0 | 0 |
| p5 | Stop at a newly reached endpoint. | (9, -1) | 1 | endpoint | 0 | 0 |

### R — School-print construction

Draw the stem downwards. Lift and return to its top, draw the bowl back to the middle landmark, then continue directly into the diagonal leg.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 1; independent cycles: 1; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 → s3 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p0: Lift and return to an existing endpoint from stroke 1. | 1 | p3: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p1: Reach an earlier interior landmark, then continue this stroke from it. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 5) | True |
| s1 | p1 → p2 | (0, 5) → (0, 0) | True |
| s2 | p0 → p1 | (0, 10) → (8, 10) → (8, 5) → (0, 5) | False |
| s3 | p1 → p3 | (0, 5) → (8, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Construct an attachment by leaving an earlier endpoint after a pen lift. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Reach an earlier interior landmark, then continue this stroke from it. | (0, 5) | 4 | multi-arm | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### S — School-print construction

Follow an upper curved return into a middle site, then reverse the bending sense through the lower return.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (7, 9) → (-2, 12) → (-2, 6) → (4, 5) | False |
| s1 | p1 → p2 | (4, 5) → (10, 4) → (10, -2) → (1, 1) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (7, 9) | 1 | endpoint | 0 | 0 |
| p1 | Continue through this site without lifting the pen. | (4, 5) | 2 | tangent-continuation | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (1, 1) | 1 | endpoint | 0 | 0 |

### T — School-print construction

Draw the top bar through its central site, lift, then attach a descending stem to that site.

Top bar first follows the supplied card. A stem-first teaching variant is also shown; the resulting artifact has the same connectivity.

Teaching reference: [Supplied teaching card; compared with publisher guides](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p1: Lift and return to an existing interior landmark from stroke 1. | 1 | p3: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Construct a secondary route outward from an earlier interior landmark. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (4, 10) | True |
| s1 | p1 → p2 | (4, 10) → (8, 10) | True |
| s2 | p1 → p3 | (4, 10) → (4, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Construct a secondary route outward from an earlier interior landmark. | (4, 10) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (4, 0) | 1 | endpoint | 0 | 0 |

### U — School-print construction

Descend the left side, follow a curved bottom return, and rise on the right without lifting.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p3: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 3) | True |
| s1 | p1 → p2 | (0, 3) → (0, -1) → (8, -1) → (8, 3) | False |
| s2 | p2 → p3 | (8, 3) → (8, 10) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Continue through this site without lifting the pen. | (0, 3) | 2 | tangent-continuation | 1 | 0 |
| p2 | Continue through this site without lifting the pen. | (8, 3) | 2 | tangent-continuation | 1 | 0 |
| p3 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |

### V — School-print construction

Descend to one bottom site, then rise from that same site toward the opposite upper end.

A low point is not, by itself, a certificate of physical instability; that needs a support and load context.

Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (4, 0) | True |
| s1 | p1 → p2 | (4, 0) → (8, 10) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (4, 0) | 2 | corner geometry | 0 | 0 |
| p2 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |

### W — School-print construction

Descend, rise to an inner peak, descend again, then rise to the final endpoint.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 → s3 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p4: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (2, 0) | True |
| s1 | p1 → p2 | (2, 0) → (4, 5) | True |
| s2 | p2 → p3 | (4, 5) → (6, 0) | True |
| s3 | p3 → p4 | (6, 0) → (8, 10) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (2, 0) | 2 | corner geometry | 0 | 0 |
| p2 | Change direction here during one continuous pen stroke. | (4, 5) | 2 | corner geometry | 0 | 0 |
| p3 | Change direction here during one continuous pen stroke. | (6, 0) | 2 | corner geometry | 0 | 0 |
| p4 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |

### X — School-print construction

Draw a diagonal from upper left to lower right. Lift and offset to the upper right, then draw down to the lower left. Their central sites coincide in this view but remain independently identified.

This is a crossing interpretation. See the joined variant for an explicit four-arm shared site.

Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 2; free ends: 4; branch sites: 0; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 → s3 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p3: Locate a new start by the supplied offset from landmark p0 on stroke 1. | none | p5: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2: declared placement relative to p0 on stroke 1, local offset (8, 0), parallel read False.
- Stroke 2, p3: Locate a new start by the supplied offset from landmark p0 on stroke 1. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p4: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p5: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (4, 5) | True |
| s1 | p1 → p2 | (4, 5) → (8, 0) | True |
| s2 | p3 → p4 | (8, 10) → (4, 5) | True |
| s3 | p4 → p5 | (4, 5) → (0, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Continue through this site without lifting the pen. | (4, 5) | 2 | tangent-continuation | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |
| p3 | Locate a new start by the supplied offset from landmark p0 on stroke 1. | (8, 10) | 1 | endpoint | 0 | 0 |
| p4 | Continue through this site without lifting the pen. | (4, 5) | 2 | tangent-continuation | 1 | 0 |
| p5 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |

### Y — School-print construction

Descend from the upper left and stop at the junction landmark. Lift and offset to the upper right, draw into that existing landmark, then continue down the stem.



Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 | True | False |
| 2 | s2 → s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p1: Stop at a newly reached endpoint. | none |
| 2 | p3: Locate a new start by the supplied offset from landmark p0 on stroke 1. | none | p2: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2: declared placement relative to p0 on stroke 1, local offset (8, 0), parallel read unresolved.
- Stroke 2, p3: Locate a new start by the supplied offset from landmark p0 on stroke 1. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Reach an earlier endpoint, then continue this stroke from it. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (4, 5) | True |
| s1 | p1 → p2 | (4, 5) → (4, 0) | True |
| s2 | p3 → p1 | (8, 10) → (4, 5) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Stop at a newly reached endpoint., Reach an earlier endpoint, then continue this stroke from it. | (4, 5) | 3 | three-arm | 0 | 0 |
| p2 | Stop at a newly reached endpoint. | (4, 0) | 1 | endpoint | 0 | 0 |
| p3 | Locate a new start by the supplied offset from landmark p0 on stroke 1. | (8, 10) | 1 | endpoint | 0 | 0 |

### Z — School-print construction

Cross the top, descend diagonally to the lower left, then cross the bottom in one path.

Rotate its viewing frame and the same unlabelled three-link topology admits another glyph interpretation.

Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p3: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (8, 10) | True |
| s1 | p1 → p2 | (8, 10) → (0, 0) | True |
| s2 | p2 → p3 | (0, 0) → (8, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (8, 10) | 2 | corner geometry | 0 | 0 |
| p2 | Change direction here during one continuous pen stroke. | (0, 0) | 2 | corner geometry | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### 0 — School-print construction

Start at the top and travel leftwards around a counterclockwise closed contour. Its topology alone does not distinguish it from O.



Teaching reference: [Zaner-Bloser manuscript formation guide](https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf).

Variant `baseline`. Components: 1; free ends: 0; branch sites: 0; independent cycles: 1; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 → s3 | True | True |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p0: Return to this stroke's actual starting site and stop. | 1 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p0: Return to this stroke's actual starting site and stop. Earlier incidence count: 1; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (4, 10) → (0, 10) → (0, 5) | False |
| s1 | p1 → p2 | (0, 5) → (0, 0) → (4, 0) | False |
| s2 | p2 → p3 | (4, 0) → (8, 0) → (8, 5) | False |
| s3 | p3 → p0 | (8, 5) → (8, 10) → (4, 10) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Return to this stroke's actual starting site and stop. | (4, 10) | 2 | tangent-continuation | 1 | 0 |
| p1 | Continue through this site without lifting the pen. | (0, 5) | 2 | tangent-continuation | 1 | 0 |
| p2 | Continue through this site without lifting the pen. | (4, 0) | 2 | tangent-continuation | 1 | 0 |
| p3 | Continue through this site without lifting the pen. | (8, 5) | 2 | tangent-continuation | 1 | 0 |

### 1 — School-print construction

Start at the top and draw one straight downward stroke to the bottom stop.

The plain school-print form follows the supplied card. A leading serif or baseline would be another construction.

Teaching reference: [Zaner-Bloser manuscript formation guide](https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p1: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (4, 10) → (4, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (4, 10) | 1 | endpoint | 0 | 0 |
| p1 | Stop at a newly reached endpoint. | (4, 0) | 1 | endpoint | 0 | 0 |

### 2 — School-print construction

Curve across the top, descend diagonally to the lower left, then turn along the baseline.



Teaching reference: [Zaner-Bloser manuscript formation guide](https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p3: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 8) → (0, 12) → (8, 12) → (8, 8) | False |
| s1 | p1 → p2 | (8, 8) → (0, 0) | True |
| s2 | p2 → p3 | (0, 0) → (8, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 8) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (8, 8) | 2 | corner geometry | 0 | 0 |
| p2 | Change direction here during one continuous pen stroke. | (0, 0) | 2 | corner geometry | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### 3 — School-print construction

Make two successive rightward curved returns joined at a middle site, with no stem closing either bowl.



Teaching reference: [Zaner-Bloser manuscript formation guide](https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (8, 10) → (8, 5) → (0, 5) | False |
| s1 | p1 → p2 | (0, 5) → (8, 5) → (8, 0) → (0, 0) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (0, 5) | 2 | fold-back | 0 | 1 |
| p2 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |

### 4 — School-print construction

Descend diagonally, turn right through a crossing site, then lift and draw a stem through that site.

Diagonal first follows the supplied card; Zaner-Bloser also teaches a vertical first arm. This open-top 4 has no cycle; the closed-top variant does.

Teaching reference: [Supplied teaching card; compared with publisher guides](https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf).

Variant `baseline`. Components: 1; free ends: 4; branch sites: 1; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 | True | False |
| 2 | s3 → s4 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p3: Stop at a newly reached endpoint. | none |
| 2 | p4: Locate a new start by the supplied offset from landmark p0 on stroke 1. | none | p5: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2: declared placement relative to p0 on stroke 1, local offset (1, 0), parallel read unresolved.
- Stroke 2, p4: Locate a new start by the supplied offset from landmark p0 on stroke 1. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p2: Reach an earlier interior landmark, then continue this stroke from it. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p5: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (5, 10) → (0, 3) | True |
| s1 | p1 → p2 | (0, 3) → (6, 3) | True |
| s2 | p2 → p3 | (6, 3) → (8, 3) | True |
| s3 | p4 → p2 | (6, 10) → (6, 3) | True |
| s4 | p2 → p5 | (6, 3) → (6, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (5, 10) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (0, 3) | 2 | corner geometry | 0 | 0 |
| p2 | Continue through this site without lifting the pen., Reach an earlier interior landmark, then continue this stroke from it. | (6, 3) | 4 | crossing-like | 2 | 0 |
| p3 | Stop at a newly reached endpoint. | (8, 3) | 1 | endpoint | 0 | 0 |
| p4 | Locate a new start by the supplied offset from landmark p0 on stroke 1. | (6, 10) | 1 | endpoint | 0 | 0 |
| p5 | Stop at a newly reached endpoint. | (6, 0) | 1 | endpoint | 0 | 0 |

### 5 — School-print construction

Cross the top toward the left, descend a short stem, then make a curved lower return.

One continuous top-bar-first stroke follows the supplied card. A separately added top bar is a legitimate alternate teaching convention.

Teaching reference: [Supplied teaching card; compared with publisher guides](https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p3: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (8, 10) → (0, 10) | True |
| s1 | p1 → p2 | (0, 10) → (0, 6) | True |
| s2 | p2 → p3 | (0, 6) → (10, 8) → (10, -1) → (0, 0) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (8, 10) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p2 | Change direction here during one continuous pen stroke. | (0, 6) | 2 | corner geometry | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |

### 6 — School-print construction

Start at the free upper tail and curve down the outer left. Continue around the lower bowl, rise on its right side, and return through the upper bowl to stop at the earlier left-hand landmark.



Teaching reference: [Zaner-Bloser manuscript formation guide](https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf).

Variant `baseline`. Components: 1; free ends: 1; branch sites: 1; independent cycles: 1; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p1: Stop at a landmark already reached earlier in this same stroke. | 1 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Stop at a landmark already reached earlier in this same stroke. Earlier incidence count: 2; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (7, 10) → (3, 8) → (-1, 7) → (0, 4) | False |
| s1 | p1 → p2 | (0, 4) → (0, -1) → (8, -1) → (8, 3) | False |
| s2 | p2 → p1 | (8, 3) → (8, 8) → (0, 8) → (0, 4) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (7, 10) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke., Stop at a landmark already reached earlier in this same stroke. | (0, 4) | 3 | T-like | 1 | 0 |
| p2 | Continue through this site without lifting the pen. | (8, 3) | 2 | tangent-continuation | 1 | 0 |

### 7 — School-print construction

Cross the top and descend diagonally from its right endpoint without lifting.



Teaching reference: [Zaner-Bloser manuscript formation guide](https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf).

Variant `baseline`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (8, 10) | True |
| s1 | p1 → p2 | (8, 10) → (2, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (8, 10) | 2 | corner geometry | 0 | 0 |
| p2 | Stop at a newly reached endpoint. | (2, 0) | 1 | endpoint | 0 | 0 |

### 8 — School-print construction

Start near the top, curve down the upper left and through the waist into the lower right. Round the bottom, pass through the waist again from the lower left, and finish up the upper right at the starting site.



Teaching reference: [Zaner-Bloser manuscript formation guide](https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf).

Variant `baseline`. Components: 1; free ends: 0; branch sites: 1; independent cycles: 2; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 → s3 | True | True |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p0: Return to this stroke's actual starting site and stop. | 1 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Revisit a landmark reached earlier in this same stroke, then continue. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 1, p0: Return to this stroke's actual starting site and stop. Earlier incidence count: 1; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (4, 10) → (-2, 10) → (-2, 8) → (4, 5) | False |
| s1 | p1 → p2 | (4, 5) → (10, 2) → (11, 0) → (4, 0) | False |
| s2 | p2 → p1 | (4, 0) → (-3, 0) → (-2, 2) → (4, 5) | False |
| s3 | p1 → p0 | (4, 5) → (10, 8) → (10, 10) → (4, 10) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Return to this stroke's actual starting site and stop. | (4, 10) | 2 | tangent-continuation | 1 | 0 |
| p1 | Continue through this site without lifting the pen., Revisit a landmark reached earlier in this same stroke, then continue. | (4, 5) | 4 | crossing-like | 2 | 0 |
| p2 | Continue through this site without lifting the pen. | (4, 0) | 2 | tangent-continuation | 1 | 0 |

### 9 — School-print construction

Start near the upper right, curve leftwards around the bowl and back to the starting landmark, then continue into the descending tail.



Teaching reference: [Zaner-Bloser manuscript formation guide](https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf).

Variant `baseline`. Components: 1; free ends: 1; branch sites: 1; independent cycles: 1; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p0: Revisit a landmark reached earlier in this same stroke, then continue. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (7, 9) → (3, 12) → (-1, 10) → (0, 7) | False |
| s1 | p1 → p0 | (0, 7) → (0, 3) → (9, 2) → (7, 9) | False |
| s2 | p0 → p2 | (7, 9) → (9, 3) → (5, -1) → (1, 0) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Revisit a landmark reached earlier in this same stroke, then continue. | (7, 9) | 3 | three-arm | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (0, 7) | 2 | corner geometry | 0 | 0 |
| p2 | Stop at a newly reached endpoint. | (1, 0) | 1 | endpoint | 0 | 0 |

### L — Stem plus attached foot

Draw the stem, lift, then attach the foot at its lower endpoint.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Variant `attached`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 | True | False |
| 2 | s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p1: Stop at a newly reached endpoint. | none |
| 2 | p1: Lift and return to an existing endpoint from stroke 1. | 1 | p2: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 0) | True |
| s1 | p1 → p2 | (0, 0) → (8, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Stop at a newly reached endpoint., Construct an attachment by leaving an earlier endpoint after a pen lift. | (0, 0) | 2 | corner geometry | 0 | 0 |
| p2 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### F — Continuous top corner

Start at the top bar's right end, turn down into the whole stem, then add the middle bar.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Variant `corner-first`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s2 reversed → s0 → s1 | True | False |
| 2 | s3 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p3: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p1: Lift and return to an existing interior landmark from stroke 1. | 1 | p4: Stop at a newly reached endpoint. | none |

- Stroke 1, p3: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p0: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Construct a secondary route outward from an earlier interior landmark. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 5) | True |
| s1 | p1 → p2 | (0, 5) → (0, 0) | True |
| s2 | p0 → p3 | (0, 10) → (8, 10) | True |
| s3 | p1 → p4 | (0, 5) → (6, 5) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Change direction here during one continuous pen stroke. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Construct a secondary route outward from an earlier interior landmark. | (0, 5) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |
| p3 | Start at a new site; no earlier landmark is supplied. | (8, 10) | 1 | endpoint | 0 | 0 |
| p4 | Stop at a newly reached endpoint. | (6, 5) | 1 | endpoint | 0 | 0 |

### F — Bars arrive at the stem

Draw the stem downwards, then draw each bar from the outside toward it. The upper bar stops at its top endpoint; the middle bar stops at its interior landmark.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Variant `bars-incoming`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 3; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 reversed | True | False |
| 3 | s3 reversed | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p3: Start at a new site; no earlier landmark is supplied. | none | p0: Arrive at the existing endpoint from stroke 1 and stop. | 1 |
| 3 | p4: Start at a new site; no earlier landmark is supplied. | none | p1: Arrive at the existing interior landmark from stroke 1 and stop. | 1 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p3: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Arrive at the existing endpoint from stroke 1 and stop. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 3, p4: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 3, p1: Arrive at the existing interior landmark from stroke 1 and stop. Earlier incidence count: 2; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 5) | True |
| s1 | p1 → p2 | (0, 5) → (0, 0) | True |
| s2 | p0 → p3 | (0, 10) → (8, 10) | True |
| s3 | p1 → p4 | (0, 5) → (6, 5) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Arrive at the existing endpoint from stroke 1 and stop. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Arrive at the existing interior landmark from stroke 1 and stop. | (0, 5) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |
| p3 | Start at a new site; no earlier landmark is supplied. | (8, 10) | 1 | endpoint | 0 | 0 |
| p4 | Start at a new site; no earlier landmark is supplied. | (6, 5) | 1 | endpoint | 0 | 0 |

### H — Opposed bridge read

Draw the same two stems, then draw the bridge from right to left.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Variant `bridge-reversed`. Components: 1; free ends: 4; branch sites: 2; independent cycles: 0; pen strokes: 3; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 → s3 | True | False |
| 3 | s4 reversed | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p3: Locate a new start by the supplied offset from landmark p0 on stroke 1; the new stroke runs parallel to it. | none | p5: Stop at a newly reached endpoint. | none |
| 3 | p4: Lift and return to an existing interior landmark from stroke 2. | 2 | p1: Arrive at the existing interior landmark from stroke 1 and stop. This stroke joins two landmarks that already existed before it began. | 1 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2: declared placement relative to p0 on stroke 1, local offset (8, 0), parallel read True.
- Stroke 2, p3: Locate a new start by the supplied offset from landmark p0 on stroke 1; the new stroke runs parallel to it. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p4: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p5: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 3, p4: Construct a secondary route outward from an earlier interior landmark. Earlier incidence count: 2; earlier source strokes: 2.
- Stroke 3, p1: Arrive at the existing interior landmark from stroke 1 and stop. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 3, p1: Connect two previously available landmarks with this new stroke. Earlier incidence count: 2; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 5) | True |
| s1 | p1 → p2 | (0, 5) → (0, 0) | True |
| s2 | p3 → p4 | (8, 10) → (8, 5) | True |
| s3 | p4 → p5 | (8, 5) → (8, 0) | True |
| s4 | p1 → p4 | (0, 5) → (8, 5) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Arrive at the existing interior landmark from stroke 1 and stop., Connect two previously available landmarks with this new stroke. | (0, 5) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |
| p3 | Locate a new start by the supplied offset from landmark p0 on stroke 1; the new stroke runs parallel to it. | (8, 10) | 1 | endpoint | 0 | 0 |
| p4 | Continue through this site without lifting the pen., Construct a secondary route outward from an earlier interior landmark. | (8, 5) | 3 | T-like | 1 | 0 |
| p5 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### T — Two corner traversals

Turn from the left arm down the stem, lift, retrace that same stem upward, then turn onto the right arm.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Variant `two-corners`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 2; revisited sections: 1; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s2 | True | False |
| 2 | s2 reversed → s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p3: Stop at a newly reached endpoint. | none |
| 2 | p3: Lift and return to an existing endpoint from stroke 1. | 1 | p2: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p3: Return and retrace an existing section; this is not a new attachment. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (4, 10) | True |
| s1 | p1 → p2 | (4, 10) → (8, 10) | True |
| s2 | p1 → p3 | (4, 10) → (4, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (4, 10) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |
| p3 | Stop at a newly reached endpoint., Return and retrace an existing section; this is not a new attachment. | (4, 0) | 1 | endpoint | 0 | 0 |

### Y — Outward construction read

Rise through the stem and leave along the left arm, then draw the right arm outward from the junction.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Variant `outward`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s1 reversed → s0 reversed | True | False |
| 2 | s2 reversed | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p2: Start at a new site; no earlier landmark is supplied. | none | p0: Stop at a newly reached endpoint. | none |
| 2 | p1: Lift and return to an existing interior landmark from stroke 1. | 1 | p3: Stop at a newly reached endpoint. | none |

- Stroke 1, p2: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p0: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Construct a secondary route outward from an earlier interior landmark. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (4, 5) | True |
| s1 | p1 → p2 | (4, 5) → (4, 0) | True |
| s2 | p3 → p1 | (8, 10) → (4, 5) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Stop at a newly reached endpoint. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke., Construct a secondary route outward from an earlier interior landmark. | (4, 5) | 3 | three-arm | 0 | 0 |
| p2 | Start at a new site; no earlier landmark is supplied. | (4, 0) | 1 | endpoint | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |

### A — Continuous turn through the apex

Start at the left foot, rise to the apex and continue down the right leg, then add the crossbar. This contrasts with the taught two descending strokes.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Variant `continuous-apex`. Components: 1; free ends: 2; branch sites: 2; independent cycles: 1; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s1 reversed → s0 reversed → s2 → s3 | True | False |
| 2 | s4 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p2: Start at a new site; no earlier landmark is supplied. | none | p4: Stop at a newly reached endpoint. | none |
| 2 | p1: Lift and return to an existing interior landmark from stroke 1. | 1 | p3: Arrive at the existing interior landmark from stroke 1 and stop. This stroke joins two landmarks that already existed before it began. | 1 |

- Stroke 1, p2: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p0: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Construct a secondary route outward from an earlier interior landmark. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p3: Arrive at the existing interior landmark from stroke 1 and stop. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p3: Connect two previously available landmarks with this new stroke. Earlier incidence count: 2; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (4, 10) → (2, 5) | True |
| s1 | p1 → p2 | (2, 5) → (0, 0) | True |
| s2 | p0 → p3 | (4, 10) → (6, 5) | True |
| s3 | p3 → p4 | (6, 5) → (8, 0) | True |
| s4 | p1 → p3 | (2, 5) → (6, 5) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Change direction here during one continuous pen stroke. | (4, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Construct a secondary route outward from an earlier interior landmark. | (2, 5) | 3 | T-like | 1 | 0 |
| p2 | Start at a new site; no earlier landmark is supplied. | (0, 0) | 1 | endpoint | 0 | 0 |
| p3 | Continue through this site without lifting the pen., Arrive at the existing interior landmark from stroke 1 and stop., Connect two previously available landmarks with this new stroke. | (6, 5) | 3 | T-like | 1 | 0 |
| p4 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### 8 — Two separately traced loops

Complete the upper loop, lift, then complete the lower loop through the same actual waist. This contrasts with the continuous taught figure-eight crossing.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Variant `two-loops`. Components: 1; free ends: 0; branch sites: 1; independent cycles: 2; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s3 | True | True |
| 2 | s1 → s2 | True | True |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p0: Return to this stroke's actual starting site and stop. | 1 |
| 2 | p1: Lift and return to an existing interior landmark from stroke 1. | 1 | p1: Return to this stroke's actual starting site and stop. | 1, 2 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p0: Return to this stroke's actual starting site and stop. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p1: Construct a secondary route outward from an earlier interior landmark. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p2: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Return to this stroke's actual starting site and stop. Earlier incidence count: 3; earlier source strokes: 1, 2.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (4, 10) → (-2, 10) → (-2, 8) → (4, 5) | False |
| s1 | p1 → p2 | (4, 5) → (10, 2) → (11, 0) → (4, 0) | False |
| s2 | p2 → p1 | (4, 0) → (-3, 0) → (-2, 2) → (4, 5) | False |
| s3 | p1 → p0 | (4, 5) → (10, 8) → (10, 10) → (4, 10) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Return to this stroke's actual starting site and stop. | (4, 10) | 2 | tangent-continuation | 1 | 0 |
| p1 | Change direction here during one continuous pen stroke., Construct a secondary route outward from an earlier interior landmark., Return to this stroke's actual starting site and stop. | (4, 5) | 4 | crossing-like | 2 | 0 |
| p2 | Continue through this site without lifting the pen. | (4, 0) | 2 | tangent-continuation | 1 | 0 |

### D — Continuous stem and return

Descend the stem and continue around the curved return in reverse to close in one stroke.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Variant `continuous`. Components: 1; free ends: 0; branch sites: 0; independent cycles: 1; pen strokes: 1; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 reversed | True | True |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p0: Return to this stroke's actual starting site and stop. | 1 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p0: Return to this stroke's actual starting site and stop. Earlier incidence count: 1; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 0) | True |
| s1 | p0 → p1 | (0, 10) → (9, 10) → (9, 0) → (0, 0) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Return to this stroke's actual starting site and stop. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (0, 0) | 2 | corner geometry | 0 | 0 |

### E — Bars before stem

Draw the three bars first, then connect their left sites with the stem.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Variant `bar-first`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 4; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s2 | True | False |
| 2 | s3 | True | False |
| 3 | s4 | True | False |
| 4 | s0 → s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p3: Stop at a newly reached endpoint. | none |
| 2 | p1: Start at a new site; no earlier landmark is supplied. | none | p4: Stop at a newly reached endpoint. | none |
| 3 | p2: Start at a new site; no earlier landmark is supplied. | none | p5: Stop at a newly reached endpoint. | none |
| 4 | p0: Lift and return to an existing endpoint from stroke 1. | 1 | p2: Arrive at the existing endpoint from stroke 3 and stop. This stroke joins two landmarks that already existed before it began. | 3 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 3, p2: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 3, p5: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 4, p0: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 4, p1: Reach an earlier endpoint, then continue this stroke from it. Earlier incidence count: 1; earlier source strokes: 2.
- Stroke 4, p2: Arrive at the existing endpoint from stroke 3 and stop. Earlier incidence count: 1; earlier source strokes: 3.
- Stroke 4, p2: Connect two previously available landmarks with this new stroke. Earlier incidence count: 1; earlier source strokes: 3.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (0, 5) | True |
| s1 | p1 → p2 | (0, 5) → (0, 0) | True |
| s2 | p0 → p3 | (0, 10) → (8, 10) | True |
| s3 | p1 → p4 | (0, 5) → (6, 5) | True |
| s4 | p2 → p5 | (0, 0) → (8, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Construct an attachment by leaving an earlier endpoint after a pen lift. | (0, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Start at a new site; no earlier landmark is supplied., Reach an earlier endpoint, then continue this stroke from it. | (0, 5) | 3 | T-like | 1 | 0 |
| p2 | Start at a new site; no earlier landmark is supplied., Arrive at the existing endpoint from stroke 3 and stop., Connect two previously available landmarks with this new stroke. | (0, 0) | 2 | corner geometry | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |
| p4 | Stop at a newly reached endpoint. | (6, 5) | 1 | endpoint | 0 | 0 |
| p5 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |

### T — Teaching variant: stem first

Draw the stem downwards first, then draw the horizontal bar through its existing top endpoint.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `stem-first`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s2 | True | False |
| 2 | s0 → s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p1: Start at a new site; no earlier landmark is supplied. | none | p3: Stop at a newly reached endpoint. | none |
| 2 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |

- Stroke 1, p1: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Reach an earlier endpoint, then continue this stroke from it. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (4, 10) | True |
| s1 | p1 → p2 | (4, 10) → (8, 10) | True |
| s2 | p1 → p3 | (4, 10) → (4, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Start at a new site; no earlier landmark is supplied., Reach an earlier endpoint, then continue this stroke from it. | (4, 10) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |
| p3 | Stop at a newly reached endpoint. | (4, 0) | 1 | endpoint | 0 | 0 |

### J — Teaching variant: stem and hook first

Draw the descending stem and hook first, then draw the top bar through the existing stem endpoint.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `stem-first`. Components: 1; free ends: 3; branch sites: 1; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s2 → s3 | True | False |
| 2 | s0 → s1 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p1: Start at a new site; no earlier landmark is supplied. | none | p4: Stop at a newly reached endpoint. | none |
| 2 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |

- Stroke 1, p1: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Reach an earlier endpoint, then continue this stroke from it. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (6, 10) | True |
| s1 | p1 → p2 | (6, 10) → (8, 10) | True |
| s2 | p1 → p3 | (6, 10) → (6, 3) | True |
| s3 | p3 → p4 | (6, 3) → (6, -1) → (0, -1) → (0, 3) | False |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Start at a new site; no earlier landmark is supplied., Reach an earlier endpoint, then continue this stroke from it. | (6, 10) | 3 | T-like | 1 | 0 |
| p2 | Stop at a newly reached endpoint. | (8, 10) | 1 | endpoint | 0 | 0 |
| p3 | Continue through this site without lifting the pen. | (6, 3) | 2 | tangent-continuation | 1 | 0 |
| p4 | Stop at a newly reached endpoint. | (0, 3) | 1 | endpoint | 0 | 0 |

### G — Teaching variant: separately attached bar

Draw the curved body and short rise first, then draw the interior bar rightward to stop at the body's existing endpoint.

An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.

Teaching reference: [Learning Without Tears capital formation chart](https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf).

Variant `attached-bar`. Components: 1; free ends: 2; branch sites: 0; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 reversed | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p3: Start at a new site; no earlier landmark is supplied. | none | p2: Arrive at the existing endpoint from stroke 1 and stop. | 1 |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p3: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p2: Arrive at the existing endpoint from stroke 1 and stop. Earlier incidence count: 1; earlier source strokes: 1.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (7, 9) → (-2, 12) → (-2, -2) → (7, 1) | False |
| s1 | p1 → p2 | (7, 1) → (7, 5) | True |
| s2 | p2 → p3 | (7, 5) → (4, 5) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (7, 9) | 1 | endpoint | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (7, 1) | 2 | corner geometry | 0 | 0 |
| p2 | Stop at a newly reached endpoint., Arrive at the existing endpoint from stroke 1 and stop. | (7, 5) | 2 | corner geometry | 0 | 0 |
| p3 | Start at a new site; no earlier landmark is supplied. | (4, 5) | 1 | endpoint | 0 | 0 |

### X — Four-arm shared junction

Draw two diagonals through one explicitly shared central site.

The silhouette matches the crossing fixture, but the selected incidence graph has one component and a degree-four junction. Flow routes are still a separate question.

Variant `joined`. Components: 1; free ends: 4; branch sites: 1; independent cycles: 0; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 | True | False |
| 2 | s2 → s3 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p2: Stop at a newly reached endpoint. | none |
| 2 | p3: Locate a new start by the supplied offset from landmark p0 on stroke 1. | none | p4: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2: declared placement relative to p0 on stroke 1, local offset (8, 0), parallel read False.
- Stroke 2, p3: Locate a new start by the supplied offset from landmark p0 on stroke 1. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p1: Reach an earlier interior landmark, then continue this stroke from it. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (0, 10) → (4, 5) | True |
| s1 | p1 → p2 | (4, 5) → (8, 0) | True |
| s2 | p3 → p1 | (8, 10) → (4, 5) | True |
| s3 | p1 → p4 | (4, 5) → (0, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied. | (0, 10) | 1 | endpoint | 0 | 0 |
| p1 | Continue through this site without lifting the pen., Reach an earlier interior landmark, then continue this stroke from it. | (4, 5) | 4 | crossing-like | 2 | 0 |
| p2 | Stop at a newly reached endpoint. | (8, 0) | 1 | endpoint | 0 | 0 |
| p3 | Locate a new start by the supplied offset from landmark p0 on stroke 1. | (8, 10) | 1 | endpoint | 0 | 0 |
| p4 | Stop at a newly reached endpoint. | (0, 0) | 1 | endpoint | 0 | 0 |

### 4 — Closed-top construction

Join the diagonal and vertical strokes at their upper endpoint, creating a triangular cycle.

This changes the actual topology, unlike merely changing pen order.

Variant `closed-top`. Components: 1; free ends: 2; branch sites: 1; independent cycles: 1; pen strokes: 2; revisited sections: 0; complete read: True.

| Stroke | Ordered section visits | Continuous | Closed |
| --- | --- | --- | --- |
| 1 | s0 → s1 → s2 | True | False |
| 2 | s3 → s4 | True | False |

#### Starts, stops and construction landmarks

| Stroke | Start | Earlier ink at start | Stop | Earlier ink at stop |
| --- | --- | --- | --- | --- |
| 1 | p0: Start at a new site; no earlier landmark is supplied. | none | p3: Stop at a newly reached endpoint. | none |
| 2 | p0: Lift and return to an existing endpoint from stroke 1. | 1 | p4: Stop at a newly reached endpoint. | none |

- Stroke 1, p0: Start at a new site; no earlier landmark is supplied. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p1: Change direction here during one continuous pen stroke. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p2: Continue through this site without lifting the pen. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 1, p3: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.
- Stroke 2, p0: Construct an attachment by leaving an earlier endpoint after a pen lift. Earlier incidence count: 1; earlier source strokes: 1.
- Stroke 2, p2: Reach an earlier interior landmark, then continue this stroke from it. Earlier incidence count: 2; earlier source strokes: 1.
- Stroke 2, p4: Stop at a newly reached endpoint. Earlier incidence count: 0; earlier source strokes: none.

| Section | Actual sites | Selected control words | Straight read |
| --- | --- | --- | --- |
| s0 | p0 → p1 | (6, 10) → (0, 3) | True |
| s1 | p1 → p2 | (0, 3) → (6, 3) | True |
| s2 | p2 → p3 | (6, 3) → (8, 3) | True |
| s3 | p0 → p2 | (6, 10) → (6, 3) | True |
| s4 | p2 → p4 | (6, 3) → (6, 0) | True |

| Site | Construction history read | Selected position | Incidences | Geometric tangent read | Opposed pairs | Cooriented pairs |
| --- | --- | --- | --- | --- | --- | --- |
| p0 | Start at a new site; no earlier landmark is supplied., Construct an attachment by leaving an earlier endpoint after a pen lift. | (6, 10) | 2 | corner geometry | 0 | 0 |
| p1 | Change direction here during one continuous pen stroke. | (0, 3) | 2 | corner geometry | 0 | 0 |
| p2 | Continue through this site without lifting the pen., Reach an earlier interior landmark, then continue this stroke from it. | (6, 3) | 4 | crossing-like | 2 | 0 |
| p3 | Stop at a newly reached endpoint. | (8, 3) | 1 | endpoint | 0 | 0 |
| p4 | Stop at a newly reached endpoint. | (6, 0) | 1 | endpoint | 0 | 0 |

## Joint machines and actual probes

The supplied native route and finite-law structures are executed. A choice remains a choice until enough control is supplied. One material packet is not duplicated into all possible exits. The report preserves incoming, accepted, blocked and remainder accounting. These transfers are atomic from input to output port. Interior sites certify path connectivity; this pass does not simulate travel time, impact or residence at those sites.

### L as a continuous bend

Draw the stem and continue around the corner without lifting.

A stream can follow the explicit turn.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| turn | top | joint | right |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| forward | top / run | turn | moved | 1 = 1 + 0 + 0 | right | True |
| backward | right / run |  | blocked | 1 = 0 + 1 + 0 |  | True |

- **forward:** One packet follows the declared turn from the top through the corner to the right.
- **backward:** The same visible bend does not imply a reverse route. That closed law admits no return.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### Attached corner, use unresolved

Draw the stem, lift, then attach the arm at the same site.

The pieces are joined, but the passage rule has not been supplied.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| turn | top | joint | right |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| unprovided-use | top / run |  | unknown | 1 = 0 + 0 + 1 |  | True |

- **unprovided-use:** Two strokes meet at a shared site, but the use law is open. Attachment is known; passage is not.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### Attached corner with passage

Draw the stem, lift, then attach the arm at the same site.

A stream can follow the explicit turn.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| turn | top | joint | right |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| connected-use | top / run | turn | moved | 1 = 1 + 0 + 0 | right | True |

- **connected-use:** An explicit passage makes the separately attached pieces behave like the continuous bend for this input.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### T as a terminal

The stem reaches a shared crossbar site.

A load or message can arrive without permission to leave through either arm.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| stop | bottom / run |  | blocked | 1 = 0 + 1 + 0 |  | True |

- **stop:** The empty closed law blocks the packet, which remains at its source.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### T as a through path with an unused stem

The bar is drawn continuously; the stem is attached later.

The bar can carry a stream while the stem remains an unselected possibility.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| across | left | joint | right |
| return | right | joint | left |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| across | left / run | across | moved | 1 = 1 + 0 + 0 | right | True |
| stem-blocked | bottom / run |  | blocked | 1 = 0 + 1 + 0 |  | True |

- **across:** The legal horizontal route crosses the branch site without turning.
- **stem-blocked:** A geometric T does not grant its stem an outgoing route.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### T as an unresolved choice

Three arms share a site.

Two legal outlets are alternatives for one packet, not copies of it.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| take-left | bottom | joint | left |
| take-right | bottom | joint | right |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| choose-later | bottom / run | take-left, take-right | choice | 1 = 0 + 0 + 1 |  | True |

- **choose-later:** Both alternatives are preserved. No packet moves until a choice is supplied.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### T as a controlled switch

Three arms share a site; a separate control is connected to the permission law.

The control selects an outlet while the other outlet remains physically present.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| take-left | bottom | joint | left |
| take-right | bottom | joint | right |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| control-left | bottom / left | take-left | moved | 1 = 1 + 0 + 0 | left | True |

- **control-left:** The selected control admits only the left outlet.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### T switch with uncertain control

Three arms share a site; a separate control is connected to the permission law.

The control selects an outlet while the other outlet remains physically present.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| take-left | bottom | joint | left |
| take-right | bottom | joint | right |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| either-control | bottom / left, right | take-left, take-right | choice | 1 = 0 + 0 + 1 |  | True |

- **either-control:** The control could select either side. The packet stays owned once at the source.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### T switch with an open control read

Three arms share a site; a separate control is connected to the permission law.

The control selects an outlet while the other outlet remains physically present.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| take-left | bottom | joint | left |
| take-right | bottom | joint | right |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| open-control | bottom / left | take-left | unknown | 1 = 0 + 0 + 1 |  | True |

- **open-control:** A known left possibility plus an open tail does not justify committing to left.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### T with uncertain permission to move

The stem can reach the right arm through a control-gated passage.

An uncertain control can mean move or hold, even when only one route is visible in the candidate union.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| outlet | bottom | joint | right |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| go-or-stop | bottom / go, stop | outlet | choice | 1 = 0 + 0 + 1 |  | True |
| two-controls-agree | bottom / go, also-go | outlet | moved | 1 = 1 + 0 + 0 | right | True |

- **go-or-stop:** There is only one candidate route, but one possible control blocks it. The packet cannot move yet.
- **two-controls-agree:** Both remaining controls permit the same route. This coarser action is certain even though the control is not.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### T as a material merge

The two arms and stem share one junction; two inlet routes point to one receiver.

The shared receiver limits the combined input. Material identity is retained, unlike a Boolean OR read.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| left-in | left | joint | bottom |
| right-in | right | joint | bottom |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| merge | left + right / run | left-in, right-in | moved | 2 = 2 + 0 + 0 | bottom | True |

- **merge:** Two independent packets enter one receiver with two sites. Both identities survive the merge.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### T merge with insufficient capacity

The two arms and stem share one junction; two inlet routes point to one receiver.

The shared receiver limits the combined input. Material identity is retained, unlike a Boolean OR read.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| left-in | left | joint | bottom |
| right-in | right | joint | bottom |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| capacity-conflict | left + right / run | left-in, right-in | blocked | 2 = 0 + 2 + 0 |  | True |

- **capacity-conflict:** One receiving site cannot hold two packets. The atomic batch is refused; neither packet is lost.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### H as a one-way bridge

Draw two separate stems, then connect their intermediate sites with a bar.

Connection is structural; direction of permitted use is an additional relation.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| outward | left |  | right |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| cross | left / run | outward | moved | 1 = 1 + 0 + 0 | right | True |
| return-blocked | right / run |  | blocked | 1 = 0 + 1 + 0 |  | True |

- **cross:** The crossbar connects the stems and permits left-to-right passage.
- **return-blocked:** Drawing a connector, or calling it a bridge, does not supply its return route.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### H as a two-way bridge

Draw two separate stems, then connect their intermediate sites with a bar.

Connection is structural; direction of permitted use is an additional relation.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| outward | left |  | right |
| return | right |  | left |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| outward | left / run | outward | moved | 1 = 1 + 0 + 0 | right | True |
| homeward | right / run | return | moved | 1 = 1 + 0 + 0 | left | True |

- **outward:** One packet crosses the explicit forward route.
- **homeward:** The same packet returns through a separately encoded reverse route.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### Cross as independent passages

Two strokes have coincident middle coordinates but different actual sites.

Geometric intersection does not create a material connection.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| horizontal | west | middle-h | east |
| vertical | south | middle-v | north |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| independent | west + south / run | horizontal, vertical | moved | 2 = 2 + 0 + 0 | east, north | True |

- **independent:** Two streams cross in the drawing but use separate middle sites and never exchange.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

### Cross as a shared turning junction

The horizontal and vertical strokes share their actual middle site.

A connected turning route changes the output direction.

| Route | Input | Passage | Output |
| --- | --- | --- | --- |
| turn | west | middle-h | north |

| Probe | Input / control | Candidates | Outcome | Incoming = accepted + blocked + remainder | Destination | Ownership preserved |
| --- | --- | --- | --- | --- | --- | --- |
| turn-at-cross | west / run | turn | moved | 1 = 1 + 0 + 0 | north | True |

- **turn-at-cross:** A genuinely shared site plus an explicit turning route lets the west stream leave north.

Limit: This fixture demonstrates connected permissions and packet accounting. It does not infer handwriting, mechanical stiffness, timing, or a physical load law from the outline.

## Ground, support and a selected use

These examples ask only where a supplied load position projects onto the chosen ground/reference, relative to an explicitly selected contact span. They do not infer actual contact from equal screen coordinates or certify mechanical stability. The same geometry can receive different contacts, a different load or a different ground frame.

### A: between two feet

Two legs end on the declared ground; the crossbar is a separate attachment.

A two-sided support candidate.

Supplied contacts: 2; validated contacts: 2; projection read: **inside**; native evidence complete: True.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

### A: move the load, keep the letter

Only the query location changes; the source shape and contacts are retained.

The same support no longer brackets the supplied load.

Supplied contacts: 2; validated contacts: 2; projection read: **outside**; native evidence complete: True.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

### V: a single contact

Both arms meet at one actual site on the ground.

A pivot or funnel is a plausible use; broad support is not supplied.

Supplied contacts: 1; validated contacts: 1; projection read: **boundary**; native evidence complete: True.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

### V: a small off-centre demand

One contact stays fixed while the selected load position changes.

A single contact does not bracket an offset load.

Supplied contacts: 1; validated contacts: 1; projection read: **outside**; native evidence complete: True.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

### Invert V: now two ground contacts

Reverse its vertical placement and declare the two lower endpoints as contacts.

The same path topology can serve as a two-legged support candidate.

Supplied contacts: 2; validated contacts: 2; projection read: **inside**; native evidence complete: True.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

### N: two feet, without a baseline bar

A continuous path visits both lower sites; they are explicitly chosen as contacts.

Bracing is a possible use, but the diagonal is not a material-strength proof.

Supplied contacts: 2; validated contacts: 2; projection read: **inside**; native evidence complete: True.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

### Z: a baseline bar

The bar's endpoints are declared contacts. No continuous contact pressure distribution is inferred.

This coarse contact read agrees with N, despite the different baseline structure.

Supplied contacts: 2; validated contacts: 2; projection read: **inside**; native evidence complete: True.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

### Z: change the supporting frame

Choose the left two endpoints and project across onto the wall instead of down onto a floor.

Ground is contextual: this read uses a vertical wall as its reference.

Supplied contacts: 2; validated contacts: 2; projection read: **inside**; native evidence complete: True.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

### T: a post and a top surface

The top and stem share an actual midpoint; the only declared floor contact is the post foot.

A central pedestal needs additional support assumptions.

Supplied contacts: 1; validated contacts: 1; projection read: **boundary**; native evidence complete: True.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

### H: two posts joined across

The crosspiece shares both mid-stem sites. A contact-span read says nothing about traversal permission.

A bridge or brace candidate with two supports.

Supplied contacts: 2; validated contacts: 2; projection read: **inside**; native evidence complete: True.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

### A: the contacts were not supplied

The source shape remains present; the selected contact inventory is empty.

A familiar outline is insufficient to answer this support query.

Supplied contacts: 0; validated contacts: 0; projection read: **unresolved**; native evidence complete: True.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

### A: the contact inventory is incomplete

One contact is supplied in an open inventory; other contacts remain unspecified.

A partial read must not confidently report outside the support.

Supplied contacts: 1; validated contacts: 0; projection read: **unresolved**; native evidence complete: False.

Limit: Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.

- These are authored constructions and contextual probes, not learned semantic categories.
- Contact-span inclusion is one geometric condition, not certification of stability. N and Z can agree under this coarse read.
- The local words count a shared uncalibrated reach. The renderer supplies pixels; no physical unit or universal glyph proportion is inferred.
- The native read is observational. It does not mutate the glyph, apply a force, or settle a mechanical structure.

## A small vocabulary of possible uses

These are proposed transfers of a connected mechanism, not assertions that a glyph means a particular object. The route experiments above exercise specific capabilities; the broader nouns still require further context.

### Bend

Glyph hints: L, U, V, corners of Z. An ordered handoff between two incident sections.

Use: Continue a cursor or packet around a change of heading.

Additional premises: A connected continuation route; history alone does not install it.

Limit: Does not establish a rigid joint, strength, or two-way traffic.

### Attachment

Glyph hints: F, E, T. A later element shares an existing endpoint or interior site.

Use: Add a branch, handle, joist, sensor or sign without replacing the carrier.

Additional premises: Shared site plus a selected coupling or route, if interaction is intended.

Limit: An attachment does not automatically give all incident paths permission to communicate.

### Bridge

Glyph hints: H, A crossbar. Two distinct local sites are joined by a third section.

Use: Exchange across previously separate strands or support a transverse span.

Additional premises: Separate directional permissions, capacity and receiver availability for material exchange.

Limit: One drawn bridge does not imply two-way use, simultaneous use or mechanical load bearing.

### Choice

Glyph hints: T, Y, K. One arrival can address several possible continuations.

Use: Select an outlet according to a control or retain unresolved alternatives.

Additional premises: A finite law and a rule for actual acceptance; uncertain prediction must not copy a packet.

Limit: A visual fork does not choose, prioritize or duplicate by itself.

### Merge

Glyph hints: Y, junctions of M and W. Several source routes meet one receiving site or passage.

Use: Combine observations, or compete for a material receiver.

Additional premises: An observation OR law or an explicit conserving transfer; these are different uses.

Limit: Participation OR is not addition of conserved material.

### Crossover

Glyph hints: X, crossed 4, Q tail. Two visible paths can coincide in projection while their sites remain distinct.

Use: Pass independently, or install an explicit exchange junction as an alternative.

Additional premises: Actual identity/coupling evidence; crossing ink is not enough.

Limit: The drawing cannot decide over/under, contact or weld.

### Enclosure

Glyph hints: A, B, D, O, P, R, 0, 6, 8, 9. Selected connected sections contain graph cycles.

Use: A loop, feedback route, container boundary or repeated circuit can be proposed.

Additional premises: A planar boundary read for area; a directed route and controller for recurrent execution.

Limit: An undirected graph cycle proves neither enclosed physical volume nor a running feedback loop.

### Support and range

Glyph hints: A, H, N, V, Z, T. A projected query lies between, on or outside selected contacts.

Use: A support-span check, bracketed estimate or between-anchors query.

Additional premises: Declared frame, contacts and projection; material mechanics need further laws.

Limit: No stiffness, mass distribution, friction, force balance or statics is solved here.

### Surface for activity

Glyph hints: T, H, table-like assemblies. A crosspiece exposes locations for additional relations.

Use: A workspace, exchange surface or display arrangement can attach to those sites.

Additional premises: The interacting participants, goals, actions and histories must be connected in the relevant context.

Limit: A table-shaped graph does not encode eating, paperwork, gambling or linguistic meanings by itself.

## What this establishes, and what stays open

The useful distinction is between an identical visible artifact and different connected histories or permissions. A shared endpoint can enable a bend, an attachment, a junction or a bridge under different laws. An independent crossing can look like a shared crossing. Different views can reuse the same section without creating a second material section.

This pass supplies fixtures and laws; it does not autonomously discover meanings, infer a person's handwriting plan, learn a noun vocabulary, or derive forces from line art. Flow probes use the declared finite packet model. Support examples are conditional geometric reads. A general workspace, mechanical settlement and learned choice of interpretation remain further work.
