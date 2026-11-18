// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

#if NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System;

/// <summary>
/// netstandard2.0 polyfill for System.UInt128. Mirrors the BCL type's public
/// surface for the operations this library needs (arithmetic, comparison,
/// bitwise, shift, conversion). Stored as a pair of 64-bit halves.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UInt128 : IEquatable<UInt128>, IComparable<UInt128>, IComparable, IFormattable
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

    public int CompareTo(object? obj)
    {
        if (obj is UInt128 other) return CompareTo(other);
        if (obj is null) return 1;
        throw new ArgumentException("Object must be of type UInt128.", nameof(obj));
    }

    // --- Helpers ---

    // Return types follow the BCL: LeadingZeroCount and PopCount return UInt128
    // there, so returning int here would let code compile downlevel that fails
    // on net8.0.

    public static int Sign(UInt128 value) => (value._upper | value._lower) == 0 ? 0 : 1;

    public static UInt128 Min(UInt128 x, UInt128 y) => x <= y ? x : y;

    public static UInt128 Max(UInt128 x, UInt128 y) => x >= y ? x : y;

    public static UInt128 LeadingZeroCount(UInt128 value) => (UInt128)(ulong)LeadingZeroCountInt32(value);

    public static UInt128 PopCount(UInt128 value) =>
        (UInt128)(ulong)(MathCompat.PopCount(value._upper) + MathCompat.PopCount(value._lower));

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

    // --- Parse ---

    // The BCL's overload set, less the UTF-8 ones: IUtf8SpanParsable is a .NET 8
    // interface. See Int128Parsing for the styles supported.

    public static UInt128 Parse(string s) => Parse(s, NumberStyles.Integer, null);

    public static UInt128 Parse(string s, NumberStyles style) => Parse(s, style, null);

    public static UInt128 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

    /// <remarks>Supports <see cref="NumberStyles.Integer"/>, <see cref="NumberStyles.HexNumber"/> and their subsets; any other valid style throws <see cref="NotSupportedException"/>.</remarks>
    public static UInt128 Parse(string s, NumberStyles style, IFormatProvider? provider)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));
        return Parse(s.AsSpan(), style, provider);
    }

    public static UInt128 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

    /// <remarks>Supports <see cref="NumberStyles.Integer"/>, <see cref="NumberStyles.HexNumber"/> and their subsets; any other valid style throws <see cref="NotSupportedException"/>.</remarks>
    public static UInt128 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
    {
        Int128Parsing.ValidateStyle(style);
        return Int128Parsing.Parse(s, style, provider, signed: false, out UInt128 bits) switch
        {
            Int128Parsing.Status.Ok => bits,
            Int128Parsing.Status.Overflow => throw Int128Parsing.OverflowError("a UInt128"),
            _ => throw Int128Parsing.FormatError(s),
        };
    }

    public static bool TryParse([NotNullWhen(true)] string? s, out UInt128 result) =>
        TryParse(s, NumberStyles.Integer, null, out result);

    public static bool TryParse(ReadOnlySpan<char> s, out UInt128 result) =>
        TryParse(s, NumberStyles.Integer, null, out result);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out UInt128 result) =>
        TryParse(s, NumberStyles.Integer, provider, out result);

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt128 result) =>
        TryParse(s, NumberStyles.Integer, provider, out result);

    /// <remarks>Throws <see cref="NotSupportedException"/> for a style <c>Parse</c> does not support, rather than returning <see langword="false"/>.</remarks>
    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out UInt128 result)
    {
        // The style is checked before the null test, as in the BCL.
        Int128Parsing.ValidateStyle(style);
        if (s is null)
        {
            result = default;
            return false;
        }
        return TryParse(s.AsSpan(), style, provider, out result);
    }

    /// <remarks>Throws <see cref="NotSupportedException"/> for a style <c>Parse</c> does not support, rather than returning <see langword="false"/>.</remarks>
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out UInt128 result)
    {
        Int128Parsing.ValidateStyle(style);
        bool ok = Int128Parsing.Parse(s, style, provider, signed: false, out UInt128 bits) == Int128Parsing.Status.Ok;
        result = bits;
        return ok;
    }

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

    public string ToString(string? format) => ToString(format, null);

    public string ToString(IFormatProvider? provider) => ToString(null, provider);

    /// <remarks>Supports the G, D, R and X standard formats; any other format throws <see cref="FormatException"/>.</remarks>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Int128Formatting.Format(false, this, this, format, formatProvider);

    // --- DivRem via binary long division ---

    public static (UInt128 Quotient, UInt128 Remainder) DivRem(UInt128 left, UInt128 right)
    {
        if (right == Zero) throw new DivideByZeroException();
        if (left < right) return (Zero, left);
        if (left == right) return (One, Zero);

        // Fast path: divisor fits in 64 bits and dividend's upper is small enough
        if (right._upper == 0 && left._upper == 0)
            return (new UInt128(0, left._lower / right._lower),
                    new UInt128(0, left._lower % right._lower));

        int shift = LeadingZeroCountInt32(right) - LeadingZeroCountInt32(left);
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

    private static int LeadingZeroCountInt32(UInt128 value)
    {
        if (value._upper != 0) return MathCompat.LeadingZeroCount(value._upper);
        return 64 + MathCompat.LeadingZeroCount(value._lower);
    }
}
#endif
