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
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            AddSameScale32(left, right, result);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta32(left[i], ld, rounding)
                    + ScaleHelper.RescaleByDelta32(right[i], rd, rounding));
        }
    }

    public static void Add(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            AddSameScale64(left, right, result);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta64(left[i], ld, rounding)
                    + ScaleHelper.RescaleByDelta64(right[i], rd, rounding));
        }
    }

    public static void Add(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
    }

    public static void Add(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
    }

    // ================================================================
    // Add — column + column, widening
    // ================================================================

    public static void AddWiden(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            AddWidenSameScale32To64(left, right, result);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.WidenByDelta32To64(left[i], ld, rounding)
                    + ScaleHelper.WidenByDelta32To64(right[i], rd, rounding));
        }
    }

    public static void AddWiden(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        for (int i = 0; i < left.Length; i++)
            result[i] = checked(ScaleHelper.WidenByDelta64To128(left[i], ld, rounding)
                + ScaleHelper.WidenByDelta64To128(right[i], rd, rounding));
    }

    public static void AddWiden(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        for (int i = 0; i < left.Length; i++)
            result[i] = checked(ScaleHelper.WidenByDelta128To256(left[i], ld, rounding)
                + ScaleHelper.WidenByDelta128To256(right[i], rd, rounding));
    }

    // ================================================================
    // Add — column + scalar (broadcast)
    // ================================================================

    public static void Add(
        ReadOnlySpan<int> left, DecimalType leftType,
        int right, DecimalType rightType,
        Span<int> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, result.Length);

        int rescaledRight = ScaleHelper.Rescale32(right, rightType.Scale, resultType.Scale, rounding);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            AddBroadcastSameScale32(left, rescaledRight, result);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta32(left[i], ld, rounding) + rescaledRight);
        }
    }

    public static void Add(
        ReadOnlySpan<long> left, DecimalType leftType,
        long right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, result.Length);

        long rescaledRight = ScaleHelper.Rescale64(right, rightType.Scale, resultType.Scale, rounding);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            AddBroadcastSameScale64(left, rescaledRight, result);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta64(left[i], ld, rounding) + rescaledRight);
        }
    }

    public static void Add(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        Int128 right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
    }

    public static void Add(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        Int256 right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
    }

    // ================================================================
    // Subtract — column - column, same width
    // ================================================================

    public static void Subtract(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<int> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            SubtractSameScale32(left, right, result);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta32(left[i], ld, rounding)
                    - ScaleHelper.RescaleByDelta32(right[i], rd, rounding));
        }
    }

    public static void Subtract(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        if (ld == 0 && rd == 0)
        {
            SubtractSameScale64(left, right, result);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta64(left[i], ld, rounding)
                    - ScaleHelper.RescaleByDelta64(right[i], rd, rounding));
        }
    }

    public static void Subtract(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
    }

    public static void Subtract(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
    }

    // ================================================================
    // Subtract — column - scalar (broadcast)
    // ================================================================

    public static void Subtract(
        ReadOnlySpan<int> left, DecimalType leftType,
        int right, DecimalType rightType,
        Span<int> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, result.Length);

        int rescaledRight = ScaleHelper.Rescale32(right, rightType.Scale, resultType.Scale, rounding);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            SubtractBroadcastColumnScalar32(left, rescaledRight, result);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta32(left[i], ld, rounding) - rescaledRight);
        }
    }

    public static void Subtract(
        ReadOnlySpan<long> left, DecimalType leftType,
        long right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, result.Length);

        long rescaledRight = ScaleHelper.Rescale64(right, rightType.Scale, resultType.Scale, rounding);
        int ld = resultType.Scale - leftType.Scale;

        if (ld == 0)
        {
            SubtractBroadcastColumnScalar64(left, rescaledRight, result);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = checked(ScaleHelper.RescaleByDelta64(left[i], ld, rounding) - rescaledRight);
        }
    }

    public static void Subtract(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        Int128 right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
    }

    public static void Subtract(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        Int256 right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
    }

    // ================================================================
    // Subtract — scalar - column (broadcast, non-commutative)
    // ================================================================

    public static void Subtract(
        int left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<int> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(right.Length, result.Length);

        int rescaledLeft = ScaleHelper.Rescale32(left, leftType.Scale, resultType.Scale, rounding);
        int rd = resultType.Scale - rightType.Scale;

        if (rd == 0)
        {
            SubtractBroadcastScalarColumn32(rescaledLeft, right, result);
        }
        else
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - ScaleHelper.RescaleByDelta32(right[i], rd, rounding));
        }
    }

    public static void Subtract(
        long left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(right.Length, result.Length);

        long rescaledLeft = ScaleHelper.Rescale64(left, leftType.Scale, resultType.Scale, rounding);
        int rd = resultType.Scale - rightType.Scale;

        if (rd == 0)
        {
            SubtractBroadcastScalarColumn64(rescaledLeft, right, result);
        }
        else
        {
            for (int i = 0; i < right.Length; i++)
                result[i] = checked(rescaledLeft - ScaleHelper.RescaleByDelta64(right[i], rd, rounding));
        }
    }

    public static void Subtract(
        Int128 left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
    }

    public static void Subtract(
        Int256 left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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

    private static void AddSameScale32(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> result)
    {
        int i = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            ReadOnlySpan<Vector<int>> rv = MemoryMarshal.Cast<int, Vector<int>>(right);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = lv.Length;
            Vector<int> overflow = Vector<int>.Zero;
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> b = rv[k];
                Vector<int> c = a + b;
                overflow |= (a ^ c) & (b ^ c);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < left.Length; i++)
            result[i] = checked(left[i] + right[i]);
    }

    private static void AddSameScale64(ReadOnlySpan<long> left, ReadOnlySpan<long> right, Span<long> result)
    {
        int i = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> lv = MemoryMarshal.Cast<long, Vector<long>>(left);
            ReadOnlySpan<Vector<long>> rv = MemoryMarshal.Cast<long, Vector<long>>(right);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            Vector<long> overflow = Vector<long>.Zero;
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> a = lv[k];
                Vector<long> b = rv[k];
                Vector<long> c = a + b;
                overflow |= (a ^ c) & (b ^ c);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            i = chunks * Vector<long>.Count;
        }
