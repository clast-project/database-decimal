// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using Clast.DatabaseDecimal.Arithmetic;

namespace Clast.DatabaseDecimal.Values;

/// <summary>
/// A 256-bit fixed-point decimal mantissa.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Decimal256 : IEquatable<Decimal256>, IComparable<Decimal256>, IComparable
{
    public readonly Int256 Mantissa;

    public Decimal256(Int256 mantissa) => Mantissa = mantissa;

    public static Decimal256 Zero => default;

    public static Decimal256 FromUnscaled(Int256 value)
    {
        return new Decimal256(value);
    }

    public static Decimal256 FromInteger(Int256 value, byte scale)
    {
        if (scale >= PowersOf10.Int256Values.Length)
            throw new OverflowException($"Scale {scale} exceeds 256-bit capacity.");

        return new Decimal256(checked(value * PowersOf10.Int256Values[scale]));
    }

    public string ToString(byte scale)
    {
        if (scale == 0)
            return Mantissa.ToString();

        bool negative = Int256.IsNegative(Mantissa);
        Int256 abs = negative ? Int256.Abs(Mantissa) : Mantissa;
        Int256 pow = PowersOf10.Int256Values[scale];
        Int256 intPart = abs / pow;
        Int256 fracPart = abs % pow;
        string sign = negative ? "-" : "";
        return $"{sign}{intPart}.{fracPart.ToString().PadLeft(scale, '0')}";
    }

    public override string ToString() => Mantissa.ToString();

    public bool Equals(Decimal256 other) => Mantissa == other.Mantissa;
    public override bool Equals(object? obj) => obj is Decimal256 other && Equals(other);
    public override int GetHashCode() => Mantissa.GetHashCode();
    public int CompareTo(Decimal256 other) => Mantissa.CompareTo(other.Mantissa);

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        Decimal256 other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(Decimal256)}.", nameof(obj)),
    };

    /// <inheritdoc cref="Decimal32.StableHash64"/>
    public ulong StableHash64()
    {
        Span<byte> buffer = stackalloc byte[32];
        UInt256 m = (UInt256)Mantissa;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, (ulong)m);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(8), (ulong)(m >>> 64));
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(16), (ulong)(m >>> 128));
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(24), (ulong)(m >>> 192));
        return XxHash3.HashToUInt64(buffer);
    }

    public static bool operator ==(Decimal256 left, Decimal256 right) => left.Mantissa == right.Mantissa;
    public static bool operator !=(Decimal256 left, Decimal256 right) => left.Mantissa != right.Mantissa;
    public static bool operator <(Decimal256 left, Decimal256 right) => left.Mantissa < right.Mantissa;
    public static bool operator >(Decimal256 left, Decimal256 right) => left.Mantissa > right.Mantissa;
    public static bool operator <=(Decimal256 left, Decimal256 right) => left.Mantissa <= right.Mantissa;
    public static bool operator >=(Decimal256 left, Decimal256 right) => left.Mantissa >= right.Mantissa;

    // Widening conversions
    public static implicit operator Decimal256(Decimal128 v) => new((Int256)v.Mantissa);

    // Narrowing conversions (explicit)
    public static explicit operator Decimal32(Decimal256 v) => new(checked((int)(Int128)v.Mantissa));
    public static explicit operator Decimal64(Decimal256 v) => new(checked((long)(Int128)v.Mantissa));
    public static explicit operator Decimal128(Decimal256 v) => new(checked((Int128)v.Mantissa));
}
