// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

public class SpanMultiplyKernelTests
{
    // ----------------------------------------------------------------
    // Multiply — column * column, widening 32→64
    // ----------------------------------------------------------------

    [Fact]
    public void Multiply_32To64_SameScale()
    {
        var leftType = DecimalType.Numeric(4, 2);
        var rightType = DecimalType.Numeric(3, 1);
        var resultType = DecimalTypeRules.Multiply(leftType, rightType);
        // result: prec=8, scale=3

        int[] left = [1250, 200];   // 12.50, 2.00
        int[] right = [35, 100];    // 3.5, 10.0
        long[] result = new long[2];

        SpanMultiplyKernel.Multiply(left, leftType, right, rightType, result, resultType);

        Assert.Equal(43750L, result[0]); // 12.50 * 3.5 = 43.750
        Assert.Equal(20000L, result[1]); // 2.00 * 10.0 = 20.000
    }

    [Fact]
    public void Multiply_32To64_WithScaleReduction()
    {
        var leftType = DecimalType.Numeric(5, 4);
        var rightType = DecimalType.Numeric(5, 4);
        var resultType = DecimalTypeRules.Multiply(leftType, rightType);
        // raw scale = 8, result might have different scale after clamping

        int[] left = [25000];   // 2.5000
        int[] right = [40000];  // 4.0000
        long[] result = new long[1];

        SpanMultiplyKernel.Multiply(left, leftType, right, rightType, result, resultType);

        // 2.5 * 4.0 = 10.0
        var d = new Decimal64(result[0]);
        string formatted = d.ToString(resultType.Scale);
        Assert.StartsWith("10.0", formatted);
    }

    // ----------------------------------------------------------------
    // Multiply — column * column, widening 64→128
    // ----------------------------------------------------------------

    [Fact]
    public void Multiply_64To128()
    {
        var leftType = DecimalType.Numeric(10, 2);
        var rightType = DecimalType.Numeric(10, 2);
        var resultType = DecimalTypeRules.Multiply(leftType, rightType);

        long[] left = [10_000_00, 50_000_00];   // 10,000.00, 50,000.00
        long[] right = [200_00, 300_00];         // 200.00, 300.00
        Int128[] result = new Int128[2];

        SpanMultiplyKernel.Multiply(left, leftType, right, rightType, result, resultType);

        // 10,000 * 200 = 2,000,000 with scale 4
        Assert.Equal((Int128)10_000_00 * 200_00, result[0]);
        Assert.Equal((Int128)50_000_00 * 300_00, result[1]);
    }

    // ----------------------------------------------------------------
    // Multiply — column * column, widening 128→256
    // ----------------------------------------------------------------

    [Fact]
    public void MultiplyWiden_128To256()
    {
        var leftType = DecimalType.Numeric(20, 5);
        var rightType = DecimalType.Numeric(20, 5);
        var resultType = DecimalType.Numeric(41, 10);

        Int128[] left = [100_00000];   // 100.00000
        Int128[] right = [200_00000];  // 200.00000
        Int256[] result = new Int256[1];

        SpanMultiplyKernel.MultiplyWiden(left, leftType, right, rightType, result, resultType);

        // 100 * 200 = 20000, scale 10
        Assert.Equal(Int256.BigMul(100_00000, 200_00000), result[0]);
    }

    // ----------------------------------------------------------------
    // Multiply — column * column, same width 128-bit
    // ----------------------------------------------------------------

    [Fact]
    public void Multiply_128Bit_SameWidth()
    {
        var leftType = DecimalType.Numeric(20, 5);
        var rightType = DecimalType.Numeric(18, 5);
        var resultType = DecimalTypeRules.Multiply(leftType, rightType);

        Int128[] left = [100_00000, 200_00000];
        Int128[] right = [300_00000, 400_00000];
        Int128[] result = new Int128[2];

        SpanMultiplyKernel.Multiply(left, leftType, right, rightType, result, resultType);

        // Verify products are reasonable
        Assert.True(result[0] > 0);
        Assert.True(result[1] > 0);
        Assert.True(result[1] > result[0]); // 200*400 > 100*300
    }

    // ----------------------------------------------------------------
    // Multiply — column * scalar (broadcast)
    // ----------------------------------------------------------------