#endif
        for (; i < left.Length; i++)
            result[i] = checked(left[i] + right[i]);
    }

    private static void SubtractSameScale32(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> result)
    {
        int i = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            ReadOnlySpan<Vector<int>> rv = MemoryMarshal.Cast<int, Vector<int>>(right);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = lv.Length;
            Vector<int> overflow = Vector<int>.Zero;
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> b = rv[k];
                Vector<int> c = a - b;
                overflow |= (a ^ b) & (a ^ c);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < left.Length; i++)
            result[i] = checked(left[i] - right[i]);
    }

    private static void SubtractSameScale64(ReadOnlySpan<long> left, ReadOnlySpan<long> right, Span<long> result)
    {
        int i = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> lv = MemoryMarshal.Cast<long, Vector<long>>(left);
            ReadOnlySpan<Vector<long>> rv = MemoryMarshal.Cast<long, Vector<long>>(right);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            Vector<long> overflow = Vector<long>.Zero;
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> a = lv[k];
                Vector<long> b = rv[k];
                Vector<long> c = a - b;
                overflow |= (a ^ b) & (a ^ c);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            i = chunks * Vector<long>.Count;
        }
#endif
        for (; i < left.Length; i++)
            result[i] = checked(left[i] - right[i]);
    }

    private static void AddWidenSameScale32To64(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<long> result)
    {
        int i = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            ReadOnlySpan<Vector<int>> rv = MemoryMarshal.Cast<int, Vector<int>>(right);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> b = rv[k];
                Vector.Widen(a, out Vector<long> aLo, out Vector<long> aHi);
                Vector.Widen(b, out Vector<long> bLo, out Vector<long> bHi);
                ov[k * 2] = aLo + bLo;
                ov[k * 2 + 1] = aHi + bHi;
            }
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < left.Length; i++)
            result[i] = (long)left[i] + right[i];
    }

    private static void AddBroadcastSameScale32(ReadOnlySpan<int> left, int right, Span<int> result)
    {
        int i = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = lv.Length;
            Vector<int> bv = new Vector<int>(right);
            Vector<int> overflow = Vector<int>.Zero;
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> c = a + bv;
                overflow |= (a ^ c) & (bv ^ c);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < left.Length; i++)
            result[i] = checked(left[i] + right);
    }

    private static void AddBroadcastSameScale64(ReadOnlySpan<long> left, long right, Span<long> result)
    {
        int i = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> lv = MemoryMarshal.Cast<long, Vector<long>>(left);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            Vector<long> bv = new Vector<long>(right);
            Vector<long> overflow = Vector<long>.Zero;
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> a = lv[k];
                Vector<long> c = a + bv;
                overflow |= (a ^ c) & (bv ^ c);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            i = chunks * Vector<long>.Count;
        }
