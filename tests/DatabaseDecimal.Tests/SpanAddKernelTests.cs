using DatabaseDecimal;
using DatabaseDecimal.Arithmetic;
using DatabaseDecimal.Values;
using Xunit;

namespace DatabaseDecimal.Tests;

public class SpanAddKernelTests
{
    // ----------------------------------------------------------------
    // Add — column + column, same scale
    // ----------------------------------------------------------------

    [Fact]
    public void Add_32Bit_SameScale()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        int[] left = [150, 225, 300];     // 1.50, 2.25, 3.00
        int[] right = [100, 175, 200];    // 1.00, 1.75, 2.00
        int[] result = new int[3];

        SpanAddKernel.Add(left, type, right, type, result, resultType);

        Assert.Equal([250, 400, 500], result);
    }

    [Fact]
    public void Add_64Bit_SameScale()
    {
        var type = DecimalType.Numeric(12, 3);
        var resultType = DecimalTypeRules.Add(type, type);

        long[] left = [1_000_000_000, 2_000_000_000];
        long[] right = [500_000_000, 750_000_000];
        long[] result = new long[2];

        SpanAddKernel.Add(left, type, right, type, result, resultType);

        Assert.Equal([1_500_000_000L, 2_750_000_000L], result);
    }

    // ----------------------------------------------------------------
    // Add — column + column, different scales
    // ----------------------------------------------------------------

    [Fact]
    public void Add_32Bit_DifferentScales()
    {
        var leftType = DecimalType.Numeric(5, 2);
        var rightType = DecimalType.Numeric(6, 3);
        var resultType = DecimalTypeRules.Add(leftType, rightType);
        // result scale = 3, left needs scale-up by 1

        int[] left = [150, 225];     // 1.50, 2.25
        int[] right = [2125, 3500];  // 2.125, 3.500
        int[] result = new int[2];

        SpanAddKernel.Add(left, leftType, right, rightType, result, resultType);

        Assert.Equal([3625, 5750], result); // 3.625, 5.750
    }

    // ----------------------------------------------------------------
    // Add — widening
    // ----------------------------------------------------------------

    [Fact]
    public void AddWiden_32To64()
    {
        var type = DecimalType.Numeric(9, 2);
        var resultType = DecimalType.Numeric(10, 2);

        int[] left = [999_999_999, 500_000_000];
        int[] right = [999_999_999, 500_000_000];
        long[] result = new long[2];

        SpanAddKernel.AddWiden(left, type, right, type, result, resultType);

        Assert.Equal([1_999_999_998L, 1_000_000_000L], result);
    }

    [Fact]
    public void AddWiden_64To128()
    {
        var type = DecimalType.Numeric(18, 0);
        var resultType = DecimalType.Numeric(19, 0);

        long[] left = [long.MaxValue / 2, 1_000_000_000_000_000_000];
        long[] right = [long.MaxValue / 2, 2_000_000_000_000_000_000];
        Int128[] result = new Int128[2];

        SpanAddKernel.AddWiden(left, type, right, type, result, resultType);

        Assert.Equal((Int128)(long.MaxValue / 2) + (long.MaxValue / 2), result[0]);
        Assert.Equal((Int128)3_000_000_000_000_000_000, result[1]);
    }

    // ----------------------------------------------------------------
    // Add — column + scalar (broadcast)
    // ----------------------------------------------------------------

    [Fact]
    public void Add_32Bit_Broadcast()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        int[] left = [100, 200, 300];
        int scalar = 50; // 0.50
        int[] result = new int[3];

        SpanAddKernel.Add(left, type, scalar, type, result, resultType);

        Assert.Equal([150, 250, 350], result);
    }

    [Fact]
    public void Add_64Bit_Broadcast_DifferentScale()
    {
        var leftType = DecimalType.Numeric(10, 2);
        var rightType = DecimalType.Numeric(10, 3);
        var resultType = DecimalTypeRules.Add(leftType, rightType);

        long[] left = [1000, 2000]; // 10.00, 20.00
        long scalar = 5500;         // 5.500
        long[] result = new long[2];

        SpanAddKernel.Add(left, leftType, scalar, rightType, result, resultType);

        Assert.Equal([15500L, 25500L], result); // 15.500, 25.500
    }

    // ----------------------------------------------------------------
    // Subtract — column - column
    // ----------------------------------------------------------------

    [Fact]
    public void Subtract_32Bit_SameScale()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Subtract(type, type);

        int[] left = [500, 300, 100];
        int[] right = [150, 175, 200];
        int[] result = new int[3];

        SpanAddKernel.Subtract(left, type, right, type, result, resultType);

        Assert.Equal([350, 125, -100], result);
    }

    // ----------------------------------------------------------------
    // Subtract — column - scalar
    // ----------------------------------------------------------------

    [Fact]
    public void Subtract_32Bit_Broadcast()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Subtract(type, type);

        int[] left = [500, 300, 100];
        int scalar = 200;
        int[] result = new int[3];

        SpanAddKernel.Subtract(left, type, scalar, type, result, resultType);

        Assert.Equal([300, 100, -100], result);
    }

    // ----------------------------------------------------------------
    // Subtract — scalar - column
    // ----------------------------------------------------------------

    [Fact]
    public void Subtract_ScalarMinusColumn()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Subtract(type, type);

        int scalar = 1000; // 10.00
        int[] right = [300, 500, 1500];
        int[] result = new int[3];

        SpanAddKernel.Subtract(scalar, type, right, type, result, resultType);

        Assert.Equal([700, 500, -500], result);
    }

    // ----------------------------------------------------------------
    // In-place aliasing
    // ----------------------------------------------------------------

    [Fact]
    public void Add_InPlace_LeftIsResult()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        int[] data = [100, 200, 300];
        int[] right = [50, 50, 50];

        SpanAddKernel.Add(data, type, right, type, data, resultType);

        Assert.Equal([150, 250, 350], data);
    }

    // ----------------------------------------------------------------
    // Empty spans
    // ----------------------------------------------------------------

    [Fact]
    public void Add_EmptySpans()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        SpanAddKernel.Add(
            ReadOnlySpan<int>.Empty, type,
            ReadOnlySpan<int>.Empty, type,
            Span<int>.Empty, resultType);
        // Should not throw
    }

    // ----------------------------------------------------------------
    // Validation
    // ----------------------------------------------------------------

    [Fact]
    public void Add_MismatchedLengths_Throws()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        Assert.Throws<ArgumentException>(() =>
            SpanAddKernel.Add(new int[3], type, new int[2], type, new int[3], resultType));
    }

    [Fact]
    public void Add_ResultTooShort_Throws()
    {
        var type = DecimalType.Numeric(5, 2);
        var resultType = DecimalTypeRules.Add(type, type);

        Assert.Throws<ArgumentException>(() =>
            SpanAddKernel.Add(new int[3], type, new int[3], type, new int[2], resultType));
    }

    // ----------------------------------------------------------------
    // Overflow
    // ----------------------------------------------------------------

    [Fact]
    public void Add_Overflow_Throws()
    {
        var type = DecimalType.Numeric(5, 0);
        var resultType = DecimalTypeRules.Add(type, type);

        int[] left = [int.MaxValue];
        int[] right = [1];

        Assert.Throws<OverflowException>(() =>
            SpanAddKernel.Add(left, type, right, type, new int[1], resultType));
    }

    // ----------------------------------------------------------------
    // 128-bit and 256-bit
    // ----------------------------------------------------------------

    [Fact]
    public void Add_128Bit()
    {
        var type = DecimalType.Numeric(20, 5);
        var resultType = DecimalTypeRules.Add(type, type);

        Int128[] left = [100_00000, 200_00000];
        Int128[] right = [50_00000, 75_00000];
        Int128[] result = new Int128[2];

        SpanAddKernel.Add(left, type, right, type, result, resultType);

        Assert.Equal((Int128)150_00000, result[0]);
        Assert.Equal((Int128)275_00000, result[1]);
    }

    [Fact]
    public void Add_256Bit()
    {
        var type = DecimalType.Numeric(40, 5);
        var resultType = DecimalTypeRules.Add(type, type);

        Int256[] left = [(Int256)100_00000, (Int256)200_00000];
        Int256[] right = [(Int256)50_00000, (Int256)75_00000];
        Int256[] result = new Int256[2];

        SpanAddKernel.Add(left, type, right, type, result, resultType);

        Assert.Equal((Int256)150_00000, result[0]);
        Assert.Equal((Int256)275_00000, result[1]);
    }

    [Fact]
    public void AddWiden_128To256()
    {
        var type = DecimalType.Numeric(38, 0);
        var resultType = DecimalType.Numeric(39, 0);

        Int128[] left = [Int128.MaxValue / 2];
        Int128[] right = [Int128.MaxValue / 2];
        Int256[] result = new Int256[1];

        SpanAddKernel.AddWiden(left, type, right, type, result, resultType);

        Int256 expected = (Int256)(Int128.MaxValue / 2) + (Int256)(Int128.MaxValue / 2);
        Assert.Equal(expected, result[0]);
    }
}
