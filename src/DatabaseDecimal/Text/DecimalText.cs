using DatabaseDecimal.Arithmetic;
using DatabaseDecimal.Values;

namespace DatabaseDecimal.Text;

/// <summary>
/// Formats and parses fixed-point decimal values as text.
/// Supports both UTF-16 (Span&lt;char&gt;) and UTF-8 (Span&lt;byte&gt;) targets.
/// All characters in decimal text are ASCII, so char↔byte conversion is trivial.
/// </summary>
public static class DecimalText
{
    /// <summary>
    /// Returns the maximum number of characters/bytes needed to format
    /// a value of the given type. Accounts for sign, integer digits, dot, and scale.
    /// </summary>
    public static int MaxFormattedLength(DecimalType type) =>
        1 + type.IntegerDigits + (type.Scale > 0 ? 1 + type.Scale : 0);

    // ================================================================
    // TryFormat — UTF-16
    // ================================================================

    public static bool TryFormat(Decimal32 value, DecimalType type,
        Span<char> destination, out int charsWritten) =>
        TryFormatChar(value.Mantissa, type.Scale, destination, out charsWritten);

    public static bool TryFormat(Decimal64 value, DecimalType type,
        Span<char> destination, out int charsWritten) =>
        TryFormatChar(value.Mantissa, type.Scale, destination, out charsWritten);

    public static bool TryFormat(Decimal128 value, DecimalType type,
        Span<char> destination, out int charsWritten) =>
        TryFormatChar(value.Mantissa, type.Scale, destination, out charsWritten);

    public static bool TryFormat(Decimal256 value, DecimalType type,
        Span<char> destination, out int charsWritten) =>
        TryFormatChar(value.Mantissa, type.Scale, destination, out charsWritten);

    // ================================================================
    // TryFormat — UTF-8
    // ================================================================

    public static bool TryFormat(Decimal32 value, DecimalType type,
        Span<byte> destination, out int bytesWritten) =>
        TryFormatUtf8(value.Mantissa, type.Scale, destination, out bytesWritten);

    public static bool TryFormat(Decimal64 value, DecimalType type,
        Span<byte> destination, out int bytesWritten) =>
        TryFormatUtf8(value.Mantissa, type.Scale, destination, out bytesWritten);

    public static bool TryFormat(Decimal128 value, DecimalType type,
        Span<byte> destination, out int bytesWritten) =>
        TryFormatUtf8(value.Mantissa, type.Scale, destination, out bytesWritten);

    public static bool TryFormat(Decimal256 value, DecimalType type,
        Span<byte> destination, out int bytesWritten) =>
        TryFormatUtf8(value.Mantissa, type.Scale, destination, out bytesWritten);

    // ================================================================
    // Format — convenience allocating methods
    // ================================================================

    public static string Format(Decimal32 value, DecimalType type) => FormatToString(value.Mantissa, type.Scale);
    public static string Format(Decimal64 value, DecimalType type) => FormatToString(value.Mantissa, type.Scale);
    public static string Format(Decimal128 value, DecimalType type) => FormatToString(value.Mantissa, type.Scale);
    public static string Format(Decimal256 value, DecimalType type) => FormatToString(value.Mantissa, type.Scale);

    // ================================================================
    // Parse — UTF-16, throwing
    // ================================================================

    public static Decimal32 ParseDecimal32(ReadOnlySpan<char> text, DecimalType type)
    {
        if (!TryParseDecimal32(text, type, out var result))
            throw new FormatException($"Cannot parse '{text.ToString()}' as {type}.");
        return result;
    }

    public static Decimal64 ParseDecimal64(ReadOnlySpan<char> text, DecimalType type)
    {
        if (!TryParseDecimal64(text, type, out var result))
            throw new FormatException($"Cannot parse '{text.ToString()}' as {type}.");
        return result;
    }

