using Gojo.Core;
using Network = ConnectionAssembly.AssemblyNetwork;

static GojoEncodedSnapshot Snapshot(params IElement[] roots) =>
    GojoEncodedSnapshot.CaptureGraph(roots);

static HashSet<ulong> RelationIds(GojoEncodedSnapshot snapshot) =>
    snapshot.Relations.Select(relation => relation.Id).ToHashSet();

static HashSet<ulong> CarrierIds(GojoEncodedSnapshot snapshot) =>
    snapshot.Carriers.Select(carrier => carrier.Id).ToHashSet();

static void Report(string title, GojoEncodedSnapshot snapshot, GojoEncodedSnapshot? baseline = null)
{
    var relationIds = RelationIds(snapshot);
    var carrierIds = CarrierIds(snapshot);
    if (baseline is not null)
    {
        relationIds.ExceptWith(RelationIds(baseline));
        carrierIds.ExceptWith(CarrierIds(baseline));
    }

    var attachmentCount = snapshot.Carriers
        .SelectMany(carrier => carrier.AttachedRelationIndexes ?? [])
        .Distinct()
        .Count();
    Console.WriteLine($"{title}: C={carrierIds.Count}, R={relationIds.Count}, totalC={snapshot.Carriers.Length}, totalR={snapshot.Relations.Length}, A={attachmentCount}");
}

var root = Network.Create();
var initial = Snapshot(root);
Report("created", initial);
var rawIds = Network.Nodes(root).Select(Network.Raw).Select(carrier => carrier.Id).ToArray();
var linkIds = Network.Links(root).Select(link => link.Id).ToArray();
var nodeIds = Network.Nodes(root).Select(node => node.Id).ToArray();
var constraintIds = Network.Nodes(root).Select(node => ((Relation)Network.Parts(node)[1]).Id).ToArray();
var operational = Network.Operational(root);
var initialOperational = Snapshot(operational);
Report("created-operational", initialOperational);

Relation witness = null!;
for (var step = 1; step <= 240; step++)
{
    var phase = (step - 1) % 16;
    long value = phase < 8 ? 2 * (phase + 1) : 2 * (15 - phase);
    if (!Network.TryObserve(root, step, value, out root, out witness))
    {
        throw new Exception("failed");
    }
}

var complete = Snapshot(root);
Report("step240", complete);
var currentOperational = Snapshot(Network.Operational(root));
Report("step240-operational", currentOperational);
Report("operational new compared with creation", currentOperational, initialOperational);
Report("operational lost compared with creation", initialOperational, currentOperational);
var rootParts = Network.Parts(root);
var witnessParts = Network.Parts(witness);

// The admission envelope's final participant is the selected latest sample.
// Its preceding participants retain the reusable physical network and live
// instruction graph. The envelope is not itself a newly constructed network.
var baseGraph = Snapshot(rootParts.Take(7).ToArray());
Report("base-without-last-witness", baseGraph);
var stateGraph = Snapshot(rootParts[0], rootParts[1], rootParts[3],
    rootParts[4], rootParts[5], rootParts[6]);
Report("state-and-links-without-node-wrappers", stateGraph);
Report("latest-witness-exclusive", complete, baseGraph);

var includedParts = rootParts.Take(7).ToList();
string[] witnessNames = ["input", "admission", "nodes", "links", "transitions", "trace"];
for (var index = 0; index < witnessParts.Length; index++)
{
    var nextParts = includedParts.Append(witnessParts[index]).ToArray();
    Report("witness " + witnessNames[index] + " own", Snapshot(witnessParts[index]), baseGraph);
    Report("witness " + witnessNames[index] + " incremental",
        Snapshot(nextParts), Snapshot(includedParts.ToArray()));
    includedParts.Add(witnessParts[index]);
}

var nodeSamples = Network.NodeSamples(witness);
var linkSamples = Network.LinkSamples(witness);
string[] nodeNames = ["identity", "values", "constraint-reads", "accounting", "unmet-reads", "engagement-update"];
string[] linkNames = ["identity", "values", "before", "ablations", "support-update", "gate-claim"];
for (var index = 0; index < nodeNames.Length; index++)
{
    var participants = nodeSamples.Select(node => Network.Parts(node)[index]).ToArray();
    Report("all node " + nodeNames[index], Snapshot(participants), baseGraph);
}
for (var index = 0; index < linkNames.Length; index++)
{
    var participants = linkSamples.Select(link => Network.Parts(link)[index]).ToArray();
    Report("all link " + linkNames[index], Snapshot(participants), baseGraph);
}

