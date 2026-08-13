// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

#if NETSTANDARD2_0
using System.Runtime.InteropServices;

namespace System;

/// <summary>
/// netstandard2.0 polyfill for System.Int128. Mirrors the BCL type's public
/// surface for the operations this library needs. Stored as two's complement
/// in a UInt128, matching BCL semantics.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Int128 : IEquatable<Int128>, IComparable<Int128>
{
    internal readonly ulong _lower;
    internal readonly ulong _upper;

    public Int128(ulong upper, ulong lower)
    {
        _lower = lower;
        _upper = upper;
    }

    // --- Constants ---

    public static Int128 Zero => default;
    public static Int128 One => new(0, 1);
    public static Int128 NegativeOne => new(ulong.MaxValue, ulong.MaxValue);
    public static Int128 MaxValue => new(0x7FFF_FFFF_FFFF_FFFF, ulong.MaxValue);
    public static Int128 MinValue => new(0x8000_0000_0000_0000, 0);

    // --- Helpers ---

    private static bool IsNegative(Int128 value) => (long)value._upper < 0;

    public static Int128 Abs(Int128 value) => IsNegative(value) ? -value : value;

    // --- Equality / comparison ---

    public bool Equals(Int128 other) => _lower == other._lower && _upper == other._upper;
    public override bool Equals(object? obj) => obj is Int128 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_lower, _upper);

    public int CompareTo(Int128 other)
    {
        // Compare upper as signed, lower as unsigned.
        long lu = (long)_upper, ru = (long)other._upper;
        if (lu != ru) return lu < ru ? -1 : 1;
        return _lower.CompareTo(other._lower);
    }

    public static bool operator ==(Int128 left, Int128 right) =>
        left._lower == right._lower && left._upper == right._upper;

    public static bool operator !=(Int128 left, Int128 right) =>
        left._lower != right._lower || left._upper != right._upper;

    public static bool operator <(Int128 left, Int128 right) =>
        ((long)left._upper < (long)right._upper) ||
        (left._upper == right._upper && left._lower < right._lower);

    public static bool operator >(Int128 left, Int128 right) =>
        ((long)left._upper > (long)right._upper) ||
        (left._upper == right._upper && left._lower > right._lower);

    public static bool operator <=(Int128 left, Int128 right) => !(left > right);
    public static bool operator >=(Int128 left, Int128 right) => !(left < right);

    // --- Arithmetic ---

    public static Int128 operator +(Int128 left, Int128 right)
    {
        ulong lower = left._lower + right._lower;
        ulong carry = (lower < left._lower) ? 1UL : 0UL;
        ulong upper = left._upper + right._upper + carry;
        return new Int128(upper, lower);
    }

    public static Int128 operator checked +(Int128 left, Int128 right)
    {
        Int128 result = left + right;
        // Signed overflow: the operands share a sign that the result does not.
        // Both terms matter and neither may be inverted — testing the operands
        // for *differing* signs instead inverts the check, so mixed-sign adds
        // throw and genuine overflow slips through.
        if ((((result._upper ^ left._upper) & ~(left._upper ^ right._upper)) & 0x8000_0000_0000_0000UL) != 0)
            throw new OverflowException();
        return result;
    }

    public static Int128 operator -(Int128 left, Int128 right)
    {
        ulong lower = left._lower - right._lower;
        ulong borrow = (lower > left._lower) ? 1UL : 0UL;
        ulong upper = left._upper - right._upper - borrow;
        return new Int128(upper, lower);
    }

    public static Int128 operator checked -(Int128 left, Int128 right)
    {
        Int128 result = left - right;
        // Signed overflow when the inputs differ in sign and the result differs from the minuend.
        if ((((left._upper ^ right._upper) & (left._upper ^ result._upper)) & 0x8000_0000_0000_0000UL) != 0)
            throw new OverflowException();
        return result;
    }

    public static Int128 operator -(Int128 value) => Zero - value;

    public static Int128 operator checked -(Int128 value) =>
        value == MinValue ? throw new OverflowException() : -value;

    public static Int128 operator *(Int128 left, Int128 right) =>
        (Int128)((UInt128)left * (UInt128)right);

    public static Int128 operator checked *(Int128 left, Int128 right)
    {
        // Negate operands to get unsigned magnitudes, then check the product fits.
        bool negResult = IsNegative(left) ^ IsNegative(right);
        UInt128 ul = IsNegative(left) ? (UInt128)(-left) : (UInt128)left;
        UInt128 ur = IsNegative(right) ? (UInt128)(-right) : (UInt128)right;

        // Each magnitude reaches 2^127, so their product needs 256 bits. A
        // 128-bit multiply wraps before any range test can see it — MinValue * 2
        // is exactly 2^128 and wraps to zero — so widen first. This borrows the
        // library's 256-bit multiply rather than repeating the limb arithmetic.
        Clast.DatabaseDecimal.Values.UInt256 wide =
            Clast.DatabaseDecimal.Values.UInt256.BigMul(ul, ur);
        if ((UInt128)(wide >>> 128) != UInt128.Zero)
            throw new OverflowException();

        UInt128 product = (UInt128)wide;
        // Overflow when bit 127 is set, except for exactly MinValue (which is -2^127).
        if (product._upper >= 0x8000_0000_0000_0000UL)
        {
            if (negResult && product._upper == 0x8000_0000_0000_0000UL && product._lower == 0)
                return MinValue;
            throw new OverflowException();
        }
        Int128 signed = (Int128)product;
        return negResult ? -signed : signed;
    }

    public static Int128 operator /(Int128 left, Int128 right)
    {
        if (right == NegativeOne && left == MinValue)
            throw new OverflowException("Int128.MinValue / -1 overflows.");

        bool negResult = IsNegative(left) ^ IsNegative(right);
        UInt128 ul = IsNegative(left) ? (UInt128)(-left) : (UInt128)left;
        UInt128 ur = IsNegative(right) ? (UInt128)(-right) : (UInt128)right;
        UInt128 q = ul / ur;
        return negResult ? -(Int128)q : (Int128)q;
    }

    public static Int128 operator %(Int128 left, Int128 right)
    {
        // Remainder takes the sign of the dividend.
        bool negResult = IsNegative(left);
        UInt128 ul = IsNegative(left) ? (UInt128)(-left) : (UInt128)left;
        UInt128 ur = IsNegative(right) ? (UInt128)(-right) : (UInt128)right;
        UInt128 r = ul % ur;
        return negResult ? -(Int128)r : (Int128)r;
    }

    public static Int128 operator ++(Int128 value) => value + One;
    public static Int128 operator --(Int128 value) => value - One;

    // --- Bitwise ---

    public static Int128 operator &(Int128 left, Int128 right) =>
        new(left._upper & right._upper, left._lower & right._lower);

    public static Int128 operator |(Int128 left, Int128 right) =>
        new(left._upper | right._upper, left._lower | right._lower);

    public static Int128 operator ^(Int128 left, Int128 right) =>
        new(left._upper ^ right._upper, left._lower ^ right._lower);

    public static Int128 operator ~(Int128 value) => new(~value._upper, ~value._lower);

    // --- Shifts ---

    public static Int128 operator <<(Int128 value, int shiftAmount)
    {
        shiftAmount &= 0x7F;
        if (shiftAmount == 0) return value;
        if (shiftAmount >= 64)
            return new Int128(value._lower << (shiftAmount - 64), 0);
        return new Int128(
            (value._upper << shiftAmount) | (value._lower >> (64 - shiftAmount)),
            value._lower << shiftAmount);
    }

    /// <summary>Arithmetic right shift (sign-extending).</summary>
    public static Int128 operator >>(Int128 value, int shiftAmount)
    {
        shiftAmount &= 0x7F;
        if (shiftAmount == 0) return value;
        if (shiftAmount >= 64)
        {
            ulong newLower = (ulong)((long)value._upper >> (shiftAmount - 64));
            ulong newUpper = (ulong)((long)value._upper >> 63); // all-zero or all-one
            return new Int128(newUpper, newLower);
        }
        return new Int128(
            (ulong)((long)value._upper >> shiftAmount),
            (value._lower >> shiftAmount) | (value._upper << (64 - shiftAmount)));
    }

    /// <summary>Logical right shift (zero-extending).</summary>
    public static Int128 operator >>>(Int128 value, int shiftAmount)
    {
        shiftAmount &= 0x7F;
        if (shiftAmount == 0) return value;
        if (shiftAmount >= 64)
            return new Int128(0, value._upper >> (shiftAmount - 64));
        return new Int128(
            value._upper >> shiftAmount,
            (value._lower >> shiftAmount) | (value._upper << (64 - shiftAmount)));
    }

    // --- Conversions ---

    public static implicit operator Int128(int value) =>
        new((value < 0) ? ulong.MaxValue : 0UL, (ulong)(long)value);

    public static implicit operator Int128(long value) =>
        new((value < 0) ? ulong.MaxValue : 0UL, (ulong)value);

    public static implicit operator Int128(short value) => (Int128)(int)value;
    public static implicit operator Int128(sbyte value) => (Int128)(int)value;

    public static explicit operator Int128(uint value) => new(0, value);
    public static explicit operator Int128(ulong value) => new(0, value);

    public static explicit operator long(Int128 value) => (long)value._lower;
    public static explicit operator int(Int128 value) => (int)value._lower;
    public static explicit operator short(Int128 value) => (short)value._lower;
    public static explicit operator sbyte(Int128 value) => (sbyte)value._lower;
    public static explicit operator ulong(Int128 value) => value._lower;
    public static explicit operator uint(Int128 value) => (uint)value._lower;
    public static explicit operator ushort(Int128 value) => (ushort)value._lower;
    public static explicit operator byte(Int128 value) => (byte)value._lower;

    // Reinterpret cast between signed and unsigned 128-bit
    public static explicit operator UInt128(Int128 value) => new(value._upper, value._lower);
    public static explicit operator Int128(UInt128 value) => new(value._upper, value._lower);

    // --- ToString ---

    public override string ToString()
    {
        if (IsNegative(this))
        {
            // Take absolute value via UInt128 to handle MinValue cleanly.
            UInt128 abs = ~(UInt128)this + UInt128.One;
            return "-" + abs.ToString();
        }
        return ((UInt128)this).ToString();
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();
}
#endif