    public static Decimal128 ParseDecimal128(ReadOnlySpan<char> text, DecimalType type)
    {
        if (!TryParseDecimal128(text, type, out var result))
            throw new FormatException($"Cannot parse '{text.ToString()}' as {type}.");
        return result;
    }

    public static Decimal256 ParseDecimal256(ReadOnlySpan<char> text, DecimalType type)
    {
        if (!TryParseDecimal256(text, type, out var result))
            throw new FormatException($"Cannot parse '{text.ToString()}' as {type}.");
        return result;
    }

    // ================================================================
    // TryParse — UTF-16
    // ================================================================

    public static bool TryParseDecimal32(ReadOnlySpan<char> text, DecimalType type, out Decimal32 result)
    {
        result = default;
        if (!TryParseMantissa128(text, type.Scale, out Int128 mantissa, out bool isNegative))
            return false;

        if (isNegative) mantissa = -mantissa;
        if (Int128.Abs(mantissa) >= PowersOf10.Int128[type.Precision])
            return false;
        if (mantissa < int.MinValue || mantissa > int.MaxValue)
            return false;

        result = new Decimal32((int)mantissa);
        return true;
    }

    public static bool TryParseDecimal64(ReadOnlySpan<char> text, DecimalType type, out Decimal64 result)
    {
        result = default;
        if (!TryParseMantissa128(text, type.Scale, out Int128 mantissa, out bool isNegative))
            return false;

        if (isNegative) mantissa = -mantissa;
        if (Int128.Abs(mantissa) >= PowersOf10.Int128[type.Precision])
            return false;
        if (mantissa < long.MinValue || mantissa > long.MaxValue)
            return false;

        result = new Decimal64((long)mantissa);
        return true;
    }

    public static bool TryParseDecimal128(ReadOnlySpan<char> text, DecimalType type, out Decimal128 result)
    {
        result = default;
        if (!TryParseMantissa128(text, type.Scale, out Int128 mantissa, out bool isNegative))
            return false;

        if (isNegative) mantissa = -mantissa;
        if (Int128.Abs(mantissa) >= PowersOf10.Int128[type.Precision])
            return false;

        result = new Decimal128(mantissa);
        return true;
    }

    public static bool TryParseDecimal256(ReadOnlySpan<char> text, DecimalType type, out Decimal256 result)
    {
        result = default;
        if (!TryParseMantissa256(text, type.Scale, out Int256 mantissa, out bool isNegative))
            return false;

        if (isNegative) mantissa = -mantissa;
        if (Int256.Abs(mantissa) >= PowersOf10.Int256[type.Precision])
            return false;

        result = new Decimal256(mantissa);
        return true;
    }

    // ================================================================
    // Parse — UTF-8, throwing
    // ================================================================

    public static Decimal32 ParseDecimal32(ReadOnlySpan<byte> utf8, DecimalType type)
    {
        if (!TryParseDecimal32(utf8, type, out var result))
            throw new FormatException($"Cannot parse UTF-8 input as {type}.");
        return result;
    }

    public static Decimal64 ParseDecimal64(ReadOnlySpan<byte> utf8, DecimalType type)
    {
        if (!TryParseDecimal64(utf8, type, out var result))
            throw new FormatException($"Cannot parse UTF-8 input as {type}.");
        return result;
    }

    public static Decimal128 ParseDecimal128(ReadOnlySpan<byte> utf8, DecimalType type)
    {
        if (!TryParseDecimal128(utf8, type, out var result))
            throw new FormatException($"Cannot parse UTF-8 input as {type}.");
        return result;
    }

    public static Decimal256 ParseDecimal256(ReadOnlySpan<byte> utf8, DecimalType type)
    {
        if (!TryParseDecimal256(utf8, type, out var result))
            throw new FormatException($"Cannot parse UTF-8 input as {type}.");
        return result;
    }

    // ================================================================
    // TryParse — UTF-8 (widen bytes to chars, delegate to char version)
    // ================================================================

