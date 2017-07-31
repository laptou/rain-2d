using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ibinimator.Shared;
using SharpDX;

namespace Ibinimator.Test
{
    [TestClass]
    public class MathTest
    {
        [TestMethod]
        public void Projection()
        {
            Vector2 vec = new Vector2(10, 10);
            Vector2 axis = new Vector2(1, 1);
            Vector2 expected = new Vector2(10, 10);
            Vector2 actual = MathUtils.Project(vec, axis);
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
            RectangleF rect = new RectangleF(0, -100, 200, 100);

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
            Vector2 v1 = new Vector2(1, 0);
            Vector2 v2 = new Vector2(MathUtils.Sqrt12, MathUtils.Sqrt12);
            Vector2 v3 = new Vector2(0, 1);
            Vector2 v4 = new Vector2(-MathUtils.Sqrt12, MathUtils.Sqrt12);
            Vector2 v5 = new Vector2(-1, 0);
            Vector2 v6 = new Vector2(-MathUtils.Sqrt12, -MathUtils.Sqrt12);
            Vector2 v7 = new Vector2(0, -1);
            Vector2 v8 = new Vector2(MathUtils.Sqrt12, -MathUtils.Sqrt12);

            Assert.AreEqual(v2, MathUtils.Rotate(v1, MathUtils.PiOverTwo / 2));
            Assert.AreEqual(v3, MathUtils.Rotate(v1, MathUtils.PiOverTwo));
            Assert.AreEqual(v5, MathUtils.Rotate(v1, MathUtils.PiOverTwo * 2));
            Assert.AreEqual(v7, MathUtils.Rotate(v1, MathUtils.PiOverTwo * 3));
            Assert.AreEqual(new Vector2(3, 0), MathUtils.Rotate(v5, v1, MathUtils.PiOverTwo * 2));
        }

        [TestMethod]
        public void Decompose()
        {
            Matrix3x2 m = Matrix3x2.Identity;
            var d = m.Decompose();

            void Verify(Matrix3x2 mat)
            {
                var decomp = mat.Decompose();

                Matrix3x2 mat2 = Matrix3x2.Identity;
                mat2 *= Matrix3x2.Scaling(decomp.scale);
                mat2 *= Matrix3x2.Skew(0, decomp.skew);
                mat2 *= Matrix3x2.Rotation(decomp.rotation);
                mat2 *= Matrix3x2.Translation(decomp.translation);

                RectangleF r = new RectangleF(0, 0, 100, 100);

                Vector2 tl = Matrix3x2.TransformPoint(mat, r.TopLeft);
                Vector2 tr = Matrix3x2.TransformPoint(mat, r.TopRight);
                Vector2 br = Matrix3x2.TransformPoint(mat, r.BottomRight);
                Vector2 bl = Matrix3x2.TransformPoint(mat, r.BottomLeft);

                Vector2 tl2 = Matrix3x2.TransformPoint(mat2, r.TopLeft);
                Vector2 tr2 = Matrix3x2.TransformPoint(mat2, r.TopRight);
                Vector2 br2 = Matrix3x2.TransformPoint(mat2, r.BottomRight);
                Vector2 bl2 = Matrix3x2.TransformPoint(mat2, r.BottomLeft);

                AreEqual(tl, tl2, 1e-4f);
                AreEqual(tr, tr2, 1e-4f);
                AreEqual(br, br2, 1e-4f);
                AreEqual(bl, bl2, 1e-4f);

                Random rnd = new Random();

                for (int i = 0; i < 10000; i++)
                {
                    Vector2 v = rnd.NextVector2(-Vector2.One * 10, Vector2.One * 10);
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
        }

        void AreEqual(Vector2 expected, Vector2 actual, float delta)
        {
            Assert.AreEqual(expected.X, actual.X, delta * expected.Length());
            Assert.AreEqual(expected.Y, actual.Y, delta * expected.Length());
        }
    }
}
