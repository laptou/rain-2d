using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.ViewModel;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Ibinimator.Service.Commands
{
    public static class ObjectCommands
    {
        public static readonly DelegateCommand<ISelectionManager> UnionCommand =
            new DelegateCommand<ISelectionManager>(Union, null);

        private static void Union(ISelectionManager manager)
        {
            BinaryOperation(manager, CombineMode.Union);
        }

        public static readonly DelegateCommand<ISelectionManager> IntersectionCommand =
            new DelegateCommand<ISelectionManager>(Intersection, null);

        private static void Intersection(ISelectionManager manager)
        {
            BinaryOperation(manager, CombineMode.Intersect);
        }

        public static readonly DelegateCommand<ISelectionManager> DifferenceCommand =
            new DelegateCommand<ISelectionManager>(Difference, null);

        private static void Difference(ISelectionManager manager)
        {
            BinaryOperation(manager, CombineMode.Exclude);
        }

        public static readonly DelegateCommand<ISelectionManager> XorCommand =
            new DelegateCommand<ISelectionManager>(Xor, null);

        private static void Xor(ISelectionManager manager)
        {
            BinaryOperation(manager, CombineMode.Xor);
        }

        private static void BinaryOperation(ISelectionManager manager,
            CombineMode operation)
        {
            if (manager?.Selection.Count != 2) return;

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
                
                using(var xtg = new TransformedGeometry(factory, xg, x.AbsoluteTransform))
                    xtg.Combine(yg, operation, y.AbsoluteTransform, 0.25f, zSink);

                zSink.Close();

                x.Parent.Add(z);
                x.Parent.Remove(x);
                y.Parent.Remove(y);
            }
        }
    }
}
