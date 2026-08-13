// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Numerics;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// Int128 and UInt128 checked against BigInteger.
/// </summary>
/// <remarks>
/// On net8.0 and later these bind to the BCL types; on netstandard2.0 they bind
/// to this repo's polyfills. Running the same bodies on every target therefore
/// does double duty: BigInteger proves both implementations correct, and any
/// case where net472 and net10.0 disagree is a polyfill divergence by
/// construction. Two such divergences — inverted checked addition and a checked
/// multiply that wrapped at 2^128 — were only ever caught by a test that
/// happened to straddle a boundary. Both are covered here now: reintroducing
/// either fault fails <see cref="Int128_CheckedAddSubtract"/> or
/// <see cref="Int128_CheckedMultiply"/> on net472.
/// </remarks>
public class Int128ConformanceTests
{
    private static readonly BigInteger Width = NumericOracle.TwoTo128;
    private static readonly BigInteger SignedMax = (BigInteger.One << 127) - 1;
    private static readonly BigInteger SignedMin = -(BigInteger.One << 127);
    private static readonly BigInteger UnsignedMax = (BigInteger.One << 128) - 1;

    // Built once; as expression-bodied properties the inner loop of each O(n^2)
    // test regenerated the whole corpus per outer element.
    private static readonly IReadOnlyList<BigInteger> Signed = NumericOracle.SignedValues(128);
    private static readonly IReadOnlyList<BigInteger> Unsigned = NumericOracle.UnsignedValues(128);
    private static readonly IReadOnlyList<BigInteger> Signed64 = NumericOracle.SignedValues(64);

    private static void AssertSigned(BigInteger expected, Int128 actual, string op)
    {
        BigInteger wrapped = NumericOracle.WrapSigned(expected, Width);
        Assert.True(wrapped == NumericOracle.ToBig(actual),
            $"{op}: expected {wrapped}, got {NumericOracle.ToBig(actual)}");
    }

    private static void AssertUnsigned(BigInteger expected, UInt128 actual, string op)
    {
        BigInteger wrapped = NumericOracle.WrapUnsigned(expected, Width);
        Assert.True(wrapped == NumericOracle.ToBig(actual),
            $"{op}: expected {wrapped}, got {NumericOracle.ToBig(actual)}");
    }

    // ================================================================
    // Int128 unchecked arithmetic
    // ================================================================

    [Fact]
    public void Int128_AddSubtractMultiply()
    {
        foreach (BigInteger a in Signed)
        {
            Int128 x = NumericOracle.ToInt128(a);
            foreach (BigInteger b in Signed)
            {
                Int128 y = NumericOracle.ToInt128(b);
                AssertSigned(a + b, x + y, $"{a} + {b}");
                AssertSigned(a - b, x - y, $"{a} - {b}");
                AssertSigned(a * b, x * y, $"{a} * {b}");
            }
        }
    }

    [Fact]
    public void Int128_DivideAndRemainder()
    {
        foreach (BigInteger a in Signed)
        {
            Int128 x = NumericOracle.ToInt128(a);
            foreach (BigInteger b in Signed)
            {
                if (b.IsZero) continue;
                if (a == SignedMin && b == BigInteger.MinusOne) continue;

                Int128 y = NumericOracle.ToInt128(b);
                AssertSigned(a / b, x / y, $"{a} / {b}");
                AssertSigned(a % b, x % y, $"{a} % {b}");
            }
        }
    }

    [Fact]
    public void Int128_NegateAndBitwise()
    {
        foreach (BigInteger a in Signed)
        {
            Int128 x = NumericOracle.ToInt128(a);
            AssertSigned(-a, -x, $"-{a}");
            AssertSigned(-a - 1, ~x, $"~{a}");

            foreach (BigInteger b in Signed)
            {
                Int128 y = NumericOracle.ToInt128(b);
                BigInteger ua = NumericOracle.WrapUnsigned(a, Width);
                BigInteger ub = NumericOracle.WrapUnsigned(b, Width);
                AssertSigned(ua & ub, x & y, $"{a} & {b}");
                AssertSigned(ua | ub, x | y, $"{a} | {b}");
                AssertSigned(ua ^ ub, x ^ y, $"{a} ^ {b}");
            }
        }
    }

    [Fact]
    public void Int128_Shifts()
    {
        foreach (BigInteger a in Signed)
        {
            Int128 x = NumericOracle.ToInt128(a);
            for (int s = 0; s < 128; s++)
            {
                AssertSigned(NumericOracle.ShiftLeft(a, s), x << s, $"{a} << {s}");
                AssertSigned(NumericOracle.ShiftRight(a, s), x >> s, $"{a} >> {s}");
                AssertSigned(NumericOracle.ShiftRight(NumericOracle.WrapUnsigned(a, Width), s), x >>> s, $"{a} >>> {s}");
            }
        }
    }

