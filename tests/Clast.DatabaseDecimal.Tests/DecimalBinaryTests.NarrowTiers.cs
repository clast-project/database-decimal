// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Clast.DatabaseDecimal.Binary;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// The 32- and 64-bit tiers, and the decimal-typed column overloads. Shares the
/// BigInteger oracle in the other half of the class.
/// </summary>
public partial class DecimalBinaryTests
{
    // ================================================================
    // Scalar — the narrow tiers
    // ================================================================

    [Fact]
    public void Int32_RoundTripsEveryWidthAndOrder()
    {
        for (int width = 1; width <= 4; width++)
        {
            foreach (DecimalByteOrder order in Orders)
            {
                foreach (BigInteger big in Representable(width))
                {
                    int value = (int)big;
                    byte[] field = new byte[width];

                    Assert.True(DecimalBinary.TryWriteInt32(value, field, order),
                        $"width {width}, order {order}, value {big}");
                    Assert.Equal(Expected(big, width, order), field);
                    Assert.Equal(value, DecimalBinary.ReadInt32(field, order));
                }
            }
        }
    }

    [Fact]
    public void Int64_RoundTripsEveryWidthAndOrder()
    {
        for (int width = 1; width <= 8; width++)
        {
            foreach (DecimalByteOrder order in Orders)
            {
                foreach (BigInteger big in Representable(width))
                {
                    long value = (long)big;
                    byte[] field = new byte[width];

                    Assert.True(DecimalBinary.TryWriteInt64(value, field, order),
                        $"width {width}, order {order}, value {big}");
                    Assert.Equal(Expected(big, width, order), field);
                    Assert.Equal(value, DecimalBinary.ReadInt64(field, order));
                }
            }
        }
    }

    [Theory]
    [InlineData(DecimalByteOrder.LittleEndian)]
    [InlineData(DecimalByteOrder.BigEndian)]
    public void Int32_NarrowField_SignExtendsAndChecksFit(DecimalByteOrder order)
    {
        // Parquet stores a DECIMAL(6,2) in three bytes; -1 is 0xFFFFFF there.
        byte[] minusOne = [0xFF, 0xFF, 0xFF];
        Assert.Equal(-1, DecimalBinary.ReadInt32(minusOne, order));

        Assert.True(DecimalBinary.TryWriteInt32(-999_999, new byte[3], order));
        Assert.False(DecimalBinary.TryWriteInt32(8_388_608, new byte[3], order)); // 2^23 needs a fourth byte
        Assert.True(DecimalBinary.TryWriteInt32(8_388_607, new byte[3], order));  // 2^23 - 1 is the edge
        Assert.True(DecimalBinary.TryWriteInt32(-8_388_608, new byte[3], order)); // and so is -2^23
    }

    [Theory]
    [InlineData(DecimalByteOrder.LittleEndian)]
    [InlineData(DecimalByteOrder.BigEndian)]
    public void Int32_WiderFieldThanTheMantissa(DecimalByteOrder order)
    {
        // A 32-bit mantissa in an 8-byte field: sign extension on the way out,
        // and a value beyond 32 bits refused on the way back in.
        byte[] field = new byte[8];
        DecimalBinary.WriteInt32(-5, field, order);
        Assert.Equal(Expected(-5, 8, order), field);
        Assert.Equal(-5, DecimalBinary.ReadInt32(field, order));

        byte[] tooWide = Expected(BigInteger.One << 40, 8, order);
        Assert.False(DecimalBinary.TryReadInt32(tooWide, order, out _));
    }

    // ================================================================
    // Bulk — the narrow tiers
    // ================================================================

    [Theory]
    [InlineData(4, DecimalByteOrder.LittleEndian)] // natural width: wholesale copy
    [InlineData(4, DecimalByteOrder.BigEndian)]    // natural width: byte swap per element
    [InlineData(3, DecimalByteOrder.BigEndian)]    // Parquet's 3-byte decimal
    [InlineData(1, DecimalByteOrder.LittleEndian)]
    public void BulkInt32_RoundTrips(int width, DecimalByteOrder order)
    {
        BigInteger[] values = Representable(width).ToArray();
        int[] mantissas = values.Select(v => (int)v).ToArray();

        byte[] buffer = new byte[values.Length * width];
        DecimalBinary.WriteInt32(mantissas, buffer, width, order);

        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(Expected(values[i], width, order),
                buffer.Skip(i * width).Take(width).ToArray());
        }

