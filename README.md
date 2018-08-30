
	// * Summary *

	BenchmarkDotNet=v0.11.1, OS=Windows 10.0.17134.228 (1803/April2018Update/Redstone4)
	Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
	.NET Core SDK=2.1.401
	  [Host]     : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT
	  DefaultJob : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT


	  Method |      Mean |     Error |    StdDev |
	-------- |----------:|----------:|----------:|
	    Copy | 12.799 ns | 0.0136 ns | 0.0127 ns |
	 InParam | 10.811 ns | 0.1184 ns | 0.1049 ns |
	 SelfRef |  5.611 ns | 0.0995 ns | 0.0931 ns |
	  RefOut |  4.857 ns | 0.0043 ns | 0.0036 ns |
