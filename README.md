
	// * Summary *

	BenchmarkDotNet=v0.11.1, OS=Windows 10.0.17134.228 (1803/April2018Update/Redstone4)
	Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
	.NET Core SDK=2.1.401
	  [Host]     : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT
	  DefaultJob : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT


	 Method |     Mean |     Error |    StdDev |
	------- |---------:|----------:|----------:|
	   Copy | 2.564 us | 0.0097 us | 0.0091 us |
	    Ref | 1.626 us | 0.0111 us | 0.0104 us |
	     In | 3.565 us | 0.0459 us | 0.0429 us |
