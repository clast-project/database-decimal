// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Binary;

/// <summary>
/// Reads and writes mantissas as raw two's-complement bytes, in either byte
/// order and at any field width — the form Arrow and Parquet store decimals in.
/// </summary>
/// <remarks>
/// The field width is the length of the byte span, not the width of the
/// mantissa type: Parquet's DECIMAL on <c>FIXED_LEN_BYTE_ARRAY</c> declares its
/// own width, which is usually narrower than the tier holding the value. A read
/// sign-extends from the top bit of the field; a write sign-extends the value
/// into the whole field, and reports values the field cannot hold.
/// <para>
/// This is deliberately not the shape of <c>IBinaryInteger</c>, whose
/// <c>TryWriteLittleEndian</c> is all-or-nothing at the type's own width: it
/// refuses a 12-byte destination even for a value that fits in 12 bytes, and
/// fills only 16 of a 20-byte destination, leaving the rest without sign
/// extension. Neither behaviour serves a fixed-width column. Those members are
/// also explicit interface implementations reachable only through generic math,
/// which netstandard2.0 does not have.
/// </para>
/// </remarks>
public static class DecimalBinary
{
    private const int Int128ByteCount = 16;
    private const int Int256ByteCount = 32;

    // ================================================================
    // Scalar — Int128
    // ================================================================

    /// <summary>
    /// Reads the whole of <paramref name="source"/> as a two's-complement
    /// integer, sign-extending from the top bit of the field.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="source"/> is empty.</exception>
    /// <exception cref="OverflowException">The field holds a value too large for <see cref="Int128"/>.</exception>
    public static Int128 ReadInt128(ReadOnlySpan<byte> source, DecimalByteOrder order)
    {
        if (!TryReadInt128(source, order, out Int128 value))
            ThrowUnreadable(source.Length, Int128ByteCount);

        return value;
    }

    /// <summary>
    /// Reads the whole of <paramref name="source"/> as a two's-complement
    /// integer. Returns false if the span is empty, or holds a value too large
    /// for <see cref="Int128"/>.
    /// </summary>
    public static bool TryReadInt128(ReadOnlySpan<byte> source, DecimalByteOrder order, out Int128 value)
    {
        Span<byte> le = stackalloc byte[Int128ByteCount];
        if (!TryReadCore(source, order, le))
        {
            value = default;
            return false;
        }

        value = FromLittleEndian128(le);
        return true;
    }

    /// <summary>
    /// Writes <paramref name="value"/> as two's complement across the whole of
    /// <paramref name="destination"/>, sign-extending into any spare bytes.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is empty.</exception>
    /// <exception cref="OverflowException">The value does not fit the field width.</exception>
    public static void WriteInt128(Int128 value, Span<byte> destination, DecimalByteOrder order)
    {
        if (!TryWriteInt128(value, destination, order))
            ThrowUnwritable(destination.Length);
    }

    /// <summary>
    /// Writes <paramref name="value"/> as two's complement across the whole of
    /// <paramref name="destination"/>. Returns false if the span is empty, or
    /// too narrow to hold the value.
    /// </summary>
    public static bool TryWriteInt128(Int128 value, Span<byte> destination, DecimalByteOrder order)
    {
        Span<byte> le = stackalloc byte[Int128ByteCount];
        ToLittleEndian128(value, le);
        return TryWriteCore(le, destination, order, checkFit: true);
    }

    // ================================================================
    // Scalar — Int256
    // ================================================================

    /// <inheritdoc cref="ReadInt128(ReadOnlySpan{byte}, DecimalByteOrder)"/>
    public static Int256 ReadInt256(ReadOnlySpan<byte> source, DecimalByteOrder order)
    {
        if (!TryReadInt256(source, order, out Int256 value))
            ThrowUnreadable(source.Length, Int256ByteCount);

        return value;
    }

    /// <inheritdoc cref="TryReadInt128(ReadOnlySpan{byte}, DecimalByteOrder, out Int128)"/>
    public static bool TryReadInt256(ReadOnlySpan<byte> source, DecimalByteOrder order, out Int256 value)
    {
        Span<byte> le = stackalloc byte[Int256ByteCount];
        if (!TryReadCore(source, order, le))
        {
            value = default;
            return false;
        }

        value = FromLittleEndian256(le);
        return true;
    }

