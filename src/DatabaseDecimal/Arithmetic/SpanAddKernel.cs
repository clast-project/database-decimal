using System.Runtime.CompilerServices;
using DatabaseDecimal.Values;

namespace DatabaseDecimal.Arithmetic;

/// <summary>
/// Span-based batch addition and subtraction on raw mantissa arrays.
/// All values in a span share the same DecimalType. Rescale factors
/// are pre-computed once before the loop.
/// The result span may safely overlap with either input span.
/// </summary>
public static class SpanAddKernel
{
    // ================================================================
    // Add — column + column, same width
    // ================================================================

    public static void Add(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<int> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] + right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale32(left[i], ld) + Rescale32(right[i], rd));
        }
    }

    public static void Add(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<long> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] + right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale64(left[i], ld) + Rescale64(right[i], rd));
        }
    }

    public static void Add(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] + right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale128(left[i], ld) + Rescale128(right[i], rd));
        }
    }

    public static void Add(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] + right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale256(left[i], ld) + Rescale256(right[i], rd));
        }
    }

    // ================================================================
    // Add — column + column, widening
    // ================================================================

    public static void AddWiden(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        for (int i = 0; i < left.Length; i++)
            result[i] = checked(Widen32To64(left[i], ld) + Widen32To64(right[i], rd));
    }

    public static void AddWiden(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        for (int i = 0; i < left.Length; i++)
            result[i] = checked(Widen64To128(left[i], ld) + Widen64To128(right[i], rd));
    }

    public static void AddWiden(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        for (int i = 0; i < left.Length; i++)
            result[i] = checked(Widen128To256(left[i], ld) + Widen128To256(right[i], rd));
    }

    // ================================================================
    // Add — column + scalar (broadcast)
    // ================================================================

    public static void Add(
        ReadOnlySpan<int> left, DecimalType leftType,
        int right, DecimalType rightType,
        Span<int> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        int rescaledRight = ScaleHelper.Rescale32(right, rightType.Scale, resultType.Scale);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] + rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale32(left[i], ld) + rescaledRight);
        }
    }

    public static void Add(
        ReadOnlySpan<long> left, DecimalType leftType,
        long right, DecimalType rightType,
        Span<long> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        long rescaledRight = ScaleHelper.Rescale64(right, rightType.Scale, resultType.Scale);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] + rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale64(left[i], ld) + rescaledRight);
        }
    }

    public static void Add(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        Int128 right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        Int128 rescaledRight = ScaleHelper.Rescale128(right, rightType.Scale, resultType.Scale);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] + rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale128(left[i], ld) + rescaledRight);
        }
    }

    public static void Add(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        Int256 right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        Int256 rescaledRight = ScaleHelper.Rescale256(right, rightType.Scale, resultType.Scale);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] + rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale256(left[i], ld) + rescaledRight);
        }
    }

    // ================================================================
    // Subtract — column - column, same width
    // ================================================================

    public static void Subtract(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<int> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] - right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale32(left[i], ld) - Rescale32(right[i], rd));
        }
    }

    public static void Subtract(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<long> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] - right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale64(left[i], ld) - Rescale64(right[i], rd));
        }
    }

    public static void Subtract(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] - right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale128(left[i], ld) - Rescale128(right[i], rd));
        }
    }

    public static void Subtract(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] - right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale256(left[i], ld) - Rescale256(right[i], rd));
        }
    }

    // ================================================================
    // Subtract — column - scalar (broadcast)
    // ================================================================

    public static void Subtract(
        ReadOnlySpan<int> left, DecimalType leftType,
        int right, DecimalType rightType,
        Span<int> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        int rescaledRight = ScaleHelper.Rescale32(right, rightType.Scale, resultType.Scale);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] - rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale32(left[i], ld) - rescaledRight);
        }
    }

    public static void Subtract(
        ReadOnlySpan<long> left, DecimalType leftType,
        long right, DecimalType rightType,
        Span<long> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        long rescaledRight = ScaleHelper.Rescale64(right, rightType.Scale, resultType.Scale);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] - rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale64(left[i], ld) - rescaledRight);
        }
    }

    public static void Subtract(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        Int128 right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        Int128 rescaledRight = ScaleHelper.Rescale128(right, rightType.Scale, resultType.Scale);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] - rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale128(left[i], ld) - rescaledRight);
        }
    }

    public static void Subtract(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        Int256 right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        Int256 rescaledRight = ScaleHelper.Rescale256(right, rightType.Scale, resultType.Scale);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(left[i] - rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(Rescale256(left[i], ld) - rescaledRight);
        }
    }

    // ================================================================
    // Subtract — scalar - column (broadcast, non-commutative)
    // ================================================================

    public static void Subtract(
        int left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<int> result, DecimalType resultType)
    {
        ValidateLengths(right.Length, result.Length);

        int rescaledLeft = ScaleHelper.Rescale32(left, leftType.Scale, resultType.Scale);
        int rd = resultType.Scale - rightType.Scale;

        if (rd == 0)
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - right[i]);
        }
        else
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - Rescale32(right[i], rd));
        }
    }

    public static void Subtract(
        long left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<long> result, DecimalType resultType)
    {
        ValidateLengths(right.Length, result.Length);

        long rescaledLeft = ScaleHelper.Rescale64(left, leftType.Scale, resultType.Scale);
        int rd = resultType.Scale - rightType.Scale;

        if (rd == 0)
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - right[i]);
        }
        else
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - Rescale64(right[i], rd));
        }
    }

    public static void Subtract(
        Int128 left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(right.Length, result.Length);

        Int128 rescaledLeft = ScaleHelper.Rescale128(left, leftType.Scale, resultType.Scale);
        int rd = resultType.Scale - rightType.Scale;

        if (rd == 0)
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - right[i]);
        }
        else
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - Rescale128(right[i], rd));
        }
    }

    public static void Subtract(
        Int256 left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(right.Length, result.Length);

        Int256 rescaledLeft = ScaleHelper.Rescale256(left, leftType.Scale, resultType.Scale);
        int rd = resultType.Scale - rightType.Scale;

        if (rd == 0)
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - right[i]);
        }
        else
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - Rescale256(right[i], rd));
        }
    }

    // ================================================================
    // Inline rescale helpers — delta is loop-invariant, so the branch
    // predictor handles the per-element branch perfectly.
    // ================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Rescale32(int value, int delta)
    {
        if (delta == 0) return value;
        if (delta > 0) return checked(value * PowersOf10.Int32[delta]);
        return ScaleHelper.DivideRoundHalfEven(value, PowersOf10.Int32[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Rescale64(long value, int delta)
    {
        if (delta == 0) return value;
        if (delta > 0) return checked(value * PowersOf10.Int64[delta]);
        return ScaleHelper.DivideRoundHalfEven(value, PowersOf10.Int64[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int128 Rescale128(Int128 value, int delta)
    {
        if (delta == 0) return value;
        if (delta > 0) return checked(value * PowersOf10.Int128[delta]);
        return ScaleHelper.DivideRoundHalfEven(value, PowersOf10.Int128[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int256 Rescale256(Int256 value, int delta)
    {
        if (delta == 0) return value;
        if (delta > 0) return checked(value * PowersOf10.Int256[delta]);
        return ScaleHelper.DivideRoundHalfEven(value, PowersOf10.Int256[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Widen32To64(int value, int delta)
    {
        long wide = value;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int64[delta]);
        return ScaleHelper.DivideRoundHalfEven(wide, PowersOf10.Int64[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int128 Widen64To128(long value, int delta)
    {
        Int128 wide = value;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int128[delta]);
        return ScaleHelper.DivideRoundHalfEven(wide, PowersOf10.Int128[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int256 Widen128To256(Int128 value, int delta)
    {
        Int256 wide = value;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int256[delta]);
        return ScaleHelper.DivideRoundHalfEven(wide, PowersOf10.Int256[-delta]);
    }

    // ================================================================
    // Validation
    // ================================================================

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
