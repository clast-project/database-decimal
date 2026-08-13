// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// Multiplication where precision clamping pulls the result scale below s1+s2.
/// The exact product is formed in 256 bits and the product is rescaled; the
/// earlier approach of reducing one operand's scale first could ask that
/// operand for more digits than it had.
/// </summary>
public class MultiplyClampTests
{
    [Fact]
    public void Multiply128_ClampedResultScale_DoesNotZeroTheOperand()
    {
        var operandType = DecimalType.Numeric(38, 10);
        var resultType = DecimalTypeRules.Multiply(operandType, operandType);

        // The rules clamp to NUMERIC(38,6): a 14-digit reduction, applied to an
        // 11-digit mantissa if it is taken from an operand instead of the product.
        Assert.Equal(38, resultType.Precision);
        Assert.Equal(6, resultType.Scale);

        // 1.0000000005 * 3.0000000000 = 3.0000000015 -> 3.000000 at scale 6
        var left = new Decimal128((Int128)10000000005L);
        var right = new Decimal128((Int128)30000000000L);

        var result = MultiplyKernel.Multiply(left, operandType, right, operandType, resultType);
        Assert.Equal((Int128)3000000, result.Mantissa);
    }

    [Fact]
    public void Multiply128_ClampedResultScale_MatchesTheWidenedProduct()
    {
        var operandType = DecimalType.Numeric(38, 10);
        var resultType = DecimalTypeRules.Multiply(operandType, operandType);

        // 1.5 * 1.5 = 2.25 -> 2.250000, which operand pre-reduction gets wrong.
        var left = new Decimal128((Int128)15000000000L);
        var right = new Decimal128((Int128)15000000000L);

        var narrow = MultiplyKernel.Multiply(left, operandType, right, operandType, resultType);
        var wide = MultiplyKernel.MultiplyWiden(left, operandType, right, operandType, resultType);

        Assert.Equal((Int128)2250000, narrow.Mantissa);
        Assert.Equal((Int256)2250000, wide.Mantissa);
    }

    [Theory]
    [InlineData(DecimalRounding.HalfEven, 2)]
    [InlineData(DecimalRounding.HalfUp, 3)]
    public void Multiply128_ClampedResultScale_HonoursRounding(DecimalRounding rounding, int expected)
    {
        // 0.5 * 5 = 2.5 at raw scale 2, rescaled to scale 0.
        var leftType = DecimalType.Numeric(38, 1);
        var rightType = DecimalType.Numeric(38, 1);
        var resultType = DecimalType.Numeric(38, 0);

        var result = MultiplyKernel.Multiply(
            new Decimal128((Int128)5), leftType, new Decimal128((Int128)50), rightType, resultType, rounding);

        Assert.Equal((Int128)expected, result.Mantissa);
    }

    [Fact]
    public void Multiply128_UnclampedResultScale_IsUnchanged()
    {
        var leftType = DecimalType.Numeric(20, 2);
        var rightType = DecimalType.Numeric(20, 3);
        var resultType = DecimalType.Numeric(38, 5);

        // 1.23 * 4.567 = 5.61741
        var result = MultiplyKernel.Multiply(
            new Decimal128((Int128)123), leftType, new Decimal128((Int128)4567), rightType, resultType);

        Assert.Equal((Int128)561741, result.Mantissa);
    }

    [Fact]
    public void Multiply128_OverflowingProduct_Throws()
    {
        var operandType = DecimalType.Numeric(38, 0);
        var resultType = DecimalType.Numeric(38, 1);  // scaling the product up by 10

        Int128 big = Int128.MaxValue / 2;
        Assert.Throws<OverflowException>(() =>
            MultiplyKernel.Multiply(new Decimal128(big), operandType, new Decimal128((Int128)3), operandType, resultType));
    }

