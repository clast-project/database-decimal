// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Text;
using Clast.DatabaseDecimal;
using Clast.DatabaseDecimal.Text;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

public class DecimalTextParseTests
{
    // ----------------------------------------------------------------
    // Parse — basic decimal values
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("123.45", 5, 2, 12345)]
    [InlineData("-123.45", 5, 2, -12345)]
    [InlineData("+123.45", 5, 2, 12345)]
    [InlineData("0.42", 5, 2, 42)]
    [InlineData("0.05", 5, 2, 5)]
    [InlineData("0.00", 5, 2, 0)]
    [InlineData("100", 5, 2, 10000)]
    [InlineData("1", 5, 2, 100)]
    [InlineData("0", 5, 2, 0)]
    [InlineData("-0.01", 5, 2, -1)]
    public void Parse_Decimal32(string text, int precision, int scale, int expectedMantissa)
    {
        var type = DecimalType.Numeric(precision, scale);
        var result = DecimalText.ParseDecimal32(text, type);
        Assert.Equal(expectedMantissa, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // Parse — integer input (no decimal point)
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("42", 5, 0, 42)]
    [InlineData("-7", 5, 0, -7)]
    [InlineData("0", 1, 0, 0)]
    public void Parse_IntegerOnly(string text, int precision, int scale, int expectedMantissa)
    {
        var type = DecimalType.Numeric(precision, scale);
        var result = DecimalText.ParseDecimal32(text, type);
        Assert.Equal(expectedMantissa, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // Parse — leading zeros
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("007.50", 5, 2, 750)]
    [InlineData("000.01", 5, 2, 1)]
    [InlineData("00100", 5, 0, 100)]
    public void Parse_LeadingZeros(string text, int precision, int scale, int expectedMantissa)
    {
        var type = DecimalType.Numeric(precision, scale);
        var result = DecimalText.ParseDecimal32(text, type);
        Assert.Equal(expectedMantissa, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // Parse — whitespace trimming
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("  123.45  ", 5, 2, 12345)]
    [InlineData("\t42\t", 5, 0, 42)]
    public void Parse_WhitespaceTrimmed(string text, int precision, int scale, int expectedMantissa)
    {
        var type = DecimalType.Numeric(precision, scale);
        var result = DecimalText.ParseDecimal32(text, type);
        Assert.Equal(expectedMantissa, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // Parse — excess fractional digits (rounding)
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("1.235", 5, 2, 124)]    // 1.235 → round half to even → 1.24 (3 is odd, round up)
    [InlineData("1.245", 5, 2, 124)]    // 1.245 → round half to even → 1.24 (4 is even, round down)
    [InlineData("1.236", 5, 2, 124)]    // 1.236 → round up → 1.24
    [InlineData("1.234", 5, 2, 123)]    // 1.234 → round down → 1.23
    [InlineData("1.2351", 5, 2, 124)]   // 1.2351 → > 0.5 → round up → 1.24
    [InlineData("1.2450", 5, 2, 124)]   // 1.2450 → exactly half, 4 is even → 1.24
    [InlineData("1.2451", 5, 2, 125)]   // 1.2451 → > 0.5 → round up → 1.25
    public void Parse_RoundsExcessFractionalDigits(string text, int precision, int scale, int expectedMantissa)
    {
        var type = DecimalType.Numeric(precision, scale);
        var result = DecimalText.ParseDecimal32(text, type);
        Assert.Equal(expectedMantissa, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // Parse — scale padding (fewer fractional digits than target)
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("1.5", 5, 3, 1500)]     // 1.5 → 1.500 → mantissa 1500
    [InlineData("42", 5, 3, 42000)]     // 42 → 42.000 → mantissa 42000
    [InlineData("0.1", 5, 3, 100)]      // 0.1 → 0.100 → mantissa 100
    public void Parse_PadsFractionalDigits(string text, int precision, int scale, int expectedMantissa)
    {
        var type = DecimalType.Numeric(precision, scale);
        var result = DecimalText.ParseDecimal32(text, type);
        Assert.Equal(expectedMantissa, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // TryParse — failure cases
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("")]           // empty
    [InlineData("   ")]        // whitespace only
    [InlineData("abc")]        // non-numeric
    [InlineData("12.34.56")]   // multiple dots
    [InlineData("1e5")]        // scientific notation
    [InlineData("-")]          // sign only
    [InlineData("+")]          // sign only
    [InlineData(".")]          // dot only (no digits)
    public void TryParse_InvalidInput_ReturnsFalse(string text)
    {
        var type = DecimalType.Numeric(9, 2);
        Assert.False(DecimalText.TryParseDecimal32(text, type, out _));
    }

    [Fact]
    public void TryParse_PrecisionExceeded_ReturnsFalse()
    {
        // "999.99" for NUMERIC(4,2): mantissa 99999 >= 10^4 = 10000
        var type = DecimalType.Numeric(4, 2);
        Assert.False(DecimalText.TryParseDecimal32("999.99", type, out _));
    }

    [Fact]
    public void TryParse_OverflowsInt32_ReturnsFalse()
    {
        // Value that fits in NUMERIC(9,0) precision but overflows int32
        var type = DecimalType.Numeric(9, 0);
        Assert.True(DecimalText.TryParseDecimal32("999999999", type, out _));  // fits
        // 10 digits doesn't fit in NUMERIC(9,0) by precision check
        Assert.False(DecimalText.TryParseDecimal32("9999999999", type, out _));
    }

    // ----------------------------------------------------------------
    // Parse — 64-bit
    // ----------------------------------------------------------------

    [Fact]
    public void Parse_Decimal64()
    {
        var type = DecimalType.Numeric(12, 3);
        var result = DecimalText.ParseDecimal64("123456789.012", type);
        Assert.Equal(123456789012L, result.Mantissa);
    }

    [Fact]
    public void Parse_Decimal64_Negative()
    {
        var type = DecimalType.Numeric(12, 3);
        var result = DecimalText.ParseDecimal64("-0.001", type);
        Assert.Equal(-1L, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // Parse — 128-bit
    // ----------------------------------------------------------------

    [Fact]
    public void Parse_Decimal128()
    {
        var type = DecimalType.Numeric(20, 5);
        var result = DecimalText.ParseDecimal128("12345.67890", type);
        Assert.Equal((Int128)1234567890, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // Parse — 256-bit
    // ----------------------------------------------------------------

    [Fact]
    public void Parse_Decimal256()
    {
        var type = DecimalType.Numeric(40, 3);
        var result = DecimalText.ParseDecimal256("42.000", type);
        Assert.Equal((Int256)42000, result.Mantissa);
    }

    [Fact]
    public void Parse_Decimal256_LargeValue()
    {
        var type = DecimalType.Numeric(50, 0);
        // A value that exceeds Int128 range
        string text = "99999999999999999999999999999999999999999"; // 41 digits
        var result = DecimalText.ParseDecimal256(text, type);
        Assert.False(Int256.IsNegative(result.Mantissa));
        Assert.Equal(text, result.Mantissa.ToString());
    }

    // ----------------------------------------------------------------
    // Parse — UTF-8
    // ----------------------------------------------------------------

    [Fact]
    public void Parse_Utf8_Decimal32()
    {
        var type = DecimalType.Numeric(5, 2);
        byte[] utf8 = "123.45"u8.ToArray();
        var result = DecimalText.ParseDecimal32(utf8, type);
        Assert.Equal(12345, result.Mantissa);
    }

    [Fact]
    public void Parse_Utf8_Negative()
    {
        var type = DecimalType.Numeric(5, 2);
        byte[] utf8 = "-42.00"u8.ToArray();
        var result = DecimalText.ParseDecimal32(utf8, type);
        Assert.Equal(-4200, result.Mantissa);
    }

    [Fact]
    public void Parse_Utf8_WithWhitespace()
    {
        var type = DecimalType.Numeric(5, 2);
        byte[] utf8 = "  1.50  "u8.ToArray();
        var result = DecimalText.ParseDecimal32(utf8, type);
        Assert.Equal(150, result.Mantissa);
    }

    [Fact]
    public void TryParse_Utf8_Invalid()
    {
        var type = DecimalType.Numeric(5, 2);
        byte[] utf8 = "abc"u8.ToArray();
        Assert.False(DecimalText.TryParseDecimal32(utf8, type, out _));
    }

    // The UTF-8 parsers decode as Encoding.UTF8 does, so they agree with the UTF-16 parsers on the
    // decoded text: non-ASCII whitespace trims, and malformed bytes (U+FFFD) never parse.

    [Fact]
    public void Parse_Utf8_TrimsNonAsciiWhitespace_LikeTheCharParser()
    {
        var type = DecimalType.Numeric(5, 2);
        byte[] nbsp = [0x31, 0x2E, 0x35, 0xC2, 0xA0];              // "1.5" + U+00A0
        byte[] emSpace = [0xE2, 0x80, 0x83, 0x31, 0x2E, 0x35];     // U+2003 + "1.5"
        Assert.Equal(150, DecimalText.ParseDecimal32(nbsp, type).Mantissa);
        Assert.Equal(150, DecimalText.ParseDecimal32(emSpace, type).Mantissa);
    }

    [Fact]
    public void TryParse_Utf8_RejectsMalformedBytes_ThatWideningWouldTrim()
    {
        // A lone 0xA0 or 0x85 widened to a char is NBSP / NEL, which Trim() removes.
        var type = DecimalType.Numeric(5, 2);
        Assert.False(DecimalText.TryParseDecimal32([0x31, 0x2E, 0x35, 0xA0], type, out _));
        Assert.False(DecimalText.TryParseDecimal64([0x85, 0x37], type, out _));
    }

    [Fact]
    public void TryParse_Utf8_MatchesTheCharParser_OnDecodedText()
    {
        var rng = new Random(20261111);
        byte[][] pieces =
        [
            [0x30], [0x35], [0x39], [0x2D], [0x2E], [0x20], [0x09], [0x65], [0x78],
            [0xC2, 0xA0], [0xE2, 0x80, 0x83], [0xD9, 0xA3], [0xF0, 0x9F, 0x98, 0x80],
            [0xA0], [0x85], [0xC2], [0xFF],
        ];
        var types = new[] { DecimalType.Numeric(5, 2), DecimalType.Numeric(18, 4), DecimalType.Numeric(38, 10), DecimalType.Numeric(76, 20) };
        for (var i = 0; i < 20_000; i++)
        {
            var bytes = new List<byte>();
            var n = rng.Next(0, i % 100 == 0 ? 120 : 8); // now and then past the stack buffer
            for (var k = 0; k < n; k++)
            {
                bytes.AddRange(pieces[rng.Next(2) == 0 ? rng.Next(3) : rng.Next(pieces.Length)]);
            }

            var utf8 = bytes.ToArray();
            var text = System.Text.Encoding.UTF8.GetString(utf8).AsSpan();
            var type = types[rng.Next(types.Length)];
            var hex = BitConverter.ToString(utf8);
            Assert.True(DecimalText.TryParseDecimal256(utf8, type, out var b256) == DecimalText.TryParseDecimal256(text, type, out var c256) && b256.Equals(c256), $"256 [{hex}]");
            if (type.Precision <= 38)
            {
                Assert.True(DecimalText.TryParseDecimal128(utf8, type, out var b128) == DecimalText.TryParseDecimal128(text, type, out var c128) && b128.Equals(c128), $"128 [{hex}]");
            }

            if (type.Precision <= 18)
            {
                Assert.True(DecimalText.TryParseDecimal64(utf8, type, out var b64) == DecimalText.TryParseDecimal64(text, type, out var c64) && b64.Equals(c64), $"64 [{hex}]");
            }

            if (type.Precision <= 9)
            {
                Assert.True(DecimalText.TryParseDecimal32(utf8, type, out var b32) == DecimalText.TryParseDecimal32(text, type, out var c32) && b32.Equals(c32), $"32 [{hex}]");
            }
        }
    }

    [Fact]
    public void TryParse_Utf8_LongInput_DoesNotDependOnStackSize()
    {
        // Was stackalloc char[utf8.Length]: a 1 MB input overflowed a 1 MB thread stack and killed the
        // process. Now it parses (well-formed) or fails (padding past any precision) like the char path.
        var type = DecimalType.Numeric(10, 2);
        var padded = new byte[1_000_000];
        padded.AsSpan().Fill(0x20);
        "12.34"u8.CopyTo(padded.AsSpan(500_000));
        bool? parsed = null;
        long mantissa = 0;
        var thread = new Thread(() =>
        {
            parsed = DecimalText.TryParseDecimal64(padded, type, out var value);
            mantissa = value.Mantissa;
        }, maxStackSize: 256 * 1024);
        thread.Start();
        thread.Join();
        Assert.True(parsed);
        Assert.Equal(1234, mantissa);
    }

    [Fact]
    public void Parse_Utf8_Decimal64()
    {
        var type = DecimalType.Numeric(12, 3);
        byte[] utf8 = "123456789.012"u8.ToArray();
        var result = DecimalText.ParseDecimal64(utf8, type);
        Assert.Equal(123456789012L, result.Mantissa);
    }

    [Fact]
    public void Parse_Utf8_Decimal128()
    {
        var type = DecimalType.Numeric(20, 5);
        byte[] utf8 = "12345.67890"u8.ToArray();
        var result = DecimalText.ParseDecimal128(utf8, type);
        Assert.Equal((Int128)1234567890, result.Mantissa);
    }

    [Fact]
    public void Parse_Utf8_Decimal256()
    {
        var type = DecimalType.Numeric(40, 3);
        byte[] utf8 = "42.000"u8.ToArray();
        var result = DecimalText.ParseDecimal256(utf8, type);
        Assert.Equal((Int256)42000, result.Mantissa);
    }

    // ----------------------------------------------------------------
    // Parse throws on invalid input
    // ----------------------------------------------------------------

    [Fact]
    public void Parse_Throws_OnInvalidInput()
    {
        var type = DecimalType.Numeric(5, 2);
        Assert.Throws<FormatException>(() => DecimalText.ParseDecimal32("abc", type));
    }

    [Fact]
    public void Parse_Utf8_Throws_OnInvalidInput()
    {
        var type = DecimalType.Numeric(5, 2);
        Assert.Throws<FormatException>(() => DecimalText.ParseDecimal32("abc"u8, type));
    }

    // ----------------------------------------------------------------
    // Round-trip: Format(Parse(text)) == text
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("123.45", 5, 2)]
    [InlineData("-0.01", 5, 2)]
    [InlineData("0.00", 5, 2)]
    [InlineData("42", 5, 0)]
    [InlineData("9999999.99", 9, 2)]
    [InlineData("0.001", 5, 3)]
    public void Roundtrip_ParseThenFormat_Decimal32(string text, int precision, int scale)
    {
        var type = DecimalType.Numeric(precision, scale);
        var parsed = DecimalText.ParseDecimal32(text, type);
        string formatted = DecimalText.Format(parsed, type);
        Assert.Equal(text, formatted);
    }

    [Theory]
    [InlineData("123.45", 5, 2)]
    [InlineData("-42.00", 5, 2)]
    [InlineData("0.000", 5, 3)]
    public void Roundtrip_FormatThenParse_Decimal32(string text, int precision, int scale)
    {
        var type = DecimalType.Numeric(precision, scale);
        var value = DecimalText.ParseDecimal32(text, type);
        string formatted = DecimalText.Format(value, type);
        var reparsed = DecimalText.ParseDecimal32(formatted, type);
        Assert.Equal(value, reparsed);
    }

    [Fact]
    public void Roundtrip_Utf8_Decimal32()
    {
        var type = DecimalType.Numeric(5, 2);
        var value = new Decimal32(12345);

        // Format to UTF-8
        Span<byte> buf = stackalloc byte[20];
        Assert.True(DecimalText.TryFormat(value, type, buf, out int written));

        // Parse back from UTF-8
        var reparsed = DecimalText.ParseDecimal32(buf[..written], type);
        Assert.Equal(value, reparsed);
    }

    [Fact]
    public void Roundtrip_Utf8_Decimal64()
    {
        var type = DecimalType.Numeric(12, 3);
        var value = new Decimal64(123456789012L);

        Span<byte> buf = stackalloc byte[20];
        Assert.True(DecimalText.TryFormat(value, type, buf, out int written));

        var reparsed = DecimalText.ParseDecimal64(buf[..written], type);
        Assert.Equal(value, reparsed);
    }
}