#endif
        for (; i < left.Length; i++)
            result[i] = checked(left[i] + right);
    }

    private static void SubtractBroadcastColumnScalar32(ReadOnlySpan<int> left, int right, Span<int> result)
    {
        int i = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> lv = MemoryMarshal.Cast<int, Vector<int>>(left);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = lv.Length;
            Vector<int> bv = new Vector<int>(right);
            Vector<int> overflow = Vector<int>.Zero;
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> a = lv[k];
                Vector<int> c = a - bv;
                overflow |= (a ^ bv) & (a ^ c);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < left.Length; i++)
            result[i] = checked(left[i] - right);
    }

    private static void SubtractBroadcastColumnScalar64(ReadOnlySpan<long> left, long right, Span<long> result)
    {
        int i = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && left.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> lv = MemoryMarshal.Cast<long, Vector<long>>(left);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = lv.Length;
            Vector<long> bv = new Vector<long>(right);
            Vector<long> overflow = Vector<long>.Zero;
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> a = lv[k];
                Vector<long> c = a - bv;
                overflow |= (a ^ bv) & (a ^ c);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            i = chunks * Vector<long>.Count;
        }
#endif
        for (; i < left.Length; i++)
            result[i] = checked(left[i] - right);
    }

    private static void SubtractBroadcastScalarColumn32(int left, ReadOnlySpan<int> right, Span<int> result)
    {
        int i = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && right.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> rv = MemoryMarshal.Cast<int, Vector<int>>(right);
            Span<Vector<int>> ov = MemoryMarshal.Cast<int, Vector<int>>(result);
            int chunks = rv.Length;
            Vector<int> av = new Vector<int>(left);
            Vector<int> overflow = Vector<int>.Zero;
            for (int k = 0; k < chunks; k++)
            {
                Vector<int> b = rv[k];
                Vector<int> c = av - b;
                overflow |= (av ^ b) & (av ^ c);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<int>.Zero))
                ThrowOverflow();
            i = chunks * Vector<int>.Count;
        }
#endif
        for (; i < right.Length; i++)
            result[i] = checked(left - right[i]);
    }

    private static void SubtractBroadcastScalarColumn64(long left, ReadOnlySpan<long> right, Span<long> result)
    {
        int i = 0;
#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && right.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> rv = MemoryMarshal.Cast<long, Vector<long>>(right);
            Span<Vector<long>> ov = MemoryMarshal.Cast<long, Vector<long>>(result);
            int chunks = rv.Length;
            Vector<long> av = new Vector<long>(left);
            Vector<long> overflow = Vector<long>.Zero;
            for (int k = 0; k < chunks; k++)
            {
                Vector<long> b = rv[k];
                Vector<long> c = av - b;
                overflow |= (av ^ b) & (av ^ c);
                ov[k] = c;
            }
            if (Vector.LessThanAny(overflow, Vector<long>.Zero))
                ThrowOverflow();
            i = chunks * Vector<long>.Count;
        }
#endif
        for (; i < right.Length; i++)
            result[i] = checked(left - right[i]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOverflow() => throw new OverflowException();
}
