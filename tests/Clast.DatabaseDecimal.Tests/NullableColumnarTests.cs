// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// The columnar entry points that take a validity bitmap in or hand an
/// out-of-range bitmap back.
/// </summary>
/// <remarks>
/// The folded-mask overloads are held against the plain kernel followed by
/// <see cref="DecimalRange"/>.<c>WriteOutOfRangeMask</c>, which is the two-pass
/// shape they replace: same results, same mask, same count.
/// </remarks>
public class NullableColumnarTests
{
    // Lengths chosen to exercise the vector tail and the mask's final partial
    // word: 1 is below any vector width, 7 and 63 straddle a word, 64 fills one
    // exactly, and 200 spans several with a remainder.
    public static TheoryData<int> Lengths => new() { 1, 3, 7, 63, 64, 65, 200 };

    /// <summary>
    /// A random 64-bit value. <c>Random.NextInt64</c> is .NET 6 and later, and
    /// this project also builds for net472 on Windows so the netstandard2.0
    /// polyfills get exercised.
    /// </summary>
    private static long RandomInt64(Random rng)
    {
        unchecked
        {
            ulong hi = (uint)rng.Next(int.MinValue, int.MaxValue);
            ulong lo = (uint)rng.Next(int.MinValue, int.MaxValue);
            return (long)((hi << 32) | lo);
        }
    }

    /// <inheritdoc cref="RandomInt64(Random)"/>
    private static long RandomInt64(Random rng, long minInclusive, long maxExclusive)
    {
        ulong range = (ulong)(maxExclusive - minInclusive);
        return minInclusive + (long)((ulong)RandomInt64(rng) % range);
    }

    private static ulong[] NewMask(int length) => new ulong[DecimalRange.MaskWordCount(length)];

    private static bool Bit(ReadOnlySpan<ulong> mask, int i) => (mask[i >> 6] & (1UL << (i & 63))) != 0;

    // ================================================================
    // Folded out-of-range mask — add and subtract
    // ================================================================

