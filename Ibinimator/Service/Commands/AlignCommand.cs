using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service.Commands
{
    public class AlignCommand : LayerCommandBase<ILayer>
    {
        private readonly Dictionary<ILayer, Matrix3x2> _transformations =
            new Dictionary<ILayer, Matrix3x2>();

        public AlignCommand(long id, ILayer[] targets, Direction direction) :
            base(id, targets)
        {
            Direction = direction;
            Description = $"Aligned {Direction}";
        }

        public override void Do(IArtContext artContext)
        {
            var totalBounds = Targets.Select(artContext.CacheManager.GetAbsoluteBounds)
                                     .Aggregate(RectangleF.Union);

            foreach (var target in Targets)
            {
                var bounds = artContext.CacheManager.GetAbsoluteBounds(target);

                var delta = Vector2.Zero;

                switch (Direction)
                {
                    case Direction.Vertical:
                        // center vertically
                        delta.Y = totalBounds.Center.Y - bounds.Center.Y;
                        break;
                    case Direction.Horizontal:
                        // center horizontally
                        delta.X = totalBounds.Center.X - bounds.Center.X;
                        break;
                    case Direction.Up:
                        delta.Y = totalBounds.Top - bounds.Top;
                        break;
                    case Direction.Down:
                        delta.Y = totalBounds.Bottom - bounds.Bottom;
                        break;
                    case Direction.Left:
                        delta.X = totalBounds.Left - bounds.Left;
                        break;
                    case Direction.Right:
                        delta.X = totalBounds.Right - bounds.Right;
                        break;
                }

                var mat = Matrix3x2.CreateTranslation(delta);
                target.ApplyTransform(global: mat);
                _transformations[target] = mat;
            }

            artContext.InvalidateSurface();
        }

        public override void Undo(IArtContext artContext)
        {
            foreach (var target in Targets)
            {
                var mat = MathUtils.Invert(_transformations[target]);
                target.ApplyTransform(global: mat);
            }

            artContext.InvalidateSurface();
        }

        public override IOperationCommand Merge(IOperationCommand newCommand)
        {
            throw new InvalidOperationException("This operation cannot be merged.");
        }

        public override string Description { get; }

        public Direction Direction { get; }
    }

    [Flags]
    public enum Direction
    {
        Up = 1 << 0,
        Down = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        Vertical = Down | Up,
        Horizontal = Left | Right
    }
}