// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using BenchmarkDotNet.Attributes;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Benchmarks;

[MemoryDiagnoser]
public class SpanMultiplyBenchmarks
{
    [Params(1024, 65536)]
    public int N;

    private int[] _i32Left = null!;
    private int[] _i32Right = null!;
    private long[] _i64ResultFromI32 = null!;

    private long[] _i64Left = null!;
    private long[] _i64Right = null!;
    private Int128[] _i128ResultFromI64 = null!;

    // Source operands at scale s; widening multiply produces s+s, then rescales to s.
    private static readonly DecimalType T32S2 = DecimalType.Numeric(precision: 9, scale: 2);
    private static readonly DecimalType T64S2 = DecimalType.Numeric(precision: 18, scale: 2);
    private static readonly DecimalType T64S4 = DecimalType.Numeric(precision: 18, scale: 4);
    private static readonly DecimalType T128S2 = DecimalType.Numeric(precision: 38, scale: 2);
    private static readonly DecimalType T128S4 = DecimalType.Numeric(precision: 38, scale: 4);

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);

        _i32Left = new int[N]; _i32Right = new int[N];
        _i64ResultFromI32 = new long[N];

        _i64Left = new long[N]; _i64Right = new long[N];
        _i128ResultFromI64 = new Int128[N];

        for (int i = 0; i < N; i++)
        {
            _i32Left[i] = rng.Next(-30_000, 30_000);
            _i32Right[i] = rng.Next(-30_000, 30_000);

            _i64Left[i] = rng.NextInt64(-1_000_000_000L, 1_000_000_000L);
            _i64Right[i] = rng.NextInt64(-1_000_000_000L, 1_000_000_000L);
        }
    }

    // 32x32 -> 64, raw scale (s+s) matches result scale: no per-element rescale.
    [Benchmark]
    public void Multiply_Int32_To_Int64_NoRescale() =>
        SpanMultiplyKernel.Multiply(_i32Left, T32S2, _i32Right, T32S2, _i64ResultFromI32, T64S4);

    // 32x32 -> 64, then divide-by-100 to drop one scale digit: exercises the rescale branch.
    [Benchmark]
    public void Multiply_Int32_To_Int64_Rescale() =>
        SpanMultiplyKernel.Multiply(_i32Left, T32S2, _i32Right, T32S2, _i64ResultFromI32, T64S2);

    [Benchmark]
    public void Multiply_Int64_To_Int128_NoRescale() =>
        SpanMultiplyKernel.Multiply(_i64Left, T64S2, _i64Right, T64S2, _i128ResultFromI64, T128S4);

    [Benchmark]
    public void Multiply_Int64_To_Int128_Rescale() =>
        SpanMultiplyKernel.Multiply(_i64Left, T64S2, _i64Right, T64S2, _i128ResultFromI64, T128S2);

    // Broadcast scalar (column * scalar) 32x32 -> 64, no rescale.
    private const int Scalar32 = 12_345;

    [Benchmark]
    public void Multiply_Int32_To_Int64_Broadcast_NoRescale() =>
        SpanMultiplyKernel.Multiply(_i32Left, T32S2, Scalar32, T32S2, _i64ResultFromI32, T64S4);
}
