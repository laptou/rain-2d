using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer.Model;

using SharpDX.Direct2D1;

namespace Ibinimator.Service.Commands
{
    public sealed class BinaryOperationCommand : LayerCommandBase<IGeometricLayer>
    {
        private readonly Dictionary<ILayer, (IContainerLayer parent, int index)> _parents =
            new Dictionary<ILayer, (IContainerLayer, int)>();

        public BinaryOperationCommand(
            long id, IEnumerable<IGeometricLayer> targets, CombineMode operation) : base(
            id,
            targets.OrderByDescending(l => l.Order).ToArray())
        {
            Operation = operation;
        }

        public override string Description => Operation.ToString();

        public CombineMode Operation { get; }

        public Path Product { get; private set; }

        public override void Do(IArtContext artContext)
        {
            var operand1 = Targets[0];

            _parents[operand1] = (operand1.Parent, operand1.Parent.SubLayers.IndexOf(operand1));

            for (var i = 1; i < Targets.Length; i++)
            {
                var operand2 = Targets[i];

                _parents[operand2] = (operand2.Parent, operand2.Parent.SubLayers.IndexOf(operand2));

                var xg = artContext.CacheManager.GetGeometry(operand1);
                var yg = artContext.CacheManager.GetGeometry(operand2);

                var z = new Path
                {
                    Fill = operand1.Fill,
                    Stroke = operand1.Stroke
                };

                using (var xtg = xg.Transform(operand1.AbsoluteTransform))
                {
                    using (var ytg = yg.Transform(operand2.AbsoluteTransform))
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


                operand1.Parent?.Remove(operand1);
                operand2.Parent?.Remove(operand2);

                operand1 = z;
            }

            Product = (Path) operand1;

            var parent = _parents[Targets[0]].parent;
            Product.ApplyTransform(global: MathUtils.Invert(parent.AbsoluteTransform));
            parent.Add(Product);
        }

        public override IOperationCommand Merge(IOperationCommand newCommand)
        {
            throw new InvalidOperationException("This operation is cannot be merged.");
        }

        public override void Undo(IArtContext artView)
        {
            Product.Parent.Remove(Product);

            foreach (var pair in _parents)
                pair.Value.parent.Add(pair.Key, pair.Value.index);
        }
    }
}