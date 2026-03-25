using DatabaseDecimal;
using Xunit;

namespace DatabaseDecimal.Tests;

public class DecimalTypeTests
{
    [Theory]
    [InlineData(1, 0, DecimalWidth.W32)]
    [InlineData(9, 2, DecimalWidth.W32)]
    [InlineData(10, 0, DecimalWidth.W64)]
    [InlineData(18, 6, DecimalWidth.W64)]
    [InlineData(19, 0, DecimalWidth.W128)]
    [InlineData(38, 10, DecimalWidth.W128)]
    [InlineData(39, 0, DecimalWidth.W256)]
    [InlineData(76, 30, DecimalWidth.W256)]
    public void Width_DerivedFromPrecision(int precision, int scale, DecimalWidth expected)
    {
        var dt = DecimalType.Numeric(precision, scale);
        Assert.Equal(expected, dt.Width);
    }

    [Fact]
    public void IntegerDigits_IsPrecisionMinusScale()
    {
        var dt = DecimalType.Numeric(7, 3);
        Assert.Equal(4, dt.IntegerDigits);
    }

    [Fact]
    public void Numeric_PrecisionTooLow_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DecimalType.Numeric(0, 0));
    }

    [Fact]
    public void Numeric_PrecisionTooHigh_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DecimalType.Numeric(77, 0));
    }

    [Fact]
    public void Numeric_ScaleExceedsPrecision_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DecimalType.Numeric(5, 6));
    }

    [Fact]
    public void Numeric_NegativeScale_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DecimalType.Numeric(5, -1));
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        Assert.Equal("NUMERIC(5,2)", DecimalType.Numeric(5, 2).ToString());
    }

    [Fact]
    public void Equality_SamePrecisionAndScale()
    {
        var a = DecimalType.Numeric(5, 2);
        var b = DecimalType.Numeric(5, 2);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Inequality_DifferentScale()
    {
        var a = DecimalType.Numeric(5, 2);
        var b = DecimalType.Numeric(5, 3);
        Assert.NotEqual(a, b);
    }
}
