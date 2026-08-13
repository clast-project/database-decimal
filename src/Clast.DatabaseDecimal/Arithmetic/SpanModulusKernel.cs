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
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
                result[i] = Rescale32(left[i], ld, rounding) % Rescale32(right[i], rd, rounding);
        }
    }

    public static void Modulus(
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
            for (int i = 0; i < left.Length; i++)
                result[i] = left[i] % right[i];
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale64(left[i], ld, rounding) % Rescale64(right[i], rd, rounding);
        }
    }

    public static void Modulus(
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
                result[i] = left[i] % right[i];
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale128(left[i], ld, rounding) % Rescale128(right[i], rd, rounding);
        }
    }

    public static void Modulus(
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
                result[i] = left[i] % right[i];
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale256(left[i], ld, rounding) % Rescale256(right[i], rd, rounding);
        }
    }

    // ================================================================
    // Modulus — column % column, widening
    // ================================================================

    public static void ModulusWiden(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        for (int i = 0; i < left.Length; i++)
            result[i] = Widen32To64(left[i], ld, rounding) % Widen32To64(right[i], rd, rounding);
    }

    public static void ModulusWiden(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        for (int i = 0; i < left.Length; i++)
            result[i] = Widen64To128(left[i], ld, rounding) % Widen64To128(right[i], rd, rounding);
    }

    public static void ModulusWiden(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int ld = resultType.Scale - leftType.Scale;
        int rd = resultType.Scale - rightType.Scale;

        for (int i = 0; i < left.Length; i++)
            result[i] = Widen128To256(left[i], ld, rounding) % Widen128To256(right[i], rd, rounding);
    }

    // ================================================================
    // Modulus — column % scalar (broadcast)
    // ================================================================

    public static void Modulus(
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
            for (int i = 0; i < left.Length; i++)
                result[i] = left[i] % rescaledRight;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale32(left[i], ld, rounding) % rescaledRight;
        }
    }

    public static void Modulus(
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
            for (int i = 0; i < left.Length; i++)
                result[i] = left[i] % rescaledRight;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale64(left[i], ld, rounding) % rescaledRight;
        }
    }

    public static void Modulus(
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
                result[i] = left[i] % rescaledRight;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale128(left[i], ld, rounding) % rescaledRight;
        }
    }

    public static void Modulus(
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
                result[i] = left[i] % rescaledRight;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale256(left[i], ld, rounding) % rescaledRight;
        }
    }

    // ================================================================
    // Helpers
    // ================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Rescale32(int value, int delta, DecimalRounding rounding)
    {
        if (delta == 0) return value;
        if (delta > 0) return checked(value * PowersOf10.Int32[delta]);
        return ScaleHelper.DivideRound(value, PowersOf10.Int32[-delta], rounding);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Rescale64(long value, int delta, DecimalRounding rounding)
    {
        if (delta == 0) return value;
        if (delta > 0) return checked(value * PowersOf10.Int64[delta]);
        return ScaleHelper.DivideRound(value, PowersOf10.Int64[-delta], rounding);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int128 Rescale128(Int128 value, int delta, DecimalRounding rounding)
    {
        if (delta == 0) return value;
        if (delta > 0) return checked(value * PowersOf10.Int128[delta]);
        return ScaleHelper.DivideRound(value, PowersOf10.Int128[-delta], rounding);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int256 Rescale256(Int256 value, int delta, DecimalRounding rounding)
    {
        if (delta == 0) return value;
        if (delta > 0) return checked(value * PowersOf10.Int256[delta]);
        return ScaleHelper.DivideRound(value, PowersOf10.Int256[-delta], rounding);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Widen32To64(int value, int delta, DecimalRounding rounding)
    {
        long wide = value;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int64[delta]);
        return ScaleHelper.DivideRound(wide, PowersOf10.Int64[-delta], rounding);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int128 Widen64To128(long value, int delta, DecimalRounding rounding)
    {
        Int128 wide = value;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int128[delta]);
        return ScaleHelper.DivideRound(wide, PowersOf10.Int128[-delta], rounding);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int256 Widen128To256(Int128 value, int delta, DecimalRounding rounding)
    {
        Int256 wide = value;
        if (delta == 0) return wide;
        if (delta > 0) return checked(wide * PowersOf10.Int256[delta]);
        return ScaleHelper.DivideRound(wide, PowersOf10.Int256[-delta], rounding);
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
