using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using osu.Framework.Extensions.MatrixExtensions;
using OpenTK;

namespace osu.Framework.Bench
{
    public class CopyVsRef
    {
        private Matrix3x2 matSeed;

        [GlobalSetup]
        public void Setup()
        {
            var rng = new Random(42);
            matSeed = new Matrix3x2(
                (float)rng.NextDouble(),
                (float)rng.NextDouble(),
                (float)rng.NextDouble(),
                (float)rng.NextDouble(),
                (float)rng.NextDouble(),
                (float)rng.NextDouble());
        }

        [Benchmark]
        public float Copy()
        {
            var mat = matSeed;
            Matrix3x2 result = new Matrix3x2();
            for (int i = 0; i < 100; i++)
            {
                MatrixExtensions.Invert(mat, out result);
                MatrixExtensions.Invert(result, out mat);
            }
            return result.M11 + result.M12 + result.M21 + result.M22 + result.M31 + result.M32;
        }

        [Benchmark]
        public float Ref()
        {
            var mat = matSeed;
            Matrix3x2 result = new Matrix3x2();
            for (int i = 0; i < 100; i++)
            {
                MatrixExtensions.InvertRef(ref mat, out result);
                MatrixExtensions.InvertRef(ref result, out mat);
            }
            return result.M11 + result.M12 + result.M21 + result.M22 + result.M31 + result.M32;
        }

        [Benchmark]
        public float In()
        {
            var mat = matSeed;
            Matrix3x2 result = new Matrix3x2();
            for (int i = 0; i < 100; i++)
            {
                MatrixExtensions.InvertIn(in mat, out result);
                MatrixExtensions.InvertIn(in result, out mat);
            }
            return result.M11 + result.M12 + result.M21 + result.M22 + result.M31 + result.M32;
        }

    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<CopyVsRef>();
        }
    }
}