        int[] read = new int[values.Length];
        DecimalBinary.ReadInt32(buffer, width, order, read);
        Assert.Equal(mantissas, read);
    }

    [Theory]
    [InlineData(8, DecimalByteOrder.LittleEndian)]
    [InlineData(8, DecimalByteOrder.BigEndian)]
    [InlineData(5, DecimalByteOrder.BigEndian)]
    [InlineData(2, DecimalByteOrder.LittleEndian)]
    public void BulkInt64_RoundTrips(int width, DecimalByteOrder order)
    {
        BigInteger[] values = Representable(width).ToArray();
        long[] mantissas = values.Select(v => (long)v).ToArray();

        byte[] buffer = new byte[values.Length * width];
        DecimalBinary.WriteInt64(mantissas, buffer, width, order);

        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(Expected(values[i], width, order),
                buffer.Skip(i * width).Take(width).ToArray());
        }

        long[] read = new long[values.Length];
        DecimalBinary.ReadInt64(buffer, width, order, read);
        Assert.Equal(mantissas, read);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    public void BulkNaturalWidth_AgreesWithTheGeneralPath(int natural)
    {
        // A field of the mantissa's own width takes a dedicated path that skips
        // the fit check and the scratch buffer. One byte wider goes through the
        // general core, where the leading byte is pure sign — so the two must
        // agree on every byte they share.
        BigInteger[] values = Representable(natural).ToArray();

        byte[] fast = new byte[values.Length * natural];
        byte[] general = new byte[values.Length * (natural + 1)];

        if (natural == 4)
        {
            int[] mantissas = values.Select(v => (int)v).ToArray();
            DecimalBinary.WriteInt32(mantissas, fast, natural, DecimalByteOrder.BigEndian);
            DecimalBinary.WriteInt32(mantissas, general, natural + 1, DecimalByteOrder.BigEndian);
        }
        else
        {
            long[] mantissas = values.Select(v => (long)v).ToArray();
            DecimalBinary.WriteInt64(mantissas, fast, natural, DecimalByteOrder.BigEndian);
            DecimalBinary.WriteInt64(mantissas, general, natural + 1, DecimalByteOrder.BigEndian);
        }

        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(
                fast.Skip(i * natural).Take(natural).ToArray(),
                general.Skip(i * (natural + 1) + 1).Take(natural).ToArray());
        }
    }

    [Fact]
    public void BulkInt32_Ignore_TruncatesInsteadOfThrowing()
    {
        int[] values = [8_388_608]; // 2^23, one bit past a 3-byte field

        Assert.Throws<OverflowException>(() =>
            DecimalBinary.WriteInt32(values, new byte[3], 3, DecimalByteOrder.BigEndian));

        byte[] buffer = new byte[3];
        DecimalBinary.WriteInt32(values, buffer, 3, DecimalByteOrder.BigEndian, DecimalOverflow.Ignore);
        Assert.Equal(new byte[] { 0x80, 0x00, 0x00 }, buffer);
    }

    [Fact]
    public void BulkInt64_ReportsTheOffendingField()
    {
        // Field 1 needs a sixth byte; the message names its index.
        long[] values = [1, 1L << 39];

        var ex = Assert.Throws<OverflowException>(() =>
            DecimalBinary.WriteInt64(values, new byte[2 * 5], 5, DecimalByteOrder.BigEndian));

        Assert.Contains("index 1", ex.Message);
    }

    // ================================================================
    // Decimal-typed columns
    // ================================================================

    [Theory]
    [InlineData(4, DecimalByteOrder.LittleEndian)]
    [InlineData(3, DecimalByteOrder.BigEndian)]
    public void DecimalColumns32_MatchTheMantissaOverload(int width, DecimalByteOrder order)
    {
        BigInteger[] values = Representable(width).ToArray();
        int[] mantissas = values.Select(v => (int)v).ToArray();
        Decimal32[] decimals = mantissas.Select(m => new Decimal32(m)).ToArray();

        byte[] viaDecimal = new byte[values.Length * width];
        byte[] viaMantissa = new byte[values.Length * width];
        DecimalBinary.WriteDecimal32(decimals, viaDecimal, width, order);
        DecimalBinary.WriteInt32(mantissas, viaMantissa, width, order);
        Assert.Equal(viaMantissa, viaDecimal);

        Decimal32[] read = new Decimal32[values.Length];
        DecimalBinary.ReadDecimal32(viaDecimal, width, order, read);
        Assert.Equal(decimals, read);
    }

    [Theory]
    [InlineData(8, DecimalByteOrder.BigEndian)]
    [InlineData(6, DecimalByteOrder.LittleEndian)]
    public void DecimalColumns64_MatchTheMantissaOverload(int width, DecimalByteOrder order)
    {
        BigInteger[] values = Representable(width).ToArray();
        long[] mantissas = values.Select(v => (long)v).ToArray();
        Decimal64[] decimals = mantissas.Select(m => new Decimal64(m)).ToArray();

        byte[] viaDecimal = new byte[values.Length * width];
        byte[] viaMantissa = new byte[values.Length * width];
        DecimalBinary.WriteDecimal64(decimals, viaDecimal, width, order);
        DecimalBinary.WriteInt64(mantissas, viaMantissa, width, order);
        Assert.Equal(viaMantissa, viaDecimal);

        Decimal64[] read = new Decimal64[values.Length];
        DecimalBinary.ReadDecimal64(viaDecimal, width, order, read);
        Assert.Equal(decimals, read);
    }

    [Theory]
    [InlineData(16, DecimalByteOrder.LittleEndian)] // Arrow decimal128
    [InlineData(9, DecimalByteOrder.BigEndian)]     // Parquet DECIMAL(20,4)
    public void DecimalColumns128_MatchTheMantissaOverload(int width, DecimalByteOrder order)
    {
        BigInteger[] values = Representable(width).ToArray();
        Int128[] mantissas = values.Select(ToInt128).ToArray();
        Decimal128[] decimals = mantissas.Select(m => new Decimal128(m)).ToArray();

        byte[] viaDecimal = new byte[values.Length * width];
        byte[] viaMantissa = new byte[values.Length * width];
        DecimalBinary.WriteDecimal128(decimals, viaDecimal, width, order);
        DecimalBinary.WriteInt128(mantissas, viaMantissa, width, order);
        Assert.Equal(viaMantissa, viaDecimal);

        Decimal128[] read = new Decimal128[values.Length];
        DecimalBinary.ReadDecimal128(viaDecimal, width, order, read);
        Assert.Equal(decimals, read);
    }

    [Theory]
    [InlineData(32, DecimalByteOrder.LittleEndian)] // Arrow decimal256
    [InlineData(17, DecimalByteOrder.BigEndian)]
    public void DecimalColumns256_MatchTheMantissaOverload(int width, DecimalByteOrder order)
    {
        BigInteger[] values = Representable(width).ToArray();
        Int256[] mantissas = values.Select(ToInt256).ToArray();
        Decimal256[] decimals = mantissas.Select(m => new Decimal256(m)).ToArray();

        byte[] viaDecimal = new byte[values.Length * width];
        byte[] viaMantissa = new byte[values.Length * width];
        DecimalBinary.WriteDecimal256(decimals, viaDecimal, width, order);
        DecimalBinary.WriteInt256(mantissas, viaMantissa, width, order);
        Assert.Equal(viaMantissa, viaDecimal);

        Decimal256[] read = new Decimal256[values.Length];
        DecimalBinary.ReadDecimal256(viaDecimal, width, order, read);
        Assert.Equal(decimals, read);
    }

    [Fact]
    public void DecimalColumns_CarryTheOverflowSetting()
    {
        Decimal32[] values = [new Decimal32(8_388_608)]; // 2^23, past a 3-byte field

        Assert.Throws<OverflowException>(() =>
            DecimalBinary.WriteDecimal32(values, new byte[3], 3, DecimalByteOrder.BigEndian));

        byte[] buffer = new byte[3];
        DecimalBinary.WriteDecimal32(values, buffer, 3, DecimalByteOrder.BigEndian, DecimalOverflow.Ignore);
        Assert.Equal(new byte[] { 0x80, 0x00, 0x00 }, buffer);
    }

    /// <summary>
    /// The decimal overloads reinterpret a column of wrappers as a column of
    /// mantissas, which holds only while each wrapper is exactly its mantissa.
    /// Asserted here so a field added to one of them fails a test rather than
    /// silently halving a column's length.
    /// </summary>
    [Fact]
    public void DecimalWrappers_AreExactlyTheirMantissa()
    {
        Assert.Equal(sizeof(int), Unsafe.SizeOf<Decimal32>());
        Assert.Equal(sizeof(long), Unsafe.SizeOf<Decimal64>());
        Assert.Equal(16, Unsafe.SizeOf<Decimal128>());
        Assert.Equal(32, Unsafe.SizeOf<Decimal256>());

        if (!BitConverter.IsLittleEndian)
            return;

        // And the wrapper's memory image is the mantissa's, not merely its size.
        Decimal128[] column = Representable(16).Select(v => new Decimal128(ToInt128(v))).ToArray();
        byte[] written = new byte[column.Length * 16];
        DecimalBinary.WriteDecimal128(column, written, 16, DecimalByteOrder.LittleEndian);

        Assert.Equal(MemoryMarshal.AsBytes<Decimal128>(column).ToArray(), written);
    }
}