    /// <inheritdoc cref="WriteInt128(Int128, Span{byte}, DecimalByteOrder)"/>
    public static void WriteInt256(Int256 value, Span<byte> destination, DecimalByteOrder order)
    {
        if (!TryWriteInt256(value, destination, order))
            ThrowUnwritable(destination.Length);
    }

    /// <inheritdoc cref="TryWriteInt128(Int128, Span{byte}, DecimalByteOrder)"/>
    public static bool TryWriteInt256(Int256 value, Span<byte> destination, DecimalByteOrder order)
    {
        Span<byte> le = stackalloc byte[Int256ByteCount];
        ToLittleEndian256(value, le);
        return TryWriteCore(le, destination, order, checkFit: true);
    }

    // ================================================================
    // Bulk — Int128
    // ================================================================

    /// <summary>
    /// Reads a column of fixed-width fields. Element <c>i</c> occupies
    /// <c>source.Slice(i * byteWidth, byteWidth)</c>; the element count comes
    /// from <paramref name="destination"/>.
    /// </summary>
    /// <exception cref="OverflowException">A field holds a value too large for <see cref="Int128"/>.</exception>
    public static void ReadInt128(ReadOnlySpan<byte> source, int byteWidth, DecimalByteOrder order,
        Span<Int128> destination)
    {
        ValidateWidth(byteWidth);
        int count = destination.Length;
        ValidateSourceLength(source.Length, count, byteWidth);
        if (count == 0)
            return;

        if (CanReinterpret(byteWidth, Int128ByteCount, order))
        {
            source.Slice(0, count * Int128ByteCount).CopyTo(MemoryMarshal.AsBytes(destination));
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (!TryReadInt128(source.Slice(i * byteWidth, byteWidth), order, out Int128 value))
                ThrowElementUnreadable(i, byteWidth, Int128ByteCount);

            destination[i] = value;
        }
    }

    /// <summary>
    /// Writes a column of fixed-width fields. Element <c>i</c> occupies
    /// <c>destination.Slice(i * byteWidth, byteWidth)</c>; the element count
    /// comes from <paramref name="values"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="DecimalOverflow.Throw"/> rejects a value the field cannot
    /// hold; <see cref="DecimalOverflow.Ignore"/> writes its low bytes
    /// unchecked, for callers that have already proven the range.
    /// </remarks>
    public static void WriteInt128(ReadOnlySpan<Int128> values, Span<byte> destination, int byteWidth,
        DecimalByteOrder order, DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateWidth(byteWidth);
        int count = values.Length;
        ValidateDestinationLength(destination.Length, count, byteWidth);
        if (count == 0)
            return;

        if (CanReinterpret(byteWidth, Int128ByteCount, order))
        {
            MemoryMarshal.AsBytes(values).CopyTo(destination);
            return;
        }

        Span<byte> le = stackalloc byte[Int128ByteCount];
        bool checkFit = overflow == DecimalOverflow.Throw;
        for (int i = 0; i < count; i++)
        {
            ToLittleEndian128(values[i], le);
            if (!TryWriteCore(le, destination.Slice(i * byteWidth, byteWidth), order, checkFit))
                ThrowElementUnwritable(i, byteWidth);
        }
    }

    // ================================================================
    // Bulk — Int256
    // ================================================================

    /// <inheritdoc cref="ReadInt128(ReadOnlySpan{byte}, int, DecimalByteOrder, Span{Int128})"/>
    public static void ReadInt256(ReadOnlySpan<byte> source, int byteWidth, DecimalByteOrder order,
        Span<Int256> destination)
    {
        ValidateWidth(byteWidth);
        int count = destination.Length;
        ValidateSourceLength(source.Length, count, byteWidth);
        if (count == 0)
            return;

        if (CanReinterpret(byteWidth, Int256ByteCount, order))
        {
            source.Slice(0, count * Int256ByteCount).CopyTo(MemoryMarshal.AsBytes(destination));
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (!TryReadInt256(source.Slice(i * byteWidth, byteWidth), order, out Int256 value))
                ThrowElementUnreadable(i, byteWidth, Int256ByteCount);

            destination[i] = value;
        }
    }

