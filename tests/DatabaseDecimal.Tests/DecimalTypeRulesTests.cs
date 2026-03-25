using DatabaseDecimal;
using DatabaseDecimal.Arithmetic;
using Xunit;

namespace DatabaseDecimal.Tests;

public class DecimalTypeRulesTests
{
    // --- Addition ---

    [Fact]
    public void Add_SameType()
    {
        // NUMERIC(5,2) + NUMERIC(5,2) => max(2,2) + max(3,3) + 1 = 6, scale 2
        var result = DecimalTypeRules.Add(
            DecimalType.Numeric(5, 2),
            DecimalType.Numeric(5, 2));

        Assert.Equal(DecimalType.Numeric(6, 2), result);
    }

    [Fact]
    public void Add_DifferentScales()
    {
        // NUMERIC(5,2) + NUMERIC(6,3) => scale=max(2,3)=3, prec=max(3,3)+1+3=7
        var result = DecimalTypeRules.Add(
            DecimalType.Numeric(5, 2),
            DecimalType.Numeric(6, 3));

        Assert.Equal(DecimalType.Numeric(7, 3), result);
    }

    [Fact]
    public void Add_IntegerOnly()
    {
        // NUMERIC(4,0) + NUMERIC(6,0) => scale=0, prec=max(4,6)+1+0=7
        var result = DecimalTypeRules.Add(
            DecimalType.Numeric(4, 0),
            DecimalType.Numeric(6, 0));

        Assert.Equal(DecimalType.Numeric(7, 0), result);
    }

    // --- Multiplication ---

    [Fact]
    public void Multiply_Basic()
    {
        // NUMERIC(5,2) * NUMERIC(4,1) => prec=5+4+1=10, scale=2+1=3
        var result = DecimalTypeRules.Multiply(
            DecimalType.Numeric(5, 2),
            DecimalType.Numeric(4, 1));

        Assert.Equal(DecimalType.Numeric(10, 3), result);
    }

    [Fact]
    public void Multiply_PrecisionClamped()
    {
        // NUMERIC(20,5) * NUMERIC(20,5) => prec=41, scale=10
        // Clamped: delta=3, scale=max(10-3, min(10,6))=7, prec=38
        var result = DecimalTypeRules.Multiply(
            DecimalType.Numeric(20, 5),
            DecimalType.Numeric(20, 5));

        Assert.Equal(38, result.Precision);
        Assert.Equal(7, result.Scale);
    }

    // --- Division ---

    [Fact]
    public void Divide_Basic()
    {
        // NUMERIC(9,2) / NUMERIC(5,3) => scale=max(6, 2+5+1)=8, prec=(9-2)+5+8=20
        var result = DecimalTypeRules.Divide(
            DecimalType.Numeric(9, 2),
            DecimalType.Numeric(5, 3));

        Assert.Equal(DecimalType.Numeric(20, 8), result);
    }

    [Fact]
    public void Divide_MinimumScale6()
    {
        // NUMERIC(3,0) / NUMERIC(2,0) => scale=max(6, 0+2+1)=6, prec=3+2+6=11
        var result = DecimalTypeRules.Divide(
            DecimalType.Numeric(3, 0),
            DecimalType.Numeric(2, 0));

        Assert.Equal(DecimalType.Numeric(11, 6), result);
    }

    // --- Modulus ---

    [Fact]
    public void Modulus_Basic()
    {
        // NUMERIC(8,2) % NUMERIC(5,3) => scale=max(2,3)=3, prec=min(6,2)+3=5
        var result = DecimalTypeRules.Modulus(
            DecimalType.Numeric(8, 2),
            DecimalType.Numeric(5, 3));

        Assert.Equal(DecimalType.Numeric(5, 3), result);
    }

    // --- Subtract uses same rules as Add ---

    [Fact]
    public void Subtract_SameAsAdd()
    {
        var left = DecimalType.Numeric(5, 2);
        var right = DecimalType.Numeric(6, 3);

        Assert.Equal(DecimalTypeRules.Add(left, right), DecimalTypeRules.Subtract(left, right));
    }
}
