// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Values;
using Xunit;

namespace Clast.DatabaseDecimal.Tests;

/// <summary>
/// The kernels lean on checked Int128 arithmetic for the mantissa-width bound,
/// so the operators have to agree with the BCL on both directions: no throw for
/// results that fit, a throw for results that do not. On netstandard2.0 these
/// bind to the polyfill rather than System.Int128, which is where they can
/// diverge — the polyfill's checked addition once had its overflow test
/// inverted, so mixed-sign addition threw and genuine overflow slipped past.
/// </summary>
public class Int128CheckedArithmeticTests
{
    // ================================================================
    // Addition
    // ================================================================

    [Theory]
    [InlineData(5, -3, 2)]
    [InlineData(-5, 3, -2)]
    [InlineData(-5, -3, -8)]
    [InlineData(5, 3, 8)]
    [InlineData(1, -1, 0)]
    [InlineData(0, 0, 0)]
    public void CheckedAdd_WithinRange_DoesNotThrow(int left, int right, int expected)
    {
        Int128 l = left, r = right;
        Assert.Equal((Int128)expected, checked(l + r));
    }

    [Fact]
    public void CheckedAdd_MixedSigns_NeverThrows()
    {
        // Adding values of opposite sign cannot overflow: the magnitude of the
        // result never exceeds the larger operand's.
        Assert.Equal(Int128.Zero, checked(Int128.MaxValue + -Int128.MaxValue));
        Assert.Equal(-Int128.One, checked(Int128.MinValue + Int128.MaxValue));
        Assert.Equal(Int128.MaxValue - Int128.One, checked(Int128.MaxValue + -Int128.One));
    }

    [Fact]
    public void CheckedAdd_PastRange_Throws()
    {
        Assert.Throws<OverflowException>(() => checked(Int128.MaxValue + Int128.One));
        Assert.Throws<OverflowException>(() => checked(Int128.MaxValue + Int128.MaxValue));
        Assert.Throws<OverflowException>(() => checked(Int128.MinValue + -Int128.One));
        Assert.Throws<OverflowException>(() => checked(Int128.MinValue + Int128.MinValue));
    }

    [Fact]
    public void CheckedAdd_AtTheBoundary_DoesNotThrow()
    {
        Assert.Equal(Int128.MaxValue, checked(Int128.MaxValue - Int128.One + Int128.One));
        Assert.Equal(Int128.MinValue, checked(Int128.MinValue + Int128.One + -Int128.One));
    }

    // ================================================================
    // Subtraction
    // ================================================================

    [Theory]
    [InlineData(5, 3, 2)]
    [InlineData(-5, 3, -8)]
    [InlineData(-5, -3, -2)]
    [InlineData(5, -3, 8)]
    public void CheckedSubtract_WithinRange_DoesNotThrow(int left, int right, int expected)
    {
        Int128 l = left, r = right;
        Assert.Equal((Int128)expected, checked(l - r));
    }

    [Fact]
    public void CheckedSubtract_PastRange_Throws()
    {
        Assert.Throws<OverflowException>(() => checked(Int128.MaxValue - -Int128.One));
        Assert.Throws<OverflowException>(() => checked(Int128.MinValue - Int128.One));
        Assert.Throws<OverflowException>(() => checked(Int128.MinValue - Int128.MaxValue));
    }

    // ================================================================
    // Multiplication and negation
    // ================================================================

    [Theory]
    [InlineData(2, 3, 6)]
    [InlineData(-2, 3, -6)]
    [InlineData(2, -3, -6)]
    [InlineData(-2, -3, 6)]
    [InlineData(0, 0, 0)]
    public void CheckedMultiply_WithinRange_DoesNotThrow(int left, int right, int expected)
    {
        Int128 l = left, r = right;
        Assert.Equal((Int128)expected, checked(l * r));
    }

    [Fact]
    public void CheckedMultiply_PastRange_Throws()
    {
        Assert.Throws<OverflowException>(() => checked(Int128.MaxValue * (Int128)2));
        Assert.Throws<OverflowException>(() => checked(Int128.MinValue * -Int128.One));
        Assert.Throws<OverflowException>(() => checked(Int128.MinValue * (Int128)2));
    }

    [Fact]
    public void CheckedNegate_MinValue_Throws()
    {
        Assert.Throws<OverflowException>(() => checked(-Int128.MinValue));
        Assert.Equal(-Int128.MaxValue, checked(-Int128.MaxValue));
    }

    // ================================================================
    // The kernel path that depends on all of the above
    // ================================================================

    [Fact]
    public void AddKernel_MixedSigns_DoesNotThrow()
    {
        var type = DecimalType.Numeric(38, 0);
        var result = Arithmetic.AddKernel.Add(
            new Decimal128((Int128)5), type, new Decimal128((Int128)(-3)), type, type);
        Assert.Equal((Int128)2, result.Mantissa);
    }
}
