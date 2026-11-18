// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Globalization;
using System.Numerics;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// The parts of the <see cref="Int128"/> and <see cref="UInt128"/> surface that let
/// the netstandard2.0 polyfill stand in for the BCL type: non-generic
/// <see cref="IComparable"/> and <see cref="IFormattable"/>, the format strings
/// behind them, and the static helpers <c>Sign</c>, <c>Min</c>, <c>Max</c>,
/// <c>DivRem</c>, <c>LeadingZeroCount</c> and <c>PopCount</c>.
/// </summary>
/// <remarks>
/// Like <see cref="Int128ConversionTests"/>, this file is differential by target:
/// net8.0 and net10.0 run it against the BCL, net472 against the polyfill. The
/// expected strings are the BCL's own output, so the BCL legs pin down the
/// expectations and the net472 leg checks the polyfill against them.
/// </remarks>
public class Int128SurfaceTests
{
    // A culture built by hand rather than looked up. A real culture's signs come
    // from the platform's globalization data, which disagrees with itself:
    // sv-SE's negative sign is U+2212 under ICU and '-' under Windows NLS, which
    // is what net472 uses. These values cannot come from any platform, so a test
    // that sees them knows the provider was honoured. Multi-character signs also
    // check that nothing assumes a sign is one char.
    private static readonly CultureInfo Odd = CreateOddCulture();