    public static bool TryParseDecimal32(ReadOnlySpan<byte> utf8, DecimalType type, out Decimal32 result)
    {
        Span<char> chars = stackalloc char[utf8.Length];
        for (int i = 0; i < utf8.Length; i++) chars[i] = (char)utf8[i];
        return TryParseDecimal32(chars, type, out result);
    }

    public static bool TryParseDecimal64(ReadOnlySpan<byte> utf8, DecimalType type, out Decimal64 result)
    {
        Span<char> chars = stackalloc char[utf8.Length];
        for (int i = 0; i < utf8.Length; i++) chars[i] = (char)utf8[i];
        return TryParseDecimal64(chars, type, out result);
    }

    public static bool TryParseDecimal128(ReadOnlySpan<byte> utf8, DecimalType type, out Decimal128 result)
    {
        Span<char> chars = stackalloc char[utf8.Length];
        for (int i = 0; i < utf8.Length; i++) chars[i] = (char)utf8[i];
        return TryParseDecimal128(chars, type, out result);
    }

    public static bool TryParseDecimal256(ReadOnlySpan<byte> utf8, DecimalType type, out Decimal256 result)
    {
        Span<char> chars = stackalloc char[utf8.Length];
        for (int i = 0; i < utf8.Length; i++) chars[i] = (char)utf8[i];
        return TryParseDecimal256(chars, type, out result);
    }

    // ================================================================
    // Formatting core — writes ASCII bytes into a buffer, returns length.
    // Shared between UTF-16 and UTF-8 output paths.
    // ================================================================

    private static int FormatToAscii(int mantissa, byte scale, Span<byte> buffer)
    {
        bool negative = mantissa < 0;
        long abs = Math.Abs((long)mantissa); // widen to handle int.MinValue
        return FormatAbsToAscii(abs, negative, scale, buffer);
    }

    private static int FormatToAscii(long mantissa, byte scale, Span<byte> buffer)
    {
        bool negative = mantissa < 0;
        // Handle long.MinValue: widen magnitude to UInt128 path
        if (mantissa == long.MinValue)
            return FormatAbsToAscii128((UInt128)Int128.MaxValue + 1, true, scale, buffer);
        long abs = Math.Abs(mantissa);
        return FormatAbsToAscii(abs, negative, scale, buffer);
    }

    private static int FormatToAscii(Int128 mantissa, byte scale, Span<byte> buffer)
    {
        bool negative = mantissa < 0;
        UInt128 abs = negative ? ~(UInt128)mantissa + 1 : (UInt128)mantissa;
        return FormatAbsToAscii128(abs, negative, scale, buffer);
    }

    private static int FormatToAscii(Int256 mantissa, byte scale, Span<byte> buffer)
    {
        bool negative = Int256.IsNegative(mantissa);
        UInt256 abs = negative ? ~(UInt256)mantissa + UInt256.One : (UInt256)mantissa;
        return FormatAbsToAscii256(abs, negative, scale, buffer);
    }

    /// <summary>Format absolute value (up to 64-bit) into ASCII buffer.</summary>
    private static int FormatAbsToAscii(long abs, bool negative, byte scale, Span<byte> buffer)
    {
        // Extract digits right-to-left
        Span<byte> digits = stackalloc byte[20]; // long has at most 19 digits
        int digitCount = 0;
        long v = abs;
        do
        {
            digits[digitCount++] = (byte)(v % 10);
            v /= 10;
        } while (v != 0);

        return WriteFormattedAscii(digits, digitCount, negative, scale, buffer);
    }

    /// <summary>Format absolute value (up to 128-bit) into ASCII buffer.</summary>
    private static int FormatAbsToAscii128(UInt128 abs, bool negative, byte scale, Span<byte> buffer)
    {
        Span<byte> digits = stackalloc byte[40]; // Int128 has at most 39 digits
        int digitCount = 0;
        UInt128 v = abs;
        do
        {
            digits[digitCount++] = (byte)(uint)(v % 10);
            v /= 10;
        } while (v != UInt128.Zero);

        return WriteFormattedAscii(digits, digitCount, negative, scale, buffer);
    }

