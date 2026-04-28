// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Clast.DatabaseDecimal;

/// <summary>
/// The backing integer width for a decimal value's mantissa.
/// </summary>
public enum DecimalWidth : byte
{
    W32,
    W64,
    W128,
    W256,
}
