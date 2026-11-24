using Xunit;
using Xunit.Abstractions;

namespace Gojo.EncodingC.Tests;

public sealed class CornerShopTests(ITestOutputHelper output)
{
    [Fact]
    public void AFullDockAndABackorderSurviveRestoreAndSettleExactlyOnce()
    {
        var shop = CornerShop.Create();
        output.WriteLine("Phase                     Supplier Waiting Dock Shelf Backorders Pickup Collected Target OnOrder Deficit Excess");
        Trace("Customer request posted", shop);

        Assert.False(shop.Step());
        AssertBlocked(Trace("Reorder booked", shop));
        Assert.True(shop.Step());
        AssertBlocked(Trace("Waiting with full dock", shop));

        var saved = shop.Save();
        var restored = shop.Restore(saved);
        Assert.Equal(saved, restored.Save());
        AssertBlocked(restored.Read());

        shop.SetUnloading(true);
        Assert.Equal(saved, restored.Save());
        restored.SetUnloading(true);
        var served = StepBoth("Unload, serve, reorder", quiet: false);
        Assert.Equal((2, 1, 0), (served.SupplierStock, served.Dock, served.Shelf));
        Assert.Equal((0, 1, 0), (served.Backorders, served.Pickup, served.Collected));
        Assert.Equal((1, 1, 0), (served.OnOrder, served.Deficit, served.Excess));

        var secondArrival = StepBoth("Second unit arrives", quiet: false);
        Assert.Equal((1, 1, 1), (secondArrival.SupplierStock, secondArrival.Dock, secondArrival.Shelf));
        Assert.Equal((0, 0, 0), (secondArrival.OnOrder, secondArrival.Deficit, secondArrival.Excess));
        Assert.Equal((0, 1, 0), (secondArrival.Backorders, secondArrival.Pickup, secondArrival.Collected));

        StepBoth("Finish shelving", quiet: false);
        var pickupHeld = StepBoth("Pickup waits", quiet: true);
        Assert.Equal((1, 0, 2), (pickupHeld.SupplierStock, pickupHeld.Dock, pickupHeld.Shelf));
        Assert.Equal((0, 1, 0), (pickupHeld.Backorders, pickupHeld.Pickup, pickupHeld.Collected));

        shop.SetCollection(true);
        restored.SetCollection(true);
        var collected = StepBoth("Collect the purchase", quiet: false);
        Assert.Equal((0, 0, 1), (collected.Backorders, collected.Pickup, collected.Collected));

        for (var i = 0; i < 2; i++)
        {
            var finished = StepBoth("At target", quiet: true);
            Assert.Equal((1, 0, 2), (finished.SupplierStock, finished.Dock, finished.Shelf));
            Assert.Equal((0, 0, 0), (finished.OnOrder, finished.Deficit, finished.Excess));
            Assert.Equal((0, 0, 1), (finished.Backorders, finished.Pickup, finished.Collected));
        }

        CornerShopReading StepBoth(string phase, bool quiet)
        {
            Assert.Equal(quiet, shop.Step());
            Assert.Equal(quiet, restored.Step());
            Assert.Equal(shop.Save(), restored.Save());
            return Trace(phase, shop);
        }

        static void AssertBlocked(CornerShopReading reading)
        {
            Assert.Equal((3, 1, 0), (reading.SupplierStock, reading.Dock, reading.Shelf));
            Assert.Equal((0, 1, 0, 0), (reading.WaitingRequests, reading.Backorders, reading.Pickup, reading.Collected));
            Assert.Equal((2, 1, 1, 0), (reading.Target, reading.OnOrder, reading.Deficit, reading.Excess));
        }
    }

