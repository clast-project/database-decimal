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
