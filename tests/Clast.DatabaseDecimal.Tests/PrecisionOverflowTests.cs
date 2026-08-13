// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// The result type's precision is a stricter bound than the mantissa width:
/// NUMERIC(38,0) stops at 10^38 - 1 while an Int128 reaches about 1.7 × 10^38.
/// Checked arithmetic catches the width; these cover the gap.
/// </summary>
public class PrecisionOverflowTests
{
    private static readonly DecimalType T9 = DecimalType.Numeric(9, 0);
    private static readonly DecimalType T18 = DecimalType.Numeric(18, 0);
    private static readonly DecimalType T38 = DecimalType.Numeric(38, 0);
    private static readonly DecimalType T50 = DecimalType.Numeric(50, 0);

    private const int Max9 = 999_999_999;
    private const long Max18 = 999_999_999_999_999_999L;
    private static Int128 Max38 => PowersOf10.Int128[38] - Int128.One;
    private static Int256 Max50 => PowersOf10.Int256Values[50] - Int256.One;

    // ================================================================
    // The cases reported in issue #2
    // ================================================================

    [Fact]
    public void Add_OneDigitPastPrecision_Throws()
    {
        Assert.Throws<OverflowException>(() =>
            AddKernel.Add(new Decimal128(Max38), T38, new Decimal128(Int128.One), T38, T38));
        Assert.Throws<OverflowException>(() =>
            AddKernel.Add(new Decimal32(Max9), T9, new Decimal32(1), T9, T9));
        Assert.Throws<OverflowException>(() =>
            AddKernel.Add(new Decimal64(Max18), T18, new Decimal64(1), T18, T18));
        Assert.Throws<OverflowException>(() =>
            AddKernel.Add(new Decimal256(Max50), T50, new Decimal256(Int256.One), T50, T50));
    }

    [Fact]
    public void Subtract_OneDigitPastPrecision_Throws()
    {
        // The negative side of the range is bounded identically.
        Assert.Throws<OverflowException>(() =>
            AddKernel.Subtract(new Decimal128(-Max38), T38, new Decimal128(Int128.One), T38, T38));
        Assert.Throws<OverflowException>(() =>
            AddKernel.Subtract(new Decimal32(-Max9), T9, new Decimal32(1), T9, T9));
    }

