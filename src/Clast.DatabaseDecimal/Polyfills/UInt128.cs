// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

#if NETSTANDARD2_0
using System.Runtime.InteropServices;

namespace System;

/// <summary>
/// netstandard2.0 polyfill for System.UInt128. Mirrors the BCL type's public
/// surface for the operations this library needs (arithmetic, comparison,
/// bitwise, shift, conversion). Stored as a pair of 64-bit halves.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UInt128 : IEquatable<UInt128>, IComparable<UInt128>
{
    internal readonly ulong _lower;
    internal readonly ulong _upper;

    public UInt128(ulong upper, ulong lower)
    {
        _lower = lower;
        _upper = upper;
    }

    // --- Constants ---

    public static UInt128 Zero => default;
    public static UInt128 One => new(0, 1);
    public static UInt128 MaxValue => new(ulong.MaxValue, ulong.MaxValue);
    public static UInt128 MinValue => Zero;

    // --- Equality / comparison ---

    public bool Equals(UInt128 other) => _lower == other._lower && _upper == other._upper;
    public override bool Equals(object? obj) => obj is UInt128 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_lower, _upper);

    public int CompareTo(UInt128 other)
    {
        int cmp = _upper.CompareTo(other._upper);
        return cmp != 0 ? cmp : _lower.CompareTo(other._lower);
    }

    public static bool operator ==(UInt128 left, UInt128 right) =>
        left._lower == right._lower && left._upper == right._upper;

    public static bool operator !=(UInt128 left, UInt128 right) =>
        left._lower != right._lower || left._upper != right._upper;

    public static bool operator <(UInt128 left, UInt128 right) =>
        (left._upper < right._upper) || (left._upper == right._upper && left._lower < right._lower);

    public static bool operator >(UInt128 left, UInt128 right) =>
        (left._upper > right._upper) || (left._upper == right._upper && left._lower > right._lower);

    public static bool operator <=(UInt128 left, UInt128 right) => !(left > right);
    public static bool operator >=(UInt128 left, UInt128 right) => !(left < right);

    // --- Arithmetic ---

    public static UInt128 operator +(UInt128 left, UInt128 right)
    {
        ulong lower = left._lower + right._lower;
        ulong carry = (lower < left._lower) ? 1UL : 0UL;
        ulong upper = left._upper + right._upper + carry;
        return new UInt128(upper, lower);
    }

    public static UInt128 operator checked +(UInt128 left, UInt128 right)
    {
        ulong lower = left._lower + right._lower;
        ulong carry = (lower < left._lower) ? 1UL : 0UL;
        ulong upper = checked(left._upper + right._upper + carry);
        return new UInt128(upper, lower);
    }

    public static UInt128 operator -(UInt128 left, UInt128 right)
    {
        ulong lower = left._lower - right._lower;
        ulong borrow = (lower > left._lower) ? 1UL : 0UL;
        ulong upper = left._upper - right._upper - borrow;
        return new UInt128(upper, lower);
    }

    public static UInt128 operator checked -(UInt128 left, UInt128 right)
    {
        ulong lower = left._lower - right._lower;
        ulong borrow = (lower > left._lower) ? 1UL : 0UL;
        ulong upper = checked(left._upper - right._upper - borrow);
        return new UInt128(upper, lower);
    }

    public static UInt128 operator -(UInt128 value) => Zero - value;

    public static UInt128 operator *(UInt128 left, UInt128 right)
    {
        // Truncated multiply: lower 128 bits of the full 256-bit product.
        ulong al = left._lower, ah = left._upper;
        ulong bl = right._lower, bh = right._upper;

        ulong p00_hi = MathCompat.BigMul64(al, bl, out ulong p00_lo);
        ulong upper = p00_hi + (al * bh) + (ah * bl);
        return new UInt128(upper, p00_lo);
    }

    /// <summary>
    /// Multiplication that throws rather than wrapping, matching the BCL's
    /// <c>checked *</c>. Without this operator C# binds <c>checked(a * b)</c>
    /// to the truncating one above and the overflow passes silently.
    /// </summary>
    public static UInt128 operator checked *(UInt128 left, UInt128 right)
    {
        // The full product needs 256 bits, so form it there rather than
        // range-testing a value that has already wrapped.
        Clast.DatabaseDecimal.Values.UInt256 wide =
            Clast.DatabaseDecimal.Values.UInt256.BigMul(left, right);
        if ((UInt128)(wide >>> 128) != Zero)
            throw new OverflowException();

        return (UInt128)wide;
    }

    public static UInt128 operator /(UInt128 left, UInt128 right)
    {
        var (q, _) = DivRem(left, right);
        return q;
    }

    public static UInt128 operator %(UInt128 left, UInt128 right)
    {
        var (_, r) = DivRem(left, right);
        return r;
    }

    public static UInt128 operator ++(UInt128 value) => value + One;
    public static UInt128 operator --(UInt128 value) => value - One;

    // --- Bitwise ---

    public static UInt128 operator &(UInt128 left, UInt128 right) =>
        new(left._upper & right._upper, left._lower & right._lower);

    public static UInt128 operator |(UInt128 left, UInt128 right) =>
        new(left._upper | right._upper, left._lower | right._lower);

    public static UInt128 operator ^(UInt128 left, UInt128 right) =>
        new(left._upper ^ right._upper, left._lower ^ right._lower);

    public static UInt128 operator ~(UInt128 value) => new(~value._upper, ~value._lower);

    // --- Shifts (mask shift amount with 0x7F to match BCL) ---

    public static UInt128 operator <<(UInt128 value, int shiftAmount)
    {
        shiftAmount &= 0x7F;
        if (shiftAmount == 0) return value;
        if (shiftAmount >= 64)
            return new UInt128(value._lower << (shiftAmount - 64), 0);
        return new UInt128(
            (value._upper << shiftAmount) | (value._lower >> (64 - shiftAmount)),
            value._lower << shiftAmount);
    }

    public static UInt128 operator >>>(UInt128 value, int shiftAmount)
    {
        shiftAmount &= 0x7F;
        if (shiftAmount == 0) return value;
        if (shiftAmount >= 64)
            return new UInt128(0, value._upper >> (shiftAmount - 64));
        return new UInt128(
            value._upper >> shiftAmount,
            (value._lower >> shiftAmount) | (value._upper << (64 - shiftAmount)));
    }

    public static UInt128 operator >>(UInt128 value, int shiftAmount) => value >>> shiftAmount;

    // --- Conversions ---

    public static implicit operator UInt128(ulong value) => new(0, value);
    public static implicit operator UInt128(uint value) => new(0, value);
    public static implicit operator UInt128(ushort value) => new(0, value);
    public static implicit operator UInt128(byte value) => new(0, value);

    public static explicit operator UInt128(int value) => new((value < 0) ? ulong.MaxValue : 0UL, (ulong)(long)value);
    public static explicit operator UInt128(long value) => new((value < 0) ? ulong.MaxValue : 0UL, (ulong)value);

    public static explicit operator ulong(UInt128 value) => value._lower;
    public static explicit operator long(UInt128 value) => (long)value._lower;
    public static explicit operator uint(UInt128 value) => (uint)value._lower;
    public static explicit operator int(UInt128 value) => (int)value._lower;
    public static explicit operator ushort(UInt128 value) => (ushort)value._lower;
    public static explicit operator byte(UInt128 value) => (byte)value._lower;

    // --- ToString ---

    public override string ToString()
    {
        if (_upper == 0 && _lower == 0) return "0";

        // Max 39 decimal digits for UInt128
        Span<char> buffer = stackalloc char[40];
        int pos = buffer.Length;
        UInt128 v = this;
        UInt128 ten = (UInt128)10UL;

        while (v != Zero)
        {
            var (q, r) = DivRem(v, ten);
            buffer[--pos] = (char)('0' + (int)r._lower);
            v = q;
        }

        return buffer.Slice(pos).ToString();
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    // --- DivRem via binary long division ---

    internal static (UInt128 Quotient, UInt128 Remainder) DivRem(UInt128 left, UInt128 right)
    {
        if (right == Zero) throw new DivideByZeroException();
        if (left < right) return (Zero, left);
        if (left == right) return (One, Zero);

        // Fast path: divisor fits in 64 bits and dividend's upper is small enough
        if (right._upper == 0 && left._upper == 0)
            return (new UInt128(0, left._lower / right._lower),
                    new UInt128(0, left._lower % right._lower));

        int shift = LeadingZeroCount(right) - LeadingZeroCount(left);
        UInt128 remainder = left;
        UInt128 quotient = Zero;
        UInt128 current = right << shift;

        for (int i = 0; i <= shift; i++)
        {
            quotient <<= 1;
            if (remainder >= current)
            {
                remainder -= current;
                quotient |= One;
            }
            current >>= 1;
        }

        return (quotient, remainder);
    }

    internal static int LeadingZeroCount(UInt128 value)
    {
        if (value._upper != 0) return MathCompat.LeadingZeroCount(value._upper);
        return 64 + MathCompat.LeadingZeroCount(value._lower);
    }
}
#endif
