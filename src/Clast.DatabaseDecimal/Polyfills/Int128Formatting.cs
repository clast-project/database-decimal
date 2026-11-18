// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

#if NETSTANDARD2_0
using System.Globalization;
using System.Text;

namespace System;

/// <summary>
/// Standard numeric format strings for the netstandard2.0 <see cref="Int128"/> and
/// <see cref="UInt128"/> polyfills.
/// </summary>
/// <remarks>
/// Only the integer specifiers whose output does not depend on culture beyond the
/// sign — <c>G</c>, <c>D</c>, <c>R</c> and <c>X</c> — are implemented. Anything
/// else (<c>N</c>, <c>F</c>, <c>E</c>, <c>C</c>, <c>P</c>, <c>B</c>, custom
/// patterns) throws <see cref="FormatException"/> rather than falling back to the
/// plain decimal string. The fallback is what the polyfill did before, and it was
/// the worse failure: <c>$"{value:X}"</c> printed hex on net8.0 and decimal on
/// netstandard2.0, with nothing to say the two had diverged.
/// </remarks>
internal static class Int128Formatting
{
    /// <param name="negative">Whether the value is negative.</param>
    /// <param name="magnitude">The absolute value.</param>
    /// <param name="bits">The raw 128-bit pattern, two's complement for a signed value; what <c>X</c> prints.</param>
    /// <param name="format">A standard format string, or null/empty for <c>G</c>.</param>
    /// <param name="provider">Supplies the negative sign and, for scientific <c>G</c>, the decimal separator.</param>
    public static string Format(bool negative, UInt128 magnitude, UInt128 bits, string? format, IFormatProvider? provider)
    {
        char specifier = 'G';
        int precision = -1;

        if (!string.IsNullOrEmpty(format))
        {
            specifier = format![0];
            if (format.Length > 1)
            {
                // A standard format string is one letter and at most nine digits;
                // anything else is a custom pattern, which is not supported.
                if (format.Length > 10) throw Unsupported(format);
                precision = 0;
                for (int i = 1; i < format.Length; i++)
                {
                    char c = format[i];
                    if (c < '0' || c > '9') throw Unsupported(format);
                    precision = precision * 10 + (c - '0');
                }
            }
        }

        switch (specifier)
        {
            case 'D': case 'd':
                return Decimal(negative, magnitude, precision, provider);
            case 'G': case 'g':
            case 'R': case 'r':
                // The BCL treats R on an integer as G, precision included: R5
                // switches to scientific notation just as G5 does.
                return General(negative, magnitude, precision, char.IsUpper(specifier) ? 'E' : 'e', provider);
            case 'X': case 'x':
                return Hex(bits, precision, specifier == 'X');
            default:
                throw Unsupported(format!);
        }
    }

    private static FormatException Unsupported(string format) =>
        new($"Format specifier '{format}' is not supported by the netstandard2.0 Int128/UInt128 polyfill, " +
            "which implements only G, D, R and X.");

    private static string Decimal(bool negative, UInt128 magnitude, int precision, IFormatProvider? provider)
    {
        string digits = magnitude.ToString();
        if (precision > digits.Length) digits = digits.PadLeft(precision, '0');
        return negative ? NumberFormatInfo.GetInstance(provider).NegativeSign + digits : digits;
    }

    private static string General(bool negative, UInt128 magnitude, int precision, char exponentChar, IFormatProvider? provider)
    {
        string digits = magnitude.ToString();

        // Without a precision, or with one that already holds every digit, G is
        // just the integer. Only a precision that cuts digits off switches to
        // scientific notation.
        if (precision <= 0 || precision >= digits.Length)
            return Decimal(negative, magnitude, -1, provider);

        NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);
        int exponent = digits.Length - 1;

        // Keep `precision` significant digits, rounding half away from zero.
        char[] kept = digits.ToCharArray(0, precision);
        if (digits[precision] >= '5')
        {
            int i = precision - 1;
            while (i >= 0 && kept[i] == '9') kept[i--] = '0';
            if (i >= 0)
            {
                kept[i]++;
            }
            else
            {
                // Carried out of the top digit: 99.. rounds to 10.., one order up.
                kept[0] = '1';
                exponent++;
            }
        }

        int length = kept.Length;
        while (length > 1 && kept[length - 1] == '0') length--;

        var sb = new StringBuilder();
        if (negative) sb.Append(info.NegativeSign);
        sb.Append(kept[0]);
        if (length > 1)
        {
            sb.Append(info.NumberDecimalSeparator);
            sb.Append(kept, 1, length - 1);
        }
        sb.Append(exponentChar);
        sb.Append(info.PositiveSign);
        sb.Append(exponent.ToString("D2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    private static string Hex(UInt128 bits, int precision, bool upper)
    {
        string alphabet = upper ? "0123456789ABCDEF" : "0123456789abcdef";
        Span<char> buffer = stackalloc char[32];
        int pos = buffer.Length;
        ulong lower = bits._lower, upperHalf = bits._upper;

        do
        {
            buffer[--pos] = alphabet[(int)(lower & 0xF)];
            lower = (lower >> 4) | (upperHalf << 60);
            upperHalf >>= 4;
        }
        while (lower != 0 || upperHalf != 0);

        string hex = buffer.Slice(pos).ToString();
        return precision > hex.Length ? hex.PadLeft(precision, '0') : hex;
    }
}
#endif
