// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Arithmetic;

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
        Span<int> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            DecimalRange.GetBounds(resultType, out int lower, out int upper);
            if (AddSameScale32(left, right, result, lower, upper) && overflow == DecimalOverflow.Throw)
                DecimalRange.ThrowOutOfRange(resultType);
            return;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta32(left[i], ld, rounding)
                    + ScaleHelper.RescaleByDelta32(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Add(
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
            DecimalRange.GetBounds(resultType, out long lower, out long upper);
            if (AddSameScale64(left, right, result, lower, upper) && overflow == DecimalOverflow.Throw)
                DecimalRange.ThrowOutOfRange(resultType);
            return;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta64(left[i], ld, rounding)
                    + ScaleHelper.RescaleByDelta64(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Add(
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
                result[i] = checked(left[i] + right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta128(left[i], ld, rounding)
                    + ScaleHelper.RescaleByDelta128(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Add(
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
                result[i] = checked(left[i] + right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta256(left[i], ld, rounding)
                    + ScaleHelper.RescaleByDelta256(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    // ================================================================
    // Add — column + column, widening
    // ================================================================

    public static void AddWiden(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            DecimalRange.GetBounds(resultType, out long lower, out long upper);
            if (AddWidenSameScale32To64(left, right, result, lower, upper) && overflow == DecimalOverflow.Throw)
                DecimalRange.ThrowOutOfRange(resultType);
            return;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.WidenByDelta32To64(left[i], ld, rounding)
                    + ScaleHelper.WidenByDelta32To64(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void AddWiden(
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
            result[i] = checked(ScaleHelper.WidenByDelta64To128(left[i], ld, rounding)
                + ScaleHelper.WidenByDelta64To128(right[i], rd, rounding));

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void AddWiden(
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
            result[i] = checked(ScaleHelper.WidenByDelta128To256(left[i], ld, rounding)
                + ScaleHelper.WidenByDelta128To256(right[i], rd, rounding));

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    // ================================================================
    // Add — column + scalar (broadcast)
    // ================================================================

    public static void Add(
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
            DecimalRange.GetBounds(resultType, out int lower, out int upper);
            if (AddBroadcastSameScale32(left, rescaledRight, result, lower, upper) && overflow == DecimalOverflow.Throw)
                DecimalRange.ThrowOutOfRange(resultType);
            return;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta32(left[i], ld, rounding) + rescaledRight);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Add(
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
            DecimalRange.GetBounds(resultType, out long lower, out long upper);
            if (AddBroadcastSameScale64(left, rescaledRight, result, lower, upper) && overflow == DecimalOverflow.Throw)
                DecimalRange.ThrowOutOfRange(resultType);
            return;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta64(left[i], ld, rounding) + rescaledRight);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Add(
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
                result[i] = checked(left[i] + rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta128(left[i], ld, rounding) + rescaledRight);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Add(
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
                result[i] = checked(left[i] + rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta256(left[i], ld, rounding) + rescaledRight);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    // ================================================================
    // Subtract — column - column, same width
    // ================================================================

    public static void Subtract(
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
            DecimalRange.GetBounds(resultType, out int lower, out int upper);
            if (SubtractSameScale32(left, right, result, lower, upper) && overflow == DecimalOverflow.Throw)
                DecimalRange.ThrowOutOfRange(resultType);
            return;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta32(left[i], ld, rounding)
                    - ScaleHelper.RescaleByDelta32(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Subtract(
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
            DecimalRange.GetBounds(resultType, out long lower, out long upper);
            if (SubtractSameScale64(left, right, result, lower, upper) && overflow == DecimalOverflow.Throw)
                DecimalRange.ThrowOutOfRange(resultType);
            return;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta64(left[i], ld, rounding)
                    - ScaleHelper.RescaleByDelta64(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Subtract(
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
                result[i] = checked(left[i] - right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta128(left[i], ld, rounding)
                    - ScaleHelper.RescaleByDelta128(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Subtract(
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
                result[i] = checked(left[i] - right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta256(left[i], ld, rounding)
                    - ScaleHelper.RescaleByDelta256(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    // ================================================================
    // Subtract — column - scalar (broadcast)
    // ================================================================

    public static void Subtract(
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
            DecimalRange.GetBounds(resultType, out int lower, out int upper);
            if (SubtractBroadcastColumnScalar32(left, rescaledRight, result, lower, upper) && overflow == DecimalOverflow.Throw)
                DecimalRange.ThrowOutOfRange(resultType);
            return;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta32(left[i], ld, rounding) - rescaledRight);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Subtract(
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
            DecimalRange.GetBounds(resultType, out long lower, out long upper);
            if (SubtractBroadcastColumnScalar64(left, rescaledRight, result, lower, upper) && overflow == DecimalOverflow.Throw)
                DecimalRange.ThrowOutOfRange(resultType);
            return;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta64(left[i], ld, rounding) - rescaledRight);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Subtract(
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
                result[i] = checked(left[i] - rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta128(left[i], ld, rounding) - rescaledRight);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    public static void Subtract(
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
                result[i] = checked(left[i] - rescaledRight);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta256(left[i], ld, rounding) - rescaledRight);
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, left.Length), resultType);
    }

    // ================================================================
    // Subtract — scalar - column (broadcast, non-commutative)
    // ================================================================

    public static void Subtract(
        int left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<int> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(right.Length, result.Length);

        int rescaledLeft = ScaleHelper.Rescale32(left, leftType.Scale, resultType.Scale, rounding);
        int rd = resultType.Scale - rightType.Scale;

        if (rd == 0)
        {
            DecimalRange.GetBounds(resultType, out int lower, out int upper);
            if (SubtractBroadcastScalarColumn32(rescaledLeft, right, result, lower, upper) && overflow == DecimalOverflow.Throw)
                DecimalRange.ThrowOutOfRange(resultType);
            return;
        }
        else
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - ScaleHelper.RescaleByDelta32(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, right.Length), resultType);
    }

    public static void Subtract(
        long left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(right.Length, result.Length);

        long rescaledLeft = ScaleHelper.Rescale64(left, leftType.Scale, resultType.Scale, rounding);
        int rd = resultType.Scale - rightType.Scale;

        if (rd == 0)
        {
            DecimalRange.GetBounds(resultType, out long lower, out long upper);
            if (SubtractBroadcastScalarColumn64(rescaledLeft, right, result, lower, upper) && overflow == DecimalOverflow.Throw)
                DecimalRange.ThrowOutOfRange(resultType);
            return;
        }
        else
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - ScaleHelper.RescaleByDelta64(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, right.Length), resultType);
    }

    public static void Subtract(
        Int128 left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(right.Length, result.Length);

        Int128 rescaledLeft = ScaleHelper.Rescale128(left, leftType.Scale, resultType.Scale, rounding);
        int rd = resultType.Scale - rightType.Scale;

        if (rd == 0)
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - right[i]);
        }
        else
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - ScaleHelper.RescaleByDelta128(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, right.Length), resultType);
    }

    public static void Subtract(
        Int256 left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateLengths(right.Length, result.Length);

        Int256 rescaledLeft = ScaleHelper.Rescale256(left, leftType.Scale, resultType.Scale, rounding);
        int rd = resultType.Scale - rightType.Scale;

        if (rd == 0)
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - right[i]);
        }
        else
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - ScaleHelper.RescaleByDelta256(right[i], rd, rounding));
        }

        if (overflow == DecimalOverflow.Throw)
            DecimalRange.Validate(result.Slice(0, right.Length), resultType);
    }

    // ================================================================
    // Add and Subtract — result plus a per-row out-of-range mask
    //
    // The plain overloads either throw when a result busts the declared
    // precision or, under DecimalOverflow.Ignore, say nothing about which rows
    // did. A caller that nulls the offending rows instead of failing the query
    // needs the per-row answer, and getting it from WriteOutOfRangeMask costs a
    // second pass over output the arithmetic loop has just had in registers.
    //
    // These overloads fold that mask into the arithmetic pass and return how
    // many rows were flagged. Precision is never enforced by throwing here —
    // the mask is the report, so there is no DecimalOverflow argument. The
    // mantissa width is still checked, as it is everywhere else.
    // ================================================================

    /// <summary>
    /// Adds two columns, reporting per row which results exceed the result
    /// type's precision instead of throwing on the first one.
    /// </summary>
    /// <param name="left">Left operand mantissas.</param>
    /// <param name="leftType">Type of the left column.</param>
    /// <param name="right">Right operand mantissas.</param>
    /// <param name="rightType">Type of the right column.</param>
    /// <param name="result">Receives the result mantissas. May overlap either operand.</param>
    /// <param name="resultType">Type the results must fit.</param>
    /// <param name="outOfRangeMask">
    /// Receives one bit per element — bit i is set when <c>result[i]</c> does
    /// not fit <paramref name="resultType"/>. Must be at least
    /// <see cref="DecimalRange.MaskWordCount"/> words long; the words covering
    /// the span are fully written, so it need not be cleared first.
    /// </param>
    /// <param name="rounding">
    /// How to break ties when an operand is rescaled to a smaller scale. Under
    /// the standard promotion rules both operands scale upward and nothing is
    /// discarded, so this only matters for a caller-supplied result type.
    /// </param>
    /// <returns>How many results were out of range.</returns>
    /// <exception cref="OverflowException">A result overflowed the mantissa width.</exception>
    public static int Add(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<int> result, DecimalType resultType,
        Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        Span<ulong> mask = MaskWords.PrepareOut(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out int lower, out int upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
            return AddSameScale32Masked(left, right, result, lower, upper, mask);

        int count = 0;
        for (int i = 0; i < left.Length; i++)
        {
            int value = checked(ScaleHelper.RescaleByDelta32(left[i], ld, rounding)
                + ScaleHelper.RescaleByDelta32(right[i], rd, rounding));
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }
        return count;
    }

    /// <inheritdoc cref="Add(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, Span{ulong}, DecimalRounding)"/>
    public static int Add(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        Span<ulong> mask = MaskWords.PrepareOut(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out long lower, out long upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
            return AddSameScale64Masked(left, right, result, lower, upper, mask);

        int count = 0;
        for (int i = 0; i < left.Length; i++)
        {
            long value = checked(ScaleHelper.RescaleByDelta64(left[i], ld, rounding)
                + ScaleHelper.RescaleByDelta64(right[i], rd, rounding));
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }
        return count;
    }

    /// <inheritdoc cref="Add(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, Span{ulong}, DecimalRounding)"/>
    public static int Add(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        Span<ulong> mask = MaskWords.PrepareOut(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out Int128 lower, out Int128 upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;
        int count = 0;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
            {
                Int128 value = checked(left[i] + right[i]);
                result[i] = value;
                if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
            }
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
            {
                Int128 value = checked(ScaleHelper.RescaleByDelta128(left[i], ld, rounding)
                    + ScaleHelper.RescaleByDelta128(right[i], rd, rounding));
                result[i] = value;
                if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
            }
        }
        return count;
    }

    /// <inheritdoc cref="Add(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, Span{ulong}, DecimalRounding)"/>
    public static int Add(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        Span<ulong> mask = MaskWords.PrepareOut(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out Int256 lower, out Int256 upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;
        int count = 0;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
            {
                Int256 value = checked(left[i] + right[i]);
                result[i] = value;
                if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
            }
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
            {
                Int256 value = checked(ScaleHelper.RescaleByDelta256(left[i], ld, rounding)
                    + ScaleHelper.RescaleByDelta256(right[i], rd, rounding));
                result[i] = value;
                if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
            }
        }
        return count;
    }

    /// <inheritdoc cref="Add(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, Span{ulong}, DecimalRounding)"/>
    public static int AddWiden(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        Span<ulong> mask = MaskWords.PrepareOut(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out long lower, out long upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
            return AddWidenSameScale32To64Masked(left, right, result, lower, upper, mask);

        int count = 0;
        for (int i = 0; i < left.Length; i++)
        {
            long value = checked(ScaleHelper.WidenByDelta32To64(left[i], ld, rounding)
                + ScaleHelper.WidenByDelta32To64(right[i], rd, rounding));
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }
        return count;
    }

    /// <inheritdoc cref="Add(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, Span{ulong}, DecimalRounding)"/>
    public static int AddWiden(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        Span<ulong> mask = MaskWords.PrepareOut(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out Int128 lower, out Int128 upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        int count = 0;
        for (int i = 0; i < left.Length; i++)
        {
            Int128 value = checked(ScaleHelper.WidenByDelta64To128(left[i], ld, rounding)
                + ScaleHelper.WidenByDelta64To128(right[i], rd, rounding));
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }
        return count;
    }

    /// <inheritdoc cref="Add(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, Span{ulong}, DecimalRounding)"/>
    public static int AddWiden(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        Span<ulong> mask = MaskWords.PrepareOut(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out Int256 lower, out Int256 upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        int count = 0;
        for (int i = 0; i < left.Length; i++)
        {
            Int256 value = checked(ScaleHelper.WidenByDelta128To256(left[i], ld, rounding)
                + ScaleHelper.WidenByDelta128To256(right[i], rd, rounding));
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }
        return count;
    }

    /// <summary>
    /// Subtracts two columns, reporting per row which results exceed the result
    /// type's precision instead of throwing on the first one.
    /// </summary>
    /// <param name="left">Left operand mantissas.</param>
    /// <param name="leftType">Type of the left column.</param>
    /// <param name="right">Right operand mantissas.</param>
    /// <param name="rightType">Type of the right column.</param>
    /// <param name="result">Receives the result mantissas. May overlap either operand.</param>
    /// <param name="resultType">Type the results must fit.</param>
    /// <param name="outOfRangeMask">
    /// Receives one bit per element — bit i is set when <c>result[i]</c> does
    /// not fit <paramref name="resultType"/>. Must be at least
    /// <see cref="DecimalRange.MaskWordCount"/> words long; the words covering
    /// the span are fully written, so it need not be cleared first.
    /// </param>
    /// <param name="rounding">
    /// How to break ties when an operand is rescaled to a smaller scale. Under
    /// the standard promotion rules both operands scale upward and nothing is
    /// discarded, so this only matters for a caller-supplied result type.
    /// </param>
    /// <returns>How many results were out of range.</returns>
    /// <exception cref="OverflowException">A result overflowed the mantissa width.</exception>
    public static int Subtract(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<int> result, DecimalType resultType,
        Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        Span<ulong> mask = MaskWords.PrepareOut(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out int lower, out int upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
            return SubtractSameScale32Masked(left, right, result, lower, upper, mask);

        int count = 0;
        for (int i = 0; i < left.Length; i++)
        {
            int value = checked(ScaleHelper.RescaleByDelta32(left[i], ld, rounding)
                - ScaleHelper.RescaleByDelta32(right[i], rd, rounding));
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }
        return count;
    }

    /// <inheritdoc cref="Subtract(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, Span{ulong}, DecimalRounding)"/>
    public static int Subtract(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        Span<ulong> mask = MaskWords.PrepareOut(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out long lower, out long upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
            return SubtractSameScale64Masked(left, right, result, lower, upper, mask);

        int count = 0;
        for (int i = 0; i < left.Length; i++)
        {
            long value = checked(ScaleHelper.RescaleByDelta64(left[i], ld, rounding)
                - ScaleHelper.RescaleByDelta64(right[i], rd, rounding));
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }
        return count;
    }

    /// <inheritdoc cref="Subtract(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, Span{ulong}, DecimalRounding)"/>
    public static int Subtract(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        Span<ulong> mask = MaskWords.PrepareOut(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out Int128 lower, out Int128 upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;
        int count = 0;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
            {
                Int128 value = checked(left[i] - right[i]);
                result[i] = value;
                if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
            }
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
            {
                Int128 value = checked(ScaleHelper.RescaleByDelta128(left[i], ld, rounding)
                    - ScaleHelper.RescaleByDelta128(right[i], rd, rounding));
                result[i] = value;
                if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
            }
        }
        return count;
    }

    /// <inheritdoc cref="Subtract(ReadOnlySpan{int}, DecimalType, ReadOnlySpan{int}, DecimalType, Span{int}, DecimalType, Span{ulong}, DecimalRounding)"/>
    public static int Subtract(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        Span<ulong> outOfRangeMask,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);
        Span<ulong> mask = MaskWords.PrepareOut(left.Length, outOfRangeMask, nameof(outOfRangeMask));
        DecimalRange.GetBounds(resultType, out Int256 lower, out Int256 upper);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;
        int count = 0;

        if (ld == 0 && rd == 0)
        {
            for (int i = 0; i < left.Length; i++)
            {
                Int256 value = checked(left[i] - right[i]);
                result[i] = value;
                if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
            }
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
            {
                Int256 value = checked(ScaleHelper.RescaleByDelta256(left[i], ld, rounding)
                    - ScaleHelper.RescaleByDelta256(right[i], rd, rounding));
                result[i] = value;
                if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
            }
        }
        return count;
    }

    // ================================================================
    // Inline rescale helpers — delta and rounding are both loop-invariant,
    // so the branch predictor handles the per-element branches perfectly.
    // ================================================================

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

    // ================================================================
    // SIMD helpers for same-scale add/subtract on int and long mantissas,
    // and same-scale widening add (int -> long). Vector<T> arithmetic is
    // unchecked, so signed overflow is detected per-vector via the sign
    // bit of ((a XOR c) AND (b XOR c))  for add and ((a XOR b) AND (a XOR c))
    // for subtract. Any lane with the high bit set indicates overflow.
    // ================================================================

    private static bool AddSameScale32(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> result, int lower, int upper)
    {
        int i = 0;
        bool outOfRangeSeen = false;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            ReadOnlySpan<Vector<int>> rv = MemoryMarshal.Cast<int, Vector<int>>(right);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = lv.Length;
            Vector<int> overflow = Vector<int>.Zero;
            Vector<int> outOfRange = Vector<int>.Zero;
            Vector<int> loVec = new Vector<int>(lower);
            Vector<int> hiVec = new Vector<int>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> b = rv[k];
                Vector<int> c = a + b;
                overflow |= (a ^ c) & (b ^ c);
                outOfRange |= Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            outOfRangeSeen |= outOfRange != Vector<int>.Zero;
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] + right[i]);
            result[i] = value;
            outOfRangeSeen |= value < lower || value > upper;
        }

        return outOfRangeSeen;
    }

    private static bool AddSameScale64(ReadOnlySpan<long> left, ReadOnlySpan<long> right, Span<long> result, long lower, long upper)
    {
        int i = 0;
        bool outOfRangeSeen = false;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> lv = MemoryMarshal.Cast<long, Vector<long>>(left);
            ReadOnlySpan<Vector<long>> rv = MemoryMarshal.Cast<long, Vector<long>>(right);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            Vector<long> overflow = Vector<long>.Zero;
            Vector<long> outOfRange = Vector<long>.Zero;
            Vector<long> loVec = new Vector<long>(lower);
            Vector<long> hiVec = new Vector<long>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> a = lv[k];
                Vector<long> b = rv[k];
                Vector<long> c = a + b;
                overflow |= (a ^ c) & (b ^ c);
                outOfRange |= Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            outOfRangeSeen |= outOfRange != Vector<long>.Zero;
            i = chunks * Vector<long>.Count;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] + right[i]);
            result[i] = value;
            outOfRangeSeen |= value < lower || value > upper;
        }

        return outOfRangeSeen;
    }

    private static bool SubtractSameScale32(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> result, int lower, int upper)
    {
        int i = 0;
        bool outOfRangeSeen = false;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            ReadOnlySpan<Vector<int>> rv = MemoryMarshal.Cast<int, Vector<int>>(right);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = lv.Length;
            Vector<int> overflow = Vector<int>.Zero;
            Vector<int> outOfRange = Vector<int>.Zero;
            Vector<int> loVec = new Vector<int>(lower);
            Vector<int> hiVec = new Vector<int>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> b = rv[k];
                Vector<int> c = a - b;
                overflow |= (a ^ b) & (a ^ c);
                outOfRange |= Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            outOfRangeSeen |= outOfRange != Vector<int>.Zero;
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] - right[i]);
            result[i] = value;
            outOfRangeSeen |= value < lower || value > upper;
        }

        return outOfRangeSeen;
    }

    private static bool SubtractSameScale64(ReadOnlySpan<long> left, ReadOnlySpan<long> right, Span<long> result, long lower, long upper)
    {
        int i = 0;
        bool outOfRangeSeen = false;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> lv = MemoryMarshal.Cast<long, Vector<long>>(left);
            ReadOnlySpan<Vector<long>> rv = MemoryMarshal.Cast<long, Vector<long>>(right);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            Vector<long> overflow = Vector<long>.Zero;
            Vector<long> outOfRange = Vector<long>.Zero;
            Vector<long> loVec = new Vector<long>(lower);
            Vector<long> hiVec = new Vector<long>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> a = lv[k];
                Vector<long> b = rv[k];
                Vector<long> c = a - b;
                overflow |= (a ^ b) & (a ^ c);
                outOfRange |= Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            outOfRangeSeen |= outOfRange != Vector<long>.Zero;
            i = chunks * Vector<long>.Count;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] - right[i]);
            result[i] = value;
            outOfRangeSeen |= value < lower || value > upper;
        }

        return outOfRangeSeen;
    }

    private static bool AddWidenSameScale32To64(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<long> result, long lower, long upper)
    {
        int i = 0;
        bool outOfRangeSeen = false;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            ReadOnlySpan<Vector<int>> rv = MemoryMarshal.Cast<int, Vector<int>>(right);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            // Widening cannot overflow the width, so unlike its siblings this
            // loop carries no overflow accumulator — but the declared precision
            // still has to be enforced, on both halves of each widened pair.
            Vector<long> outOfRange = Vector<long>.Zero;
            Vector<long> loVec = new Vector<long>(lower);
            Vector<long> hiVec = new Vector<long>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> b = rv[k];
                Vector.Widen(a, out Vector<long> aLo, out Vector<long> aHi);
                Vector.Widen(b, out Vector<long> bLo, out Vector<long> bHi);
                Vector<long> low = aLo + bLo;
                Vector<long> high = aHi + bHi;
                outOfRange |= Vector.LessThan(low, loVec) | Vector.GreaterThan(low, hiVec);
                outOfRange |= Vector.LessThan(high, loVec) | Vector.GreaterThan(high, hiVec);
                ov[k * 2] = low;
                ov[k * 2 + 1] = high;
            }
            outOfRangeSeen |= outOfRange != Vector<long>.Zero;
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = (long)left[i] + right[i];
            result[i] = value;
            outOfRangeSeen |= value < lower || value > upper;
        }

        return outOfRangeSeen;
    }

    private static bool AddBroadcastSameScale32(ReadOnlySpan<int> left, int right, Span<int> result, int lower, int upper)
    {
        int i = 0;
        bool outOfRangeSeen = false;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = lv.Length;
            Vector<int> bv = new Vector<int>(right);
            Vector<int> overflow = Vector<int>.Zero;
            Vector<int> outOfRange = Vector<int>.Zero;
            Vector<int> loVec = new Vector<int>(lower);
            Vector<int> hiVec = new Vector<int>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> c = a + bv;
                overflow |= (a ^ c) & (bv ^ c);
                outOfRange |= Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            outOfRangeSeen |= outOfRange != Vector<int>.Zero;
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] + right);
            result[i] = value;
            outOfRangeSeen |= value < lower || value > upper;
        }

        return outOfRangeSeen;
    }

    private static bool AddBroadcastSameScale64(ReadOnlySpan<long> left, long right, Span<long> result, long lower, long upper)
    {
        int i = 0;
        bool outOfRangeSeen = false;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> lv = MemoryMarshal.Cast<long, Vector<long>>(left);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            Vector<long> bv = new Vector<long>(right);
            Vector<long> overflow = Vector<long>.Zero;
            Vector<long> outOfRange = Vector<long>.Zero;
            Vector<long> loVec = new Vector<long>(lower);
            Vector<long> hiVec = new Vector<long>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> a = lv[k];
                Vector<long> c = a + bv;
                overflow |= (a ^ c) & (bv ^ c);
                outOfRange |= Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            outOfRangeSeen |= outOfRange != Vector<long>.Zero;
            i = chunks * Vector<long>.Count;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] + right);
            result[i] = value;
            outOfRangeSeen |= value < lower || value > upper;
        }

        return outOfRangeSeen;
    }

    private static bool SubtractBroadcastColumnScalar32(ReadOnlySpan<int> left, int right, Span<int> result, int lower, int upper)
    {
        int i = 0;
        bool outOfRangeSeen = false;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = lv.Length;
            Vector<int> bv = new Vector<int>(right);
            Vector<int> overflow = Vector<int>.Zero;
            Vector<int> outOfRange = Vector<int>.Zero;
            Vector<int> loVec = new Vector<int>(lower);
            Vector<int> hiVec = new Vector<int>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> c = a - bv;
                overflow |= (a ^ bv) & (a ^ c);
                outOfRange |= Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            outOfRangeSeen |= outOfRange != Vector<int>.Zero;
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] - right);
            result[i] = value;
            outOfRangeSeen |= value < lower || value > upper;
        }

        return outOfRangeSeen;
    }

    private static bool SubtractBroadcastColumnScalar64(ReadOnlySpan<long> left, long right, Span<long> result, long lower, long upper)
    {
        int i = 0;
        bool outOfRangeSeen = false;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> lv = MemoryMarshal.Cast<long, Vector<long>>(left);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            Vector<long> bv = new Vector<long>(right);
            Vector<long> overflow = Vector<long>.Zero;
            Vector<long> outOfRange = Vector<long>.Zero;
            Vector<long> loVec = new Vector<long>(lower);
            Vector<long> hiVec = new Vector<long>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> a = lv[k];
                Vector<long> c = a - bv;
                overflow |= (a ^ bv) & (a ^ c);
                outOfRange |= Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            outOfRangeSeen |= outOfRange != Vector<long>.Zero;
            i = chunks * Vector<long>.Count;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] - right);
            result[i] = value;
            outOfRangeSeen |= value < lower || value > upper;
        }

        return outOfRangeSeen;
    }

    private static bool SubtractBroadcastScalarColumn32(int left, ReadOnlySpan<int> right, Span<int> result, int lower, int upper)
    {
        int i = 0;
        bool outOfRangeSeen = false;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && right.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> rv = MemoryMarshal.Cast<int, Vector<int>>(right);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = rv.Length;
            Vector<int> av = new Vector<int>(left);
            Vector<int> overflow = Vector<int>.Zero;
            Vector<int> outOfRange = Vector<int>.Zero;
            Vector<int> loVec = new Vector<int>(lower);
            Vector<int> hiVec = new Vector<int>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> b = rv[k];
                Vector<int> c = av - b;
                overflow |= (av ^ b) & (av ^ c);
                outOfRange |= Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            outOfRangeSeen |= outOfRange != Vector<int>.Zero;
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < right.Length; i++)
        {
            var value = checked(left - right[i]);
            result[i] = value;
            outOfRangeSeen |= value < lower || value > upper;
        }

        return outOfRangeSeen;
    }

    private static bool SubtractBroadcastScalarColumn64(long left, ReadOnlySpan<long> right, Span<long> result, long lower, long upper)
    {
        int i = 0;
        bool outOfRangeSeen = false;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && right.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> rv = MemoryMarshal.Cast<long, Vector<long>>(right);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = rv.Length;
            Vector<long> av = new Vector<long>(left);
            Vector<long> overflow = Vector<long>.Zero;
            Vector<long> outOfRange = Vector<long>.Zero;
            Vector<long> loVec = new Vector<long>(lower);
            Vector<long> hiVec = new Vector<long>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> b = rv[k];
                Vector<long> c = av - b;
                overflow |= (av ^ b) & (av ^ c);
                outOfRange |= Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            outOfRangeSeen |= outOfRange != Vector<long>.Zero;
            i = chunks * Vector<long>.Count;
        }
