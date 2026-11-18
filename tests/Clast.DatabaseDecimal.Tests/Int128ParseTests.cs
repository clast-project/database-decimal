// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Globalization;
using System.Numerics;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// <c>Parse</c> and <c>TryParse</c> on <see cref="Int128"/> and <see cref="UInt128"/>.
/// </summary>
/// <remarks>
/// Differential by target, like <see cref="Int128SurfaceTests"/>: net8.0 and net10.0
/// run this against the BCL, net472 against the polyfill. Expected values come from
/// <see cref="BigInteger"/>, never from the type under test, so the BCL legs pin
/// the expectations down and the net472 leg checks the polyfill against them.
/// </remarks>
public class Int128ParseTests
{
    private const string Int128Max = "170141183460469231731687303715884105727";
    private const string Int128MinMagnitude = "170141183460469231731687303715884105728";
    private const string UInt128Max = "340282366920938463463374607431768211455";
    private const string UInt128MaxPlusOne = "340282366920938463463374607431768211456";

    // Not in netstandard2.0's NumberStyles, but the BCL type accepts the value.
    private const NumberStyles BinaryNumber =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | (NumberStyles)0x400;

    // Hand-built rather than looked up; see Int128SurfaceTests for why.
    private static readonly CultureInfo Odd = Culture(negative: "neg~", positive: "pos~");

    // A minus sign that is a dash lookalike, as sv-SE's is under ICU.
    private static readonly CultureInfo Minus = Culture(negative: "−", positive: "+");

