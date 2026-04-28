using System.Runtime.InteropServices;

namespace DatabaseDecimal.Values;

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
    // Division via binary long division
    // ----------------------------------------------------------------

    internal static (UInt256 Quotient, UInt256 Remainder) DivRem(UInt256 left, UInt256 right)
    {
        if (right == Zero)
            throw new DivideByZeroException();
        if (left < right)
            return (Zero, left);
        if (left == right)
            return (One, Zero);

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
}
