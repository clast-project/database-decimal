// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;

namespace Clast.DatabaseDecimal;

/// <summary>
/// Describes a fixed-point decimal type with a given precision and scale.
/// Precision is the total number of significant digits (1..76).
/// Scale is the number of digits after the decimal point (0..precision).
/// The backing integer width is derived from the precision.
/// </summary>
/// <remarks>
/// Every constructed value is validated, so <see cref="IntegerDigits"/> is never
/// negative. The one unvalidated value is <c>default(DecimalType)</c>, which a
/// struct always permits: it has a precision of 0 and behaves as NUMERIC(0,0).
/// </remarks>
public readonly record struct DecimalType
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
    /// Creates a DecimalType, validating precision and scale.
    /// </summary>
    /// <param name="precision">Total number of significant digits, 1..76.</param>
    /// <param name="scale">Digits after the decimal point, 0..<paramref name="precision"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The precision is outside 1..76, or the scale exceeds the precision.
    /// </exception>
    public DecimalType(byte precision, byte scale)
    {
        if (precision < 1 || precision > MaxPrecision256)
            ThrowPrecisionOutOfRange(precision);

        if (scale > precision)
            ThrowScaleOutOfRange(precision, scale);

        Precision = precision;
        Scale = scale;
    }

    /// <summary>The total number of significant digits, 1..76.</summary>
    public byte Precision { get; }

    /// <summary>The number of digits after the decimal point, 0..<see cref="Precision"/>.</summary>
    public byte Scale { get; }

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
    /// Creates a DecimalType with validation. Equivalent to the constructor, but
    /// takes <see cref="int"/> arguments so out-of-range values are rejected rather
    /// than silently truncated by the conversion to <see cref="byte"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The precision is outside 1..76, or the scale is negative or exceeds the precision.
    /// </exception>
    public static DecimalType Numeric(int precision, int scale)
    {
        if (precision < 1 || precision > MaxPrecision256)
            ThrowPrecisionOutOfRange(precision);

        if (scale < 0 || scale > precision)
            ThrowScaleOutOfRange(precision, scale);

        return new DecimalType((byte)precision, (byte)scale);
    }

    /// <summary>Splits the type into its precision and scale.</summary>
    public void Deconstruct(out byte precision, out byte scale)
    {
        precision = Precision;
        scale = Scale;
    }

    public override string ToString() => $"NUMERIC({Precision},{Scale})";

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowPrecisionOutOfRange(int precision) =>
        throw new ArgumentOutOfRangeException(nameof(precision),
            $"Precision must be between 1 and {MaxPrecision256}, got {precision}.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowScaleOutOfRange(int precision, int scale) =>
        throw new ArgumentOutOfRangeException(nameof(scale),
            $"Scale must be between 0 and precision ({precision}), got {scale}.");
}
