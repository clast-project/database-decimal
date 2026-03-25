using System.Runtime.InteropServices;

namespace DatabaseDecimal.Values;

/// <summary>
/// A 256-bit signed integer in two's complement representation,
/// built from two UInt128 halves. Follows the same design patterns
/// as the BCL's Int128 type.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Int256 : IEquatable<Int256>, IComparable<Int256>
{
    internal readonly UInt128 _lower;
    internal readonly UInt128 _upper;

    public Int256(UInt128 upper, UInt128 lower)
    {
        _lower = lower;
        _upper = upper;
    }

    internal UInt128 Lower => _lower;
    internal UInt128 Upper => _upper;

    // ----------------------------------------------------------------
    // Constants
    // ----------------------------------------------------------------

    public static Int256 Zero => default;
    public static Int256 One => new(UInt128.Zero, UInt128.One);
    public static Int256 MinusOne => new(UInt128.MaxValue, UInt128.MaxValue);

    /// <summary>2^255 - 1</summary>
    public static Int256 MaxValue => new(
        new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF),
        UInt128.MaxValue);

    /// <summary>-2^255</summary>
    public static Int256 MinValue => new(
        new UInt128(0x8000_0000_0000_0000, 0),
        UInt128.Zero);

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    public static bool IsNegative(Int256 value) =>
        (long)(ulong)(value._upper >>> 64) < 0;

    public static bool IsZero(Int256 value) =>
        value._lower == UInt128.Zero && value._upper == UInt128.Zero;

    public static Int256 Abs(Int256 value) =>
        IsNegative(value) ? -value : value;

    // ----------------------------------------------------------------
    // Widening multiply: Int128 * Int128 -> Int256
    // Delegates to UInt256.BigMul with sign correction,
    // matching the BCL's Int128.BigMul pattern.
    // ----------------------------------------------------------------

    public static Int256 BigMul(Int128 left, Int128 right)
    {
        UInt256 product = UInt256.BigMul((UInt128)left, (UInt128)right);

        // Sign correction for two's complement:
        // If left is negative, subtract right from the upper half.
        // If right is negative, subtract left from the upper half.
        UInt128 upper = product._upper;
        if (left < 0) upper -= (UInt128)right;
        if (right < 0) upper -= (UInt128)left;

        return new Int256(upper, product._lower);
    }

    // ----------------------------------------------------------------
    // Arithmetic operators
    // ----------------------------------------------------------------

    public static Int256 operator +(Int256 left, Int256 right)
    {
        UInt128 lower = left._lower + right._lower;
        UInt128 carry = (lower < left._lower) ? UInt128.One : UInt128.Zero;
        UInt128 upper = left._upper + right._upper + carry;
        return new Int256(upper, lower);
    }

    public static Int256 operator checked +(Int256 left, Int256 right)
    {
        Int256 result = left + right;
        // Signed overflow: (result ^ left) & ~(left ^ right) has sign bit set
        if (IsNegative(new Int256((result._upper ^ left._upper) & ~(left._upper ^ right._upper), UInt128.Zero)))
            throw new OverflowException();
        return result;
    }

    public static Int256 operator -(Int256 left, Int256 right)
    {
        UInt128 lower = left._lower - right._lower;
        UInt128 borrow = (lower > left._lower) ? UInt128.One : UInt128.Zero;
        UInt128 upper = left._upper - right._upper - borrow;
        return new Int256(upper, lower);
    }

    public static Int256 operator checked -(Int256 left, Int256 right)
    {
        Int256 result = left - right;
        // Signed overflow: (result ^ left) & (left ^ right) has sign bit set
        if (IsNegative(new Int256((result._upper ^ left._upper) & (left._upper ^ right._upper), UInt128.Zero)))
            throw new OverflowException();
        return result;
    }

    public static Int256 operator -(Int256 value) => Zero - value;

    public static Int256 operator checked -(Int256 value) => checked(Zero - value);

    /// <summary>
    /// Truncated multiplication. Delegates to UInt256 since truncated
    /// multiply is the same for signed and unsigned two's complement.
    /// </summary>
    public static Int256 operator *(Int256 left, Int256 right) =>
        (Int256)((UInt256)left * (UInt256)right);

    public static Int256 operator /(Int256 left, Int256 right)
    {
        if (right == MinusOne && left == MinValue)
            throw new OverflowException("Int256.MinValue / -1 overflows.");

        // Convert to unsigned absolute values, divide, restore sign.
        bool negateResult = IsNegative(left) ^ IsNegative(right);
        UInt256 uleft = IsNegative(left) ? (UInt256)(-left) : (UInt256)left;
        UInt256 uright = IsNegative(right) ? (UInt256)(-right) : (UInt256)right;
        UInt256 result = uleft / uright;
        return negateResult ? -(Int256)result : (Int256)result;
    }

    public static Int256 operator %(Int256 left, Int256 right)
    {
        // remainder has the sign of the dividend
        bool negateResult = IsNegative(left);
        UInt256 uleft = IsNegative(left) ? (UInt256)(-left) : (UInt256)left;
        UInt256 uright = IsNegative(right) ? (UInt256)(-right) : (UInt256)right;
        UInt256 result = uleft % uright;
        return negateResult ? -(Int256)result : (Int256)result;
    }

    public static Int256 operator ++(Int256 value) => value + One;
    public static Int256 operator --(Int256 value) => value - One;

    // ----------------------------------------------------------------
    // Comparison operators (signed)
    // Upper word compared as signed (cast to Int128), lower as unsigned.
    // ----------------------------------------------------------------

    public static bool operator ==(Int256 left, Int256 right) =>
        left._lower == right._lower && left._upper == right._upper;

    public static bool operator !=(Int256 left, Int256 right) =>
        left._lower != right._lower || left._upper != right._upper;

    public static bool operator <(Int256 left, Int256 right) =>
        ((Int128)left._upper < (Int128)right._upper) ||
        (left._upper == right._upper && left._lower < right._lower);

    public static bool operator >(Int256 left, Int256 right) =>
        ((Int128)left._upper > (Int128)right._upper) ||
        (left._upper == right._upper && left._lower > right._lower);

    public static bool operator <=(Int256 left, Int256 right) => !(left > right);
    public static bool operator >=(Int256 left, Int256 right) => !(left < right);

    // ----------------------------------------------------------------
    // Bitwise operators
    // ----------------------------------------------------------------

    public static Int256 operator &(Int256 left, Int256 right) =>
        new(left._upper & right._upper, left._lower & right._lower);

    public static Int256 operator |(Int256 left, Int256 right) =>
        new(left._upper | right._upper, left._lower | right._lower);

    public static Int256 operator ^(Int256 left, Int256 right) =>
        new(left._upper ^ right._upper, left._lower ^ right._lower);

    public static Int256 operator ~(Int256 value) =>
        new(~value._upper, ~value._lower);

    // ----------------------------------------------------------------
    // Shift operators
    // Arithmetic right shift: sign-extends from the upper word.
    // ----------------------------------------------------------------

    public static Int256 operator <<(Int256 value, int shiftAmount)
    {
        shiftAmount &= 0xFF;

        if (shiftAmount == 0)
            return value;

        if (shiftAmount >= 128)
            return new Int256(value._lower << (shiftAmount - 128), UInt128.Zero);

        return new Int256(
            (value._upper << shiftAmount) | (value._lower >>> (128 - shiftAmount)),
            value._lower << shiftAmount);
    }

    /// <summary>Arithmetic right shift (sign-extending).</summary>
    public static Int256 operator >>(Int256 value, int shiftAmount)
    {
        shiftAmount &= 0xFF;

        if (shiftAmount == 0)
            return value;

        if (shiftAmount >= 128)
        {
            // Upper fills with sign bits, lower gets upper shifted
            UInt128 newLower = (UInt128)((Int128)value._upper >> (shiftAmount - 128));
            UInt128 newUpper = (UInt128)((Int128)value._upper >> 127); // all sign bits
            return new Int256(newUpper, newLower);
        }

        return new Int256(
            (UInt128)((Int128)value._upper >> shiftAmount),
            (value._lower >>> shiftAmount) | (value._upper << (128 - shiftAmount)));
    }

    /// <summary>Logical right shift (zero-extending).</summary>
    public static Int256 operator >>>(Int256 value, int shiftAmount)
    {
        shiftAmount &= 0xFF;

        if (shiftAmount == 0)
            return value;

        if (shiftAmount >= 128)
            return new Int256(UInt128.Zero, value._upper >>> (shiftAmount - 128));

        return new Int256(
            value._upper >>> shiftAmount,
            (value._lower >>> shiftAmount) | (value._upper << (128 - shiftAmount)));
    }

    // ----------------------------------------------------------------
    // Conversion operators
    // ----------------------------------------------------------------

    // Widening (implicit)
    public static implicit operator Int256(Int128 value)
    {
        UInt128 upper = (value < 0) ? UInt128.MaxValue : UInt128.Zero;
        return new Int256(upper, (UInt128)value);
    }

    public static implicit operator Int256(long value) => (Int256)(Int128)value;
    public static implicit operator Int256(int value) => (Int256)(Int128)value;

    // Narrowing (explicit)
    public static explicit operator Int128(Int256 value)
    {
        // In a checked context the caller should verify the value fits
        return (Int128)value._lower;
    }

    public static explicit operator long(Int256 value) => (long)(Int128)value._lower;
    public static explicit operator int(Int256 value) => (int)(Int128)value._lower;

    // Reinterpret casts between signed and unsigned
    public static explicit operator UInt256(Int256 value) => new(value._upper, value._lower);
    public static explicit operator Int256(UInt256 value) => new(value._upper, value._lower);

    // ----------------------------------------------------------------
    // IEquatable, IComparable
    // ----------------------------------------------------------------

    public bool Equals(Int256 other) => this == other;
    public override bool Equals(object? obj) => obj is Int256 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_upper, _lower);

    public int CompareTo(Int256 other)
    {
        // Compare upper as signed, lower as unsigned
        int cmp = ((Int128)_upper).CompareTo((Int128)other._upper);
        return cmp != 0 ? cmp : _lower.CompareTo(other._lower);
    }

    // ----------------------------------------------------------------
    // ToString
    // Handles sign, delegates magnitude to UInt256.
    // ----------------------------------------------------------------

    public override string ToString()
    {
        if (IsNegative(this))
        {
            // Negate via two's complement at the unsigned level
            // (avoids overflow for MinValue)
            UInt256 abs = ~(UInt256)this + UInt256.One;
            return "-" + abs.ToString();
        }

        return ((UInt256)this).ToString();
    }
}
