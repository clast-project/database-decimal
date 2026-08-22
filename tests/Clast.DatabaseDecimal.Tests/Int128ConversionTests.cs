// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Buffers.Binary;
using System.Numerics;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// The implicit widenings into <see cref="Int128"/>, which the polyfill has to
/// declare in the same shape as the BCL type it stands in for.
/// </summary>
/// <remarks>
/// These assert at compile time before they assert anything at run time. Every
/// conversion below is written without a cast, so on net472 — which binds to the
/// netstandard2.0 build and therefore to the polyfill — the file fails to
/// <em>build</em> if a widening is missing or has become ambiguous. That is the
/// failure mode worth guarding: it is invisible on net8.0 and net10.0, where the
/// BCL type wins, and it stops a consumer's downlevel leg from compiling at all.
/// <para>
/// The whole set is declared rather than only the widenings a caller trips over,
/// and that is load-bearing. C# resolves a user-defined conversion by finding the
/// most encompassed source type among the candidates, so a partial set works by
/// chaining a standard conversion first — <c>byte</c> reaches <c>Int128</c>
/// through <c>int</c>. Adding a single operator can leave no unique most
/// encompassed type and break conversions that previously worked: promoting only
/// <c>ulong</c> to implicit makes <c>byte</c>, <c>char</c>, <c>ushort</c> and
/// <c>uint</c> all fail with CS0457. So a test that covers only the two
/// widenings which are broken today would pass against a fix that regresses four
/// others.
/// </para>
/// </remarks>
public class Int128ConversionTests
{
    // No casts anywhere in this class. A cast would compile against an explicit
    // conversion and hide exactly what is under test.

    [Fact]
    public void SignedWidenings_KeepTheirSign()
    {
        Int128 fromSByte = (sbyte)-128;
        Int128 fromShort = (short)-32_768;
        Int128 fromInt = int.MinValue;
        Int128 fromLong = long.MinValue;

        Assert.Equal(NumericOracle.ToBig(fromSByte), new BigInteger(-128));
        Assert.Equal(NumericOracle.ToBig(fromShort), new BigInteger(-32_768));
        Assert.Equal(NumericOracle.ToBig(fromInt), new BigInteger(int.MinValue));
        Assert.Equal(NumericOracle.ToBig(fromLong), new BigInteger(long.MinValue));
    }

    [Fact]
    public void UnsignedWidenings_DoNotSignExtend()
    {
        // The interesting half: every one of these has the high bit set in its
        // own width, so a conversion that sign-extends would land on a negative
        // Int128 instead of the value the caller wrote.
        Int128 fromByte = byte.MaxValue;
        Int128 fromChar = char.MaxValue;
        Int128 fromUShort = ushort.MaxValue;
        Int128 fromUInt = uint.MaxValue;
        Int128 fromULong = ulong.MaxValue;

        Assert.Equal(new BigInteger(byte.MaxValue), NumericOracle.ToBig(fromByte));
        Assert.Equal(new BigInteger(char.MaxValue), NumericOracle.ToBig(fromChar));
        Assert.Equal(new BigInteger(ushort.MaxValue), NumericOracle.ToBig(fromUShort));
        Assert.Equal(new BigInteger(uint.MaxValue), NumericOracle.ToBig(fromUInt));
        Assert.Equal(new BigInteger(ulong.MaxValue), NumericOracle.ToBig(fromULong));

        Assert.True(fromULong > Int128.Zero);
    }

    [Fact]
    public void NativeIntegerWidenings_Convert()
    {
        Int128 fromNInt = (nint)(-42);
        Int128 fromNUInt = (nuint)42;

        Assert.Equal(new BigInteger(-42), NumericOracle.ToBig(fromNInt));
        Assert.Equal(new BigInteger(42), NumericOracle.ToBig(fromNUInt));
    }

    [Fact]
    public void AWidenedOperandCombinesWithoutACast()
    {
        // Issue #18's repro: reading a signed 96-bit little-endian integer. The
        // `| low` is the part that did not compile, because it needs Int128 to
        // meet ulong without the caller spelling out a conversion.
        byte[] source = new byte[12];
        BinaryPrimitives.WriteUInt64LittleEndian(source.AsSpan(0, 8), 0xFEDC_BA98_7654_3210UL);
        BinaryPrimitives.WriteInt32LittleEndian(source.AsSpan(8, 4), -2);

        ulong low = BinaryPrimitives.ReadUInt64LittleEndian(source);
        int high = BinaryPrimitives.ReadInt32LittleEndian(source.AsSpan(8));
        Int128 value = ((Int128)high << 64) | low;

        BigInteger expected = (new BigInteger(-2) << 64) | new BigInteger(0xFEDC_BA98_7654_3210UL);
        Assert.Equal(expected, NumericOracle.ToBig(value));
    }

    [Fact]
    public void WidenedOperandsWorkInArithmeticAndComparison()
    {
        // The conversions have to be reachable where a binary operator needs
        // them, not only in an assignment.
        ulong big = ulong.MaxValue;
        uint mid = uint.MaxValue;
        byte small = 7;

        Int128 sum = Int128.One + big;
        Assert.Equal(new BigInteger(ulong.MaxValue) + 1, NumericOracle.ToBig(sum));

        Int128 product = new Int128(0, 2) * mid;
        Assert.Equal(new BigInteger(uint.MaxValue) * 2, NumericOracle.ToBig(product));

        Assert.True(Int128.One < big);
        Assert.True(small < Int128.MaxValue);
    }

    [Fact]
    public void ExistingExplicitCastsStillCompile()
    {
        // Promoting a widening from explicit to implicit is source-compatible in
        // that direction, so callers that already wrote the cast keep building.
        Int128 fromULong = (Int128)ulong.MaxValue;
        Int128 fromUInt = (Int128)uint.MaxValue;

        Assert.Equal(new BigInteger(ulong.MaxValue), NumericOracle.ToBig(fromULong));
        Assert.Equal(new BigInteger(uint.MaxValue), NumericOracle.ToBig(fromUInt));
    }

    [Fact]
    public void NarrowingStaysExplicit()
    {
        // The other direction must not have become implicit by accident: these
        // lose information, and the BCL keeps them explicit too.
        Int128 value = new Int128(0, 300);

        Assert.Equal(300L, (long)value);
        Assert.Equal(300, (int)value);
        Assert.Equal((ushort)300, (ushort)value);
        Assert.Equal((byte)44, (byte)value);   // 300 truncated to eight bits
    }
}