    /// <summary>Format absolute value (up to 256-bit) into ASCII buffer.</summary>
    private static int FormatAbsToAscii256(UInt256 abs, bool negative, byte scale, Span<byte> buffer)
    {
        Span<byte> digits = stackalloc byte[78]; // UInt256 has at most 77 digits
        int digitCount = 0;
        UInt256 v = abs;
        do
        {
            UInt256 remainder = v % (UInt256)10UL;
            digits[digitCount++] = (byte)(ulong)remainder;
            v = v / (UInt256)10UL;
        } while (v != UInt256.Zero);

        return WriteFormattedAscii(digits, digitCount, negative, scale, buffer);
    }

    /// <summary>
    /// Takes a reversed digit array and writes the formatted decimal
    /// (sign, integer part, dot, fractional part) into the ASCII buffer.
    /// </summary>
    private static int WriteFormattedAscii(
        ReadOnlySpan<byte> digits, int digitCount,
        bool negative, byte scale, Span<byte> buffer)
    {
        // Pad with leading zeros so we have at least scale + 1 digits
        // (ensures at least one integer digit, e.g. "0.042" not ".042")
        int effectiveDigits = Math.Max(digitCount, scale + 1);

        int pos = 0;
        if (negative) buffer[pos++] = (byte)'-';

        // Integer digits (leftmost)
        int intDigits = effectiveDigits - scale;
        for (int i = intDigits - 1; i >= 0; i--)
        {
            int di = scale + i; // index into reversed digits array
            buffer[pos++] = (byte)('0' + (di < digitCount ? digits[di] : 0));
        }

        if (scale > 0)
        {
            buffer[pos++] = (byte)'.';

            // Fractional digits (rightmost of mantissa)
            for (int i = scale - 1; i >= 0; i--)
                buffer[pos++] = (byte)('0' + (i < digitCount ? digits[i] : 0));
        }

        return pos;
    }

    // ================================================================
    // TryFormat dispatch — char and byte
    // ================================================================

    private static bool TryFormatChar(int mantissa, byte scale, Span<char> dest, out int written)
    {
        Span<byte> buf = stackalloc byte[22];
        int len = FormatToAscii(mantissa, scale, buf);
        if (dest.Length < len) { written = 0; return false; }
        for (int i = 0; i < len; i++) dest[i] = (char)buf[i];
        written = len;
        return true;
    }

    private static bool TryFormatChar(long mantissa, byte scale, Span<char> dest, out int written)
    {
        Span<byte> buf = stackalloc byte[22];
        int len = FormatToAscii(mantissa, scale, buf);
        if (dest.Length < len) { written = 0; return false; }
        for (int i = 0; i < len; i++) dest[i] = (char)buf[i];
        written = len;
        return true;
    }

    private static bool TryFormatChar(Int128 mantissa, byte scale, Span<char> dest, out int written)
    {
        Span<byte> buf = stackalloc byte[42];
        int len = FormatToAscii(mantissa, scale, buf);
        if (dest.Length < len) { written = 0; return false; }
        for (int i = 0; i < len; i++) dest[i] = (char)buf[i];
        written = len;
        return true;
    }

    private static bool TryFormatChar(Int256 mantissa, byte scale, Span<char> dest, out int written)
    {
        Span<byte> buf = stackalloc byte[80];
        int len = FormatToAscii(mantissa, scale, buf);
        if (dest.Length < len) { written = 0; return false; }
        for (int i = 0; i < len; i++) dest[i] = (char)buf[i];
        written = len;
        return true;
    }

