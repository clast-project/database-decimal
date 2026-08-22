// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Arithmetic;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// The 32- and 64-bit same-scale paths fold the result-precision check into the
/// vectorised arithmetic rather than making a second pass over the output.
/// </summary>
/// <remarks>
/// That loop has two halves — a vector body and a scalar tail for the elements
/// that do not fill a final vector — and the bound has to be enforced in both.
/// A length of 65 puts index 64 in the tail for every plausible
/// <c>Vector&lt;int&gt;.Count</c> (4, 8, or 16, all of which divide 64) while
/// index 0 is always inside the vector body.
/// </remarks>
public class FusedRangeCheckTests
{
    private static readonly DecimalType T9 = DecimalType.Numeric(9, 0);
    private static readonly DecimalType T18 = DecimalType.Numeric(18, 0);
    private const int Max9 = 999_999_999;
    private const long Max18 = 999_999_999_999_999_999L;

    private const int Length = 65;
    private const int InVectorBody = 0;
    private const int InScalarTail = 64;

    // ================================================================
    // 32-bit
    // ================================================================

    [Theory]
    [InlineData(InVectorBody)]
    [InlineData(InScalarTail)]
    public void Add32_PastPrecision_Throws(int index)
    {
        int[] left = new int[Length];
        int[] right = new int[Length];
        left[index] = Max9;
        right[index] = 1;

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Add(left, T9, right, T9, new int[Length], T9));
    }

    [Theory]
    [InlineData(InVectorBody)]
    [InlineData(InScalarTail)]
    public void Add32_Ignore_WritesEverythingAndDoesNotThrow(int index)
    {
        int[] left = new int[Length];
        int[] right = new int[Length];
        left[index] = Max9;
        right[index] = 1;
        int[] result = new int[Length];

        SpanAddKernel.Add(left, T9, right, T9, result, T9,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);

        Assert.Equal(1_000_000_000, result[index]);
        for (int i = 0; i < Length; i++)
            if (i != index) Assert.Equal(0, result[i]);
    }

    [Theory]
    [InlineData(InVectorBody)]
    [InlineData(InScalarTail)]
    public void Subtract32_PastPrecision_Throws(int index)
    {
        int[] left = new int[Length];
        int[] right = new int[Length];
        left[index] = -Max9;
        right[index] = 1;

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Subtract(left, T9, right, T9, new int[Length], T9));
    }

    [Theory]
    [InlineData(InVectorBody)]
    [InlineData(InScalarTail)]
    public void Add32_Broadcast_PastPrecision_Throws(int index)
    {
        int[] left = new int[Length];
        left[index] = Max9;

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Add(left, T9, 1, T9, new int[Length], T9));
    }

    [Theory]
    [InlineData(InVectorBody)]
    [InlineData(InScalarTail)]
    public void Subtract32_BroadcastScalarColumn_PastPrecision_Throws(int index)
    {
        int[] right = new int[Length];
        right[index] = -Max9;

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Subtract(1, T9, right, T9, new int[Length], T9));
    }

    // ================================================================
    // 64-bit and widening
    // ================================================================

    [Theory]
    [InlineData(InVectorBody)]
    [InlineData(InScalarTail)]
    public void Add64_PastPrecision_Throws(int index)
    {
        long[] left = new long[Length];
        long[] right = new long[Length];
        left[index] = Max18;
        right[index] = 1;

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Add(left, T18, right, T18, new long[Length], T18));
    }

    [Theory]
    [InlineData(InVectorBody)]
    [InlineData(InScalarTail)]
    public void AddWiden32To64_PastPrecision_Throws(int index)
    {
        // Widening cannot overflow the width, so only the declared precision
        // can reject this: NUMERIC(9,0) in a 64-bit result.
        int[] left = new int[Length];
        int[] right = new int[Length];
        left[index] = Max9;
        right[index] = 1;

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.AddWiden(left, T9, right, T9, new long[Length], T9));
    }

    [Theory]
    [InlineData(InVectorBody)]
    [InlineData(InScalarTail)]
    public void SubtractWiden32To64_PastPrecision_Throws(int index)
    {
        // Widening cannot overflow the width, so only the declared precision
        // can reject this: NUMERIC(9,0) in a 64-bit result.
        int[] left = new int[Length];
        int[] right = new int[Length];
        left[index] = Max9;
        right[index] = -1;

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.SubtractWiden(left, T9, right, T9, new long[Length], T9));
    }

    [Theory]
    [InlineData(InVectorBody)]
    [InlineData(InScalarTail)]
    public void SubtractWiden32To64_Ignore_WritesEverythingAndDoesNotThrow(int index)
    {
        int[] left = new int[Length];
        int[] right = new int[Length];
        left[index] = Max9;
        right[index] = -1;
        long[] result = new long[Length];

        SpanAddKernel.SubtractWiden(left, T9, right, T9, result, T9,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);

        Assert.Equal(Max9 + 1L, result[index]);
    }

    [Theory]
    [InlineData(InVectorBody)]
    [InlineData(InScalarTail)]
    public void Subtract64_BroadcastColumnScalar_PastPrecision_Throws(int index)
    {
        long[] left = new long[Length];
        left[index] = -Max18;

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Subtract(left, T18, 1L, T18, new long[Length], T18));
    }

    // ================================================================
    // The bound must not fire when the width cannot exceed the precision
    // ================================================================

    [Fact]
    public void PrecisionBeyondTheWidth_NeverTrips()
    {
        // No 32-bit value can reach 10^10, so nothing here is out of range —
        // including int.MinValue, which a sentinel bound would misreport.
        var t10 = DecimalType.Numeric(10, 0);
        int[] left = new int[Length];
        int[] right = new int[Length];
        left[InScalarTail] = int.MinValue;
        left[InVectorBody] = int.MaxValue;
        int[] result = new int[Length];

        SpanAddKernel.Add(left, t10, right, t10, result, t10);

        Assert.Equal(int.MinValue, result[InScalarTail]);
        Assert.Equal(int.MaxValue, result[InVectorBody]);
    }

    [Fact]
    public void WithinPrecision_IsUnaffected()
    {
        int[] left = new int[Length];
        int[] right = new int[Length];
        for (int i = 0; i < Length; i++) { left[i] = i; right[i] = i; }
        int[] result = new int[Length];

        SpanAddKernel.Add(left, T9, right, T9, result, T9);

        for (int i = 0; i < Length; i++) Assert.Equal(i * 2, result[i]);
    }

    /// <summary>Shorter than one vector, so the tail handles everything.</summary>
    [Fact]
    public void ShorterThanAVector_StillChecksTheBound()
    {
        int[] left = { Max9 };
        int[] right = { 1 };

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Add(left, T9, right, T9, new int[1], T9));

        int[] result = new int[1];
        SpanAddKernel.Add(left, T9, right, T9, result, T9,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        Assert.Equal(1_000_000_000, result[0]);
    }
}
