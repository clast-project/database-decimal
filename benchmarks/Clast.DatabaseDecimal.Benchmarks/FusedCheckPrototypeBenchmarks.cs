// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

// EXPERIMENT BRANCH ONLY — prototypes for issue #11, not library code.
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Benchmarks;

/// <summary>
/// Prototype shapes for the 128/256-bit result-precision check (#11), for
/// comparing across architectures. Not library code: each prototype is an
/// out-of-line same-scale add written here, so the shapes can be compared
/// without changing the kernels.
/// </summary>
/// <remarks>
/// P*_Fused: full-width compare folded into the add loop.
/// P*_FusedFlag / FlagOff: the same, gated by a loop-invariant bool, checked and unchecked.
/// T*_FusedTop: only the top 64-bit limb compared in the loop (a real version
///   would rescan exactly when it trips).
/// V*_IgnorePlusScreen: the Ignore kernel, then a separate Vector&lt;long&gt; pass
///   over the top limbs.
/// S128_IgnorePlusBias: the Ignore kernel, then a scalar biased unsigned compare.
/// A*_Throw / A*_Ignore / A*_Masked: the real kernels, for reference.
/// </remarks>
public class FusedCheckPrototypeBenchmarks
{
    [Params(65536)] public int N;

    private Int128[] _l128 = null!, _r128 = null!, _o128 = null!;
    private Int256[] _l256 = null!, _r256 = null!, _o256 = null!;
    private ulong[] _mask = null!;
    private Int128 _lo128, _hi128;
    private Int256 _lo256, _hi256;
    private bool _check = true;

