// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Clast.DatabaseDecimal;

/// <summary>
/// Selects what happens when an arithmetic result exceeds the precision of the
/// result <see cref="DecimalType"/>.
/// </summary>
/// <remarks>
/// This governs only the check against the *declared* precision — the gap
/// between 10^precision and the mantissa's own range. The mantissa width is
/// always checked: the kernels use checked arithmetic throughout, so a result
/// that overflows <c>Int128</c> throws regardless of this setting.
/// <para>
/// For example, with a result type of <c>NUMERIC(38,0)</c>, the value
/// 10^38 fits comfortably in a 128-bit mantissa (whose range reaches about
/// 1.7 × 10^38) but needs 39 digits, so it exceeds the declared precision.
/// </para>
/// </remarks>
public enum DecimalOverflow
{
    /// <summary>
    /// Throw <see cref="OverflowException"/> when a result does not fit the
    /// result type's precision. This matches what the width check already does.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// Return the result without checking it against the declared precision.
    /// </summary>
    /// <remarks>
    /// Intended for callers that have already proven the range by other means,
    /// and for batch callers that want per-row overflow information rather than
    /// an exception: pair this with
    /// <see cref="Arithmetic.DecimalRange"/>.<c>WriteOutOfRangeMask</c>
    /// to flag the offending rows in a single pass. Engines that null out
    /// overflowing rows rather than failing the query — Spark outside ANSI mode,
    /// for one — treat overflow as routine, so an exception per batch would be
    /// both costly and wrong.
    /// </remarks>
    Ignore = 1,
}
