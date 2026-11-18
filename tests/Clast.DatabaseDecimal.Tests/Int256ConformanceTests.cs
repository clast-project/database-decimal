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
/// Checked multiplication is covered here now that both types define
/// <c>operator checked *</c>. Before they did, C# bound <c>checked(a * b)</c>
/// to the unchecked operator and the overflow passed silently.
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

    // ================================================================
    // Division by a divisor below 2^64 — the limb-wise path. The corpus above
    // holds few such divisors, and none chosen to stress the digit estimate.
    // ================================================================

    /// <summary>
    /// Divisors that exercise every normalisation shift and both correction
    /// loops of the 128-by-64 step: the powers of ten the rescales use, values
    /// either side of each power of two, and divisors whose low 32-bit digit is
    /// all ones or zero, which push the digit estimate furthest off.
    /// </summary>
    private static IReadOnlyList<ulong> SmallDivisors()
    {
        var divisors = new List<ulong> { 1, 2, 3, 7, 10, ulong.MaxValue, ulong.MaxValue - 1 };
        ulong power = 1;
        for (int i = 0; i <= 19; i++)
        {
            divisors.Add(power);
            if (i < 19) power *= 10;
        }
        for (int k = 1; k < 64; k++)
        {
            ulong p = 1UL << k;
            divisors.Add(p);
            divisors.Add(p - 1);
            divisors.Add(p + 1);
        }
        divisors.Add(0x8000_0000_0000_0001UL);
        divisors.Add(0x8000_0000_FFFF_FFFFUL);
        divisors.Add(0xFFFF_FFFF_0000_0000UL);
        divisors.Add(0x0000_0001_FFFF_FFFFUL);
        divisors.Add(0x0000_0001_0000_0000UL);
        divisors.Add(0x7FFF_FFFF_8000_0000UL);

        var rng = new Random(20261117);
        byte[] buffer = new byte[8];
        for (int i = 0; i < 40; i++)
        {
            rng.NextBytes(buffer);
            ulong value = BitConverter.ToUInt64(buffer, 0) >> rng.Next(64);
            if (value != 0) divisors.Add(value);
        }

        return divisors.Distinct().ToList();
    }

    [Fact]
    public void UInt256_DivideBySmallDivisor()
    {
        // Dividends built to sit just below and at multiples of the divisor, so
        // every limb's partial remainder is as large as it can be.
        IReadOnlyList<ulong> divisors = SmallDivisors();
        var dividends = new List<BigInteger>(Unsigned);
        BigInteger max = Width - 1;
        dividends.Add(max);
        dividends.Add(max >> 64);
        dividends.Add(max >> 128);

        foreach (ulong d in divisors)
        {
            var divisor = new BigInteger(d);
            UInt256 y = NumericOracle.ToUInt256(divisor);
            var cases = new List<BigInteger>(dividends)
            {
                max - (max % divisor),
                max - (max % divisor) - 1,
                // Below 2^128 with the largest remainder, so the top limbs are zero.
                ((max >> 128) / divisor) * divisor - 1,
            };

            foreach (BigInteger a in cases)
            {
                if (a.Sign < 0) continue;
                UInt256 x = NumericOracle.ToUInt256(a);
                var (q, r) = UInt256.DivRem(x, y);
                AssertUnsigned(a / divisor, q, $"{a} / {d}");
                AssertUnsigned(a % divisor, r, $"{a} % {d}");
            }
        }
    }

    [Fact]
    public void UInt256_DivRem128By64()
    {
        var rng = new Random(1117);
        byte[] buffer = new byte[8];
        foreach (ulong d in SmallDivisors())
        {
            var divisor = new BigInteger(d);
            // high must be below the divisor. Take the extremes and a spread.
            var highs = new List<ulong> { 0, d - 1, (d - 1) / 2, d > 1 ? d - 2 : 0 };
            for (int i = 0; i < 8; i++)
            {
                rng.NextBytes(buffer);
                highs.Add(BitConverter.ToUInt64(buffer, 0) % d);
            }

            foreach (ulong high in highs.Distinct())
            {
                foreach (ulong low in new[] { 0UL, 1UL, ulong.MaxValue, 0x8000_0000_0000_0000UL, 0x0000_0000_FFFF_FFFFUL, 0xFFFF_FFFF_0000_0000UL })
                {
                    BigInteger n = (new BigInteger(high) << 64) + low;
                    ulong q = UInt256.DivRem128By64(high, low, d, out ulong r);
                    Assert.True(n / divisor == q && n % divisor == r,
                        $"{high}:{low} / {d}: expected {n / divisor} r {n % divisor}, got {q} r {r}");
                }
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
    public void Int256_CheckedMultiply()
    {
        BigInteger max = (BigInteger.One << 255) - 1;
        BigInteger min = -(BigInteger.One << 255);

        foreach (BigInteger a in Signed)
        {
            Int256 x = NumericOracle.ToInt256(a);
            foreach (BigInteger b in Signed)
            {
                Int256 y = NumericOracle.ToInt256(b);
                BigInteger product = a * b;

                if (product > max || product < min)
                    Assert.Throws<OverflowException>(() => checked(x * y));
                else
                    AssertSigned(product, checked(x * y), $"checked({a} * {b})");
            }
        }
    }

    [Fact]
    public void UInt256_CheckedMultiply()
    {
        BigInteger max = (BigInteger.One << 256) - 1;

        foreach (BigInteger a in Unsigned)
        {
            UInt256 x = NumericOracle.ToUInt256(a);
            foreach (BigInteger b in Unsigned)
            {
                UInt256 y = NumericOracle.ToUInt256(b);
                BigInteger product = a * b;

                if (product > max)
                    Assert.Throws<OverflowException>(() => checked(x * y));
                else
                    AssertUnsigned(product, checked(x * y), $"checked({a} * {b})");
            }
        }
    }

    /// <summary>
    /// The band where the bit-length test cannot decide on its own: operands
    /// whose lengths sum to exactly 257, where the product may or may not fit.
    /// </summary>
    [Fact]
    public void UInt256_CheckedMultiply_AmbiguousBitLengthBand()
    {
        UInt256 twoTo127 = NumericOracle.ToUInt256(BigInteger.One << 127);
        UInt256 twoTo128 = NumericOracle.ToUInt256(BigInteger.One << 128);

        // 128 + 129 == 257, and 2^255 fits.
        Assert.Equal(NumericOracle.ToUInt256(BigInteger.One << 255), checked(twoTo127 * twoTo128));

        // 129 + 129 == 258, and 2^256 does not.
        Assert.Throws<OverflowException>(() => checked(twoTo128 * twoTo128));

        // Just inside and just outside on the same bit lengths.
        UInt256 max = UInt256.MaxValue;
        Assert.Throws<OverflowException>(() => checked(max * (UInt256)2UL));
        Assert.Equal(max, checked(max * UInt256.One));
    }

    /// <summary>The case reported in issue #6.</summary>
    [Fact]
    public void Int256_CheckedMultiply_NoLongerWrapsToZero()
    {
        Int256 twoTo128 = Int256.One << 128;
        Assert.Throws<OverflowException>(() => checked(twoTo128 * twoTo128));

        Int256 twoTo254 = Int256.One << 254;
        Assert.Throws<OverflowException>(() => checked(twoTo254 * (Int256)4));

        // -2^255 is representable, so this one must not throw.
        Int256 twoTo127 = Int256.One << 127;
        Assert.Equal(Int256.MinValue, checked(twoTo127 * -(Int256.One << 128)));

        // +2^255 is not.
        Assert.Throws<OverflowException>(() => checked(twoTo127 * (Int256.One << 128)));
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
