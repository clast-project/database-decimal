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

## Example

```csharp
using Clast.DatabaseDecimal;
using Clast.DatabaseDecimal.Text;
using Clast.DatabaseDecimal.Values;

var type = DecimalType.Numeric(precision: 18, scale: 4);

var a = DecimalText.ParseDecimal64("1234.5678", type.Scale);
var b = DecimalText.ParseDecimal64("0.0001", type.Scale);

var sum = new Decimal64(a.Mantissa + b.Mantissa);
Console.WriteLine(sum.ToString(type.Scale)); // 1234.5679
```

## Target frameworks

- `netstandard2.0`
- `net8.0`
- `net10.0`

## License

Licensed under the [Apache License, Version 2.0](LICENSE).
