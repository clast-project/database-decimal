using DatabaseDecimal.Values;
using Xunit;

namespace DatabaseDecimal.Tests;

public class UInt256Tests
{
    // ----------------------------------------------------------------
    // Constants and basics
    // ----------------------------------------------------------------

    [Fact]
    public void Zero_IsAllZeroBits()
    {
        Assert.Equal(UInt128.Zero, UInt256.Zero.Upper);
        Assert.Equal(UInt128.Zero, UInt256.Zero.Lower);
    }

    [Fact]
    public void One_HasLowerBitSet()
    {
        Assert.Equal(UInt128.Zero, UInt256.One.Upper);
        Assert.Equal(UInt128.One, UInt256.One.Lower);
    }

    [Fact]
    public void MaxValue_IsAllOneBits()
    {
        Assert.Equal(UInt128.MaxValue, UInt256.MaxValue.Upper);
        Assert.Equal(UInt128.MaxValue, UInt256.MaxValue.Lower);
    }

    // ----------------------------------------------------------------
    // Addition
    // ----------------------------------------------------------------

    [Fact]
    public void Add_Simple()
    {
        UInt256 a = (UInt256)100UL;
        UInt256 b = (UInt256)200UL;
        Assert.Equal((UInt256)300UL, a + b);
    }

    [Fact]
    public void Add_WithCarry()
    {
        // Lower overflows, carry into upper
        UInt256 a = new(UInt128.Zero, UInt128.MaxValue);
        UInt256 b = UInt256.One;
        UInt256 result = a + b;

        Assert.Equal(UInt128.One, result.Upper);
        Assert.Equal(UInt128.Zero, result.Lower);
    }

    [Fact]
    public void Add_Wraps_Unchecked()
    {
        UInt256 result = UInt256.MaxValue + UInt256.One;
        Assert.Equal(UInt256.Zero, result);
    }

    [Fact]
    public void Add_Checked_Throws_OnOverflow()
    {
        Assert.Throws<OverflowException>(() => checked(UInt256.MaxValue + UInt256.One));
    }

    // ----------------------------------------------------------------
    // Subtraction
    // ----------------------------------------------------------------

    [Fact]
    public void Subtract_Simple()
    {
        UInt256 a = (UInt256)500UL;
        UInt256 b = (UInt256)200UL;
        Assert.Equal((UInt256)300UL, a - b);
    }

    [Fact]
    public void Subtract_WithBorrow()
    {
        UInt256 a = new(UInt128.One, UInt128.Zero); // 2^128
        UInt256 b = UInt256.One;
        UInt256 result = a - b;

        Assert.Equal(UInt128.Zero, result.Upper);
        Assert.Equal(UInt128.MaxValue, result.Lower);
    }

    [Fact]
    public void Subtract_Checked_Throws_OnUnderflow()
    {
        Assert.Throws<OverflowException>(() => checked(UInt256.Zero - UInt256.One));
    }

    // ----------------------------------------------------------------
    // Multiplication
    // ----------------------------------------------------------------

    [Fact]
    public void Multiply_Simple()
    {
        UInt256 a = (UInt256)12UL;
        UInt256 b = (UInt256)34UL;
        Assert.Equal((UInt256)408UL, a * b);
    }

    [Fact]
    public void Multiply_Large_StaysIn256Bits()
    {
        // (2^128 - 1) * 2 = 2^129 - 2
        UInt256 a = (UInt256)UInt128.MaxValue;
        UInt256 b = (UInt256)2UL;
        UInt256 result = a * b;

        // Upper should be 1, lower should be MaxValue - 1
        Assert.Equal(UInt128.One, result.Upper);
        Assert.Equal(UInt128.MaxValue - 1, result.Lower);
    }

    [Fact]
    public void Multiply_ByZero()
    {
        Assert.Equal(UInt256.Zero, (UInt256)12345UL * UInt256.Zero);
    }

    [Fact]
    public void Multiply_ByOne()
    {
        UInt256 v = new(42, 99);
        Assert.Equal(v, v * UInt256.One);
    }

    // ----------------------------------------------------------------
    // BigMul: UInt128 * UInt128 -> UInt256
    // ----------------------------------------------------------------

    [Fact]
    public void BigMul_Small()
    {
        UInt256 result = UInt256.BigMul(100, 200);
        Assert.Equal(UInt128.Zero, result.Upper);
        Assert.Equal((UInt128)20000, result.Lower);
    }