    /// <inheritdoc cref="WriteInt128(ReadOnlySpan{Int128}, Span{byte}, int, DecimalByteOrder, DecimalOverflow)"/>
    public static void WriteInt256(ReadOnlySpan<Int256> values, Span<byte> destination, int byteWidth,
        DecimalByteOrder order, DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        ValidateWidth(byteWidth);
        int count = values.Length;
        ValidateDestinationLength(destination.Length, count, byteWidth);
        if (count == 0)
            return;

        if (CanReinterpret(byteWidth, Int256ByteCount, order))
        {
            MemoryMarshal.AsBytes(values).CopyTo(destination);
            return;
        }

        Span<byte> le = stackalloc byte[Int256ByteCount];
        bool checkFit = overflow == DecimalOverflow.Throw;
        for (int i = 0; i < count; i++)
        {
            ToLittleEndian256(values[i], le);
            if (!TryWriteCore(le, destination.Slice(i * byteWidth, byteWidth), order, checkFit))
                ThrowElementUnwritable(i, byteWidth);
        }
    }

    // ================================================================
    // Field width
    // ================================================================

    /// <summary>
    /// The narrowest field that holds every value of <paramref name="type"/> —
    /// the width a Parquet writer declares for a <c>FIXED_LEN_BYTE_ARRAY</c>
    /// DECIMAL of that precision.
    /// </summary>
    public static int MinByteWidth(DecimalType type) => MinByteWidths[type.Precision];

    /// <summary>
    /// Indexed by precision: the smallest n with 10^precision &lt;= 2^(8n-1),
    /// so that every mantissa of that precision survives the round trip.
    /// Index 0 covers <c>default(DecimalType)</c>, which holds only zero.
    /// </summary>
    private static ReadOnlySpan<byte> MinByteWidths => new byte[]
    {
        1, 1, 1, 2, 2, 3, 3, 4, 4, 4,
        5, 5, 6, 6, 6, 7, 7, 8, 8, 9,
        9, 9, 10, 10, 11, 11, 11, 12, 12, 13,
        13, 13, 14, 14, 15, 15, 16, 16, 16, 17,
        17, 18, 18, 18, 19, 19, 20, 20, 21, 21,
        21, 22, 22, 23, 23, 23, 24, 24, 25, 25,
        26, 26, 26, 27, 27, 28, 28, 28, 29, 29,
        30, 30, 31, 31, 31, 32, 32,
    };

    // ================================================================
    // Byte-level core, shared by both widths
    // ================================================================