    [Fact]
    public void Ignore_ReturnsTheUncheckedResult()
    {
        var sum = AddKernel.Add(new Decimal128(Max38), T38, new Decimal128(Int128.One), T38, T38,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        Assert.Equal(PowersOf10.Int128[38], sum.Mantissa);

        var small = AddKernel.Add(new Decimal32(Max9), T9, new Decimal32(1), T9, T9,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        Assert.Equal(1_000_000_000, small.Mantissa);
    }

    [Fact]
    public void Ignore_DoesNotDisableTheWidthCheck()
    {
        // max38 + max38 is about 2e38, past Int128.MaxValue, so the checked
        // arithmetic still throws however the precision policy is set.
        Assert.Throws<OverflowException>(() =>
            AddKernel.Add(new Decimal128(Max38), T38, new Decimal128(Max38), T38, T38,
                DecimalRounding.HalfEven, DecimalOverflow.Ignore));
    }

    [Fact]
    public void WithinPrecision_IsUnaffected()
    {
        Assert.Equal(Max38, AddKernel.Add(
            new Decimal128(Max38 - Int128.One), T38, new Decimal128(Int128.One), T38, T38).Mantissa);
        Assert.Equal(Max9, AddKernel.Add(
            new Decimal32(Max9 - 1), T9, new Decimal32(1), T9, T9).Mantissa);
    }

    // ================================================================
    // The other kernels
    // ================================================================

    [Fact]
    public void Multiply_PastPrecision_Throws()
    {
        var t20 = DecimalType.Numeric(20, 0);
        Int128 tenPow19 = PowersOf10.Int128[19];

        // 10^19 * 10 = 10^20, which needs 21 digits.
        Assert.Throws<OverflowException>(() =>
            MultiplyKernel.Multiply(new Decimal128(tenPow19), t20, new Decimal128((Int128)10), t20, t20));

        Assert.Equal(PowersOf10.Int128[20], MultiplyKernel.Multiply(
            new Decimal128(tenPow19), t20, new Decimal128((Int128)10), t20, t20,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore).Mantissa);
    }

    [Fact]
    public void MultiplyWiden_PastPrecision_Throws()
    {
        var t39 = DecimalType.Numeric(39, 0);
        Int128 tenPow38 = PowersOf10.Int128[38] - Int128.One;

        Assert.Throws<OverflowException>(() =>
            MultiplyKernel.MultiplyWiden(new Decimal128(tenPow38), t39, new Decimal128(tenPow38), t39, t39));
    }

    [Fact]
    public void Divide_PastPrecision_Throws()
    {
        // 10^8 / 0.1 = 10^9, one digit too many for NUMERIC(9,0).
        var tenth = DecimalType.Numeric(9, 1);
        Assert.Throws<OverflowException>(() =>
            DivideKernel.Divide(new Decimal32(100_000_000), T9, new Decimal32(1), tenth, T9));
    }

    [Fact]
    public void Modulus_UnderThePromotionRules_CannotOverflow()
    {
        // |l % r| <= |l|, and l is the left operand rescaled to the result
        // scale, so a result type from DecimalTypeRules always has room for it.
        var left = DecimalType.Numeric(9, 2);
        var right = DecimalType.Numeric(7, 3);
        var resultType = DecimalTypeRules.Modulus(left, right);

        var result = ModulusKernel.Modulus(
            new Decimal64(Max18 % PowersOf10.Int64[9]), left, new Decimal64(7_777), right, resultType);

        Assert.True(DecimalRange.IsInRange(result.Mantissa, resultType));
    }

    [Fact]
    public void Modulus_PastPrecision_Throws()
    {
        // It takes a caller-supplied result type whose scale forces the operands
        // *up*: at scale 2 the left operand becomes 10^10, below the divisor, so
        // the modulus is the rescaled left operand itself — ten digits in a
        // NUMERIC(9,2).
        var operands = DecimalType.Numeric(9, 0);
        var resultType = DecimalType.Numeric(9, 2);

        Assert.Throws<OverflowException>(() =>
            ModulusKernel.ModulusWiden(new Decimal32(100_000_000), operands, new Decimal32(Max9), operands, resultType));

        Assert.Equal(10_000_000_000L, ModulusKernel.ModulusWiden(
            new Decimal32(100_000_000), operands, new Decimal32(Max9), operands, resultType,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore).Mantissa);
    }

    // ================================================================
    // Span kernels
    // ================================================================

    [Fact]
    public void SpanAdd_PastPrecision_Throws()
    {
        Int128[] left = { Max38, Int128.One };
        Int128[] right = { Int128.One, Int128.One };
        Int128[] result = new Int128[2];

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Add(left, T38, right, T38, result, T38));
    }

    [Fact]
    public void SpanAdd_Ignore_LeavesTheResultAlone()
    {
        Int128[] left = { Max38, Int128.One };
        Int128[] right = { Int128.One, Int128.One };
        Int128[] result = new Int128[2];

        SpanAddKernel.Add(left, T38, right, T38, result, T38, DecimalRounding.HalfEven, DecimalOverflow.Ignore);

        Assert.Equal(PowersOf10.Int128[38], result[0]);
        Assert.Equal((Int128)2, result[1]);
    }

    [Fact]
    public void SpanAdd_ValidatesOnlyTheWrittenPrefix()
    {
        // The result span may be longer than the inputs. Whatever the caller
        // left in the tail is not this call's output and must not be checked.
        int[] left = { 1, 2 };
        int[] right = { 1, 2 };
        int[] result = { 0, 0, Max9, Max9 };

        SpanAddKernel.Add(left, T9, right, T9, result.AsSpan(), T9);

        Assert.Equal(new[] { 2, 4, Max9, Max9 }, result);
    }

    [Fact]
    public void SpanAdd_ThrowsAfterWritingTheWholeOutput()
    {
        // Validation is a separate pass over the finished span, so the output is
        // fully computed even when it throws — unlike a mid-loop check, which
        // would leave the tail undefined.
        int[] left = { 1, Max9, 3 };
        int[] right = { 1, 1, 3 };
        int[] result = new int[3];

        Assert.Throws<OverflowException>(() => SpanAddKernel.Add(left, T9, right, T9, result, T9));

        Assert.Equal(new[] { 2, 1_000_000_000, 6 }, result);
    }

    [Fact]
    public void SpanDivideAndMultiply_PastPrecision_Throw()
    {
        int[] left = { 100_000_000 };
        int[] right = { 1 };
        long[] result = new long[1];

        var tenth = DecimalType.Numeric(9, 1);
        Assert.Throws<OverflowException>(() =>
            SpanDivideKernel.Divide(left, T9, right, tenth, result, T9));

        int[] big = { 100_000 };
        Assert.Throws<OverflowException>(() =>
            SpanMultiplyKernel.Multiply(big, T9, big, T9, result, T9));
    }

    // ================================================================
    // DecimalRange
    // ================================================================

    [Fact]
    public void IsInRange_BoundIsExclusive()
    {
        Assert.True(DecimalRange.IsInRange(Max9, T9));
        Assert.False(DecimalRange.IsInRange(1_000_000_000, T9));
        Assert.True(DecimalRange.IsInRange(-Max9, T9));
        Assert.False(DecimalRange.IsInRange(-1_000_000_000, T9));

        Assert.True(DecimalRange.IsInRange(Max38, T38));
        Assert.False(DecimalRange.IsInRange(PowersOf10.Int128[38], T38));
        Assert.False(DecimalRange.IsInRange(-PowersOf10.Int128[38], T38));
    }

    [Fact]
    public void IsInRange_PrecisionBeyondTheWidthHasNoBound()
    {
        // long.MaxValue is about 9.2e18, so no 64-bit value can reach 10^19.
        var t19 = DecimalType.Numeric(19, 0);
        Assert.True(DecimalRange.IsInRange(long.MaxValue, t19));
        Assert.True(DecimalRange.IsInRange(long.MinValue, t19));

        var t39 = DecimalType.Numeric(39, 0);
        Assert.True(DecimalRange.IsInRange(Int128.MaxValue, t39));
        Assert.True(DecimalRange.IsInRange(Int128.MinValue, t39));

        var t10 = DecimalType.Numeric(10, 0);
        Assert.True(DecimalRange.IsInRange(int.MaxValue, t10));
        Assert.True(DecimalRange.IsInRange(int.MinValue, t10));
    }

    [Fact]
    public void Validate_Throws()
    {
        Assert.Throws<OverflowException>(() => DecimalRange.Validate(1_000_000_000, T9));
        DecimalRange.Validate(Max9, T9); // does not throw

        int[] values = { 1, 2, 1_000_000_000 };
        Assert.Throws<OverflowException>(() => DecimalRange.Validate(values, T9));
    }

    [Fact]
    public void IndexOfOutOfRange()
    {
        Assert.Equal(-1, DecimalRange.IndexOfOutOfRange(new[] { 1, 2, 3 }, T9));
        Assert.Equal(1, DecimalRange.IndexOfOutOfRange(new[] { 1, 1_000_000_000, 3 }, T9));
        Assert.Equal(0, DecimalRange.IndexOfOutOfRange(new[] { -1_000_000_000, 1 }, T9));
        Assert.Equal(-1, DecimalRange.IndexOfOutOfRange(ReadOnlySpan<int>.Empty, T9));
    }

    [Fact]
    public void WriteOutOfRangeMask_FlagsEveryOffender()
    {
        int[] values = { 1, 1_000_000_000, 3, -1_000_000_000, Max9 };
        ulong[] mask = new ulong[DecimalRange.MaskWordCount(values.Length)];

        int count = DecimalRange.WriteOutOfRangeMask(values, T9, mask);

        Assert.Equal(2, count);
        Assert.Equal(0b0000_1010UL, mask[0]);
    }

    [Fact]
    public void WriteOutOfRangeMask_ClearsTheBufferFirst()
    {
        int[] values = { 1, 2, 3 };
        ulong[] mask = { ulong.MaxValue, ulong.MaxValue };

        int count = DecimalRange.WriteOutOfRangeMask(values, T9, mask);

        Assert.Equal(0, count);
        Assert.Equal(0UL, mask[0]);
        Assert.Equal(ulong.MaxValue, mask[1]); // words past the span are untouched
    }

    [Fact]
    public void WriteOutOfRangeMask_SpansMultipleWords()
    {
        int[] values = new int[130];
        values[0] = 1_000_000_000;
        values[64] = 1_000_000_000;
        values[129] = 1_000_000_000;

        ulong[] mask = new ulong[DecimalRange.MaskWordCount(values.Length)];
        Assert.Equal(3, mask.Length);

        int count = DecimalRange.WriteOutOfRangeMask(values, T9, mask);

        Assert.Equal(3, count);
        Assert.Equal(1UL, mask[0]);
        Assert.Equal(1UL, mask[1]);
        Assert.Equal(1UL << 1, mask[2]);
    }

    [Fact]
    public void WriteOutOfRangeMask_RejectsAShortBuffer()
    {
        int[] values = new int[65];
        ulong[] mask = new ulong[1];
        Assert.Throws<ArgumentException>(() => DecimalRange.WriteOutOfRangeMask(values, T9, mask));
    }

    [Fact]
    public void WriteOutOfRangeMask_AllWidths()
    {
        ulong[] mask = new ulong[1];

        Assert.Equal(1, DecimalRange.WriteOutOfRangeMask(new[] { 1_000_000_000 }, T9, mask));
        Assert.Equal(1, DecimalRange.WriteOutOfRangeMask(new[] { 1_000_000_000_000_000_000L }, T18, mask));
        Assert.Equal(1, DecimalRange.WriteOutOfRangeMask(new[] { PowersOf10.Int128[38] }, T38, mask));
        Assert.Equal(1, DecimalRange.WriteOutOfRangeMask(new[] { PowersOf10.Int256Values[50] }, T50, mask));
    }

    /// <summary>
    /// The shape a non-ANSI caller uses: compute with Ignore, then flag the
    /// overflowing rows in one pass instead of paying for an exception.
    /// </summary>
    [Fact]
    public void IgnoreThenMask_IsTheNullingWorkflow()
    {
        int[] left = { 1, Max9, 3, Max9 };
        int[] right = { 1, 1, 3, 1 };
        int[] result = new int[4];

        SpanAddKernel.Add(left, T9, right, T9, result, T9, DecimalRounding.HalfEven, DecimalOverflow.Ignore);

        ulong[] mask = new ulong[DecimalRange.MaskWordCount(result.Length)];
        int overflowed = DecimalRange.WriteOutOfRangeMask(result, T9, mask);

        Assert.Equal(2, overflowed);
        Assert.Equal(0b1010UL, mask[0]);
        Assert.Equal(new[] { 2, 1_000_000_000, 6, 1_000_000_000 }, result);
    }
}
