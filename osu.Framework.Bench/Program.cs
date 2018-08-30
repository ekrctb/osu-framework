using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using osu.Framework.Extensions.MatrixExtensions;
using OpenTK;

namespace osu.Framework.Bench
{
    public class CopyVsRef
    {
        private Matrix3x2 mat;
        private Matrix3x2 result;

        [GlobalSetup]
        public void Setup()
        {
            var rng = new Random(42);
            mat = new Matrix3x2(
                (float)rng.NextDouble(),
                (float)rng.NextDouble(),
                (float)rng.NextDouble(),
                (float)rng.NextDouble(),
                (float)rng.NextDouble(),
                (float)rng.NextDouble());
        }

        [Benchmark]
        public void Copy()
        {
            result = MatrixExtensions.Invert(mat);
        }

        [Benchmark]
        public void InParam()
        {
            result = MatrixExtensions.InvertInParam(ref mat);
        }

        [Benchmark]
        public void SelfRef()
        {
            result = mat;
            MatrixExtensions.InvertSelfRef(ref result);
        }

        [Benchmark]
        public void RefOut()
        {
            MatrixExtensions.InvertRefOut(ref mat, out result);
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
