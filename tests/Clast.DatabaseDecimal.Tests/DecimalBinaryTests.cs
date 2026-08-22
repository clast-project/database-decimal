// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Numerics;
using System.Runtime.InteropServices;
using Clast.DatabaseDecimal.Binary;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// Covers the raw two's-complement mantissa accessors against a
/// <see cref="BigInteger"/> oracle.
/// </summary>
/// <remarks>
/// These run on net472 as well as net8.0+, which makes them differential: the
/// netstandard2.0 build binds the <c>Int128</c> polyfill while net8.0+ binds the
/// BCL type, so both must agree with BigInteger and therefore with each other.
/// <para>
/// Big-endian fields on a little-endian host exercise the same byte-reversing
/// path a big-endian host would take for little-endian fields, which is the only
/// way to cover that code on hardware .NET ships for.
/// </para>
/// </remarks>
public partial class DecimalBinaryTests
{
    private static readonly DecimalByteOrder[] Orders =
        [DecimalByteOrder.LittleEndian, DecimalByteOrder.BigEndian];

    // ================================================================
    // Oracle
    // ================================================================

    /// <summary>
    /// The two's-complement image of <paramref name="value"/> in a field of
    /// <paramref name="byteWidth"/> bytes, built from BigInteger rather than
    /// from the code under test.
    /// </summary>
    private static byte[] Expected(BigInteger value, int byteWidth, DecimalByteOrder order)
    {
        BigInteger modulus = BigInteger.One << (8 * byteWidth);
        BigInteger unsigned = value < 0 ? value + modulus : value;

        byte[] field = new byte[byteWidth];
        for (int i = 0; i < byteWidth; i++)
            field[i] = (byte)(unsigned >> (8 * i) & 0xFF); // little-endian

        if (order == DecimalByteOrder.BigEndian)
            Array.Reverse(field);

        return field;
    }

    private static BigInteger ToBig(Int128 value) => NumericOracle.ToBig(value);

    private static BigInteger ToBig(Int256 value) => NumericOracle.ToBig(value);

    private static Int128 ToInt128(BigInteger value) => NumericOracle.ToInt128(value);

    private static Int256 ToInt256(BigInteger value) => NumericOracle.ToInt256(value);

    /// <summary>Values that fit a field of the given width, including its edges.</summary>
    private static IEnumerable<BigInteger> Representable(int byteWidth)
    {
        BigInteger max = (BigInteger.One << (8 * byteWidth - 1)) - 1;
        BigInteger min = -(BigInteger.One << (8 * byteWidth - 1));

        yield return BigInteger.Zero;
        yield return BigInteger.One;
        yield return BigInteger.MinusOne;
        yield return max;
        yield return min;
        yield return max - 1;
        yield return min + 1;
        yield return max / 3;
        yield return -(max / 3);
        yield return new BigInteger(0x0102_0304) % (max + 1);
    }

    // ================================================================
    // Scalar round trip — every field width
    // ================================================================

    [Fact]
    public void Int128_RoundTripsEveryWidthAndOrder()
    {
        for (int width = 1; width <= 16; width++)
        {
            foreach (DecimalByteOrder order in Orders)
            {
                foreach (BigInteger big in Representable(width))
                {
                    Int128 value = ToInt128(big);
                    byte[] field = new byte[width];

                    Assert.True(DecimalBinary.TryWriteInt128(value, field, order),
                        $"width {width}, order {order}, value {big}");
                    Assert.Equal(Expected(big, width, order), field);

                    Assert.True(DecimalBinary.TryReadInt128(field, order, out Int128 read));
                    Assert.Equal(big, ToBig(read));
                }
            }
        }
    }

    [Fact]
    public void Int256_RoundTripsEveryWidthAndOrder()
    {
        for (int width = 1; width <= 32; width++)
        {
            foreach (DecimalByteOrder order in Orders)
            {
                foreach (BigInteger big in Representable(width))
                {
                    Int256 value = ToInt256(big);
                    byte[] field = new byte[width];

                    Assert.True(DecimalBinary.TryWriteInt256(value, field, order),
                        $"width {width}, order {order}, value {big}");
                    Assert.Equal(Expected(big, width, order), field);

                    Assert.True(DecimalBinary.TryReadInt256(field, order, out Int256 read));
                    Assert.Equal(big, ToBig(read));
                }
            }
        }
    }

    // ================================================================
    // Golden vectors — pinned to the formats, not to this implementation
    // ================================================================

