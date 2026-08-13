// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using BenchmarkDotNet.Attributes;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Benchmarks;

/// <summary>
/// Division is the operation where the rounding mode applies on nearly every
/// element, and the 128-bit same-width multiply is the path that forms the
/// exact product in 256 bits when precision clamping reduces the result scale.
/// Both loops read the mode once per call, so these measure whether that
/// stayed true.
/// </summary>
[MemoryDiagnoser]
public class SpanDivideBenchmarks
{
    [Params(1024, 65536)]
    public int N;

    private int[] _i32Left = null!;
    private int[] _i32Right = null!;
    private long[] _i64Result = null!;

    private Int128[] _i128Left = null!;
    private Int128[] _i128Right = null!;
    private Int128[] _i128Result = null!;

    private static readonly DecimalType T32S2 = DecimalType.Numeric(precision: 9, scale: 2);
    private static readonly DecimalType T64S4 = DecimalType.Numeric(precision: 18, scale: 4);
    private static readonly DecimalType T128S2 = DecimalType.Numeric(precision: 38, scale: 2);
    private static readonly DecimalType T128S4 = DecimalType.Numeric(precision: 38, scale: 4);

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);

        _i32Left = new int[N];
        _i32Right = new int[N];
        _i64Result = new long[N];

        _i128Left = new Int128[N];
        _i128Right = new Int128[N];
        _i128Result = new Int128[N];

        for (int i = 0; i < N; i++)
        {
            _i32Left[i] = rng.Next(-30_000, 30_000);
            // Never zero: division by zero is a caller concern, not a kernel one.
            int r = rng.Next(1, 30_000);
            _i32Right[i] = rng.Next(2) == 0 ? r : -r;

            _i128Left[i] = (Int128)rng.NextInt64(-1_000_000_000L, 1_000_000_000L);
            long rr = rng.NextInt64(1, 1_000_000_000L);
            _i128Right[i] = (Int128)(rng.Next(2) == 0 ? rr : -rr);
        }
    }

    [Benchmark]
    public void Divide_Int32_To_Int64_HalfEven() =>
        SpanDivideKernel.Divide(_i32Left, T32S2, _i32Right, T32S2, _i64Result, T64S4, DecimalRounding.HalfEven);

    [Benchmark]
    public void Divide_Int32_To_Int64_HalfUp() =>
        SpanDivideKernel.Divide(_i32Left, T32S2, _i32Right, T32S2, _i64Result, T64S4, DecimalRounding.HalfUp);

    [Benchmark]
    public void Divide_Int128_SameWidth_HalfEven() =>
        SpanDivideKernel.Divide(_i128Left, T128S2, _i128Right, T128S2, _i128Result, T128S4, DecimalRounding.HalfEven);

    // 128x128 -> 128 where the result scale is below s1+s2, so the product is
    // formed in 256 bits and rescaled. This is the path that used to reduce an
    // operand's scale instead.
    [Benchmark]
    public void Multiply_Int128_SameWidth_Rescale() =>
        SpanMultiplyKernel.Multiply(_i128Left, T128S2, _i128Right, T128S2, _i128Result, T128S2);

    [Benchmark]
    public void Multiply_Int128_SameWidth_NoRescale() =>
        SpanMultiplyKernel.Multiply(_i128Left, T128S2, _i128Right, T128S2, _i128Result, T128S4);
}
