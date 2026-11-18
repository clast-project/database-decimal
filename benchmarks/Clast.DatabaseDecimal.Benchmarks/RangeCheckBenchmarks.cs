// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using BenchmarkDotNet.Attributes;
using Clast.DatabaseDecimal.Arithmetic;
using Clast.DatabaseDecimal.Values;

namespace Clast.DatabaseDecimal.Benchmarks;

/// <summary>
/// The result-precision check on the 128- and 256-bit tiers (#11), separated
/// from the arithmetic it follows.
/// </summary>
/// <remarks>
/// Throw against Ignore on the same add is the cost of the check inside a
/// kernel; the Mask rows are the same comparisons called directly, which is
/// roughly what the check should cost once it is not competing with the kernel
/// for inlining budget. The precisions are chosen to land on each branch the
/// wide checks could take: p38 and p76 have a non-zero top 64-bit limb in
/// their bound, p50 has it only in the upper 128-bit half, and p19 in neither.
/// <code>
/// dotnet run -c Release -f net10.0 --project benchmarks/Clast.DatabaseDecimal.Benchmarks -- --filter '*RangeCheck*' --job medium
/// </code>
/// Results have so far been measured on ARM64 only; x64 may differ, since
/// Vector&lt;long&gt; has four lanes there rather than two.
/// </remarks>
public class RangeCheckBenchmarks
{
    [Params(65536)] public int N;
    private Int128[] _l = null!, _r = null!, _o = null!;
    private Int256[] _l2 = null!, _r2 = null!, _o2 = null!;
    private ulong[] _mask = null!;
    private static readonly DecimalType T19 = DecimalType.Numeric(19, 2);
    private static readonly DecimalType T38 = DecimalType.Numeric(38, 2);
    private static readonly DecimalType T50 = DecimalType.Numeric(50, 2);
    private static readonly DecimalType T76 = DecimalType.Numeric(76, 2);

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _l = new Int128[N]; _r = new Int128[N]; _o = new Int128[N];
        _l2 = new Int256[N]; _r2 = new Int256[N]; _o2 = new Int256[N];
        _mask = new ulong[(N + 63) / 64];
        for (int i = 0; i < N; i++)
        {
            // halved so the p19 sums stay inside 10^19 and the check passes
            _l[i] = rng.NextInt64() / 2; _r[i] = rng.NextInt64() / 2;
            _l2[i] = rng.NextInt64(); _r2[i] = rng.NextInt64();
        }
    }

    [Benchmark] public void A128p38_Throw() => SpanAddKernel.Add(_l, T38, _r, T38, _o, T38);
    [Benchmark] public void A128p38_Ignore() => SpanAddKernel.Add(_l, T38, _r, T38, _o, T38, DecimalRounding.HalfEven, DecimalOverflow.Ignore);
    [Benchmark] public void A128p19_Throw() => SpanAddKernel.Add(_l, T19, _r, T19, _o, T19);
    [Benchmark] public void A256p76_Throw() => SpanAddKernel.Add(_l2, T76, _r2, T76, _o2, T76);
    [Benchmark] public void A256p76_Ignore() => SpanAddKernel.Add(_l2, T76, _r2, T76, _o2, T76, DecimalRounding.HalfEven, DecimalOverflow.Ignore);
    [Benchmark] public void A256p50_Throw() => SpanAddKernel.Add(_l2, T50, _r2, T50, _o2, T50);
    [Benchmark] public int Mask128p38() => DecimalRange.WriteOutOfRangeMask(_o, T38, _mask);
    [Benchmark] public int Mask256p76() => DecimalRange.WriteOutOfRangeMask(_o2, T76, _mask);
    [Benchmark] public int Mask256p50() => DecimalRange.WriteOutOfRangeMask(_o2, T50, _mask);
}