    [Fact]
    public void Int128_Comparisons()
    {
        foreach (BigInteger a in Signed)
        {
            Int128 x = NumericOracle.ToInt128(a);
            foreach (BigInteger b in Signed)
            {
                Int128 y = NumericOracle.ToInt128(b);
                Assert.True((a < b) == (x < y), $"{a} < {b}");
                Assert.True((a > b) == (x > y), $"{a} > {b}");
                Assert.True((a <= b) == (x <= y), $"{a} <= {b}");
                Assert.True((a >= b) == (x >= y), $"{a} >= {b}");
                Assert.True((a == b) == (x == y), $"{a} == {b}");
                Assert.True(a.CompareTo(b) == Math.Sign(x.CompareTo(y)), $"{a}.CompareTo({b})");
            }
        }
    }

    [Fact]
    public void Int128_AbsAndToString()
    {
        foreach (BigInteger a in Signed)
        {
            Int128 x = NumericOracle.ToInt128(a);
            Assert.Equal(a.ToString(), x.ToString());
            if (a != SignedMin)
                AssertSigned(BigInteger.Abs(a), Int128.Abs(x), $"Abs({a})");
        }
    }

    // ================================================================
    // Int128 checked arithmetic — where both known polyfill faults lived
    // ================================================================

    [Fact]
    public void Int128_CheckedAddSubtract()
    {
        foreach (BigInteger a in Signed)
        {
            Int128 x = NumericOracle.ToInt128(a);
            foreach (BigInteger b in Signed)
            {
                Int128 y = NumericOracle.ToInt128(b);

                BigInteger sum = a + b;
                if (sum > SignedMax || sum < SignedMin)
                    Assert.Throws<OverflowException>(() => checked(x + y));
                else
                    AssertSigned(sum, checked(x + y), $"checked({a} + {b})");

                BigInteger diff = a - b;
                if (diff > SignedMax || diff < SignedMin)
                    Assert.Throws<OverflowException>(() => checked(x - y));
                else
                    AssertSigned(diff, checked(x - y), $"checked({a} - {b})");
            }
        }
    }

    [Fact]
    public void Int128_CheckedMultiply()
    {
        foreach (BigInteger a in Signed)
        {
            Int128 x = NumericOracle.ToInt128(a);
            foreach (BigInteger b in Signed)
            {
                Int128 y = NumericOracle.ToInt128(b);
                BigInteger product = a * b;

                if (product > SignedMax || product < SignedMin)
                    Assert.Throws<OverflowException>(() => checked(x * y));
                else
                    AssertSigned(product, checked(x * y), $"checked({a} * {b})");
            }
        }
    }

    [Fact]
    public void Int128_CheckedNegate()
    {
        foreach (BigInteger a in Signed)
        {
            Int128 x = NumericOracle.ToInt128(a);
            if (a == SignedMin) Assert.Throws<OverflowException>(() => checked(-x));
            else AssertSigned(-a, checked(-x), $"checked(-{a})");
        }
    }

    [Fact]
    public void Int128_MinValueDividedByMinusOne_Throws()
    {
        Assert.Throws<OverflowException>(() => Int128.MinValue / (Int128)(-1));
    }

