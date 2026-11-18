// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

#if NETSTANDARD2_0
using System.Globalization;

namespace System;

/// <summary>
/// <c>Parse</c> and <c>TryParse</c> for the netstandard2.0 <see cref="Int128"/> and
/// <see cref="UInt128"/> polyfills.
/// </summary>
/// <remarks>
/// <para>
/// Only the styles built from leading and trailing white space, a leading sign, and
/// a hex or binary specifier are implemented — <see cref="NumberStyles.Integer"/>,
/// <see cref="NumberStyles.HexNumber"/> and their subsets, which covers the
/// default. Every other style the BCL accepts (thousands separators, parentheses,
/// trailing signs, decimal points, exponents, currency symbols) throws
/// <see cref="NotSupportedException"/>, from <c>TryParse</c> as well as
/// <c>Parse</c>. Returning <see langword="false"/> would be the quiet failure:
/// <c>"1,000"</c> parsed on net8.0 and would merely fail to parse on
/// netstandard2.0, indistinguishable from bad input.
/// </para>
/// <para>
/// Within that subset the rules are the BCL's, including the corners: white space
/// is only U+0009–U+000D and U+0020; signs are the provider's strings, matched
/// ordinally, positive first; an ASCII hyphen is also a negative sign when the
/// provider's is a dash lookalike such as U+2212; trailing NUL characters are
/// ignored; a format error anywhere outranks overflow; and an unsigned type
/// accepts a negative zero. <c>DecimalText</c> is not reused because its
/// precision stops at 38 digits and these types need 39.
/// </para>
/// </remarks>
internal static class Int128Parsing
{
    private const NumberStyles Whitespace = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

    // NumberStyles.AllowBinarySpecifier and BinaryNumber arrived in .NET 8 and
    // are missing from netstandard2.0's enum, but a caller can still pass the
    // value and the BCL type accepts it.
    private const NumberStyles AllowBinarySpecifier = (NumberStyles)0x400;

    private const NumberStyles AllDefinedStyles =
        NumberStyles.Any | NumberStyles.AllowHexSpecifier | AllowBinarySpecifier;

    internal enum Status { Ok, Format, Overflow }

    /// <summary>
    /// Throws for a style the BCL rejects (<see cref="ArgumentException"/>) and for
    /// one it accepts that this polyfill does not implement
    /// (<see cref="NotSupportedException"/>).
    /// </summary>
    public static void ValidateStyle(NumberStyles style)
    {
        if ((style & ~AllDefinedStyles) != 0)
            throw new ArgumentException("An undefined NumberStyles value is being used.", nameof(style));

        NumberStyles radix = style & (NumberStyles.AllowHexSpecifier | AllowBinarySpecifier);
        if (radix != 0)
        {
            if (radix == (NumberStyles.AllowHexSpecifier | AllowBinarySpecifier) || (style & ~(radix | Whitespace)) != 0)
            {
                throw new ArgumentException(
                    "With the AllowHexSpecifier or AllowBinarySpecifier bit set in the enum bit field, " +
                    "the only other valid bits that can be combined into the enum value must be a subset of " +
                    "those in NumberStyles.HexNumber or NumberStyles.BinaryNumber.",
                    nameof(style));
            }
            return;
        }

        if ((style & ~NumberStyles.Integer) != 0)
        {
            throw new NotSupportedException(
                $"NumberStyles '{style}' is not supported by the netstandard2.0 Int128/UInt128 polyfill, " +
                "which implements only combinations of AllowLeadingWhite, AllowTrailingWhite and AllowLeadingSign, " +
                "or AllowHexSpecifier with white space.");
        }
    }

