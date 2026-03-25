using DatabaseDecimal.Values;
using Xunit;

namespace DatabaseDecimal.Tests;

public class Int256Tests
{
    // ----------------------------------------------------------------
    // Constants and helpers
    // ----------------------------------------------------------------

    [Fact]
    public void Zero_IsZero()
    {
        Assert.True(Int256.IsZero(Int256.Zero));
        Assert.False(Int256.IsNegative(Int256.Zero));
    }

    [Fact]
    public void One_IsPositive()
    {
        Assert.False(Int256.IsNegative(Int256.One));
        Assert.False(Int256.IsZero(Int256.One));
    }

    [Fact]
    public void MinusOne_IsNegative()
    {
        Assert.True(Int256.IsNegative(Int256.MinusOne));
        Assert.Equal(UInt128.MaxValue, Int256.MinusOne.Upper);
        Assert.Equal(UInt128.MaxValue, Int256.MinusOne.Lower);
    }

    [Fact]
    public void MinValue_IsNegative()
    {
        Assert.True(Int256.IsNegative(Int256.MinValue));
    }

    [Fact]
    public void MaxValue_IsPositive()
    {
        Assert.False(Int256.IsNegative(Int256.MaxValue));
    }

    // ----------------------------------------------------------------
    // Arithmetic: add/subtract
    // ----------------------------------------------------------------

    [Fact]
    public void Add_PositiveNumbers()
    {
        Int256 a = (Int256)100;
        Int256 b = (Int256)200;
        Assert.Equal((Int256)300, a + b);
    }

    [Fact]
    public void Add_NegativeNumbers()
    {
        Int256 a = (Int256)(-50);
        Int256 b = (Int256)(-30);
        Assert.Equal((Int256)(-80), a + b);
    }

    [Fact]
    public void Add_Mixed()
    {
        Int256 a = (Int256)100;
        Int256 b = (Int256)(-30);
        Assert.Equal((Int256)70, a + b);
    }

    [Fact]
    public void Add_Checked_Throws_OnOverflow()
    {
        Assert.Throws<OverflowException>(() => checked(Int256.MaxValue + Int256.One));
    }

    [Fact]
    public void Add_Checked_Throws_OnNegativeOverflow()
    {
        Assert.Throws<OverflowException>(() => checked(Int256.MinValue + Int256.MinusOne));
    }

    [Fact]
    public void Subtract_Basic()
    {
        Int256 a = (Int256)100;
        Int256 b = (Int256)30;
        Assert.Equal((Int256)70, a - b);
    }

    [Fact]
    public void Subtract_NegativeResult()
    {
        Int256 a = (Int256)30;
        Int256 b = (Int256)100;
        Int256 result = a - b;
        Assert.True(Int256.IsNegative(result));
        Assert.Equal((Int256)(-70), result);
    }

    // ----------------------------------------------------------------
    // Negation and Abs
    // ----------------------------------------------------------------

    [Fact]
    public void Negate_Positive()
    {
        Int256 a = (Int256)42;
        Int256 neg = -a;
        Assert.True(Int256.IsNegative(neg));
        Assert.Equal((Int256)(-42), neg);
    }

    [Fact]
    public void Negate_Negative()
    {
        Int256 a = (Int256)(-42);
        Assert.Equal((Int256)42, -a);
    }

    [Fact]
    public void Negate_Zero()
    {
        Assert.Equal(Int256.Zero, -Int256.Zero);
    }

    [Fact]
    public void Abs_Positive()
    {
        Assert.Equal((Int256)42, Int256.Abs((Int256)42));
    }

    [Fact]
    public void Abs_Negative()
    {
        Assert.Equal((Int256)42, Int256.Abs((Int256)(-42)));
    }

    // ----------------------------------------------------------------
    // Multiplication
    // ----------------------------------------------------------------

    [Fact]
    public void Multiply_Simple()
    {
        Int256 a = (Int256)12;
        Int256 b = (Int256)34;
        Assert.Equal((Int256)408, a * b);
    }

    [Fact]
    public void Multiply_NegativeTimesPositive()
    {
        Int256 a = (Int256)(-5);
        Int256 b = (Int256)7;
        Assert.Equal((Int256)(-35), a * b);
    }

    [Fact]
    public void Multiply_NegativeTimesNegative()
    {
        Int256 a = (Int256)(-6);
        Int256 b = (Int256)(-7);
        Assert.Equal((Int256)42, a * b);
    }

    [Fact]
    public void Multiply_ByZero()
    {
        Assert.Equal(Int256.Zero, (Int256)12345 * Int256.Zero);
    }

    // ----------------------------------------------------------------
    // BigMul: Int128 * Int128 -> Int256
    // ----------------------------------------------------------------

    [Fact]
    public void BigMul_PositiveValues()
    {
        Int256 result = Int256.BigMul(1000, 2000);
        Assert.Equal((Int256)2_000_000, result);
    }

    [Fact]
    public void BigMul_NegativeTimesPositive()
    {
        Int256 result = Int256.BigMul(-100, 200);
        Assert.Equal((Int256)(-20_000), result);
    }

    [Fact]
    public void BigMul_NegativeTimesNegative()
    {
        Int256 result = Int256.BigMul(-100, -200);
        Assert.Equal((Int256)20_000, result);
    }

