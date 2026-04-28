using System.Buffers.Binary;
using System.Globalization;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using DatabaseDecimal.Arithmetic;

namespace DatabaseDecimal.Values;

/// <summary>
/// A 32-bit fixed-point decimal mantissa. Stores the raw scaled integer value
/// without any precision/scale metadata. The logical decimal value is
/// Mantissa / 10^scale, where scale comes from the associated DecimalType.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Decimal32 : IEquatable<Decimal32>, IComparable<Decimal32>, IComparable
{
    public readonly int Mantissa;

    public Decimal32(int mantissa) => Mantissa = mantissa;

    public static Decimal32 Zero => default;

    /// <summary>
    /// Wraps a raw mantissa value. The associated DecimalType supplies the scale,
    /// so e.g. mantissa 123 represents 1.23 when paired with scale 2.
    /// </summary>
    public static Decimal32 FromUnscaled(int value)
    {
        return new Decimal32(value);
    }

    /// <summary>
    /// Creates a Decimal32 from a whole integer, scaling it to the given scale.
    /// E.g., FromInteger(42, scale=2) represents 42.00 with mantissa 4200.
    /// </summary>
    public static Decimal32 FromInteger(int value, byte scale)
    {
        if (scale >= PowersOf10.Int32.Length)
            throw new OverflowException($"Scale {scale} exceeds 32-bit capacity.");

        return new Decimal32(checked(value * PowersOf10.Int32[scale]));
    }

    /// <summary>
    /// Formats this mantissa as a decimal string using the given scale.
    /// </summary>
    public string ToString(byte scale)
    {
        if (scale == 0)
            return Mantissa.ToString(CultureInfo.InvariantCulture);

        var abs = Math.Abs((long)Mantissa);
        var intPart = abs / PowersOf10.Int64[scale];
        var fracPart = abs % PowersOf10.Int64[scale];
        var sign = Mantissa < 0 ? "-" : "";
        return $"{sign}{intPart}.{fracPart.ToString(CultureInfo.InvariantCulture).PadLeft(scale, '0')}";
    }

    public override string ToString() => Mantissa.ToString(CultureInfo.InvariantCulture);

    // Equality and comparison (valid when comparing values of the same DecimalType)
    public bool Equals(Decimal32 other) => Mantissa == other.Mantissa;
    public override bool Equals(object? obj) => obj is Decimal32 other && Equals(other);
    public override int GetHashCode() => Mantissa.GetHashCode();
    public int CompareTo(Decimal32 other) => Mantissa.CompareTo(other.Mantissa);

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        Decimal32 other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(Decimal32)}.", nameof(obj)),
    };

    /// <summary>
    /// Returns a 64-bit hash of the mantissa that is stable across processes
    /// and runtime versions. Suitable for sharding, persistence, or
    /// content-addressable storage. Honors the same equality contract as
    /// <see cref="GetHashCode"/>: equal values (same DecimalType) hash equal.
    /// </summary>
    public ulong StableHash64()
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, Mantissa);
        return XxHash3.HashToUInt64(buffer);
    }

    public static bool operator ==(Decimal32 left, Decimal32 right) => left.Mantissa == right.Mantissa;
    public static bool operator !=(Decimal32 left, Decimal32 right) => left.Mantissa != right.Mantissa;
    public static bool operator <(Decimal32 left, Decimal32 right) => left.Mantissa < right.Mantissa;
    public static bool operator >(Decimal32 left, Decimal32 right) => left.Mantissa > right.Mantissa;
    public static bool operator <=(Decimal32 left, Decimal32 right) => left.Mantissa <= right.Mantissa;
    public static bool operator >=(Decimal32 left, Decimal32 right) => left.Mantissa >= right.Mantissa;

    // Widening conversions
    public static implicit operator Decimal64(Decimal32 v) => new(v.Mantissa);
    public static implicit operator Decimal128(Decimal32 v) => new(v.Mantissa);
}