    [Fact]
    public void BigMul_MaxValues()
    {
        // (2^128-1)^2 = 2^256 - 2^129 + 1
        // Upper 128 bits = 2^128 - 2 = UInt128.MaxValue - 1
        // Lower 128 bits = 1
        UInt256 result = UInt256.BigMul(UInt128.MaxValue, UInt128.MaxValue);

        Assert.Equal(UInt128.MaxValue - 1, result.Upper);
        Assert.Equal(UInt128.One, result.Lower);
    }

    [Fact]
    public void BigMul_PowersOf2()
    {
        // 2^64 * 2^64 = 2^128
        UInt128 twoTo64 = new(1, 0);
        UInt256 result = UInt256.BigMul(twoTo64, twoTo64);

        // 2^128 = UInt256(upper=1, lower=0) -- but actually 2^128 means bit 128 set
        // Which is UInt256(upper=1, lower=0)
        Assert.Equal(UInt128.One, result.Upper);
        Assert.Equal(UInt128.Zero, result.Lower);
    }

    // ----------------------------------------------------------------
    // Division and modulus
    // ----------------------------------------------------------------

    [Fact]
    public void Divide_Simple()
    {
        Assert.Equal((UInt256)5UL, (UInt256)15UL / (UInt256)3UL);
    }

    [Fact]
    public void Divide_WithRemainder()
    {
        Assert.Equal((UInt256)3UL, (UInt256)7UL / (UInt256)2UL);
    }

    [Fact]
    public void Modulus_Simple()
    {
        Assert.Equal((UInt256)1UL, (UInt256)7UL % (UInt256)2UL);
    }

