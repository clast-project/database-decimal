// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

public class SpanAddKernelTests
{
    // ----------------------------------------------------------------
    // Add — column + column, same scale
    // ----------------------------------------------------------------

    [Fact]
    public void Add_32Bit_SameScale()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        int[] left = [150, 225, 300];     // 1.50, 2.25, 3.00
        int[] right = [100, 175, 200];    // 1.00, 1.75, 2.00
        int[] result = new int[3];

        SpanAddKernel.Add(left, type, right, type, result, resultType);

        Assert.Equal([250, 400, 500], result);
    }

    [Fact]
    public void Add_64Bit_SameScale()
    {
        var type = DecimalType.Numeric(12, 3);
        var resultType = DecimalTypeRules.Add(type, type);

        long[] left = [1_000_000_000, 2_000_000_000];
        long[] right = [500_000_000, 750_000_000];
        long[] result = new long[2];

        SpanAddKernel.Add(left, type, right, type, result, resultType);

        Assert.Equal([1_500_000_000L, 2_750_000_000L], result);
    }

    // ----------------------------------------------------------------
    // Add — column + column, different scales
    // ----------------------------------------------------------------

    [Fact]
    public void Add_32Bit_DifferentScales()
    {
        var leftType = DecimalType.Numeric(5, 2);
        var rightType = DecimalType.Numeric(6, 3);
        var resultType = DecimalTypeRules.Add(leftType, rightType);
        // result scale = 3, left needs scale-up by 1

        int[] left = [150, 225];     // 1.50, 2.25
        int[] right = [2125, 3500];  // 2.125, 3.500
        int[] result = new int[2];

        SpanAddKernel.Add(left, leftType, right, rightType, result, resultType);

        Assert.Equal([3625, 5750], result); // 3.625, 5.750
    }

    // ----------------------------------------------------------------
    // Add — widening
    // ----------------------------------------------------------------

    [Fact]
    public void AddWiden_32To64()
    {
        var type = DecimalType.Numeric(9, 2);
        var resultType = DecimalType.Numeric(10, 2);

        int[] left = [999_999_999, 500_000_000];
        int[] right = [999_999_999, 500_000_000];
        long[] result = new long[2];

        SpanAddKernel.AddWiden(left, type, right, type, result, resultType);

        Assert.Equal([1_999_999_998L, 1_000_000_000L], result);
    }

    [Fact]
    public void AddWiden_64To128()
    {
        var type = DecimalType.Numeric(18, 0);
        var resultType = DecimalType.Numeric(19, 0);

        long[] left = [long.MaxValue / 2, 1_000_000_000_000_000_000];
        long[] right = [long.MaxValue / 2, 2_000_000_000_000_000_000];
        Int128[] result = new Int128[2];

        SpanAddKernel.AddWiden(left, type, right, type, result, resultType);

        Assert.Equal((Int128)(long.MaxValue / 2) + (long.MaxValue / 2), result[0]);
        Assert.Equal((Int128)3_000_000_000_000_000_000, result[1]);
    }

    // ----------------------------------------------------------------
    // Subtract — column - column, widening
    // ----------------------------------------------------------------

    [Fact]
    public void SubtractWiden_32To64()
    {
        var type = DecimalType.Numeric(9, 2);
        var resultType = DecimalType.Numeric(10, 2);

        int[] left = [999_999_999, 500_000_000];
        int[] right = [-999_999_999, -500_000_000];
        long[] result = new long[2];

        SpanAddKernel.SubtractWiden(left, type, right, type, result, resultType);

        Assert.Equal([1_999_999_998L, 1_000_000_000L], result);
    }

    [Fact]
    public void SubtractWiden_32To64_DifferentScales()
    {
        var leftType = DecimalType.Numeric(9, 0);
        var rightType = DecimalType.Numeric(9, 2);
        var resultType = DecimalType.Numeric(12, 2);

        int[] left = [1, -1];
        int[] right = [50, -50];
        long[] result = new long[2];

        SpanAddKernel.SubtractWiden(left, leftType, right, rightType, result, resultType);

        Assert.Equal([50L, -50L], result); // 1.00 - 0.50, -1.00 - -0.50
    }

    [Fact]
    public void SubtractWiden_64To128()
    {
        var type = DecimalType.Numeric(18, 0);
        var resultType = DecimalType.Numeric(19, 0);

        long[] left = [long.MaxValue / 2, 1_000_000_000_000_000_000];
        long[] right = [-(long.MaxValue / 2), -2_000_000_000_000_000_000];
        Int128[] result = new Int128[2];

        SpanAddKernel.SubtractWiden(left, type, right, type, result, resultType);

        Assert.Equal((Int128)(long.MaxValue / 2) + (long.MaxValue / 2), result[0]);
        Assert.Equal((Int128)3_000_000_000_000_000_000, result[1]);
    }

    [Fact]
    public void SubtractWiden_128To256()
    {
        var type = DecimalType.Numeric(38, 0);
        var resultType = DecimalType.Numeric(39, 0);

        Int128[] left = [Int128.MaxValue / 2];
        Int128[] right = [-(Int128.MaxValue / 2)];
        Int256[] result = new Int256[1];

        SpanAddKernel.SubtractWiden(left, type, right, type, result, resultType);

        Int256 expected = (Int256)(Int128.MaxValue / 2) + (Int256)(Int128.MaxValue / 2);
        Assert.Equal(expected, result[0]);
    }

    // ----------------------------------------------------------------
    // Add — column + scalar (broadcast)
    // ----------------------------------------------------------------

    [Fact]
    public void Add_32Bit_Broadcast()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        int[] left = [100, 200, 300];
        int scalar = 50; // 0.50
        int[] result = new int[3];

        SpanAddKernel.Add(left, type, scalar, type, result, resultType);

        Assert.Equal([150, 250, 350], result);
    }

    [Fact]
    public void Add_64Bit_Broadcast_DifferentScale()
    {
        var leftType = DecimalType.Numeric(10, 2);
        var rightType = DecimalType.Numeric(10, 3);
        var resultType = DecimalTypeRules.Add(leftType, rightType);

        long[] left = [1000, 2000]; // 10.00, 20.00
        long scalar = 5500;         // 5.500
        long[] result = new long[2];

        SpanAddKernel.Add(left, leftType, scalar, rightType, result, resultType);

        Assert.Equal([15500L, 25500L], result); // 15.500, 25.500
    }

    // ----------------------------------------------------------------
    // Subtract — column - column
    // ----------------------------------------------------------------

    [Fact]
    public void Subtract_32Bit_SameScale()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Subtract(type, type);

        int[] left = [500, 300, 100];
        int[] right = [150, 175, 200];
        int[] result = new int[3];

        SpanAddKernel.Subtract(left, type, right, type, result, resultType);

        Assert.Equal([350, 125, -100], result);
    }

    // ----------------------------------------------------------------
    // Subtract — column - scalar
    // ----------------------------------------------------------------

    [Fact]
    public void Subtract_32Bit_Broadcast()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Subtract(type, type);

        int[] left = [500, 300, 100];
        int scalar = 200;
        int[] result = new int[3];

        SpanAddKernel.Subtract(left, type, scalar, type, result, resultType);

        Assert.Equal([300, 100, -100], result);
    }

    // ----------------------------------------------------------------
    // Subtract — scalar - column
    // ----------------------------------------------------------------

    [Fact]
    public void Subtract_ScalarMinusColumn()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Subtract(type, type);

        int scalar = 1000; // 10.00
        int[] right = [300, 500, 1500];
        int[] result = new int[3];

        SpanAddKernel.Subtract(scalar, type, right, type, result, resultType);

        Assert.Equal([700, 500, -500], result);
    }

    // ----------------------------------------------------------------
    // In-place aliasing
    // ----------------------------------------------------------------

    [Fact]
    public void Add_InPlace_LeftIsResult()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        int[] data = [100, 200, 300];
        int[] right = [50, 50, 50];

        SpanAddKernel.Add(data, type, right, type, data, resultType);

        Assert.Equal([150, 250, 350], data);
    }

    // ----------------------------------------------------------------
    // Empty spans
    // ----------------------------------------------------------------

    [Fact]
    public void Add_EmptySpans()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        SpanAddKernel.Add(
            ReadOnlySpan<int>.Empty, type,
            ReadOnlySpan<int>.Empty, type,
            Span<int>.Empty, resultType);
        // Should not throw
    }

    // ----------------------------------------------------------------
    // Validation
    // ----------------------------------------------------------------

    [Fact]
    public void Add_MismatchedLengths_Throws()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        Assert.Throws<ArgumentException>(() =>
            SpanAddKernel.Add(new int[3], type, new int[2], type, new int[3], resultType));
    }

    [Fact]
    public void Add_ResultTooShort_Throws()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        Assert.Throws<ArgumentException>(() =>
            SpanAddKernel.Add(new int[3], type, new int[3], type, new int[2], resultType));
    }

    // ----------------------------------------------------------------
    // Overflow
    // ----------------------------------------------------------------

    [Fact]
    public void Add_Overflow_Throws()
    {
        var type = DecimalType.Numeric(5, 0);
        var resultType = DecimalTypeRules.Add(type, type);

        int[] left = [int.MaxValue];
        int[] right = [1];

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Add(left, type, right, type, new int[1], resultType));
    }

    // ----------------------------------------------------------------
    // 128-bit and 256-bit
    // ----------------------------------------------------------------

    [Fact]
    public void Add_128Bit()
    {
        var type = DecimalType.Numeric(20, 5);
        var resultType = DecimalTypeRules.Add(type, type);

        Int128[] left = [100_00000, 200_00000];
        Int128[] right = [50_00000, 75_00000];
        Int128[] result = new Int128[2];

        SpanAddKernel.Add(left, type, right, type, result, resultType);

        Assert.Equal((Int128)150_00000, result[0]);
        Assert.Equal((Int128)275_00000, result[1]);
    }

    [Fact]
    public void Add_256Bit()
    {
        var type = DecimalType.Numeric(40, 5);
        var resultType = DecimalTypeRules.Add(type, type);

        Int256[] left = [(Int256)100_00000, (Int256)200_00000];
        Int256[] right = [(Int256)50_00000, (Int256)75_00000];
        Int256[] result = new Int256[2];

        SpanAddKernel.Add(left, type, right, type, result, resultType);

        Assert.Equal((Int256)150_00000, result[0]);
        Assert.Equal((Int256)275_00000, result[1]);
    }

    [Fact]
    public void AddWiden_128To256()
    {
        var type = DecimalType.Numeric(38, 0);
        var resultType = DecimalType.Numeric(39, 0);

        Int128[] left = [Int128.MaxValue / 2];
        Int128[] right = [Int128.MaxValue / 2];
        Int256[] result = new Int256[1];

        SpanAddKernel.AddWiden(left, type, right, type, result, resultType);

        Int256 expected = (Int256)(Int128.MaxValue / 2) + (Int256)(Int128.MaxValue / 2);
        Assert.Equal(expected, result[0]);
    }

    // ----------------------------------------------------------------
    // SIMD chunked-path coverage. Existing tests use 1-3 element spans
    // which all fall into the scalar tail (Vector<int>.Count is 8 on
    // AVX2). These tests use larger lengths and lengths that are not
    // multiples of any specific vector width, so both the SIMD chunk
    // loop and the scalar tail are exercised on any hardware.
    // ----------------------------------------------------------------

    [Fact]
    public void Add_32Bit_SameScale_SimdChunkedAndTail()
    {
        var type = DecimalType.Numeric(9, 2);
        int n = 23;
        int[] left = new int[n];
        int[] right = new int[n];
        int[] expected = new int[n];
        for (int i = 0; i < n; i++)
        {
            left[i] = i * 100;
            right[i] = i * 50;
            expected[i] = i * 150;
        }
        int[] result = new int[n];
        SpanAddKernel.Add(left, type, right, type, result, type);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Add_64Bit_SameScale_SimdChunkedAndTail()
    {
        var type = DecimalType.Numeric(18, 3);
        int n = 19;
        long[] left = new long[n];
        long[] right = new long[n];
        long[] expected = new long[n];
        for (int i = 0; i < n; i++)
        {
            left[i] = (long)i * 1_000_000_000L;
            right[i] = (long)i * 500_000_000L;
            expected[i] = (long)i * 1_500_000_000L;
        }
        long[] result = new long[n];
        SpanAddKernel.Add(left, type, right, type, result, type);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Subtract_32Bit_SameScale_SimdChunkedAndTail()
    {
        var type = DecimalType.Numeric(9, 2);
        int n = 23;
        int[] left = new int[n];
        int[] right = new int[n];
        int[] expected = new int[n];
        for (int i = 0; i < n; i++)
        {
            left[i] = i * 1000;
            right[i] = i * 300;
            expected[i] = i * 700;
        }
        int[] result = new int[n];
        SpanAddKernel.Subtract(left, type, right, type, result, type);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Subtract_64Bit_SameScale_SimdChunkedAndTail()
    {
        var type = DecimalType.Numeric(18, 3);
        int n = 19;
        long[] left = new long[n];
        long[] right = new long[n];
        long[] expected = new long[n];
        for (int i = 0; i < n; i++)
        {
            left[i] = (long)i * 1_000_000_000L;
            right[i] = (long)i * 300_000_000L;
            expected[i] = (long)i * 700_000_000L;
        }
        long[] result = new long[n];
        SpanAddKernel.Subtract(left, type, right, type, result, type);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Add_32Bit_OverflowInSimdChunk_Throws()
    {
        var type = DecimalType.Numeric(9, 0);
        int n = 16;
        int[] left = new int[n];
        int[] right = new int[n];
        for (int i = 0; i < n; i++) { left[i] = 1; right[i] = 1; }
        // Overflow placed in the second SIMD chunk (index 10 on AVX2, where Vector<int>.Count=8).
        left[10] = int.MaxValue;
        right[10] = 1;
        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Add(left, type, right, type, new int[n], type));
    }

    [Fact]
    public void Subtract_32Bit_OverflowInSimdChunk_Throws()
    {
        var type = DecimalType.Numeric(9, 0);
        int n = 16;
        int[] left = new int[n];
        int[] right = new int[n];
        for (int i = 0; i < n; i++) { left[i] = 1; right[i] = 1; }
        left[5] = int.MinValue;
        right[5] = 1;
        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Subtract(left, type, right, type, new int[n], type));
    }

    [Fact]
    public void Add_64Bit_OverflowInSimdChunk_Throws()
    {
        var type = DecimalType.Numeric(18, 0);
        int n = 8;
        long[] left = new long[n];
        long[] right = new long[n];
        for (int i = 0; i < n; i++) { left[i] = 1; right[i] = 1; }
        left[3] = long.MaxValue;
        right[3] = 1;
        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Add(left, type, right, type, new long[n], type));
    }

    [Fact]
    public void AddWiden_32To64_SimdChunkedAndTail_PreservesElementOrder()
    {
        // Vector.Widen splits each Vector<int> into lower/upper Vector<long>
        // halves; this test verifies output[i] == (long)left[i] + right[i]
        // for every index, catching any swap of the low/high writes.
        var type = DecimalType.Numeric(9, 2);
        var resultType = DecimalType.Numeric(10, 2);
        int n = 23;
        int[] left = new int[n];
        int[] right = new int[n];
        long[] expected = new long[n];
        for (int i = 0; i < n; i++)
        {
            left[i] = int.MaxValue - i;
            right[i] = int.MaxValue - 2 * i;
            expected[i] = (long)left[i] + right[i];
        }
        long[] result = new long[n];
        SpanAddKernel.AddWiden(left, type, right, type, result, resultType);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SubtractWiden_32To64_SimdChunkedAndTail_PreservesElementOrder()
    {
        // As with the widening add, Vector.Widen splits each Vector<int> into
        // lower/upper Vector<long> halves; this checks every index, so a swap
        // of the low/high writes cannot pass.
        var type = DecimalType.Numeric(9, 2);
        var resultType = DecimalType.Numeric(10, 2);
        int n = 23;
        int[] left = new int[n];
        int[] right = new int[n];
        long[] expected = new long[n];
        for (int i = 0; i < n; i++)
        {
            left[i] = int.MaxValue - i;
            right[i] = int.MinValue + 2 * i;
            expected[i] = (long)left[i] - right[i];
        }
        long[] result = new long[n];
        SpanAddKernel.SubtractWiden(left, type, right, type, result, resultType);
        Assert.Equal(expected, result);
    }

    // ----------------------------------------------------------------
    // Broadcast (column + scalar / column - scalar / scalar - column)
    // SIMD chunked-path coverage. The broadcast helpers load the scalar
    // into a Vector<T> once and reuse it across the chunk loop.
    // ----------------------------------------------------------------

    [Fact]
    public void Add_32Bit_Broadcast_SimdChunkedAndTail()
    {
        var type = DecimalType.Numeric(9, 2);
        int n = 23;
        int[] left = new int[n];
        int[] expected = new int[n];
        int scalar = 777;
        for (int i = 0; i < n; i++)
        {
            left[i] = i * 100;
            expected[i] = i * 100 + scalar;
        }
        int[] result = new int[n];
        SpanAddKernel.Add(left, type, scalar, type, result, type);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Add_64Bit_Broadcast_SimdChunkedAndTail()
    {
        var type = DecimalType.Numeric(18, 3);
        int n = 19;
        long[] left = new long[n];
        long[] expected = new long[n];
        long scalar = 1_234_567_890L;
        for (int i = 0; i < n; i++)
        {
            left[i] = (long)i * 1_000_000_000L;
            expected[i] = left[i] + scalar;
        }
        long[] result = new long[n];
        SpanAddKernel.Add(left, type, scalar, type, result, type);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Subtract_32Bit_BroadcastColumnScalar_SimdChunkedAndTail()
    {
        var type = DecimalType.Numeric(9, 2);
        int n = 23;
        int[] left = new int[n];
        int[] expected = new int[n];
        int scalar = 333;
        for (int i = 0; i < n; i++)
        {
            left[i] = i * 500;
            expected[i] = i * 500 - scalar;
        }
        int[] result = new int[n];
        SpanAddKernel.Subtract(left, type, scalar, type, result, type);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Subtract_32Bit_BroadcastScalarColumn_SimdChunkedAndTail()
    {
        var type = DecimalType.Numeric(9, 2);
        int n = 23;
        int[] right = new int[n];
        int[] expected = new int[n];
        int scalar = 100_000;
        for (int i = 0; i < n; i++)
        {
            right[i] = i * 300;
            expected[i] = scalar - i * 300;
        }
        int[] result = new int[n];
        SpanAddKernel.Subtract(scalar, type, right, type, result, type);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Subtract_64Bit_BroadcastScalarColumn_SimdChunkedAndTail()
    {
        var type = DecimalType.Numeric(18, 3);
        int n = 19;
        long[] right = new long[n];
        long[] expected = new long[n];
        long scalar = 999_999_999_999L;
        for (int i = 0; i < n; i++)
        {
            right[i] = (long)i * 100_000_000L;
            expected[i] = scalar - right[i];
        }
        long[] result = new long[n];
        SpanAddKernel.Subtract(scalar, type, right, type, result, type);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Add_32Bit_Broadcast_OverflowInSimdChunk_Throws()
    {
        var type = DecimalType.Numeric(9, 0);
        int n = 16;
        int[] left = new int[n];
        for (int i = 0; i < n; i++) left[i] = 1;
        left[10] = int.MaxValue;
        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Add(left, type, 1, type, new int[n], type));
    }
}
