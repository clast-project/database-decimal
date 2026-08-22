// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Span-based batch modulus on raw mantissa arrays.
/// Both operands are rescaled to the result scale before integer modulus.
/// The result span may safely overlap with either input span.
/// </summary>
public static class SpanModulusKernel
{
    // ================================================================
    // Modulus — column % column, same width
    // ================================================================

    public static void Modulus(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<int> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = left[i] % right[i];
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = ScaleHelper.RescaleByDelta32(left[i], ld, rounding)
                    % ScaleHelper.RescaleByDelta32(right[i], rd, rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Modulus(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = left[i] % right[i];
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = ScaleHelper.RescaleByDelta64(left[i], ld, rounding)
                    % ScaleHelper.RescaleByDelta64(right[i], rd, rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Modulus(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = left[i] % right[i];
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = ScaleHelper.RescaleByDelta128(left[i], ld, rounding)
                    % ScaleHelper.RescaleByDelta128(right[i], rd, rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Modulus(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = left[i] % right[i];
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = ScaleHelper.RescaleByDelta256(left[i], ld, rounding)
                    % ScaleHelper.RescaleByDelta256(right[i], rd, rounding);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    // ================================================================
    // Modulus — column % column, widening
    // ================================================================

    public static void ModulusWiden(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        for (int i = 0; i < left.Length; i++)
            result[i] = ScaleHelper.WidenByDelta32To64(left[i], ld, rounding)
                % ScaleHelper.WidenByDelta32To64(right[i], rd, rounding);

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void ModulusWiden(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        for (int i = 0; i < left.Length; i++)
            result[i] = ScaleHelper.WidenByDelta64To128(left[i], ld, rounding)
                % ScaleHelper.WidenByDelta64To128(right[i], rd, rounding);

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void ModulusWiden(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        for (int i = 0; i < left.Length; i++)
            result[i] = ScaleHelper.WidenByDelta128To256(left[i], ld, rounding)
                % ScaleHelper.WidenByDelta128To256(right[i], rd, rounding);

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    // ================================================================
    // Modulus — column % column over a nullable column
    //
    // Same reasoning as the divide kernel: the value under a null slot is
    // undefined and in practice zero, so a dense pass over a nullable divisor
    // throws DivideByZeroException on rows nobody was going to read. These
    // overloads take the caller's combined validity bitmap and touch only the
    // rows whose bit is set, iterating the set bits rather than branching on
    // each one.
    // ================================================================

    /// <summary>
    /// Takes the modulus of two columns, skipping rows whose validity bit is
    /// clear and reporting per row which results exceed the result type's
    /// precision.
    /// </summary>
    /// <param name="left">Left operand mantissas.</param>
    /// <param name="leftType">Type of the left column.</param>
    /// <param name="right">Right operand mantissas. Only the valid rows are read.</param>
    /// <param name="rightType">Type of the right column.</param>
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
    /// <param name="rounding">
    /// How to break ties when an operand is rescaled to a smaller scale.
    /// </param>
    /// <returns>How many computed results were out of range.</returns>
    /// <exception cref="DivideByZeroException">A valid row had a zero divisor.</exception>
    public static int Modulus(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<int> result, DecimalType resultType,
        ReadOnlySpan<ulong> validity, Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        ReadOnlySpan<ulong> valid = MaskWords.PrepareIn(left.Length, validity, nameof(validity));
        Span<ulong> mask = MaskWords.PrepareOutForFullWrite(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out int lower, out int upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

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
                int value = ScaleHelper.RescaleByDelta32(left[i], ld, rounding)
                    % ScaleHelper.RescaleByDelta32(right[i], rd, rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    /// <inheritdoc cref="Modulus(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, ReadOnlySpan{ulong}, Span{ulong}, DecimalRounding)"/>
    public static int Modulus(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        ReadOnlySpan<ulong> validity, Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        ReadOnlySpan<ulong> valid = MaskWords.PrepareIn(left.Length, validity, nameof(validity));
        Span<ulong> mask = MaskWords.PrepareOutForFullWrite(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out long lower, out long upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

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
                long value = ScaleHelper.RescaleByDelta64(left[i], ld, rounding)
                    % ScaleHelper.RescaleByDelta64(right[i], rd, rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    /// <inheritdoc cref="Modulus(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, ReadOnlySpan{ulong}, Span{ulong}, DecimalRounding)"/>
    public static int Modulus(
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

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

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
                Int128 value = ScaleHelper.RescaleByDelta128(left[i], ld, rounding)
                    % ScaleHelper.RescaleByDelta128(right[i], rd, rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    /// <inheritdoc cref="Modulus(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, ReadOnlySpan{ulong}, Span{ulong}, DecimalRounding)"/>
    public static int Modulus(
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

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

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
                Int256 value = ScaleHelper.RescaleByDelta256(left[i], ld, rounding)
                    % ScaleHelper.RescaleByDelta256(right[i], rd, rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    /// <inheritdoc cref="Modulus(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, ReadOnlySpan{ulong}, Span{ulong}, DecimalRounding)"/>
    public static int ModulusWiden(
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

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

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
                long value = ScaleHelper.WidenByDelta32To64(left[i], ld, rounding)
                    % ScaleHelper.WidenByDelta32To64(right[i], rd, rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    /// <inheritdoc cref="Modulus(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, ReadOnlySpan{ulong}, Span{ulong}, DecimalRounding)"/>
    public static int ModulusWiden(
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

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

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
                Int128 value = ScaleHelper.WidenByDelta64To128(left[i], ld, rounding)
                    % ScaleHelper.WidenByDelta64To128(right[i], rd, rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    /// <inheritdoc cref="Modulus(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, ReadOnlySpan{ulong}, Span{ulong}, DecimalRounding)"/>
    public static int ModulusWiden(
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

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

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
                Int256 value = ScaleHelper.WidenByDelta128To256(left[i], ld, rounding)
                    % ScaleHelper.WidenByDelta128To256(right[i], rd, rounding);
                result[i] = value;
                if (value < lower || value > upper) { flags |= 1UL << bit; count++; }
            }
            mask[w] = flags;
        }
        return count;
    }

    // ================================================================
    // Modulus — column % scalar (broadcast)
    // ================================================================

    public static void Modulus(
        ReadOnlySpan<int> left, DecimalType leftType,
        int right, DecimalType rightType,
        Span<int> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, result.Length);

        int rescaledRight = ScaleHelper.Rescale32(right, rightType.Scale, resultType.Scale, rounding);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = left[i] % rescaledRight;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = ScaleHelper.RescaleByDelta32(left[i], ld, rounding) % rescaledRight;
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Modulus(
        ReadOnlySpan<long> left, DecimalType leftType,
        long right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, result.Length);

        long rescaledRight = ScaleHelper.Rescale64(right, rightType.Scale, resultType.Scale, rounding);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = left[i] % rescaledRight;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = ScaleHelper.RescaleByDelta64(left[i], ld, rounding) % rescaledRight;
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Modulus(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        Int128 right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, result.Length);

        Int128 rescaledRight = ScaleHelper.Rescale128(right, rightType.Scale, resultType.Scale, rounding);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = left[i] % rescaledRight;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = ScaleHelper.RescaleByDelta128(left[i], ld, rounding) % rescaledRight;
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Modulus(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        Int256 right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, result.Length);

        Int256 rescaledRight = ScaleHelper.Rescale256(right, rightType.Scale, resultType.Scale, rounding);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = left[i] % rescaledRight;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = ScaleHelper.RescaleByDelta256(left[i], ld, rounding) % rescaledRight;
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    // ================================================================
    // Helpers
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
