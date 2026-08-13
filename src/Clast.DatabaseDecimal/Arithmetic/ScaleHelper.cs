// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Rescales mantissa values by multiplying or dividing by powers of 10.
/// Scaling up is exact; scaling down discards digits and applies the
/// requested <see cref="DecimalRounding"/> mode.
/// </summary>
public static class ScaleHelper
{
    /// <summary>
    /// Rescale a 32-bit mantissa, staying within 32-bit.
    /// </summary>
    public static int Rescale32(int mantissa, int fromScale, int toScale,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        int delta = toScale - fromScale;
        if (delta == 0) return mantissa;
        if (delta > 0) return checked(mantissa * PowersOf10.Int32[delta]);
        return DivideRound(mantissa, PowersOf10.Int32[-delta], rounding);
    }

    /// <summary>
    /// Widen a 32-bit mantissa to 64-bit and rescale.
    /// </summary>
    public static long Widen32To64(int mantissa, int fromScale, int toScale,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        long wide = mantissa;
        int delta = toScale - fromScale;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int64[delta]);
        return DivideRound(wide, PowersOf10.Int64[-delta], rounding);
    }

    /// <summary>
    /// Rescale a 64-bit mantissa, staying within 64-bit.
    /// </summary>
    public static long Rescale64(long mantissa, int fromScale, int toScale,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        int delta = toScale - fromScale;
        if (delta == 0) return mantissa;
        if (delta > 0) return checked(mantissa * PowersOf10.Int64[delta]);
        return DivideRound(mantissa, PowersOf10.Int64[-delta], rounding);
    }

    /// <summary>
    /// Widen a 64-bit mantissa to 128-bit and rescale.
    /// </summary>
    public static Int128 Widen64To128(long mantissa, int fromScale, int toScale,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        Int128 wide = mantissa;
        int delta = toScale - fromScale;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int128[delta]);
        return DivideRound(wide, PowersOf10.Int128[-delta], rounding);
    }

    /// <summary>
    /// Rescale a 128-bit mantissa, staying within 128-bit.
    /// </summary>
    public static Int128 Rescale128(Int128 mantissa, int fromScale, int toScale,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        int delta = toScale - fromScale;
        if (delta == 0) return mantissa;
        if (delta > 0) return checked(mantissa * PowersOf10.Int128[delta]);
        return DivideRound(mantissa, PowersOf10.Int128[-delta], rounding);
    }

    /// <summary>
    /// Widen a 128-bit mantissa to 256-bit and rescale.
    /// </summary>
    public static Int256 Widen128To256(Int128 mantissa, int fromScale, int toScale,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        Int256 wide = mantissa;
        int delta = toScale - fromScale;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int256[delta]);
        return DivideRound(wide, PowersOf10.Int256[-delta], rounding);
    }

    /// <summary>
    /// Rescale a 256-bit mantissa, staying within 256-bit.
    /// </summary>
    public static Int256 Rescale256(Int256 mantissa, int fromScale, int toScale,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        int delta = toScale - fromScale;
        if (delta == 0) return mantissa;
        if (delta > 0) return checked(mantissa * PowersOf10.Int256[delta]);
        return DivideRound(mantissa, PowersOf10.Int256[-delta], rounding);
    }

    // ================================================================
    // Rounded integer division — the one place a rounding mode is applied.
    //
    // The quotient is rounded to nearest; the mode only decides which way an
    // exact midpoint goes. |remainder| is compared against floor(|divisor|/2):
    // when the divisor is odd that floor is strictly below the true midpoint,
    // so equality there means the value is below half and rounds toward zero
    // regardless of mode. Only an even divisor can produce a real midpoint.
    //
    // The adjustment moves the quotient away from zero, so its direction
    // follows the sign of the quotient — dividend sign XOR divisor sign —
    // not the sign of the dividend. (The dividend is always the value being
    // rescaled and the divisor a positive power of 10 on the Rescale paths,
    // but DivideKernel passes a caller-supplied divisor that may be negative.)
    // ================================================================

    /// <summary>
    /// Integer division rounded to nearest, with <paramref name="rounding"/>
    /// deciding the direction of an exact midpoint.
    /// </summary>
    internal static int DivideRound(int dividend, int divisor, DecimalRounding rounding)
    {
        int quotient = dividend / divisor;
        int remainder = dividend % divisor;
        if (remainder == 0) return quotient;

        int absRemainder = remainder < 0 ? -remainder : remainder;
        int absDivisor = divisor < 0 ? -divisor : divisor;
        int halfDivisor = absDivisor >> 1;

        bool roundAway = absRemainder > halfDivisor
            || (absRemainder == halfDivisor
                && (absDivisor & 1) == 0
                && (rounding == DecimalRounding.HalfUp || (quotient & 1) != 0));

        if (!roundAway) return quotient;
        return quotient + (((dividend < 0) != (divisor < 0)) ? -1 : 1);
    }

    /// <summary>
    /// Integer division rounded to nearest, with <paramref name="rounding"/>
    /// deciding the direction of an exact midpoint.
    /// </summary>
    internal static long DivideRound(long dividend, long divisor, DecimalRounding rounding)
    {
        long quotient = dividend / divisor;
        long remainder = dividend % divisor;
        if (remainder == 0) return quotient;

        long absRemainder = remainder < 0 ? -remainder : remainder;
        long absDivisor = divisor < 0 ? -divisor : divisor;
        long halfDivisor = absDivisor >> 1;

        bool roundAway = absRemainder > halfDivisor
            || (absRemainder == halfDivisor
                && (absDivisor & 1) == 0
                && (rounding == DecimalRounding.HalfUp || (quotient & 1) != 0));

        if (!roundAway) return quotient;
        return quotient + (((dividend < 0) != (divisor < 0)) ? -1L : 1L);
    }

    /// <summary>
    /// Integer division rounded to nearest, with <paramref name="rounding"/>
    /// deciding the direction of an exact midpoint.
    /// </summary>
    internal static Int128 DivideRound(Int128 dividend, Int128 divisor, DecimalRounding rounding)
    {
        Int128 quotient = dividend / divisor;
        Int128 remainder = dividend % divisor;
        if (remainder == Int128.Zero) return quotient;

        Int128 absRemainder = remainder < Int128.Zero ? -remainder : remainder;
        Int128 absDivisor = divisor < Int128.Zero ? -divisor : divisor;
        Int128 halfDivisor = absDivisor >> 1;

        bool roundAway = absRemainder > halfDivisor
            || (absRemainder == halfDivisor
                && (absDivisor & Int128.One) == Int128.Zero
                && (rounding == DecimalRounding.HalfUp || (quotient & Int128.One) != Int128.Zero));

        if (!roundAway) return quotient;
        return quotient + (((dividend < Int128.Zero) != (divisor < Int128.Zero)) ? -Int128.One : Int128.One);
    }

    /// <summary>
    /// Integer division rounded to nearest, with <paramref name="rounding"/>
    /// deciding the direction of an exact midpoint.
    /// </summary>
    internal static Int256 DivideRound(Int256 dividend, Int256 divisor, DecimalRounding rounding)
    {
        Int256 quotient = dividend / divisor;
        Int256 remainder = dividend % divisor;
        if (Int256.IsZero(remainder)) return quotient;

        Int256 absRemainder = Int256.Abs(remainder);
        Int256 absDivisor = Int256.Abs(divisor);
        Int256 halfDivisor = absDivisor >> 1;

        bool roundAway = absRemainder > halfDivisor
            || (absRemainder == halfDivisor
                && (absDivisor & Int256.One) == Int256.Zero
                && (rounding == DecimalRounding.HalfUp || (quotient & Int256.One) != Int256.Zero));

        if (!roundAway) return quotient;
        return quotient + ((Int256.IsNegative(dividend) != Int256.IsNegative(divisor)) ? Int256.MinusOne : Int256.One);
    }
}