    [Fact]
    public void BigMul_LargeValues()
    {
        // Int128.MaxValue * 2 should produce a 129-bit positive result
        Int256 result = Int256.BigMul(Int128.MaxValue, 2);
        Assert.False(Int256.IsNegative(result));

        // Verify: MaxValue * 2 = 2^128 - 2
        // As Int256: upper should be 0, lower should be 2^128 - 2
        // But wait, 2^128 - 2 doesn't fit in UInt128 (max is 2^128 - 1), so it does fit.
        // Actually Int128.MaxValue = 2^127 - 1, so MaxValue * 2 = 2^128 - 2
        // This exceeds Int128 range but fits in Int256.
        Int256 expected = (Int256)(Int128.MaxValue) + (Int256)(Int128.MaxValue);
        Assert.Equal(expected, result);
    }

    // ----------------------------------------------------------------
    // Division
    // ----------------------------------------------------------------

    [Fact]
    public void Divide_Simple()
    {
        Assert.Equal((Int256)5, (Int256)15 / (Int256)3);
    }

    [Fact]
    public void Divide_NegativeDividend()
    {
        Assert.Equal((Int256)(-5), (Int256)(-15) / (Int256)3);
    }

    [Fact]
    public void Divide_NegativeDivisor()
    {
        Assert.Equal((Int256)(-5), (Int256)15 / (Int256)(-3));
    }

    [Fact]
    public void Divide_BothNegative()
    {
        Assert.Equal((Int256)5, (Int256)(-15) / (Int256)(-3));
    }

    [Fact]
    public void Divide_Truncates()
    {
        Assert.Equal((Int256)3, (Int256)7 / (Int256)2);
    }

    [Fact]
    public void Divide_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => (Int256)1 / Int256.Zero);
    }

    [Fact]
    public void Divide_MinValueByMinusOne_Throws()
    {
        Assert.Throws<OverflowException>(() => Int256.MinValue / Int256.MinusOne);
    }

    // ----------------------------------------------------------------
    // Modulus
    // ----------------------------------------------------------------

    [Fact]
    public void Modulus_Positive()
    {
        Assert.Equal((Int256)1, (Int256)7 % (Int256)2);
    }

    [Fact]
    public void Modulus_NegativeDividend()
    {
        // Remainder has sign of dividend
        Assert.Equal((Int256)(-1), (Int256)(-7) % (Int256)2);
    }

    // ----------------------------------------------------------------
    // Comparison (signed)
    // ----------------------------------------------------------------

    [Fact]
    public void Comparison_NegativeLessThanPositive()
    {
        Assert.True((Int256)(-1) < (Int256)1);
        Assert.True((Int256)1 > (Int256)(-1));
    }

    [Fact]
    public void Comparison_MinValueLessThanMaxValue()
    {
        Assert.True(Int256.MinValue < Int256.MaxValue);
    }

    [Fact]
    public void Comparison_Equal()
    {
        Int256 a = (Int256)(-42);
        Int256 b = (Int256)(-42);
        Assert.True(a == b);
        Assert.True(a <= b);
        Assert.True(a >= b);
    }

    // ----------------------------------------------------------------
    // Shifts
    // ----------------------------------------------------------------

    [Fact]
    public void ArithmeticRightShift_NegativeValue()
    {
        Int256 v = Int256.MinusOne; // all bits set
        Assert.Equal(Int256.MinusOne, v >> 1); // sign-extends
    }

    [Fact]
    public void LogicalRightShift_NegativeValue()
    {
        Int256 v = Int256.MinusOne;
        Int256 result = v >>> 1;
        Assert.Equal(Int256.MaxValue, result); // top bit cleared
    }

    [Fact]
    public void LeftShift_Simple()
    {
        Int256 v = (Int256)1;
        Assert.Equal((Int256)8, v << 3);
    }

    // ----------------------------------------------------------------
    // Conversions
    // ----------------------------------------------------------------

    [Fact]
    public void ImplicitConversion_FromInt()
    {
        Int256 v = 42;
        Assert.Equal((Int256)(Int128)42, v);
    }

    [Fact]
    public void ImplicitConversion_FromNegativeInt()
    {
        Int256 v = -42;
        Assert.True(Int256.IsNegative(v));
        Assert.Equal((Int256)42, -v);
    }

    [Fact]
    public void ExplicitConversion_ToInt128()
    {
        Int256 v = 42;
        Assert.Equal((Int128)42, (Int128)v);
    }

    [Fact]
    public void Reinterpret_UInt256_Int256()
    {
        UInt256 u = UInt256.MaxValue;
        Int256 s = (Int256)u;
        Assert.Equal(Int256.MinusOne, s);
    }

    // ----------------------------------------------------------------
    // ToString
    // ----------------------------------------------------------------

    [Fact]
    public void ToString_Zero()
    {
        Assert.Equal("0", Int256.Zero.ToString());
    }

    [Fact]
    public void ToString_Positive()
    {
        Assert.Equal("12345", ((Int256)12345).ToString());
    }

    [Fact]
    public void ToString_Negative()
    {
        Assert.Equal("-42", ((Int256)(-42)).ToString());
    }

    [Fact]
    public void ToString_MinusOne()
    {
        Assert.Equal("-1", Int256.MinusOne.ToString());
    }

    [Fact]
    public void ToString_MinValue()
    {
        // -2^255 = -57896044618658097711785492504343953926634992332820282019728792003956564819968
        string expected = "-57896044618658097711785492504343953926634992332820282019728792003956564819968";
        Assert.Equal(expected, Int256.MinValue.ToString());
    }

    [Fact]
    public void ToString_MaxValue()
    {
        // 2^255 - 1 = 57896044618658097711785492504343953926634992332820282019728792003956564819967
        string expected = "57896044618658097711785492504343953926634992332820282019728792003956564819967";
        Assert.Equal(expected, Int256.MaxValue.ToString());
    }
}
