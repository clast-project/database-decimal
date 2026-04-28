using System.Buffers.Binary;
using System.Globalization;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using DatabaseDecimal.Arithmetic;

namespace DatabaseDecimal.Values;

/// <summary>
/// A 128-bit fixed-point decimal mantissa.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Decimal128 : IEquatable<Decimal128>, IComparable<Decimal128>, IComparable
{
    public readonly Int128 Mantissa;

    public Decimal128(Int128 mantissa) => Mantissa = mantissa;

    public static Decimal128 Zero => default;

    public static Decimal128 FromUnscaled(Int128 value)
    {
        return new Decimal128(value);
    }

    public static Decimal128 FromInteger(Int128 value, byte scale)
    {
        if (scale >= PowersOf10.Int128.Length)
            throw new OverflowException($"Scale {scale} exceeds 128-bit capacity.");

        return new Decimal128(checked(value * PowersOf10.Int128[scale]));
    }

    public string ToString(byte scale)
    {
        if (scale == 0)
            return Mantissa.ToString("D", CultureInfo.InvariantCulture);

        var abs = Mantissa < 0 ? -Mantissa : Mantissa;
        var pow = PowersOf10.Int128[scale];
        var intPart = abs / pow;
        var fracPart = abs % pow;
        var sign = Mantissa < 0 ? "-" : "";
        return $"{sign}{intPart.ToString("D", CultureInfo.InvariantCulture)}.{fracPart.ToString("D", CultureInfo.InvariantCulture).PadLeft(scale, '0')}";
    }

    public override string ToString() => Mantissa.ToString("D", CultureInfo.InvariantCulture);

    public bool Equals(Decimal128 other) => Mantissa == other.Mantissa;
    public override bool Equals(object? obj) => obj is Decimal128 other && Equals(other);
    public override int GetHashCode() => Mantissa.GetHashCode();
    public int CompareTo(Decimal128 other) => Mantissa.CompareTo(other.Mantissa);

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        Decimal128 other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(Decimal128)}.", nameof(obj)),
    };

    /// <inheritdoc cref="Decimal32.StableHash64"/>
    public ulong StableHash64()
    {
        Span<byte> buffer = stackalloc byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, (ulong)Mantissa);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(8), (ulong)(Mantissa >>> 64));
        return XxHash3.HashToUInt64(buffer);
    }

    public static bool operator ==(Decimal128 left, Decimal128 right) => left.Mantissa == right.Mantissa;
    public static bool operator !=(Decimal128 left, Decimal128 right) => left.Mantissa != right.Mantissa;
    public static bool operator <(Decimal128 left, Decimal128 right) => left.Mantissa < right.Mantissa;
    public static bool operator >(Decimal128 left, Decimal128 right) => left.Mantissa > right.Mantissa;
    public static bool operator <=(Decimal128 left, Decimal128 right) => left.Mantissa <= right.Mantissa;
    public static bool operator >=(Decimal128 left, Decimal128 right) => left.Mantissa >= right.Mantissa;

    // Narrowing conversions (explicit, checked)
    public static explicit operator Decimal32(Decimal128 v) => new(checked((int)v.Mantissa));
    public static explicit operator Decimal64(Decimal128 v) => new(checked((long)v.Mantissa));
}
