using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.Utility;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.Service.Commands
{
    public sealed class BinaryOperationCommand : LayerCommandBase<IGeometricLayer>
    {
        private IGeometricLayer _operand1;
        private IGeometricLayer _operand2;
        private IContainerLayer _parent1;
        private IContainerLayer _parent2;
        private Path _product;

        public BinaryOperationCommand(long id, IGeometricLayer[] targets, CombineMode operation) : base(id, targets)
        {
            if (targets.Length != 2)
                throw new ArgumentException("Binary operations can only have 2 operands.");
            Operation = operation;
        }

        public override string Description => Operation.ToString();

        public CombineMode Operation { get; }

        public override void Do(ArtView artView)
        {
            if (_product == null)
            {
                _operand1 = Targets[0];
                _operand2 = Targets[1];
                var factory = artView.Direct2DFactory;

                var xg = artView.CacheManager.GetGeometry(_operand1);
                var yg = artView.CacheManager.GetGeometry(_operand2);

                var z = new Path
                {
                    FillBrush = _operand1.FillBrush,
                    StrokeBrush = _operand1.StrokeBrush,
                    StrokeInfo = _operand1.StrokeInfo
                };

                var zSink = z.Open();

                using (var xtg = new TransformedGeometry(factory, xg, _operand1.AbsoluteTransform))
                {
                    xtg.Combine(yg, Operation, _operand2.AbsoluteTransform, 0.25f, zSink);
                }

                zSink.Close();

                (z.Scale, z.Rotation, z.Position, z.Shear) =
                    Matrix3x2.Invert(_operand1.WorldTransform).Decompose();

                _product = z;
                _parent1 = _operand1.Parent;
                _parent2 = _operand2.Parent;
            }

            _parent1.Add(_product);
            _parent1.Remove(_operand1 as Layer);
            _parent2.Remove(_operand2 as Layer);
        }

        public override void Undo(ArtView artView)
        {
            _product.Parent.Remove(_product);
            _parent1.Add(_operand1 as Layer);
            _parent2.Add(_operand2 as Layer);
        }
    }
}