    private static CultureInfo Culture(string negative, string positive)
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = negative;
        culture.NumberFormat.PositiveSign = positive;
        return culture;
    }

    private static Int128 I(string decimalText) => NumericOracle.ToInt128(BigInteger.Parse(decimalText, CultureInfo.InvariantCulture));

    private static UInt128 U(string decimalText) => NumericOracle.ToUInt128(BigInteger.Parse(decimalText, CultureInfo.InvariantCulture));

    // --- Values ---

    [Theory]
    [InlineData("0", "0")]
    [InlineData("-0", "0")]
    [InlineData("+42", "42")]
    [InlineData("-42", "-42")]
    [InlineData("0000000000000000000000000000000000000000000000000001", "1")]
    [InlineData("18446744073709551616", "18446744073709551616")] // 2^64: crosses the halves
    [InlineData(Int128Max, Int128Max)]
    [InlineData("-" + Int128MinMagnitude, "-" + Int128MinMagnitude)]
    [InlineData(" \t\r\n\v\f-7 \t\r\n\v\f", "-7")]
    [InlineData("42\0\0", "42")]   // the BCL ignores trailing NULs,
    [InlineData("42 \0", "42")]    // after trailing white space as well
    public void Int128ParsesDecimal(string text, string expected)
    {
        Assert.Equal(I(expected), Int128.Parse(text));
        Assert.Equal(I(expected), Int128.Parse(text.AsSpan()));
        Assert.True(Int128.TryParse(text, out Int128 result));
        Assert.Equal(I(expected), result);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("-0", "0")]                  // negative zero is not negative
    [InlineData("-0000", "0")]
    [InlineData("+" + UInt128Max, UInt128Max)]
    [InlineData("000" + UInt128Max, UInt128Max)]
    public void UInt128ParsesDecimal(string text, string expected)
    {
        Assert.Equal(U(expected), UInt128.Parse(text));
        Assert.True(UInt128.TryParse(text.AsSpan(), out UInt128 result));
        Assert.Equal(U(expected), result);
    }

    [Fact]
    public void RoundTripsThroughToString()
    {
        foreach (BigInteger value in NumericOracle.SignedValues(128))
        {
            Int128 x = NumericOracle.ToInt128(value);
            Assert.Equal(x, Int128.Parse(x.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));
            Assert.Equal(x, Int128.Parse(x.ToString("X", CultureInfo.InvariantCulture), NumberStyles.HexNumber));
        }

        foreach (BigInteger value in NumericOracle.UnsignedValues(128))
        {
            UInt128 x = NumericOracle.ToUInt128(value);
            Assert.Equal(x, UInt128.Parse(x.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));
            Assert.Equal(x, UInt128.Parse(x.ToString("X", CultureInfo.InvariantCulture), NumberStyles.HexNumber));
        }
    }

    // --- Failures ---

    [Theory]
    [InlineData(Int128MinMagnitude)]                  // MaxValue + 1
    [InlineData("-170141183460469231731687303715884105729")] // MinValue - 1
    [InlineData(UInt128MaxPlusOne)]
    [InlineData("99999999999999999999999999999999999999999999999999")]
    public void Int128Overflows(string text)
    {
        Assert.Throws<OverflowException>(() => Int128.Parse(text));
        Assert.False(Int128.TryParse(text, out Int128 result));
        Assert.Equal(Int128.Zero, result);
    }

    [Theory]
    [InlineData(UInt128MaxPlusOne)]
    [InlineData("-1")]   // a sign is accepted, but only on zero
    [InlineData("-" + UInt128MaxPlusOne)]
    public void UInt128Overflows(string text)
    {
        Assert.Throws<OverflowException>(() => UInt128.Parse(text));
        Assert.False(UInt128.TryParse(text, out UInt128 result));
        Assert.Equal(UInt128.Zero, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    [InlineData("+-1")]
    [InlineData("- 1")]
    [InlineData("1 2")]
    [InlineData("1-")]
    [InlineData("1,000")]
    [InlineData("1.0")]
    [InlineData("1e3")]
    [InlineData("0x10")]
    [InlineData(" 1")]    // no-break space is not white space here
    [InlineData("1\0x")]       // only NULs may trail
    [InlineData("99999999999999999999999999999999999999999999999x")] // format error outranks overflow
    public void MalformedInputIsAFormatError(string text)
    {
        Assert.Throws<FormatException>(() => Int128.Parse(text));
        Assert.Throws<FormatException>(() => UInt128.Parse(text));
        Assert.False(Int128.TryParse(text, out _));
        Assert.False(UInt128.TryParse(text, out _));
    }

    [Fact]
    public void NullInput()
    {
        Assert.Throws<ArgumentNullException>("s", () => Int128.Parse((string)null!));
        Assert.Throws<ArgumentNullException>("s", () => UInt128.Parse((string)null!, NumberStyles.HexNumber));
        Assert.False(Int128.TryParse((string?)null, out _));
        Assert.False(UInt128.TryParse((string?)null, CultureInfo.InvariantCulture, out _));
    }

    // --- Styles ---

    [Fact]
    public void WhiteSpaceAndSignNeedTheirStyleBits()
    {
        Assert.Throws<FormatException>(() => Int128.Parse(" 1", NumberStyles.None));
        Assert.Throws<FormatException>(() => Int128.Parse("1 ", NumberStyles.AllowLeadingWhite));
        Assert.Throws<FormatException>(() => Int128.Parse("-1", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite));
        Assert.Equal((Int128)(-1), Int128.Parse(" -1", NumberStyles.AllowLeadingWhite | NumberStyles.AllowLeadingSign));
    }

    [Theory]
    [InlineData("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", "-1", UInt128Max)]
    [InlineData("7FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", Int128Max, Int128Max)]
    [InlineData("80000000000000000000000000000000", "-" + Int128MinMagnitude, Int128MinMagnitude)]
    [InlineData("0000000000ffffffffffffffffffffffffffffffff", "-1", UInt128Max)]
    [InlineData(" 1aB\t", "427", "427")]
    public void HexIsTheBitPattern(string text, string asInt128, string asUInt128)
    {
        Assert.Equal(I(asInt128), Int128.Parse(text, NumberStyles.HexNumber));
        Assert.Equal(U(asUInt128), UInt128.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void HexFailures()
    {
        Assert.Throws<OverflowException>(() => Int128.Parse("1" + new string('0', 32), NumberStyles.HexNumber));
        Assert.Throws<OverflowException>(() => UInt128.Parse(new string('F', 33), NumberStyles.HexNumber));
        Assert.Throws<FormatException>(() => Int128.Parse("-1", NumberStyles.HexNumber));
        Assert.Throws<FormatException>(() => Int128.Parse("0x1", NumberStyles.HexNumber));
        Assert.Throws<FormatException>(() => Int128.Parse("G", NumberStyles.HexNumber));
    }

    [Fact]
    public void Binary()
    {
        Assert.Equal((Int128)5, Int128.Parse(" 101 ", BinaryNumber));
        Assert.Equal(Int128.MinValue, Int128.Parse("1" + new string('0', 127), BinaryNumber));
        Assert.Equal(UInt128.MaxValue, UInt128.Parse(new string('1', 128), BinaryNumber));
        Assert.Throws<OverflowException>(() => UInt128.Parse("1" + new string('0', 128), BinaryNumber));
        Assert.Throws<FormatException>(() => Int128.Parse("102", BinaryNumber));
    }

    [Theory]
    [InlineData(0x800)]                  // undefined bit
    [InlineData(0x600)]                  // hex and binary together
    [InlineData(0x204)]                  // hex with a sign
    [InlineData(0x220)]                  // hex with a decimal point
    public void InvalidStylesThrowArgumentException(int style)
    {
        Assert.Throws<ArgumentException>("style", () => Int128.Parse("1", (NumberStyles)style));
        Assert.Throws<ArgumentException>("style", () => UInt128.TryParse("1", (NumberStyles)style, null, out _));

        // TryParse checks the style before the input, so even a null string
        // throws for it. Parse is the other way round: a null string is
        // reported first, whatever the style. Both orders are the BCL's.
        Assert.Throws<ArgumentException>("style", () => Int128.TryParse((string?)null, (NumberStyles)style, null, out _));
        Assert.Throws<ArgumentNullException>("s", () => Int128.Parse((string)null!, (NumberStyles)style, null));
        Assert.Throws<ArgumentNullException>("s", () => UInt128.Parse((string)null!, (NumberStyles)style));
    }

    // --- Provider ---

    [Fact]
    public void SignsComeFromTheProvider()
    {
        Assert.Equal((Int128)(-42), Int128.Parse("neg~42", Odd));
        Assert.Equal((Int128)42, Int128.Parse("pos~42", NumberStyles.Integer, Odd));
        Assert.Equal(UInt128.Zero, UInt128.Parse("neg~0", Odd));
        Assert.Throws<OverflowException>(() => UInt128.Parse("neg~1", Odd));

        // The invariant signs mean nothing to a provider that has others.
        Assert.False(Int128.TryParse("-42", Odd, out _));
        Assert.False(Int128.TryParse("+42".AsSpan(), Odd, out _));
    }

    [Fact]
    public void AHyphenStandsInForADashLikeMinusSign()
    {
        // The BCL accepts '-' wherever the provider's minus sign is U+2212 or
        // one of its lookalikes, so "-5" parses under sv-SE on ICU.
        Assert.Equal((Int128)(-5), Int128.Parse("−5", Minus));
        Assert.Equal((Int128)(-5), Int128.Parse("-5", Minus));
        Assert.Equal((Int128)(-5), Int128.Parse("-5".AsSpan(), NumberStyles.Integer, Minus));

        // The hyphen is tried before the provider's positive sign, so it wins
        // even when the positive sign is itself "-". Odd, but it is what the
        // BCL does for Int128, int and long alike.
        CultureInfo hyphenIsPositive = Culture(negative: "−", positive: "-");
        Assert.Equal((Int128)(-5), Int128.Parse("-5", hyphenIsPositive));
        Assert.Equal(UInt128.Zero, UInt128.Parse("-0", hyphenIsPositive));
        Assert.Throws<OverflowException>(() => UInt128.Parse("-5", hyphenIsPositive));
    }

    [Fact]
    public void ParameterlessParseUsesTheCurrentCulture()
    {
        CultureInfo saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = Odd;
            Assert.Equal((Int128)(-42), Int128.Parse("neg~42"));
            Assert.True(UInt128.TryParse("pos~7", out UInt128 seven));
            Assert.Equal((UInt128)7UL, seven);
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

#if NETFRAMEWORK
    [Theory]
    [InlineData(NumberStyles.Number, "1,000")]
    [InlineData(NumberStyles.Float, "1e3")]
    [InlineData(NumberStyles.Any, "1")]
    [InlineData(NumberStyles.Currency, "1")]
    [InlineData(NumberStyles.AllowThousands, "1")]
    [InlineData(NumberStyles.AllowParentheses, "(1)")]
    [InlineData(NumberStyles.AllowTrailingSign, "1-")]
    [InlineData(NumberStyles.AllowDecimalPoint, "1.0")]
    public void UnsupportedStylesThrowInsteadOfFailingToParse(NumberStyles style, string text)
    {
        // The BCL parses all of these. Returning false would look exactly like
        // bad input, so the polyfill refuses the style outright — from TryParse
        // too, and whatever the input.
        Assert.Throws<NotSupportedException>(() => Int128.Parse(text, style, CultureInfo.InvariantCulture));
        Assert.Throws<NotSupportedException>(() => UInt128.TryParse(text, style, CultureInfo.InvariantCulture, out _));
        Assert.Throws<NotSupportedException>(() => Int128.TryParse("x".AsSpan(), style, null, out _));
    }
#endif
}