    [Fact]
    public void GoldenVectors_MatchTheFormatLayouts()
    {
        // Arrow decimal128 is 16 bytes little-endian two's complement.
        Assert.Equal(
            new byte[] { 0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            Write128(Int128.One, 16, DecimalByteOrder.LittleEndian));

        Assert.Equal(
            Enumerable.Repeat((byte)0xFF, 16).ToArray(),
            Write128(-Int128.One, 16, DecimalByteOrder.LittleEndian));

        // Parquet FIXED_LEN_BYTE_ARRAY DECIMAL is big-endian two's complement,
        // sign-extended to the declared width. 100 in a 4-byte field:
        Assert.Equal(
            new byte[] { 0x00, 0x00, 0x00, 0x64 },
            Write128(new Int128(0, 100), 4, DecimalByteOrder.BigEndian));

        // -100 in the same field: sign extension fills the leading bytes.
        Assert.Equal(
            new byte[] { 0xFF, 0xFF, 0xFF, 0x9C },
            Write128(-new Int128(0, 100), 4, DecimalByteOrder.BigEndian));
    }

    private static byte[] Write128(Int128 value, int width, DecimalByteOrder order)
    {
        byte[] field = new byte[width];
        DecimalBinary.WriteInt128(value, field, order);
        return field;
    }

    // ================================================================
    // Narrow fields: sign extension in, fit check out
    // ================================================================

    [Theory]
    [InlineData(DecimalByteOrder.LittleEndian)]
    [InlineData(DecimalByteOrder.BigEndian)]
    public void NarrowField_ReadSignExtends(DecimalByteOrder order)
    {
        // Twelve bytes of 0xFF is -1 in 96-bit two's complement — the width the
        // extended-precision timestamp carrier uses.
        byte[] field = Enumerable.Repeat((byte)0xFF, 12).ToArray();

        Assert.Equal(-BigInteger.One, ToBig(DecimalBinary.ReadInt128(field, order)));
    }

    [Fact]
    public void NarrowField_WritesTheLowBytesWhenTheValueFits()
    {
        // The 96-bit carrier again: a value inside 12 bytes must be accepted,
        // where the BCL's TryWriteLittleEndian refuses any width but 16.
        Int128 value = new Int128(0, 1_234_567);
        byte[] field = new byte[12];

        Assert.True(DecimalBinary.TryWriteInt128(value, field, DecimalByteOrder.LittleEndian));
        Assert.Equal(Expected(1_234_567, 12, DecimalByteOrder.LittleEndian), field);
    }

    [Fact]
    public void NarrowField_RejectsAValueItCannotHold()
    {
        // 2^95 needs a 13th byte in two's complement.
        Int128 tooWide = ToInt128(BigInteger.One << 95);

        Assert.False(DecimalBinary.TryWriteInt128(tooWide, new byte[12], DecimalByteOrder.LittleEndian));
        Assert.Throws<OverflowException>(() =>
            DecimalBinary.WriteInt128(tooWide, new byte[12], DecimalByteOrder.BigEndian));

        // But -2^95 is exactly the low edge of a 12-byte field.
        Int128 lowEdge = ToInt128(-(BigInteger.One << 95));
        Assert.True(DecimalBinary.TryWriteInt128(lowEdge, new byte[12], DecimalByteOrder.LittleEndian));
    }

    [Theory]
    [InlineData(DecimalByteOrder.LittleEndian)]
    [InlineData(DecimalByteOrder.BigEndian)]
    public void WiderField_SignExtendsRatherThanLeavingTheSpareBytes(DecimalByteOrder order)
    {
        // A 20-byte field holding a 128-bit value: the four spare bytes carry
        // sign, which is where the BCL's writer stops short.
        byte[] field = new byte[20];
        DecimalBinary.WriteInt128(-new Int128(0, 5), field, order);

        Assert.Equal(Expected(-5, 20, order), field);
        Assert.Equal(-new BigInteger(5), ToBig(DecimalBinary.ReadInt128(field, order)));
    }

    [Theory]
    [InlineData(DecimalByteOrder.LittleEndian)]
    [InlineData(DecimalByteOrder.BigEndian)]
    public void WiderField_RejectsAValueTheMantissaCannotHold(DecimalByteOrder order)
    {
        // 2^130 fits a 20-byte field but not a 128-bit mantissa.
        byte[] field = Expected(BigInteger.One << 130, 20, order);

        Assert.False(DecimalBinary.TryReadInt128(field, order, out _));
        Assert.Throws<OverflowException>(() => DecimalBinary.ReadInt128(field, order));

        // The same width is fine when the spare bytes are pure sign.
        byte[] representable = Expected(-BigInteger.One, 20, order);
        Assert.True(DecimalBinary.TryReadInt128(representable, order, out Int128 value));
        Assert.Equal(-BigInteger.One, ToBig(value));
    }

    [Fact]
    public void EmptySpans_AreRejected()
    {
        Assert.False(DecimalBinary.TryReadInt128([], DecimalByteOrder.LittleEndian, out _));
        Assert.False(DecimalBinary.TryWriteInt128(Int128.One, [], DecimalByteOrder.LittleEndian));

        Assert.Throws<ArgumentException>(() =>
            DecimalBinary.ReadInt128([], DecimalByteOrder.LittleEndian));
        Assert.Throws<ArgumentException>(() =>
            DecimalBinary.WriteInt128(Int128.One, [], DecimalByteOrder.LittleEndian));
    }

    // ================================================================
    // Bulk
    // ================================================================

    [Theory]
    [InlineData(16, DecimalByteOrder.LittleEndian)] // the reinterpret fast path
    [InlineData(16, DecimalByteOrder.BigEndian)]
    [InlineData(12, DecimalByteOrder.LittleEndian)]
    [InlineData(9, DecimalByteOrder.BigEndian)]
    [InlineData(1, DecimalByteOrder.LittleEndian)]
    public void BulkInt128_RoundTrips(int width, DecimalByteOrder order)
    {
        BigInteger[] values = Representable(width).ToArray();
        Int128[] mantissas = values.Select(ToInt128).ToArray();

        byte[] buffer = new byte[values.Length * width];
        DecimalBinary.WriteInt128(mantissas, buffer, width, order);

        // Every field matches the oracle, element by element.
        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(Expected(values[i], width, order),
                buffer.Skip(i * width).Take(width).ToArray());
        }

        Int128[] read = new Int128[values.Length];
        DecimalBinary.ReadInt128(buffer, width, order, read);
        Assert.Equal(values, read.Select(ToBig).ToArray());
    }