    [Fact]
    public void DisconnectKeepsTheBookedOrderUntilTheSupplierReturns()
    {
        var shop = CornerShop.Create();
        Assert.False(shop.Step());
        shop.ConnectSupplier(false);
        shop.SetUnloading(true);
        Assert.False(shop.Step());
        Assert.True(shop.Step());

        var disconnected = Trace("Supplier disconnected", shop);
        Assert.Equal((3, 0, 0), (disconnected.SupplierStock, disconnected.Dock, disconnected.Shelf));
        Assert.Equal((1, 2, 0), (disconnected.OnOrder, disconnected.Deficit, disconnected.Excess));
        Assert.Equal((0, 1, 0), (disconnected.Backorders, disconnected.Pickup, disconnected.Collected));

        shop = shop.Restore(shop.Save());
        Assert.True(shop.Step());
        Assert.Equal(disconnected, shop.Read());

        shop.SetTarget(0);
        shop.ConnectSupplier(true);
        Assert.False(shop.Step());
        var reconnected = Trace("Supplier reconnected", shop);
        Assert.Equal((2, 1, 0), (reconnected.SupplierStock, reconnected.Dock, reconnected.Shelf));
        Assert.Equal((0, 0, 1), (reconnected.OnOrder, reconnected.Deficit, reconnected.Excess));

        Assert.False(shop.Step());
        Assert.True(shop.Step());
        var finished = Trace("Lower target reached", shop);
        Assert.Equal((2, 0, 1), (finished.SupplierStock, finished.Dock, finished.Shelf));
        Assert.Equal((0, 0, 1), (finished.OnOrder, finished.Deficit, finished.Excess));
    }

    [Fact]
    public void NewCustomersWaitAtFullPickupAndAnEmptySupplierLeavesTheOrderOpen()
    {
        var shop = CornerShop.Create();
        shop.SetUnloading(true);
        shop.SetCollection(true);
        Settle(shop);
        var firstSale = Trace("First sale finished", shop);
        Assert.Equal((1, 2, 1, 0), (firstSale.SupplierStock, firstSale.Shelf, firstSale.Collected, firstSale.OnOrder));

        shop.SetCollection(false);
        Assert.True(shop.PostRequest());
        Assert.True(shop.PostRequest());
        Assert.False(shop.PostRequest());
        Settle(shop);
        var waiting = Trace("Pickup full again", shop, customerRequests: 3);
        Assert.Equal((0, 0, 2, 1, 1, 1), (waiting.SupplierStock, waiting.Dock, waiting.Shelf,
            waiting.Backorders, waiting.Pickup, waiting.Collected));
        Assert.Equal((0, 0, 0), (waiting.OnOrder, waiting.Deficit, waiting.Excess));

        shop.SetCollection(true);
        Settle(shop);
        var supplierEmpty = Trace("Supplier exhausted", shop, customerRequests: 3);
        Assert.Equal((0, 0, 1), (supplierEmpty.SupplierStock, supplierEmpty.Dock, supplierEmpty.Shelf));
        Assert.Equal((0, 0, 0, 3), (supplierEmpty.WaitingRequests, supplierEmpty.Backorders, supplierEmpty.Pickup, supplierEmpty.Collected));
        Assert.Equal((1, 1, 0), (supplierEmpty.OnOrder, supplierEmpty.Deficit, supplierEmpty.Excess));
        Assert.True(shop.Step());
        Assert.Equal(supplierEmpty, shop.Read());
    }

    private static void Settle(CornerShop shop)
    {
        for (var step = 0; step < 12; step++)
            if (shop.Step()) return;
        throw new InvalidOperationException("The shop did not settle within twelve steps.");
    }

    private CornerShopReading Trace(string phase, CornerShop shop, int customerRequests = 1)
    {
        var state = shop.Read();
        output.WriteLine($"{phase,-25} {state.SupplierStock,8} {state.WaitingRequests,7} {state.Dock,4} {state.Shelf,5} " +
            $"{state.Backorders,10} {state.Pickup,6} {state.Collected,9} {state.Target,6} {state.OnOrder,7} " +
            $"{state.Deficit,7} {state.Excess,6}");
        Assert.Equal(4, state.SupplierStock + state.Dock + state.Shelf + state.Pickup + state.Collected);
        Assert.Equal(customerRequests, state.WaitingRequests + state.Backorders + state.Pickup + state.Collected);
        return state;
    }
}
