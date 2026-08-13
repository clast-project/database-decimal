// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Text;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// Rounding-mode coverage for every path that discards digits.
/// The HalfUp expectations are the values Spark and SQL Server produce.
/// </summary>
public class RoundingModeTests
{
    // ================================================================
    // Rescale — the shared "drop digits" primitive
    // ================================================================

    [Theory]
    // mantissa, fromScale, toScale, halfEven, halfUp
    [InlineData(25, 1, 0, 2, 3)]      //  2.5
    [InlineData(-25, 1, 0, -2, -3)]   // -2.5
    [InlineData(35, 1, 0, 4, 4)]      //  3.5 — both round to 4
    [InlineData(-35, 1, 0, -4, -4)]
    [InlineData(145, 2, 1, 14, 15)]   //  1.45
    [InlineData(-145, 2, 1, -14, -15)]
    [InlineData(155, 2, 1, 16, 16)]   //  1.55
    [InlineData(26, 1, 0, 3, 3)]      //  2.6 — above the midpoint either way
    [InlineData(24, 1, 0, 2, 2)]      //  2.4 — below the midpoint either way
    [InlineData(-24, 1, 0, -2, -2)]
    [InlineData(2500, 3, 0, 2, 3)]    //  2.500 — midpoint with trailing zeros
    [InlineData(2501, 3, 0, 3, 3)]    //  2.501 — just above the midpoint
    [InlineData(2499, 3, 0, 2, 2)]    //  2.499 — just below
    [InlineData(0, 1, 0, 0, 0)]
    [InlineData(5, 1, 0, 0, 1)]       //  0.5 — rounds to even zero, or up to 1
    [InlineData(-5, 1, 0, 0, -1)]
    public void Rescale_AllWidths(int mantissa, int fromScale, int toScale, int halfEven, int halfUp)
    {
        Assert.Equal(halfEven, ScaleHelper.Rescale32(mantissa, fromScale, toScale, DecimalRounding.HalfEven));
        Assert.Equal(halfUp, ScaleHelper.Rescale32(mantissa, fromScale, toScale, DecimalRounding.HalfUp));

        Assert.Equal(halfEven, ScaleHelper.Rescale64(mantissa, fromScale, toScale, DecimalRounding.HalfEven));
        Assert.Equal(halfUp, ScaleHelper.Rescale64(mantissa, fromScale, toScale, DecimalRounding.HalfUp));

        Assert.Equal((Int128)halfEven, ScaleHelper.Rescale128(mantissa, fromScale, toScale, DecimalRounding.HalfEven));
        Assert.Equal((Int128)halfUp, ScaleHelper.Rescale128(mantissa, fromScale, toScale, DecimalRounding.HalfUp));

        Assert.Equal((Int256)halfEven, ScaleHelper.Rescale256((Int256)mantissa, fromScale, toScale, DecimalRounding.HalfEven));
        Assert.Equal((Int256)halfUp, ScaleHelper.Rescale256((Int256)mantissa, fromScale, toScale, DecimalRounding.HalfUp));

        Assert.Equal(halfEven, ScaleHelper.Widen32To64(mantissa, fromScale, toScale, DecimalRounding.HalfEven));
        Assert.Equal(halfUp, ScaleHelper.Widen32To64(mantissa, fromScale, toScale, DecimalRounding.HalfUp));

        Assert.Equal((Int128)halfEven, ScaleHelper.Widen64To128(mantissa, fromScale, toScale, DecimalRounding.HalfEven));
        Assert.Equal((Int128)halfUp, ScaleHelper.Widen64To128(mantissa, fromScale, toScale, DecimalRounding.HalfUp));

        Assert.Equal((Int256)halfEven, ScaleHelper.Widen128To256(mantissa, fromScale, toScale, DecimalRounding.HalfEven));
        Assert.Equal((Int256)halfUp, ScaleHelper.Widen128To256(mantissa, fromScale, toScale, DecimalRounding.HalfUp));
    }

    [Fact]
    public void Rescale_DefaultsToHalfEven()
    {
        // The mode is optional so that existing callers keep today's behaviour.
        Assert.Equal(2, ScaleHelper.Rescale32(25, 1, 0));
        Assert.Equal(2L, ScaleHelper.Rescale64(25, 1, 0));
        Assert.Equal((Int128)2, ScaleHelper.Rescale128(25, 1, 0));
        Assert.Equal((Int256)2, ScaleHelper.Rescale256((Int256)25, 1, 0));
    }

    // ================================================================
    // DivideRound — including the negative-divisor sign cases
    // ================================================================

