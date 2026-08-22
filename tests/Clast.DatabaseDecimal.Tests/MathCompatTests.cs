// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// The bit-counting compatibility helpers, against a naive shift-one-at-a-time
/// reference.
/// </summary>
/// <remarks>
/// The reference is deliberately not <c>BitOperations</c>: that type does not
/// exist on net472, which is the one configuration where these helpers run
/// anything other than a forward to the BCL. A loop that shifts a bit at a time
/// is obviously correct, works on every target, and differs enough from the
/// branchless binary search in the fallback to catch a mistake in it.
/// <para>
/// So on net8.0 and later this checks that the BCL matches the contract the
/// library relies on — 64 for zero in particular — and on net472 it checks the
/// hand-rolled fallback the netstandard2.0 build uses. Windows CI covers the
/// second case.
/// </para>
/// </remarks>
public class MathCompatTests
{
    private static int NaiveTrailingZeroCount(ulong value)
    {
        if (value == 0) return 64;
        int count = 0;
        while ((value & 1UL) == 0) { count++; value >>= 1; }
        return count;
    }

    private static int NaiveLeadingZeroCount(ulong value)
    {
        if (value == 0) return 64;
        int count = 0;
        while ((value & 0x8000_0000_0000_0000UL) == 0) { count++; value <<= 1; }
        return count;
    }

    public static TheoryData<ulong> Interesting
    {
        get
        {
            var data = new TheoryData<ulong>
            {
                0UL,
                1UL,
                2UL,
                3UL,
                ulong.MaxValue,
                0x8000_0000_0000_0000UL,
                0x0000_0001_0000_0000UL,
                0x0000_0000_8000_0000UL,
                0xFFFF_FFFF_0000_0000UL,
                0x0000_0000_FFFF_FFFFUL,
            };

            // Every single-bit value, which pins the count at each position.
            for (int bit = 0; bit < 64; bit++) data.Add(1UL << bit);

            // A bit set with arbitrary rubbish above it, so a fallback that
            // stops scanning at the wrong step would be caught.
            for (int bit = 0; bit < 64; bit++) data.Add((1UL << bit) | (0xA5A5A5A5A5A5A5A5UL << bit));

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Interesting))]
    public void TrailingZeroCount_MatchesTheNaiveReference(ulong value)
    {
        Assert.Equal(NaiveTrailingZeroCount(value), MathCompat.TrailingZeroCount(value));
    }

    [Theory]
    [MemberData(nameof(Interesting))]
    public void LeadingZeroCount_MatchesTheNaiveReference(ulong value)
    {
        Assert.Equal(NaiveLeadingZeroCount(value), MathCompat.LeadingZeroCount(value));
    }

    [Fact]
    public void BitCounts_MatchTheNaiveReference_OverARandomSweep()
    {
        var rng = new Random(99);
        for (int i = 0; i < 5_000; i++)
        {
            unchecked
            {
                ulong hi = (uint)rng.Next(int.MinValue, int.MaxValue);
                ulong lo = (uint)rng.Next(int.MinValue, int.MaxValue);
                ulong value = (hi << 32) | lo;
                Assert.Equal(NaiveTrailingZeroCount(value), MathCompat.TrailingZeroCount(value));
                Assert.Equal(NaiveLeadingZeroCount(value), MathCompat.LeadingZeroCount(value));
            }
        }
    }
}
