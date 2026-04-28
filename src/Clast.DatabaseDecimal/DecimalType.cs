// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Clast.DatabaseDecimal;

/// <summary>
/// Describes a fixed-point decimal type with a given precision and scale.
/// Precision is the total number of significant digits (1..76).
/// Scale is the number of digits after the decimal point (0..precision).
/// The backing integer width is derived from the precision.
/// </summary>
public readonly record struct DecimalType(byte Precision, byte Scale)
{
    /// <summary>Max digits for a 32-bit mantissa: floor(log10(2^31)) = 9.</summary>
    public const int MaxPrecision32 = 9;

    /// <summary>Max digits for a 64-bit mantissa: floor(log10(2^63)) = 18.</summary>
    public const int MaxPrecision64 = 18;

    /// <summary>Max digits for a 128-bit mantissa: floor(log10(2^127)) = 38.</summary>
    public const int MaxPrecision128 = 38;

    /// <summary>Max digits for a 256-bit mantissa: floor(log10(2^255)) = 76.</summary>
    public const int MaxPrecision256 = 76;

    /// <summary>
    /// The backing integer width tier, derived from the precision.
    /// </summary>
    public DecimalWidth Width => Precision switch
    {
        <= MaxPrecision32 => DecimalWidth.W32,
        <= MaxPrecision64 => DecimalWidth.W64,
        <= MaxPrecision128 => DecimalWidth.W128,
        _ => DecimalWidth.W256,
    };

    /// <summary>
    /// The number of integer digits (digits before the decimal point).
    /// </summary>
    public int IntegerDigits => Precision - Scale;

    /// <summary>
    /// Creates a DecimalType with validation.
    /// </summary>
    public static DecimalType Numeric(int precision, int scale)
    {
        if (precision < 1 || precision > MaxPrecision256)
            throw new ArgumentOutOfRangeException(nameof(precision),
                $"Precision must be between 1 and {MaxPrecision256}, got {precision}.");

        if (scale < 0 || scale > precision)
            throw new ArgumentOutOfRangeException(nameof(scale),
                $"Scale must be between 0 and precision ({precision}), got {scale}.");

        return new DecimalType((byte)precision, (byte)scale);
    }

    public override string ToString() => $"NUMERIC({Precision},{Scale})";
}