var transitions = Network.Parts((Relation)witnessParts[4]);
// These ordered participants are experiment-specific selected evidence, not a
// universal step schema. Number them without assigning old reconstruction
// labels to a newer compact witness layout.
for (var index = 0; index < transitions.Length; index++)
{
    Report("transition participant " + index, Snapshot(transitions[index]), baseGraph);
}

// Keep capture and reporting outside the admission allocation measurement.
var graphBefore = complete;
long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
if (!Network.TryObserve(root, 241, 2, out root, out witness))
{
    throw new Exception("failed");
}
Console.WriteLine("step241 allocation=" + (GC.GetAllocatedBytesForCurrentThread() - allocatedBefore));

var graphAfter = Snapshot(root);
Report("step241", graphAfter);
Report("step241 new compared with240", graphAfter, graphBefore);
Report("step240 lost compared with241", graphBefore, graphAfter);
Console.WriteLine("stable raw identity=" +
    rawIds.SequenceEqual(Network.Nodes(root).Select(Network.Raw).Select(carrier => carrier.Id)));
Console.WriteLine("stable link identity=" +
    linkIds.SequenceEqual(Network.Links(root).Select(link => link.Id)));
Console.WriteLine("stable node identity=" +
    nodeIds.SequenceEqual(Network.Nodes(root).Select(node => node.Id)));
Console.WriteLine("stable constraint identity=" +
    constraintIds.SequenceEqual(Network.Nodes(root).Select(node => ((Relation)Network.Parts(node)[1]).Id)));
Console.WriteLine("stable operational root identity=" + ReferenceEquals(operational, Network.Operational(root)));
Console.WriteLine("all original operational relations retained=" +
    RelationIds(initialOperational).IsSubsetOf(RelationIds(Snapshot(Network.Operational(root)))));

var groupHolderIds = new HashSet<ulong>();
var visitedRelationIds = new HashSet<ulong>();
var scaffoldRelationIds = new HashSet<ulong>();
var visitedCarrierIds = new HashSet<ulong>();

// Count each ordered group's anchor, member attachments and chain, stopping
// before its participants so their independent structure is not scaffolding.
void VisitGroupScaffold(IElement element, Carrier holder)
{
    if (element is not Relation relation)
    {
        return;
    }

    scaffoldRelationIds.Add(relation.Id);
    if (relation.Left is CarrierLocation location && relation.Right is Carrier carrier &&
        carrier.SameIdentityAs(holder) && location.Carrier.SameIdentityAs(holder))
    {
        return;
    }

    if (relation.Left is Relation point && point.Left is CarrierLocation pointLocation &&
        point.Right is Carrier pointCarrier && pointCarrier.SameIdentityAs(holder) &&
        pointLocation.Carrier.SameIdentityAs(holder))
    {
        scaffoldRelationIds.Add(point.Id);
        return;
    }

    VisitGroupScaffold(relation.Left, holder);
    VisitGroupScaffold(relation.Right, holder);
}

void Visit(IElement element)
{
    if (element is CarrierLocation location)
    {
        Visit(location.Carrier);
        return;
    }

    if (element is Carrier carrier)
    {
        if (visitedCarrierIds.Add(carrier.Id))
        {
            foreach (var attachment in carrier.AttachedRelations)
            {
                Visit(attachment);
            }
        }
        return;
    }

    if (element is not Relation relation || !visitedRelationIds.Add(relation.Id))
    {
        return;
    }

    if (relation.TryReadOrderedGroup(out _) && relation.Right is Carrier holder)
    {
        groupHolderIds.Add(holder.Id);
        scaffoldRelationIds.Add(relation.Id);
        VisitGroupScaffold(relation.Left, holder);
    }
    Visit(relation.Left);
    Visit(relation.Right);
}

Visit(root);
Console.WriteLine("ordered group holder identities=" + groupHolderIds.Count);
Console.WriteLine("ordered group scaffold relation identities=" + scaffoldRelationIds.Count);
