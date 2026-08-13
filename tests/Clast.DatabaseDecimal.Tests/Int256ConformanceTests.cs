// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Numerics;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// Int256 and UInt256 checked against BigInteger over a boundary-weighted
/// corpus. These types are hand-rolled and ship on every target, so unlike the
/// 128-bit polyfills they have no BCL implementation to fall back on.
/// </summary>
/// <remarks>
/// Checked multiplication is absent below because neither type defines
/// <c>operator checked *</c> — C# then binds <c>checked(a * b)</c> to the
/// unchecked operator and it wraps. That is tracked as its own defect rather
/// than asserted here, since a test cannot both document the current behaviour
/// and demand the correct one. See issue #6.
/// </remarks>
public class Int256ConformanceTests
{
    private static readonly BigInteger Width = NumericOracle.TwoTo256;

    // Built once. As expression-bodied properties these were rebuilt on every
    // access, so the inner loop of each O(n^2) test regenerated the whole corpus
    // per outer element.
    private static readonly IReadOnlyList<BigInteger> Signed = NumericOracle.SignedValues(256);
    private static readonly IReadOnlyList<BigInteger> Unsigned = NumericOracle.UnsignedValues(256);
    private static readonly IReadOnlyList<BigInteger> Signed128 = NumericOracle.SignedValues(128);
    private static readonly IReadOnlyList<BigInteger> Unsigned128 = NumericOracle.UnsignedValues(128);

    private static void AssertSigned(BigInteger expected, Int256 actual, string op)
    {
        BigInteger wrapped = NumericOracle.WrapSigned(expected, Width);
        Assert.True(wrapped == NumericOracle.ToBig(actual),
            $"{op}: expected {wrapped}, got {NumericOracle.ToBig(actual)}");
    }

    private static void AssertUnsigned(BigInteger expected, UInt256 actual, string op)
    {
        BigInteger wrapped = NumericOracle.WrapUnsigned(expected, Width);
        Assert.True(wrapped == NumericOracle.ToBig(actual),
            $"{op}: expected {wrapped}, got {NumericOracle.ToBig(actual)}");
    }

    // ================================================================
    // Int256 arithmetic
    // ================================================================

    [Fact]
    public void Int256_AddSubtractMultiply()
    {
        foreach (BigInteger a in Signed)
        {
            Int256 x = NumericOracle.ToInt256(a);
            foreach (BigInteger b in Signed)
            {
                Int256 y = NumericOracle.ToInt256(b);
                AssertSigned(a + b, x + y, $"{a} + {b}");
                AssertSigned(a - b, x - y, $"{a} - {b}");
                AssertSigned(a * b, x * y, $"{a} * {b}");
            }
        }
    }

    [Fact]
    public void Int256_DivideAndRemainder()
    {
        BigInteger min = -(BigInteger.One << 255);
        foreach (BigInteger a in Signed)
        {
            Int256 x = NumericOracle.ToInt256(a);
            foreach (BigInteger b in Signed)
            {
                if (b.IsZero) continue;
                // MinValue / -1 has no representable result; covered separately.
                if (a == min && b == BigInteger.MinusOne) continue;

                Int256 y = NumericOracle.ToInt256(b);
                AssertSigned(a / b, x / y, $"{a} / {b}");
                AssertSigned(a % b, x % y, $"{a} % {b}");
            }
        }
    }

    [Fact]
    public void Int256_Negate()
    {
        foreach (BigInteger a in Signed)
            AssertSigned(-a, -NumericOracle.ToInt256(a), $"-{a}");
    }

    [Fact]
    public void Int256_Bitwise()
    {
        foreach (BigInteger a in Signed)
        {
            Int256 x = NumericOracle.ToInt256(a);
            AssertSigned(-a - 1, ~x, $"~{a}"); // ~a == -a - 1 in two's complement
            foreach (BigInteger b in Signed)
            {
                Int256 y = NumericOracle.ToInt256(b);
                BigInteger ua = NumericOracle.WrapUnsigned(a, Width);
                BigInteger ub = NumericOracle.WrapUnsigned(b, Width);
                AssertSigned(ua & ub, x & y, $"{a} & {b}");
                AssertSigned(ua | ub, x | y, $"{a} | {b}");
                AssertSigned(ua ^ ub, x ^ y, $"{a} ^ {b}");
            }
        }
    }

