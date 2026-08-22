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
/// shape they replace: same results, same mask, same count. The validity-aware
/// divide and modulus overloads are held against the same dense kernels run over
/// operands whose null slots have been patched to something harmless, since a
/// dense pass over a real null slot is exactly what they exist to avoid.
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

    /// <summary>A bitmap with roughly one row in three null, plus a fixed pattern at the front.</summary>
    private static ulong[] BuildValidity(int length, int seed, out int validCount)
    {
        var rng = new Random(seed);
        ulong[] validity = NewMask(length);
        validCount = 0;
        for (int i = 0; i < length; i++)
        {
            // Row 0 valid and row 1 null in every case, so the small lengths
            // still cover both branches.
            bool valid = i == 0 || (i != 1 && rng.Next(3) != 0);
            if (!valid) continue;
            validity[i >> 6] |= 1UL << (i & 63);
            validCount++;
        }
        return validity;
    }

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

    [Theory]
    [MemberData(nameof(Lengths))]
    public void SubtractWiden32To64_FoldedMask_MatchesKernelPlusSeparatePass(int length)
    {
        var operandType = DecimalType.Numeric(9, 2);
        // A result precision the widened difference can exceed, so the mask has
        // something to say.
        var resultType = DecimalType.Numeric(9, 2);
        var rng = new Random(41);
        int[] left = new int[length], right = new int[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = rng.Next(-900_000_000, 900_000_000);
            right[i] = rng.Next(-900_000_000, 900_000_000);
        }
        left[0] = -900_000_000; right[0] = 900_000_000;

        long[] expected = new long[length];
        SpanAddKernel.SubtractWiden(left, operandType, right, operandType, expected, resultType,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        ulong[] expectedMask = NewMask(length);
        int expectedCount = DecimalRange.WriteOutOfRangeMask(expected, resultType, expectedMask);

        long[] actual = new long[length];
        ulong[] actualMask = NewMask(length);
        int actualCount = SpanAddKernel.SubtractWiden(left, operandType, right, operandType, actual, resultType, actualMask);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedMask, actualMask);
        Assert.Equal(expectedCount, actualCount);
        Assert.True(expectedCount > 0);
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void SubtractWiden64To128_FoldedMask_MatchesKernelPlusSeparatePass(int length)
    {
        var operandType = DecimalType.Numeric(18, 2);
        var resultType = DecimalType.Numeric(18, 2);
        var rng = new Random(42);
        long[] left = new long[length], right = new long[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = RandomInt64(rng, -900_000_000_000_000_000L, 900_000_000_000_000_000L);
            right[i] = RandomInt64(rng, -900_000_000_000_000_000L, 900_000_000_000_000_000L);
        }
        left[0] = -900_000_000_000_000_000L; right[0] = 900_000_000_000_000_000L;

        Int128[] expected = new Int128[length];
        SpanAddKernel.SubtractWiden(left, operandType, right, operandType, expected, resultType,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        ulong[] expectedMask = NewMask(length);
        int expectedCount = DecimalRange.WriteOutOfRangeMask(expected, resultType, expectedMask);

        Int128[] actual = new Int128[length];
        ulong[] actualMask = NewMask(length);
        int actualCount = SpanAddKernel.SubtractWiden(left, operandType, right, operandType, actual, resultType, actualMask);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedMask, actualMask);
        Assert.Equal(expectedCount, actualCount);
        Assert.True(expectedCount > 0);
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void SubtractWiden128To256_FoldedMask_MatchesKernelPlusSeparatePass(int length)
    {
        var operandType = DecimalType.Numeric(38, 2);
        var resultType = DecimalType.Numeric(38, 2);
        var rng = new Random(43);
        Int128[] left = new Int128[length], right = new Int128[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = (Int128)RandomInt64(rng) * 90;
            right[i] = (Int128)RandomInt64(rng) * 90;
        }
        // 9e37 each way, so the difference is 1.8e38 against NUMERIC(38,2)'s
        // bound of 10^38. Built by repeated multiplication because 10^37 does
        // not fit a long literal.
        Int128 nine37 = Int128.One;
        for (int k = 0; k < 37; k++) nine37 *= 10;
        nine37 *= 9;
        left[0] = -nine37;
        right[0] = nine37;

        Int256[] expected = new Int256[length];
        SpanAddKernel.SubtractWiden(left, operandType, right, operandType, expected, resultType,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        ulong[] expectedMask = NewMask(length);
        int expectedCount = DecimalRange.WriteOutOfRangeMask(expected, resultType, expectedMask);

        Int256[] actual = new Int256[length];
        ulong[] actualMask = NewMask(length);
        int actualCount = SpanAddKernel.SubtractWiden(left, operandType, right, operandType, actual, resultType, actualMask);

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

    // ================================================================
    // Validity-aware divide and modulus
    // ================================================================

    [Theory]
    [MemberData(nameof(Lengths))]
    public void Divide128_Validity_MatchesDenseOnTheValidRows(int length)
    {
        var type = DecimalType.Numeric(38, 2);
        var resultType = DecimalType.Numeric(38, 6);
        var rng = new Random(21);
        ulong[] validity = BuildValidity(length, 22, out int validCount);

        Int128[] left = new Int128[length], right = new Int128[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = RandomInt64(rng);
            // A zero divisor under every null slot: the value a builder leaves
            // there, and the one a dense pass would trip over.
            right[i] = Bit(validity, i) ? RandomInt64(rng, 1, 1_000_000) : Int128.Zero;
        }

        // Reference: the dense kernel over a divisor whose null slots have been
        // patched to one, which is the workaround this overload removes.
        Int128[] patched = new Int128[length];
        for (int i = 0; i < length; i++) patched[i] = Bit(validity, i) ? right[i] : Int128.One;
        Int128[] expected = new Int128[length];
        SpanDivideKernel.Divide(left, type, patched, type, expected, resultType,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);

        const int sentinel = -424242;
        Int128[] actual = new Int128[length];
        for (int i = 0; i < length; i++) actual[i] = sentinel;
        ulong[] mask = NewMask(length);

        int count = SpanDivideKernel.Divide(left, type, right, type, actual, resultType,
            validity, mask);

        Assert.Equal(0, count);
        for (int i = 0; i < length; i++)
        {
            if (Bit(validity, i))
                Assert.Equal(expected[i], actual[i]);
            else
                Assert.Equal((Int128)sentinel, actual[i]);   // untouched
            Assert.False(Bit(mask, i));
        }
        Assert.True(validCount > 0);
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void Modulus64_Validity_MatchesDenseOnTheValidRows(int length)
    {
        var type = DecimalType.Numeric(18, 2);
        var rng = new Random(23);
        ulong[] validity = BuildValidity(length, 24, out _);

        long[] left = new long[length], right = new long[length];
        for (int i = 0; i < length; i++)
        {
            left[i] = RandomInt64(rng, -1_000_000_000L, 1_000_000_000L);
            right[i] = Bit(validity, i) ? RandomInt64(rng, 1, 100_000) : 0L;
        }

        long[] patched = new long[length];
        for (int i = 0; i < length; i++) patched[i] = Bit(validity, i) ? right[i] : 1L;
        long[] expected = new long[length];
        SpanModulusKernel.Modulus(left, type, patched, type, expected, type,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);

        long[] actual = new long[length];
        for (int i = 0; i < length; i++) actual[i] = long.MinValue;
        ulong[] mask = NewMask(length);

        int count = SpanModulusKernel.Modulus(left, type, right, type, actual, type, validity, mask);

        Assert.Equal(0, count);
        for (int i = 0; i < length; i++)
        {
            if (Bit(validity, i))
                Assert.Equal(expected[i], actual[i]);
            else
                Assert.Equal(long.MinValue, actual[i]);
        }
    }

    [Fact]
    public void Divide_Validity_DoesNotDivideByAZeroUnderANullSlot()
    {
        // The whole point: the dense overload throws on this input, the
        // validity-aware one does not.
        var type = DecimalType.Numeric(38, 2);
        var resultType = DecimalType.Numeric(38, 4);
        Int128[] left = [100, 200, 300];
        Int128[] right = [5, 0, 3];       // element 1 is null, and holds zero
        Int128[] result = new Int128[3];
        ulong[] validity = [0b101];
        ulong[] mask = NewMask(3);

        Assert.Throws<DivideByZeroException>(() =>
            SpanDivideKernel.Divide(left, type, right, type, result, resultType,
                DecimalRounding.HalfEven, DecimalOverflow.Ignore));

        int count = SpanDivideKernel.Divide(left, type, right, type, result, resultType, validity, mask);

        Assert.Equal(0, count);
        Assert.Equal((Int128)200_000, result[0]);   // 100 / 5 at scale 4
        Assert.Equal(Int128.Zero, result[1]);       // never written
        Assert.Equal((Int128)1_000_000, result[2]); // 300 / 3 at scale 4
    }

    [Fact]
    public void Divide_Validity_StillThrowsOnAZeroDivisorInAValidRow()
    {
        var type = DecimalType.Numeric(38, 2);
        var resultType = DecimalType.Numeric(38, 4);
        Int128[] left = [100, 200];
        Int128[] right = [5, 0];
        Int128[] result = new Int128[2];
        ulong[] validity = [0b11];     // row 1 is valid and its divisor is zero
        ulong[] mask = NewMask(2);

        Assert.Throws<DivideByZeroException>(() =>
            SpanDivideKernel.Divide(left, type, right, type, result, resultType, validity, mask));
    }

    [Fact]
    public void Divide_Validity_ReportsOutOfRangeOnlyForValidRows()
    {
        var type = DecimalType.Numeric(38, 0);
        var resultType = DecimalType.Numeric(2, 0);   // bounds at 99
        Int128[] left = [1000, 1000, 4];
        Int128[] right = [1, 1, 2];
        Int128[] result = new Int128[3];
        ulong[] validity = [0b101];                   // row 1 is null
        ulong[] mask = NewMask(3);

        int count = SpanDivideKernel.Divide(left, type, right, type, result, resultType, validity, mask);

        Assert.Equal(1, count);
        Assert.True(Bit(mask, 0));    // 1000 needs three digits
        Assert.False(Bit(mask, 1));   // skipped, so not flagged
        Assert.False(Bit(mask, 2));   // 2 fits
        Assert.Equal((Int128)1000, result[0]);
        Assert.Equal((Int128)2, result[2]);
    }

    [Fact]
    public void Divide_Validity_IgnoresBitsPastTheEndOfTheSpan()
    {
        // An all-ones validity word over a five-element column must not send the
        // loop past the end of the operands.
        var type = DecimalType.Numeric(38, 0);
        Int128[] left = [10, 20, 30, 40, 50];
        Int128[] right = [1, 2, 3, 4, 5];
        Int128[] result = new Int128[5];
        ulong[] validity = [ulong.MaxValue];
        ulong[] mask = NewMask(5);

        int count = SpanDivideKernel.Divide(left, type, right, type, result, type, validity, mask);

        Assert.Equal(0, count);
        Assert.Equal([(Int128)10, (Int128)10, (Int128)10, (Int128)10, (Int128)10], result);
    }

    [Fact]
    public void Divide_Validity_AllNullTouchesNothing()
    {
        var type = DecimalType.Numeric(38, 0);
        Int128[] left = [10, 20, 30];
        Int128[] right = [0, 0, 0];
        Int128[] result = [(Int128)7, (Int128)7, (Int128)7];
        ulong[] validity = NewMask(3);      // every bit clear
        ulong[] mask = [ulong.MaxValue];

        int count = SpanDivideKernel.Divide(left, type, right, type, result, type, validity, mask);

        Assert.Equal(0, count);
        Assert.Equal([(Int128)7, (Int128)7, (Int128)7], result);
        Assert.Equal(0UL, mask[0]);
    }

    [Fact]
    public void Validity_OverwritesADirtyMask_AcrossTheOverloadShapes()
    {
        // The validity overloads assign every mask word rather than clearing the
        // buffer and setting bits into it, so a caller reusing a dirty buffer
        // must still get a clean answer. Every row here is null, which is the
        // case that breaks if a loop ever skips words with nothing to report.
        var t32 = DecimalType.Numeric(9, 0);
        var t64 = DecimalType.Numeric(18, 0);
        var t128 = DecimalType.Numeric(38, 0);
        ulong[] allNull = { 0UL };

        int[] l32 = { 1, 84, 1, 84 }, r32 = { 1, 4, 1, 4 };
        long[] l64 = { 1, 84, 1, 84 }, r64 = { 1, 4, 1, 4 };
        Int128[] l128 = { 1, 84, 1, 84 }, r128 = { 1, 4, 1, 4 };
        long[] o64 = new long[4];
        Int128[] o128 = new Int128[4];
        Int256[] o256 = new Int256[4];
        ulong[] mask = { ulong.MaxValue };

        Assert.Equal(0, SpanDivideKernel.Divide(l32, t32, r32, t32, o64, t64, allNull, mask));
        Assert.Equal(0UL, mask[0]);

        mask[0] = ulong.MaxValue;
        Assert.Equal(0, SpanDivideKernel.Divide(l64, t64, r64, t64, o128, t128, allNull, mask));
        Assert.Equal(0UL, mask[0]);

        mask[0] = ulong.MaxValue;
        Assert.Equal(0, SpanDivideKernel.DivideWiden(l128, t128, r128, t128, o256, t128, allNull, mask));
        Assert.Equal(0UL, mask[0]);

        mask[0] = ulong.MaxValue;
        Assert.Equal(0, SpanModulusKernel.Modulus(l128, t128, r128, t128, o128, t128, allNull, mask));
        Assert.Equal(0UL, mask[0]);

        mask[0] = ulong.MaxValue;
        Assert.Equal(0, SpanModulusKernel.ModulusWiden(l64, t64, r64, t64, o128, t128, allNull, mask));
        Assert.Equal(0UL, mask[0]);
    }

    [Fact]
    public void Validity_OverwritesADirtyMask_InEveryWord()
    {
        // Same guard over a mask spanning several words, with the middle word
        // entirely null: a loop that skipped it would leave that word's stale
        // ones behind and report 64 phantom out-of-range rows.
        var t = DecimalType.Numeric(38, 0);
        const int length = 200;
        Int128[] left = new Int128[length], right = new Int128[length];
        for (int i = 0; i < length; i++) { left[i] = 84; right[i] = 4; }
        Int128[] result = new Int128[length];

        ulong[] validity = NewMask(length);
        validity[0] = ulong.MaxValue;                 // rows 0-63 valid
        validity[1] = 0UL;                            // rows 64-127 all null
        validity[2] = ulong.MaxValue;                 // rows 128-191 valid
        validity[3] = 0b1111UL;                       // rows 192-195 valid
        Assert.True(validity.Length >= 4);

        ulong[] mask = NewMask(length);
        for (int w = 0; w < mask.Length; w++) mask[w] = ulong.MaxValue;

        int count = SpanDivideKernel.Divide(left, t, right, t, result, t, validity, mask);

        Assert.Equal(0, count);
        for (int w = 0; w < mask.Length; w++) Assert.Equal(0UL, mask[w]);
        Assert.Equal((Int128)21, result[0]);
        Assert.Equal(Int128.Zero, result[100]);       // inside the all-null word
        Assert.Equal((Int128)21, result[128]);
    }

    [Fact]
    public void Divide_Validity_RejectsAShortValidityMask()
    {
        var type = DecimalType.Numeric(38, 0);
        Int128[] left = new Int128[65], right = new Int128[65], result = new Int128[65];
        ulong[] validity = new ulong[1];    // 65 elements need two words
        ulong[] mask = NewMask(65);

        Assert.Throws<ArgumentException>(() =>
            SpanDivideKernel.Divide(left, type, right, type, result, type, validity, mask));
    }

    [Fact]
    public void Divide_Validity_RejectsAShortOutOfRangeMask()
    {
        var type = DecimalType.Numeric(38, 0);
        Int128[] left = new Int128[65], right = new Int128[65], result = new Int128[65];
        ulong[] validity = NewMask(65);
        ulong[] mask = new ulong[1];

        Assert.Throws<ArgumentException>(() =>
            SpanDivideKernel.Divide(left, type, right, type, result, type, validity, mask));
    }

    [Fact]
    public void ModulusWiden_Validity_MatchesDenseOnTheValidRows()
    {
        var type = DecimalType.Numeric(9, 2);
        var resultType = DecimalType.Numeric(18, 2);
        int[] left = [1000, 2000, 3000, 4000];
        int[] right = [7, 0, 11, 0];
        long[] result = new long[4];
        for (int i = 0; i < result.Length; i++) result[i] = -1L;
        ulong[] validity = [0b0101];
        ulong[] mask = NewMask(4);

        int count = SpanModulusKernel.ModulusWiden(left, type, right, type, result, resultType,
            validity, mask);

        Assert.Equal(0, count);
        Assert.Equal(1000L % 7, result[0]);
        Assert.Equal(-1L, result[1]);
        Assert.Equal(3000L % 11, result[2]);
        Assert.Equal(-1L, result[3]);
    }

    [Fact]
    public void Divide_Validity_TrailingPartialWordIsIterated()
    {
        // 65 elements: the second mask word holds exactly one live bit, which is
        // the case a word-at-a-time loop is most likely to drop.
        var type = DecimalType.Numeric(38, 0);
        int length = 65;
        Int128[] left = new Int128[length], right = new Int128[length];
        for (int i = 0; i < length; i++) { left[i] = 84; right[i] = 4; }
        Int128[] result = new Int128[length];
        ulong[] validity = NewMask(length);
        validity[1] |= 1UL;                 // element 64 only
        ulong[] mask = NewMask(length);

        int count = SpanDivideKernel.Divide(left, type, right, type, result, type, validity, mask);

        Assert.Equal(0, count);
        Assert.Equal((Int128)21, result[64]);
        Assert.Equal(Int128.Zero, result[63]);
    }
}
