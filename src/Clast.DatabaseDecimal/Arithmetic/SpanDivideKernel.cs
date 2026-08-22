// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Span-based batch division on raw mantissa arrays.
/// The dividend is pre-scaled to preserve fractional digits.
/// The prescale factor is computed once per call, not per element.
/// The result span may safely overlap with either input span (when types match).
/// </summary>
public static class SpanDivideKernel
{
    // ================================================================
    // Divide — column / column, widening
    // ================================================================

    /// <summary>32÷32 → 64 bit. Prescales dividend to 64-bit before dividing.</summary>
    public static void Divide(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        long prescaleFactor = PowersOf10.Int64[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            long scaledDividend = checked((long)left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, right[i], rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    /// <summary>64÷64 → 128 bit. Prescales dividend to 128-bit before dividing.</summary>
    public static void Divide(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 prescaleFactor = PowersOf10.Int128[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int128 scaledDividend = checked((Int128)left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, (Int128)right[i], rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    /// <summary>128÷128 → 256 bit. Prescales dividend to 256-bit before dividing.</summary>
    public static void DivideWiden(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 prescaleFactor = PowersOf10.Int256[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int256 scaledDividend = checked((Int256)left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, (Int256)right[i], rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    // ================================================================
    // Divide — column / column, same width (128-bit and 256-bit)
    // ================================================================

    /// <summary>128÷128 → 128 bit. Prescales within 128-bit.</summary>
    public static void Divide(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 prescaleFactor = PowersOf10.Int128[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int128 scaledDividend = checked(left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, right[i], rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    /// <summary>256÷256 → 256 bit. Prescales within 256-bit.</summary>
    public static void Divide(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 prescaleFactor = PowersOf10.Int256[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int256 scaledDividend = checked(left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, right[i], rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    // ================================================================
    // Divide — column / column over a nullable column
    //
    // Division is the one place where "compute the null rows too and mask the
    // results afterwards" is not merely wasteful but wrong: the value under a
    // null slot is undefined, and the value builders actually leave there is
    // zero, so a dense pass over a nullable divisor throws DivideByZeroException
    // on rows whose result was never going to be used. DecimalOverflow.Ignore
    // does not help — it relaxes the declared-precision check, never the width
    // check, and never the divisor.
    //
    // These overloads take the caller's combined validity bitmap (the AND of the
    // two operand bitmaps, which is what SQL null propagation calls for) and
    // touch only the rows whose bit is set. Rows whose bit is clear are left
    // exactly as they were in the result span.
    //
    // Skipping earns its keep here in a way it does not for addition: what it
    // avoids is a whole software division rather than one lane of a vector add.
    //
    // Set bits are iterated rather than branched on per element — a predicated
    // branch on the validity bit costs more in mispredictions than the
    // arithmetic it skips.
    // ================================================================

    /// <summary>
    /// Divides two columns, skipping rows whose validity bit is clear and
    /// reporting per row which results exceed the result type's precision.
    /// </summary>
    /// <param name="left">Dividend mantissas.</param>
    /// <param name="leftType">Type of the dividend column.</param>
    /// <param name="right">Divisor mantissas. Only the valid rows are read.</param>
    /// <param name="rightType">Type of the divisor column.</param>
    /// <param name="result">
    /// Receives the result mantissas. Rows whose validity bit is clear are left
    /// untouched, so the caller sees whatever the buffer already held.
    /// </param>
    /// <param name="resultType">Type the results must fit.</param>
    /// <param name="validity">
    /// One bit per element, set when the row is non-null in both operands. Must
    /// be at least <see cref="DecimalRange.MaskWordCount"/> words long; bits past
    /// the end of the span are ignored.
    /// </param>
    /// <param name="outOfRangeMask">
    /// Receives one bit per element — bit i is set when a computed
    /// <c>result[i]</c> does not fit <paramref name="resultType"/>. Bits for
    /// skipped rows are clear. Must be at least
    /// <see cref="DecimalRange.MaskWordCount"/> words long; the words covering
    /// the span are fully written, so it need not be cleared first.
    /// </param>
    /// <param name="rounding">How to break ties in the final division.</param>
    /// <returns>How many computed results were out of range.</returns>
    /// <exception cref="DivideByZeroException">A valid row had a zero divisor.</exception>
    /// <exception cref="OverflowException">A prescaled dividend overflowed the mantissa width.</exception>
    public static int Divide(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        ReadOnlySpan<ulong> validity, Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        ReadOnlySpan<ulong> valid = MaskWords.PrepareIn(left.Length, validity, nameof(validity));
        Span<ulong> mask = MaskWords.PrepareOutForFullWrite(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out long lower, out long upper);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        long prescaleFactor = PowersOf10.Int64[prescaleAmount];

        int count = 0;
        for (int w = 0; w < valid.Length; w++)
        {
            ulong bits = MaskWords.Live(valid[w], w, left.Length);
            int start = w << 6;
            ulong flags = 0;
            while (bits != 0)
            {
                int bit = MathCompat.TrailingZeroCount(bits);
                bits &= bits - 1;
                int i = start + bit;
                long scaledDividend = checked((long)left[i] * prescaleFactor);
                long value = ScaleHelper.DivideRound(scaledDividend, right[i], rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    /// <inheritdoc cref="Divide(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{long}, DecimalType, ReadOnlySpan{ulong}, Span{ulong}, DecimalRounding)"/>
    public static int Divide(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        ReadOnlySpan<ulong> validity, Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        ReadOnlySpan<ulong> valid = MaskWords.PrepareIn(left.Length, validity, nameof(validity));
        Span<ulong> mask = MaskWords.PrepareOutForFullWrite(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out Int128 lower, out Int128 upper);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 prescaleFactor = PowersOf10.Int128[prescaleAmount];

        int count = 0;
        for (int w = 0; w < valid.Length; w++)
        {
            ulong bits = MaskWords.Live(valid[w], w, left.Length);
            int start = w << 6;
            ulong flags = 0;
            while (bits != 0)
            {
                int bit = MathCompat.TrailingZeroCount(bits);
                bits &= bits - 1;
                int i = start + bit;
                Int128 scaledDividend = checked((Int128)left[i] * prescaleFactor);
                Int128 value = ScaleHelper.DivideRound(scaledDividend, (Int128)right[i], rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    /// <inheritdoc cref="Divide(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{long}, DecimalType, ReadOnlySpan{ulong}, Span{ulong}, DecimalRounding)"/>
    public static int DivideWiden(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        ReadOnlySpan<ulong> validity, Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        ReadOnlySpan<ulong> valid = MaskWords.PrepareIn(left.Length, validity, nameof(validity));
        Span<ulong> mask = MaskWords.PrepareOutForFullWrite(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out Int256 lower, out Int256 upper);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 prescaleFactor = PowersOf10.Int256[prescaleAmount];

        int count = 0;
        for (int w = 0; w < valid.Length; w++)
        {
            ulong bits = MaskWords.Live(valid[w], w, left.Length);
            int start = w << 6;
            ulong flags = 0;
            while (bits != 0)
            {
                int bit = MathCompat.TrailingZeroCount(bits);
                bits &= bits - 1;
                int i = start + bit;
                Int256 scaledDividend = checked((Int256)left[i] * prescaleFactor);
                Int256 value = ScaleHelper.DivideRound(scaledDividend, (Int256)right[i], rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    /// <inheritdoc cref="Divide(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{long}, DecimalType, ReadOnlySpan{ulong}, Span{ulong}, DecimalRounding)"/>
    public static int Divide(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        ReadOnlySpan<ulong> validity, Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        ReadOnlySpan<ulong> valid = MaskWords.PrepareIn(left.Length, validity, nameof(validity));
        Span<ulong> mask = MaskWords.PrepareOutForFullWrite(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out Int128 lower, out Int128 upper);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 prescaleFactor = PowersOf10.Int128[prescaleAmount];

        int count = 0;
        for (int w = 0; w < valid.Length; w++)
        {
            ulong bits = MaskWords.Live(valid[w], w, left.Length);
            int start = w << 6;
            ulong flags = 0;
            while (bits != 0)
            {
                int bit = MathCompat.TrailingZeroCount(bits);
                bits &= bits - 1;
                int i = start + bit;
                Int128 scaledDividend = checked(left[i] * prescaleFactor);
                Int128 value = ScaleHelper.DivideRound(scaledDividend, right[i], rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    /// <inheritdoc cref="Divide(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{long}, DecimalType, ReadOnlySpan{ulong}, Span{ulong}, DecimalRounding)"/>
    public static int Divide(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        ReadOnlySpan<ulong> validity, Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        ReadOnlySpan<ulong> valid = MaskWords.PrepareIn(left.Length, validity, nameof(validity));
        Span<ulong> mask = MaskWords.PrepareOutForFullWrite(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out Int256 lower, out Int256 upper);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 prescaleFactor = PowersOf10.Int256[prescaleAmount];

        int count = 0;
        for (int w = 0; w < valid.Length; w++)
        {
            ulong bits = MaskWords.Live(valid[w], w, left.Length);
            int start = w << 6;
            ulong flags = 0;
            while (bits != 0)
            {
                int bit = MathCompat.TrailingZeroCount(bits);
                bits &= bits - 1;
                int i = start + bit;
                Int256 scaledDividend = checked(left[i] * prescaleFactor);
                Int256 value = ScaleHelper.DivideRound(scaledDividend, right[i], rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    // ================================================================
    // Divide — column / scalar (broadcast)
    // ================================================================

    public static void Divide(
        ReadOnlySpan<int> left, DecimalType leftType,
        int right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        long prescaleFactor = PowersOf10.Int64[prescaleAmount];
        long wideRight = right;

        for (int i = 0; i < left.Length; i++)
        {
            long scaledDividend = checked((long)left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, wideRight, rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Divide(
        ReadOnlySpan<long> left, DecimalType leftType,
        long right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 prescaleFactor = PowersOf10.Int128[prescaleAmount];
        Int128 wideRight = right;

        for (int i = 0; i < left.Length; i++)
        {
            Int128 scaledDividend = checked((Int128)left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, wideRight, rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Divide(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        Int128 right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 prescaleFactor = PowersOf10.Int128[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int128 scaledDividend = checked(left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, right, rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Divide(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        Int256 right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 prescaleFactor = PowersOf10.Int256[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int256 scaledDividend = checked(left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, right, rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    // ================================================================
    // Divide — scalar / column (broadcast, non-commutative)
    // ================================================================

    public static void Divide(
        int left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        long scaledLeft = checked((long)left * PowersOf10.Int64[prescaleAmount]);

        for (int i = 0; i < right.Length; i++)
            result[i] = ScaleHelper.DivideRound(scaledLeft, right[i], rounding);

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, right.Length), resultType);
    }

    public static void Divide(
        long left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 scaledLeft = checked((Int128)left * PowersOf10.Int128[prescaleAmount]);

        for (int i = 0; i < right.Length; i++)
            result[i] = ScaleHelper.DivideRound(scaledLeft, (Int128)right[i], rounding);

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, right.Length), resultType);
    }

    public static void Divide(
        Int128 left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 scaledLeft = checked(left * PowersOf10.Int128[prescaleAmount]);

        for (int i = 0; i < right.Length; i++)
            result[i] = ScaleHelper.DivideRound(scaledLeft, right[i], rounding);

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, right.Length), resultType);
    }

    public static void Divide(
        Int256 left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 scaledLeft = checked(left * PowersOf10.Int256[prescaleAmount]);

        for (int i = 0; i < right.Length; i++)
            result[i] = ScaleHelper.DivideRound(scaledLeft, right[i], rounding);

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, right.Length), resultType);
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
