// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

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

    [Fact]
    public void Constructor_ScaleExceedsPrecision_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new DecimalType(10, 30));
        Assert.Equal("scale", ex.ParamName);
    }

    [Fact]
    public void Constructor_PrecisionZero_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new DecimalType(0, 0));
        Assert.Equal("precision", ex.ParamName);
    }

    [Fact]
    public void Constructor_PrecisionAboveMax_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new DecimalType(77, 0));
        Assert.Equal("precision", ex.ParamName);
    }

    [Fact]
    public void Constructor_MatchesNumeric()
    {
        Assert.Equal(DecimalType.Numeric(18, 4), new DecimalType(18, 4));
    }

    [Fact]
    public void Deconstruct_YieldsPrecisionAndScale()
    {
        var (precision, scale) = DecimalType.Numeric(18, 4);
        Assert.Equal(18, precision);
        Assert.Equal(4, scale);
    }

    [Fact]
    public void IntegerDigits_NeverNegative_ForConstructibleTypes()
    {
        for (int precision = 1; precision <= DecimalType.MaxPrecision256; precision++)
        {
            for (int scale = 0; scale <= precision; scale++)
                Assert.True(DecimalType.Numeric(precision, scale).IntegerDigits >= 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => DecimalType.Numeric(precision, precision + 1));
        }
    }
}
