using System.Text;
using DatabaseDecimal;
using DatabaseDecimal.Text;
using DatabaseDecimal.Values;
using Xunit;

namespace DatabaseDecimal.Tests;

public class DecimalTextFormatTests
{
    // ----------------------------------------------------------------
    // Format — convenience string methods
    // ----------------------------------------------------------------

    [Theory]
    [InlineData(12345, 5, 2, "123.45")]
    [InlineData(-12345, 5, 2, "-123.45")]
    [InlineData(42, 5, 2, "0.42")]
    [InlineData(5, 5, 2, "0.05")]
    [InlineData(0, 5, 2, "0.00")]
    [InlineData(100, 3, 0, "100")]
    [InlineData(-100, 3, 0, "-100")]
    [InlineData(0, 3, 0, "0")]
    [InlineData(1, 1, 0, "1")]
    [InlineData(999999999, 9, 2, "9999999.99")]
    [InlineData(1000, 5, 3, "1.000")]
    [InlineData(-1, 5, 3, "-0.001")]
    public void Format_Decimal32(int mantissa, int precision, int scale, string expected)
    {
        var type = DecimalType.Numeric(precision, scale);
        var value = new Decimal32(mantissa);
        Assert.Equal(expected, DecimalText.Format(value, type));
    }

    [Theory]
    [InlineData(123456789012345L, 18, 6, "123456789.012345")]
    [InlineData(0L, 10, 3, "0.000")]
    [InlineData(-1L, 10, 3, "-0.001")]
    public void Format_Decimal64(long mantissa, int precision, int scale, string expected)
    {
        var type = DecimalType.Numeric(precision, scale);
        var value = new Decimal64(mantissa);
        Assert.Equal(expected, DecimalText.Format(value, type));
    }

    [Fact]
    public void Format_Decimal128()
    {
        var type = DecimalType.Numeric(20, 5);
        var value = new Decimal128(12345_67890);
        Assert.Equal("12345.67890", DecimalText.Format(value, type));
    }

    [Fact]
    public void Format_Decimal256()
    {
        var type = DecimalType.Numeric(40, 3);
        var value = new Decimal256((Int256)42_000);
        Assert.Equal("42.000", DecimalText.Format(value, type));
    }

    [Fact]
    public void Format_Decimal256_Negative()
    {
        var type = DecimalType.Numeric(40, 2);
        var value = new Decimal256((Int256)(-1));
        Assert.Equal("-0.01", DecimalText.Format(value, type));
    }

    // ----------------------------------------------------------------
    // TryFormat — UTF-16 (Span<char>)
    // ----------------------------------------------------------------

    [Fact]
    public void TryFormat_Char_Basic()
    {
        var type = DecimalType.Numeric(5, 2);
        var value = new Decimal32(12345);
        Span<char> buf = stackalloc char[20];

        Assert.True(DecimalText.TryFormat(value, type, buf, out int written));
        Assert.Equal("123.45", buf.Slice(0, written).ToString());
    }

    [Fact]
    public void TryFormat_Char_BufferTooSmall()
    {
        var type = DecimalType.Numeric(5, 2);
        var value = new Decimal32(12345);
        Span<char> buf = stackalloc char[3]; // too small for "123.45"

        Assert.False(DecimalText.TryFormat(value, type, buf, out int written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void TryFormat_Char_ExactSize()
    {
        var type = DecimalType.Numeric(5, 2);
        var value = new Decimal32(12345);
        Span<char> buf = stackalloc char[6]; // exactly "123.45"

        Assert.True(DecimalText.TryFormat(value, type, buf, out int written));
        Assert.Equal(6, written);
        Assert.Equal("123.45", buf.Slice(0, written).ToString());
    }

    [Fact]
    public void TryFormat_Char_128Bit()
    {
        var type = DecimalType.Numeric(20, 2);
        var value = new Decimal128(42_00);
        Span<char> buf = stackalloc char[20];

        Assert.True(DecimalText.TryFormat(value, type, buf, out int written));
        Assert.Equal("42.00", buf.Slice(0, written).ToString());
    }

    // ----------------------------------------------------------------
    // TryFormat — UTF-8 (Span<byte>)
    // ----------------------------------------------------------------

    [Fact]
    public void TryFormat_Utf8_Basic()
    {
        var type = DecimalType.Numeric(5, 2);
        var value = new Decimal32(12345);
        Span<byte> buf = stackalloc byte[20];

        Assert.True(DecimalText.TryFormat(value, type, buf, out int written));
        Assert.Equal("123.45", Encoding.ASCII.GetString(buf.Slice(0, written).ToArray()));
    }

    [Fact]
    public void TryFormat_Utf8_Negative()
    {
        var type = DecimalType.Numeric(5, 2);
        var value = new Decimal32(-4200);
        Span<byte> buf = stackalloc byte[20];

        Assert.True(DecimalText.TryFormat(value, type, buf, out int written));
        Assert.Equal("-42.00", Encoding.ASCII.GetString(buf.Slice(0, written).ToArray()));
    }

    [Fact]
    public void TryFormat_Utf8_BufferTooSmall()
    {
        var type = DecimalType.Numeric(5, 2);
        var value = new Decimal32(12345);
        Span<byte> buf = stackalloc byte[3];

        Assert.False(DecimalText.TryFormat(value, type, buf, out int written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void TryFormat_Utf8_ScaleZero()
    {
        var type = DecimalType.Numeric(5, 0);
        var value = new Decimal32(42);
        Span<byte> buf = stackalloc byte[10];

        Assert.True(DecimalText.TryFormat(value, type, buf, out int written));
        Assert.Equal("42", Encoding.ASCII.GetString(buf.Slice(0, written).ToArray()));
    }

    [Fact]
    public void TryFormat_Utf8_Zero()
    {
        var type = DecimalType.Numeric(5, 3);
        var value = Decimal64.Zero;
        Span<byte> buf = stackalloc byte[10];

        Assert.True(DecimalText.TryFormat(value, type, buf, out int written));
        Assert.Equal("0.000", Encoding.ASCII.GetString(buf.Slice(0, written).ToArray()));
    }

    // ----------------------------------------------------------------
    // MaxFormattedLength
    // ----------------------------------------------------------------

    [Theory]
    [InlineData(5, 2, 7)]   // sign(1) + 3 int digits + dot(1) + 2 frac = 7
    [InlineData(9, 0, 10)]  // sign(1) + 9 digits = 10
    [InlineData(1, 0, 2)]   // sign(1) + 1 digit = 2
    [InlineData(1, 1, 3)]   // sign(1) + 0 int digits + dot(1) + 1 frac = 3
    public void MaxFormattedLength(int precision, int scale, int expected)
    {
        var type = DecimalType.Numeric(precision, scale);
        Assert.Equal(expected, DecimalText.MaxFormattedLength(type));
    }

    // ----------------------------------------------------------------
    // Format matches existing ToString
    // ----------------------------------------------------------------

    [Theory]
    [InlineData(12345, 5, 2)]
    [InlineData(-4200, 5, 2)]
    [InlineData(0, 5, 2)]
    [InlineData(1, 5, 3)]
    [InlineData(999999999, 9, 2)]
    public void Format_MatchesToString_Decimal32(int mantissa, int precision, int scale)
    {
        var type = DecimalType.Numeric(precision, scale);
        var value = new Decimal32(mantissa);

        string fromFormat = DecimalText.Format(value, type);
        string fromToString = value.ToString((byte)scale);

        Assert.Equal(fromToString, fromFormat);
    }
}
