using System.Buffers.Binary;
using System.Globalization;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using DatabaseDecimal.Arithmetic;

namespace DatabaseDecimal.Values;

/// <summary>
/// A 64-bit fixed-point decimal mantissa.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Decimal64 : IEquatable<Decimal64>, IComparable<Decimal64>, IComparable
{
    public readonly long Mantissa;

    public Decimal64(long mantissa) => Mantissa = mantissa;

    public static Decimal64 Zero => default;

    public static Decimal64 FromUnscaled(long value)
    {
        return new Decimal64(value);
    }

    public static Decimal64 FromInteger(long value, byte scale)
    {
        if (scale >= PowersOf10.Int64.Length)
            throw new OverflowException($"Scale {scale} exceeds 64-bit capacity.");

        return new Decimal64(checked(value * PowersOf10.Int64[scale]));
    }

    public string ToString(byte scale)
    {
        if (scale == 0)
            return Mantissa.ToString(CultureInfo.InvariantCulture);

        var abs = Math.Abs(Mantissa);
        var pow = PowersOf10.Int64[scale];
        var intPart = abs / pow;
        var fracPart = abs % pow;
        var sign = Mantissa < 0 ? "-" : "";
        return $"{sign}{intPart}.{fracPart.ToString(CultureInfo.InvariantCulture).PadLeft(scale, '0')}";
    }

    public override string ToString() => Mantissa.ToString(CultureInfo.InvariantCulture);

    public bool Equals(Decimal64 other) => Mantissa == other.Mantissa;
    public override bool Equals(object? obj) => obj is Decimal64 other && Equals(other);
    public override int GetHashCode() => Mantissa.GetHashCode();
    public int CompareTo(Decimal64 other) => Mantissa.CompareTo(other.Mantissa);

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        Decimal64 other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(Decimal64)}.", nameof(obj)),
    };

    /// <inheritdoc cref="Decimal32.StableHash64"/>
    public ulong StableHash64()
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, Mantissa);
        return XxHash3.HashToUInt64(buffer);
    }

    public static bool operator ==(Decimal64 left, Decimal64 right) => left.Mantissa == right.Mantissa;
    public static bool operator !=(Decimal64 left, Decimal64 right) => left.Mantissa != right.Mantissa;
    public static bool operator <(Decimal64 left, Decimal64 right) => left.Mantissa < right.Mantissa;
    public static bool operator >(Decimal64 left, Decimal64 right) => left.Mantissa > right.Mantissa;
    public static bool operator <=(Decimal64 left, Decimal64 right) => left.Mantissa <= right.Mantissa;
    public static bool operator >=(Decimal64 left, Decimal64 right) => left.Mantissa >= right.Mantissa;

    // Widening conversion
    public static implicit operator Decimal128(Decimal64 v) => new(v.Mantissa);

    // Narrowing conversion (explicit, checked)
    public static explicit operator Decimal32(Decimal64 v) => new(checked((int)v.Mantissa));
}
