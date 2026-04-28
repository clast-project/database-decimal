// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

public class SpanDivideKernelTests
{
    // ----------------------------------------------------------------
    // Divide — column / column, widening 32→64
    // ----------------------------------------------------------------

    [Fact]
    public void Divide_32To64()
    {
        var leftType = DecimalType.Numeric(4, 2);
        var rightType = DecimalType.Numeric(2, 1);
        var resultType = DecimalTypeRules.Divide(leftType, rightType);

        int[] left = [1000, 2000];  // 10.00, 20.00
        int[] right = [20, 30];     // 2.0, 3.0
        long[] result = new long[2];

        SpanDivideKernel.Divide(left, leftType, right, rightType, result, resultType);

        // 10.00 / 2.0 = 5.0
        var d0 = new Decimal64(result[0]);
        Assert.StartsWith("5.0", d0.ToString(resultType.Scale));

        // 20.00 / 3.0 = 6.666...
        var d1 = new Decimal64(result[1]);
        Assert.StartsWith("6.666", d1.ToString(resultType.Scale));
    }

    // ----------------------------------------------------------------
    // Divide — column / column, widening 64→128
    // ----------------------------------------------------------------

    [Fact]
    public void Divide_64To128()
    {
        var leftType = DecimalType.Numeric(10, 2);
        var rightType = DecimalType.Numeric(5, 1);
        var resultType = DecimalTypeRules.Divide(leftType, rightType);

        long[] left = [10000];   // 100.00
        long[] right = [40];     // 4.0
        Int128[] result = new Int128[1];

        SpanDivideKernel.Divide(left, leftType, right, rightType, result, resultType);

        var d = new Decimal128(result[0]);
        Assert.StartsWith("25.0", d.ToString(resultType.Scale));
    }

    // ----------------------------------------------------------------
    // Divide — column / column, same width 128-bit
    // ----------------------------------------------------------------

    [Fact]
    public void Divide_128Bit_SameWidth()
    {
        var leftType = DecimalType.Numeric(20, 2);
        var rightType = DecimalType.Numeric(10, 1);
        var resultType = DecimalTypeRules.Divide(leftType, rightType);

        Int128[] left = [(Int128)10000];  // 100.00
        Int128[] right = [(Int128)20];    // 2.0
        Int128[] result = new Int128[1];

        SpanDivideKernel.Divide(left, leftType, right, rightType, result, resultType);

        var d = new Decimal128(result[0]);
        Assert.StartsWith("50.0", d.ToString(resultType.Scale));
    }

    // ----------------------------------------------------------------
    // Divide — column / scalar (broadcast)
    // ----------------------------------------------------------------

    [Fact]
    public void Divide_32To64_Broadcast()
    {
        var leftType = DecimalType.Numeric(4, 2);
        var rightType = DecimalType.Numeric(2, 1);
        var resultType = DecimalTypeRules.Divide(leftType, rightType);

        int[] left = [1000, 2000, 3000]; // 10.00, 20.00, 30.00
        int scalar = 20;                  // 2.0
        long[] result = new long[3];

        SpanDivideKernel.Divide(left, leftType, scalar, rightType, result, resultType);

        // All should be exact: 5.0, 10.0, 15.0
        var d0 = new Decimal64(result[0]);
        Assert.StartsWith("5.0", d0.ToString(resultType.Scale));

        var d1 = new Decimal64(result[1]);
        Assert.StartsWith("10.0", d1.ToString(resultType.Scale));

        var d2 = new Decimal64(result[2]);
        Assert.StartsWith("15.0", d2.ToString(resultType.Scale));
    }

    [Fact]
    public void Divide_64To128_Broadcast()
    {
        var leftType = DecimalType.Numeric(10, 2);
        var rightType = DecimalType.Numeric(5, 1);
        var resultType = DecimalTypeRules.Divide(leftType, rightType);

        long[] left = [10000, 20000]; // 100.00, 200.00
        long scalar = 40;             // 4.0
        Int128[] result = new Int128[2];

        SpanDivideKernel.Divide(left, leftType, scalar, rightType, result, resultType);

        var d0 = new Decimal128(result[0]);
        Assert.StartsWith("25.0", d0.ToString(resultType.Scale));

        var d1 = new Decimal128(result[1]);
        Assert.StartsWith("50.0", d1.ToString(resultType.Scale));
    }