#endif
        for (; i < right.Length; i++)
        {
            var value = checked(left - right[i]);
            result[i] = value;
            outOfRangeSeen |= value < lower || value > upper;
        }

        return outOfRangeSeen;
    }

    // ================================================================
    // SIMD helpers that report which lanes left the declared precision, rather
    // than only whether any did. Same arithmetic and the same width-overflow
    // detection as their bool-returning siblings above; the difference is the
    // mask, which costs nothing until a lane actually trips.
    // ================================================================

#if NET5_0_OR_GREATER
    /// <summary>
    /// Records a comparison result's set lanes in <paramref name="mask"/> and
    /// returns how many there were.
    /// </summary>
    /// <remarks>
    /// <c>Vector&lt;T&gt;</c> has no <c>ExtractMostSignificantBits</c> — only the
    /// fixed-width <c>Vector128/256&lt;T&gt;</c> do — so the lanes are read one
    /// at a time. Callers guard the call on some lane being set, and a result
    /// outside the declared precision is rare by design, so this stays off the
    /// hot path.
    /// <para>
    /// A chunk never straddles a mask word: 64 is a multiple of every
    /// <c>Vector&lt;T&gt;.Count</c>, and chunks start at multiples of it.
    /// </para>
    /// </remarks>
    private static int FlagLanes32(Vector<int> bad, int firstIndex, Span<ulong> mask)
    {
        ulong bits = 0;
        int count = 0;
        for (int e = 0; e < Vector<int>.Count; e++)
            if (bad[e] != 0) { bits |= 1UL << e; count++; }
        mask[firstIndex >> 6] |= bits << (firstIndex & 63);
        return count;
    }

    /// <inheritdoc cref="FlagLanes32"/>
    private static int FlagLanes64(Vector<long> bad, int firstIndex, Span<ulong> mask)
    {
        ulong bits = 0;
        int count = 0;
        for (int e = 0; e < Vector<long>.Count; e++)
            if (bad[e] != 0L) { bits |= 1UL << e; count++; }
        mask[firstIndex >> 6] |= bits << (firstIndex & 63);
        return count;
    }
