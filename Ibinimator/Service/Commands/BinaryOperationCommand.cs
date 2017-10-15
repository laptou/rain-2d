using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Utility;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Renderer.Model.Layer;

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

        public override void Do(IArtContext artContext)
        {
            if (_product == null)
            {
                _operand1 = Targets[0];
                _operand2 = Targets[1];

                var xg = artContext.CacheManager.GetGeometry(_operand1);
                var yg = artContext.CacheManager.GetGeometry(_operand2);

                var z = new Path
                {
                    Fill = _operand1.Fill,
                    Stroke = _operand1.Stroke
                };

                using (var zSink = z.Open())
                {
                    using (var xtg = xg.Transform(_operand1.AbsoluteTransform))
                    {
                        using (var ytg = yg.Transform(_operand2.AbsoluteTransform))
                        {
                            IGeometry zg;

                            switch (Operation)
                            {
                                case CombineMode.Union:
                                    zg = xtg.Union(ytg);
                                    break;
                                case CombineMode.Intersect:
                                    zg = xtg.Intersection(ytg);
                                    break;
                                case CombineMode.Xor:
                                    zg = xtg.Xor(ytg);
                                    break;
                                case CombineMode.Exclude:
                                    zg = xtg.Difference(ytg);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            zg.Read(zSink);
                        }
                    }
                }

                (z.Scale, z.Rotation, z.Position, z.Shear) =
                    MathUtils.Invert(_operand1.WorldTransform).Decompose();

                _product = z;
                _parent1 = _operand1.Parent;
                _parent2 = _operand2.Parent;
            }

            _parent1.Add(_product);
            _parent1.Remove(_operand1 as Layer);
            _parent2.Remove(_operand2 as Layer);
        }

        public override void Undo(IArtContext artView)
        {
            _product.Parent.Remove(_product);
            _parent1.Add(_operand1 as Layer);
            _parent2.Add(_operand2 as Layer);
        }
    }
}