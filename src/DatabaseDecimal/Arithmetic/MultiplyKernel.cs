using DatabaseDecimal.Values;

namespace DatabaseDecimal.Arithmetic;

/// <summary>
/// Performs multiplication on fixed-point decimal values.
/// Multiplying two mantissas produces a result with scale = s1 + s2,
/// which may then need rescaling if the result type has a different scale
/// (e.g. due to precision clamping).
/// </summary>
public static class MultiplyKernel
{
    /// <summary>
    /// Multiply two 32-bit values, widening to 64-bit to avoid overflow,
    /// then rescale to the result scale if needed.
    /// </summary>
    public static Decimal64 Multiply(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType)
    {
        long product = (long)left.Mantissa * right.Mantissa;
        int rawScale = leftType.Scale + rightType.Scale;
        if (rawScale != resultType.Scale)
            product = ScaleHelper.Rescale64(product, rawScale, resultType.Scale);
        return new Decimal64(product);
    }

    /// <summary>
    /// Multiply two 64-bit values, widening to 128-bit.
    /// </summary>
    public static Decimal128 Multiply(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType)
    {
        Int128 product = (Int128)left.Mantissa * right.Mantissa;
        int rawScale = leftType.Scale + rightType.Scale;
        if (rawScale != resultType.Scale)
            product = ScaleHelper.Rescale128(product, rawScale, resultType.Scale);
        return new Decimal128(product);
    }

    /// <summary>
    /// Multiply two 128-bit values. The raw product could be up to 256 bits,
    /// but when precision is clamped to 38 and scale is reduced, we can
    /// first rescale one operand down and then multiply within 128 bits.
    /// </summary>
    public static Decimal128 Multiply(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType)
    {
        // The full product scale is s1 + s2. The result scale may be smaller
        // due to precision clamping. We reduce the scale of the operand with
        // more fractional digits before multiplying to stay within 128 bits.
        int rawScale = leftType.Scale + rightType.Scale;
        int scaleReduction = rawScale - resultType.Scale;

        Int128 l = left.Mantissa;
        Int128 r = right.Mantissa;

        if (scaleReduction > 0)
        {
            // Reduce scale of the operand with more scale digits
            if (leftType.Scale >= rightType.Scale)
                l = ScaleHelper.Rescale128(l, leftType.Scale, leftType.Scale - scaleReduction);
            else
                r = ScaleHelper.Rescale128(r, rightType.Scale, rightType.Scale - scaleReduction);
        }

        return new Decimal128(checked(l * r));
    }

    /// <summary>
    /// Multiply two 128-bit values, widening to 256-bit via Int256.BigMul.
    /// </summary>
    public static Decimal256 MultiplyWiden(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType)
    {
        Int256 product = Int256.BigMul(left.Mantissa, right.Mantissa);
        int rawScale = leftType.Scale + rightType.Scale;
        if (rawScale != resultType.Scale)
            product = ScaleHelper.Rescale256(product, rawScale, resultType.Scale);
        return new Decimal256(product);
    }

    /// <summary>
    /// Multiply two 256-bit values. Pre-reduces scale to avoid overflow,
    /// following the same pattern as the 128-bit overload.
    /// </summary>
    public static Decimal256 Multiply(Decimal256 left, DecimalType leftType, Decimal256 right, DecimalType rightType, DecimalType resultType)
    {
        int rawScale = leftType.Scale + rightType.Scale;
        int scaleReduction = rawScale - resultType.Scale;

        Int256 l = left.Mantissa;
        Int256 r = right.Mantissa;

        if (scaleReduction > 0)
        {
            if (leftType.Scale >= rightType.Scale)
                l = ScaleHelper.Rescale256(l, leftType.Scale, leftType.Scale - scaleReduction);
            else
                r = ScaleHelper.Rescale256(r, rightType.Scale, rightType.Scale - scaleReduction);
        }

        return new Decimal256(l * r);
    }
}