    [Fact]
    public void Int256_Shifts()
    {
        foreach (BigInteger a in Signed)
        {
            Int256 x = NumericOracle.ToInt256(a);
            for (int s = 0; s < 256; s++)
            {
                AssertSigned(NumericOracle.ShiftLeft(a, s), x << s, $"{a} << {s}");

                // Arithmetic right shift floors. NumericOracle.ShiftRight computes
                // that as floor division rather than using BigInteger's >>, which
                // disagrees with itself across targets — see the note there.
                AssertSigned(NumericOracle.ShiftRight(a, s), x >> s, $"{a} >> {s}");

                // Logical right shift operates on the unsigned bit pattern.
                BigInteger logical = NumericOracle.ShiftRight(NumericOracle.WrapUnsigned(a, Width), s);
                AssertSigned(logical, x >>> s, $"{a} >>> {s}");
            }
        }
    }

    [Fact]
    public void Int256_Comparisons()
    {
        foreach (BigInteger a in Signed)
        {
            Int256 x = NumericOracle.ToInt256(a);
            foreach (BigInteger b in Signed)
            {
                Int256 y = NumericOracle.ToInt256(b);
                Assert.True((a < b) == (x < y), $"{a} < {b}");
                Assert.True((a > b) == (x > y), $"{a} > {b}");
                Assert.True((a <= b) == (x <= y), $"{a} <= {b}");
                Assert.True((a >= b) == (x >= y), $"{a} >= {b}");
                Assert.True((a == b) == (x == y), $"{a} == {b}");
                Assert.True((a != b) == (x != y), $"{a} != {b}");
                Assert.True(a.CompareTo(b) == Math.Sign(x.CompareTo(y)), $"{a}.CompareTo({b})");
            }
        }
    }

    [Fact]
    public void Int256_AbsAndPredicates()
    {
        BigInteger min = -(BigInteger.One << 255);
        foreach (BigInteger a in Signed)
        {
            Int256 x = NumericOracle.ToInt256(a);
            Assert.True(Int256.IsNegative(x) == (a < 0), $"IsNegative({a})");
            Assert.True(Int256.IsZero(x) == a.IsZero, $"IsZero({a})");
            if (a != min)
                AssertSigned(BigInteger.Abs(a), Int256.Abs(x), $"Abs({a})");
        }
    }

    [Fact]
    public void Int256_ToStringMatchesBigInteger()
    {
        foreach (BigInteger a in Signed)
            Assert.Equal(a.ToString(), NumericOracle.ToInt256(a).ToString());
    }

    [Fact]
    public void Int256_BigMulMatchesBigInteger()
    {
        foreach (BigInteger a in Signed128)
        {
            Int128 x = NumericOracle.ToInt128(a);
            foreach (BigInteger b in Signed128)
            {
                Int128 y = NumericOracle.ToInt128(b);
                AssertSigned(a * b, Int256.BigMul(x, y), $"BigMul({a}, {b})");
            }
        }
    }

    // ================================================================
    // UInt256
    // ================================================================

    [Fact]
    public void UInt256_AddSubtractMultiply()
    {
        foreach (BigInteger a in Unsigned)
        {
            UInt256 x = NumericOracle.ToUInt256(a);
            foreach (BigInteger b in Unsigned)
            {
                UInt256 y = NumericOracle.ToUInt256(b);
                AssertUnsigned(a + b, x + y, $"{a} + {b}");
                AssertUnsigned(a - b, x - y, $"{a} - {b}");
                AssertUnsigned(a * b, x * y, $"{a} * {b}");
            }
        }
    }

    [Fact]
    public void UInt256_DivideAndRemainder()
    {
        foreach (BigInteger a in Unsigned)
        {
            UInt256 x = NumericOracle.ToUInt256(a);
            foreach (BigInteger b in Unsigned)
            {
                if (b.IsZero) continue;
                UInt256 y = NumericOracle.ToUInt256(b);
                AssertUnsigned(a / b, x / y, $"{a} / {b}");
                AssertUnsigned(a % b, x % y, $"{a} % {b}");
            }
        }
    }

