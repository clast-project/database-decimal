// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Performs division on fixed-point decimal values.
/// The dividend is pre-scaled (multiplied by 10^result_scale) before
/// integer division to preserve fractional digits in the result.
/// </summary>
public static class DivideKernel
{
    /// <summary>
    /// Divide two 32-bit values, widening to 64-bit for the pre-scaled dividend.
    /// Result is 64-bit.
    /// </summary>
    public static Decimal64 Divide(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType)
    {
        // The logical value is (left / 10^ls) / (right / 10^rs) = (left / right) * 10^(rs-ls)
        // We want a result with scale resultType.Scale, so:
        // result_mantissa = left * 10^(resultType.Scale - ls + rs) / right
        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        long scaledDividend = ScaleHelper.Widen32To64(left.Mantissa, 0, prescaleAmount);
        return new Decimal64(ScaleHelper.DivideRoundHalfEven(scaledDividend, right.Mantissa));
    }

    /// <summary>
    /// Divide two 64-bit values, widening to 128-bit for the pre-scaled dividend.
    /// Result is 128-bit.
    /// </summary>
    public static Decimal128 Divide(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType)
    {
        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 scaledDividend = ScaleHelper.Widen64To128(left.Mantissa, 0, prescaleAmount);
        return new Decimal128(ScaleHelper.DivideRoundHalfEven(scaledDividend, right.Mantissa));
    }

    /// <summary>
    /// Divide two 128-bit values. Pre-scales within 128-bit (may lose precision
    /// if the prescale amount is very large, but this is bounded by the
    /// clamping rules which limit the result to 38 digits).
    /// </summary>
    public static Decimal128 Divide(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType)
    {
        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 scaledDividend = ScaleHelper.Rescale128(left.Mantissa, 0, prescaleAmount);
        return new Decimal128(ScaleHelper.DivideRoundHalfEven(scaledDividend, right.Mantissa));
    }

    /// <summary>
    /// Divide two 128-bit values, widening to 256-bit for the pre-scaled dividend.
    /// Result is 256-bit.
    /// </summary>
    public static Decimal256 DivideWiden(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType)
    {
        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 scaledDividend = ScaleHelper.Widen128To256(left.Mantissa, 0, prescaleAmount);
        return new Decimal256(ScaleHelper.DivideRoundHalfEven(scaledDividend, (Int256)right.Mantissa));
    }

    /// <summary>
    /// Divide two 256-bit values.
    /// </summary>
    public static Decimal256 Divide(Decimal256 left, DecimalType leftType, Decimal256 right, DecimalType rightType, DecimalType resultType)
    {
        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 scaledDividend = ScaleHelper.Rescale256(left.Mantissa, 0, prescaleAmount);
        return new Decimal256(ScaleHelper.DivideRoundHalfEven(scaledDividend, right.Mantissa));
    }
}
