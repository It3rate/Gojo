using System.Text.Json;
using Gojo.Core;
using N = ConnectionAssembly.NativeMath;

namespace LocalServiceGrowth;

internal static class DiscoveryChecks
{
    public static int Run()
    {
        UniqueContributionAndLifecycle();
        BasisAndResourceSeparation();
        BoundedNeighborhood();
        UnresolvedCursorAndGate();
        NativeContinuation();
        return 5;
    }

    private static void UniqueContributionAndLifecycle()
    {
        var advert = LocalDiscovery.Advertise(new Carrier());
        var consumer = N.Group(new Carrier());
        var first = LocalDiscovery.Register(advert, consumer);
        var again = LocalDiscovery.Register(advert, consumer);
        Require(ReferenceEquals(LocalDiscovery.Gate(first), LocalDiscovery.Gate(again)) &&
            ReferenceEquals(LocalDiscovery.Qualified(first), LocalDiscovery.Qualified(again)),
            "Repeat registration shares the same contribution sites.");
        LocalDiscovery.Update(first, true, true);
        LocalDiscovery.Update(again, true, true);
        Counts(advert, 1, 1);
        LocalDiscovery.Update(first, true, null);
        Counts(advert, 1, 1);
        LocalDiscovery.Update(first, true, false);
        Counts(advert, 1, 0);
        var second = LocalDiscovery.Register(advert, N.Group(new Carrier()));
        LocalDiscovery.Update(second, true, true);
        Counts(advert, 2, 1);
        LocalDiscovery.Update(first, true, true);
        Counts(advert, 2, 2);
        LocalDiscovery.Update(first, false, null);
        LocalDiscovery.Update(again, false, false);
        Counts(advert, 1, 1);
        LocalDiscovery.Update(first, true, null);
        Counts(advert, 2, 1);
        LocalDiscovery.Update(second, false, true);
        Counts(advert, 1, 0);
        Require(LocalDiscovery.Gate(second).Value == 0 && LocalDiscovery.Qualified(second).Value == 0,
            "A detached use cannot retain qualification even if passed a positive outcome.");
    }

    private static void BasisAndResourceSeparation()
    {
        var supplierWallet = new Carrier();
        supplierWallet.Add(17);
        var buyerWallet = new Carrier();
        buyerWallet.Add(10);
        var outputBasis = new Carrier();
        var exposedPort = Relation.Segment(outputBasis, 0, 4);
        var service = N.Group(exposedPort, supplierWallet);
        var consumer = N.Group(buyerWallet, Relation.Segment(outputBasis, 0, 1));
        var a = LocalDiscovery.Advertise(service);
        var b = LocalDiscovery.Advertise(N.Group(exposedPort));
        var useA = LocalDiscovery.Register(a, consumer);
        var useB = LocalDiscovery.Register(b, consumer);
        LocalDiscovery.Update(useA, true, true);
        LocalDiscovery.Update(useB, true, false);
        Counts(a, 1, 1);
        Counts(b, 1, 0);
        Require(ReferenceEquals(LocalDiscovery.Service(a), service) && ReferenceEquals(N.Parts(useA)[1], consumer),
            "Service and consumer basis remain connected to the public use surface.");
        Require(supplierWallet.Value == 17 && buyerWallet.Value == 10 && supplierWallet.Tension == 0 && buyerWallet.Tension == 0,
            "Use counts cannot mint support, transfer fees, or reward their own popularity.");
        Require(supplierWallet.AttachedRelations.Count == 0 && buyerWallet.AttachedRelations.Count == 0 &&
            outputBasis.AttachedRelations.Count == 0,
            "The thin incidence ledger does not attach private service or consumer recipes.");
    }

    private static void BoundedNeighborhood()
    {
        var adverts = Enumerable.Range(0, 9).Select(_ => LocalDiscovery.Advertise(new Carrier())).ToArray();
        var neighborhood = LocalDiscovery.Neighborhood(new Carrier(), adverts.Take(8).ToArray());
        var visits = new List<Relation>();
        for (var i = 0; i < 4; i++)
        {
            var read = LocalDiscovery.ReadNext(neighborhood);
            var selected = LocalDiscovery.Visited(read);
            Require(selected.Length == 2, "One local discovery request reads at most two advertised references.");
            visits.AddRange(selected);
            Require(!selected.Contains(adverts[8]), "An outside offer cannot be found through a global fallback.");
        }
        Require(visits.SequenceEqual(adverts.Take(8)) && LocalDiscovery.Cursor(neighborhood).Value == 0,
            "The native cursor visits the supplied local order and wraps at its own boundary.");
        Require(LocalDiscovery.Visited(LocalDiscovery.ReadNext(LocalDiscovery.Neighborhood(new Carrier(), adverts[0]))).Length == 1,
            "One available neighbor cannot appear as two endorsements or two candidates.");
        Require(LocalDiscovery.Visited(LocalDiscovery.ReadNext(LocalDiscovery.Neighborhood(new Carrier(), adverts[0], adverts[0]))).Length == 1,
            "Duplicate local edges to the same physical advertisement are not additional candidates.");
        Require(LocalDiscovery.Visited(LocalDiscovery.ReadNext(LocalDiscovery.Neighborhood(new Carrier()))).Length == 0,
            "An empty local space stays empty.");
        var before = LocalDiscovery.Cursor(neighborhood).Value;
        Require(LocalDiscovery.Visited(LocalDiscovery.ReadNext(neighborhood, 0)).Length == 0 &&
            LocalDiscovery.Cursor(neighborhood).Value == before, "A zero-work request does not advance discovery.");
        Require(Throws(() => LocalDiscovery.ReadNext(neighborhood, 3)) &&
            Throws(() => LocalDiscovery.Neighborhood(new Carrier(), adverts)),
            "The supplied experiment explicitly refuses unbounded discovery budgets and neighborhood sizes.");
    }