    /// <summary>Parses a validated style into a raw 128-bit pattern.</summary>
    /// <remarks>
    /// With <c>signed</c> set, for <see cref="Int128"/>, a decimal magnitude may reach
    /// 2^127 when negative and 2^127 − 1 otherwise, and the result is two's
    /// complement. Without it, for <see cref="UInt128"/>, only zero may carry a
    /// negative sign.
    /// </remarks>
    public static Status Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, bool signed, out UInt128 bits)
    {
        bits = UInt128.Zero;
        int i = 0;

        if ((style & NumberStyles.AllowLeadingWhite) != 0)
        {
            while (i < s.Length && IsWhite(s[i])) i++;
        }

        if ((style & NumberStyles.AllowHexSpecifier) != 0)
            return ParsePowerOfTwo(s, i, style, 4, out bits);
        if ((style & AllowBinarySpecifier) != 0)
            return ParsePowerOfTwo(s, i, style, 1, out bits);

        bool negative = false;
        if ((style & NumberStyles.AllowLeadingSign) != 0 && i < s.Length)
        {
            NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);
            string positiveSign = info.PositiveSign, negativeSign = info.NegativeSign;
            ReadOnlySpan<char> rest = s.Slice(i);

            if (s[i] == '-' && AllowsHyphen(negativeSign))
            {
                negative = true;
                i++;
            }
            else if (!string.IsNullOrEmpty(positiveSign) && rest.StartsWith(positiveSign.AsSpan()))
            {
                i += positiveSign.Length;
            }
            else if (!string.IsNullOrEmpty(negativeSign) && rest.StartsWith(negativeSign.AsSpan()))
            {
                negative = true;
                i += negativeSign.Length;
            }
        }

        if (i >= s.Length || !IsDigit(s[i])) return Status.Format;

        // A magnitude only overflows past MaxValue / 10 times ten, so test the
        // accumulator against that before each step rather than after the
        // multiply has already wrapped.
        UInt128 limit = signed ? (negative ? (UInt128)Int128.MinValue : (UInt128)Int128.MaxValue) : UInt128.MaxValue;
        UInt128 tenthOfMax = UInt128.MaxValue / 10UL;
        UInt128 value = UInt128.Zero;
        bool overflow = false;

        for (; i < s.Length && IsDigit(s[i]); i++)
        {
            if (overflow) continue;

            uint digit = (uint)(s[i] - '0');
            if (value > tenthOfMax)
            {
                overflow = true;
                continue;
            }

            UInt128 next = value * 10UL + digit;
            if (next < value || next > limit)
            {
                // Keep scanning: a format error later in the string takes
                // precedence over overflow, as it does in the BCL.
                overflow = true;
                continue;
            }
            value = next;
        }

        if (!AtEnd(s, i, style)) return Status.Format;
        if (overflow) return Status.Overflow;

        if (negative)
        {
            if (!signed && value != UInt128.Zero) return Status.Overflow;
            value = UInt128.Zero - value;
        }

        bits = value;
        return Status.Ok;
    }

    private static Status ParsePowerOfTwo(ReadOnlySpan<char> s, int i, NumberStyles style, int bitsPerDigit, out UInt128 bits)
    {
        bits = UInt128.Zero;
        if (i >= s.Length || DigitValue(s[i], bitsPerDigit) < 0) return Status.Format;

        // Leading zeroes are free; after them the value holds 128 bits, so any
        // significant digit beyond that overflows. The bit pattern is the result
        // for both types — "FFFF…" is −1 as an Int128, as in the BCL.
        while (i < s.Length && s[i] == '0') i++;

        int maxDigits = 128 / bitsPerDigit;
        int count = 0;
        UInt128 value = UInt128.Zero;

        for (; i < s.Length; i++)
        {
            int digit = DigitValue(s[i], bitsPerDigit);
            if (digit < 0) break;
            if (++count <= maxDigits) value = (value << bitsPerDigit) | (uint)digit;
        }

        if (!AtEnd(s, i, style)) return Status.Format;
        if (count > maxDigits) return Status.Overflow;

        bits = value;
        return Status.Ok;
    }

    // What may follow the digits: trailing white space if the style allows it,
    // then any number of NULs, which the BCL ignores.
    private static bool AtEnd(ReadOnlySpan<char> s, int i, NumberStyles style)
    {
        if ((style & NumberStyles.AllowTrailingWhite) != 0)
        {
            while (i < s.Length && IsWhite(s[i])) i++;
        }

        for (; i < s.Length; i++)
        {
            if (s[i] != '\0') return false;
        }
        return true;
    }

    private static bool IsWhite(char c) => c == ' ' || (c >= '\t' && c <= '\r');

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    private static int DigitValue(char c, int bitsPerDigit)
    {
        if (bitsPerDigit == 1) return c == '0' ? 0 : c == '1' ? 1 : -1;
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'a' && c <= 'f') return c - 'a' + 10;
        if (c >= 'A' && c <= 'F') return c - 'A' + 10;
        return -1;
    }

    // The BCL's NumberFormatInfo.AllowHyphenDuringParsing: cultures whose minus
    // sign is a dash lookalike still accept the ASCII hyphen, so "-5" parses under
    // sv-SE on ICU, where the sign is U+2212.
    private static bool AllowsHyphen(string negativeSign) =>
        negativeSign.Length == 1 && negativeSign[0] switch
        {
            '‒' or '⁻' or '₋' or '−' or '➖' or '﹣' or '－' => true,
            _ => false,
        };

    public static FormatException FormatError(ReadOnlySpan<char> s) =>
        new($"The input string '{s.ToString()}' was not in a correct format.");

    public static OverflowException OverflowError(string typeName) =>
        new($"Value was either too large or too small for {typeName}.");
}
#endif
