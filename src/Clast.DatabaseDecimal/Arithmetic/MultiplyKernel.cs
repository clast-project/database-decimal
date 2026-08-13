// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Performs multiplication on fixed-point decimal values.
/// Multiplying two mantissas produces a result with scale = s1 + s2,
/// which may then need rescaling if the result type has a different scale
/// (e.g. due to precision clamping). Rescaling downward discards digits and
/// applies <c>rounding</c>.
/// </summary>
public static class MultiplyKernel
{
    /// <summary>
    /// Multiply two 32-bit values, widening to 64-bit to avoid overflow,
    /// then rescale to the result scale if needed.
    /// </summary>
    public static Decimal64 Multiply(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        long product = (long)left.Mantissa * right.Mantissa;
        int rawScale = leftType.Scale + rightType.Scale;
        if (rawScale != resultType.Scale)
            product = ScaleHelper.Rescale64(product, rawScale, resultType.Scale, rounding);
        return new Decimal64(DecimalRange.Enforce(product, resultType, overflow));
    }

    /// <summary>
    /// Multiply two 64-bit values, widening to 128-bit.
    /// </summary>
    public static Decimal128 Multiply(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        Int128 product = (Int128)left.Mantissa * right.Mantissa;
        int rawScale = leftType.Scale + rightType.Scale;
        if (rawScale != resultType.Scale)
            product = ScaleHelper.Rescale128(product, rawScale, resultType.Scale, rounding);
        return new Decimal128(DecimalRange.Enforce(product, resultType, overflow));
    }

    /// <summary>
    /// Multiply two 128-bit values, producing a 128-bit result.
    /// </summary>
    /// <remarks>
    /// The exact product needs up to 256 bits, so it is formed there and the
    /// product itself is rescaled to the result scale. Reducing the scale of
    /// one operand before multiplying is not equivalent: precision clamping can
    /// demand more digits than that operand has, which zeroes it — e.g.
    /// NUMERIC(38,10) × NUMERIC(38,10) clamps to NUMERIC(38,6), a 14-digit
    /// reduction applied to an 11-digit mantissa.
    /// </remarks>
    /// <exception cref="OverflowException">The rescaled product does not fit in 128 bits.</exception>
    public static Decimal128 Multiply(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        int rawScale = leftType.Scale + rightType.Scale;
        if (rawScale == resultType.Scale)
            return new Decimal128(DecimalRange.Enforce(checked(left.Mantissa * right.Mantissa), resultType, overflow));

        return new Decimal128(DecimalRange.Enforce(MultiplyRescale128(
            left.Mantissa, right.Mantissa, rawScale, resultType.Scale, rounding), resultType, overflow));
    }

    /// <summary>
    /// Multiplies two 128-bit mantissas exactly and rescales the product to
    /// <paramref name="resultScale"/>, narrowing back to 128 bits.
    /// </summary>
    /// <remarks>
    /// The exact product is formed in 256 bits, but most products still fit in
    /// 128 bits — and 128-bit division is an order of magnitude cheaper than the
    /// software long division on <see cref="Int256"/> — so the common case
    /// narrows before rescaling.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Int128 MultiplyRescale128(Int128 left, Int128 right, int rawScale, int resultScale,
        DecimalRounding rounding)
    {
        Int256 product = Int256.BigMul(left, right);

        if (product <= (Int256)Int128.MaxValue && product >= (Int256)Int128.MinValue)
            return ScaleHelper.Rescale128((Int128)product, rawScale, resultScale, rounding);

        return NarrowChecked(ScaleHelper.Rescale256(product, rawScale, resultScale, rounding));
    }

    /// <summary>
    /// Multiply two 128-bit values, widening to 256-bit via Int256.BigMul.
    /// </summary>
    public static Decimal256 MultiplyWiden(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        Int256 product = Int256.BigMul(left.Mantissa, right.Mantissa);
        int rawScale = leftType.Scale + rightType.Scale;
        if (rawScale != resultType.Scale)
            product = ScaleHelper.Rescale256(product, rawScale, resultType.Scale, rounding);
        return new Decimal256(DecimalRange.Enforce(product, resultType, overflow));
    }

    /// <summary>
    /// Multiply two 256-bit values.
    /// </summary>
    /// <remarks>
    /// There is no 512-bit intermediate to hold the exact product, so when the
    /// result scale is below s1+s2 the reduction is applied to the operands
    /// instead: as much as possible to the operand with the larger scale, and
    /// the remainder to the other, which is always enough because the result
    /// scale is non-negative. Each operand is rounded independently, so the
    /// result can differ by one unit in the last place from rounding the exact
    /// product. Use <see cref="MultiplyWiden"/> from the 128-bit tier where an
    /// exact product is required.
    /// </remarks>
    public static Decimal256 Multiply(Decimal256 left, DecimalType leftType, Decimal256 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        int rawScale = leftType.Scale + rightType.Scale;
        int scaleReduction = rawScale - resultType.Scale;

        if (scaleReduction <= 0)
        {
            Int256 product = left.Mantissa * right.Mantissa;
            if (scaleReduction < 0)
                product = ScaleHelper.Rescale256(product, rawScale, resultType.Scale, rounding);
            return new Decimal256(DecimalRange.Enforce(product, resultType, overflow));
        }

        SplitScaleReduction(scaleReduction, leftType.Scale, rightType.Scale,
            out int leftReduction, out int rightReduction);

        Int256 l = ScaleHelper.Rescale256(left.Mantissa, leftType.Scale, leftType.Scale - leftReduction, rounding);
        Int256 r = ScaleHelper.Rescale256(right.Mantissa, rightType.Scale, rightType.Scale - rightReduction, rounding);
        return new Decimal256(DecimalRange.Enforce(l * r, resultType, overflow));
    }

    /// <summary>
    /// Divides a scale reduction between two operands, taking as much as
    /// possible from the one with the larger scale and spilling the rest to the
    /// other. Neither operand is driven to a negative scale: the total
    /// reduction never exceeds s1+s2 because the result scale is non-negative.
    /// </summary>
    internal static void SplitScaleReduction(int scaleReduction, int leftScale, int rightScale,
        out int leftReduction, out int rightReduction)
    {
        if (leftScale >= rightScale)
        {
            leftReduction = Math.Min(scaleReduction, leftScale);
            rightReduction = scaleReduction - leftReduction;
        }
        else
        {
            rightReduction = Math.Min(scaleReduction, rightScale);
            leftReduction = scaleReduction - rightReduction;
        }
    }

    /// <summary>
    /// Narrows a 256-bit value to 128 bits, throwing if it does not fit.
    /// Mirrors the <c>checked</c> multiply on the same-scale path.
    /// </summary>
    internal static Int128 NarrowChecked(Int256 value)
    {
        if (value > (Int256)Int128.MaxValue || value < (Int256)Int128.MinValue)
            throw new OverflowException("The decimal product does not fit in a 128-bit mantissa.");
        return (Int128)value;
    }
}
