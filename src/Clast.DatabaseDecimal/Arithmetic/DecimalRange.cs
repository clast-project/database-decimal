// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Checks mantissas against the precision of a <see cref="DecimalType"/>.
/// </summary>
/// <remarks>
/// A mantissa fits a <c>NUMERIC(p,s)</c> when its magnitude is below 10^p. That
/// bound is stricter than the mantissa width: <c>NUMERIC(38,0)</c> allows up to
/// 10^38 - 1, while a 128-bit mantissa reaches about 1.7 × 10^38, and the gap
/// between them is not caught by checked arithmetic.
/// <para>
/// The span methods are the batch counterpart of the scalar ones.
/// <see cref="WriteOutOfRangeMask(ReadOnlySpan{Int128}, DecimalType, Span{ulong})"/>
/// reports every offending element in one pass, which is what a caller wants
/// when overflow means "null this row" rather than "fail the query".
/// </para>
/// </remarks>
public static class DecimalRange
{
    // ================================================================
    // Bounds. A precision at or beyond the width's digit capacity cannot be
    // exceeded by any value of that width, so no bound applies and every value
    // is in range — NUMERIC(19,0) in a 64-bit mantissa, for instance, since
    // long.MaxValue is about 9.2 × 10^18 and the bound would be 10^19.
    // ================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetBound(DecimalType type, out int bound)
    {
        if (type.Precision >= PowersOf10.Int32.Length) { bound = 0; return false; }
        bound = PowersOf10.Int32[type.Precision];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetBound(DecimalType type, out long bound)
    {
        if (type.Precision >= PowersOf10.Int64.Length) { bound = 0L; return false; }
        bound = PowersOf10.Int64[type.Precision];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetBound(DecimalType type, out Int128 bound)
    {
        if (type.Precision >= PowersOf10.Int128.Length) { bound = Int128.Zero; return false; }
        bound = PowersOf10.Int128[type.Precision];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetBound(DecimalType type, out Int256 bound)
    {
        if (type.Precision >= PowersOf10.Int256Values.Length) { bound = Int256.Zero; return false; }
        bound = PowersOf10.Int256Values[type.Precision];
        return true;
    }

    // ================================================================
    // Inclusive bounds, for loops that fold the range test into the
    // arithmetic rather than making a second pass over the output.
    //
    // A type whose precision the width cannot exceed yields the width's own
    // limits, so the comparison is present but never trips. Using a sentinel
    // magnitude instead would misreport MinValue, whose negation is itself.
    // ================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void GetBounds(DecimalType type, out int lower, out int upper)
    {
        if (TryGetBound(type, out int bound)) { upper = bound - 1; lower = -upper; }
        else { upper = int.MaxValue; lower = int.MinValue; }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void GetBounds(DecimalType type, out long lower, out long upper)
    {
        if (TryGetBound(type, out long bound)) { upper = bound - 1L; lower = -upper; }
        else { upper = long.MaxValue; lower = long.MinValue; }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void GetBounds(DecimalType type, out Int128 lower, out Int128 upper)
    {
        if (TryGetBound(type, out Int128 bound)) { upper = bound - Int128.One; lower = -upper; }
        else { upper = Int128.MaxValue; lower = Int128.MinValue; }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void GetBounds(DecimalType type, out Int256 lower, out Int256 upper)
    {
        if (TryGetBound(type, out Int256 bound)) { upper = bound - Int256.One; lower = -upper; }
        else { upper = Int256.MaxValue; lower = Int256.MinValue; }
    }

    /// <summary>Reports a result that does not fit the type's precision.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowOutOfRange(DecimalType type) => ThrowOverflow(type);

    // ================================================================
    // Scalar
    // ================================================================

    /// <summary>Whether a 32-bit mantissa fits the type's precision.</summary>
    public static bool IsInRange(int value, DecimalType type) =>
        !TryGetBound(type, out int bound) || (value > -bound && value < bound);

    /// <summary>Whether a 64-bit mantissa fits the type's precision.</summary>
    public static bool IsInRange(long value, DecimalType type) =>
        !TryGetBound(type, out long bound) || (value > -bound && value < bound);

    /// <summary>Whether a 128-bit mantissa fits the type's precision.</summary>
    public static bool IsInRange(Int128 value, DecimalType type) =>
        !TryGetBound(type, out Int128 bound) || (value > -bound && value < bound);

    /// <summary>Whether a 256-bit mantissa fits the type's precision.</summary>
    public static bool IsInRange(Int256 value, DecimalType type) =>
        !TryGetBound(type, out Int256 bound) || (value > -bound && value < bound);

    /// <summary>Throws if a 32-bit mantissa does not fit the type's precision.</summary>
    /// <exception cref="OverflowException">The value needs more than <c>type.Precision</c> digits.</exception>
    public static void Validate(int value, DecimalType type)
    {
        if (!IsInRange(value, type)) ThrowOverflow(type);
    }

    /// <summary>Throws if a 64-bit mantissa does not fit the type's precision.</summary>
    /// <exception cref="OverflowException">The value needs more than <c>type.Precision</c> digits.</exception>
    public static void Validate(long value, DecimalType type)
    {
        if (!IsInRange(value, type)) ThrowOverflow(type);
    }

    /// <summary>Throws if a 128-bit mantissa does not fit the type's precision.</summary>
    /// <exception cref="OverflowException">The value needs more than <c>type.Precision</c> digits.</exception>
    public static void Validate(Int128 value, DecimalType type)
    {
        if (!IsInRange(value, type)) ThrowOverflow(type);
    }

    /// <summary>Throws if a 256-bit mantissa does not fit the type's precision.</summary>
    /// <exception cref="OverflowException">The value needs more than <c>type.Precision</c> digits.</exception>
    public static void Validate(Int256 value, DecimalType type)
    {
        if (!IsInRange(value, type)) ThrowOverflow(type);
    }

    // ================================================================
    // Kernel entry points — apply the caller's DecimalOverflow policy.
    // ================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Enforce(int value, DecimalType type, DecimalOverflow overflow)
    {
        if (overflow == DecimalOverflow.Throw && !IsInRange(value, type)) ThrowOverflow(type);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long Enforce(long value, DecimalType type, DecimalOverflow overflow)
    {
        if (overflow == DecimalOverflow.Throw && !IsInRange(value, type)) ThrowOverflow(type);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Int128 Enforce(Int128 value, DecimalType type, DecimalOverflow overflow)
    {
        if (overflow == DecimalOverflow.Throw && !IsInRange(value, type)) ThrowOverflow(type);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Int256 Enforce(Int256 value, DecimalType type, DecimalOverflow overflow)
    {
        if (overflow == DecimalOverflow.Throw && !IsInRange(value, type)) ThrowOverflow(type);
        return value;
    }

    // ================================================================
    // Batch — validate
    // ================================================================

    /// <summary>Throws if any element does not fit the type's precision.</summary>
    /// <exception cref="OverflowException">Some element needs more than <c>type.Precision</c> digits.</exception>
    public static void Validate(ReadOnlySpan<int> values, DecimalType type)
    {
        if (AnyOutOfRange(values, type)) ThrowOverflow(type);
    }

    /// <summary>Throws if any element does not fit the type's precision.</summary>
    /// <exception cref="OverflowException">Some element needs more than <c>type.Precision</c> digits.</exception>
    public static void Validate(ReadOnlySpan<long> values, DecimalType type)
    {
        if (AnyOutOfRange(values, type)) ThrowOverflow(type);
    }

    // ================================================================
    // "Is anything out of range" — the form the span kernels need, which does
    // not have to locate the offender and so vectorizes cleanly. It runs over
    // output the arithmetic loop just produced, competing with a SIMD add, so a
    // scalar compare here would dominate the operation it is checking.
    //
    // The bound is tested as v > max || v < -max with max = 10^p - 1 rather than
    // via Vector.Abs, whose result for int.MinValue is not the magnitude.
    // ================================================================

    private static bool AnyOutOfRange(ReadOnlySpan<int> values, DecimalType type)
    {
        if (!TryGetBound(type, out int bound)) return false;
        int max = bound - 1;
        int i = 0;

#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && values.Length >= Vector<int>.Count)
        {
            ReadOnlySpan<Vector<int>> vv = MemoryMarshal.Cast<int, Vector<int>>(values);
            Vector<int> hi = new Vector<int>(max);
            Vector<int> lo = new Vector<int>(-max);
            Vector<int> acc = Vector<int>.Zero;
            for (int k = 0; k < vv.Length; k++)
            {
                Vector<int> v = vv[k];
                acc |= Vector.GreaterThan(v, hi) | Vector.LessThan(v, lo);
            }
            if (acc != Vector<int>.Zero) return true;
            i = vv.Length * Vector<int>.Count;
        }
#endif

        for (; i < values.Length; i++)
        {
            int v = values[i];
            if (v > max || v < -max) return true;
        }
        return false;
    }

    private static bool AnyOutOfRange(ReadOnlySpan<long> values, DecimalType type)
    {
        if (!TryGetBound(type, out long bound)) return false;
        long max = bound - 1;
        int i = 0;

#if NET5_0_OR_GREATER
        if (Vector.IsHardwareAccelerated && values.Length >= Vector<long>.Count)
        {
            ReadOnlySpan<Vector<long>> vv = MemoryMarshal.Cast<long, Vector<long>>(values);
            Vector<long> hi = new Vector<long>(max);
            Vector<long> lo = new Vector<long>(-max);
            Vector<long> acc = Vector<long>.Zero;
            for (int k = 0; k < vv.Length; k++)
            {
                Vector<long> v = vv[k];
                acc |= Vector.GreaterThan(v, hi) | Vector.LessThan(v, lo);
            }
            if (acc != Vector<long>.Zero) return true;
            i = vv.Length * Vector<long>.Count;
        }
#endif

        for (; i < values.Length; i++)
        {
            long v = values[i];
            if (v > max || v < -max) return true;
        }
        return false;
    }

    /// <summary>Throws if any element does not fit the type's precision.</summary>
    /// <exception cref="OverflowException">Some element needs more than <c>type.Precision</c> digits.</exception>
    public static void Validate(ReadOnlySpan<Int128> values, DecimalType type)
    {
        if (IndexOfOutOfRange(values, type) >= 0) ThrowOverflow(type);
    }

    /// <summary>Throws if any element does not fit the type's precision.</summary>
    /// <exception cref="OverflowException">Some element needs more than <c>type.Precision</c> digits.</exception>
    public static void Validate(ReadOnlySpan<Int256> values, DecimalType type)
    {
        if (IndexOfOutOfRange(values, type) >= 0) ThrowOverflow(type);
    }

    // ================================================================
    // Batch — locate the first offender
    // ================================================================

    /// <summary>Index of the first element outside the type's precision, or -1.</summary>
    public static int IndexOfOutOfRange(ReadOnlySpan<int> values, DecimalType type)
    {
        if (!TryGetBound(type, out int bound)) return -1;
        for (int i = 0; i < values.Length; i++)
        {
            int v = values[i];
            if (v <= -bound || v >= bound) return i;
        }
        return -1;
    }

    /// <summary>Index of the first element outside the type's precision, or -1.</summary>
    public static int IndexOfOutOfRange(ReadOnlySpan<long> values, DecimalType type)
    {
        if (!TryGetBound(type, out long bound)) return -1;
        for (int i = 0; i < values.Length; i++)
        {
            long v = values[i];
            if (v <= -bound || v >= bound) return i;
        }
        return -1;
    }

    /// <summary>Index of the first element outside the type's precision, or -1.</summary>
    public static int IndexOfOutOfRange(ReadOnlySpan<Int128> values, DecimalType type)
    {
        if (!TryGetBound(type, out Int128 bound)) return -1;
        Int128 lower = -bound;
        for (int i = 0; i < values.Length; i++)
        {
            Int128 v = values[i];
            if (v <= lower || v >= bound) return i;
        }
        return -1;
    }

    /// <summary>Index of the first element outside the type's precision, or -1.</summary>
    public static int IndexOfOutOfRange(ReadOnlySpan<Int256> values, DecimalType type)
    {
        if (!TryGetBound(type, out Int256 bound)) return -1;
        Int256 lower = -bound;
        for (int i = 0; i < values.Length; i++)
        {
            Int256 v = values[i];
            if (v <= lower || v >= bound) return i;
        }
        return -1;
    }

    // ================================================================
    // Batch — flag every offender
    //
    // Bit i of the mask corresponds to values[i]: word i >> 6, bit i & 63. The
    // words covering the span are fully written, so the caller does not have to
    // clear the buffer first; bits past the end of the span are left zero.
    // ================================================================

    /// <summary>Number of 64-bit words <c>WriteOutOfRangeMask</c> needs for a span of this length.</summary>
    public static int MaskWordCount(int length) => (length + 63) >> 6;

    /// <summary>
    /// Flags every element outside the type's precision in <paramref name="mask"/>
    /// and returns how many there were.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="mask"/> is shorter than <see cref="MaskWordCount"/>.</exception>
    public static int WriteOutOfRangeMask(ReadOnlySpan<int> values, DecimalType type, Span<ulong> mask)
    {
        Span<ulong> words = PrepareMask(values.Length, mask);
        if (!TryGetBound(type, out int bound)) return 0;

        int count = 0;
        for (int i = 0; i < values.Length; i++)
        {
            int v = values[i];
            if (v > -bound && v < bound) continue;
            words[i >> 6] |= 1UL << (i & 63);
            count++;
        }
        return count;
    }

    /// <summary>
    /// Flags every element outside the type's precision in <paramref name="mask"/>
    /// and returns how many there were.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="mask"/> is shorter than <see cref="MaskWordCount"/>.</exception>
    public static int WriteOutOfRangeMask(ReadOnlySpan<long> values, DecimalType type, Span<ulong> mask)
    {
        Span<ulong> words = PrepareMask(values.Length, mask);
        if (!TryGetBound(type, out long bound)) return 0;

        int count = 0;
        for (int i = 0; i < values.Length; i++)
        {
            long v = values[i];
            if (v > -bound && v < bound) continue;
            words[i >> 6] |= 1UL << (i & 63);
            count++;
        }
        return count;
    }

    /// <summary>
    /// Flags every element outside the type's precision in <paramref name="mask"/>
    /// and returns how many there were.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="mask"/> is shorter than <see cref="MaskWordCount"/>.</exception>
    public static int WriteOutOfRangeMask(ReadOnlySpan<Int128> values, DecimalType type, Span<ulong> mask)
    {
        Span<ulong> words = PrepareMask(values.Length, mask);
        if (!TryGetBound(type, out Int128 bound)) return 0;
        Int128 lower = -bound;

        int count = 0;
        for (int i = 0; i < values.Length; i++)
        {
            Int128 v = values[i];
            if (v > lower && v < bound) continue;
            words[i >> 6] |= 1UL << (i & 63);
            count++;
        }
        return count;
    }

    /// <summary>
    /// Flags every element outside the type's precision in <paramref name="mask"/>
    /// and returns how many there were.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="mask"/> is shorter than <see cref="MaskWordCount"/>.</exception>
    public static int WriteOutOfRangeMask(ReadOnlySpan<Int256> values, DecimalType type, Span<ulong> mask)
    {
        Span<ulong> words = PrepareMask(values.Length, mask);
        if (!TryGetBound(type, out Int256 bound)) return 0;
        Int256 lower = -bound;

        int count = 0;
        for (int i = 0; i < values.Length; i++)
        {
            Int256 v = values[i];
            if (v > lower && v < bound) continue;
            words[i >> 6] |= 1UL << (i & 63);
            count++;
        }
        return count;
    }

    private static Span<ulong> PrepareMask(int length, Span<ulong> mask)
    {
        int words = MaskWordCount(length);
        if (mask.Length < words)
            throw new ArgumentException(
                $"Mask must be at least {words} words for {length} values.", nameof(mask));

        Span<ulong> used = mask.Slice(0, words);
        used.Clear();
        return used;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOverflow(DecimalType type) =>
        throw new OverflowException($"The result does not fit {type}.");
}
