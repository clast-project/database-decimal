// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

public class ArithmeticTests
{
    // --- Addition ---

    [Fact]
    public void Add_SameScale_32Bit()
    {
        // 1.50 + 2.25 = 3.75 (all NUMERIC(5,2))
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        var left = new Decimal32(150);   // 1.50
        var right = new Decimal32(225);  // 2.25

        var result = AddKernel.Add(left, type, right, type, resultType);
        Assert.Equal(375, result.Mantissa);
        Assert.Equal("3.75", result.ToString(resultType.Scale));
    }

    [Fact]
    public void Add_DifferentScales_32Bit()
    {
        // 1.50 (NUMERIC(5,2)) + 2.125 (NUMERIC(6,3)) = 3.625 (NUMERIC(7,3))
        var leftType = DecimalType.Numeric(5, 2);
        var rightType = DecimalType.Numeric(6, 3);
        var resultType = DecimalTypeRules.Add(leftType, rightType);

        var left = new Decimal32(150);    // 1.50
        var right = new Decimal32(2125);  // 2.125

        var result = AddKernel.Add(left, leftType, right, rightType, resultType);
        Assert.Equal(3625, result.Mantissa); // 1.500 + 2.125 = 3.625
        Assert.Equal("3.625", result.ToString(resultType.Scale));
    }

    [Fact]
    public void Add_Widening_32To64()
    {
        // Two NUMERIC(9,2) values whose sum exceeds 32-bit
        var type = DecimalType.Numeric(9, 2);
        var resultType = DecimalTypeRules.Add(type, type); // NUMERIC(10,2) => 64-bit

        Assert.Equal(DecimalWidth.W64, resultType.Width);

        var left = new Decimal32(999_999_999);  // 9,999,999.99
        var right = new Decimal32(999_999_999); // 9,999,999.99

        var result = AddKernel.AddWiden(left, type, right, type, resultType);
        Assert.Equal(1_999_999_998L, result.Mantissa); // 19,999,999.98
    }

    // --- Subtraction ---

    [Fact]
    public void Subtract_Basic()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Subtract(type, type);

        var left = new Decimal32(500);   // 5.00
        var right = new Decimal32(175);  // 1.75

