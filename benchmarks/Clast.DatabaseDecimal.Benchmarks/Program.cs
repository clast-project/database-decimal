// Copyright (c) clast-project. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

// Run all benchmarks:   dotnet run -c Release --project benchmarks/Clast.DatabaseDecimal.Benchmarks -- --filter '*'
// Run one class:        dotnet run -c Release --project benchmarks/Clast.DatabaseDecimal.Benchmarks -- --filter '*SpanAdd*'

using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
