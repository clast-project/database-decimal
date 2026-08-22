// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Clast.DatabaseDecimal;

/// <summary>
/// Selects the byte order of a two's-complement mantissa in a binary buffer.
/// </summary>
/// <remarks>
/// The order belongs to the format, not to the host: Arrow stores
/// <c>decimal128</c> and <c>decimal256</c> little-endian, while Parquet stores
/// DECIMAL on <c>FIXED_LEN_BYTE_ARRAY</c> big-endian. Both can appear in one
/// file, so <see cref="Binary.DecimalBinary"/> takes the order as an argument
/// rather than baking it into a method name, and supplies no default — a
/// silently assumed order is exactly the mistake that produces a plausible but
/// wrong value.
/// </remarks>
public enum DecimalByteOrder
{
    /// <summary>Least significant byte first. Arrow's decimal layout.</summary>
    LittleEndian = 0,

    /// <summary>Most significant byte first. Parquet's DECIMAL layout.</summary>
    BigEndian = 1,
}
