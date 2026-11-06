# Can a local seam marker help?

This follow-up changes what the existing inspector sees on port 10. The original
program receives immutable seed ink there. One experimental arm replaces it
with a single mark at site zero, adjacent to the fixed repeat boundary between
the last site and zero. A third arm supplies one decoy mark, outside the two
edge neighborhoods that the seam judge allows to change. The decoy position is
determined by a hash of the parent group and reflects with its sibling variant.

The initial material, protected sites, hidden observations, starting phase,
available stock, edit budget, disturbance and goal stay the same within every
comparison. Visibility, existing ink and protection can differ between the
two marker locations. The case export records those properties. A correct
mark supplies location only. The receiver must still obtain the desired seam
gap and control its edits through its stored rows. Replacing original ink
with a single marker changes both the information source and its sparsity.
The matched decoy supplies the control for marker size.

~~~powershell
dotnet build experiments/OrnamentSeamFollowup -c Release
dotnet experiments/OrnamentSeamFollowup/bin/Release/net10.0/OrnamentSeamFollowup.dll check
dotnet experiments/OrnamentSeamFollowup/bin/Release/net10.0/OrnamentSeamFollowup.dll freeze RUN PRIMARY PROSPECTIVE-SPEC.md
dotnet experiments/OrnamentSeamFollowup/bin/Release/net10.0/OrnamentSeamFollowup.dll train RUN PRIMARY 8
dotnet experiments/OrnamentSeamFollowup/bin/Release/net10.0/OrnamentSeamFollowup.dll lock RUN PRIMARY
dotnet experiments/OrnamentSeamFollowup/bin/Release/net10.0/OrnamentSeamFollowup.dll assay RUN PRIMARY
~~~

Use a fresh run folder for each protocol. The freeze binds the prospective
design, host assembly, exact primary kernel assembly, primary winner lock and
completed primary blind assessment. Each of four independent replicates tries
the same 4,096 physical programs under all three cue treatments. The first is
a holding machine, and the others are uniform samples of the original program
grammar. Programs train against 32 matched seam episodes. The top eight
distinct programs for each treatment receive validation trials, then one
winner per treatment and replicate is frozen. Replicate pools have recoverable
checksums. The reserve phase crosses every frozen winner with each of the three
cue publications, includes the holding baseline, and reports per-parent-group
outcomes as well as the aggregate comparison. This experiment never supplies
the final desired image or adds a semantic seam instruction to the kernel.

After the completed campaign, run the independent replay verifier:

~~~powershell
python experiments/OrnamentSeamFollowup/Verify.py artifacts/local-ornament
~~~

It rebuilds the input from the frozen episode ledger, verifies the twelve
winner genotypes against their proposal pools, and launches the preserved
primary runtime from a fresh process. A separate native reference host checks
the same inputs against the slower relation evaluator. All recorded material
outcomes, idle comparisons, channel-cut controls and grouped verdicts are
checked. The certificate and compressed replay rows are written under
the verification directory, outside the sealed run folders.
