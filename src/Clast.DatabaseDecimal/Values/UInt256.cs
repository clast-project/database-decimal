// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clast.DatabaseDecimal.Values;

/// <summary>
/// A 256-bit unsigned integer, built from two UInt128 halves.
/// Follows the same design patterns as the BCL's UInt128 type.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UInt256 : IEquatable<UInt256>, IComparable<UInt256>
{
    internal readonly UInt128 _lower;
    internal readonly UInt128 _upper;

    public UInt256(UInt128 upper, UInt128 lower)
    {
        _lower = lower;
        _upper = upper;
    }

    internal UInt128 Lower => _lower;
    internal UInt128 Upper => _upper;

    // ----------------------------------------------------------------
    // Constants
    // ----------------------------------------------------------------

    public static UInt256 Zero => default;
    public static UInt256 One => new(UInt128.Zero, UInt128.One);
    public static UInt256 MaxValue => new(UInt128.MaxValue, UInt128.MaxValue);
    public static UInt256 MinValue => Zero;

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    internal static int LeadingZeroCount(UInt256 value)
    {
        if (value._upper != UInt128.Zero)
            return LeadingZeroCount128(value._upper);
        return 128 + LeadingZeroCount128(value._lower);
    }

    internal static int LeadingZeroCount128(UInt128 value)
    {
        ulong upper = (ulong)(value >>> 64);
        if (upper != 0)
            return MathCompat.LeadingZeroCount(upper);
        return 64 + MathCompat.LeadingZeroCount((ulong)value);
    }

    // ----------------------------------------------------------------
    // Widening multiply: UInt128 * UInt128 -> UInt256
    // Uses the FOIL method with a 64x64 -> 128 multiplication primitive.
    // ----------------------------------------------------------------

    public static UInt256 BigMul(UInt128 left, UInt128 right)
    {
        ulong al = (ulong)left;
        ulong ah = (ulong)(left >>> 64);
        ulong bl = (ulong)right;
        ulong bh = (ulong)(right >>> 64);

        // Four 64x64 -> 128-bit partial products
        ulong p00_hi = MathCompat.BigMul64(al, bl, out ulong p00_lo);
        ulong p01_hi = MathCompat.BigMul64(ah, bl, out ulong p01_lo);
        ulong p10_hi = MathCompat.BigMul64(al, bh, out ulong p10_lo);
        ulong p11_hi = MathCompat.BigMul64(ah, bh, out ulong p11_lo);

        // Accumulate middle terms with carries
        UInt128 t = new UInt128(p01_hi, p01_lo) + p00_hi;
        UInt128 tl = new UInt128(p10_hi, p10_lo) + (ulong)t;

        UInt128 lower = new UInt128((ulong)tl, p00_lo);
        UInt128 upper = new UInt128(p11_hi, p11_lo) + (ulong)(t >>> 64) + (ulong)(tl >>> 64);

        return new UInt256(upper, lower);
    }

    // ----------------------------------------------------------------
    // Arithmetic operators
    // ----------------------------------------------------------------

    public static UInt256 operator +(UInt256 left, UInt256 right)
    {
        UInt128 lower = left._lower + right._lower;
        UInt128 carry = (lower < left._lower) ? UInt128.One : UInt128.Zero;
        UInt128 upper = left._upper + right._upper + carry;
        return new UInt256(upper, lower);
    }

    public static UInt256 operator checked +(UInt256 left, UInt256 right)
    {
        UInt128 lower = left._lower + right._lower;
        UInt128 carry = (lower < left._lower) ? UInt128.One : UInt128.Zero;
        UInt128 upper = checked(left._upper + right._upper + carry);
        return new UInt256(upper, lower);
    }

    public static UInt256 operator -(UInt256 left, UInt256 right)
    {
        UInt128 lower = left._lower - right._lower;
        UInt128 borrow = (lower > left._lower) ? UInt128.One : UInt128.Zero;
        UInt128 upper = left._upper - right._upper - borrow;
        return new UInt256(upper, lower);
    }

    public static UInt256 operator checked -(UInt256 left, UInt256 right)
    {
        UInt128 lower = left._lower - right._lower;
        UInt128 borrow = (lower > left._lower) ? UInt128.One : UInt128.Zero;
        UInt128 upper = checked(left._upper - right._upper - borrow);
        return new UInt256(upper, lower);
    }

    public static UInt256 operator *(UInt256 left, UInt256 right)
    {
        // Truncated multiply: only compute the lower 256 bits of the full product.
        UInt256 full = BigMul(left._lower, right._lower);
        UInt128 upper = full._upper + left._upper * right._lower + left._lower * right._upper;
        return new UInt256(upper, full._lower);
    }

    /// <summary>
    /// Multiplication that throws rather than wrapping.
    /// </summary>
    /// <remarks>
    /// There is no 512-bit type to widen into, so overflow is decided from the
    /// operands' bit lengths. With a &lt; 2^m and b &lt; 2^n the product is below
    /// 2^(m+n), and with a >= 2^(m-1) and b >= 2^(n-1) it is at least 2^(m+n-2).
    /// So m+n &lt;= 256 always fits and m+n >= 258 always overflows, leaving only
    /// m+n == 257 undecided — and there the wrapped product answers it, since
    /// dividing it back by a non-zero operand reproduces the other only when no
    /// wrap occurred. Both operands are non-zero whenever m+n is 257, because a
    /// zero operand has bit length 0.
    /// </remarks>
    /// <exception cref="OverflowException">The product does not fit in 256 bits.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt256 operator checked *(UInt256 left, UInt256 right)
    {
        // Two 128-bit operands cannot exceed 256 bits, and BigMul gives their
        // exact product directly. This is the common case, and keeping it here
        // rather than in one large method is what lets it inline.
        if (left._upper == UInt128.Zero && right._upper == UInt128.Zero)
            return BigMul(left._lower, right._lower);

        return CheckedMultiplyWide(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static UInt256 CheckedMultiplyWide(UInt256 left, UInt256 right)
    {
        int bits = BitLength(left) + BitLength(right);
        if (bits > 257) throw new OverflowException();

        UInt256 product = left * right;
        if (bits == 257 && product / left != right) throw new OverflowException();

        return product;
    }

    /// <summary>Position of the highest set bit, or 0 for zero.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BitLength(UInt256 value) => 256 - LeadingZeroCount(value);

    public static UInt256 operator /(UInt256 left, UInt256 right)
    {
        var (quotient, _) = DivRem(left, right);
        return quotient;
    }

    public static UInt256 operator %(UInt256 left, UInt256 right)
    {
        var (_, remainder) = DivRem(left, right);
        return remainder;
    }

    public static UInt256 operator -(UInt256 value) => ~value + One;

    public static UInt256 operator ++(UInt256 value) => value + One;
    public static UInt256 operator --(UInt256 value) => value - One;

    // ----------------------------------------------------------------
    // Comparison operators
    // ----------------------------------------------------------------

    public static bool operator ==(UInt256 left, UInt256 right) =>
        left._lower == right._lower && left._upper == right._upper;

    public static bool operator !=(UInt256 left, UInt256 right) =>
        left._lower != right._lower || left._upper != right._upper;

    public static bool operator <(UInt256 left, UInt256 right) =>
        (left._upper < right._upper) || (left._upper == right._upper && left._lower < right._lower);

    public static bool operator >(UInt256 left, UInt256 right) =>
        (left._upper > right._upper) || (left._upper == right._upper && left._lower > right._lower);

    public static bool operator <=(UInt256 left, UInt256 right) => !(left > right);
    public static bool operator >=(UInt256 left, UInt256 right) => !(left < right);

    // ----------------------------------------------------------------
    // Bitwise operators
    // ----------------------------------------------------------------

    public static UInt256 operator &(UInt256 left, UInt256 right) =>
        new(left._upper & right._upper, left._lower & right._lower);

    public static UInt256 operator |(UInt256 left, UInt256 right) =>
        new(left._upper | right._upper, left._lower | right._lower);

    public static UInt256 operator ^(UInt256 left, UInt256 right) =>
        new(left._upper ^ right._upper, left._lower ^ right._lower);

    public static UInt256 operator ~(UInt256 value) =>
        new(~value._upper, ~value._lower);

    // ----------------------------------------------------------------
    // Shift operators
    // Shift amount masked with 0xFF (mod 256), matching BCL convention.
    // ----------------------------------------------------------------

    public static UInt256 operator <<(UInt256 value, int shiftAmount)
    {
        shiftAmount &= 0xFF;

        if (shiftAmount == 0)
            return value;

        if (shiftAmount >= 128)
            return new UInt256(value._lower << (shiftAmount - 128), UInt128.Zero);

        return new UInt256(
            (value._upper << shiftAmount) | (value._lower >>> (128 - shiftAmount)),
            value._lower << shiftAmount);
    }

    public static UInt256 operator >>>(UInt256 value, int shiftAmount)
    {
        shiftAmount &= 0xFF;

        if (shiftAmount == 0)
            return value;

        if (shiftAmount >= 128)
            return new UInt256(UInt128.Zero, value._upper >>> (shiftAmount - 128));

        return new UInt256(
            value._upper >>> shiftAmount,
            (value._lower >>> shiftAmount) | (value._upper << (128 - shiftAmount)));
    }

    public static UInt256 operator >>(UInt256 value, int shiftAmount) => value >>> shiftAmount;

    // ----------------------------------------------------------------
    // Conversion operators
    // ----------------------------------------------------------------

    public static implicit operator UInt256(UInt128 value) => new(UInt128.Zero, value);
    public static implicit operator UInt256(ulong value) => new(UInt128.Zero, value);
    public static implicit operator UInt256(uint value) => new(UInt128.Zero, value);

    public static explicit operator UInt128(UInt256 value) => value._lower;
    public static explicit operator ulong(UInt256 value) => (ulong)value._lower;

    // ----------------------------------------------------------------
    // IEquatable, IComparable
    // ----------------------------------------------------------------

    public bool Equals(UInt256 other) => this == other;
    public override bool Equals(object? obj) => obj is UInt256 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_upper, _lower);

    public int CompareTo(UInt256 other)
    {
        int cmp = _upper.CompareTo(other._upper);
        return cmp != 0 ? cmp : _lower.CompareTo(other._lower);
    }

    // ----------------------------------------------------------------
    // ToString
    // Converts to decimal by repeatedly extracting groups of 18 digits.
    // ----------------------------------------------------------------

    public override string ToString()
    {
        if (this == Zero)
            return "0";

        // UInt256 max is ~1.16 * 10^77, so at most 78 decimal digits
        Span<char> buffer = stackalloc char[78];
        int pos = buffer.Length;
        var remaining = this;
        UInt256 divisor = new UInt256(UInt128.Zero, 1_000_000_000_000_000_000UL); // 10^18

        while (remaining != Zero)
        {
            var (q, r) = DivRem(remaining, divisor);
            ulong digits = (ulong)r._lower;

            if (q != Zero)
            {
                // Not the leading group: pad to 18 digits
                for (int i = 0; i < 18; i++)
                {
                    buffer[--pos] = (char)('0' + (int)(digits % 10));
                    digits /= 10;
                }
            }
            else
            {
                // Leading group: no padding
                do
                {
                    buffer[--pos] = (char)('0' + (int)(digits % 10));
                    digits /= 10;
                } while (digits != 0);
            }

            remaining = q;
        }

        return buffer.Slice(pos).ToString();
    }

    // ----------------------------------------------------------------
    // Division
    //
    // The general case is binary long division: one quotient bit per pass,
    // each pass a 256-bit shift, compare and subtract. Its cost follows the
    // length of the quotient, not of the operands, which is the wrong shape for
    // this package's commonest division — a rescale, dividing a wide mantissa
    // by a small power of ten, whose quotient is nearly as long as the
    // dividend. Dropping two digits from a 77-bit mantissa took about seventy
    // passes per element.
    //
    // A divisor that fits in 64 bits — every power of ten up to 10^19 — instead
    // goes through schoolbook division by 64-bit limbs: four 128-by-64 steps
    // whatever the quotient's length. A dividend and divisor that both fit in
    // 128 bits go to UInt128, which is the runtime's on .NET 8 and later.
    // ----------------------------------------------------------------

    internal static (UInt256 Quotient, UInt256 Remainder) DivRem(UInt256 left, UInt256 right)
    {
        if (right == Zero)
            throw new DivideByZeroException();
        if (left < right)
            return (Zero, left);
        if (left == right)
            return (One, Zero);

        if (right._upper == UInt128.Zero)
        {
            UInt128 divisor = right._lower;
            if ((divisor >> 64) == UInt128.Zero)
                return DivRemBy64(left, (ulong)divisor);

            if (left._upper == UInt128.Zero)
            {
                UInt128 quotient128 = left._lower / divisor;
                return (new UInt256(UInt128.Zero, quotient128),
                        new UInt256(UInt128.Zero, left._lower - quotient128 * divisor));
            }
        }

        int shift = LeadingZeroCount(right) - LeadingZeroCount(left);
        UInt256 remainder = left;
        UInt256 quotient = Zero;
        UInt256 current = right << shift;

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

    /// <summary>
    /// Schoolbook division by a single 64-bit limb, most significant limb first.
    /// The running remainder stays below <paramref name="divisor"/>, so each
    /// step's quotient digit fits in 64 bits.
    /// </summary>
    private static (UInt256 Quotient, UInt256 Remainder) DivRemBy64(UInt256 left, ulong divisor)
    {
        ulong remainder = 0;
        ulong q3 = DivRemStep((ulong)(left._upper >> 64), divisor, ref remainder);
        ulong q2 = DivRemStep((ulong)left._upper, divisor, ref remainder);
        ulong q1 = DivRemStep((ulong)(left._lower >> 64), divisor, ref remainder);
        ulong q0 = DivRemStep((ulong)left._lower, divisor, ref remainder);

        return (new UInt256(new UInt128(q3, q2), new UInt128(q1, q0)),
                new UInt256(UInt128.Zero, new UInt128(0, remainder)));
    }

    /// <summary>
    /// Divides <c>remainder:limb</c> by <paramref name="divisor"/>, replacing
    /// <paramref name="remainder"/> with the new remainder.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong DivRemStep(ulong limb, ulong divisor, ref ulong remainder)
    {
        // Leading zero limbs, and every limb of a dividend below 2^64, skip the
        // 128-by-64 step: a single hardware division does.
        if (remainder == 0)
        {
            ulong quotient = limb / divisor;
            remainder = limb - quotient * divisor;
            return quotient;
        }

        return DivRem128By64(remainder, limb, divisor, out remainder);
    }

    /// <summary>
    /// Divides the 128-bit value <c>high:low</c> by <paramref name="divisor"/>,
    /// which must exceed <paramref name="high"/> so the quotient fits in 64 bits.
    /// </summary>
    /// <remarks>
    /// Knuth's Algorithm D specialised to a two-digit divisor in base 2^32, as
    /// <c>divlu</c> in Hacker's Delight (2nd ed., §9-4). The divisor is
    /// normalised so its top bit is set; each 32-bit quotient digit is then
    /// estimated from the top divisor digit and is at most two too large, which
    /// the correction loops repair. Only 64-bit arithmetic is used, so this is
    /// the same code on every target — the netstandard2.0 UInt128 polyfill
    /// divides one bit at a time and would gain nothing from delegating to it.
    /// </remarks>
    internal static ulong DivRem128By64(ulong high, ulong low, ulong divisor, out ulong remainder)
    {
        const ulong Base = 1UL << 32;
        const ulong DigitMask = Base - 1;

        unchecked
        {
            int shift = MathCompat.LeadingZeroCount(divisor);
            divisor <<= shift;
            ulong divisorHigh = divisor >> 32;
            ulong divisorLow = divisor & DigitMask;

            // Shift the dividend by the same amount. A shift of 64 would be
            // masked to 0 by C#, hence the guard on the carried-in bits.
            ulong numerator32 = shift == 0 ? high : (high << shift) | (low >> (64 - shift));
            ulong numerator10 = low << shift;
            ulong numerator1 = numerator10 >> 32;
            ulong numerator0 = numerator10 & DigitMask;

            // First quotient digit. The q1 >= Base test short-circuits before
            // q1 * divisorLow could overflow; rhat stays below Base whenever the
            // product test runs, so Base * rhat + numerator1 cannot overflow.
            ulong q1 = numerator32 / divisorHigh;
            ulong rhat = numerator32 - q1 * divisorHigh;
            while (q1 >= Base || q1 * divisorLow > Base * rhat + numerator1)
            {
                q1--;
                rhat += divisorHigh;
                if (rhat >= Base) break;
            }

            // Wraps modulo 2^64, but the true value is below the divisor.
            ulong numerator21 = numerator32 * Base + numerator1 - q1 * divisor;

            ulong q0 = numerator21 / divisorHigh;
            rhat = numerator21 - q0 * divisorHigh;
            while (q0 >= Base || q0 * divisorLow > Base * rhat + numerator0)
            {
                q0--;
                rhat += divisorHigh;
                if (rhat >= Base) break;
            }

            remainder = (numerator21 * Base + numerator0 - q0 * divisor) >> shift;
            return q1 * Base + q0;
        }
    }
}
