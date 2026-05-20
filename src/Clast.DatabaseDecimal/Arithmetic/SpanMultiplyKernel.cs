// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Arithmetic;

/// <summary>
/// Span-based batch multiplication on raw mantissa arrays.
/// Multiplication naturally widens (32→64, 64→128, 128→256).
/// The raw product scale (s1+s2) is adjusted to the result scale once
/// per call, not per element.
/// The result span may safely overlap with either input span (when types match).
/// </summary>
public static class SpanMultiplyKernel
{
    // ================================================================
    // Multiply — column * column, widening
    // ================================================================

    /// <summary>32×32 → 64 bit widening multiply.</summary>
    public static void Multiply(
        ReadOnlySpan<int> left, DecimalType leftType,
        ReadOnlySpan<int> right, DecimalType rightType,
        Span<long> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            MultiplyWideningNoRescale32(left, right, result);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale64((long)left[i] * right[i], scaleDelta);
        }
    }

    /// <summary>64×64 → 128 bit widening multiply.</summary>
    public static void Multiply(
        ReadOnlySpan<long> left, DecimalType leftType,
        ReadOnlySpan<long> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = (Int128)left[i] * right[i];
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale128((Int128)left[i] * right[i], scaleDelta);
        }
    }

    /// <summary>128×128 → 256 bit widening multiply via Int256.BigMul.</summary>
    public static void MultiplyWiden(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Int256.BigMul(left[i], right[i]);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale256(Int256.BigMul(left[i], right[i]), scaleDelta);
        }
    }

    // ================================================================
    // Multiply — column * column, same width (128-bit and 256-bit)
    // For 128-bit, pre-reduces scale of one operand to stay within range.
    // ================================================================

    /// <summary>128×128 → 128 bit multiply with pre-scale-reduction.</summary>
    public static void Multiply(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        ReadOnlySpan<Int128> right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleReduction = rawScale - resultType.Scale;
        bool reduceLeft = leftType.Scale >= rightType.Scale;

        if (scaleReduction <= 0)
        {
            // No reduction needed (or need to scale up the product)
            int scaleDelta = resultType.Scale - rawScale;
            for (int i = 0; i < left.Length; i++)
            {
                Int128 product = checked(left[i] * right[i]);
                result[i] = scaleDelta != 0 ? Rescale128(product, scaleDelta) : product;
            }
        }
        else if (reduceLeft)
        {
            int newLeftScale = leftType.Scale - scaleReduction;
            for (int i = 0; i < left.Length; i++)
            {
                Int128 l = ScaleHelper.Rescale128(left[i], leftType.Scale, newLeftScale);
                result[i] = checked(l * right[i]);
            }
        }
        else
        {
            int newRightScale = rightType.Scale - scaleReduction;
            for (int i = 0; i < left.Length; i++)
            {
                Int128 r = ScaleHelper.Rescale128(right[i], rightType.Scale, newRightScale);
                result[i] = checked(left[i] * r);
            }
        }
    }

    /// <summary>256×256 → 256 bit multiply with pre-scale-reduction.</summary>
    public static void Multiply(
        ReadOnlySpan<Int256> left, DecimalType leftType,
        ReadOnlySpan<Int256> right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, right.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleReduction = rawScale - resultType.Scale;
        bool reduceLeft = leftType.Scale >= rightType.Scale;

        if (scaleReduction <= 0)
        {
            int scaleDelta = resultType.Scale - rawScale;
            for (int i = 0; i < left.Length; i++)
            {
                Int256 product = left[i] * right[i];
                result[i] = scaleDelta != 0 ? Rescale256(product, scaleDelta) : product;
            }
        }
        else if (reduceLeft)
        {
            int newLeftScale = leftType.Scale - scaleReduction;
            for (int i = 0; i < left.Length; i++)
            {
                Int256 l = ScaleHelper.Rescale256(left[i], leftType.Scale, newLeftScale);
                result[i] = l * right[i];
            }
        }
        else
        {
            int newRightScale = rightType.Scale - scaleReduction;
            for (int i = 0; i < left.Length; i++)
            {
                Int256 r = ScaleHelper.Rescale256(right[i], rightType.Scale, newRightScale);
                result[i] = left[i] * r;
            }
        }
    }

    // ================================================================
    // Multiply — column * scalar (broadcast)
    // ================================================================

    public static void Multiply(
        ReadOnlySpan<int> left, DecimalType leftType,
        int right, DecimalType rightType,
        Span<long> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        long wideRight = right;
        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            MultiplyBroadcastWideningNoRescale32(left, right, result);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale64((long)left[i] * wideRight, scaleDelta);
        }
    }

    public static void Multiply(
        ReadOnlySpan<long> left, DecimalType leftType,
        long right, DecimalType rightType,
        Span<Int128> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        Int128 wideRight = right;
        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = (Int128)left[i] * wideRight;
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale128((Int128)left[i] * wideRight, scaleDelta);
        }
    }

    public static void Multiply(
        ReadOnlySpan<Int128> left, DecimalType leftType,
        Int128 right, DecimalType rightType,
        Span<Int256> result, DecimalType resultType)
    {
        ValidateLengths(left.Length, result.Length);

        int rawScale = leftType.Scale + rightType.Scale;
        int scaleDelta = resultType.Scale - rawScale;

        if (scaleDelta == 0)
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Int256.BigMul(left[i], right);
        }
        else
        {
            for (int i = 0; i < left.Length; i++)
                result[i] = Rescale256(Int256.BigMul(left[i], right), scaleDelta);
        }
    }

    // ================================================================
    // Helpers
    // ================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Rescale64(long value, int delta)
    {
        if (delta > 0) return checked(value * PowersOf10.Int64[delta]);
        return ScaleHelper.DivideRoundHalfEven(value, PowersOf10.Int64[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int128 Rescale128(Int128 value, int delta)
    {
        if (delta > 0) return checked(value * PowersOf10.Int128[delta]);
        return ScaleHelper.DivideRoundHalfEven(value, PowersOf10.Int128[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int256 Rescale256(Int256 value, int delta)
    {
        if (delta > 0) return checked(value * PowersOf10.Int256[delta]);
        return ScaleHelper.DivideRoundHalfEven(value, PowersOf10.Int256[-delta]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateLengths(int leftLen, int rightLen, int resultLen)
    {
        if (leftLen != rightLen)
            throw new ArgumentException("Input spans must have the same length.");
        if (resultLen < leftLen)
            throw new ArgumentException("Result span must be at least as long as input spans.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateLengths(int inputLen, int resultLen)
    {
        if (resultLen < inputLen)
            throw new ArgumentException("Result span must be at least as long as input span.");
    }

    // ================================================================
    // SIMD helper for 32x32 -> 64 widening multiply with no rescale.
    // Uses Avx2 vpmovsxdq (ConvertToVector256Int64) to sign-extend
    // four int32 to four int64, then vpmuldq (Avx2.Multiply on the
    // Int32 view, which reads the low 32 bits of each 64-bit lane)
    // to produce four int64 products per iteration. No overflow is
    // possible: (long)int * int always fits in long.
    // ================================================================

    private static void MultiplyWideningNoRescale32(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<long> result)
    {
        int i = 0;
#if NET8_0_OR_GREATER
        if (Avx2.IsSupported && left.Length >= 4)
        {
            const int chunkSize = 4;
            int chunks = left.Length / chunkSize;
            ref int leftRef = ref MemoryMarshal.GetReference(left);
            ref int rightRef = ref MemoryMarshal.GetReference(right);
            ref long resultRef = ref MemoryMarshal.GetReference(result);
            for (int k = 0; k < chunks; k++)
            {
                nuint off = (nuint)(k * chunkSize);
                Vector128<int> a128 = Vector128.LoadUnsafe(ref leftRef, off);
                Vector128<int> b128 = Vector128.LoadUnsafe(ref rightRef, off);
                Vector256<long> a256 = Avx2.ConvertToVector256Int64(a128);
                Vector256<long> b256 = Avx2.ConvertToVector256Int64(b128);
                Vector256<long> product = Avx2.Multiply(a256.AsInt32(), b256.AsInt32());
                product.StoreUnsafe(ref resultRef, off);
            }
            i = chunks * chunkSize;
        }
#endif
        for (; i < left.Length; i++)
            result[i] = (long)left[i] * right[i];
    }

    private static void MultiplyBroadcastWideningNoRescale32(ReadOnlySpan<int> left, int right, Span<long> result)
    {
        int i = 0;
#if NET8_0_OR_GREATER
        if (Avx2.IsSupported && left.Length >= 4)
        {
            const int chunkSize = 4;
            int chunks = left.Length / chunkSize;
            ref int leftRef = ref MemoryMarshal.GetReference(left);
            ref long resultRef = ref MemoryMarshal.GetReference(result);
            // Broadcast the scalar into the int32 lanes that vpmuldq reads
            // (lanes 0, 2, 4, 6 of a Vector256<int>) by creating a Vector256<long>
            // with all 4 lanes set to (long)right and reinterpreting as int32.
            Vector256<int> bv = Vector256.Create((long)right).AsInt32();
            for (int k = 0; k < chunks; k++)
            {
                nuint off = (nuint)(k * chunkSize);
                Vector128<int> a128 = Vector128.LoadUnsafe(ref leftRef, off);
                Vector256<long> a256 = Avx2.ConvertToVector256Int64(a128);
                Vector256<long> product = Avx2.Multiply(a256.AsInt32(), bv);
                product.StoreUnsafe(ref resultRef, off);
            }
            i = chunks * chunkSize;
        }
#endif
        for (; i < left.Length; i++)
            result[i] = (long)left[i] * right;
    }
}
