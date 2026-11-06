# Eight named construction engines and five joint controls

A name here is a way to find a known starting machine. After selection, the pen receives its row bytes and bound extents. A running key can lose its common seam when a row changes. A spiral can lose its expected terminal when a stop is removed. Names and catalogue descriptions never adjudicate these cases at execution.

## Catalogue

The shared direction slots are east, northeast, north, northwest, west, southwest, south and southeast in that order. Row lengths reference A, B, A+B or the pitch. Several rows using A therefore change together when A is reinterpreted. The selected number of vertical slots belongs to the tension run's admissible drawing space. It is independent of the number of rows stored in a body.

| Engine | Ordered construction | Exposed variation and boundary |
| --- | --- | --- |
| Running key | East B, north A, west B, north B, east A+B, south A+B, east B. | Pitch A+2B brings the end to the material anchor of the next bay. This is the previous [open-course feasibility control](../OrnamentOpenMeander/README.md), now explicitly offered as an authored prior. |
| Inward square spiral | Three outer sides of extent A+B, a shorter descent A, an inward horizontal A, then two shorter sides B. The last row has a stop. | For A>B the route normally ends inside its coil. A small pitch can let subsequent coils collide. A=B can collapse clearance. A separate disconnected spiral in the next bay is legitimate for this brief. |
| Through zigzag | Northeast A then southeast A. | Pitch 2A creates a continuing seam. The two links retain an ordered joint. |
| Opposed chevrons | Two such zigzags, one in a reflected vertical frame, share a projected baseline. | Their meeting tips remain independent material sites unless a later explicit binding joins them. This permits a lozenge read without forcing a four-way joint. |
| Crossing ribbons | Two zigzags begin at 0 and A. The second descends before ascending. | The two courses can cross in an edge interior, including between integer sites. A transverse crossing leaves their material flows distinct. |
| Branching spray | A main pen draws two horizontal A steps. Two side pens read the prior beat's exposed stem site and activity, leave it in opposite diagonal directions, then draw outward and stop. | At pitch 2A there is both a continuing stem and a shared four-arm site. Disconnecting the stem publication removes the source for each limb. |
| Alternating pinline | Five steps draw a flat lead, a diagonal shoulder, a crown, the returning shoulder and a flat tail. A second pen draws a rule in a separate register. | The owner toggles its writable vertical frame on each beat. The odd-length body reflects the next crown while preserving the connected pitch 2A+3B. In the frozen-frame control the reflected relation changes. |
| Counterstep | Two six-row ascending/descending step bodies occupy different origin lanes and opposed frames. | At pitch A+2B each can continue through its own seam. The whole band can exhibit mirror relations at a larger scale than the individual turns. |

`EngineLibrary.Create(family)` emits the canonical program as native packed path bytes. `Create(rng,family,edits)`, `Mutate` and `Cross` reuse the same native row and owner changes as the previous tournament. A mutation can move a port, change an extent reference, edit a Boolean law, insert or delete a row, or exchange an owner. Program names remain provenance for the training parent. They cannot prevent a variant from drawing something substantially different.

The catalogue borrows construction questions from historical material. Meyer’s fret plates 81–84 show band bodies as well as ends and corners. His spiral-band chapter treats a related construction separately. The Met's fourteenth-century textile fragment 90.5.429 combines zigzags and diamonds in successive scales. The Met's Attic krater 14.130.14 places distinct treatments in different registers. Our supplied angular engines test returns, nested gaps, register separation and larger symmetry. Their eight templates are authored experiments and make no claim to recover an artisan’s historical drawing order. [Meyer, *A Handbook of Ornament*](https://upload.wikimedia.org/wikipedia/commons/7/71/Handbook_of_ornament%3B_a_grammar_of_art%2C_industrial_and_architectural_designing_in_all_its_branches%2C_for_practical_as_well_as_theoretical_use_%28IA_handbookoforname1900meye%29.pdf) · [Met textile fragment 90.5.429](https://www.metmuseum.org/art/collection/search/443915) · [Met krater 14.130.14](https://www.metmuseum.org/art/collection/search/248904).

## Reading an X back from history

For a four-ray projected contact, the observer asks where the pen continued, which directed rays arrive and leave, and which of those strokes share an actual site. Half-step coordinates preserve diagonal intersections between the calibrated sites. A positive-length collinear intersection is counted separately for each overlapping pair. Two collinear segments that only meet end to end add no overlap.

| Control | What was actually made | Result |
| --- | --- | --- |
| Opposed Vs | Two native pens each turn at its own center site. The two centers happen to share a position. | Two opposed turns, zero four-arm material sites. |
| Straight cross | Two native pens pass through the center inside a single diagonal stroke each. | One transverse crossing, zero collinear overlaps, zero material center sites. |
| Shared four | A pen reaches the center and publishes it. The next pen explicitly targets it. Each then draws a different outgoing arm from that one site. | One physical four-arm site, two incoming and two outgoing rays. |
| Four arrivals | A constructed observer fixture directs four independently arriving arms to the same central site. | One four-in terminal. The fixture tests the read of this topology separately from what the three-owner program can construct. |
| Collinear overlap | A native pen deposits a long horizontal line. Another pen deposits part of it again on the same supporting line. | One positive-length overlap and zero transverse crossings. |

The first four controls have exactly the same unique line support. The first three execute actual native row programs. A separate focused test moves a transverse crossing to (½,½), demonstrating that a legal crossing does not need an integer site there. Mark coincidence, site identity and straight-through continuation are three different questions.

`JointCensus.Read(trace,details:true)` publishes the contact locations in double coordinates, incident material sites, arm masks, the number of interior pairs, and the traced through axes. `TurnPairs` counts successive signed changes of direction along the *same owner’s identity-connected stroke chain*. `ForkPairs` counts relative incoming/outgoing ray masks at each three-or-more-direction site from each incoming perspective. A fork accepting two incoming lanes therefore contributes two such reads. A loose tip here is a one-ray sink away from the far boundary, with neither a start witness there nor an explicit stop. A disconnected seam has its own contract; loose-tip counting alone cannot judge it.

Geometric equality of output masks establishes how much a viewer can distinguish at that resolution. It does not establish common producing machinery or an individual's preference for either result. The joint census reads events after drawing and never edits a path.