    [Theory]
    // dividend, divisor, halfEven, halfUp
    [InlineData(5, 2, 2, 3)]        //  2.5
    [InlineData(-5, 2, -2, -3)]     // -2.5
    [InlineData(5, -2, -2, -3)]     // -2.5, expressed with a negative divisor
    [InlineData(-5, -2, 2, 3)]      //  2.5
    [InlineData(7, 2, 4, 4)]        //  3.5
    [InlineData(-7, 2, -4, -4)]
    [InlineData(7, -2, -4, -4)]     // -3.5 — the adjustment must follow the quotient's sign
    [InlineData(-7, -2, 4, 4)]
    [InlineData(1, 2, 0, 1)]        //  0.5 — quotient is zero, so the sign comes from the operands
    [InlineData(1, -2, 0, -1)]      // -0.5
    [InlineData(-1, 2, 0, -1)]
    [InlineData(-1, -2, 0, 1)]
    [InlineData(3, 5, 1, 1)]        //  0.6 — odd divisor, no midpoint possible
    [InlineData(2, 5, 0, 0)]        //  0.4
    [InlineData(-3, 5, -1, -1)]
    [InlineData(3, -5, -1, -1)]
    [InlineData(6, 3, 2, 2)]        //  exact, no rounding
    [InlineData(6, -3, -2, -2)]
    public void DivideRound_AllWidths(int dividend, int divisor, int halfEven, int halfUp)
    {
        Assert.Equal(halfEven, ScaleHelper.DivideRound(dividend, divisor, DecimalRounding.HalfEven));
        Assert.Equal(halfUp, ScaleHelper.DivideRound(dividend, divisor, DecimalRounding.HalfUp));

        Assert.Equal((long)halfEven, ScaleHelper.DivideRound((long)dividend, divisor, DecimalRounding.HalfEven));
        Assert.Equal((long)halfUp, ScaleHelper.DivideRound((long)dividend, divisor, DecimalRounding.HalfUp));

        Assert.Equal((Int128)halfEven, ScaleHelper.DivideRound((Int128)dividend, divisor, DecimalRounding.HalfEven));
        Assert.Equal((Int128)halfUp, ScaleHelper.DivideRound((Int128)dividend, divisor, DecimalRounding.HalfUp));

        Assert.Equal((Int256)halfEven, ScaleHelper.DivideRound((Int256)dividend, (Int256)divisor, DecimalRounding.HalfEven));
        Assert.Equal((Int256)halfUp, ScaleHelper.DivideRound((Int256)dividend, (Int256)divisor, DecimalRounding.HalfUp));
    }

    /// <summary>
    /// Exhaustive cross-check of the rounding core against decimal arithmetic
    /// over every small dividend/divisor pair, both signs.
    /// </summary>
    [Theory]
    [InlineData(DecimalRounding.HalfEven, MidpointRounding.ToEven)]
    [InlineData(DecimalRounding.HalfUp, MidpointRounding.AwayFromZero)]
    public void DivideRound_MatchesDecimalRound(DecimalRounding rounding, MidpointRounding midpoint)
    {
        for (int dividend = -60; dividend <= 60; dividend++)
        {
            for (int divisor = -12; divisor <= 12; divisor++)
            {
                if (divisor == 0) continue;

                int expected = (int)Math.Round((decimal)dividend / divisor, 0, midpoint);
                Assert.Equal(expected, ScaleHelper.DivideRound(dividend, divisor, rounding));
                Assert.Equal((long)expected, ScaleHelper.DivideRound((long)dividend, divisor, rounding));
                Assert.Equal((Int128)expected, ScaleHelper.DivideRound((Int128)dividend, divisor, rounding));
                Assert.Equal((Int256)expected, ScaleHelper.DivideRound((Int256)dividend, (Int256)divisor, rounding));
            }
        }
    }

    /// <summary>
    /// A divisor of MinValue has no positive counterpart, so a signed
    /// magnitude wraps back to MinValue and every comparison against
    /// floor(|divisor|/2) reads as "above the midpoint". Mantissas within their
    /// declared precision never reach MinValue, but the kernels do not enforce
    /// that, so the rounding core has to stay correct on its own.
    /// </summary>
    [Theory]
    [InlineData(DecimalRounding.HalfEven)]
    [InlineData(DecimalRounding.HalfUp)]
    public void DivideRound_MinValueDivisor_RoundsTowardZero(DecimalRounding rounding)
    {
        // 1 / int.MinValue is far below the midpoint, so it rounds to zero.
        Assert.Equal(0, ScaleHelper.DivideRound(1, int.MinValue, rounding));
        Assert.Equal(0, ScaleHelper.DivideRound(-1, int.MinValue, rounding));
        Assert.Equal(0L, ScaleHelper.DivideRound(1L, long.MinValue, rounding));
        Assert.Equal(0L, ScaleHelper.DivideRound(-1L, long.MinValue, rounding));
        Assert.Equal(Int128.Zero, ScaleHelper.DivideRound(Int128.One, Int128.MinValue, rounding));
        Assert.Equal(Int256.Zero, ScaleHelper.DivideRound(Int256.One, Int256.MinValue, rounding));
    }

