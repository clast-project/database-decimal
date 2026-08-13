// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using BenchmarkDotNet.Attributes;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Benchmarks;

/// <summary>
/// A fixed spread of kernel paths, one per shape, for comparing a working tree
/// against a released version.
/// </summary>
/// <remarks>
/// Every benchmark here uses default optional arguments only, so the file
/// compiles unchanged against older revisions:
/// <code>
/// git checkout v0.2.0 -- src/
/// dotnet run -c Release --project benchmarks/... -- --filter '*ReleaseBaseline*' --job medium
/// git checkout HEAD -- src/
/// </code>
/// That property is the point of the file and is worth preserving — measuring
/// each change against the state immediately before it, as this project did
/// through 0.3.0, hides regressions that accumulate across several changes. A
/// 2.7x regression in 128-bit division survived three releases-in-progress that
/// way, because the only divide benchmark covered the 32-bit path.
/// <para>
/// Use --job medium: short runs on this workload have shown 20% spreads, enough
/// to invent or hide a regression of the size worth acting on.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class ReleaseBaselineBenchmarks
{
    [Params(65536)]
    public int N;

    private int[] _i32L = null!, _i32R = null!, _i32Out = null!;
    private long[] _i64L = null!, _i64R = null!, _i64Out = null!;
    private Int128[] _i128L = null!, _i128R = null!, _i128Out = null!;
    private Int256[] _i256L = null!, _i256R = null!, _i256Out = null!;

    private static readonly DecimalType T32S2 = DecimalType.Numeric(9, 2);
    private static readonly DecimalType T32S4 = DecimalType.Numeric(9, 4);
    private static readonly DecimalType T64S2 = DecimalType.Numeric(18, 2);
    private static readonly DecimalType T64S4 = DecimalType.Numeric(18, 4);
    private static readonly DecimalType T128S2 = DecimalType.Numeric(38, 2);
    private static readonly DecimalType T128S4 = DecimalType.Numeric(38, 4);
    private static readonly DecimalType T256S2 = DecimalType.Numeric(76, 2);
    private static readonly DecimalType T256S4 = DecimalType.Numeric(76, 4);

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _i32L = new int[N]; _i32R = new int[N]; _i32Out = new int[N];
        _i64L = new long[N]; _i64R = new long[N]; _i64Out = new long[N];
        _i128L = new Int128[N]; _i128R = new Int128[N]; _i128Out = new Int128[N];
        _i256L = new Int256[N]; _i256R = new Int256[N]; _i256Out = new Int256[N];

        for (int i = 0; i < N; i++)
        {
            _i32L[i] = rng.Next(-10_000, 10_000);
            _i32R[i] = rng.Next(1, 10_000);              // non-zero: also used as a divisor
            _i64L[i] = rng.NextInt64(-1_000_000_000L, 1_000_000_000L);
            _i64R[i] = rng.NextInt64(-1_000_000_000L, 1_000_000_000L);
            _i128L[i] = rng.NextInt64();
            _i128R[i] = rng.NextInt64(1, long.MaxValue);
            _i256L[i] = rng.NextInt64();
            _i256R[i] = rng.NextInt64();
        }
    }

    // --- add, same scale: no rescale, so this is arithmetic plus the range check ---
    [Benchmark] public void Add32_SameScale() => SpanAddKernel.Add(_i32L, T32S2, _i32R, T32S2, _i32Out, T32S2);
    [Benchmark] public void Add64_SameScale() => SpanAddKernel.Add(_i64L, T64S2, _i64R, T64S2, _i64Out, T64S2);
    [Benchmark] public void Add128_SameScale() => SpanAddKernel.Add(_i128L, T128S2, _i128R, T128S2, _i128Out, T128S2);
    [Benchmark] public void Add256_SameScale() => SpanAddKernel.Add(_i256L, T256S2, _i256R, T256S2, _i256Out, T256S2);

    // --- add, mixed scale: per-element rescale by a power of ten ---
    [Benchmark] public void Add32_RescaleUp() => SpanAddKernel.Add(_i32L, T32S2, _i32R, T32S4, _i32Out, T32S4);
    [Benchmark] public void Add128_RescaleUp() => SpanAddKernel.Add(_i128L, T128S2, _i128R, T128S4, _i128Out, T128S4);
    [Benchmark] public void Add256_RescaleUp() => SpanAddKernel.Add(_i256L, T256S2, _i256R, T256S4, _i256Out, T256S4);

    // --- multiply ---
    [Benchmark] public void Mul64To128_Rescale() => SpanMultiplyKernel.Multiply(_i64L, T64S2, _i64R, T64S2, _i128Out, T128S2);
    [Benchmark] public void Mul128_SameWidth_Rescale() => SpanMultiplyKernel.Multiply(_i128L, T128S2, _i128R, T128S2, _i128Out, T128S2);

    // --- divide: the rounding mode applies on nearly every element ---
    [Benchmark] public void Div32To64() => SpanDivideKernel.Divide(_i32L, T32S2, _i32R, T32S2, _i64Out, T64S4);
    [Benchmark] public void Div128_SameWidth() => SpanDivideKernel.Divide(_i128L, T128S2, _i128R, T128S2, _i128Out, T128S4);
}
