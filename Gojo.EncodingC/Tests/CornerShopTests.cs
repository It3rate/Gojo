using Xunit;
using Xunit.Abstractions;

namespace Gojo.EncodingC.Tests;

public sealed class CornerShopTests(ITestOutputHelper output)
{
    [Fact]
    public void ABookedSupplierCanBeReplacedOnlyWithAuthorizationAndItsReceiptReplays()
    {
        var shop = CornerShop.Create();
        output.WriteLine("Phase                               A cases/booked/received  B cases/booked/received  Waiting Dock Shelf Backorders Pickup Collected Target Deficit Excess");
        Trace("Customer request posted", shop);

        Assert.False(shop.Step());
        Settle(shop);
        Assert.True(shop.Step());
        var blocked = Trace("Dock has no room for the case", shop);
        Assert.Equal((2, true, true, 0), SupplierState(blocked.A));
        Assert.Equal((2, false, false, 0), SupplierState(blocked.B));
        Assert.Equal((0, 1, 0, 1), (blocked.WaitingRequests, blocked.Dock, blocked.Shelf, blocked.Backorders));
        Assert.Equal((2, 2, 0), (blocked.Target, blocked.Deficit, blocked.Excess));

        shop.SetAvailability(Supplier.A, false);
        shop.SetAvailability(Supplier.B, true);
        Settle(shop);
        var waiting = Trace("A unavailable, booking held", shop);
        Assert.Equal((2, false, true, 0), SupplierState(waiting.A));
        Assert.Equal((2, true, false, 0), SupplierState(waiting.B));
        Assert.Equal((1, 0, 1, 0), (waiting.Dock, waiting.Shelf, waiting.Backorders, waiting.Pickup));

        var saved = shop.Save();
        var restored = shop.Restore(saved);
        Assert.Equal(saved, restored.Save());
        Assert.Equal(waiting, restored.Read());

        shop.SetRebooking(true);
        Assert.Equal(saved, restored.Save());
        restored.SetRebooking(true);
        var rebooked = StepBoth("Authorized booking moves to B", quiet: false);
        Assert.Equal((2, false, false, 0), SupplierState(rebooked.A));
        Assert.Equal((2, true, true, 0), SupplierState(rebooked.B));
        Assert.Equal((1, 0, 1, 0), (rebooked.Dock, rebooked.Shelf, rebooked.Backorders, rebooked.Pickup));
        StepBoth("Booking clears demand for choice", quiet: false);
        StepBoth("B waits for whole-case room", quiet: true);

        shop.SetUnloading(true);
        restored.SetUnloading(true);
        var received = StepBoth("Serve and receive B's case", quiet: false);
        Assert.Equal((2, false, false, 0), SupplierState(received.A));
        Assert.Equal((1, true, false, 1), SupplierState(received.B));
        Assert.Equal((2, 0, 0, 1), (received.Dock, received.Shelf, received.Backorders, received.Pickup));
        Assert.Equal((0, 0), (received.Deficit, received.Excess));

        StepBoth("Shelve first item from B", quiet: false);
        var shelved = StepBoth("Shelve second item from B", quiet: false);
        Assert.Equal((0, 2, 1, 0), (shelved.Dock, shelved.Shelf, shelved.Pickup, shelved.Collected));
        StepBoth("Pickup waits", quiet: true);

        shop.SetCollection(true);
        restored.SetCollection(true);
        var collected = StepBoth("Customer collects exactly one", quiet: false);
        Assert.Equal((0, 0, 1), (collected.Backorders, collected.Pickup, collected.Collected));

        var finished = StepBoth("Quiet at target", quiet: true);
        Assert.Equal((2, false, false, 0), SupplierState(finished.A));
        Assert.Equal((1, true, false, 1), SupplierState(finished.B));
        Assert.Equal((0, 2, 0, 0), (finished.Dock, finished.Shelf, finished.Deficit, finished.Excess));
        Assert.Equal(collected, finished);

        CornerShopReading StepBoth(string phase, bool quiet)
        {
            Assert.Equal(quiet, shop.Step());
            Assert.Equal(quiet, restored.Step());
            Assert.Equal(shop.Save(), restored.Save());
            var state = Trace(phase, shop);
            Assert.Equal(state, restored.Read());
            return state;
        }
    }