    private static CultureInfo CreateOddCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "neg~";
        culture.NumberFormat.PositiveSign = "pos~";
        culture.NumberFormat.NumberDecimalSeparator = "_";
        return culture;
    }

    // --- Return types ---

    [Fact]
    public void BitCountsReturnTheTypeNotInt()
    {
        // The BCL returns Int128/UInt128 here. An int-returning polyfill would let
        // `int n = Int128.PopCount(x)` build downlevel and fail on net8.0. That
        // cannot be caught at compile time from this side — int widens to Int128
        // implicitly — so check it at run time instead.
        Assert.IsType<Int128>(Int128.LeadingZeroCount(Int128.One));
        Assert.IsType<Int128>(Int128.PopCount(Int128.One));
        Assert.IsType<UInt128>(UInt128.LeadingZeroCount(UInt128.One));
        Assert.IsType<UInt128>(UInt128.PopCount(UInt128.One));
        Assert.IsType<int>(Int128.Sign(Int128.One));
        Assert.IsType<int>(UInt128.Sign(UInt128.One));
    }

    // --- Static helpers ---

    [Fact]
    public void SignMinMax()
    {
        Assert.Equal(-1, Int128.Sign(Int128.MinValue));
        Assert.Equal(0, Int128.Sign(Int128.Zero));
        Assert.Equal(1, Int128.Sign(Int128.MaxValue));
        Assert.Equal(0, UInt128.Sign(UInt128.Zero));
        Assert.Equal(1, UInt128.Sign(UInt128.MaxValue));

        Assert.True(Int128.IsNegative(Int128.NegativeOne));
        Assert.False(Int128.IsNegative(Int128.Zero));

        Assert.Equal(Int128.MinValue, Int128.Min(Int128.MinValue, Int128.MaxValue));
        Assert.Equal(Int128.MaxValue, Int128.Max(Int128.MinValue, Int128.MaxValue));
        Assert.Equal(UInt128.Zero, UInt128.Min(UInt128.Zero, UInt128.MaxValue));
        Assert.Equal(UInt128.MaxValue, UInt128.Max(UInt128.Zero, UInt128.MaxValue));
    }

    [Theory]
    [InlineData("170141183460469231731687303715884105727", "-3")]
    [InlineData("-170141183460469231731687303715884105728", "7")]
    [InlineData("-7", "2")]
    [InlineData("7", "-2")]
    [InlineData("-7", "-2")]
    [InlineData("0", "5")]
    public void DivRem_MatchesTruncatingDivision(string dividend, string divisor)
    {
        BigInteger a = BigInteger.Parse(dividend), b = BigInteger.Parse(divisor);

        var (q, r) = Int128.DivRem(NumericOracle.ToInt128(a), NumericOracle.ToInt128(b));

        // BigInteger truncates toward zero and gives the remainder the dividend's
        // sign, which is what Int128 does.
        Assert.Equal(BigInteger.Divide(a, b), NumericOracle.ToBig(q));
        Assert.Equal(BigInteger.Remainder(a, b), NumericOracle.ToBig(r));
    }

    [Fact]
    public void DivRem_FailuresMatchTheOperators()
    {
        Assert.Throws<DivideByZeroException>(() => Int128.DivRem(Int128.One, Int128.Zero));
        Assert.Throws<OverflowException>(() => Int128.DivRem(Int128.MinValue, Int128.NegativeOne));
        Assert.Throws<DivideByZeroException>(() => UInt128.DivRem(UInt128.One, UInt128.Zero));

        var (q, r) = UInt128.DivRem(UInt128.MaxValue, new UInt128(0, 10));
        Assert.Equal(NumericOracle.ToBig(UInt128.MaxValue) / 10, NumericOracle.ToBig(q));
        Assert.Equal(NumericOracle.ToBig(UInt128.MaxValue) % 10, NumericOracle.ToBig(r));
    }

    [Fact]
    public void LeadingZeroCountAndPopCount()
    {
        Assert.Equal((Int128)128, Int128.LeadingZeroCount(Int128.Zero));
        Assert.Equal((Int128)0, Int128.LeadingZeroCount(Int128.NegativeOne));
        Assert.Equal((Int128)1, Int128.LeadingZeroCount(Int128.MaxValue));
        Assert.Equal((Int128)63, Int128.LeadingZeroCount(new Int128(1, 0)));
        Assert.Equal((Int128)127, Int128.PopCount(Int128.MaxValue));
        Assert.Equal((Int128)128, Int128.PopCount(Int128.NegativeOne));
        Assert.Equal((Int128)1, Int128.PopCount(Int128.MinValue));

        Assert.Equal((UInt128)128UL, UInt128.LeadingZeroCount(UInt128.Zero));
        Assert.Equal((UInt128)64UL, UInt128.LeadingZeroCount(new UInt128(0, ulong.MaxValue)));
        Assert.Equal((UInt128)128UL, UInt128.PopCount(UInt128.MaxValue));
        Assert.Equal((UInt128)32UL, UInt128.PopCount(new UInt128(0xFFFF, 0xFFFF_0000)));
    }

    // --- IComparable ---

    [Fact]
    public void NonGenericCompareTo()
    {
        IComparable minusOne = Int128.NegativeOne;
        Assert.True(minusOne.CompareTo(Int128.One) < 0);
        Assert.True(minusOne.CompareTo(Int128.MinValue) > 0);
        Assert.Equal(0, minusOne.CompareTo(Int128.NegativeOne));
        Assert.True(minusOne.CompareTo(null) > 0);
        Assert.Throws<ArgumentException>(() => minusOne.CompareTo(-1L));

        IComparable max = UInt128.MaxValue;
        Assert.True(max.CompareTo(UInt128.Zero) > 0);
        Assert.True(max.CompareTo(null) > 0);
        Assert.Throws<ArgumentException>(() => max.CompareTo(ulong.MaxValue));

        // What IComparable is for: sorting through APIs that only know object.
        var list = new System.Collections.ArrayList { Int128.One, Int128.MinValue, Int128.Zero };
        list.Sort();
        Assert.Equal(new object[] { Int128.MinValue, Int128.Zero, Int128.One }, list.ToArray());
    }

    // --- IFormattable ---

    [Theory]
    [InlineData(null, "-1234567890123456789012345")]
    [InlineData("", "-1234567890123456789012345")]
    [InlineData("G", "-1234567890123456789012345")]
    [InlineData("D", "-1234567890123456789012345")]
    [InlineData("D30", "-000001234567890123456789012345")]
    [InlineData("R", "-1234567890123456789012345")]
    [InlineData("G5", "-1.2346E+24")]
    [InlineData("g5", "-1.2346e+24")]
    [InlineData("R5", "-1.2346E+24")]
    [InlineData("G1", "-1E+24")]
    [InlineData("G25", "-1234567890123456789012345")]
    [InlineData("X", "FFFFFFFFFFFEFA91F0C959BBC21D2087")]
    [InlineData("x", "fffffffffffefa91f0c959bbc21d2087")]
    [InlineData("X36", "0000FFFFFFFFFFFEFA91F0C959BBC21D2087")]
    public void Int128Formats(string? format, string expected)
    {
        Int128 value = NumericOracle.ToInt128(BigInteger.Parse("-1234567890123456789012345"));
        Assert.Equal(expected, value.ToString(format, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(null, "340282366920938463463374607431768211455")]
    [InlineData("D41", "00340282366920938463463374607431768211455")]
    [InlineData("G3", "3.4E+38")]           // 3.40 with its trailing zero dropped
    [InlineData("X", "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF")]
    public void UInt128Formats(string? format, string expected)
    {
        Assert.Equal(expected, UInt128.MaxValue.ToString(format, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("D0000000001", "-12")]          // leading zeroes in the precision are fine;
    [InlineData("D00000000000000005", "-00012")] // the BCL bounds its value, not its length
    [InlineData("X000000000000004", "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF4")]
    public void PrecisionMayHaveLeadingZeroes(string format, string expected)
    {
        Assert.Equal(expected, ((Int128)(-12)).ToString(format, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void PrecisionAboveTheBclLimitThrows()
    {
        // 999,999,999 is the BCL's limit, and it is a FormatException there too.
        Assert.Throws<FormatException>(() => Int128.One.ToString("D1000000000", CultureInfo.InvariantCulture));
        Assert.Throws<FormatException>(() => UInt128.One.ToString("X9999999999", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FormattingCarriesIntoTheNextPowerOfTen()
    {
        Int128 nines = NumericOracle.ToInt128(BigInteger.Parse("99999"));
        Assert.Equal("1E+05", nines.ToString("G2", CultureInfo.InvariantCulture));
        Assert.Equal("0", Int128.Zero.ToString("G3", CultureInfo.InvariantCulture));
        Assert.Equal("0", Int128.Zero.ToString("X", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FormattingHonoursTheProvider()
    {
        // Scientific G draws on all three: the negative sign, the decimal
        // separator, and the positive sign in front of the exponent.
        Int128 value = -1234567;
        Assert.Equal("neg~1234567", value.ToString(Odd));
        Assert.Equal("neg~1_23Epos~06", value.ToString("G3", Odd));
        Assert.Equal("1_23Epos~06", ((UInt128)1234567UL).ToString("G3", Odd));
    }

    [Fact]
    public void ParameterlessToStringUsesTheCurrentCulture()
    {
        CultureInfo saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = Odd;
            Assert.Equal("neg~42", ((Int128)(-42)).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public void FormatStringsReachTheValueThroughIFormattable()
    {
        // The reason the interface matters: composite formatting and
        // interpolation only pass the format on when the type implements it.
        // Without it, the polyfill printed {0:X} as decimal.
        Int128 value = 255;
        Assert.Equal("FF|00255", string.Format(CultureInfo.InvariantCulture, "{0:X}|{0:D5}", value));
        Assert.Equal("ff", FormattableString.Invariant($"{value:x}"));
        Assert.Equal("FF", FormattableString.Invariant($"{UInt128.One * 255:X}"));
    }

#if NETFRAMEWORK
    [Theory]
    [InlineData("N0")]
    [InlineData("F2")]
    [InlineData("E")]
    [InlineData("C")]
    [InlineData("P")]
    [InlineData("B")]
    [InlineData("#,##0")]
    public void UnsupportedFormatsThrowInsteadOfPrintingDecimal(string format)
    {
        // The BCL implements these; the polyfill does not. Throwing is the honest
        // failure — the polyfill's old behaviour was to ignore the format and
        // print the plain decimal string, which looks right until it is compared
        // with the net8.0 output.
        Assert.Throws<FormatException>(() => Int128.MaxValue.ToString(format, CultureInfo.InvariantCulture));
        Assert.Throws<FormatException>(() => UInt128.MaxValue.ToString(format, CultureInfo.InvariantCulture));
    }
#endif
}
