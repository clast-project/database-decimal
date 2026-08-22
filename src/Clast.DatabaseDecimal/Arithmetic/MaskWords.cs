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
    /// span, and returns just those words. Clearing up front is what lets the
    /// kernels write only the bits they set.
    /// </summary>
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