    [Theory]
    [InlineData(DecimalRounding.HalfEven)]
    [InlineData(DecimalRounding.HalfUp)]
    public void DivideRound_MinValueDivisor_AtTheMidpoint(DecimalRounding rounding)
    {
        // |dividend| == |divisor| / 2 exactly: the true quotient is -0.5, so
        // half-even keeps zero and half-up goes to -1.
        int expected = rounding == DecimalRounding.HalfUp ? -1 : 0;

        Assert.Equal(expected, ScaleHelper.DivideRound(1 << 30, int.MinValue, rounding));
        Assert.Equal((long)expected, ScaleHelper.DivideRound(1L << 62, long.MinValue, rounding));
        Assert.Equal((Int128)expected, ScaleHelper.DivideRound(Int128.MaxValue / 2 + 1, Int128.MinValue, rounding));
        Assert.Equal((Int256)expected, ScaleHelper.DivideRound(Int256.MaxValue / (Int256)2 + Int256.One, Int256.MinValue, rounding));
    }

    [Theory]
    [InlineData(DecimalRounding.HalfEven)]
    [InlineData(DecimalRounding.HalfUp)]
    public void DivideRound_MinValueDividend(DecimalRounding rounding)
    {
        // The remainder can never itself be MinValue — |remainder| < |divisor|
        // forces it — but a MinValue dividend still has to divide cleanly.
        Assert.Equal(int.MinValue / 2, ScaleHelper.DivideRound(int.MinValue, 2, rounding));
        Assert.Equal(long.MinValue / 2, ScaleHelper.DivideRound(long.MinValue, 2, rounding));
        Assert.Equal(1, ScaleHelper.DivideRound(int.MinValue, int.MinValue, rounding));
        Assert.Equal(Int128.One, ScaleHelper.DivideRound(Int128.MinValue, Int128.MinValue, rounding));
        Assert.Equal(Int256.One, ScaleHelper.DivideRound(Int256.MinValue, Int256.MinValue, rounding));
    }

    // ================================================================
    // DivideKernel — the case from issue #1
    // ================================================================

    [Fact]
    public void Divide_5By2_AtScaleZero()
    {
        var operandType = DecimalType.Numeric(9, 0);
        var resultType = DecimalType.Numeric(18, 0);

        Assert.Equal(2L, DivideKernel.Divide(new Decimal32(5), operandType, new Decimal32(2), operandType, resultType,
            DecimalRounding.HalfEven).Mantissa);
        Assert.Equal(3L, DivideKernel.Divide(new Decimal32(5), operandType, new Decimal32(2), operandType, resultType,
            DecimalRounding.HalfUp).Mantissa);
    }

    [Fact]
    public void Divide_NegativeDivisor_RoundsAwayFromZeroOnTheQuotient()
    {
        var operandType = DecimalType.Numeric(9, 0);
        var resultType = DecimalType.Numeric(18, 0);

        // 7 / -2 = -3.5; both modes round to -4 because -4 is the even neighbour.
        Assert.Equal(-4L, DivideKernel.Divide(new Decimal32(7), operandType, new Decimal32(-2), operandType, resultType,
            DecimalRounding.HalfEven).Mantissa);
        Assert.Equal(-4L, DivideKernel.Divide(new Decimal32(7), operandType, new Decimal32(-2), operandType, resultType,
            DecimalRounding.HalfUp).Mantissa);

        // 5 / -2 = -2.5; half-even keeps -2, half-up goes to -3.
        Assert.Equal(-2L, DivideKernel.Divide(new Decimal32(5), operandType, new Decimal32(-2), operandType, resultType,
            DecimalRounding.HalfEven).Mantissa);
        Assert.Equal(-3L, DivideKernel.Divide(new Decimal32(5), operandType, new Decimal32(-2), operandType, resultType,
            DecimalRounding.HalfUp).Mantissa);
    }