    /// <summary>
    /// Gathers <paramref name="source"/> into a little-endian mantissa image,
    /// sign-extending a narrow field and checking that a field wider than the
    /// mantissa carries nothing but sign.
    /// </summary>
    private static bool TryReadCore(ReadOnlySpan<byte> source, DecimalByteOrder order, Span<byte> le)
    {
        int width = le.Length;
        int length = source.Length;
        if (length == 0)
            return false;

        int taken = Math.Min(length, width);

        if (order == DecimalByteOrder.LittleEndian)
        {
            source.Slice(0, taken).CopyTo(le);
        }
        else
        {
            for (int i = 0; i < taken; i++)
                le[i] = source[length - 1 - i];
        }

        byte sign = SignByte(le[taken - 1]);
        le.Slice(taken).Fill(sign);

        if (length > width)
        {
            // The bytes the mantissa cannot hold have to be pure sign, or the
            // field carries a value this tier cannot represent.
            ReadOnlySpan<byte> spare = order == DecimalByteOrder.LittleEndian
                ? source.Slice(width)
                : source.Slice(0, length - width);

            for (int i = 0; i < spare.Length; i++)
            {
                if (spare[i] != sign)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Emits a little-endian mantissa image across the whole destination field,
    /// sign-extending into a wider field and — when <paramref name="checkFit"/>
    /// is set — refusing a field too narrow to round-trip the value.
    /// </summary>
    private static bool TryWriteCore(ReadOnlySpan<byte> le, Span<byte> destination, DecimalByteOrder order,
        bool checkFit)
    {
        int width = le.Length;
        int length = destination.Length;
        if (length == 0)
            return false;

        byte sign = SignByte(le[width - 1]);

        if (length < width && checkFit)
        {
            // Every dropped byte must be sign, and the sign has to survive in
            // the top bit of the last byte that is kept.
            for (int i = length; i < width; i++)
            {
                if (le[i] != sign)
                    return false;
            }

            if (SignByte(le[length - 1]) != sign)
                return false;
        }

        int taken = Math.Min(length, width);

        if (order == DecimalByteOrder.LittleEndian)
        {
            le.Slice(0, taken).CopyTo(destination);
            destination.Slice(taken).Fill(sign);
        }
        else
        {
            destination.Slice(0, length - taken).Fill(sign);
            for (int i = 0; i < taken; i++)
                destination[length - 1 - i] = le[i];
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte SignByte(byte mostSignificant) =>
        (mostSignificant & 0x80) != 0 ? (byte)0xFF : (byte)0x00;

    /// <summary>
    /// Whether a column can be copied wholesale rather than assembled element by
    /// element. The mantissa types are <c>Sequential</c> with the low half
    /// first, so on a little-endian host their memory image is already the
    /// little-endian field of the same width. A big-endian host is excluded
    /// rather than reasoned about: the layout claim is only tested where .NET
    /// actually runs.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CanReinterpret(int byteWidth, int mantissaWidth, DecimalByteOrder order) =>
        byteWidth == mantissaWidth && order == DecimalByteOrder.LittleEndian && BitConverter.IsLittleEndian;

    private static void ToLittleEndian128(Int128 value, Span<byte> le)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(le, (ulong)value);
        BinaryPrimitives.WriteUInt64LittleEndian(le.Slice(8), (ulong)(value >>> 64));
    }

    private static Int128 FromLittleEndian128(ReadOnlySpan<byte> le) => new(
        BinaryPrimitives.ReadUInt64LittleEndian(le.Slice(8)),
        BinaryPrimitives.ReadUInt64LittleEndian(le));

    private static void ToLittleEndian256(Int256 value, Span<byte> le)
    {
        UInt256 m = (UInt256)value;
        BinaryPrimitives.WriteUInt64LittleEndian(le, (ulong)m);
        BinaryPrimitives.WriteUInt64LittleEndian(le.Slice(8), (ulong)(m >>> 64));
        BinaryPrimitives.WriteUInt64LittleEndian(le.Slice(16), (ulong)(m >>> 128));
        BinaryPrimitives.WriteUInt64LittleEndian(le.Slice(24), (ulong)(m >>> 192));
    }

    private static Int256 FromLittleEndian256(ReadOnlySpan<byte> le) => new(
        new UInt128(
            BinaryPrimitives.ReadUInt64LittleEndian(le.Slice(24)),
            BinaryPrimitives.ReadUInt64LittleEndian(le.Slice(16))),
        new UInt128(
            BinaryPrimitives.ReadUInt64LittleEndian(le.Slice(8)),
            BinaryPrimitives.ReadUInt64LittleEndian(le)));

    // ================================================================
    // Validation
    // ================================================================

    private static void ValidateWidth(int byteWidth)
    {
        if (byteWidth < 1)
            throw new ArgumentOutOfRangeException(nameof(byteWidth),
                $"Field width must be at least 1 byte, got {byteWidth}.");
    }

    private static void ValidateSourceLength(int sourceLength, int count, int byteWidth)
    {
        if ((long)count * byteWidth > sourceLength)
            throw new ArgumentException(
                $"Source holds {sourceLength} bytes, too few for {count} fields of {byteWidth} bytes.");
    }

    private static void ValidateDestinationLength(int destinationLength, int count, int byteWidth)
    {
        if ((long)count * byteWidth > destinationLength)
            throw new ArgumentException(
                $"Destination holds {destinationLength} bytes, too few for {count} fields of {byteWidth} bytes.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnreadable(int length, int mantissaWidth)
    {
        if (length == 0)
            throw new ArgumentException("Source must hold at least one byte.", "source");

        throw new OverflowException(
            $"A {length}-byte field carries a value that does not fit a {mantissaWidth * 8}-bit mantissa.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnwritable(int length)
    {
        if (length == 0)
            throw new ArgumentException("Destination must hold at least one byte.", "destination");

        throw new OverflowException($"The value does not fit a {length}-byte field.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowElementUnreadable(int index, int byteWidth, int mantissaWidth) =>
        throw new OverflowException(
            $"Field {index} of {byteWidth} bytes carries a value that does not fit a {mantissaWidth * 8}-bit mantissa.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowElementUnwritable(int index, int byteWidth) =>
        throw new OverflowException($"Value {index} does not fit a {byteWidth}-byte field.");
}
