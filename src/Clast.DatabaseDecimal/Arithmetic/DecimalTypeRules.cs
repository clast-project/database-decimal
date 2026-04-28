// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Computes result DecimalType for arithmetic operations using
/// SQL Server / Substrait promotion rules.
/// </summary>
public static class DecimalTypeRules
{
    private const int MaxPrecision = DecimalType.MaxPrecision128; // 38, matching SQL Server

    public static DecimalType Add(DecimalType left, DecimalType right) =>
        AddOrSubtract(left, right);

    public static DecimalType Subtract(DecimalType left, DecimalType right) =>
        AddOrSubtract(left, right);

    private static DecimalType AddOrSubtract(DecimalType left, DecimalType right)
    {
        int s = Math.Max(left.Scale, right.Scale);
        int p = Math.Max(left.IntegerDigits, right.IntegerDigits) + 1 + s;
        return Clamp(p, s);
    }

    public static DecimalType Multiply(DecimalType left, DecimalType right)
    {
        int s = left.Scale + right.Scale;
        int p = left.Precision + right.Precision + 1;
        return Clamp(p, s);
    }

    public static DecimalType Divide(DecimalType left, DecimalType right)
    {
        int s = Math.Max(6, left.Scale + right.Precision + 1);
        int p = left.IntegerDigits + right.Precision + s;
        return Clamp(p, s);
    }

    public static DecimalType Modulus(DecimalType left, DecimalType right)
    {
        int s = Math.Max(left.Scale, right.Scale);
        int p = Math.Min(left.IntegerDigits, right.IntegerDigits) + s;
        return Clamp(p, s);
    }

    /// <summary>
    /// Clamps precision to MaxPrecision, borrowing from scale if needed.
    /// Follows SQL Server / Substrait overflow rules:
    /// scale is reduced but never below min(original_scale, 6).
    /// </summary>
    private static DecimalType Clamp(int precision, int scale)
    {
        if (precision <= MaxPrecision)
            return DecimalType.Numeric(precision, scale);

        int minScale = Math.Min(scale, 6);
        int delta = precision - MaxPrecision;
        int newScale = Math.Max(scale - delta, minScale);
        return DecimalType.Numeric(MaxPrecision, newScale);
    }
}
