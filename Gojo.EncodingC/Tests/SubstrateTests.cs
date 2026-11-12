using Xunit;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Gojo.EncodingC.Tests;

public sealed class SubstrateTests
{
    [Fact]
    public void APlaneIsAsLongAsItNeedsToBe()
    {
        var plane = new Plane(200);
        plane[0] = plane[63] = plane[64] = plane[199] = true;

        Assert.Equal(4, plane.Count);
        Assert.Equal(new[] { 0, 63, 64, 199 }, plane.Indexes());
        Assert.Equal(196, plane.Not().Count);
        Assert.True(Plane.Ones(130).Covers(Plane.Of(130, 129)));
        Assert.Equal("1010", Plane.Parse("1010").ToString());
    }

    [Fact]
    public void AStoreGrowsAndNeverReusesARetiredPosition()
    {
        var store = new Store(1, 100);
        store.Retire(10);

        Assert.False(store.IsLive(10));
        Assert.Equal(100, store.Add(50));
        Assert.Equal(150, store.Count);
        Assert.Equal(149, store.Live.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Write(0, 10, true));

        var later = store.AddPlane();
        Assert.Equal(1, later);
        Assert.True(store.Read(later).None);
    }

    [Fact]
    public void AReferenceCostsTheBitsItsTargetsNeed()
    {
        var references = new PackedInts();
        foreach (var value in new[] { 0, 1, 2, 3, 3 }) references.Add(value);

        Assert.Equal(2, references.Width);
        Assert.Equal(10, references.BitLength);

        references.Add(100);
        Assert.Equal(7, references.Width);
        Assert.Equal(new[] { 0, 1, 2, 3, 3, 100 }, references.ToArray());

        references[0] = 1000;
        Assert.Equal(10, references.Width);
        Assert.Equal(1000, references[0]);
        Assert.Equal(100, references[5]);
    }

    [Fact]
    public void AConstantPlaneIsOneBitForEveryPosition()
    {
        var store = new Store(1, 5);
        var orientation = store.AddConstantPlane(false);

        Assert.True(store.Read(orientation).None);
        store.SetConstant(orientation, true);

        Assert.Equal(5, store.Read(orientation).Count);
        Assert.Throws<InvalidOperationException>(() => store.Write(orientation, 0, false));
    }

    [Fact]
    public void AShapeLaysValuesOutWithoutSayingWhatTheyMean()
    {
        var count = Shape.Unary(4);
        Assert.Equal(5, count.Size);
        Assert.Equal(4, count.Bits);
        Assert.Equal(0b0111UL, count.Encode(3));
        Assert.Equal(3, count.Decode(0b0111));
        Assert.Null(count.Decode(0b0101));

        var code = Shape.Binary(5);
        Assert.Equal(3, code.Bits);
        Assert.Null(code.Decode(6));

        var store = new Store(4, 2);
        var field = new Field(count, 0, 1, 2, 3);
        field.Write(store, 1, 2);
        Assert.Equal(2, field.Read(store, 1));
        Assert.Equal(0, field.Read(store, 0));
    }

    [Fact]
    public void ChangeFromOutsideEntersAsUnitsAndOverflowIsReturned()
    {
        var (store, count) = Make.Counts(3, 2);

        Assert.True(Unary.Add(store, count, store.Live).Blocked.None);
        Assert.True(Unary.Add(store, count, Plane.Of(3, 0, 1)).Blocked.None);
        var overflow = Unary.Add(store, count, store.Live);

        Assert.Equal(Plane.Of(3, 0, 1), overflow.Blocked);
        Assert.Equal(Plane.Of(3, 2), overflow.Taken);
        Assert.All(Enumerable.Range(0, 3), i => Assert.Equal(2, count.Read(store, i)));

        var underflow = Unary.Remove(store, count, Plane.Of(3, 2));
        Assert.True(underflow.Blocked.None);
        Assert.Equal(1, count.Read(store, 2));
    }