    [Fact]
    public void Int128_DivideByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => Int128.One / Int128.Zero);
        Assert.Throws<DivideByZeroException>(() => Int128.One % Int128.Zero);
        Assert.Throws<DivideByZeroException>(() => UInt128.One / UInt128.Zero);
        Assert.Throws<DivideByZeroException>(() => UInt128.One % UInt128.Zero);
    }

    // ================================================================
    // UInt128
    // ================================================================

    [Fact]
    public void UInt128_AddSubtractMultiply()
    {
        foreach (BigInteger a in Unsigned)
        {
            UInt128 x = NumericOracle.ToUInt128(a);
            foreach (BigInteger b in Unsigned)
            {
                UInt128 y = NumericOracle.ToUInt128(b);
                AssertUnsigned(a + b, x + y, $"{a} + {b}");
                AssertUnsigned(a - b, x - y, $"{a} - {b}");
                AssertUnsigned(a * b, x * y, $"{a} * {b}");
            }
        }
    }

    [Fact]
    public void UInt128_DivideAndRemainder()
    {
        foreach (BigInteger a in Unsigned)
        {
            UInt128 x = NumericOracle.ToUInt128(a);
            foreach (BigInteger b in Unsigned)
            {
                if (b.IsZero) continue;
                UInt128 y = NumericOracle.ToUInt128(b);
                AssertUnsigned(a / b, x / y, $"{a} / {b}");
                AssertUnsigned(a % b, x % y, $"{a} % {b}");
            }
        }
    }

    [Fact]
    public void UInt128_Shifts()
    {
        foreach (BigInteger a in Unsigned)
        {
            UInt128 x = NumericOracle.ToUInt128(a);
            for (int s = 0; s < 128; s++)
            {
                AssertUnsigned(NumericOracle.ShiftLeft(a, s), x << s, $"{a} << {s}");
                AssertUnsigned(NumericOracle.ShiftRight(a, s), x >> s, $"{a} >> {s}");
                AssertUnsigned(NumericOracle.ShiftRight(a, s), x >>> s, $"{a} >>> {s}");
            }
        }
    }

    [Fact]
    public void UInt128_ComparisonsAndToString()
    {
        foreach (BigInteger a in Unsigned)
        {
            UInt128 x = NumericOracle.ToUInt128(a);
            Assert.Equal(a.ToString(), x.ToString());
            foreach (BigInteger b in Unsigned)
            {
                UInt128 y = NumericOracle.ToUInt128(b);
                Assert.True((a < b) == (x < y), $"{a} < {b}");
                Assert.True((a > b) == (x > y), $"{a} > {b}");
                Assert.True((a <= b) == (x <= y), $"{a} <= {b}");
                Assert.True((a >= b) == (x >= y), $"{a} >= {b}");
                Assert.True((a == b) == (x == y), $"{a} == {b}");
                Assert.True(a.CompareTo(b) == Math.Sign(x.CompareTo(y)), $"{a}.CompareTo({b})");
            }
        }
    }

    [Fact]
    public void UInt128_CheckedAddSubtract()
    {
        foreach (BigInteger a in Unsigned)
        {
            UInt128 x = NumericOracle.ToUInt128(a);
            foreach (BigInteger b in Unsigned)
            {
                UInt128 y = NumericOracle.ToUInt128(b);

                BigInteger sum = a + b;
                if (sum > UnsignedMax) Assert.Throws<OverflowException>(() => checked(x + y));
                else AssertUnsigned(sum, checked(x + y), $"checked({a} + {b})");

                BigInteger diff = a - b;
                if (diff < BigInteger.Zero) Assert.Throws<OverflowException>(() => checked(x - y));
                else AssertUnsigned(diff, checked(x - y), $"checked({a} - {b})");
            }
        }
    }

    [Fact]
    public void UInt128_CheckedMultiply()
    {
        foreach (BigInteger a in Unsigned)
        {
            UInt128 x = NumericOracle.ToUInt128(a);
            foreach (BigInteger b in Unsigned)
            {
                UInt128 y = NumericOracle.ToUInt128(b);
                BigInteger product = a * b;

                if (product > UnsignedMax)
                    Assert.Throws<OverflowException>(() => checked(x * y));
                else
                    AssertUnsigned(product, checked(x * y), $"checked({a} * {b})");
            }
        }
    }

    // ================================================================
    // Conversions — truncating, matching the BCL's unchecked casts
    // ================================================================

    [Fact]
    public void Int128_NarrowingConversions()
    {
        BigInteger mask64 = (BigInteger.One << 64) - 1;
        BigInteger mask32 = (BigInteger.One << 32) - 1;

        foreach (BigInteger a in Signed)
        {
            Int128 x = NumericOracle.ToInt128(a);
            BigInteger raw = NumericOracle.WrapUnsigned(a, Width);

            Assert.True((ulong)x == (ulong)(raw & mask64), $"(ulong){a}");
            Assert.True((uint)x == (uint)(raw & mask32), $"(uint){a}");
            Assert.True((long)x == unchecked((long)(ulong)(raw & mask64)), $"(long){a}");
            Assert.True((int)x == unchecked((int)(uint)(raw & mask32)), $"(int){a}");
        }
    }

    [Fact]
    public void Int128_RoundTripsThroughUInt128()
    {
        foreach (BigInteger a in Signed)
        {
            Int128 x = NumericOracle.ToInt128(a);
            AssertSigned(a, (Int128)(UInt128)x, $"roundtrip {a}");
        }
    }

    [Fact]
    public void Int128_WideningConversions()
    {
        foreach (BigInteger a in Signed64)
        {
            long v = (long)NumericOracle.WrapSigned(a, BigInteger.One << 64);
            AssertSigned(v, (Int128)v, $"(Int128){v}");
            AssertSigned((int)(v & int.MaxValue), (Int128)(int)(v & int.MaxValue), $"(Int128)(int){v}");
        }
    }
}
