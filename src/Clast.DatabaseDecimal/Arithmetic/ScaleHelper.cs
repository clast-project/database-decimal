// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Rescales mantissa values by multiplying or dividing by powers of 10.
/// </summary>
public static class ScaleHelper
{
    /// <summary>
    /// Rescale a 32-bit mantissa, staying within 32-bit.
    /// </summary>
    public static int Rescale32(int mantissa, int fromScale, int toScale)
    {
        int delta = toScale - fromScale;
        if (delta == 0) return mantissa;
        if (delta > 0) return checked(mantissa * PowersOf10.Int32[delta]);
        return DivideRoundHalfEven(mantissa, PowersOf10.Int32[-delta]);
    }

    /// <summary>
    /// Widen a 32-bit mantissa to 64-bit and rescale.
    /// </summary>
    public static long Widen32To64(int mantissa, int fromScale, int toScale)
    {
        long wide = mantissa;
        int delta = toScale - fromScale;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int64[delta]);
        return DivideRoundHalfEven(wide, PowersOf10.Int64[-delta]);
    }

    /// <summary>
    /// Rescale a 64-bit mantissa, staying within 64-bit.
    /// </summary>
    public static long Rescale64(long mantissa, int fromScale, int toScale)
    {
        int delta = toScale - fromScale;
        if (delta == 0) return mantissa;
        if (delta > 0) return checked(mantissa * PowersOf10.Int64[delta]);
        return DivideRoundHalfEven(mantissa, PowersOf10.Int64[-delta]);
    }

    /// <summary>
    /// Widen a 64-bit mantissa to 128-bit and rescale.
    /// </summary>
    public static Int128 Widen64To128(long mantissa, int fromScale, int toScale)
    {
        Int128 wide = mantissa;
        int delta = toScale - fromScale;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int128[delta]);
        return DivideRoundHalfEven(wide, PowersOf10.Int128[-delta]);
    }

    /// <summary>
    /// Rescale a 128-bit mantissa, staying within 128-bit.
    /// </summary>
    public static Int128 Rescale128(Int128 mantissa, int fromScale, int toScale)
    {
        int delta = toScale - fromScale;
        if (delta == 0) return mantissa;
        if (delta > 0) return checked(mantissa * PowersOf10.Int128[delta]);
        return DivideRoundHalfEven(mantissa, PowersOf10.Int128[-delta]);
    }

    /// <summary>
    /// Integer division with banker's rounding (round half to even).
    /// </summary>
    internal static int DivideRoundHalfEven(int dividend, int divisor)
    {
        int quotient = dividend / divisor;
        int remainder = dividend % divisor;
        int absRemainder = Math.Abs(remainder);
        int halfDivisor = Math.Abs(divisor) / 2;

        if (absRemainder > halfDivisor)
            quotient += (dividend >= 0) ? 1 : -1;
        else if (absRemainder == halfDivisor && Math.Abs(divisor) % 2 == 0 && (quotient & 1) != 0)
            quotient += (dividend >= 0) ? 1 : -1;

        return quotient;
    }

    /// <summary>
    /// Integer division with banker's rounding (round half to even).
    /// </summary>
    internal static long DivideRoundHalfEven(long dividend, long divisor)
    {
        long quotient = dividend / divisor;
        long remainder = dividend % divisor;
        long absRemainder = Math.Abs(remainder);
        long halfDivisor = Math.Abs(divisor) / 2;

        if (absRemainder > halfDivisor)
            quotient += (dividend >= 0) ? 1L : -1L;
        else if (absRemainder == halfDivisor && Math.Abs(divisor) % 2 == 0 && (quotient & 1) != 0)
            quotient += (dividend >= 0) ? 1L : -1L;

        return quotient;
    }

    /// <summary>
    /// Integer division with banker's rounding (round half to even).
    /// </summary>
    internal static Int128 DivideRoundHalfEven(Int128 dividend, Int128 divisor)
    {
        Int128 quotient = dividend / divisor;
        Int128 remainder = dividend % divisor;
        Int128 absRemainder = Int128.Abs(remainder);
        Int128 absDivisor = Int128.Abs(divisor);
        Int128 halfDivisor = absDivisor / 2;

        if (absRemainder > halfDivisor)
            quotient += (dividend >= 0) ? Int128.One : -Int128.One;
        else if (absRemainder == halfDivisor && absDivisor % 2 == 0 && (quotient & 1) != 0)
            quotient += (dividend >= 0) ? Int128.One : -Int128.One;

        return quotient;
    }

    /// <summary>
    /// Widen a 128-bit mantissa to 256-bit and rescale.
    /// </summary>
    public static Int256 Widen128To256(Int128 mantissa, int fromScale, int toScale)
    {
        Int256 wide = mantissa;
        int delta = toScale - fromScale;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int256[delta]);
        return DivideRoundHalfEven(wide, PowersOf10.Int256[-delta]);
    }

    /// <summary>
    /// Rescale a 256-bit mantissa, staying within 256-bit.
    /// </summary>
    public static Int256 Rescale256(Int256 mantissa, int fromScale, int toScale)
    {
        int delta = toScale - fromScale;
        if (delta == 0) return mantissa;
        if (delta > 0) return checked(mantissa * PowersOf10.Int256[delta]);
        return DivideRoundHalfEven(mantissa, PowersOf10.Int256[-delta]);
    }

    /// <summary>
    /// Integer division with banker's rounding (round half to even).
    /// </summary>
    internal static Int256 DivideRoundHalfEven(Int256 dividend, Int256 divisor)
    {
        Int256 quotient = dividend / divisor;
        Int256 remainder = dividend % divisor;
        Int256 absRemainder = Int256.Abs(remainder);
        Int256 absDivisor = Int256.Abs(divisor);
        Int256 halfDivisor = absDivisor / (Int256)2;

        if (absRemainder > halfDivisor)
            quotient += Int256.IsNegative(dividend) ? Int256.MinusOne : Int256.One;
        else if (absRemainder == halfDivisor && absDivisor % (Int256)2 == Int256.Zero && (quotient & Int256.One) != Int256.Zero)
            quotient += Int256.IsNegative(dividend) ? Int256.MinusOne : Int256.One;

        return quotient;
    }
}