    // ----------------------------------------------------------------
    // Divide — scalar / column (non-commutative broadcast)
    // ----------------------------------------------------------------

    [Fact]
    public void Divide_ScalarDivColumn_32To64()
    {
        var leftType = DecimalType.Numeric(4, 2);
        var rightType = DecimalType.Numeric(2, 1);
        var resultType = DecimalTypeRules.Divide(leftType, rightType);

        int scalar = 1000;                // 10.00
        int[] right = [20, 40, 50];       // 2.0, 4.0, 5.0
        long[] result = new long[3];

        SpanDivideKernel.Divide(scalar, leftType, right, rightType, result, resultType);

        // 10 / 2 = 5, 10 / 4 = 2.5, 10 / 5 = 2
        var d0 = new Decimal64(result[0]);
        Assert.StartsWith("5.0", d0.ToString(resultType.Scale));

        var d1 = new Decimal64(result[1]);
        Assert.StartsWith("2.5", d1.ToString(resultType.Scale));

        var d2 = new Decimal64(result[2]);
        Assert.StartsWith("2.0", d2.ToString(resultType.Scale));
    }

    [Fact]
    public void Divide_ScalarDivColumn_128Bit()
    {
        var leftType = DecimalType.Numeric(20, 2);
        var rightType = DecimalType.Numeric(10, 1);
        var resultType = DecimalTypeRules.Divide(leftType, rightType);

        Int128 scalar = 10000;            // 100.00
        Int128[] right = [(Int128)20, (Int128)50]; // 2.0, 5.0
        Int128[] result = new Int128[2];

        SpanDivideKernel.Divide(scalar, leftType, right, rightType, result, resultType);

        var d0 = new Decimal128(result[0]);
        Assert.StartsWith("50.0", d0.ToString(resultType.Scale));

        var d1 = new Decimal128(result[1]);
        Assert.StartsWith("20.0", d1.ToString(resultType.Scale));
    }

    // ----------------------------------------------------------------
    // Edge cases
    // ----------------------------------------------------------------

    [Fact]
    public void Divide_EmptySpans()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Divide(type, type);

        SpanDivideKernel.Divide(
            ReadOnlySpan<int>.Empty, type,
            ReadOnlySpan<int>.Empty, type,
            Span<long>.Empty, resultType);
    }

    [Fact]
    public void Divide_MismatchedLengths_Throws()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Divide(type, type);

        Assert.Throws<ArgumentException>(() =>
            SpanDivideKernel.Divide(new int[3], type, new int[2], type, new long[3], resultType));
    }

    [Fact]
    public void Divide_ByZero_Throws()
    {
        var leftType = DecimalType.Numeric(4, 2);
        var rightType = DecimalType.Numeric(2, 1);
        var resultType = DecimalTypeRules.Divide(leftType, rightType);

        int[] left = [1000];
        int[] right = [0];
        long[] result = new long[1];

        Assert.Throws<DivideByZeroException>(() =>
            SpanDivideKernel.Divide(left, leftType, right, rightType, result, resultType));
    }

    // ----------------------------------------------------------------
    // Consistency with scalar kernel
    // ----------------------------------------------------------------

    [Fact]
    public void Divide_MatchesScalarKernel()
    {
        var leftType = DecimalType.Numeric(4, 2);
        var rightType = DecimalType.Numeric(2, 1);
        var resultType = DecimalTypeRules.Divide(leftType, rightType);

        int leftVal = 1000;  // 10.00
        int rightVal = 30;   // 3.0

        // Scalar kernel
        var scalarResult = DivideKernel.Divide(
            new Decimal32(leftVal), leftType,
            new Decimal32(rightVal), rightType,
            resultType);

        // Span kernel
        int[] left = [leftVal];
        int[] right = [rightVal];
        long[] spanResult = new long[1];
        SpanDivideKernel.Divide(left, leftType, right, rightType, spanResult, resultType);

        Assert.Equal(scalarResult.Mantissa, spanResult[0]);
    }
}
