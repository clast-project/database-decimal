// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Clast.DatabaseDecimal;

/// <summary>
/// Selects how a value is rounded when a scale change discards digits.
/// </summary>
/// <remarks>
/// Rounding only happens where digits are dropped: dividing, rescaling to a
/// smaller scale, and parsing text with more fractional digits than the target
/// scale. Operations that only scale upward (addition and subtraction, whose
/// result scale is <c>max(s1,s2)</c>) never round, so the mode has no effect
/// on them.
/// <para>
/// <see cref="HalfEven"/> is the default so that the mode can be omitted
/// without changing behaviour, but it is not what most SQL engines do — Spark,
/// SQL Server, PostgreSQL, and MySQL all round half away from zero. Callers
/// matching those engines should pass <see cref="HalfUp"/>.
/// </para>
/// </remarks>
public enum DecimalRounding
{
    /// <summary>
    /// Round to nearest; on an exact midpoint round to the neighbour whose last
    /// digit is even ("banker's rounding"). 2.5 becomes 2, 3.5 becomes 4.
    /// This is the IEEE 754 default and the behaviour of <c>Math.Round</c>.
    /// </summary>
    HalfEven = 0,

    /// <summary>
    /// Round to nearest; on an exact midpoint round away from zero.
    /// 2.5 becomes 3, -2.5 becomes -3. This is the SQL family's behaviour
    /// (ANSI <c>ROUND_HALF_UP</c>, Spark, SQL Server, PostgreSQL, MySQL).
    /// </summary>
    HalfUp = 1,
}
