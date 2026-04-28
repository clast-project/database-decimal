// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

public class ModulusKernelTests
{
    // ----------------------------------------------------------------
    // Scalar — same scale
    // ----------------------------------------------------------------

    [Fact]
    public void Modulus_32Bit_SameScale()
    {
        // 10.50 % 3.00 = 1.50
        var type = DecimalType.Numeric(4, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        var left = new Decimal32(1050);  // 10.50
        var right = new Decimal32(300);  // 3.00
        var result = ModulusKernel.Modulus(left, type, right, type, resultType);

        Assert.Equal(150, result.Mantissa); // 1.50
    }

    [Fact]
    public void Modulus_32Bit_ExactDivision()
    {
        // 9.00 % 3.00 = 0.00
        var type = DecimalType.Numeric(4, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        var left = new Decimal32(900);
        var right = new Decimal32(300);
        var result = ModulusKernel.Modulus(left, type, right, type, resultType);

        Assert.Equal(0, result.Mantissa);
    }

    [Fact]
    public void Modulus_32Bit_NegativeDividend()
    {
        // -10.50 % 3.00 = -1.50 (remainder has sign of dividend)
        var type = DecimalType.Numeric(4, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        var left = new Decimal32(-1050);
        var right = new Decimal32(300);
        var result = ModulusKernel.Modulus(left, type, right, type, resultType);

        Assert.Equal(-150, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // Scalar — different scales
    // ----------------------------------------------------------------

    [Fact]
    public void Modulus_32Bit_DifferentScales()
    {
        // 10.5 (scale 1) % 3.00 (scale 2) → result scale = 2
        var leftType = DecimalType.Numeric(3, 1);
        var rightType = DecimalType.Numeric(3, 2);
        var resultType = DecimalTypeRules.Modulus(leftType, rightType);

        Assert.Equal(2, resultType.Scale);

        var left = new Decimal32(105);   // 10.5
        var right = new Decimal32(300);  // 3.00
        var result = ModulusKernel.Modulus(left, leftType, right, rightType, resultType);

        Assert.Equal(150, result.Mantissa); // 1.50
    }

    // ----------------------------------------------------------------
    // Scalar — 64-bit, 128-bit, 256-bit
    // ----------------------------------------------------------------

    [Fact]
    public void Modulus_64Bit()
    {
        var type = DecimalType.Numeric(12, 3);
        var resultType = DecimalTypeRules.Modulus(type, type);

        var left = new Decimal64(10_500);  // 10.500
        var right = new Decimal64(3_000);  // 3.000
        var result = ModulusKernel.Modulus(left, type, right, type, resultType);

        Assert.Equal(1_500L, result.Mantissa);
    }

    [Fact]
    public void Modulus_128Bit()
    {
        var type = DecimalType.Numeric(20, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        var left = new Decimal128(1050);
        var right = new Decimal128(300);
        var result = ModulusKernel.Modulus(left, type, right, type, resultType);

        Assert.Equal((Int128)150, result.Mantissa);
    }

    [Fact]
    public void Modulus_256Bit()
    {
        var type = DecimalType.Numeric(40, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        var left = new Decimal256((Int256)1050);
        var right = new Decimal256((Int256)300);
        var result = ModulusKernel.Modulus(left, type, right, type, resultType);

        Assert.Equal((Int256)150, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // Scalar — widening
    // ----------------------------------------------------------------

    [Fact]
    public void ModulusWiden_32To64()
    {
        var leftType = DecimalType.Numeric(9, 0);
        var rightType = DecimalType.Numeric(9, 5);
        var resultType = DecimalTypeRules.Modulus(leftType, rightType);

        // Left needs rescaling by 5 decimal places which may overflow 32-bit
        var left = new Decimal32(100_000);
        var right = new Decimal32(300_000);  // 3.00000
        var result = ModulusKernel.ModulusWiden(left, leftType, right, rightType, resultType);

        // 100000.00000 % 3.00000 → remainder
        long rescaledLeft = 100_000L * 100_000; // 10_000_000_000
        Assert.Equal(rescaledLeft % 300_000L, result.Mantissa);
    }

    [Fact]
    public void ModulusWiden_64To128()
    {
        var type = DecimalType.Numeric(18, 6);
        var resultType = DecimalTypeRules.Modulus(type, type);

        var left = new Decimal64(10_500_000);
        var right = new Decimal64(3_000_000);
        var result = ModulusKernel.ModulusWiden(left, type, right, type, resultType);

        Assert.Equal((Int128)1_500_000, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // Scalar — divide-by-zero
    // ----------------------------------------------------------------

    [Fact]
    public void Modulus_ByZero_Throws()
    {
        var type = DecimalType.Numeric(4, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        Assert.Throws<DivideByZeroException>(() =>
            ModulusKernel.Modulus(
                new Decimal32(100), type,
                Decimal32.Zero, type,
                resultType));
    }
}

public class SpanModulusKernelTests
{
    // ----------------------------------------------------------------
    // Column % column — same scale
    // ----------------------------------------------------------------

    [Fact]
    public void Modulus_32Bit_SameScale()
    {
        var type = DecimalType.Numeric(4, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        int[] left = [1050, 900, 725];   // 10.50, 9.00, 7.25
        int[] right = [300, 300, 200];   // 3.00, 3.00, 2.00
        int[] result = new int[3];

        SpanModulusKernel.Modulus(left, type, right, type, result, resultType);

        Assert.Equal(150, result[0]); // 10.50 % 3.00 = 1.50
        Assert.Equal(0, result[1]);   // 9.00 % 3.00 = 0.00
        Assert.Equal(125, result[2]); // 7.25 % 2.00 = 1.25
    }

    // ----------------------------------------------------------------
    // Column % column — different scales
    // ----------------------------------------------------------------

    [Fact]
    public void Modulus_32Bit_DifferentScales()
    {
        var leftType = DecimalType.Numeric(3, 1);
        var rightType = DecimalType.Numeric(3, 2);
        var resultType = DecimalTypeRules.Modulus(leftType, rightType);

        int[] left = [105, 200];    // 10.5, 20.0
        int[] right = [300, 300];   // 3.00, 3.00
        int[] result = new int[2];

        SpanModulusKernel.Modulus(left, leftType, right, rightType, result, resultType);

        Assert.Equal(150, result[0]); // 10.50 % 3.00 = 1.50
        Assert.Equal(200, result[1]); // 20.00 % 3.00 = 2.00
    }

    // ----------------------------------------------------------------
    // Column % scalar (broadcast)
    // ----------------------------------------------------------------

    [Fact]
    public void Modulus_32Bit_Broadcast()
    {
        var type = DecimalType.Numeric(4, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        int[] left = [1050, 900, 725];
        int scalar = 300;
        int[] result = new int[3];

        SpanModulusKernel.Modulus(left, type, scalar, type, result, resultType);

        Assert.Equal(150, result[0]);
        Assert.Equal(0, result[1]);
        Assert.Equal(125, result[2]);
    }

    [Fact]
    public void Modulus_64Bit_Broadcast()
    {
        var type = DecimalType.Numeric(12, 3);
        var resultType = DecimalTypeRules.Modulus(type, type);

        long[] left = [10_500, 7_250];
        long scalar = 3_000;
        long[] result = new long[2];

        SpanModulusKernel.Modulus(left, type, scalar, type, result, resultType);

        Assert.Equal(1_500L, result[0]);
        Assert.Equal(1_250L, result[1]);
    }

    // ----------------------------------------------------------------
    // Widening
    // ----------------------------------------------------------------

    [Fact]
    public void ModulusWiden_32To64()
    {
        var leftType = DecimalType.Numeric(5, 1);
        var rightType = DecimalType.Numeric(5, 3);
        var resultType = DecimalTypeRules.Modulus(leftType, rightType);

        int[] left = [105, 200];      // 10.5, 20.0
        int[] right = [3000, 3000];   // 3.000, 3.000
        long[] result = new long[2];

        SpanModulusKernel.ModulusWiden(left, leftType, right, rightType, result, resultType);

        // 10.500 % 3.000 = 1.500 → mantissa 1500
        Assert.Equal(1500L, result[0]);
        // 20.000 % 3.000 = 2.000 → mantissa 2000
        Assert.Equal(2000L, result[1]);
    }

    // ----------------------------------------------------------------
    // 128-bit and 256-bit
    // ----------------------------------------------------------------

    [Fact]
    public void Modulus_128Bit()
    {
        var type = DecimalType.Numeric(20, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        Int128[] left = [1050, 725];
        Int128[] right = [300, 200];
        Int128[] result = new Int128[2];

        SpanModulusKernel.Modulus(left, type, right, type, result, resultType);

        Assert.Equal((Int128)150, result[0]);
        Assert.Equal((Int128)125, result[1]);
    }

    [Fact]
    public void Modulus_256Bit()
    {
        var type = DecimalType.Numeric(40, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        Int256[] left = [(Int256)1050, (Int256)725];
        Int256[] right = [(Int256)300, (Int256)200];
        Int256[] result = new Int256[2];

        SpanModulusKernel.Modulus(left, type, right, type, result, resultType);

        Assert.Equal((Int256)150, result[0]);
        Assert.Equal((Int256)125, result[1]);
    }

    // ----------------------------------------------------------------
    // Edge cases
    // ----------------------------------------------------------------

    [Fact]
    public void Modulus_EmptySpans()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        SpanModulusKernel.Modulus(
            ReadOnlySpan<int>.Empty, type,
            ReadOnlySpan<int>.Empty, type,
            Span<int>.Empty, resultType);
    }

    [Fact]
    public void Modulus_MismatchedLengths_Throws()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        Assert.Throws<ArgumentException>(() =>
            SpanModulusKernel.Modulus(new int[3], type, new int[2], type, new int[3], resultType));
    }

    [Fact]
    public void Modulus_ByZero_Throws()
    {
        var type = DecimalType.Numeric(4, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        Assert.Throws<DivideByZeroException>(() =>
            SpanModulusKernel.Modulus(new int[] { 100 }, type, new int[] { 0 }, type, new int[1], resultType));
    }

    [Fact]
    public void Modulus_InPlace()
    {
        var type = DecimalType.Numeric(4, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        int[] data = [1050, 900, 725];
        int[] right = [300, 300, 200];

        SpanModulusKernel.Modulus(data, type, right, type, data, resultType);

        Assert.Equal(150, data[0]);
        Assert.Equal(0, data[1]);
        Assert.Equal(125, data[2]);
    }

    // ----------------------------------------------------------------
    // Consistency with scalar kernel
    // ----------------------------------------------------------------

    [Fact]
    public void Modulus_MatchesScalarKernel()
    {
        var type = DecimalType.Numeric(4, 2);
        var resultType = DecimalTypeRules.Modulus(type, type);

        int leftVal = 1050;
        int rightVal = 300;

        var scalarResult = ModulusKernel.Modulus(
            new Decimal32(leftVal), type,
            new Decimal32(rightVal), type,
            resultType);

        int[] spanResult = new int[1];
        SpanModulusKernel.Modulus(new[] { leftVal }, type, new[] { rightVal }, type, spanResult, resultType);

        Assert.Equal(scalarResult.Mantissa, spanResult[0]);
    }
}