    private static readonly DecimalType T128 = DecimalType.Numeric(38, 2);
    private static readonly DecimalType T256 = DecimalType.Numeric(76, 2);

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _l128 = new Int128[N]; _r128 = new Int128[N]; _o128 = new Int128[N];
        _l256 = new Int256[N]; _r256 = new Int256[N]; _o256 = new Int256[N];
        _mask = new ulong[(N + 63) / 64];
        for (int i = 0; i < N; i++)
        {
            _l128[i] = rng.NextInt64(); _r128[i] = rng.NextInt64(1, long.MaxValue);
            _l256[i] = rng.NextInt64(); _r256[i] = rng.NextInt64();
        }
        Int128 p = 1; for (int i = 0; i < 38; i++) p *= 10;
        _hi128 = p - 1; _lo128 = -_hi128;
        Int256 q = Int256.One; Int256 ten = 10;
        for (int i = 0; i < 76; i++) q *= ten;
        _hi256 = q - Int256.One; _lo256 = -_hi256;
    }

    // --- current library paths ---
    [Benchmark] public void A128_Throw() => SpanAddKernel.Add(_l128, T128, _r128, T128, _o128, T128);
    [Benchmark] public void A128_Ignore() => SpanAddKernel.Add(_l128, T128, _r128, T128, _o128, T128, DecimalRounding.HalfEven, DecimalOverflow.Ignore);
    [Benchmark] public int A128_Masked() => SpanAddKernel.Add(_l128, T128, _r128, T128, _o128, T128, _mask);
    [Benchmark] public void A256_Throw() => SpanAddKernel.Add(_l256, T256, _r256, T256, _o256, T256);
    [Benchmark] public void A256_Ignore() => SpanAddKernel.Add(_l256, T256, _r256, T256, _o256, T256, DecimalRounding.HalfEven, DecimalOverflow.Ignore);
    [Benchmark] public int A256_Masked() => SpanAddKernel.Add(_l256, T256, _r256, T256, _o256, T256, _mask);

    // --- prototypes, out of line like the real kernels ---
    [Benchmark] public bool P128_Fused() => Fused128(_l128, _r128, _o128, _lo128, _hi128);
    [Benchmark] public bool P128_FusedFlag() => FusedFlag128(_l128, _r128, _o128, _lo128, _hi128, _check);
    [Benchmark] public bool P128_FusedFlagOff() => FusedFlag128(_l128, _r128, _o128, _lo128, _hi128, false);
    [Benchmark] public bool P256_Fused() => Fused256(_l256, _r256, _o256, _lo256, _hi256);
    [Benchmark] public bool P256_FusedFlag() => FusedFlag256(_l256, _r256, _o256, _lo256, _hi256, _check);
    [Benchmark] public bool P256_FusedFlagOff() => FusedFlag256(_l256, _r256, _o256, _lo256, _hi256, false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Fused128(ReadOnlySpan<Int128> l, ReadOnlySpan<Int128> r, Span<Int128> o, Int128 lo, Int128 hi)
    {
        bool bad = false;
        for (int i = 0; i < l.Length; i++)
        {
            Int128 v = checked(l[i] + r[i]);
            o[i] = v;
            bad |= v < lo | v > hi;
        }
        return bad;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool FusedFlag128(ReadOnlySpan<Int128> l, ReadOnlySpan<Int128> r, Span<Int128> o, Int128 lo, Int128 hi, bool check)
    {
        bool bad = false;
        for (int i = 0; i < l.Length; i++)
        {
            Int128 v = checked(l[i] + r[i]);
            o[i] = v;
            if (check) bad |= v < lo | v > hi;
        }
        return bad;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Fused256(ReadOnlySpan<Int256> l, ReadOnlySpan<Int256> r, Span<Int256> o, Int256 lo, Int256 hi)
    {
        bool bad = false;
        for (int i = 0; i < l.Length; i++)
        {
            Int256 v = checked(l[i] + r[i]);
            o[i] = v;
            bad |= v < lo | v > hi;
        }
        return bad;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool FusedFlag256(ReadOnlySpan<Int256> l, ReadOnlySpan<Int256> r, Span<Int256> o, Int256 lo, Int256 hi, bool check)
    {
        bool bad = false;
        for (int i = 0; i < l.Length; i++)
        {
            Int256 v = checked(l[i] + r[i]);
            o[i] = v;
            if (check) bad |= v < lo | v > hi;
        }
        return bad;
    }

    // --- cheaper separate pass: vectorised top-limb screen, exact rescan only if it trips ---
    [Benchmark] public bool V128_IgnorePlusScreen()
    {
        SpanAddKernel.Add(_l128, T128, _r128, T128, _o128, T128, DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        return Screen(MemoryMarshal.Cast<Int128, long>(_o128), 2, (long)(ulong)(_hi128 >> 64));
    }
    [Benchmark] public bool V256_IgnorePlusScreen()
    {
        SpanAddKernel.Add(_l256, T256, _r256, T256, _o256, T256, DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        return Screen(MemoryMarshal.Cast<Int256, long>(_o256), 4, (long)(_hi256 >> 192));
    }
    [Benchmark] public bool S128_IgnorePlusBias()
    {
        SpanAddKernel.Add(_l128, T128, _r128, T128, _o128, T128, DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        return Bias128(_o128, _hi128);
    }

    // limbs: little-endian words per element; the top limb of each element is
    // at word index limbs-1 within it. In range for certain when -H < top < H,
    // H = top limb of the upper bound. When a vector is narrower than an
    // element (Int256 on 128-bit NEON), the top limb lands in every other
    // vector, so the threshold pattern repeats over `period` vectors.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Screen(ReadOnlySpan<long> words, int limbs, long h)
    {
        int count = Vector<long>.Count;
        int period = Math.Max(1, limbs / count);
        var hiV = new Vector<long>[period];
        var loV = new Vector<long>[period];
        Span<long> hiT = stackalloc long[count];
        Span<long> loT = stackalloc long[count];
        for (int p = 0; p < period; p++)
        {
            for (int j = 0; j < count; j++)
            {
                bool top = ((p * count + j) % limbs) == limbs - 1;
                hiT[j] = top ? h - 1 : long.MaxValue;
                loT[j] = top ? -h : long.MinValue;
            }
            hiV[p] = new Vector<long>(hiT);
            loV[p] = new Vector<long>(loT);
        }

        var vv = MemoryMarshal.Cast<long, Vector<long>>(words);
        var acc = Vector<long>.Zero;
        int k = 0;
        if (period == 1)
        {
            Vector<long> hi = hiV[0], lo = loV[0];
            for (; k < vv.Length; k++)
                acc |= Vector.GreaterThan(vv[k], hi) | Vector.LessThan(vv[k], lo);
        }
        else
        {
            Vector<long> hi0 = hiV[0], lo0 = loV[0], hi1 = hiV[1], lo1 = loV[1];
            for (; k + 1 < vv.Length; k += 2)
                acc |= Vector.GreaterThan(vv[k], hi0) | Vector.LessThan(vv[k], lo0)
                     | Vector.GreaterThan(vv[k + 1], hi1) | Vector.LessThan(vv[k + 1], lo1);
        }
        bool trip = acc != Vector<long>.Zero;
        for (int i = k * count; i < words.Length; i++)
            if (i % limbs == limbs - 1 && (words[i] >= h || words[i] < -h)) trip = true;
        return trip; // a real implementation would rescan exactly here
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bias128(ReadOnlySpan<Int128> v, Int128 max)
    {
        UInt128 span = (UInt128)max * 2; UInt128 b = (UInt128)max; bool bad = false;
        for (int i = 0; i < v.Length; i++) bad |= (UInt128)v[i] + b > span;
        return bad;
    }

    [Benchmark] public bool T128_FusedTop() => FusedTop128(_l128, _r128, _o128, (long)(ulong)(_hi128 >> 64));
    [Benchmark] public bool T256_FusedTop() => FusedTop256(_l256, _r256, _o256, (long)(_hi256 >> 192));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool FusedTop128(ReadOnlySpan<Int128> l, ReadOnlySpan<Int128> r, Span<Int128> o, long h)
    {
        bool bad = false;
        for (int i = 0; i < l.Length; i++)
        {
            Int128 v = checked(l[i] + r[i]);
            o[i] = v;
            long t = (long)(v >> 64);
            bad |= (t >= h) | (t < -h);
        }
        return bad; // real code: exact rescan only when this trips
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool FusedTop256(ReadOnlySpan<Int256> l, ReadOnlySpan<Int256> r, Span<Int256> o, long h)
    {
        bool bad = false;
        for (int i = 0; i < l.Length; i++)
        {
            Int256 v = checked(l[i] + r[i]);
            o[i] = v;
            long t = (long)(v >> 192);
            bad |= (t >= h) | (t < -h);
        }
        return bad;
    }
}