    [Theory]
    [MemberData(nameof(Lengths))]
    public void Add32_FoldedMask_MatchesKernelPlusSeparatePass(int length)
    {
        // NUMERIC(9,2) bounds at 10^9 - 1, and operands near half that make a
        // meaningful share of the sums overflow the declared precision.
        var type = DecimalType.Numeric(9, 2);
        var rng = new Random(11);
        int[] left = new int[length], right = new int[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = rng.Next(-900_000_000, 900_000_000);
            right[i] = rng.Next(-900_000_000, 900_000_000);
        }
        // Guarantee an offender even at length 1: 1.8e9 needs ten digits.
        left[0] = 900_000_000; right[0] = 900_000_000;

        int[] expected = new int[length];
        SpanAddKernel.Add(left, type, right, type, expected, type,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        ulong[] expectedMask = NewMask(length);
        int expectedCount = DecimalRange.WriteOutOfRangeMask(expected, type, expectedMask);

        int[] actual = new int[length];
        ulong[] actualMask = NewMask(length);
        int actualCount = SpanAddKernel.Add(left, type, right, type, actual, type, actualMask);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedMask, actualMask);
        Assert.Equal(expectedCount, actualCount);
        Assert.True(expectedCount > 0, "the fixture should produce out-of-range rows or the mask check proves nothing");
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void Add64_FoldedMask_MatchesKernelPlusSeparatePass(int length)
    {
        var type = DecimalType.Numeric(18, 2);
        var rng = new Random(12);
        long[] left = new long[length], right = new long[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = RandomInt64(rng, -900_000_000_000_000_000L, 900_000_000_000_000_000L);
            right[i] = RandomInt64(rng, -900_000_000_000_000_000L, 900_000_000_000_000_000L);
        }
        // Guarantee an offender even at length 1: 1.8e18 needs nineteen digits.
        left[0] = 900_000_000_000_000_000L; right[0] = 900_000_000_000_000_000L;

        long[] expected = new long[length];
        SpanAddKernel.Add(left, type, right, type, expected, type,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        ulong[] expectedMask = NewMask(length);
        int expectedCount = DecimalRange.WriteOutOfRangeMask(expected, type, expectedMask);

        long[] actual = new long[length];
        ulong[] actualMask = NewMask(length);
        int actualCount = SpanAddKernel.Add(left, type, right, type, actual, type, actualMask);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedMask, actualMask);
        Assert.Equal(expectedCount, actualCount);
        Assert.True(expectedCount > 0);
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void Subtract64_FoldedMask_MatchesKernelPlusSeparatePass(int length)
    {
        var type = DecimalType.Numeric(18, 2);
        var rng = new Random(13);
        long[] left = new long[length], right = new long[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = RandomInt64(rng, -900_000_000_000_000_000L, 900_000_000_000_000_000L);
            right[i] = RandomInt64(rng, -900_000_000_000_000_000L, 900_000_000_000_000_000L);
        }
        left[0] = -900_000_000_000_000_000L; right[0] = 900_000_000_000_000_000L;

        long[] expected = new long[length];
        SpanAddKernel.Subtract(left, type, right, type, expected, type,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        ulong[] expectedMask = NewMask(length);
        int expectedCount = DecimalRange.WriteOutOfRangeMask(expected, type, expectedMask);

        long[] actual = new long[length];
        ulong[] actualMask = NewMask(length);
        int actualCount = SpanAddKernel.Subtract(left, type, right, type, actual, type, actualMask);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedMask, actualMask);
        Assert.Equal(expectedCount, actualCount);
        Assert.True(expectedCount > 0);
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void Subtract32_FoldedMask_MatchesKernelPlusSeparatePass(int length)
    {
        var type = DecimalType.Numeric(9, 2);
        var rng = new Random(14);
        int[] left = new int[length], right = new int[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = rng.Next(-900_000_000, 900_000_000);
            right[i] = rng.Next(-900_000_000, 900_000_000);
        }
        left[0] = -900_000_000; right[0] = 900_000_000;

        int[] expected = new int[length];
        SpanAddKernel.Subtract(left, type, right, type, expected, type,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        ulong[] expectedMask = NewMask(length);
        int expectedCount = DecimalRange.WriteOutOfRangeMask(expected, type, expectedMask);

        int[] actual = new int[length];
        ulong[] actualMask = NewMask(length);
        int actualCount = SpanAddKernel.Subtract(left, type, right, type, actual, type, actualMask);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedMask, actualMask);
        Assert.Equal(expectedCount, actualCount);
        Assert.True(expectedCount > 0);
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void AddWiden32To64_FoldedMask_MatchesKernelPlusSeparatePass(int length)
    {
        var operandType = DecimalType.Numeric(9, 2);
        // A result precision below what the widened sum can reach, so the mask
        // has something to say.
        var resultType = DecimalType.Numeric(9, 2);
        var rng = new Random(15);
        int[] left = new int[length], right = new int[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = rng.Next(-900_000_000, 900_000_000);
            right[i] = rng.Next(-900_000_000, 900_000_000);
        }
        left[0] = 900_000_000; right[0] = 900_000_000;

        long[] expected = new long[length];
        SpanAddKernel.AddWiden(left, operandType, right, operandType, expected, resultType,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        ulong[] expectedMask = NewMask(length);
        int expectedCount = DecimalRange.WriteOutOfRangeMask(expected, resultType, expectedMask);

        long[] actual = new long[length];
        ulong[] actualMask = NewMask(length);
        int actualCount = SpanAddKernel.AddWiden(left, operandType, right, operandType, actual, resultType, actualMask);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedMask, actualMask);
        Assert.Equal(expectedCount, actualCount);
        Assert.True(expectedCount > 0);
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void Add128_FoldedMask_MatchesKernelPlusSeparatePass(int length)
    {
        var type = DecimalType.Numeric(20, 2);
        var rng = new Random(16);
        Int128[] left = new Int128[length], right = new Int128[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = (Int128)RandomInt64(rng) * 90;
            right[i] = (Int128)RandomInt64(rng) * 90;
        }
        // 1.8e20 against NUMERIC(20,2)'s bound of 10^20.
        left[0] = (Int128)900_000_000_000_000_000L * 100;
        right[0] = (Int128)900_000_000_000_000_000L * 100;

        Int128[] expected = new Int128[length];
        SpanAddKernel.Add(left, type, right, type, expected, type,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        ulong[] expectedMask = NewMask(length);
        int expectedCount = DecimalRange.WriteOutOfRangeMask(expected, type, expectedMask);

        Int128[] actual = new Int128[length];
        ulong[] actualMask = NewMask(length);
        int actualCount = SpanAddKernel.Add(left, type, right, type, actual, type, actualMask);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedMask, actualMask);
        Assert.Equal(expectedCount, actualCount);
        Assert.True(expectedCount > 0);
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void Subtract256_FoldedMask_MatchesKernelPlusSeparatePass(int length)
    {
        var type = DecimalType.Numeric(20, 2);
        var rng = new Random(17);
        Int256[] left = new Int256[length], right = new Int256[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = (Int256)RandomInt64(rng) * (Int256)90;
            right[i] = (Int256)RandomInt64(rng) * (Int256)90;
        }
        left[0] = (Int256)(-900_000_000_000_000_000L) * (Int256)100;
        right[0] = (Int256)900_000_000_000_000_000L * (Int256)100;

        Int256[] expected = new Int256[length];
        SpanAddKernel.Subtract(left, type, right, type, expected, type,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        ulong[] expectedMask = NewMask(length);
        int expectedCount = DecimalRange.WriteOutOfRangeMask(expected, type, expectedMask);

        Int256[] actual = new Int256[length];
        ulong[] actualMask = NewMask(length);
        int actualCount = SpanAddKernel.Subtract(left, type, right, type, actual, type, actualMask);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedMask, actualMask);
        Assert.Equal(expectedCount, actualCount);
        Assert.True(expectedCount > 0);
    }

    [Fact]
    public void FoldedMask_RescalingPath_MatchesKernelPlusSeparatePass()
    {
        // Different operand scales, so the vectorised same-scale branch is
        // bypassed and the mask has to be folded into the scalar loop instead.
        var leftType = DecimalType.Numeric(18, 2);
        var rightType = DecimalType.Numeric(18, 4);
        var resultType = DecimalType.Numeric(18, 4);

        long[] left = [1, 2, 99_999_999_999_999_99L, -99_999_999_999_999_99L];
        long[] right = [5000, -25_000, 9_999_999_999_999_9999L, -9_999_999_999_999_9999L];

        long[] expected = new long[left.Length];
        SpanAddKernel.Add(left, leftType, right, rightType, expected, resultType,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        ulong[] expectedMask = NewMask(left.Length);
        int expectedCount = DecimalRange.WriteOutOfRangeMask(expected, resultType, expectedMask);

        long[] actual = new long[left.Length];
        ulong[] actualMask = NewMask(left.Length);
        int actualCount = SpanAddKernel.Add(left, leftType, right, rightType, actual, resultType, actualMask);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedMask, actualMask);
        Assert.Equal(expectedCount, actualCount);
    }

    [Fact]
    public void FoldedMask_OverwritesADirtyBuffer()
    {
        var type = DecimalType.Numeric(9, 0);
        int[] left = [1, 1];
        int[] right = [1, 1];
        int[] result = new int[2];

        ulong[] mask = [ulong.MaxValue];
        int count = SpanAddKernel.Add(left, type, right, type, result, type, mask);

        Assert.Equal(0, count);
        Assert.Equal(0UL, mask[0]);
    }

    [Fact]
    public void FoldedMask_FlagsOnlyTheOffendingRow()
    {
        var type = DecimalType.Numeric(9, 0);
        int[] left = [1, 999_999_999, 3];
        int[] right = [1, 1, 3];
        int[] result = new int[3];
        ulong[] mask = NewMask(3);

        int count = SpanAddKernel.Add(left, type, right, type, result, type, mask);

        Assert.Equal(1, count);
        Assert.False(Bit(mask, 0));
        Assert.True(Bit(mask, 1));
        Assert.False(Bit(mask, 2));
        // The output is still fully written, offending row included.
        Assert.Equal([2, 1_000_000_000, 6], result);
    }

    [Fact]
    public void FoldedMask_LeavesBitsPastTheEndAlone()
    {
        var type = DecimalType.Numeric(9, 0);
        int[] left = [999_999_999];
        int[] right = [1];
        int[] result = new int[1];
        ulong[] mask = NewMask(1);

        int count = SpanAddKernel.Add(left, type, right, type, result, type, mask);

        Assert.Equal(1, count);
        Assert.Equal(1UL, mask[0]);
    }

    [Fact]
    public void FoldedMask_RejectsAShortMask()
    {
        var type = DecimalType.Numeric(9, 0);
        int[] left = new int[65], right = new int[65], result = new int[65];
        ulong[] mask = new ulong[1];   // 65 elements need two words

        Assert.Throws<ArgumentException>(() =>
            SpanAddKernel.Add(left, type, right, type, result, type, mask));
    }

    [Fact]
    public void FoldedMask_StillThrowsOnWidthOverflow()
    {
        // The declared precision is reported through the mask, but the mantissa
        // width is checked as it is everywhere else.
        var type = DecimalType.Numeric(19, 0);
        long[] left = [long.MaxValue];
        long[] right = [long.MaxValue];
        long[] result = new long[1];
        ulong[] mask = NewMask(1);

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Add(left, type, right, type, result, type, mask));
    }
}
