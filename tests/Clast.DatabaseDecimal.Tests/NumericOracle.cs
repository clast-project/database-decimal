// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Numerics;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// Converts the hand-rolled integer types to and from <see cref="BigInteger"/>,
/// which the conformance tests use as ground truth.
/// </summary>
/// <remarks>
/// Conversion goes through shifts and narrowing casts only — never
/// <c>ToString</c>, which is itself under test — so a formatting bug cannot
/// disguise an arithmetic one. Those primitives are load-bearing enough that a
/// fault in them would show up everywhere rather than hide here.
/// <para>
/// Running the same tests on net472 and net8.0+ makes the 128-bit cases
/// differential as well: netstandard2.0 binds to the polyfills while net8.0+
/// binds to the BCL, so the two must agree with BigInteger and therefore with
/// each other.
/// </para>
/// </remarks>
internal static class NumericOracle
{
    public static readonly BigInteger TwoTo128 = BigInteger.One << 128;
    public static readonly BigInteger TwoTo256 = BigInteger.One << 256;
    public static readonly BigInteger TwoTo127 = BigInteger.One << 127;
    public static readonly BigInteger TwoTo255 = BigInteger.One << 255;

    // ================================================================
    // To BigInteger
    // ================================================================

    public static BigInteger ToBig(UInt128 value) =>
        ((BigInteger)(ulong)(value >> 64) << 64) | (ulong)value;

    public static BigInteger ToBig(Int128 value)
    {
        BigInteger raw = ToBig((UInt128)value);
        return raw >= TwoTo127 ? raw - TwoTo128 : raw;
    }

    public static BigInteger ToBig(UInt256 value) =>
        (ToBig((UInt128)(value >> 128)) << 128) | ToBig((UInt128)value);

    public static BigInteger ToBig(Int256 value)
    {
        BigInteger raw = ToBig((UInt256)value);
        return raw >= TwoTo255 ? raw - TwoTo256 : raw;
    }

    // ================================================================
    // From BigInteger, reducing modulo the type's width the way unchecked
    // arithmetic does.
    // ================================================================

    public static UInt128 ToUInt128(BigInteger value)
    {
        BigInteger v = Mod(value, TwoTo128);
        ulong lower = (ulong)(v & ulong.MaxValue);
        ulong upper = (ulong)((v >> 64) & ulong.MaxValue);
        return ((UInt128)upper << 64) | lower;
    }

    public static Int128 ToInt128(BigInteger value) => (Int128)ToUInt128(value);

    public static UInt256 ToUInt256(BigInteger value)
    {
        BigInteger v = Mod(value, TwoTo256);
        return new UInt256(ToUInt128(v >> 128), ToUInt128(v));
    }

    public static Int256 ToInt256(BigInteger value) => (Int256)ToUInt256(value);

    /// <summary>Wraps to the signed range the way unchecked arithmetic does.</summary>
    public static BigInteger WrapSigned(BigInteger value, BigInteger twoToWidth)
    {
        BigInteger half = twoToWidth >> 1;
        BigInteger v = Mod(value, twoToWidth);
        return v >= half ? v - twoToWidth : v;
    }

    public static BigInteger WrapUnsigned(BigInteger value, BigInteger twoToWidth) => Mod(value, twoToWidth);

    private static BigInteger Mod(BigInteger value, BigInteger modulus)
    {
        BigInteger r = value % modulus;
        return r < BigInteger.Zero ? r + modulus : r;
    }

    // ================================================================
    // Shifts, expressed as multiplication and floor division.
    //
    // BigInteger's own >> operator is not usable as an oracle: on .NET
    // Framework it returns 0 for (-(2^64 - 1)) >> 32, where .NET 10 and the
    // types under test both return -4294967296. Whatever the cause, an oracle
    // that disagrees with itself across targets cannot arbitrate between them,
    // and multiply/floor-divide is unambiguous on both.
    // ================================================================

    private static readonly BigInteger[] s_powersOfTwo = BuildPowersOfTwo();