    private static bool TryFormatUtf8(int mantissa, byte scale, Span<byte> dest, out int written)
    {
        Span<byte> buf = stackalloc byte[22];
        int len = FormatToAscii(mantissa, scale, buf);
        if (dest.Length < len) { written = 0; return false; }
        buf.Slice(0, len).CopyTo(dest);
        written = len;
        return true;
    }

    private static bool TryFormatUtf8(long mantissa, byte scale, Span<byte> dest, out int written)
    {
        Span<byte> buf = stackalloc byte[22];
        int len = FormatToAscii(mantissa, scale, buf);
        if (dest.Length < len) { written = 0; return false; }
        buf.Slice(0, len).CopyTo(dest);
        written = len;
        return true;
    }

    private static bool TryFormatUtf8(Int128 mantissa, byte scale, Span<byte> dest, out int written)
    {
        Span<byte> buf = stackalloc byte[42];
        int len = FormatToAscii(mantissa, scale, buf);
        if (dest.Length < len) { written = 0; return false; }
        buf.Slice(0, len).CopyTo(dest);
        written = len;
        return true;
    }

    private static bool TryFormatUtf8(Int256 mantissa, byte scale, Span<byte> dest, out int written)
    {
        Span<byte> buf = stackalloc byte[80];
        int len = FormatToAscii(mantissa, scale, buf);
        if (dest.Length < len) { written = 0; return false; }
        buf.Slice(0, len).CopyTo(dest);
        written = len;
        return true;
    }

    private static string FormatToString(int mantissa, byte scale)
    {
        Span<byte> buf = stackalloc byte[22];
        int len = FormatToAscii(mantissa, scale, buf);
        return AsciiToString(buf, len);
    }

    private static string FormatToString(long mantissa, byte scale)
    {
        Span<byte> buf = stackalloc byte[22];
        int len = FormatToAscii(mantissa, scale, buf);
        return AsciiToString(buf, len);
    }

    private static string FormatToString(Int128 mantissa, byte scale)
    {
        Span<byte> buf = stackalloc byte[42];
        int len = FormatToAscii(mantissa, scale, buf);
        return AsciiToString(buf, len);
    }

    private static string FormatToString(Int256 mantissa, byte scale)
    {
        Span<byte> buf = stackalloc byte[80];
        int len = FormatToAscii(mantissa, scale, buf);
        return AsciiToString(buf, len);
    }

    private static string AsciiToString(ReadOnlySpan<byte> ascii, int len)
    {
        Span<char> chars = stackalloc char[len];
        for (int i = 0; i < len; i++) chars[i] = (char)ascii[i];
        return new string(chars);
    }

    // ================================================================
    // Parsing core — accumulates digits into Int128 (covers 32/64/128)
    // ================================================================