    [Fact]
    public void UInt256_Shifts()
    {
        foreach (BigInteger a in Unsigned)
        {
            UInt256 x = NumericOracle.ToUInt256(a);
            for (int s = 0; s < 256; s++)
            {
                AssertUnsigned(NumericOracle.ShiftLeft(a, s), x << s, $"{a} << {s}");
                AssertUnsigned(NumericOracle.ShiftRight(a, s), x >> s, $"{a} >> {s}");
                AssertUnsigned(NumericOracle.ShiftRight(a, s), x >>> s, $"{a} >>> {s}");
            }
        }
    }

    [Fact]
    public void UInt256_Comparisons()
    {
        foreach (BigInteger a in Unsigned)
        {
            UInt256 x = NumericOracle.ToUInt256(a);
            foreach (BigInteger b in Unsigned)
            {
                UInt256 y = NumericOracle.ToUInt256(b);
                Assert.True((a < b) == (x < y), $"{a} < {b}");
                Assert.True((a > b) == (x > y), $"{a} > {b}");
                Assert.True((a <= b) == (x <= y), $"{a} <= {b}");
                Assert.True((a >= b) == (x >= y), $"{a} >= {b}");
                Assert.True((a == b) == (x == y), $"{a} == {b}");
            }
        }
    }

    [Fact]
    public void UInt256_ToStringMatchesBigInteger()
    {
        foreach (BigInteger a in Unsigned)
            Assert.Equal(a.ToString(), NumericOracle.ToUInt256(a).ToString());
    }

    [Fact]
    public void UInt256_BigMulMatchesBigInteger()
    {
        foreach (BigInteger a in Unsigned128)
        {
            UInt128 x = NumericOracle.ToUInt128(a);
            foreach (BigInteger b in Unsigned128)
            {
                UInt128 y = NumericOracle.ToUInt128(b);
                AssertUnsigned(a * b, UInt256.BigMul(x, y), $"BigMul({a}, {b})");
            }
        }
    }

    // ================================================================
    // Checked operators
    // ================================================================

    [Fact]
    public void Int256_CheckedAddSubtract()
    {
        BigInteger max = (BigInteger.One << 255) - 1;
        BigInteger min = -(BigInteger.One << 255);

        foreach (BigInteger a in Signed)
        {
            Int256 x = NumericOracle.ToInt256(a);
            foreach (BigInteger b in Signed)
            {
                Int256 y = NumericOracle.ToInt256(b);

                BigInteger sum = a + b;
                if (sum > max || sum < min)
                    Assert.Throws<OverflowException>(() => checked(x + y));
                else
                    AssertSigned(sum, checked(x + y), $"checked({a} + {b})");

                BigInteger diff = a - b;
                if (diff > max || diff < min)
                    Assert.Throws<OverflowException>(() => checked(x - y));
                else
                    AssertSigned(diff, checked(x - y), $"checked({a} - {b})");
            }
        }
    }

    [Fact]
    public void UInt256_CheckedAddSubtract()
    {
        BigInteger max = (BigInteger.One << 256) - 1;

        foreach (BigInteger a in Unsigned)
        {
            UInt256 x = NumericOracle.ToUInt256(a);
            foreach (BigInteger b in Unsigned)
            {
                UInt256 y = NumericOracle.ToUInt256(b);

                BigInteger sum = a + b;
                if (sum > max) Assert.Throws<OverflowException>(() => checked(x + y));
                else AssertUnsigned(sum, checked(x + y), $"checked({a} + {b})");

                BigInteger diff = a - b;
                if (diff < BigInteger.Zero) Assert.Throws<OverflowException>(() => checked(x - y));
                else AssertUnsigned(diff, checked(x - y), $"checked({a} - {b})");
            }
        }
    }

    [Fact]
    public void Int256_MinValueDividedByMinusOne_Throws()
    {
        Assert.Throws<OverflowException>(() => Int256.MinValue / Int256.MinusOne);
    }

    [Fact]
    public void Int256_DivideByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => Int256.One / Int256.Zero);
        Assert.Throws<DivideByZeroException>(() => Int256.One % Int256.Zero);
        Assert.Throws<DivideByZeroException>(() => UInt256.One / UInt256.Zero);
        Assert.Throws<DivideByZeroException>(() => UInt256.One % UInt256.Zero);
    }
}