#endif

    private static int AddSameScale32Masked(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> result, int lower, int upper, Span<ulong> mask)
    {
        int i = 0;
        int count = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            ReadOnlySpan<Vector<int>> rv = MemoryMarshal.Cast<int, Vector<int>>(right);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = lv.Length;
            int lanes = Vector<int>.Count;
            Vector<int> overflow = Vector<int>.Zero;
            Vector<int> loVec = new Vector<int>(lower);
            Vector<int> hiVec = new Vector<int>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> b = rv[k];
                Vector<int> c = a + b;
                overflow |= (a ^ c) & (b ^ c);
                Vector<int> bad = Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
                if (bad != Vector<int>.Zero)
                    count += FlagLanes32(bad, k * lanes, mask);
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            i = chunks * lanes;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] + right[i]);
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }

        return count;
    }

    private static int AddSameScale64Masked(ReadOnlySpan<long> left, ReadOnlySpan<long> right, Span<long> result, long lower, long upper, Span<ulong> mask)
    {
        int i = 0;
        int count = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> lv = MemoryMarshal.Cast<long, Vector<long>>(left);
            ReadOnlySpan<Vector<long>> rv = MemoryMarshal.Cast<long, Vector<long>>(right);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            int lanes = Vector<long>.Count;
            Vector<long> overflow = Vector<long>.Zero;
            Vector<long> loVec = new Vector<long>(lower);
            Vector<long> hiVec = new Vector<long>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> a = lv[k];
                Vector<long> b = rv[k];
                Vector<long> c = a + b;
                overflow |= (a ^ c) & (b ^ c);
                Vector<long> bad = Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
                if (bad != Vector<long>.Zero)
                    count += FlagLanes64(bad, k * lanes, mask);
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            i = chunks * lanes;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] + right[i]);
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }

        return count;
    }

    private static int SubtractSameScale32Masked(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> result, int lower, int upper, Span<ulong> mask)
    {
        int i = 0;
        int count = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            ReadOnlySpan<Vector<int>> rv = MemoryMarshal.Cast<int, Vector<int>>(right);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = lv.Length;
            int lanes = Vector<int>.Count;
            Vector<int> overflow = Vector<int>.Zero;
            Vector<int> loVec = new Vector<int>(lower);
            Vector<int> hiVec = new Vector<int>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> b = rv[k];
                Vector<int> c = a - b;
                overflow |= (a ^ b) & (a ^ c);
                Vector<int> bad = Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
                if (bad != Vector<int>.Zero)
                    count += FlagLanes32(bad, k * lanes, mask);
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            i = chunks * lanes;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] - right[i]);
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }

        return count;
    }

    private static int SubtractSameScale64Masked(ReadOnlySpan<long> left, ReadOnlySpan<long> right, Span<long> result, long lower, long upper, Span<ulong> mask)
    {
        int i = 0;
        int count = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> lv = MemoryMarshal.Cast<long, Vector<long>>(left);
            ReadOnlySpan<Vector<long>> rv = MemoryMarshal.Cast<long, Vector<long>>(right);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            int lanes = Vector<long>.Count;
            Vector<long> overflow = Vector<long>.Zero;
            Vector<long> loVec = new Vector<long>(lower);
            Vector<long> hiVec = new Vector<long>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> a = lv[k];
                Vector<long> b = rv[k];
                Vector<long> c = a - b;
                overflow |= (a ^ b) & (a ^ c);
                Vector<long> bad = Vector.LessThan(c, loVec) | Vector.GreaterThan(c, hiVec);
                ov[k] = c;
                if (bad != Vector<long>.Zero)
                    count += FlagLanes64(bad, k * lanes, mask);
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            i = chunks * lanes;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = checked(left[i] - right[i]);
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }

        return count;
    }

    private static int AddWidenSameScale32To64Masked(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<long> result, long lower, long upper, Span<ulong> mask)
    {
        int i = 0;
        int count = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            ReadOnlySpan<Vector<int>> rv = MemoryMarshal.Cast<int, Vector<int>>(right);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            // Widening cannot overflow the width, so as in the bool-returning
            // sibling there is no overflow accumulator — but the declared
            // precision still has to be reported, on both halves of each pair.
            int halfLanes = Vector<long>.Count;
            Vector<long> loVec = new Vector<long>(lower);
            Vector<long> hiVec = new Vector<long>(upper);
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> b = rv[k];
                Vector.Widen(a, out Vector<long> aLo, out Vector<long> aHi);
                Vector.Widen(b, out Vector<long> bLo, out Vector<long> bHi);
                Vector<long> low = aLo + bLo;
                Vector<long> high = aHi + bHi;
                Vector<long> badLow = Vector.LessThan(low, loVec) | Vector.GreaterThan(low, hiVec);
                Vector<long> badHigh = Vector.LessThan(high, loVec) | Vector.GreaterThan(high, hiVec);
                ov[k * 2] = low;
                ov[k * 2 + 1] = high;
                int b0 = k * Vector<int>.Count;
                if (badLow != Vector<long>.Zero)
                    count += FlagLanes64(badLow, b0, mask);
                if (badHigh != Vector<long>.Zero)
                    count += FlagLanes64(badHigh, b0 + halfLanes, mask);
            }
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < left.Length; i++)
        {
            var value = (long)left[i] + right[i];
            result[i] = value;
            if (value < lower || value > upper) { mask[i >> 6] |= 1UL << (i & 63); count++; }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOverflow() => throw new OverflowException();
}