    [Fact]
    public void SpanMultiply128_ClampedResultScale_MatchesScalar()
    {
        var operandType = DecimalType.Numeric(38, 10);
        var resultType = DecimalTypeRules.Multiply(operandType, operandType);

        Int128[] left = { (Int128)10000000005L, (Int128)15000000000L, (Int128)(-15000000000L) };
        Int128[] right = { (Int128)30000000000L, (Int128)15000000000L, (Int128)15000000000L };
        Int128[] result = new Int128[3];

        SpanMultiplyKernel.Multiply(left, operandType, right, operandType, result, resultType);

        for (int i = 0; i < left.Length; i++)
        {
            var scalar = MultiplyKernel.Multiply(
                new Decimal128(left[i]), operandType, new Decimal128(right[i]), operandType, resultType);
            Assert.Equal(scalar.Mantissa, result[i]);
        }

        Assert.Equal((Int128)3000000, result[0]);
        Assert.Equal((Int128)2250000, result[1]);
        Assert.Equal((Int128)(-2250000), result[2]);
    }

    /// <summary>
    /// Products that fit in 128 bits are rescaled there; this one does not, so
    /// it exercises the 256-bit rescale and the narrowing behind it.
    /// </summary>
    [Theory]
    [InlineData(DecimalRounding.HalfEven, 2000000000L)]
    [InlineData(DecimalRounding.HalfUp, 2000000001L)]
    public void Multiply128_ProductExceeds128Bits_RescalesInt256(DecimalRounding rounding, long expected)
    {
        var leftType = DecimalType.Numeric(38, 30);
        var rightType = DecimalType.Numeric(38, 0);
        var resultType = DecimalType.Numeric(38, 0);

        // 5e29 * 4000000001 = 2.0000000005e39, which is past Int128.MaxValue
        // (~1.7e38). Dropping 30 digits lands exactly on a midpoint.
        Int128 left = (Int128)5 * PowersOf10.Int128[29];
        Int128 right = (Int128)4000000001L;

        Assert.True(Int256.BigMul(left, right) > (Int256)Int128.MaxValue);

        var result = MultiplyKernel.Multiply(
            new Decimal128(left), leftType, new Decimal128(right), rightType, resultType, rounding);

        Assert.Equal((Int128)expected, result.Mantissa);
    }

    [Fact]
    public void Multiply128_RescaledProductStillTooWide_Throws()
    {
        var operandType = DecimalType.Numeric(38, 1);
        var resultType = DecimalType.Numeric(38, 0);

        // 1e30 * 1e30 = 1e60; dropping one digit still leaves 1e59.
        Int128 big = PowersOf10.Int128[30];
        Assert.Throws<OverflowException>(() =>
            MultiplyKernel.Multiply(new Decimal128(big), operandType, new Decimal128(big), operandType, resultType));
    }

    [Fact]
    public void Multiply256_ClampedResultScale_SplitsTheReductionAcrossOperands()
    {
        // No 512-bit intermediate exists, so the reduction is applied to the
        // operands — but split so neither is driven below scale 0.
        var operandType = DecimalType.Numeric(50, 10);
        var resultType = DecimalType.Numeric(50, 6);  // a 14-digit reduction

        var left = new Decimal256((Int256)10000000005L);   // 1.0000000005
        var right = new Decimal256((Int256)30000000000L);  // 3.0000000000

        var result = MultiplyKernel.Multiply(left, operandType, right, operandType, resultType);
        Assert.Equal((Int256)3000000, result.Mantissa);
    }

    [Fact]
    public void SpanMultiply256_ClampedResultScale_MatchesScalar()
    {
        var operandType = DecimalType.Numeric(50, 10);
        var resultType = DecimalType.Numeric(50, 6);

        Int256[] left = { (Int256)10000000005L, (Int256)20000000000L };
        Int256[] right = { (Int256)30000000000L, (Int256)25000000000L };
        Int256[] result = new Int256[2];

        SpanMultiplyKernel.Multiply(left, operandType, right, operandType, result, resultType);

        for (int i = 0; i < left.Length; i++)
        {
            var scalar = MultiplyKernel.Multiply(
                new Decimal256(left[i]), operandType, new Decimal256(right[i]), operandType, resultType);
            Assert.Equal(scalar.Mantissa, result[i]);
        }

        Assert.Equal((Int256)3000000, result[0]);   // 1.0000000005 * 3 -> 3.000000
        Assert.Equal((Int256)5000000, result[1]);   // 2 * 2.5 -> 5.000000
    }
}
