namespace Gojo.EncodingB;

/// <summary>One bit plane in a site grammar. Bit i of the plane is site i.
/// The descriptor says what a clear and a set bit mean, so the address
/// (tile, plane, site) is enough to read any stored bit.</summary>
/// <param name="Group">Value, tension, controller and so on: a reading aid, not a type.</param>
/// <param name="Material">Material planes change only through admitted transition
/// laws or tension absorption. Other planes may be supplied by observation.</param>
/// <param name="BasisSupplied">The plane is constant across a tile and is not
/// stored: the tile's basis supplies it (for example ODV orientation).</param>
public sealed record PlaneDescriptor(
    string Name,
    string Group,
    string WhenClear,
    string WhenSet,
    bool Material = false,
    bool BasisSupplied = false);

/// <summary>Several planes read together as one value at each site.</summary>
public abstract record FieldDescriptor(string Name, IReadOnlyList<string> Planes);

/// <summary>A binary code, low bit first: code = sum of bit k shifted left by k.
/// The domain names each code.</summary>
public sealed record CodeField(string Name, IReadOnlyList<string> Planes, Domain Domain)
    : FieldDescriptor(Name, Planes);

/// <summary>A thermometer (unary) code: bit k is set when the magnitude is
/// greater than k. Only prefixes are valid. More participation sets more bits.</summary>
public sealed record ThermometerField(string Name, IReadOnlyList<string> Planes)
    : FieldDescriptor(Name, Planes)
{
    public int Capacity => Planes.Count;
}

/// <summary>Declares which planes a tile has, in order, and which planes read
/// together as fields. Declared once and shared by every tile that uses it.
/// This is the positional basis that gives each stored bit its meaning.</summary>
public sealed class SiteGrammar
{
    private readonly PlaneDescriptor[] planes;
    private readonly Dictionary<string, int> planeIndex;
    private readonly Dictionary<string, FieldDescriptor> fields;
    private readonly Dictionary<string, (FieldDescriptor Field, int Bit)> fieldOfPlane;

    private SiteGrammar(string name, IReadOnlyList<PlaneDescriptor> planes, IReadOnlyList<FieldDescriptor> fields)
    {
        Name = name;
        this.planes = planes.ToArray();
        planeIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < this.planes.Length; i++)
            if (!planeIndex.TryAdd(this.planes[i].Name, i))
                throw new ArgumentException($"Plane '{this.planes[i].Name}' is declared twice.");
        this.fields = new Dictionary<string, FieldDescriptor>(StringComparer.Ordinal);
        fieldOfPlane = new Dictionary<string, (FieldDescriptor, int)>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (planeIndex.ContainsKey(field.Name) || !this.fields.TryAdd(field.Name, field))
                throw new ArgumentException($"Field name '{field.Name}' is already used.");
            if (field.Planes.Count == 0) throw new ArgumentException($"Field '{field.Name}' has no planes.");
            for (var bit = 0; bit < field.Planes.Count; bit++)
            {
                var plane = field.Planes[bit];
                if (!planeIndex.ContainsKey(plane))
                    throw new ArgumentException($"Field '{field.Name}' names unknown plane '{plane}'.");
                if (!fieldOfPlane.TryAdd(plane, (field, bit)))
                    throw new ArgumentException($"Plane '{plane}' belongs to two fields.");
            }
            if (field is CodeField code && code.Domain.CodeBits != code.Planes.Count)
                throw new ArgumentException($"Field '{field.Name}' needs {code.Domain.CodeBits} planes for domain {code.Domain}.");
        }
    }

    public string Name { get; }
    public IReadOnlyList<PlaneDescriptor> Planes => planes;
    public IEnumerable<FieldDescriptor> Fields => fields.Values;

    public static Builder Create(string name) => new(name);

    public bool HasPlane(string name) => planeIndex.ContainsKey(name);

    public int IndexOf(string plane) =>
        planeIndex.TryGetValue(plane, out var index)
            ? index
            : throw new ArgumentException($"Grammar {Name} has no plane '{plane}'.", nameof(plane));

    public PlaneDescriptor Plane(string name) => planes[IndexOf(name)];

    public FieldDescriptor Field(string name) =>
        fields.TryGetValue(name, out var field)
            ? field
            : throw new ArgumentException($"Grammar {Name} has no field '{name}'.", nameof(name));

    public bool TryField(string name, out FieldDescriptor field) => fields.TryGetValue(name, out field!);

    /// <summary>The field a plane belongs to, and which bit of that field it is.</summary>
    public (FieldDescriptor Field, int Bit)? FieldOf(string plane) =>
        fieldOfPlane.TryGetValue(plane, out var entry) ? entry : null;

    /// <summary>A role reads either a single plane (a one-bit binary code) or a
    /// declared field. Returns the planes in code order, low bit first.</summary>
    public IReadOnlyList<string> PlanesOf(string planeOrField)
    {
        if (fields.TryGetValue(planeOrField, out var field)) return field.Planes;
        IndexOf(planeOrField);
        return [planeOrField];
    }

    public sealed class Builder(string name)
    {
        private readonly List<PlaneDescriptor> planes = [];
        private readonly List<FieldDescriptor> fields = [];

        public Builder Plane(PlaneDescriptor plane)
        {
            planes.Add(plane);
            return this;
        }

        public Builder Plane(string name, string group, string whenClear, string whenSet, bool material = false) =>
            Plane(new PlaneDescriptor(name, group, whenClear, whenSet, material));

        /// <summary>A plane constant across a tile, supplied by the tile basis and never stored.</summary>
        public Builder BasisPlane(string name, string group, string whenClear, string whenSet) =>
            Plane(new PlaneDescriptor(name, group, whenClear, whenSet, BasisSupplied: true));

        /// <summary>Declares a binary code field over existing planes, low bit first.</summary>
        public Builder Code(string field, Domain domain, params string[] planesLowFirst)
        {
            fields.Add(new CodeField(field, planesLowFirst, domain));
            return this;
        }

        /// <summary>Adds planes <c>{field}0 .. {field}{capacity-1}</c>, where bit k
        /// means "magnitude greater than k", and declares them as one field.</summary>
        public Builder Thermometer(string field, string group, int capacity, string quantity, bool material)
        {
            if (capacity is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(capacity));
            var names = new string[capacity];
            for (var k = 0; k < capacity; k++)
            {
                names[k] = field + k;
                planes.Add(new PlaneDescriptor(names[k], group,
                    $"{quantity} is at most {k}", $"{quantity} is greater than {k}", material));
            }
            fields.Add(new ThermometerField(field, names));
            return this;
        }

        public SiteGrammar Build() => new(name, planes, fields);
    }
}
