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
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        long prescaleFactor = PowersOf10.Int64[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            long scaledDividend = checked((long)left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, right[i], rounding);
        }
    }

    /// <summary>64÷64 → 128 bit. Prescales dividend to 128-bit before dividing.</summary>
    public static void Divide(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 prescaleFactor = PowersOf10.Int128[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int128 scaledDividend = checked((Int128)left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, (Int128)right[i], rounding);
        }
    }

    /// <summary>128÷128 → 256 bit. Prescales dividend to 256-bit before dividing.</summary>
    public static void DivideWiden(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 prescaleFactor = PowersOf10.Int256[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int256 scaledDividend = checked((Int256)left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, (Int256)right[i], rounding);
        }
    }

    // ================================================================
    // Divide — column / column, same width (128-bit and 256-bit)
    // ================================================================

    /// <summary>128÷128 → 128 bit. Prescales within 128-bit.</summary>
    public static void Divide(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 prescaleFactor = PowersOf10.Int128[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int128 scaledDividend = checked(left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, right[i], rounding);
        }
    }

    /// <summary>256÷256 → 256 bit. Prescales within 256-bit.</summary>
    public static void Divide(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 prescaleFactor = PowersOf10.Int256[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int256 scaledDividend = checked(left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, right[i], rounding);
        }
    }

    // ================================================================
    // Divide — column / scalar (broadcast)
    // ================================================================

    public static void Divide(
        ReadOnlySpan<int> left, DecimalType leftType,
        int right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
    }

    public static void Divide(
        ReadOnlySpan<long> left, DecimalType leftType,
        long right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
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
    }

    public static void Divide(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        Int128 right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 prescaleFactor = PowersOf10.Int128[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int128 scaledDividend = checked(left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, right, rounding);
        }
    }

    public static void Divide(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        Int256 right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(left.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 prescaleFactor = PowersOf10.Int256[prescaleAmount];

        for (int i = 0; i < left.Length; i++)
        {
            Int256 scaledDividend = checked(left[i] * prescaleFactor);
            result[i] = ScaleHelper.DivideRound(scaledDividend, right, rounding);
        }
    }

    // ================================================================
    // Divide — scalar / column (broadcast, non-commutative)
    // ================================================================

    public static void Divide(
        int left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        long scaledLeft = checked((long)left * PowersOf10.Int64[prescaleAmount]);

        for (int i = 0; i < right.Length; i++)
            result[i] = ScaleHelper.DivideRound(scaledLeft, right[i], rounding);
    }

    public static void Divide(
        long left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 scaledLeft = checked((Int128)left * PowersOf10.Int128[prescaleAmount]);

        for (int i = 0; i < right.Length; i++)
            result[i] = ScaleHelper.DivideRound(scaledLeft, (Int128)right[i], rounding);
    }

    public static void Divide(
        Int128 left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int128 scaledLeft = checked(left * PowersOf10.Int128[prescaleAmount]);

        for (int i = 0; i < right.Length; i++)
            result[i] = ScaleHelper.DivideRound(scaledLeft, right[i], rounding);
    }

    public static void Divide(
        Int256 left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven)
    {
        ValidateLengths(right.Length, result.Length);

        int prescaleAmount = resultType.Scale - leftType.Scale + rightType.Scale;
        Int256 scaledLeft = checked(left * PowersOf10.Int256[prescaleAmount]);

        for (int i = 0; i < right.Length; i++)
            result[i] = ScaleHelper.DivideRound(scaledLeft, right[i], rounding);
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