    [Fact]
    public void CurrentSupplierFactsReopenChoiceAndOnlyDemandRaisesTension()
    {
        var shop = CornerShop.Create();
        shop.SetAvailability(Supplier.B, true);
        var open = ReadSettled();
        Assert.Equal((true, true), (open.A.Eligible, open.B.Eligible));
        Assert.Equal((true, false, 2), (open.ChoiceOpen, open.ChoiceContradiction, open.Deficit));

        shop.SetTarget(0);
        var undemandedChoice = ReadSettled();
        Assert.Equal((true, true), (undemandedChoice.A.Eligible, undemandedChoice.B.Eligible));
        Assert.Equal((false, false, 0), (undemandedChoice.ChoiceOpen, undemandedChoice.ChoiceContradiction, undemandedChoice.Deficit));

        shop.SetAvailability(Supplier.A, false);
        shop.SetAvailability(Supplier.B, false);
        var undemandedFailure = ReadSettled();
        Assert.Equal((false, false), (undemandedFailure.A.Eligible, undemandedFailure.B.Eligible));
        Assert.Equal((false, false, 0), (undemandedFailure.ChoiceOpen, undemandedFailure.ChoiceContradiction, undemandedFailure.Deficit));

        shop.SetTarget(2);
        var contradiction = ReadSettled();
        Assert.Equal((false, true, 2), (contradiction.ChoiceOpen, contradiction.ChoiceContradiction, contradiction.Deficit));

        shop.SetAvailability(Supplier.B, true);
        Settle(shop);
        var recovered = shop.Read();
        AssertLedgers(recovered);
        Assert.Equal((2, false, false, 0), SupplierState(recovered.A));
        Assert.Equal((2, true, true, 0), SupplierState(recovered.B));
        Assert.Equal((false, false, 2), (recovered.ChoiceOpen, recovered.ChoiceContradiction, recovered.Deficit));

        CornerShopReading ReadSettled()
        {
            Settle(shop);
            var state = shop.Read();
            AssertLedgers(state);
            Assert.False(state.A.Booked);
            Assert.False(state.B.Booked);
            return state;
        }
    }

    [Fact]
    public void ADisconnectedStockPortKeepsItsBookingUntilItReturns()
    {
        var shop = CornerShop.Create();
        Assert.False(shop.Step());
        shop.ConnectSupplier(Supplier.A, false);
        shop.SetUnloading(true);
        Settle(shop);

        var disconnected = shop.Read();
        AssertLedgers(disconnected);
        Assert.Equal((2, true, true, 0), SupplierState(disconnected.A));
        Assert.Equal((0, 0, 0, 1), (disconnected.Dock, disconnected.Shelf, disconnected.Backorders, disconnected.Pickup));

        var restored = shop.Restore(shop.Save());
        Assert.True(restored.Step());
        Assert.Equal(disconnected, restored.Read());

        restored.SetTarget(0);
        Settle(restored);
        Assert.True(restored.Read().A.Booked);
        restored.ConnectSupplier(Supplier.A, true);
        Assert.False(restored.Step());
        var received = restored.Read();
        AssertLedgers(received);
        Assert.Equal((1, true, false, 1), SupplierState(received.A));
        Assert.Equal((2, false, false, 0), SupplierState(received.B));
        Assert.Equal((2, 0, 1, 0), (received.Dock, received.Shelf, received.Pickup, received.Collected));
        Assert.Equal((0, 2), (received.Deficit, received.Excess));
    }

