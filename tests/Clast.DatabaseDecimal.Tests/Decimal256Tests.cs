// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

public class Decimal256Tests
{
    // ----------------------------------------------------------------
    // Value type basics
    // ----------------------------------------------------------------

    [Fact]
    public void Decimal256_ToString_WithScale()
    {
        var v = new Decimal256((Int256)12345);
        Assert.Equal("123.45", v.ToString(2));
        Assert.Equal("12.345", v.ToString(3));
        Assert.Equal("12345", v.ToString(0));
    }

    [Fact]
    public void Decimal256_Negative_ToString()
    {
        var v = new Decimal256((Int256)(-4200));
        Assert.Equal("-42.00", v.ToString(2));
    }

    [Fact]
    public void Decimal256_Zero_ToString()
    {
        Assert.Equal("0.00", Decimal256.Zero.ToString(2));
    }

    [Fact]
    public void Decimal256_Comparison()
    {
        var a = new Decimal256((Int256)100);
        var b = new Decimal256((Int256)200);
        Assert.True(a < b);
        Assert.True(b > a);
        Assert.True(a != b);
    }

    [Fact]
    public void Decimal256_Widening_From_Decimal128()
    {
        Decimal128 narrow = new(42);
        Decimal256 wide = narrow; // implicit
        Assert.Equal((Int256)42, wide.Mantissa);
    }

    // ----------------------------------------------------------------
    // DecimalType width for 256-bit tier
    // ----------------------------------------------------------------

    [Fact]
    public void DecimalType_Width256_ForHighPrecision()
    {
        var dt = DecimalType.Numeric(39, 10);
        Assert.Equal(DecimalWidth.W256, dt.Width);
    }

    [Fact]
    public void DecimalType_Width256_MaxPrecision()
    {
        var dt = DecimalType.Numeric(76, 30);
        Assert.Equal(DecimalWidth.W256, dt.Width);
    }

    // ----------------------------------------------------------------
    // Arithmetic via kernels
    // ----------------------------------------------------------------

    [Fact]
    public void Add_256Bit()
    {
        var type = DecimalType.Numeric(40, 5);
        var resultType = DecimalTypeRules.Add(type, type);

        Assert.Equal(DecimalWidth.W256, type.Width);

        var left = new Decimal256((Int256)150_00000);   // 1500.00000
        var right = new Decimal256((Int256)225_00000);   // 2250.00000

        var result = AddKernel.Add(left, type, right, type, resultType);
        Assert.Equal((Int256)375_00000, result.Mantissa);
    }

    [Fact]
    public void Add_Widening_128To256()
    {
        var type = DecimalType.Numeric(38, 10);
        var resultType = DecimalTypeRules.Add(type, type);

        // Result precision = max(10,10) + max(28,28) + 1 = 39, which is > 38
        // Clamped to 38 by DecimalTypeRules
        // But for a true widening case, use types that produce > 38 precision
        var leftType = DecimalType.Numeric(38, 0);
        var rightType = DecimalType.Numeric(38, 0);
        var wideResultType = DecimalType.Numeric(39, 0); // manually specify 256-bit result

        Assert.Equal(DecimalWidth.W256, wideResultType.Width);

        var left = new Decimal128(Int128.MaxValue / 2);
        var right = new Decimal128(Int128.MaxValue / 2);

        var result = AddKernel.AddWiden(left, leftType, right, rightType, wideResultType);
        var expected = (Int256)(Int128.MaxValue / 2) + (Int256)(Int128.MaxValue / 2);
        Assert.Equal(expected, result.Mantissa);
    }

    [Fact]
    public void Subtract_256Bit()
    {
        var type = DecimalType.Numeric(40, 2);
        var resultType = DecimalTypeRules.Subtract(type, type);

        var left = new Decimal256((Int256)50000);  // 500.00
        var right = new Decimal256((Int256)17500);  // 175.00
        var result = AddKernel.Subtract(left, type, right, type, resultType);
        Assert.Equal((Int256)32500, result.Mantissa); // 325.00
    }

    [Fact]
    public void Subtract_Widening_128To256()
    {
        var leftType = DecimalType.Numeric(38, 0);
        var rightType = DecimalType.Numeric(38, 0);
        var wideResultType = DecimalType.Numeric(39, 0); // 256-bit result

        Assert.Equal(DecimalWidth.W256, wideResultType.Width);

        var left = new Decimal128(Int128.MaxValue / 2);
        var right = new Decimal128(-(Int128.MaxValue / 2));

        var result = AddKernel.SubtractWiden(left, leftType, right, rightType, wideResultType);
        var expected = (Int256)(Int128.MaxValue / 2) - (Int256)(-(Int128.MaxValue / 2));
        Assert.Equal(expected, result.Mantissa);

        // The by-hand workaround: promote both operands and subtract at 256-bit.
        var byHand = AddKernel.Subtract(
            (Decimal256)left, leftType, (Decimal256)right, rightType, wideResultType);
        Assert.Equal(byHand.Mantissa, result.Mantissa);
    }