        var result = AddKernel.Subtract(left, type, right, type, resultType);
        Assert.Equal(325, result.Mantissa); // 3.25
    }

    [Fact]
    public void Subtract_NegativeResult()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Subtract(type, type);

        var left = new Decimal32(100);   // 1.00
        var right = new Decimal32(250);  // 2.50

        var result = AddKernel.Subtract(left, type, right, type, resultType);
        Assert.Equal(-150, result.Mantissa); // -1.50
        Assert.Equal("-1.50", result.ToString(resultType.Scale));
    }

    [Fact]
    public void Subtract_Widening_32To64()
    {
        // Two NUMERIC(9,2) values whose difference exceeds 32-bit
        var type = DecimalType.Numeric(9, 2);
        var resultType = DecimalTypeRules.Subtract(type, type); // NUMERIC(10,2) => 64-bit

        Assert.Equal(DecimalWidth.W64, resultType.Width);

        var left = new Decimal32(999_999_999);   // 9,999,999.99
        var right = new Decimal32(-999_999_999); // -9,999,999.99

        var result = AddKernel.SubtractWiden(left, type, right, type, resultType);
        Assert.Equal(1_999_999_998L, result.Mantissa); // 19,999,999.98
    }

    [Fact]
    public void Subtract_Widening_64To128()
    {
        var type = DecimalType.Numeric(18, 0);
        var resultType = DecimalTypeRules.Subtract(type, type); // NUMERIC(19,0) => 128-bit

        Assert.Equal(DecimalWidth.W128, resultType.Width);

        var left = new Decimal64(long.MaxValue / 2);
        var right = new Decimal64(-(long.MaxValue / 2));

        var result = AddKernel.SubtractWiden(left, type, right, type, resultType);
        Assert.Equal((Int128)(long.MaxValue / 2) - -(Int128)(long.MaxValue / 2), result.Mantissa);
    }

    [Fact]
    public void SubtractWiden_MatchesPromotingBothOperandsFirst()
    {
        // The workaround SubtractWiden replaces: promote to the wider tier by
        // hand and subtract there. Rescaling is monotone in the mantissa, so the
        // two must agree — including when the operands carry different scales.
        var leftType = DecimalType.Numeric(9, 2);
        var rightType = DecimalType.Numeric(9, 4);
        var resultType = DecimalTypeRules.Subtract(leftType, rightType); // NUMERIC(12,4)

        Assert.Equal(DecimalWidth.W64, resultType.Width);

        int[] mantissas = [0, 1, -1, 12_345, -12_345, 999_999_999, -999_999_999];
        foreach (int l in mantissas)
        {
            foreach (int r in mantissas)
            {
                var widened = AddKernel.SubtractWiden(
                    new Decimal32(l), leftType, new Decimal32(r), rightType, resultType);
                var byHand = AddKernel.Subtract(
                    new Decimal64(l), leftType, new Decimal64(r), rightType, resultType);

                Assert.Equal(byHand.Mantissa, widened.Mantissa);
            }
        }
    }

    [Fact]
    public void SubtractWiden_PastResultPrecision_Throws()
    {
        var type = DecimalType.Numeric(9, 0);

        // The difference needs 10 digits, but the result type allows 9.
        Assert.Throws<OverflowException>(() => AddKernel.SubtractWiden(
            new Decimal32(999_999_999), type, new Decimal32(-1), type, type));
    }

    [Fact]
    public void SubtractWiden_PastResultPrecision_Ignore_DoesNotThrow()
    {
        var type = DecimalType.Numeric(9, 0);

        var result = AddKernel.SubtractWiden(
            new Decimal32(999_999_999), type, new Decimal32(-1), type, type,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);

        Assert.Equal(1_000_000_000L, result.Mantissa);
    }

    // --- Multiplication ---

    [Fact]
    public void Multiply_32To64()
    {
        // 12.50 (NUMERIC(4,2)) * 3.5 (NUMERIC(3,1)) = 43.750 => NUMERIC(8,3)
        var leftType = DecimalType.Numeric(4, 2);
        var rightType = DecimalType.Numeric(3, 1);
        var resultType = DecimalTypeRules.Multiply(leftType, rightType);

        Assert.Equal(8, resultType.Precision);
        Assert.Equal(3, resultType.Scale);

        var left = new Decimal32(1250);  // 12.50
        var right = new Decimal32(35);   // 3.5

        var result = MultiplyKernel.Multiply(left, leftType, right, rightType, resultType);
        Assert.Equal(43750L, result.Mantissa); // 43.750
        Assert.Equal("43.750", result.ToString(resultType.Scale));
    }

    [Fact]
    public void Multiply_ScaleReduction_64To128()
    {
        // Large precision values where scale gets clamped
        var leftType = DecimalType.Numeric(18, 6);
        var rightType = DecimalType.Numeric(18, 6);
        var resultType = DecimalTypeRules.Multiply(leftType, rightType);

        // prec=37, scale=12 (no clamping needed at 38)
        Assert.Equal(37, resultType.Precision);
        Assert.Equal(12, resultType.Scale);
    }

    // --- Division ---

    [Fact]
    public void Divide_Basic()
    {
        // 10.00 / 3.0 using NUMERIC(4,2) / NUMERIC(2,1)
        var leftType = DecimalType.Numeric(4, 2);
        var rightType = DecimalType.Numeric(2, 1);
        var resultType = DecimalTypeRules.Divide(leftType, rightType);

        var left = new Decimal32(1000);  // 10.00
        var right = new Decimal32(30);   // 3.0

        var result = DivideKernel.Divide(left, leftType, right, rightType, resultType);

        // 10.00 / 3.0 = 3.333333... with result scale
        // result mantissa = 1000 * 10^(scale - 2 + 1) / 30
        string formatted = result.ToString(resultType.Scale);
        Assert.StartsWith("3.333333", formatted);
    }

    [Fact]
    public void Divide_Exact()
    {
        // 10.0 / 2.0 = 5.0
        var leftType = DecimalType.Numeric(3, 1);
        var rightType = DecimalType.Numeric(3, 1);
        var resultType = DecimalTypeRules.Divide(leftType, rightType);

        var left = new Decimal32(100);   // 10.0
        var right = new Decimal32(20);   // 2.0

        var result = DivideKernel.Divide(left, leftType, right, rightType, resultType);
        string formatted = result.ToString(resultType.Scale);
        // Should be exactly 5.000000... with the appropriate scale
        Assert.StartsWith("5.0", formatted);
    }

    // --- Value type basics ---

    [Fact]
    public void Decimal32_ToString_WithScale()
    {
        var v = new Decimal32(12345);
        Assert.Equal("123.45", v.ToString(2));
        Assert.Equal("12.345", v.ToString(3));
        Assert.Equal("12345", v.ToString(0));
    }

    [Fact]
    public void Decimal32_Negative_ToString()
    {
        var v = new Decimal32(-4200);
        Assert.Equal("-42.00", v.ToString(2));
    }

    [Fact]
    public void Decimal32_Zero_ToString()
    {
        var v = Decimal32.Zero;
        Assert.Equal("0.00", v.ToString(2));
    }

    [Fact]
    public void Decimal32_FromInteger()
    {
        var v = Decimal32.FromInteger(42, 2);
        Assert.Equal(4200, v.Mantissa);
        Assert.Equal("42.00", v.ToString(2));
    }

    [Fact]
    public void Decimal32_Comparison()
    {
        var a = new Decimal32(100);
        var b = new Decimal32(200);

        Assert.True(a < b);
        Assert.True(b > a);
        Assert.True(a <= b);
        Assert.True(a != b);
        Assert.False(a == b);
    }

    [Fact]
    public void Decimal32_Widening_To_Decimal64()
    {
        Decimal32 narrow = new(42);
        Decimal64 wide = narrow; // implicit conversion
        Assert.Equal(42L, wide.Mantissa);
    }

    [Fact]
    public void Decimal64_Narrowing_To_Decimal32()
    {
        Decimal64 wide = new(42L);
        Decimal32 narrow = (Decimal32)wide; // explicit conversion
        Assert.Equal(42, narrow.Mantissa);
    }

    [Fact]
    public void Decimal64_Narrowing_Overflow_Throws()
    {
        Decimal64 wide = new(long.MaxValue);
        Assert.Throws<OverflowException>(() => (Decimal32)wide);
    }
}
