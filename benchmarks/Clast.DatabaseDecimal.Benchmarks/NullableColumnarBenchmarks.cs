// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using BenchmarkDotNet.Attributes;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Benchmarks;

/// <summary>
/// The folded out-of-range mask against the two-pass shape it replaces.
/// </summary>
/// <remarks>
/// Both benchmarks produce the same deliverable — results plus a per-row
/// out-of-range mask — so the difference is the second walk over the output and
/// nothing else. The operands are bounded well inside NUMERIC(18,2), so no row
/// actually trips: that is the case the mask is optimised for, since overflow is
/// rare by design. A column where most rows overflow pays for the per-lane
/// extract and would narrow the gap.
/// </remarks>
[MemoryDiagnoser]
public class FoldedMaskBenchmarks
{
    // 4096 is a typical record-batch; 65536 spills out of L2.
    [Params(4096, 65536)]
    public int N;

    private long[] _left = null!;
    private long[] _right = null!;
    private long[] _result = null!;
    private ulong[] _mask = null!;

    private static readonly DecimalType T64S2 = DecimalType.Numeric(18, 2);

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _left = new long[N];
        _right = new long[N];
        _result = new long[N];
        _mask = new ulong[DecimalRange.MaskWordCount(N)];

        for (int i = 0; i < N; i++)
        {
            _left[i] = rng.NextInt64(-1_000_000_000L, 1_000_000_000L);
            _right[i] = rng.NextInt64(-1_000_000_000L, 1_000_000_000L);
        }
    }

    /// <summary>Kernel, then a separate pass to find out which rows overflowed.</summary>
    [Benchmark(Baseline = true)]
    public int Add_Int64_TwoPass()
    {
        SpanAddKernel.Add(_left, T64S2, _right, T64S2, _result, T64S2,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);
        return DecimalRange.WriteOutOfRangeMask(_result, T64S2, _mask);
    }

    /// <summary>One pass, mask folded into the arithmetic.</summary>
    [Benchmark]
    public int Add_Int64_FoldedMask() =>
        SpanAddKernel.Add(_left, T64S2, _right, T64S2, _result, T64S2, _mask);

    [Benchmark]
    public int Subtract_Int64_FoldedMask() =>
        SpanAddKernel.Subtract(_left, T64S2, _right, T64S2, _result, T64S2, _mask);
}

/// <summary>
/// Validity-aware division against the workaround it removes: copy the divisor
/// column with the null slots patched to something non-zero, then divide densely
/// and mask afterwards.
/// </summary>
/// <remarks>
/// The dense path cannot be run over the raw divisor at all — the null slots
/// hold zero, so it would throw DivideByZeroException — which is why the patch
/// pass is part of the baseline rather than an optimisation the baseline skips.
/// </remarks>
[MemoryDiagnoser]
public class ValidityDivideBenchmarks
{
    private const int N = 65536;

    /// <summary>Fraction of rows null in one operand or the other.</summary>
    [Params(0.0, 0.10, 0.50)]
    public double NullFraction;

    private Int128[] _left = null!;
    private Int128[] _divisor = null!;
    private Int128[] _patched = null!;
    private Int128[] _result = null!;
    private ulong[] _validity = null!;
    private ulong[] _mask = null!;

    private static readonly DecimalType T128S2 = DecimalType.Numeric(38, 2);
    private static readonly DecimalType T128S6 = DecimalType.Numeric(38, 6);

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _left = new Int128[N];
        _divisor = new Int128[N];
        _patched = new Int128[N];
        _result = new Int128[N];

        int words = DecimalRange.MaskWordCount(N);
        _validity = new ulong[words];
        _mask = new ulong[words];

        for (int i = 0; i < N; i++)
        {
            _left[i] = rng.NextInt64();
            _divisor[i] = rng.NextInt64(1, 1_000_000);
        }

        for (int i = 0; i < N; i++)
            if (rng.NextDouble() >= NullFraction)
                _validity[i >> 6] |= 1UL << (i & 63);

        // Zero under every null slot: what a builder leaves there, and what makes
        // a dense pass throw unless the column is patched first.
        for (int i = 0; i < N; i++)
            if ((_validity[i >> 6] & (1UL << (i & 63))) == 0)
                _divisor[i] = Int128.Zero;
    }

    [Benchmark(Baseline = true)]
    public int Divide_PatchThenDense()
    {
        for (int i = 0; i < N; i++)
            _patched[i] = (_validity[i >> 6] & (1UL << (i & 63))) != 0 ? _divisor[i] : Int128.One;

        SpanDivideKernel.Divide(_left, T128S2, _patched, T128S2, _result, T128S6,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);

        int count = DecimalRange.WriteOutOfRangeMask(_result, T128S6, _mask);
        for (int w = 0; w < _mask.Length; w++) _mask[w] &= _validity[w];
        return count;
    }

    [Benchmark]
    public int Divide_Validity() =>
        SpanDivideKernel.Divide(_left, T128S2, _divisor, T128S2, _result, T128S6, _validity, _mask);
}