    [Fact]
    public void Multiply_Widening_128To256()
    {
        var leftType = DecimalType.Numeric(20, 5);
        var rightType = DecimalType.Numeric(20, 5);
        // prec = 20+20+1 = 41, scale = 10 => clamped to 38, scale 7
        // But we can also test with a 256-bit result type explicitly
        var resultType = DecimalType.Numeric(41, 10);

        Assert.Equal(DecimalWidth.W256, resultType.Width);

        // 100.00000 * 200.00000 = 20000.0000000000
        var left = new Decimal128(100_00000);
        var right = new Decimal128(200_00000);
        var result = MultiplyKernel.MultiplyWiden(left, leftType, right, rightType, resultType);

        Assert.Equal((Int256)20000_00000_00000L, result.Mantissa);
        Assert.Equal("20000.0000000000", result.ToString(resultType.Scale));
    }

    [Fact]
    public void Divide_Widening_128To256()
    {
        var leftType = DecimalType.Numeric(20, 2);
        var rightType = DecimalType.Numeric(20, 1);
        // Use a 256-bit result to test the widening divide path
        var resultType = DecimalType.Numeric(40, 8);

        var left = new Decimal128(1000);   // 10.00
        var right = new Decimal128(30);    // 3.0

        var result = DivideKernel.DivideWiden(left, leftType, right, rightType, resultType);
        string formatted = result.ToString(resultType.Scale);
        Assert.StartsWith("3.33333333", formatted);
    }

    [Fact]
    public void Divide_Exact_256Bit()
    {
        var type = DecimalType.Numeric(40, 2);
        var resultType = DecimalType.Numeric(50, 8);

        var left = new Decimal256((Int256)1000);  // 10.00
        var right = new Decimal256((Int256)200);   // 2.00

        var result = DivideKernel.Divide(left, type, right, type, resultType);
        string formatted = result.ToString(resultType.Scale);
        Assert.StartsWith("5.0", formatted);
    }

    // ----------------------------------------------------------------
    // Powers of 10 for Int256
    // ----------------------------------------------------------------

    [Fact]
    public void PowersOf10_Int256_FirstFew()
    {
        Assert.Equal(Int256.One, PowersOf10.Int256[0]);
        Assert.Equal((Int256)10, PowersOf10.Int256[1]);
        Assert.Equal((Int256)100, PowersOf10.Int256[2]);
        Assert.Equal((Int256)1000, PowersOf10.Int256[3]);
    }

    [Fact]
    public void PowersOf10_Int256_MatchesInt128_UpTo38()
    {
        for (int i = 0; i <= 38; i++)
        {
            Int256 expected256 = (Int256)PowersOf10.Int128[i];
            Assert.Equal(expected256, PowersOf10.Int256[i]);
        }
    }

    [Fact]
    public void PowersOf10_Int256_Has77Elements()
    {
        // 10^0 through 10^76
        Assert.Equal(77, PowersOf10.Int256.Length);
    }

    [Fact]
    public void PowersOf10_Int256_Pow76_FitsInInt256()
    {
        // 10^76 should not be negative (should fit in 255 bits)
        Assert.False(Int256.IsNegative(PowersOf10.Int256[76]));
    }

    // ----------------------------------------------------------------
    // ScaleHelper for Int256
    // ----------------------------------------------------------------

    [Fact]
    public void ScaleHelper_Rescale256_ScaleUp()
    {
        Int256 mantissa = (Int256)42;
        Int256 result = ScaleHelper.Rescale256(mantissa, 0, 3);
        Assert.Equal((Int256)42000, result);
    }

    [Fact]
    public void ScaleHelper_Rescale256_ScaleDown()
    {
        Int256 mantissa = (Int256)42500;
        Int256 result = ScaleHelper.Rescale256(mantissa, 3, 1);
        // 42500 / 100 = 425 (exact)
        Assert.Equal((Int256)425, result);
    }

    [Fact]
    public void ScaleHelper_Rescale256_NoChange()
    {
        Int256 mantissa = (Int256)42;
        Assert.Equal(mantissa, ScaleHelper.Rescale256(mantissa, 5, 5));
    }

    [Fact]
    public void ScaleHelper_Widen128To256()
    {
        Int128 mantissa = 42;
        Int256 result = ScaleHelper.Widen128To256(mantissa, 0, 3);
        Assert.Equal((Int256)42000, result);
    }

    [Fact]
    public void ScaleHelper_DivideRoundHalfEven_256()
    {
        // Banker's rounding: 2.5 rounds to 2, 3.5 rounds to 4
        Assert.Equal((Int256)2, ScaleHelper.DivideRound((Int256)25, (Int256)10, DecimalRounding.HalfEven));
        Assert.Equal((Int256)4, ScaleHelper.DivideRound((Int256)35, (Int256)10, DecimalRounding.HalfEven));
        Assert.Equal((Int256)3, ScaleHelper.DivideRound((Int256)26, (Int256)10, DecimalRounding.HalfEven));
    }

    [Fact]
    public void ScaleHelper_DivideRoundHalfUp_256()
    {
        // Half away from zero: 2.5 rounds to 3, 3.5 rounds to 4
        Assert.Equal((Int256)3, ScaleHelper.DivideRound((Int256)25, (Int256)10, DecimalRounding.HalfUp));
        Assert.Equal((Int256)4, ScaleHelper.DivideRound((Int256)35, (Int256)10, DecimalRounding.HalfUp));
        Assert.Equal((Int256)(-3), ScaleHelper.DivideRound((Int256)(-25), (Int256)10, DecimalRounding.HalfUp));
    }
}