    [Fact]
    public void NewCustomerRequestsWaitAtFullPickupAndCanBookAnotherCase()
    {
        var shop = CornerShop.Create();
        shop.SetUnloading(true);
        shop.SetCollection(true);
        Settle(shop);
        var firstSale = shop.Read();
        AssertLedgers(firstSale);
        Assert.Equal((1, true, false, 1), SupplierState(firstSale.A));
        Assert.Equal((2, 1), (firstSale.Shelf, firstSale.Collected));

        shop.SetCollection(false);
        Assert.True(shop.PostRequest());
        Assert.True(shop.PostRequest());
        Assert.False(shop.PostRequest());
        Settle(shop);
        var waiting = shop.Read();
        AssertLedgers(waiting, customerRequests: 3);
        Assert.Equal((0, false, false, 2), SupplierState(waiting.A));
        Assert.Equal((1, 2, 1, 1, 1), (waiting.Dock, waiting.Shelf, waiting.Backorders, waiting.Pickup, waiting.Collected));

        shop.SetCollection(true);
        Settle(shop);
        var collected = shop.Read();
        AssertLedgers(collected, customerRequests: 3);
        Assert.Equal((0, 2, 0, 0, 3), (collected.Dock, collected.Shelf, collected.Backorders, collected.Pickup, collected.Collected));

        Assert.True(shop.PostRequest());
        Settle(shop);
        var exhausted = shop.Read();
        AssertLedgers(exhausted, customerRequests: 4);
        Assert.Equal((0, false, false, 2), SupplierState(exhausted.A));
        Assert.Equal((2, false, false, 0), SupplierState(exhausted.B));
        Assert.Equal((1, 1, 4), (exhausted.Shelf, exhausted.Deficit, exhausted.Collected));
        Assert.True(exhausted.ChoiceContradiction);

        shop.SetAvailability(Supplier.B, true);
        Settle(shop);
        var repaired = shop.Read();
        AssertLedgers(repaired, customerRequests: 4);
        Assert.Equal((1, true, false, 1), SupplierState(repaired.B));
        Assert.Equal((2, 1, 4, 0, 1), (repaired.Shelf, repaired.Dock, repaired.Collected, repaired.Deficit, repaired.Excess));
        Assert.False(repaired.ChoiceContradiction);
        Assert.True(shop.Step());
    }

    [Fact]
    public void AReceiptOfTheLastCaseClosesThatSupplierBeforeAnotherBooking()
    {
        var shop = CornerShop.Create(casesA: 1);
        Assert.True(shop.PostRequest());
        Settle(shop);
        Assert.True(shop.Read().A.Booked);

        shop.SetUnloading(true);
        Assert.False(shop.Step());
        var shortfall = shop.Read();
        AssertLedgers(shortfall, customerRequests: 2, initialCasesA: 1);
        Assert.Equal((0, false, false, 1), SupplierState(shortfall.A));
        Assert.Equal((2, 0, 1, 1), (shortfall.Dock, shortfall.Shelf, shortfall.Backorders, shortfall.Deficit));
        Assert.True(shortfall.ChoiceContradiction);
    }

    private static void Settle(CornerShop shop)
    {
        for (var step = 0; step < 12; step++)
            if (shop.Step()) return;
        throw new InvalidOperationException("The shop did not settle within twelve steps.");
    }

    private static (int Cases, bool Eligible, bool Booked, int Receipts) SupplierState(SupplierReading supplier) =>
        (supplier.Cases, supplier.Eligible, supplier.Booked, supplier.Receipts);

    private static void AssertLedgers(CornerShopReading state, int customerRequests = 1, int initialCasesA = 2)
    {
        Assert.Equal(2 * (initialCasesA + 2) + 1, 2 * (state.A.Cases + state.B.Cases) + state.Dock + state.Shelf + state.Pickup + state.Collected);
        Assert.Equal(customerRequests, state.WaitingRequests + state.Backorders + state.Pickup + state.Collected);
        Assert.Equal(initialCasesA, state.A.Cases + state.A.Receipts);
        Assert.Equal(2, state.B.Cases + state.B.Receipts);
        Assert.False(state.A.Booked && state.B.Booked);
    }

    private CornerShopReading Trace(string phase, CornerShop shop)
    {
        var state = shop.Read();
        var supplierA = $"{state.A.Cases}/{(state.A.Booked ? 1 : 0)}/{state.A.Receipts}";
        var supplierB = $"{state.B.Cases}/{(state.B.Booked ? 1 : 0)}/{state.B.Receipts}";
        output.WriteLine($"{phase,-35} {supplierA,23} {supplierB,23} {state.WaitingRequests,7} {state.Dock,4} {state.Shelf,5} " +
            $"{state.Backorders,10} {state.Pickup,6} {state.Collected,9} {state.Target,6} {state.Deficit,7} {state.Excess,6}");
        AssertLedgers(state);
        return state;
    }
}