    [Fact]
    public void Divide_AllWidths_HalfUp()
    {
        var t32 = DecimalType.Numeric(9, 0);
        var t64 = DecimalType.Numeric(18, 0);
        var t128 = DecimalType.Numeric(38, 0);
        var t256 = DecimalType.Numeric(50, 0);

        Assert.Equal(3L, DivideKernel.Divide(new Decimal32(5), t32, new Decimal32(2), t32, t64, DecimalRounding.HalfUp).Mantissa);
        Assert.Equal((Int128)3, DivideKernel.Divide(new Decimal64(5), t64, new Decimal64(2), t64, t128, DecimalRounding.HalfUp).Mantissa);
        Assert.Equal((Int128)3, DivideKernel.Divide(new Decimal128(5), t128, new Decimal128(2), t128, t128, DecimalRounding.HalfUp).Mantissa);
        Assert.Equal((Int256)3, DivideKernel.DivideWiden(new Decimal128(5), t128, new Decimal128(2), t128, t256, DecimalRounding.HalfUp).Mantissa);
        Assert.Equal((Int256)3, DivideKernel.Divide(new Decimal256((Int256)5), t256, new Decimal256((Int256)2), t256, t256, DecimalRounding.HalfUp).Mantissa);
    }

    // ================================================================
    // Span kernels
    // ================================================================

    [Fact]
    public void SpanDivide_HonoursRounding()
    {
        var operandType = DecimalType.Numeric(9, 0);
        var resultType = DecimalType.Numeric(18, 0);

        int[] left = { 5, 7, -5, 5 };
        int[] right = { 2, 2, 2, -2 };
        long[] halfEven = new long[4];
        long[] halfUp = new long[4];

        SpanDivideKernel.Divide(left, operandType, right, operandType, halfEven, resultType, DecimalRounding.HalfEven);
        SpanDivideKernel.Divide(left, operandType, right, operandType, halfUp, resultType, DecimalRounding.HalfUp);

        Assert.Equal(new long[] { 2, 4, -2, -2 }, halfEven);
        Assert.Equal(new long[] { 3, 4, -3, -3 }, halfUp);
    }

    [Fact]
    public void SpanDivide_DefaultsToHalfEven()
    {
        var operandType = DecimalType.Numeric(9, 0);
        var resultType = DecimalType.Numeric(18, 0);

        int[] left = { 5 };
        int[] right = { 2 };
        long[] result = new long[1];

        SpanDivideKernel.Divide(left, operandType, right, operandType, result, resultType);
        Assert.Equal(2L, result[0]);
    }

    [Fact]
    public void SpanAdd_HonoursRoundingWhenResultScaleIsSmaller()
    {
        // Not what the promotion rules produce, but the kernels accept any
        // result type, and a narrower scale is where add/subtract can round.
        var operandType = DecimalType.Numeric(9, 1);
        var resultType = DecimalType.Numeric(9, 0);

        int[] left = { 25, 35 };   // 2.5, 3.5
        int[] right = { 25, 5 };   // 2.5, 0.5
        int[] halfEven = new int[2];
        int[] halfUp = new int[2];

        SpanAddKernel.Add(left, operandType, right, operandType, halfEven, resultType, DecimalRounding.HalfEven);
        SpanAddKernel.Add(left, operandType, right, operandType, halfUp, resultType, DecimalRounding.HalfUp);

        Assert.Equal(new[] { 2 + 2, 4 + 0 }, halfEven);
        Assert.Equal(new[] { 3 + 3, 4 + 1 }, halfUp);
    }

    [Fact]
    public void SpanModulus_HonoursRounding()
    {
        var operandType = DecimalType.Numeric(9, 1);
        var resultType = DecimalType.Numeric(9, 0);

        int[] left = { 25 };   // 2.5 -> 2 (even) or 3 (up)
        int[] right = { 20 };  // 2.0 -> 2
        int[] halfEven = new int[1];
        int[] halfUp = new int[1];

        SpanModulusKernel.Modulus(left, operandType, right, operandType, halfEven, resultType, DecimalRounding.HalfEven);
        SpanModulusKernel.Modulus(left, operandType, right, operandType, halfUp, resultType, DecimalRounding.HalfUp);

        Assert.Equal(0, halfEven[0]);  // 2 % 2
        Assert.Equal(1, halfUp[0]);    // 3 % 2
    }

    [Fact]
    public void SpanMultiply_HonoursRounding()
    {
        var leftType = DecimalType.Numeric(9, 2);
        var rightType = DecimalType.Numeric(9, 0);
        var resultType = DecimalType.Numeric(18, 1);  // raw scale 2, so one digit is dropped

        int[] left = { 25, 35 };  // 0.25, 0.35
        int[] right = { 1, 1 };
        long[] halfEven = new long[2];
        long[] halfUp = new long[2];

        SpanMultiplyKernel.Multiply(left, leftType, right, rightType, halfEven, resultType, DecimalRounding.HalfEven);
        SpanMultiplyKernel.Multiply(left, leftType, right, rightType, halfUp, resultType, DecimalRounding.HalfUp);

        Assert.Equal(new long[] { 2, 4 }, halfEven);  // 0.2, 0.4
        Assert.Equal(new long[] { 3, 4 }, halfUp);    // 0.3, 0.4
    }

