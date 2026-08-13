// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using BenchmarkDotNet.Attributes;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Benchmarks;

[MemoryDiagnoser]
public class SpanAddBenchmarks
{
    // 1024 fits in L1 for narrow mantissas; 65536 spills out of L1/L2 and exposes memory bandwidth.
    [Params(1024, 65536)]
    public int N;

    private int[] _i32Left = null!;
    private int[] _i32Right = null!;
    private int[] _i32Result = null!;

    private long[] _i64Left = null!;
    private long[] _i64Right = null!;
    private long[] _i64Result = null!;

    private Int128[] _i128Left = null!;
    private Int128[] _i128Right = null!;
    private Int128[] _i128Result = null!;

    private Int256[] _i256Left = null!;
    private Int256[] _i256Right = null!;
    private Int256[] _i256Result = null!;

    private static readonly DecimalType T32S2 = DecimalType.Numeric(precision: 9, scale: 2);
    private static readonly DecimalType T32S4 = DecimalType.Numeric(precision: 9, scale: 4);
    private static readonly DecimalType T64S2 = DecimalType.Numeric(precision: 18, scale: 2);
    private static readonly DecimalType T128S2 = DecimalType.Numeric(precision: 38, scale: 2);
    private static readonly DecimalType T256S2 = DecimalType.Numeric(precision: 76, scale: 2);
    private static readonly DecimalType T256S4 = DecimalType.Numeric(precision: 76, scale: 4);

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);

        _i32Left = new int[N]; _i32Right = new int[N]; _i32Result = new int[N];
        _i64Left = new long[N]; _i64Right = new long[N]; _i64Result = new long[N];
        _i128Left = new Int128[N]; _i128Right = new Int128[N]; _i128Result = new Int128[N];
        _i256Left = new Int256[N]; _i256Right = new Int256[N]; _i256Result = new Int256[N];

        for (int i = 0; i < N; i++)
        {
            // Bounded so rescale-by-100 stays in range without throwing.
            _i32Left[i] = rng.Next(-10_000, 10_000);
            _i32Right[i] = rng.Next(-10_000, 10_000);

            _i64Left[i] = rng.NextInt64(-1_000_000_000L, 1_000_000_000L);
            _i64Right[i] = rng.NextInt64(-1_000_000_000L, 1_000_000_000L);

            _i128Left[i] = rng.NextInt64();
            _i128Right[i] = rng.NextInt64();

            _i256Left[i] = rng.NextInt64();
            _i256Right[i] = rng.NextInt64();
        }
    }

    [Benchmark]
    public void Add_Int32_SameScale() =>
        SpanAddKernel.Add(_i32Left, T32S2, _i32Right, T32S2, _i32Result, T32S2);

    // Same arithmetic without the result-precision pass, which is what a caller
    // that has already proven the range — or one that scans for overflowing rows
    // itself via DecimalRange — pays instead.
    [Benchmark]
    public void Add_Int32_SameScale_IgnoreOverflow() =>
        SpanAddKernel.Add(_i32Left, T32S2, _i32Right, T32S2, _i32Result, T32S2,
            DecimalRounding.HalfEven, DecimalOverflow.Ignore);

    // Mixed scales force per-element rescale-by-power-of-10.
    [Benchmark]
    public void Add_Int32_Rescale() =>
        SpanAddKernel.Add(_i32Left, T32S2, _i32Right, T32S4, _i32Result, T32S4);

    [Benchmark]
    public void Add_Int64_SameScale() =>
        SpanAddKernel.Add(_i64Left, T64S2, _i64Right, T64S2, _i64Result, T64S2);

    [Benchmark]
    public void Add_Int128_SameScale() =>
        SpanAddKernel.Add(_i128Left, T128S2, _i128Right, T128S2, _i128Result, T128S2);

    [Benchmark]
    public void Add_Int256_SameScale() =>
        SpanAddKernel.Add(_i256Left, T256S2, _i256Right, T256S2, _i256Result, T256S2);

    // Scaling up multiplies each mantissa by a power of ten under checked
    // arithmetic, so this is the path that pays for Int256's checked multiply.
    [Benchmark]
    public void Add_Int256_RescaleUp() =>
        SpanAddKernel.Add(_i256Left, T256S2, _i256Right, T256S4, _i256Result, T256S4);

    [Benchmark]
    public void AddWiden_Int32_To_Int64_SameScale() =>
        SpanAddKernel.AddWiden(_i32Left, T32S2, _i32Right, T32S2, _i64Result, T64S2);

    // Inline unchecked control: same shape as Add_Int32_SameScale but without `checked`.
    // If this vectorizes and Add_Int32_SameScale doesn't, `checked` is the blocker.
    [Benchmark]
    public void Add_Int32_SameScale_UncheckedInline()
    {
        var l = _i32Left;
        var r = _i32Right;
        var o = _i32Result;
        for (int i = 0; i < l.Length; i++)
            o[i] = l[i] + r[i];
    }

    // Broadcast-scalar variants — exercise the per-element-scalar SIMD paths
    // (a Vector<T> is built from the scalar once before the loop and reused).
    private const int Scalar32 = 12_345;
    private const long Scalar64 = 1_234_567_890L;

    [Benchmark]
    public void Add_Int32_Broadcast() =>
        SpanAddKernel.Add(_i32Left, T32S2, Scalar32, T32S2, _i32Result, T32S2);

    [Benchmark]
    public void Add_Int64_Broadcast() =>
        SpanAddKernel.Add(_i64Left, T64S2, Scalar64, T64S2, _i64Result, T64S2);

    [Benchmark]
    public void Subtract_Int32_BroadcastColumnScalar() =>
        SpanAddKernel.Subtract(_i32Left, T32S2, Scalar32, T32S2, _i32Result, T32S2);

    [Benchmark]
    public void Subtract_Int32_BroadcastScalarColumn() =>
        SpanAddKernel.Subtract(Scalar32, T32S2, _i32Right, T32S2, _i32Result, T32S2);
}
