// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// The 128- and 256-bit span checks (<see cref="DecimalRange.IndexOfOutOfRange(ReadOnlySpan{Int128}, DecimalType)"/>,
/// <see cref="DecimalRange.WriteOutOfRangeMask(ReadOnlySpan{Int128}, DecimalType, Span{ulong})"/> and
/// <c>Validate</c>) pinned to the scalar <see cref="DecimalRange.IsInRange(Int128, DecimalType)"/>
/// at every precision, around the bound and around each limb boundary of it.
/// </summary>
/// <remarks>
/// Written for #11, which tried screening each element on its top limb before
/// the full comparison. That screen is not in place, but any shortcut of that
/// kind fails here first: the candidates sit exactly where an off-by-one in a
/// limb threshold would let an out-of-range value through.
/// </remarks>
public class WideRangeCheckTests
{
    private const int Length = 70;   // past one mask word, so the offender's bit is not always in word 0
    private const int Offender = 67;

    public static IEnumerable<object[]> Precisions128() =>
        Enumerable.Range(1, DecimalType.MaxPrecision128).Select(p => new object[] { p });

    public static IEnumerable<object[]> Precisions256() =>
        Enumerable.Range(1, DecimalType.MaxPrecision256).Select(p => new object[] { p });

    [Theory]
    [MemberData(nameof(Precisions128))]
    public void Int128_SpanChecksAgreeWithScalar(int precision)
    {
        var type = DecimalType.Numeric(precision, 0);
        foreach (Int128 value in Candidates128(PowersOf10.Int128[precision]))
        {
            bool inRange = DecimalRange.IsInRange(value, type);

            var values = new Int128[Length];
            values[Offender] = value;

            Assert.Equal(inRange ? -1 : Offender, DecimalRange.IndexOfOutOfRange(values, type));

            var mask = new ulong[DecimalRange.MaskWordCount(Length)];
            Assert.Equal(inRange ? 0 : 1, DecimalRange.WriteOutOfRangeMask(values, type, mask));
            Assert.Equal(!inRange, (mask[Offender >> 6] & (1UL << (Offender & 63))) != 0);

            if (inRange) DecimalRange.Validate(values, type);
            else Assert.Throws<OverflowException>(() => DecimalRange.Validate(values, type));
        }
    }

    [Theory]
    [MemberData(nameof(Precisions256))]
    public void Int256_SpanChecksAgreeWithScalar(int precision)
    {
        var type = DecimalType.Numeric(precision, 0);
        foreach (Int256 value in Candidates256(PowersOf10.Int256Values[precision]))
        {
            bool inRange = DecimalRange.IsInRange(value, type);

            var values = new Int256[Length];
            values[Offender] = value;

            Assert.Equal(inRange ? -1 : Offender, DecimalRange.IndexOfOutOfRange(values, type));

            var mask = new ulong[DecimalRange.MaskWordCount(Length)];
            Assert.Equal(inRange ? 0 : 1, DecimalRange.WriteOutOfRangeMask(values, type, mask));
            Assert.Equal(!inRange, (mask[Offender >> 6] & (1UL << (Offender & 63))) != 0);

            if (inRange) DecimalRange.Validate(values, type);
            else Assert.Throws<OverflowException>(() => DecimalRange.Validate(values, type));
        }
    }

    [Fact]
    public void IndexOfOutOfRange_ReportsTheFirstOffender()
    {
        var t38 = DecimalType.Numeric(38, 0);
        var t76 = DecimalType.Numeric(76, 0);
        var t50 = DecimalType.Numeric(50, 0);

        Int128 bad128 = PowersOf10.Int128[38];
        Assert.Equal(3, DecimalRange.IndexOfOutOfRange(new[] { Int128.Zero, Int128.One, -Int128.One, bad128, -bad128 }, t38));

        Int256 bad76 = PowersOf10.Int256Values[76];
        Assert.Equal(2, DecimalRange.IndexOfOutOfRange(new[] { Int256.Zero, Int256.One, -bad76, bad76 }, t76));

        Int256 bad50 = PowersOf10.Int256Values[50];
        Assert.Equal(1, DecimalRange.IndexOfOutOfRange(new[] { Int256.One, bad50, -bad50 }, t50));
    }

    // Values at and around the bound, around the screen thresholds H·2^k for
    // each limb boundary k, and the width's extremes — the places where an
    // off-by-one in the screen would let an out-of-range value through.
    private static IEnumerable<Int128> Candidates128(Int128 bound)
    {
        Int128 max = bound - Int128.One;
        var centres = new List<Int128> { Int128.Zero, max, bound, (max >> 64) << 64, ((max >> 64) + Int128.One) << 64 };
        foreach (Int128 c in centres)
            for (int d = -2; d <= 2; d++)
            {
                Int128 v = c + d;
                yield return v;
                yield return -v;
            }
        yield return Int128.MaxValue;
        yield return Int128.MinValue;
        yield return Int128.MinValue + Int128.One;
    }

    private static IEnumerable<Int256> Candidates256(Int256 bound)
    {
        Int256 max = bound - Int256.One;
        var centres = new List<Int256> { Int256.Zero, max, bound };
        foreach (int k in new[] { 64, 128, 192 })
        {
            centres.Add((max >> k) << k);
            centres.Add(((max >> k) + Int256.One) << k);
        }
        foreach (Int256 c in centres)
            for (int d = -2; d <= 2; d++)
            {
                Int256 v = c + (Int256)d;
                yield return v;
                yield return -v;
            }
        yield return Int256.MaxValue;
        yield return Int256.MinValue;
        yield return Int256.MinValue + Int256.One;
    }
}
