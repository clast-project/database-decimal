using DatabaseDecimal.Values;

namespace DatabaseDecimal.Arithmetic;

/// <summary>
/// Precomputed powers of 10 for each integer width tier.
/// </summary>
internal static class PowersOf10
{
    public static ReadOnlySpan<int> Int32 =>
    [
        1,
        10,
        100,
        1_000,
        10_000,
        100_000,
        1_000_000,
        10_000_000,
        100_000_000,
        1_000_000_000, // 10^9
    ];

    public static ReadOnlySpan<long> Int64 =>
    [
        1L,
        10L,
        100L,
        1_000L,
        10_000L,
        100_000L,
        1_000_000L,
        10_000_000L,
        100_000_000L,
        1_000_000_000L,
        10_000_000_000L,
        100_000_000_000L,
        1_000_000_000_000L,
        10_000_000_000_000L,
        100_000_000_000_000L,
        1_000_000_000_000_000L,
        10_000_000_000_000_000L,
        100_000_000_000_000_000L,
        1_000_000_000_000_000_000L, // 10^18
    ];

    private static readonly Int128[] s_int128 = ComputeInt128Powers();

    public static ReadOnlySpan<Int128> Int128 => s_int128;

    private static Int128[] ComputeInt128Powers()
    {
        // 10^0 through 10^38
        var powers = new Int128[39];
        powers[0] = System.Int128.One;
        for (int i = 1; i < powers.Length; i++)
            powers[i] = powers[i - 1] * 10;
        return powers;
    }

    private static readonly Int256[] s_int256 = ComputeInt256Powers();

    public static ReadOnlySpan<Int256> Int256 => s_int256;

    /// <summary>
    /// Provides direct array access for Decimal256.FromInteger bounds checking.
    /// </summary>
    internal static Int256[] Int256Values => s_int256;

    private static Int256[] ComputeInt256Powers()
    {
        // 10^0 through 10^76
        var powers = new Int256[77];
        powers[0] = Values.Int256.One;
        for (int i = 1; i < powers.Length; i++)
            powers[i] = powers[i - 1] * (Values.Int256)10;
        return powers;
    }
}