    /// <summary>
    /// Parses decimal text into an unsigned Int128 mantissa, tracking sign
    /// separately. The mantissa is scaled to the target scale, with
    /// banker's rounding applied for excess fractional digits.
    /// </summary>
    private static bool TryParseMantissa128(
        ReadOnlySpan<char> text, byte targetScale,
        out Int128 mantissa, out bool isNegative)
    {
        mantissa = Int128.Zero;
        isNegative = false;

        text = text.Trim();
        if (text.IsEmpty) return false;

        int pos = 0;

        // Sign
        if (text[pos] == '-') { isNegative = true; pos++; }
        else if (text[pos] == '+') { pos++; }
        if (pos >= text.Length) return false;

        bool hasDigits = false;
        bool inFraction = false;
        int fracDigitsAccumulated = 0;
        int intSignificantDigits = 0;
        int roundingDigit = -1;
        bool hasNonZeroAfterRounding = false;
        bool pastLeadingZeros = false;

        while (pos < text.Length)
        {
            char c = text[pos++];

            if (c >= '0' && c <= '9')
            {
                int digit = c - '0';
                hasDigits = true;

                if (!inFraction)
                {
                    if (digit != 0 || pastLeadingZeros)
                    {
                        pastLeadingZeros = true;
                        intSignificantDigits++;
                        if (intSignificantDigits > 39) return false; // exceeds Int128 capacity
                    }
                    mantissa = mantissa * 10 + digit;
                }
                else
                {
                    if (fracDigitsAccumulated < targetScale)
                    {
                        mantissa = mantissa * 10 + digit;
                        fracDigitsAccumulated++;
                    }
                    else if (roundingDigit < 0)
                    {
                        roundingDigit = digit;
                    }
                    else if (digit != 0)
                    {
                        hasNonZeroAfterRounding = true;
                    }
                }
            }
            else if (c == '.' && !inFraction)
            {
                inFraction = true;
            }
            else
            {
                return false; // invalid character
            }
        }

        if (!hasDigits) return false;

        // Scale up if fewer fractional digits than target
        if (fracDigitsAccumulated < targetScale)
        {
            int scaleUp = targetScale - fracDigitsAccumulated;
            mantissa *= PowersOf10.Int128[scaleUp];
        }

        // Apply banker's rounding for excess fractional digits
        if (roundingDigit >= 0)
        {
            if (roundingDigit > 5 || (roundingDigit == 5 && hasNonZeroAfterRounding))
                mantissa++;
            else if (roundingDigit == 5 && !hasNonZeroAfterRounding && (mantissa & 1) != 0)
                mantissa++;
        }

        return true;
    }

    /// <summary>
    /// Parses decimal text into an unsigned Int256 mantissa.
    /// Same algorithm as TryParseMantissa128 but for the 256-bit tier.
    /// </summary>
    private static bool TryParseMantissa256(
        ReadOnlySpan<char> text, byte targetScale,
        out Int256 mantissa, out bool isNegative)
    {
        mantissa = Int256.Zero;
        isNegative = false;

        text = text.Trim();
        if (text.IsEmpty) return false;

        int pos = 0;

        if (text[pos] == '-') { isNegative = true; pos++; }
        else if (text[pos] == '+') { pos++; }
        if (pos >= text.Length) return false;

        bool hasDigits = false;
        bool inFraction = false;
        int fracDigitsAccumulated = 0;
        int intSignificantDigits = 0;
        int roundingDigit = -1;
        bool hasNonZeroAfterRounding = false;
        bool pastLeadingZeros = false;
        Int256 ten = (Int256)10;

        while (pos < text.Length)
        {
            char c = text[pos++];

            if (c >= '0' && c <= '9')
            {
                int digit = c - '0';
                hasDigits = true;

                if (!inFraction)
                {
                    if (digit != 0 || pastLeadingZeros)
                    {
                        pastLeadingZeros = true;
                        intSignificantDigits++;
                        if (intSignificantDigits > 77) return false;
                    }
                    mantissa = mantissa * ten + (Int256)digit;
                }
                else
                {
                    if (fracDigitsAccumulated < targetScale)
                    {
                        mantissa = mantissa * ten + (Int256)digit;
                        fracDigitsAccumulated++;
                    }
                    else if (roundingDigit < 0)
                    {
                        roundingDigit = digit;
                    }
                    else if (digit != 0)
                    {
                        hasNonZeroAfterRounding = true;
                    }
                }
            }
            else if (c == '.' && !inFraction)
            {
                inFraction = true;
            }
            else
            {
                return false;
            }
        }

        if (!hasDigits) return false;

        if (fracDigitsAccumulated < targetScale)
        {
            int scaleUp = targetScale - fracDigitsAccumulated;
            mantissa *= PowersOf10.Int256[scaleUp];
        }

        if (roundingDigit >= 0)
        {
            if (roundingDigit > 5 || (roundingDigit == 5 && hasNonZeroAfterRounding))
                mantissa += Int256.One;
            else if (roundingDigit == 5 && !hasNonZeroAfterRounding && (mantissa & Int256.One) != Int256.Zero)
                mantissa += Int256.One;
        }

        return true;
    }
}
