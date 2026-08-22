// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Shared handling for the 64-bit-word bitmaps the columnar entry points take
/// and return: validity coming in, out-of-range going out.
/// </summary>
/// <remarks>
/// Bit i of a mask belongs to element i: word <c>i &gt;&gt; 6</c>, bit
/// <c>i &amp; 63</c>. This is the layout
/// <see cref="DecimalRange"/>.<c>WriteOutOfRangeMask</c> already writes, and the
/// layout an Arrow validity buffer already has, so neither side has to convert.
/// </remarks>
internal static class MaskWords
{
    /// <summary>Words needed to cover <paramref name="length"/> elements.</summary>
    internal static int WordCount(int length) => (length + 63) >> 6;

    /// <summary>
    /// Checks an outbound mask is long enough, clears the words covering the
    /// span, and returns just those words. Clearing up front is what lets a
    /// kernel write only the bits it sets and leave the rest alone.
    /// </summary>
    /// <remarks>
    /// For loops that set bits one at a time — the element-at-a-time paths,
    /// where a row's bit is reached only if that row is out of range. A loop
    /// that instead composes a whole word and assigns it wants
    /// <see cref="PrepareOutForFullWrite"/>, which skips the clear.
    /// </remarks>
    internal static Span<ulong> PrepareOut(int length, Span<ulong> mask, string paramName)
    {
        int words = WordCount(length);
        if (mask.Length < words)
            throw new ArgumentException(
                $"Mask must be at least {words} words for {length} values.", paramName);

        Span<ulong> used = mask.Slice(0, words);
        used.Clear();
        return used;
    }

    /// <summary>
    /// Checks an outbound mask is long enough and returns the words covering
    /// the span without clearing them.
    /// </summary>
    /// <remarks>
    /// Only for a caller that assigns <em>every</em> returned word, which the
    /// word-at-a-time loops in the divide and modulus kernels do: they build a
    /// word's worth of flags and store it whether or not any bit is set, so
    /// clearing first would write the buffer twice and, worse, suggest to a
    /// reader that the clear is load-bearing. A caller that only sets bits it
    /// finds must use <see cref="PrepareOut"/> instead, or stale bits from a
    /// reused buffer will be reported as out-of-range rows.
    /// </remarks>
    internal static Span<ulong> PrepareOutForFullWrite(int length, Span<ulong> mask, string paramName)
    {
        int words = WordCount(length);
        if (mask.Length < words)
            throw new ArgumentException(
                $"Mask must be at least {words} words for {length} values.", paramName);

        return mask.Slice(0, words);
    }

    /// <summary>
    /// Checks an inbound validity bitmap is long enough and returns just the
    /// words covering the span. Bits past the end of the span are ignored
    /// rather than rejected — a caller slicing a longer column should not have
    /// to trim its bitmap to match.
    /// </summary>
    internal static ReadOnlySpan<ulong> PrepareIn(int length, ReadOnlySpan<ulong> validity, string paramName)
    {
        int words = WordCount(length);
        if (validity.Length < words)
            throw new ArgumentException(
                $"Validity mask must be at least {words} words for {length} values.", paramName);

        return validity.Slice(0, words);
    }

    /// <summary>
    /// Clears the bits of the final word that sit past the end of the span, so
    /// an iteration over set bits never runs off the end of the operands.
    /// </summary>
    internal static ulong Live(ulong bits, int wordIndex, int length)
    {
        int covered = length - (wordIndex << 6);
        return covered >= 64 ? bits : bits & ((1UL << covered) - 1);
    }
}
