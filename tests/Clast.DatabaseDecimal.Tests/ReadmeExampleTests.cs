// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Binary;
using Clast.DatabaseDecimal.Text;
using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// The README's examples, compiled and asserted.
/// </summary>
/// <remarks>
/// Twice now a README snippet has shipped that could not compile — once calling
/// a parse overload that does not exist, once using an undeclared variable and
/// an <c>Int128.Parse</c> the netstandard2.0 polyfill does not implement. Prose
/// is not built, so nothing caught either. Mirroring the snippets here puts them
/// through the compiler on every target, and asserting the values in their
/// trailing comments keeps those honest too.
/// <para>
/// Examples must use only the public API — <c>PowersOf10</c> is internal and
/// reachable from these tests via InternalsVisibleTo, but not by a consumer, so
/// it must not appear in a snippet.
/// </para>
/// </remarks>
public class ReadmeExampleTests
{
    /// <summary>The "Rounding" section.</summary>
    [Fact]
    public void Rounding()
    {
        var intType = DecimalType.Numeric(precision: 9, scale: 0);
        var oneDecimal = DecimalType.Numeric(precision: 18, scale: 1);
        var five = new Decimal32(5);
        var two = new Decimal32(2);

        // Banker's rounding — the default, and what IEEE 754 and Math.Round do.
        Assert.Equal(2L, DivideKernel.Divide(five, intType, two, intType, intType).Mantissa);
        Assert.Equal((Int128)2, ScaleHelper.Rescale128(25, fromScale: 1, toScale: 0));
        Assert.Equal("1.4", DecimalText.ParseDecimal64("1.45".AsSpan(), oneDecimal).ToString(oneDecimal.Scale));

        // Half away from zero — what Spark, SQL Server, PostgreSQL, and MySQL do.
        Assert.Equal(3L, DivideKernel.Divide(five, intType, two, intType, intType, DecimalRounding.HalfUp).Mantissa);
        Assert.Equal((Int128)3, ScaleHelper.Rescale128(25, 1, 0, DecimalRounding.HalfUp));
        Assert.Equal("1.5", DecimalText.ParseDecimal64("1.45".AsSpan(), oneDecimal, DecimalRounding.HalfUp)
            .ToString(oneDecimal.Scale));
    }

    /// <summary>The "Overflow" section's throwing example.</summary>
    [Fact]
    public void Overflow_Throws()
    {
        var t = DecimalType.Numeric(38, 0);
        var max38 = DecimalText.ParseDecimal128("99999999999999999999999999999999999999".AsSpan(), t);
        var one = DecimalText.ParseDecimal128("1".AsSpan(), t);

        // throws: 39 digits does not fit NUMERIC(38,0)
        Assert.Throws<OverflowException>(() => AddKernel.Add(max38, t, one, t, t));
    }

    /// <summary>The "Overflow" section's mask example.</summary>
    [Fact]
    public void Overflow_Mask()
    {
        var t = DecimalType.Numeric(9, 0);
        int[] left = { 1, 999_999_999 };
        int[] right = { 1, 1 };
        int[] result = new int[2];

        SpanAddKernel.Add(left, t, right, t, result, t,
                          DecimalRounding.HalfEven, DecimalOverflow.Ignore);

        ulong[] mask = new ulong[DecimalRange.MaskWordCount(result.Length)];
        int overflowed = DecimalRange.WriteOutOfRangeMask(result, t, mask);

        // overflowed == 1, and bit 1 of mask[0] flags result[1], which reached 10 digits
        Assert.Equal(1, overflowed);
        Assert.Equal(0b10UL, mask[0]);
        Assert.Equal(new[] { 2, 1_000_000_000 }, result);
    }

    /// <summary>The "Overflow" section's folded-mask example.</summary>
    [Fact]
    public void Overflow_FoldedMask()
    {
        var t = DecimalType.Numeric(9, 0);
        int[] left = { 1, 999_999_999 };
        int[] right = { 1, 1 };
        int[] result = new int[2];
        ulong[] mask = new ulong[DecimalRange.MaskWordCount(result.Length)];

        int overflowed = SpanAddKernel.Add(left, t, right, t, result, t, mask);

        // same result and same mask as the two-pass version above, in one pass
        Assert.Equal(1, overflowed);
        Assert.Equal(0b10UL, mask[0]);
        Assert.Equal(new[] { 2, 1_000_000_000 }, result);
    }

    /// <summary>The "Nullable columns" section's divide example.</summary>
    [Fact]
    public void NullableColumns_Divide()
    {
        var t = DecimalType.Numeric(38, 2);
        var resultType = DecimalType.Numeric(38, 6);
        Int128[] left = { 100, 200, 300 };
        Int128[] right = { 5, 0, 3 };        // element 1 is null, and holds zero
        Int128[] result = new Int128[3];
        ulong[] validity = { 0b101 };        // caller's left AND right
        ulong[] mask = new ulong[DecimalRange.MaskWordCount(result.Length)];

        int overflowed = SpanDivideKernel.Divide(
            left, t, right, t, result, resultType, validity, mask);

        Assert.Equal(0, overflowed);
        Assert.Equal(0UL, mask[0]);
        // The skipped row is left exactly as it was found.
        Assert.Equal(Int128.Zero, result[1]);
        Assert.Equal(new[] { (Int128)20_000_000, Int128.Zero, (Int128)100_000_000 }, result);
    }

    /// <summary>The "Binary layout" section.</summary>
    [Fact]
    public void BinaryLayout()
    {
        var type = DecimalType.Numeric(precision: 20, scale: 4);
        int width = DecimalBinary.MinByteWidth(type);                  // 9 bytes for 20 digits
        Assert.Equal(9, width);

        var value = new Decimal128((Int128)(-12_345_678_901_234_567L)); // -1234567890123.4567
        Assert.Equal("-1234567890123.4567", value.ToString(type.Scale));

        var field = new byte[width];
        DecimalBinary.WriteInt128(value.Mantissa, field, DecimalByteOrder.BigEndian);

        var column = new byte[2 * 16];
        var mantissas = new Int128[] { value.Mantissa, Int128.One };
        DecimalBinary.WriteInt128(mantissas, column, byteWidth: 16, DecimalByteOrder.LittleEndian);

        var read = new Decimal128[2];
        DecimalBinary.ReadDecimal128(column, byteWidth: 16, DecimalByteOrder.LittleEndian, read);

        Assert.Equal(mantissas.Select(m => new Decimal128(m)), read);

        // The narrow big-endian field carries the same value back.
        Assert.Equal(value.Mantissa, DecimalBinary.ReadInt128(field, DecimalByteOrder.BigEndian));

        // Sign extension fills the field's leading bytes, since the value is negative.
        Assert.Equal(0xFF, field[0]);
    }

    /// <summary>The "Example" section.</summary>
    [Fact]
    public void Example()
    {
        var type = DecimalType.Numeric(precision: 18, scale: 4);

        var a = DecimalText.ParseDecimal64("1234.5678".AsSpan(), type);
        var b = DecimalText.ParseDecimal64("0.0001".AsSpan(), type);

        var sum = new Decimal64(a.Mantissa + b.Mantissa);
        Assert.Equal("1234.5679", sum.ToString(type.Scale)); // 1234.5679
    }
}
