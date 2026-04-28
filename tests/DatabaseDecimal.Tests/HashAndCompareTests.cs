using System.Buffers.Binary;
using System.Collections;
using System.IO.Hashing;
using DatabaseDecimal;
using DatabaseDecimal.Values;
using Xunit;

namespace DatabaseDecimal.Tests;

public class HashAndCompareTests
{
    // ================================================================
    // Non-generic IComparable
    // ================================================================

    [Fact]
    public void NonGenericCompareTo_Null_ReturnsPositive()
    {
        IComparable a = new Decimal32(0);
        Assert.Equal(1, a.CompareTo(null));
    }

    [Fact]
    public void NonGenericCompareTo_WrongType_Throws()
    {
        IComparable a = new Decimal64(42);
        Assert.Throws<ArgumentException>(() => a.CompareTo("not a decimal"));
    }

    [Fact]
    public void NonGenericCompareTo_SameType_OrdersByMantissa()
    {
        IComparable a = new Decimal128(100);
        Assert.True(a.CompareTo(new Decimal128(100)) == 0);
        Assert.True(a.CompareTo(new Decimal128(200)) < 0);
        Assert.True(a.CompareTo(new Decimal128(50)) > 0);
    }

    [Fact]
    public void NonGenericCompareTo_WorksThroughLegacyArrayList()
    {
        // ArrayList.Sort uses non-generic IComparable.
        var list = new ArrayList
        {
            new Decimal256((Int256)3),
            new Decimal256((Int256)1),
            new Decimal256((Int256)2),
        };
        list.Sort();
        Assert.Equal(new Decimal256((Int256)1), list[0]);
        Assert.Equal(new Decimal256((Int256)2), list[1]);
        Assert.Equal(new Decimal256((Int256)3), list[2]);
    }

    [Fact]
    public void NonGenericCompareTo_AllTiers()
    {
        Assert.Equal(1, ((IComparable)new Decimal32(0)).CompareTo(null));
        Assert.Equal(1, ((IComparable)new Decimal64(0)).CompareTo(null));
        Assert.Equal(1, ((IComparable)new Decimal128(0)).CompareTo(null));
        Assert.Equal(1, ((IComparable)new Decimal256(Int256.Zero)).CompareTo(null));

        Assert.Throws<ArgumentException>(() => ((IComparable)new Decimal32(0)).CompareTo(new Decimal64(0)));
        Assert.Throws<ArgumentException>(() => ((IComparable)new Decimal64(0)).CompareTo(new Decimal32(0)));
        Assert.Throws<ArgumentException>(() => ((IComparable)new Decimal128(0)).CompareTo(new Decimal256(Int256.Zero)));
        Assert.Throws<ArgumentException>(() => ((IComparable)new Decimal256(Int256.Zero)).CompareTo(new Decimal128(0)));
    }

    // ================================================================
    // StableHash64 — determinism and distinctness
    // ================================================================

    [Fact]
    public void StableHash64_IsDeterministic()
    {
        Assert.Equal(new Decimal32(12345).StableHash64(), new Decimal32(12345).StableHash64());
        Assert.Equal(new Decimal64(12345).StableHash64(), new Decimal64(12345).StableHash64());
        Assert.Equal(new Decimal128(12345).StableHash64(), new Decimal128(12345).StableHash64());
        Assert.Equal(new Decimal256((Int256)12345).StableHash64(), new Decimal256((Int256)12345).StableHash64());
    }

    [Fact]
    public void StableHash64_DistinctMantissasProduceDistinctHashes()
    {
        // Not strictly required by hashing, but a sanity check: a handful
        // of values should not collide.
        var seen = new HashSet<ulong>();
        for (int i = 0; i < 1000; i++)
        {
            Assert.True(seen.Add(new Decimal32(i).StableHash64()),
                $"Collision at Decimal32 mantissa {i}.");
        }
    }

    // ================================================================
    // StableHash64 — golden values
    // These pin the wire format so accidental changes to the byte layout
    // (endianness, mantissa width, etc.) break cross-process compatibility
    // visibly instead of silently.
    // ================================================================

    [Fact]
    public void StableHash64_Decimal32_GoldenValues()
    {
        Assert.Equal(ExpectedHash(stackalloc byte[] { 0, 0, 0, 0 }),
            new Decimal32(0).StableHash64());
        Assert.Equal(ExpectedHash(stackalloc byte[] { 1, 0, 0, 0 }),
            new Decimal32(1).StableHash64());
        Assert.Equal(ExpectedHash(stackalloc byte[] { 0xFF, 0xFF, 0xFF, 0xFF }),
            new Decimal32(-1).StableHash64());
    }

    [Fact]
    public void StableHash64_Decimal64_GoldenValues()
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(b, 0);
        Assert.Equal(ExpectedHash(b), new Decimal64(0).StableHash64());

        BinaryPrimitives.WriteInt64LittleEndian(b, 1234567890L);
        Assert.Equal(ExpectedHash(b), new Decimal64(1234567890L).StableHash64());

        BinaryPrimitives.WriteInt64LittleEndian(b, -1L);
        Assert.Equal(ExpectedHash(b), new Decimal64(-1L).StableHash64());
    }

    [Fact]
    public void StableHash64_Decimal128_GoldenValues()
    {
        Span<byte> b = stackalloc byte[16];

        // Mantissa 0
        b.Clear();
        Assert.Equal(ExpectedHash(b), new Decimal128(Int128.Zero).StableHash64());

        // Mantissa 1
        b.Clear();
        b[0] = 1;
        Assert.Equal(ExpectedHash(b), new Decimal128((Int128)1).StableHash64());

        // Mantissa -1 (all ones)
        b.Fill(0xFF);
        Assert.Equal(ExpectedHash(b), new Decimal128((Int128)(-1)).StableHash64());

        // Large value spanning both halves
        Int128 big = ((Int128)0x0123456789ABCDEFUL << 64) | (Int128)0xFEDCBA9876543210UL;
        BinaryPrimitives.WriteUInt64LittleEndian(b, 0xFEDCBA9876543210UL);
        BinaryPrimitives.WriteUInt64LittleEndian(b.Slice(8), 0x0123456789ABCDEFUL);
        Assert.Equal(ExpectedHash(b), new Decimal128(big).StableHash64());
    }

    [Fact]
    public void StableHash64_Decimal256_GoldenValues()
    {
        Span<byte> b = stackalloc byte[32];

        // Mantissa 0
        b.Clear();
        Assert.Equal(ExpectedHash(b), new Decimal256(Int256.Zero).StableHash64());

        // Mantissa 1
        b.Clear();
        b[0] = 1;
        Assert.Equal(ExpectedHash(b), new Decimal256(Int256.One).StableHash64());

        // Mantissa -1 (all ones)
        b.Fill(0xFF);
        Assert.Equal(ExpectedHash(b), new Decimal256(Int256.MinusOne).StableHash64());
    }

    private static ulong ExpectedHash(ReadOnlySpan<byte> bytes) =>
        XxHash3.HashToUInt64(bytes);
}
