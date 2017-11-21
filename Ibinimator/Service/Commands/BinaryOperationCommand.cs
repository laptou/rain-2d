using System;
using System.Collections.Generic;
using Ibinimator.Core.Utility;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
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

        public BinaryOperationCommand(long id, IGeometricLayer[] targets, CombineMode operation) : base(
            id, targets)
        {
            if (targets.Length != 2)
                throw new ArgumentException("Binary operations can only have 2 operands.");
            Operation = operation;
        }

        public override IOperationCommand Merge(IOperationCommand newCommand)
        {
            throw new InvalidOperationException("This operation is cannot be merged.");
        }

        public override string Description => Operation.ToString();

        public CombineMode Operation { get; }

        public Path Product { get; private set; }

        public override void Do(IArtContext artContext)
        {
            if (Product == null)
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

                        z.Instructions.AddItems(zg.Read());
                    }
                }

                z.ApplyTransform(MathUtils.Invert(_operand1.WorldTransform), Matrix3x2.Identity);

                Product = z;
                _parent1 = _operand1.Parent;
                _parent2 = _operand2.Parent;
            }

            _parent1.Add(Product);
            _parent1.Remove(_operand1 as Layer);
            _parent2.Remove(_operand2 as Layer);
        }

        public override void Undo(IArtContext artView)
        {
            Product.Parent.Remove(Product);
            _parent1.Add(_operand1 as Layer);
            _parent2.Add(_operand2 as Layer);
        }
    }
}