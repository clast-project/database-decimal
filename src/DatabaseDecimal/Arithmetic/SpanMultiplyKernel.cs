using System.Runtime.CompilerServices;
using DatabaseDecimal.Values;

namespace DatabaseDecimal.Arithmetic;

/// <summary>
/// Span-based batch multiplication on raw mantissa arrays.
/// Multiplication naturally widens (32→64, 64→128, 128→256).
/// The raw product scale (s1+s2) is adjusted to the result scale once
/// per call, not per element.
/// The result span may safely overlap with either input span (when types match).
/// </summary>
public static class SpanMultiplyKernel
{
    // ================================================================
    // Multiply — column * column, widening
    // ================================================================

    /// <summary>32×32 → 64 bit widening multiply.</summary>
    public static void Multiply(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = (long)left[i] * right[i];
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale64((long)left[i] * right[i], scaleDelta);
        }
    }

    /// <summary>64×64 → 128 bit widening multiply.</summary>
    public static void Multiply(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = (Int128)left[i] * right[i];
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale128((Int128)left[i] * right[i], scaleDelta);
        }
    }

    /// <summary>128×128 → 256 bit widening multiply via Int256.BigMul.</summary>
    public static void MultiplyWiden(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Int256.BigMul(left[i], right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale256(Int256.BigMul(left[i], right[i]), scaleDelta);
        }
    }

    // ================================================================
    // Multiply — column * column, same width (128-bit and 256-bit)
    // For 128-bit, pre-reduces scale of one operand to stay within range.
    // ================================================================

    /// <summary>128×128 → 128 bit multiply with pre-scale-reduction.</summary>
    public static void Multiply(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleReduction = rawScale - resultType.Scale;
        bool reduceLeft = leftType.Scale >= rightType.Scale;

        if (scaleReduction <= 0)
        {
            // No reduction needed (or need to scale up the product)
            int scaleDelta = resultType.Scale - rawScale;
            for (int i = 0; i < left.Length; i++)
            {
                Int128 product = checked(left[i] * right[i]);
                result[i] = scaleDelta != 0 ? Rescale128(product, scaleDelta) : product;
            }
        }
        else if (reduceLeft)
        {
            int newLeftScale = leftType.Scale - scaleReduction;
            for (int i = 0; i < left.Length; i++)
            {
                Int128 l = ScaleHelper.Rescale128(left[i], leftType.Scale, newLeftScale);
                result[i] = checked(l * right[i]);
            }
        }
        else
        {
            int newRightScale = rightType.Scale - scaleReduction;
            for (int i = 0; i < left.Length; i++)
            {
                Int128 r = ScaleHelper.Rescale128(right[i], rightType.Scale, newRightScale);
                result[i] = checked(left[i] * r);
            }
        }
    }

    /// <summary>256×256 → 256 bit multiply with pre-scale-reduction.</summary>
    public static void Multiply(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleReduction = rawScale - resultType.Scale;
        bool reduceLeft = leftType.Scale >= rightType.Scale;

        if (scaleReduction <= 0)
        {
            int scaleDelta = resultType.Scale - rawScale;
            for (int i = 0; i < left.Length; i++)
            {
                Int256 product = left[i] * right[i];
                result[i] = scaleDelta != 0 ? Rescale256(product, scaleDelta) : product;
            }
        }
        else if (reduceLeft)
        {
            int newLeftScale = leftType.Scale - scaleReduction;
            for (int i = 0; i < left.Length; i++)
            {
                Int256 l = ScaleHelper.Rescale256(left[i], leftType.Scale, newLeftScale);
                result[i] = l * right[i];
            }
        }
        else
        {
            int newRightScale = rightType.Scale - scaleReduction;
            for (int i = 0; i < left.Length; i++)
            {
                Int256 r = ScaleHelper.Rescale256(right[i], rightType.Scale, newRightScale);
                result[i] = left[i] * r;
            }
        }
    }

    // ================================================================
    // Multiply — column * scalar (broadcast)
    // ================================================================

    public static void Multiply(
        ReadOnlySpan<int> left, DecimalType leftType,
        int right, DecimalType rightType,
        Span<long> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        long wideRight = right;
        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = (long)left[i] * wideRight;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale64((long)left[i] * wideRight, scaleDelta);
        }
    }

    public static void Multiply(
        ReadOnlySpan<long> left, DecimalType leftType,
        long right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        Int128 wideRight = right;
        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = (Int128)left[i] * wideRight;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale128((Int128)left[i] * wideRight, scaleDelta);
        }
    }

    public static void Multiply(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        Int128 right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Int256.BigMul(left[i], right);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale256(Int256.BigMul(left[i], right), scaleDelta);
        }
    }

    // ================================================================
    // Helpers
    // ================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Rescale64(long value, int delta)
    {
        if (delta > 0) return checked(value * PowersOf10.Int64[delta]);
        return ScaleHelper.DivideRoundHalfEven(value, PowersOf10.Int64[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int128 Rescale128(Int128 value, int delta)
    {
        if (delta > 0) return checked(value * PowersOf10.Int128[delta]);
        return ScaleHelper.DivideRoundHalfEven(value, PowersOf10.Int128[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int256 Rescale256(Int256 value, int delta)
    {
        if (delta > 0) return checked(value * PowersOf10.Int256[delta]);
        return ScaleHelper.DivideRoundHalfEven(value, PowersOf10.Int256[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateLengths(int leftLen, int rightLen, int resultLen)
    {
        if (leftLen != rightLen)
            throw new ArgumentException("Input spans must have the same length.");
        if (resultLen < leftLen)
            throw new ArgumentException("Result span must be at least as long as input spans.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateLengths(int inputLen, int resultLen)
    {
        if (resultLen < inputLen)
            throw new ArgumentException("Result span must be at least as long as input span.");
    }
}