    [Fact]
    public void Divide_Large()
    {
        // MaxValue / 2
        UInt256 result = UInt256.MaxValue / (UInt256)2UL;
        // Should be 2^255 - 1 (since 2^256-1 is odd, division truncates)
        Assert.Equal(new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), result.Upper);
        Assert.Equal(UInt128.MaxValue, result.Lower);
    }

    [Fact]
    public void Divide_BySelf()
    {
        UInt256 v = new(42, 12345);
        Assert.Equal(UInt256.One, v / v);
    }

    [Fact]
    public void Divide_ByLarger_ReturnsZero()
    {
        Assert.Equal(UInt256.Zero, (UInt256)3UL / (UInt256)5UL);
    }

    [Fact]
    public void Divide_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => (UInt256)1UL / UInt256.Zero);
    }

    [Fact]
    public void DivRem_Consistency()
    {
        UInt256 a = new(100, UInt128.MaxValue - 42);
        UInt256 b = new(0, 1_000_000_007);
        UInt256 q = a / b;
        UInt256 r = a % b;
        Assert.Equal(a, q * b + r);
        Assert.True(r < b);
    }

    // ----------------------------------------------------------------
    // Comparison
    // ----------------------------------------------------------------

    [Fact]
    public void Comparison_Equal()
    {
        UInt256 a = new(10, 20);
        UInt256 b = new(10, 20);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.True(a >= b);
        Assert.True(a <= b);
    }

    [Fact]
    public void Comparison_LessThan_ByUpper()
    {
        UInt256 a = new(1, UInt128.MaxValue);
        UInt256 b = new(2, 0);
        Assert.True(a < b);
        Assert.True(b > a);
    }

    [Fact]
    public void Comparison_LessThan_ByLower()
    {
        UInt256 a = new(5, 10);
        UInt256 b = new(5, 20);
        Assert.True(a < b);
    }

    // ----------------------------------------------------------------
    // Shifts
    // ----------------------------------------------------------------

    [Fact]
    public void LeftShift_Small()
    {
        UInt256 v = UInt256.One;
        Assert.Equal((UInt256)8UL, v << 3);
    }

    [Fact]
    public void LeftShift_AcrossBoundary()
    {
        UInt256 v = new(0, UInt128.One << 127); // bit 127 set
        UInt256 result = v << 1;
        Assert.Equal(UInt128.One, result.Upper);
        Assert.Equal(UInt128.Zero, result.Lower);
    }

    [Fact]
    public void LeftShift_FullyCrossover()
    {
        UInt256 v = (UInt256)1UL;
        UInt256 result = v << 128;
        Assert.Equal(UInt128.One, result.Upper);
        Assert.Equal(UInt128.Zero, result.Lower);
    }

    [Fact]
    public void RightShift_Simple()
    {
        UInt256 v = (UInt256)16UL;
        Assert.Equal((UInt256)2UL, v >>> 3);
    }

    [Fact]
    public void RightShift_AcrossBoundary()
    {
        UInt256 v = new(UInt128.One, UInt128.Zero); // bit 128 set
        UInt256 result = v >>> 1;
        Assert.Equal(UInt128.Zero, result.Upper);
        Assert.Equal(UInt128.One << 127, result.Lower);
    }

    [Fact]
    public void Shift_Zero_ReturnsOriginal()
    {
        UInt256 v = new(42, 99);
        Assert.Equal(v, v << 0);
        Assert.Equal(v, v >>> 0);
    }

    // ----------------------------------------------------------------
    // Bitwise
    // ----------------------------------------------------------------

    [Fact]
    public void BitwiseNot()
    {
        Assert.Equal(UInt256.MaxValue, ~UInt256.Zero);
        Assert.Equal(UInt256.Zero, ~UInt256.MaxValue);
    }

    [Fact]
    public void BitwiseAnd()
    {
        UInt256 a = new(0xFF, 0xFF);
        UInt256 b = new(0x0F, 0xF0);
        UInt256 result = a & b;
        Assert.Equal((UInt128)0x0F, result.Upper);
        Assert.Equal((UInt128)0xF0, result.Lower);
    }

    // ----------------------------------------------------------------
    // Conversions
    // ----------------------------------------------------------------

    [Fact]
    public void ImplicitConversion_FromUlong()
    {
        UInt256 v = 42UL;
        Assert.Equal(UInt128.Zero, v.Upper);
        Assert.Equal((UInt128)42, v.Lower);
    }

    [Fact]
    public void ExplicitConversion_ToUInt128()
    {
        UInt256 v = new(999, 42);
        Assert.Equal((UInt128)42, (UInt128)v);
    }

    // ----------------------------------------------------------------
    // ToString
    // ----------------------------------------------------------------

    [Fact]
    public void ToString_Zero()
    {
        Assert.Equal("0", UInt256.Zero.ToString());
    }

    [Fact]
    public void ToString_Small()
    {
        Assert.Equal("12345", ((UInt256)12345UL).ToString());
    }

    [Fact]
    public void ToString_Large()
    {
        // 2^128 = 340282366920938463463374607431768211456
        UInt256 v = new(UInt128.One, UInt128.Zero);
        Assert.Equal("340282366920938463463374607431768211456", v.ToString());
    }

    [Fact]
    public void ToString_MaxValue()
    {
        // 2^256 - 1 = 115792089237316195423570985008687907853269984665640564039457584007913129639935
        string expected = "115792089237316195423570985008687907853269984665640564039457584007913129639935";
        Assert.Equal(expected, UInt256.MaxValue.ToString());
    }

    [Fact]
    public void ToString_Roundtrip_Via_Multiply_Small()
    {
        // 10^9 * 10^9 = 10^18
        UInt256 result = UInt256.BigMul(1_000_000_000UL, 1_000_000_000UL);
        Assert.Equal("1000000000000000000", result.ToString());
    }

    [Fact]
    public void ToString_Roundtrip_Via_Multiply_Large()
    {
        // 10^18 * 10^18 = 10^36 (37 characters: 1 followed by 36 zeros)
        UInt128 tenTo18 = 1_000_000_000_000_000_000UL;
        UInt256 result = UInt256.BigMul(tenTo18, tenTo18);
        string s = result.ToString();
        Assert.Equal(37, s.Length);
        Assert.Equal('1', s[0]);
        Assert.True(s[1..].All(c => c == '0'));
    }

    [Fact]
    public void BigMul_Divisibility_Check()
    {
        // 10^20 * 10^20 = 10^40, verify via division
        UInt128 tenTo20 = (UInt128)10_000_000_000_000_000_000UL * 10;
        UInt256 tenTo40 = UInt256.BigMul(tenTo20, tenTo20);

        // 10^40 / 10^20 should equal 10^20
        UInt256 quotient = tenTo40 / (UInt256)tenTo20;
        Assert.Equal((UInt256)tenTo20, quotient);

        // 10^40 % 10^20 should be 0
        Assert.Equal(UInt256.Zero, tenTo40 % (UInt256)tenTo20);
    }
}