    [Theory]
    [InlineData(32, DecimalByteOrder.LittleEndian)] // the reinterpret fast path
    [InlineData(32, DecimalByteOrder.BigEndian)]
    [InlineData(19, DecimalByteOrder.BigEndian)]
    public void BulkInt256_RoundTrips(int width, DecimalByteOrder order)
    {
        BigInteger[] values = Representable(width).ToArray();
        Int256[] mantissas = values.Select(ToInt256).ToArray();

        byte[] buffer = new byte[values.Length * width];
        DecimalBinary.WriteInt256(mantissas, buffer, width, order);

        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(Expected(values[i], width, order),
                buffer.Skip(i * width).Take(width).ToArray());
        }

        Int256[] read = new Int256[values.Length];
        DecimalBinary.ReadInt256(buffer, width, order, read);
        Assert.Equal(values, read.Select(ToBig).ToArray());
    }

    [Fact]
    public void BulkFastPath_AgreesWithTheElementLoop()
    {
        // Width 16 little-endian takes the wholesale copy; width 16 big-endian
        // takes the loop. Reversing one gives the other, so the fast path
        // cannot silently diverge from the general case.
        Int128[] values = Representable(16).Select(ToInt128).ToArray();

        byte[] viaCopy = new byte[values.Length * 16];
        DecimalBinary.WriteInt128(values, viaCopy, 16, DecimalByteOrder.LittleEndian);

        byte[] viaLoop = new byte[values.Length * 16];
        DecimalBinary.WriteInt128(values, viaLoop, 16, DecimalByteOrder.BigEndian);
        for (int i = 0; i < values.Length; i++)
            Array.Reverse(viaLoop, i * 16, 16);

        Assert.Equal(viaLoop, viaCopy);
    }

    [Fact]
    public void BulkWrite_Ignore_TruncatesInsteadOfThrowing()
    {
        Int128[] values = [ToInt128(BigInteger.One << 95)]; // needs 13 bytes

        Assert.Throws<OverflowException>(() =>
            DecimalBinary.WriteInt128(values, new byte[12], 12, DecimalByteOrder.LittleEndian));

        byte[] buffer = new byte[12];
        DecimalBinary.WriteInt128(values, buffer, 12, DecimalByteOrder.LittleEndian,
            DecimalOverflow.Ignore);

        // The low 12 bytes of 2^95 are zero; only the sign bit of byte 11 survives.
        Assert.Equal(Expected((BigInteger.One << 95) % (BigInteger.One << 96), 12,
            DecimalByteOrder.LittleEndian), buffer);
    }

    [Fact]
    public void BulkRead_ReportsTheOffendingField()
    {
        // Field 1 carries a value no 128-bit mantissa can hold.
        byte[] buffer = new byte[2 * 20];
        Expected(BigInteger.One, 20, DecimalByteOrder.LittleEndian).CopyTo(buffer, 0);
        Expected(BigInteger.One << 130, 20, DecimalByteOrder.LittleEndian).CopyTo(buffer, 20);

        var ex = Assert.Throws<OverflowException>(() =>
            DecimalBinary.ReadInt128(buffer, 20, DecimalByteOrder.LittleEndian, new Int128[2]));

        Assert.Contains("index 1", ex.Message);
    }

    [Fact]
    public void BulkWrite_ReportsTheOffendingValue()
    {
        // Value 1 needs 13 bytes; the message has to say which element failed,
        // in the same terms the read path uses.
        Int128[] values = [Int128.One, ToInt128(BigInteger.One << 95)];

        var ex = Assert.Throws<OverflowException>(() =>
            DecimalBinary.WriteInt128(values, new byte[2 * 12], 12, DecimalByteOrder.LittleEndian));

        Assert.Contains("index 1", ex.Message);
    }

    [Fact]
    public void BulkLengths_AreValidated()
    {
        Assert.Throws<ArgumentException>(() =>
            DecimalBinary.ReadInt128(new byte[31], 16, DecimalByteOrder.LittleEndian, new Int128[2]));

        Assert.Throws<ArgumentException>(() =>
            DecimalBinary.WriteInt128(new Int128[2], new byte[31], 16, DecimalByteOrder.LittleEndian));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DecimalBinary.ReadInt128(new byte[16], 0, DecimalByteOrder.LittleEndian, new Int128[1]));
    }

    [Fact]
    public void BulkEmpty_IsANoOp()
    {
        DecimalBinary.ReadInt128([], 16, DecimalByteOrder.LittleEndian, []);
        DecimalBinary.WriteInt128([], [], 16, DecimalByteOrder.BigEndian);
    }

    // ================================================================
    // The layout claim itself
    // ================================================================

    /// <summary>
    /// The assumption a columnar caller would otherwise have to make about the
    /// mantissa types' memory layout, asserted here rather than at the call
    /// site: on a little-endian host the in-memory image of a mantissa is
    /// already its little-endian field of the same width.
    /// </summary>
    [Fact]
    public void MantissaLayout_MatchesTheLittleEndianField()
    {
        if (!BitConverter.IsLittleEndian)
            return;

        Int128[] values128 = Representable(16).Select(ToInt128).ToArray();
        byte[] written128 = new byte[values128.Length * 16];
        DecimalBinary.WriteInt128(values128, written128, 16, DecimalByteOrder.LittleEndian);
        Assert.Equal(MemoryMarshal.AsBytes<Int128>(values128).ToArray(), written128);

        Int256[] values256 = Representable(32).Select(ToInt256).ToArray();
        byte[] written256 = new byte[values256.Length * 32];
        DecimalBinary.WriteInt256(values256, written256, 32, DecimalByteOrder.LittleEndian);
        Assert.Equal(MemoryMarshal.AsBytes<Int256>(values256).ToArray(), written256);
    }

    // ================================================================
    // Field width
    // ================================================================

    [Fact]
    public void MinByteWidth_MatchesTheDefinition()
    {
        // Recomputed from the definition: the narrowest signed field that holds
        // every mantissa of the precision, i.e. 10^p <= 2^(8n-1).
        for (int precision = 1; precision <= DecimalType.MaxPrecision256; precision++)
        {
            int expected = 1;
            while (BigInteger.Pow(10, precision) > BigInteger.One << (8 * expected - 1))
                expected++;

            Assert.Equal(expected, DecimalBinary.MinByteWidth(DecimalType.Numeric(precision, 0)));
        }
    }

    [Theory]
    [InlineData(9, 4)]
    [InlineData(18, 8)]
    [InlineData(38, 16)]
    [InlineData(76, 32)]
    public void MinByteWidth_MeetsTheTierBoundaries(int precision, int expected)
    {
        Assert.Equal(expected, DecimalBinary.MinByteWidth(DecimalType.Numeric(precision, 0)));
    }

    [Fact]
    public void MinByteWidth_HoldsEveryMantissaOfThePrecision()
    {
        // The point of the width: the largest mantissa of the precision must
        // round-trip through a field that wide.
        for (int precision = 1; precision <= DecimalType.MaxPrecision256; precision++)
        {
            var type = DecimalType.Numeric(precision, 0);
            int width = DecimalBinary.MinByteWidth(type);
            BigInteger largest = BigInteger.Pow(10, precision) - 1;

            byte[] field = new byte[width];
            Int256 value = ToInt256(largest);
            Assert.True(DecimalBinary.TryWriteInt256(value, field, DecimalByteOrder.BigEndian),
                $"precision {precision} does not fit {width} bytes");
            Assert.Equal(largest, ToBig(DecimalBinary.ReadInt256(field, DecimalByteOrder.BigEndian)));

            Assert.True(DecimalBinary.TryWriteInt256(-value, field, DecimalByteOrder.BigEndian));
            Assert.Equal(-largest, ToBig(DecimalBinary.ReadInt256(field, DecimalByteOrder.BigEndian)));
        }
    }
}
