// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Performs addition and subtraction on fixed-point decimal values.
/// Both operands are rescaled to the result scale before the integer operation.
/// Under the standard promotion rules the result scale is <c>max(s1,s2)</c>, so
/// both operands scale upward and no rounding occurs; <c>rounding</c> only
/// matters when a caller supplies a result type with a smaller scale.
/// </summary>
public static class AddKernel
{
    public static Decimal32 Add(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        int l = ScaleHelper.Rescale32(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        int r = ScaleHelper.Rescale32(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal32(DecimalRange.Enforce(checked(l + r), resultType, overflow));
    }

    public static Decimal64 Add(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        long l = ScaleHelper.Rescale64(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        long r = ScaleHelper.Rescale64(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal64(DecimalRange.Enforce(checked(l + r), resultType, overflow));
    }

    public static Decimal128 Add(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        Int128 l = ScaleHelper.Rescale128(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        Int128 r = ScaleHelper.Rescale128(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal128(DecimalRange.Enforce(checked(l + r), resultType, overflow));
    }

    /// <summary>
    /// Add two 32-bit values, widening to 64-bit result.
    /// Used when the result precision exceeds 9 digits.
    /// </summary>
    public static Decimal64 AddWiden(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        long l = ScaleHelper.Widen32To64(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        long r = ScaleHelper.Widen32To64(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal64(DecimalRange.Enforce(checked(l + r), resultType, overflow));
    }

    /// <summary>
    /// Add two 64-bit values, widening to 128-bit result.
    /// </summary>
    public static Decimal128 AddWiden(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        Int128 l = ScaleHelper.Widen64To128(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        Int128 r = ScaleHelper.Widen64To128(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal128(DecimalRange.Enforce(checked(l + r), resultType, overflow));
    }

    // Subtraction mirrors addition with a sign flip

    public static Decimal32 Subtract(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        int l = ScaleHelper.Rescale32(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        int r = ScaleHelper.Rescale32(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal32(DecimalRange.Enforce(checked(l - r), resultType, overflow));
    }

    public static Decimal64 Subtract(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        long l = ScaleHelper.Rescale64(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        long r = ScaleHelper.Rescale64(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal64(DecimalRange.Enforce(checked(l - r), resultType, overflow));
    }

    public static Decimal128 Subtract(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        Int128 l = ScaleHelper.Rescale128(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        Int128 r = ScaleHelper.Rescale128(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal128(DecimalRange.Enforce(checked(l - r), resultType, overflow));
    }

    /// <summary>
    /// Subtract two 32-bit values, widening to 64-bit result.
    /// Used when the result precision exceeds 9 digits.
    /// </summary>
    public static Decimal64 SubtractWiden(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        long l = ScaleHelper.Widen32To64(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        long r = ScaleHelper.Widen32To64(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal64(DecimalRange.Enforce(checked(l - r), resultType, overflow));
    }

    /// <summary>
    /// Subtract two 64-bit values, widening to 128-bit result.
    /// </summary>
    public static Decimal128 SubtractWiden(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        Int128 l = ScaleHelper.Widen64To128(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        Int128 r = ScaleHelper.Widen64To128(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal128(DecimalRange.Enforce(checked(l - r), resultType, overflow));
    }

    // --- 256-bit ---

    public static Decimal256 Add(Decimal256 left, DecimalType leftType, Decimal256 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        Int256 l = ScaleHelper.Rescale256(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        Int256 r = ScaleHelper.Rescale256(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal256(DecimalRange.Enforce(checked(l + r), resultType, overflow));
    }

    /// <summary>
    /// Add two 128-bit values, widening to 256-bit result.
    /// </summary>
    public static Decimal256 AddWiden(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        Int256 l = ScaleHelper.Widen128To256(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        Int256 r = ScaleHelper.Widen128To256(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal256(DecimalRange.Enforce(checked(l + r), resultType, overflow));
    }

    public static Decimal256 Subtract(Decimal256 left, DecimalType leftType, Decimal256 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        Int256 l = ScaleHelper.Rescale256(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        Int256 r = ScaleHelper.Rescale256(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal256(DecimalRange.Enforce(checked(l - r), resultType, overflow));
    }

    /// <summary>
    /// Subtract two 128-bit values, widening to 256-bit result.
    /// </summary>
    public static Decimal256 SubtractWiden(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType,
        DecimalRounding rounding = DecimalRounding.HalfEven,
        DecimalOverflow overflow = DecimalOverflow.Throw)
    {
        Int256 l = ScaleHelper.Widen128To256(left.Mantissa, leftType.Scale, resultType.Scale, rounding);
        Int256 r = ScaleHelper.Widen128To256(right.Mantissa, rightType.Scale, resultType.Scale, rounding);
        return new Decimal256(DecimalRange.Enforce(checked(l - r), resultType, overflow));
    }
}
