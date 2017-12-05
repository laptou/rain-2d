using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Matrix3x2 = SharpDX.Matrix3x2;

namespace Ibinimator.Test
{
    [TestClass]
    public class MathTest
    {
        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestMatrices()
        {
            var times1 = new List<double>();

            for (var i = 0; i < 100; i++)
            {
                var mat = Matrix3x2.Identity;

                var s = Matrix3x2.Scaling(13);
                var r = Matrix3x2.Rotation(13);
                var t = Matrix3x2.Translation(13, 13);

                var sw = Stopwatch.StartNew();

                for (var j = 0; j < 10000; j++)
                {
                    mat *= s;
                    mat *= r;
                    mat *= t;
                }

                sw.Stop();

                times1.Add(sw.Elapsed.TotalMilliseconds);
            }

            var times2 = new List<double>();

            for (var i = 0; i < 100; i++)
            {

                var mat = System.Numerics.Matrix3x2.Identity;

                var s = System.Numerics.Matrix3x2.CreateScale(13);
                var r = System.Numerics.Matrix3x2.CreateRotation(13);
                var t = System.Numerics.Matrix3x2.CreateTranslation(13, 13);

                var sw = Stopwatch.StartNew();

                for (var j = 0; j < 10000; j++)
                {
                    mat *= s;
                    mat *= r;
                    mat *= t;
                }

                sw.Stop();

                times2.Add(sw.Elapsed.TotalMilliseconds);
            }

            TestContext.WriteLine($"SharpDX: {times1.Average()}ms\n" +
                                  $"System.Numerics: {times2.Average()}ms");
            TestContext.WriteLine($"HW Acceleration: {Vector.IsHardwareAccelerated}");
            TestContext.WriteLine($"64-Bit CPU: {Environment.Is64BitOperatingSystem}");
            TestContext.WriteLine($"64-Bit Process: {Environment.Is64BitProcess}");
        }
    }
}