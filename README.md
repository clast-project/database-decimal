# Clast.DatabaseDecimal

Fixed-point decimal arithmetic for database engines, with mantissa tiers from 32 to 256 bits and SQL/Substrait precision-and-scale promotion rules built in.

[![NuGet](https://img.shields.io/nuget/v/Clast.DatabaseDecimal.svg)](https://www.nuget.org/packages/Clast.DatabaseDecimal/)
[![CI](https://github.com/clast-project/database-decimal/actions/workflows/ci.yml/badge.svg)](https://github.com/clast-project/database-decimal/actions/workflows/ci.yml)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://github.com/clast-project/database-decimal/blob/main/LICENSE)

## Overview

`Clast.DatabaseDecimal` provides the numeric primitives a query engine or columnar runtime needs to implement SQL `NUMERIC(p,s)` faithfully:

- **Four mantissa tiers** — `Decimal32`, `Decimal64`, `Decimal128`, and `Decimal256`, covering precisions of 1–9, 10–18, 19–38, and 39–76 digits respectively. The right tier for a given precision is selected by `DecimalType.Width`.
- **Scalar and span kernels** for add, subtract, multiply, divide, and modulus. The span (columnar) kernels operate over `ReadOnlySpan<T>` / `Span<T>` for batch evaluation without per-row allocations.
- **UTF-8 and UTF-16 parsing and formatting** via `DecimalText`, with culture-invariant round-tripping.
- **SQL Server / Substrait promotion rules** in `DecimalTypeRules`, so `(p1,s1) ⊕ (p2,s2)` yields the same result type a database planner would produce.
- **Selectable rounding** via `DecimalRounding`, on every entry point that discards digits.

## Rounding

Rounding happens wherever digits are dropped: division, rescaling to a smaller
scale (which is what precision clamping forces on multiplication), and parsing
text with more fractional digits than the target scale. Addition, subtraction,
and modulus scale both operands *up* to `max(s1,s2)` under the promotion rules,
so they never round.

Every kernel, `ScaleHelper` rescale helper, and `DecimalText` parse method takes
an optional trailing `DecimalRounding`:

```csharp
var intType = DecimalType.Numeric(precision: 9, scale: 0);
var oneDecimal = DecimalType.Numeric(precision: 18, scale: 1);
var five = new Decimal32(5);
var two = new Decimal32(2);

// Banker's rounding — the default, and what IEEE 754 and Math.Round do.
DivideKernel.Divide(five, intType, two, intType, intType);                      // 2
ScaleHelper.Rescale128(25, fromScale: 1, toScale: 0);                           // 2
DecimalText.ParseDecimal64("1.45".AsSpan(), oneDecimal);                        // 1.4

// Half away from zero — what Spark, SQL Server, PostgreSQL, and MySQL do.
DivideKernel.Divide(five, intType, two, intType, intType, DecimalRounding.HalfUp); // 3
ScaleHelper.Rescale128(25, 1, 0, DecimalRounding.HalfUp);                          // 3
DecimalText.ParseDecimal64("1.45".AsSpan(), oneDecimal, DecimalRounding.HalfUp);   // 1.5
```

The default is `HalfEven` so the argument can be omitted without changing
behaviour. Note that it does **not** match the promotion rules' SQL Server
lineage — callers implementing SQL semantics should pass `HalfUp` explicitly.

The mode is loop-invariant in the span kernels: it is read once per call, not
per element, and the batch paths stay allocation-free.

## Overflow

A result must fit the result type's *precision*, which is a stricter bound than
the mantissa width: `NUMERIC(38,0)` stops at `10^38 - 1`, while a 128-bit
mantissa reaches about `1.7 × 10^38`. Checked arithmetic catches the width; the
kernels check the precision, and both throw `OverflowException`.

```csharp
var t = DecimalType.Numeric(38, 0);
var max38 = DecimalText.ParseDecimal128("99999999999999999999999999999999999999".AsSpan(), t);
var one = DecimalText.ParseDecimal128("1".AsSpan(), t);

AddKernel.Add(max38, t, one, t, t);   // throws: 39 digits does not fit NUMERIC(38,0)
```

Engines that null overflowing rows rather than failing the query — Spark outside
ANSI mode, for one — treat overflow as routine, so an exception per batch would
be both costly and wrong. Pass `DecimalOverflow.Ignore` and scan the output:

```csharp
var t = DecimalType.Numeric(9, 0);
int[] left = { 1, 999_999_999 };
int[] right = { 1, 1 };
int[] result = new int[2];

SpanAddKernel.Add(left, t, right, t, result, t,
                  DecimalRounding.HalfEven, DecimalOverflow.Ignore);

ulong[] mask = new ulong[DecimalRange.MaskWordCount(result.Length)];
int overflowed = DecimalRange.WriteOutOfRangeMask(result, t, mask);
// overflowed == 1, and bit 1 of mask[0] flags result[1], which reached 10 digits
```

`DecimalRange` also offers `IsInRange`, `Validate` (throws), and
`IndexOfOutOfRange` (first offender, or -1). `Ignore` relaxes only the declared
precision check — the mantissa width is always checked, so a result that
overflows `Int128` still throws.

In the span kernels the check is a separate pass over the finished output, not a
per-element branch, so the arithmetic loop stays branch-free and vectorized and
the output is fully written even when it throws.

## Example

```csharp
using Clast.DatabaseDecimal;
using Clast.DatabaseDecimal.Text;
using Clast.DatabaseDecimal.Values;

var type = DecimalType.Numeric(precision: 18, scale: 4);

var a = DecimalText.ParseDecimal64("1234.5678".AsSpan(), type);
var b = DecimalText.ParseDecimal64("0.0001".AsSpan(), type);

var sum = new Decimal64(a.Mantissa + b.Mantissa);
Console.WriteLine(sum.ToString(type.Scale)); // 1234.5679
```

## Target frameworks

- `netstandard2.0`
- `net8.0`
- `net10.0`

## License

Licensed under the [Apache License, Version 2.0](LICENSE).