    private static void UnresolvedCursorAndGate()
    {
        var advert = LocalDiscovery.Advertise(new Carrier());
        var neighborhood = LocalDiscovery.Neighborhood(new Carrier(), advert);
        LocalDiscovery.Cursor(neighborhood).Set(9);
        var before = Canonical(neighborhood);
        var read = LocalDiscovery.ReadNext(neighborhood);
        Require(LocalDiscovery.Visited(read).Length == 0 && Canonical(neighborhood) == before &&
            N.Parts(LocalDiscovery.Evidence(read)).Length > 0,
            "An out-of-range cursor exposes failed local traversal without wrapping an invented index.");
        var underdrawn = LocalDiscovery.Neighborhood(new Carrier(), advert);
        LocalDiscovery.Cursor(underdrawn).Set(-1);
        read = LocalDiscovery.ReadNext(underdrawn);
        Require(LocalDiscovery.Visited(read).Length == 0 && LocalDiscovery.Cursor(underdrawn).Tension == -1,
            "An unresolved cursor is not interpreted as a concrete zero index.");
        var use = LocalDiscovery.Register(advert, new Carrier());
        LocalDiscovery.Gate(use).Set(-1);
        before = Canonical(use);
        LocalDiscovery.Update(use, true, true);
        Require(Canonical(use) == before && LocalDiscovery.Gate(use).Tension == -1,
            "An unsettled contribution is left visible rather than repaired into another vote.");
    }

    private static void NativeContinuation()
    {
        var consumer = N.Group(new Carrier());
        var first = LocalDiscovery.Advertise(N.Group(new Carrier()));
        var second = LocalDiscovery.Advertise(N.Group(new Carrier()));
        var use = LocalDiscovery.Register(first, consumer);
        var neighborhood = LocalDiscovery.Neighborhood(consumer, first, second);
        LocalDiscovery.Update(use, true, true);
        LocalDiscovery.ReadNext(neighborhood, 1);
        var root = N.Group(first, second, consumer, use, neighborhood);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single();
        Require(Canonical(root) == Canonical(clone), "Native transport retains counters, cursor, identities and thin incidences.");
        var copied = N.Parts(clone);
        var cloneFirst = (Relation)copied[0];
        var cloneConsumer = (Relation)copied[2];
        var cloneUse = (Relation)copied[3];
        var registered = LocalDiscovery.Register(cloneFirst, cloneConsumer);
        Require(ReferenceEquals(LocalDiscovery.Gate(registered), LocalDiscovery.Gate(cloneUse)),
            "Restored incidence references find the existing contribution without an external registry.");
        LocalDiscovery.Update(use, true, null);
        LocalDiscovery.Update(cloneUse, true, null);
        LocalDiscovery.ReadNext(neighborhood);
        LocalDiscovery.ReadNext((Relation)copied[4]);
        Require(Canonical(root) == Canonical(clone), "Missing outcomes and future local searches continue identically after reload.");
        LocalDiscovery.Update(use, false, null);
        LocalDiscovery.Update(cloneUse, false, null);
        Counts(first, 0, 0);
        Counts(cloneFirst, 0, 0);
        Require(Canonical(root) == Canonical(clone), "Withdrawal retracts restored contributions exactly once.");
    }

    private static void Counts(Relation advert, long active, long useful)
    {
        Require(LocalDiscovery.ActiveCount(advert).ValueSlot.Ticks == active &&
            LocalDiscovery.SuccessfulCount(advert).ValueSlot.Ticks == useful && useful <= active &&
            LocalDiscovery.ActiveCount(advert).Tension == 0 && LocalDiscovery.SuccessfulCount(advert).Tension == 0,
            "Public counters equal unique current contribution states, with qualification bounded by attachment.");
    }

    private static string Canonical(Relation root)
    {
        var snapshot = GojoEncodedSnapshot.CaptureGraph(root);
        return JsonSerializer.Serialize(snapshot with
        {
            Carriers = snapshot.Carriers.Select(row => row with { Id = 0 }).ToArray(),
            TickRefs = snapshot.TickRefs.Select(row => row with { Id = 0 }).ToArray(),
            Relations = snapshot.Relations.Select(row => row with { Id = 0 }).ToArray(),
            UnitShapes = snapshot.UnitShapes.Select(row => row with { Id = 0 }).ToArray()
        });
    }

    private static bool Throws(Action action)
    {
        try { action(); } catch (ArgumentOutOfRangeException) { return true; }
        return false;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Local discovery check failed: " + message);
    }
}