    /// <summary><c>010</c> is not a count. Adding to it would shift it into a
    /// valid-looking 3; instead it is reported as broken and keeps its bits,
    /// and no read counts it as holding anything.</summary>
    [Fact]
    public void ABrokenCountIsReportedAndNeverRepaired()
    {
        var (store, count) = Make.Counts(2, 3);
        store.Write(1, 0, true);

        var added = Unary.Add(store, count, store.Live);
        var removed = Unary.Remove(store, count, Plane.Of(2, 0));

        Assert.Equal(Plane.Of(2, 0), added.Broken);
        Assert.Equal(Plane.Of(2, 1), added.Taken);
        Assert.Equal(Plane.Of(2, 0), removed.Broken);
        Assert.Equal(0b010UL, count.Pattern(store, 0));
        Assert.Null(count.Read(store, 0));
        Assert.Equal(Plane.Of(2, 0), Unary.Broken(store, count));
        Assert.Equal(Plane.Of(2, 1), Unary.NonZero(store, count));
    }

    /// <summary>Names belong to a lens for people. No type of the encoding itself
    /// holds a string, so nothing in it can depend on one.</summary>
    [Fact]
    public void TheEncodingHoldsNoStrings()
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var encoding = typeof(Store).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(Store).Namespace)
            .Where(t => t != typeof(Lens) && t != typeof(OdvNotation))
            .Where(t => t.DeclaringType != typeof(Lens) && t.DeclaringType != typeof(OdvNotation));
        var holders = encoding
            .SelectMany(t => t.GetFields(all).Where(f => Stringy(f.FieldType)).Select(f => $"{t.Name}.{f.Name}"))
            .ToArray();

        Assert.Empty(holders);
    }

    /// <summary>The encoding counts, compares and looks up. It never measures
    /// with real numbers: no field, parameter or result anywhere in it is a
    /// floating-point or decimal number.</summary>
    [Fact]
    public void TheEncodingUsesNoRealNumbers()
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        static bool Real(Type type) =>
            type == typeof(double) || type == typeof(float) || type == typeof(decimal) || type == typeof(Half) ||
            (type.HasElementType && Real(type.GetElementType()!)) ||
            (type.IsGenericType && type.GetGenericArguments().Any(Real));
        var users = typeof(Store).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(Store).Namespace)
            .SelectMany(t => t.GetFields(all).Where(f => Real(f.FieldType)).Select(f => $"{t.Name}.{f.Name}")
                .Concat(t.GetMethods(all)
                    .Where(m => Real(m.ReturnType) || m.GetParameters().Any(p => Real(p.ParameterType)))
                    .Select(m => $"{t.Name}.{m.Name}")))
            .ToArray();

        Assert.Empty(users);
    }

    /// <summary>Counting, comparing and looking up a table are enough at this
    /// level, so nothing in the encoding uses the math library, not even for a
    /// minimum. The check reads every type the assembly refers to.</summary>
    [Fact]
    public void TheEncodingCallsNoMathLibrary()
    {
        var module = typeof(Store).Module;
        var referenced = new List<string>();
        for (var row = 1; ; row++)
        {
            Type type;
            try
            {
                type = module.ResolveType(0x01000000 | row);
            }
            catch (ArgumentException)
            {
                break;
            }
            referenced.Add(type.FullName ?? type.Name);
        }

        Assert.Contains("System.Object", referenced);
        Assert.DoesNotContain("System.Math", referenced);
        Assert.DoesNotContain("System.MathF", referenced);
    }

    private static bool Stringy(Type type) =>
        type == typeof(string) ||
        (type.IsArray && Stringy(type.GetElementType()!)) ||
        (type.IsGenericType && type.GetGenericArguments().Any(Stringy));
}

internal static class Make
{
    /// <summary>A store whose planes 0 … capacity − 1 hold a unary count.</summary>
    public static (Store Store, Field Count) Counts(int positions, int capacity) =>
        (new Store(capacity, positions), new Field(Shape.Unary(capacity), Enumerable.Range(0, capacity).ToArray()));

    /// <summary>A two-role law from lane or position 0 of one surface to position 0 of a store.</summary>
    public static Wiring Wire(TransitionLaw law, Surface from, Store to)
    {
        var wiring = new Wiring(law,
            new Binding(from, Enumerable.Range(0, law.Roles[0].Bits).ToArray()),
            new Binding(to, Enumerable.Range(0, law.Roles[1].Bits).ToArray()));
        wiring.Add(0, 0);
        return wiring;
    }
}
