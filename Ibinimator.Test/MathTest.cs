using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using Matrix = Ibinimator.Shared.Mathematics.Matrix;

namespace Ibinimator.Test
{
    [TestClass]
    public class MathTest
    {
        [TestMethod]
        public void Projection()
        {
            var vec = new Vector2(10, 10);
            var axis = new Vector2(1, 1);
            var expected = new Vector2(10, 10);
            var actual = MathUtils.Project(vec, axis);
            Assert.AreEqual(expected, actual);

            vec = new Vector2(10, 0);
            axis = new Vector2(1, 1);
            expected = new Vector2(5, 5);
            actual = MathUtils.Project(vec, axis);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CrossSection()
        {
            var rect = new RectangleF(0, -100, 200, 100);

            Assert.AreEqual((new Vector2(0, -100), Vector2.Zero),
                MathUtils.CrossSection(new Vector2(0, 1), Vector2.Zero, rect));
            Assert.AreEqual((new Vector2(200, 0), Vector2.Zero),
                MathUtils.CrossSection(new Vector2(1, 0), Vector2.Zero, rect));
            Assert.AreEqual(
                (new Vector2(100, -100), Vector2.Zero),
                MathUtils.CrossSection(new Vector2(1, 1), Vector2.Zero, rect));
            Assert.AreEqual(
                (new Vector2(200, -100), Vector2.Zero),
                MathUtils.CrossSection(new Vector2(1, 0.5f), Vector2.Zero, rect));
            Assert.AreEqual(
                (new Vector2(200, -50), Vector2.Zero),
                MathUtils.CrossSection(new Vector2(1, 0.25f), Vector2.Zero, rect));
        }

        [TestMethod]
        public void Rotation()
        {
            var v1 = new Vector2(1, 0);
            var v2 = new Vector2(MathUtils.InverseSqrt2, MathUtils.InverseSqrt2);
            var v3 = new Vector2(0, 1);
            var v4 = new Vector2(-MathUtils.InverseSqrt2, MathUtils.InverseSqrt2);
            var v5 = new Vector2(-1, 0);
            var v6 = new Vector2(-MathUtils.InverseSqrt2, -MathUtils.InverseSqrt2);
            var v7 = new Vector2(0, -1);
            var v8 = new Vector2(MathUtils.InverseSqrt2, -MathUtils.InverseSqrt2);

            Assert.AreEqual(v2, MathUtils.Rotate(v1, MathUtils.PiOverTwo / 2));
            Assert.AreEqual(v3, MathUtils.Rotate(v1, MathUtils.PiOverTwo));
            Assert.AreEqual(v5, MathUtils.Rotate(v1, MathUtils.PiOverTwo * 2));
            Assert.AreEqual(v7, MathUtils.Rotate(v1, MathUtils.PiOverTwo * 3));
            Assert.AreEqual(new Vector2(3, 0), MathUtils.Rotate(v5, v1, MathUtils.PiOverTwo * 2));
        }

        [TestMethod]
        public void Decompose()
        {
            var m = Matrix3x2.Identity;
            var d = m.Decompose();

            void Verify(Matrix3x2 mat)
            {
                var decomp = mat.Decompose();

                var mat2 = Matrix3x2.Identity;
                mat2 *= Matrix3x2.Scaling(decomp.scale);
                mat2 *= Matrix3x2.Skew(0, decomp.skew);
                mat2 *= Matrix3x2.Rotation(decomp.rotation);
                mat2 *= Matrix3x2.Translation(decomp.translation);

                var r = new RectangleF(0, 0, 100, 100);

                var tl = Matrix3x2.TransformPoint(mat, r.TopLeft);
                var tr = Matrix3x2.TransformPoint(mat, r.TopRight);
                var br = Matrix3x2.TransformPoint(mat, r.BottomRight);
                var bl = Matrix3x2.TransformPoint(mat, r.BottomLeft);

                var tl2 = Matrix3x2.TransformPoint(mat2, r.TopLeft);
                var tr2 = Matrix3x2.TransformPoint(mat2, r.TopRight);
                var br2 = Matrix3x2.TransformPoint(mat2, r.BottomRight);
                var bl2 = Matrix3x2.TransformPoint(mat2, r.BottomLeft);

                AreEqual(tl, tl2, 1e-4f);
                AreEqual(tr, tr2, 1e-4f);
                AreEqual(br, br2, 1e-4f);
                AreEqual(bl, bl2, 1e-4f);

                var rnd = new Random();

                for (var i = 0; i < 10000; i++)
                {
                    var v = rnd.NextVector2(-Vector2.One * 10, Vector2.One * 10);
                    AreEqual(Matrix3x2.TransformPoint(mat, v), Matrix3x2.TransformPoint(mat2, v), 1e-4f);
                }
            }

            Assert.AreEqual(Vector2.One, d.scale);
            Assert.AreEqual(Vector2.Zero, d.translation);
            Assert.AreEqual(0, d.skew, 1e-5f);
            Assert.AreEqual(0, d.rotation, 1e-5f);
            Verify(m);

            m *= Matrix3x2.Scaling(2, 3);
            d = m.Decompose();
            Assert.AreEqual(new Vector2(2, 3), d.scale);
            Assert.AreEqual(Vector2.Zero, d.translation);
            Assert.AreEqual(0, d.skew, 1e-5f);
            Assert.AreEqual(0, d.rotation, 1e-5f);
            Verify(m);

            m *= Matrix3x2.Translation(2, 3);
            d = m.Decompose();
            Assert.AreEqual(new Vector2(2, 3), d.scale);
            Assert.AreEqual(new Vector2(2, 3), d.translation);
            Assert.AreEqual(0, d.skew, 1e-5f);
            Assert.AreEqual(0, d.rotation, 1e-5f);
            Verify(m);

            m *= Matrix3x2.Rotation(MathUtils.PiOverTwo);
            d = m.Decompose();
            Assert.AreEqual(new Vector2(2, 3), d.scale);
            Assert.AreEqual(new Vector2(-3, 2), d.translation);
            Assert.AreEqual(0, d.skew, 1e-5f);
            Assert.AreEqual(MathUtils.PiOverTwo, d.rotation, 1e-5f);
            Verify(m);

            m *= Matrix3x2.Scaling(2, 1);
            d = m.Decompose();
            Assert.AreEqual(new Vector2(2, 6), d.scale);
            Assert.AreEqual(new Vector2(-6, 2), d.translation);
            Assert.AreEqual(MathUtils.PiOverTwo, d.rotation, 1e-5f);
            Verify(m);

            m *= Matrix3x2.Rotation(MathUtils.PiOverTwo / 2);
            d = m.Decompose();
            Assert.AreEqual(new Vector2(2, 6), d.scale);
            Assert.AreEqual(MathUtils.Rotate(new Vector2(-6, 2), MathUtils.PiOverTwo / 2), d.translation);
            Assert.AreEqual(MathUtils.PiOverTwo * 1.5f, d.rotation, 1e-5f);
            Verify(m);

            m = Matrix3x2.Rotation(MathUtils.PiOverTwo / 2) * Matrix3x2.Scaling(2, 1);
            Verify(m);

            m = Matrix3x2.Rotation(MathUtils.PiOverTwo * 1.25f) * Matrix3x2.Scaling(2, 1);
            m *= Matrix3x2.Rotation(MathUtils.PiOverTwo * 1.25f) * Matrix3x2.Scaling(2, 1);
            Verify(m);

            m = Matrix3x2.Rotation(-MathUtils.PiOverTwo * 1.25f) * Matrix3x2.Scaling(2, 1);
            m *= Matrix3x2.Rotation(-MathUtils.PiOverTwo * 1.25f) * Matrix3x2.Scaling(10, -6);
            Verify(m);

            m = Matrix3x2.Scaling(2, -1);
            Verify(m);

            m = Matrix3x2.Skew(45, 0);
            Verify(m);
        }

        [TestMethod]
        public void MyMatrix()
        {
            var m = Matrix.Identity;
            var v0 = new Vector2(5, 10);
            var v1 = v0;
            var rnd = new Random();

            for (var i = 0; i < 10000; i++)
            {
                switch (rnd.Next(3))
                {
                    case 0:
                        var t = rnd.NextVector2(Vector2.Zero, Vector2.One * 10);
                        m = Matrix.Translate(t) * m;
                        v1 += t;
                        break;
                    case 1:
                        var s = rnd.NextVector2(Vector2.One * 0.9f, Vector2.One * 1.1f);
                        m = Matrix.Scale(s) * m;
                        v1 *= s;
                        break;
                    case 2:
                        var r = rnd.NextFloat(0.1f, 6.27f);
                        m = Matrix.Rotate(r) * m;
                        v1 = MathUtils.Rotate(v1, r);
                        break;
                }

                var d = m.Decompose();
                var v2 = 
                    MathUtils.Rotate(
                        MathUtils.ShearX(
                            v0 * d.scale, 
                            d.shear), 
                        d.rotation) 
                        + d.translation;

                AreEqual(v1, m * v0, 1e-4f);
                AreEqual(v2, m * v0, 1e-4f);
            }
        }

        private void AreEqual(Vector2 expected, Vector2 actual, float delta)
        {
            Assert.AreEqual(expected.X, actual.X, delta * expected.Length());
            Assert.AreEqual(expected.Y, actual.Y, delta * expected.Length());
        }
    }
}