    private static BigInteger[] BuildPowersOfTwo()
    {
        var powers = new BigInteger[257];
        powers[0] = BigInteger.One;
        for (int i = 1; i < powers.Length; i++) powers[i] = powers[i - 1] * 2;
        return powers;
    }

    public static BigInteger ShiftLeft(BigInteger value, int count) => value * s_powersOfTwo[count];

    /// <summary>Arithmetic (sign-propagating, flooring) right shift.</summary>
    public static BigInteger ShiftRight(BigInteger value, int count)
    {
        BigInteger divisor = s_powersOfTwo[count];
        BigInteger quotient = BigInteger.Divide(value, divisor);
        // BigInteger.Divide truncates toward zero; an arithmetic shift floors.
        if (value.Sign < 0 && quotient * divisor != value) quotient -= BigInteger.One;
        return quotient;
    }

    // ================================================================
    // Corpora — boundaries first, then a seeded random tail. The boundaries are
    // where every fault found so far has lived: MinValue, the 2^64 and 2^128
    // limb edges, and the values just either side of them.
    // ================================================================

    public static IReadOnlyList<BigInteger> SignedValues(int bits)
    {
        BigInteger max = (BigInteger.One << (bits - 1)) - 1;
        BigInteger min = -(BigInteger.One << (bits - 1));
        var values = new List<BigInteger>
        {
            0, 1, -1, 2, -2, 3, -3, 10, -10,
            max, max - 1, min, min + 1, min + 2,
            ulong.MaxValue, -(BigInteger)ulong.MaxValue,
            BigInteger.One << 63, -(BigInteger.One << 63),
            BigInteger.One << 64, -(BigInteger.One << 64),
            (BigInteger.One << 64) - 1, (BigInteger.One << 64) + 1,
            BigInteger.Pow(10, 18), -BigInteger.Pow(10, 18),
        };

        if (bits > 128)
        {
            values.Add(BigInteger.One << 127);
            values.Add(-(BigInteger.One << 127));
            values.Add(BigInteger.One << 128);
            values.Add(-(BigInteger.One << 128));
            values.Add((BigInteger.One << 128) - 1);
            values.Add(BigInteger.Pow(10, 38));
            values.Add(-BigInteger.Pow(10, 38));
        }

        AddRandom(values, bits, signed: true);
        return values.Select(v => WrapSigned(v, BigInteger.One << bits)).Distinct().ToList();
    }

    public static IReadOnlyList<BigInteger> UnsignedValues(int bits)
    {
        BigInteger max = (BigInteger.One << bits) - 1;
        var values = new List<BigInteger>
        {
            0, 1, 2, 3, 10,
            max, max - 1,
            ulong.MaxValue, (BigInteger)ulong.MaxValue + 1,
            BigInteger.One << 63, BigInteger.One << 64,
            BigInteger.Pow(10, 18),
        };

        if (bits > 128)
        {
            values.Add(BigInteger.One << 127);
            values.Add(BigInteger.One << 128);
            values.Add((BigInteger.One << 128) - 1);
            values.Add(BigInteger.One << 255);
            values.Add(BigInteger.Pow(10, 38));
            values.Add(BigInteger.Pow(10, 76));
        }

        AddRandom(values, bits, signed: false);
        return values.Select(v => WrapUnsigned(v, BigInteger.One << bits)).Distinct().ToList();
    }

    private static void AddRandom(List<BigInteger> values, int bits, bool signed)
    {
        // Fixed seed: a conformance failure has to be reproducible.
        var rng = new Random(20260812);
        byte[] buffer = new byte[bits / 8];
        for (int i = 0; i < 12; i++)
        {
            rng.NextBytes(buffer);
            // Vary the magnitude so small values are represented too.
            int keep = 1 + (i % (bits / 8));
            for (int k = keep; k < buffer.Length; k++) buffer[k] = 0;

            var v = new BigInteger(buffer.Concat(new byte[] { 0 }).ToArray());
            values.Add(signed && (i % 2 == 1) ? -v : v);
        }
    }
}
