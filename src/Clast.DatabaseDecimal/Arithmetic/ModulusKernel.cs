// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Performs modulus on fixed-point decimal values.
/// Both operands are rescaled to the result scale before the integer
/// modulus operation, following the same pattern as addition.
/// </summary>
public static class ModulusKernel
{
    public static Decimal32 Modulus(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType)
    {
        int l = ScaleHelper.Rescale32(left.Mantissa, leftType.Scale, resultType.Scale);
        int r = ScaleHelper.Rescale32(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal32(l % r);
    }

    public static Decimal64 Modulus(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType)
    {
        long l = ScaleHelper.Rescale64(left.Mantissa, leftType.Scale, resultType.Scale);
        long r = ScaleHelper.Rescale64(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal64(l % r);
    }

    public static Decimal128 Modulus(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType)
    {
        Int128 l = ScaleHelper.Rescale128(left.Mantissa, leftType.Scale, resultType.Scale);
        Int128 r = ScaleHelper.Rescale128(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal128(l % r);
    }

    public static Decimal256 Modulus(Decimal256 left, DecimalType leftType, Decimal256 right, DecimalType rightType, DecimalType resultType)
    {
        Int256 l = ScaleHelper.Rescale256(left.Mantissa, leftType.Scale, resultType.Scale);
        Int256 r = ScaleHelper.Rescale256(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal256(l % r);
    }

    /// <summary>
    /// Modulus with widening from 32 to 64 bit, used when rescaling
    /// could overflow the 32-bit range.
    /// </summary>
    public static Decimal64 ModulusWiden(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType)
    {
        long l = ScaleHelper.Widen32To64(left.Mantissa, leftType.Scale, resultType.Scale);
        long r = ScaleHelper.Widen32To64(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal64(l % r);
    }

    /// <summary>
    /// Modulus with widening from 64 to 128 bit.
    /// </summary>
    public static Decimal128 ModulusWiden(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType)
    {
        Int128 l = ScaleHelper.Widen64To128(left.Mantissa, leftType.Scale, resultType.Scale);
        Int128 r = ScaleHelper.Widen64To128(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal128(l % r);
    }

    /// <summary>
    /// Modulus with widening from 128 to 256 bit.
    /// </summary>
    public static Decimal256 ModulusWiden(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType)
    {
        Int256 l = ScaleHelper.Widen128To256(left.Mantissa, leftType.Scale, resultType.Scale);
        Int256 r = ScaleHelper.Widen128To256(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal256(l % r);
    }
}
