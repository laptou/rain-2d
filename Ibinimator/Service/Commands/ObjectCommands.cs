using System;
using System.Collections.Generic;
using Ibinimator.Shared;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.ViewModel;
using SharpDX;
using SharpDX.Direct2D1;
using static Ibinimator.Service.CommandManager;

namespace Ibinimator.Service.Commands
{
    public static class ObjectCommands
    {
        public static readonly DelegateCommand<ISelectionManager> UnionCommand =
            Instance.Register<ISelectionManager>(Union);

        public static readonly DelegateCommand<ISelectionManager> IntersectionCommand =
            Instance.Register<ISelectionManager>(Intersection);

        public static readonly DelegateCommand<ISelectionManager> DifferenceCommand =
            Instance.Register<ISelectionManager>(Difference);

        public static readonly DelegateCommand<ISelectionManager> XorCommand =
            Instance.Register<ISelectionManager>(Xor);

        private static void BinaryOperation(ISelectionManager manager, CombineMode operation)
        {
            if (manager?.Selection.Count != 2)
                throw new InvalidOperationException("Binary operations require two and only two objects.");

            var x = manager.Selection[0];
            var y = manager.Selection[1];
            var factory = manager.ArtView.Direct2DFactory;

            if (x is IGeometricLayer xgl && y is IGeometricLayer ygl)
            {
                var xg = manager.ArtView.CacheManager.GetGeometry(xgl);
                var yg = manager.ArtView.CacheManager.GetGeometry(ygl);

                var z = new Path
                {
                    FillBrush = xgl.FillBrush,
                    StrokeBrush = xgl.StrokeBrush,
                    StrokeInfo = xgl.StrokeInfo
                };

                var zSink = z.Open();

                using (var xtg = new TransformedGeometry(factory, xg, x.AbsoluteTransform))
                {
                    xtg.Combine(yg, operation, y.AbsoluteTransform, 0.25f, zSink);
                }

                zSink.Close();

                (z.Scale, z.Rotation, z.Position, z.Shear) =
                    Matrix3x2.Invert(x.WorldTransform).Decompose();

                x.Parent.Add(z);
                x.Parent.Remove(x);
                y.Parent.Remove(y);
            }
            else throw new InvalidOperationException("Both operands must be convertible to paths.");
        }

        private static void Difference(ISelectionManager manager)
        {
            BinaryOperation(manager, CombineMode.Exclude);
        }

        private static void Intersection(ISelectionManager manager)
        {
            BinaryOperation(manager, CombineMode.Intersect);
        }

        private static void Union(ISelectionManager manager)
        {
            BinaryOperation(manager, CombineMode.Union);
        }

        private static void Xor(ISelectionManager manager)
        {
            BinaryOperation(manager, CombineMode.Xor);
        }
    }
}