    // ================================================================
    // Text parsing
    // ================================================================

    [Theory]
    // text, targetScale, halfEven, halfUp
    [InlineData("2.5", 0, 2, 3)]
    [InlineData("-2.5", 0, -2, -3)]
    [InlineData("3.5", 0, 4, 4)]
    [InlineData("1.45", 1, 14, 15)]
    [InlineData("-1.45", 1, -14, -15)]
    [InlineData("1.55", 1, 16, 16)]
    [InlineData("2.51", 0, 3, 3)]
    [InlineData("2.49", 0, 2, 2)]
    [InlineData("0.5", 0, 0, 1)]
    [InlineData("-0.5", 0, 0, -1)]
    [InlineData("2.5000", 0, 2, 3)]   // trailing zeros do not make it non-midpoint
    [InlineData("2.5001", 0, 3, 3)]
    public void Parse_HonoursRounding(string text, int targetScale, int halfEven, int halfUp)
    {
        var type = DecimalType.Numeric(9, targetScale);
        Assert.Equal(halfEven, DecimalText.ParseDecimal32(text.AsSpan(), type, DecimalRounding.HalfEven).Mantissa);
        Assert.Equal(halfUp, DecimalText.ParseDecimal32(text.AsSpan(), type, DecimalRounding.HalfUp).Mantissa);

        var type64 = DecimalType.Numeric(18, targetScale);
        Assert.Equal(halfEven, DecimalText.ParseDecimal64(text.AsSpan(), type64, DecimalRounding.HalfEven).Mantissa);
        Assert.Equal(halfUp, DecimalText.ParseDecimal64(text.AsSpan(), type64, DecimalRounding.HalfUp).Mantissa);

        var type128 = DecimalType.Numeric(38, targetScale);
        Assert.Equal((Int128)halfEven, DecimalText.ParseDecimal128(text.AsSpan(), type128, DecimalRounding.HalfEven).Mantissa);
        Assert.Equal((Int128)halfUp, DecimalText.ParseDecimal128(text.AsSpan(), type128, DecimalRounding.HalfUp).Mantissa);

        var type256 = DecimalType.Numeric(50, targetScale);
        Assert.Equal((Int256)halfEven, DecimalText.ParseDecimal256(text.AsSpan(), type256, DecimalRounding.HalfEven).Mantissa);
        Assert.Equal((Int256)halfUp, DecimalText.ParseDecimal256(text.AsSpan(), type256, DecimalRounding.HalfUp).Mantissa);
    }

    [Fact]
    public void ParseUtf8_HonoursRounding()
    {
        var type = DecimalType.Numeric(9, 0);
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes("2.5");

        Assert.Equal(2, DecimalText.ParseDecimal32(utf8.AsSpan(), type, DecimalRounding.HalfEven).Mantissa);
        Assert.Equal(3, DecimalText.ParseDecimal32(utf8.AsSpan(), type, DecimalRounding.HalfUp).Mantissa);

        Assert.True(DecimalText.TryParseDecimal32(utf8.AsSpan(), type, out var result, DecimalRounding.HalfUp));
        Assert.Equal(3, result.Mantissa);
    }

    [Fact]
    public void Parse_DefaultsToHalfEven()
    {
        var type = DecimalType.Numeric(9, 0);
        Assert.Equal(2, DecimalText.ParseDecimal32("2.5".AsSpan(), type).Mantissa);
        Assert.True(DecimalText.TryParseDecimal32("2.5".AsSpan(), type, out var result));
        Assert.Equal(2, result.Mantissa);
    }

    // ================================================================
    // Spark's documented casts, end to end
    // ================================================================

    [Theory]
    [InlineData("2.5", 3, 0, "3")]
    [InlineData("1.45", 3, 1, "1.5")]
    [InlineData("-2.5", 3, 0, "-3")]
    [InlineData("-1.45", 3, 1, "-1.5")]
    public void SparkCastSemantics(string input, int precision, int scale, string expected)
    {
        var type = DecimalType.Numeric(precision, scale);
        var value = DecimalText.ParseDecimal64(input.AsSpan(), type, DecimalRounding.HalfUp);
        Assert.Equal(expected, DecimalText.Format(value, type));
    }
}
