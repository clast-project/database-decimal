// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace System;

/// <summary>
/// Internal compatibility helpers for math operations that aren't available
/// on every target. On TFMs that have BCL implementations, delegates to them.
/// </summary>
internal static class MathCompat
{
    /// <summary>
    /// Full 64x64-&gt;128 unsigned multiplication. Returns the high 64 bits;
    /// the low 64 bits are written to <paramref name="low"/>.
    /// </summary>
    public static ulong BigMul64(ulong a, ulong b, out ulong low)
    {
#if NETSTANDARD2_0
        // Hand-rolled 64x64 -> 128 multiplication via 32-bit halves.
        uint al = (uint)a, ah = (uint)(a >> 32);
        uint bl = (uint)b, bh = (uint)(b >> 32);

        ulong mll = (ulong)al * bl;
        ulong mlh = (ulong)al * bh;
        ulong mhl = (ulong)ah * bl;
        ulong mhh = (ulong)ah * bh;

        ulong mid = (mll >> 32) + (uint)mlh + (uint)mhl;
        low = (mll & 0xFFFFFFFFUL) | (mid << 32);
        return mhh + (mlh >> 32) + (mhl >> 32) + (mid >> 32);
#else
        return Math.BigMul(a, b, out low);
#endif
    }

    /// <summary>
    /// Counts the leading zero bits in a 64-bit value. Returns 64 for zero.
    /// </summary>
    public static int LeadingZeroCount(ulong value)
    {
#if NETSTANDARD2_0
        if (value == 0) return 64;
        int count = 0;
        if ((value & 0xFFFFFFFF00000000UL) == 0) { count += 32; value <<= 32; }
        if ((value & 0xFFFF000000000000UL) == 0) { count += 16; value <<= 16; }
        if ((value & 0xFF00000000000000UL) == 0) { count += 8;  value <<= 8;  }
        if ((value & 0xF000000000000000UL) == 0) { count += 4;  value <<= 4;  }
        if ((value & 0xC000000000000000UL) == 0) { count += 2;  value <<= 2;  }
        if ((value & 0x8000000000000000UL) == 0) { count += 1;                }
        return count;
#else
        return System.Numerics.BitOperations.LeadingZeroCount(value);
#endif
    }
}