    [Fact]
    public void Multiply_32To64_Broadcast()
    {
        var leftType = DecimalType.Numeric(5, 2);
        var rightType = DecimalType.Numeric(3, 1);
        var resultType = DecimalTypeRules.Multiply(leftType, rightType);

        int[] left = [100, 200, 300]; // 1.00, 2.00, 3.00
        int scalar = 25;              // 2.5
        long[] result = new long[3];

        SpanMultiplyKernel.Multiply(left, leftType, scalar, rightType, result, resultType);

        // 1.00 * 2.5 = 2.500 (mantissa 2500), 2.00 * 2.5 = 5.000, 3.00 * 2.5 = 7.500
        Assert.Equal(2500L, result[0]);
        Assert.Equal(5000L, result[1]);
        Assert.Equal(7500L, result[2]);
    }

    [Fact]
    public void Multiply_64To128_Broadcast()
    {
        var leftType = DecimalType.Numeric(10, 2);
        var rightType = DecimalType.Numeric(10, 2);
        var resultType = DecimalTypeRules.Multiply(leftType, rightType);

        long[] left = [10000, 20000];
        long scalar = 300;
        Int128[] result = new Int128[2];

        SpanMultiplyKernel.Multiply(left, leftType, scalar, rightType, result, resultType);

        Assert.Equal((Int128)10000 * 300, result[0]);
        Assert.Equal((Int128)20000 * 300, result[1]);
    }

    // ----------------------------------------------------------------
    // Empty spans
    // ----------------------------------------------------------------

    [Fact]
    public void Multiply_EmptySpans()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Multiply(type, type);

        SpanMultiplyKernel.Multiply(
            ReadOnlySpan<int>.Empty, type,
            ReadOnlySpan<int>.Empty, type,
            Span<long>.Empty, resultType);
    }

    // ----------------------------------------------------------------
    // Validation
    // ----------------------------------------------------------------

    [Fact]
    public void Multiply_MismatchedLengths_Throws()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Multiply(type, type);

        Assert.Throws<ArgumentException>(() =>
            SpanMultiplyKernel.Multiply(new int[3], type, new int[2], type, new long[3], resultType));
    }

    // ----------------------------------------------------------------
    // SIMD chunked-path coverage for the 32x32 -> 64 no-rescale path.
    // On AVX2 the helper processes 4 ints per vpmuldq; this test uses
    // a length that exercises both the SIMD chunk loop and the scalar
    // tail and verifies element-wise equality with a scalar reference.
    // ----------------------------------------------------------------

    [Fact]
    public void Multiply_32To64_SameScale_SimdChunkedAndTail()
    {
        var leftType = DecimalType.Numeric(4, 2);
        var rightType = DecimalType.Numeric(3, 1);
        // Result scale (3) equals raw scale (2+1=3), so the no-rescale SIMD path runs.
        var resultType = DecimalType.Numeric(8, 3);
        int n = 23;
        int[] left = new int[n];
        int[] right = new int[n];
        long[] expected = new long[n];
        for (int i = 0; i < n; i++)
        {
            left[i] = 1250 + i;
            right[i] = 35 + 2 * i;
            expected[i] = (long)left[i] * right[i];
        }
        long[] result = new long[n];
        SpanMultiplyKernel.Multiply(left, leftType, right, rightType, result, resultType);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Multiply_32To64_Broadcast_SimdChunkedAndTail()
    {
        // Broadcast scalar path with no rescale — exercises the vpmuldq SIMD
        // helper with a Vector256<int> built from the scalar.
        var leftType = DecimalType.Numeric(4, 2);
        var rightType = DecimalType.Numeric(3, 1);
        var resultType = DecimalType.Numeric(8, 3);
        int n = 23;
        int[] left = new int[n];
        int scalar = -42;
        long[] expected = new long[n];
        for (int i = 0; i < n; i++)
        {
            left[i] = 1250 + i;
            expected[i] = (long)left[i] * scalar;
        }
        long[] result = new long[n];
        SpanMultiplyKernel.Multiply(left, leftType, scalar, rightType, result, resultType);
        Assert.Equal(expected, result);
    }
}
