using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Service.Commands;
using Ibinimator.ViewModel;
using SharpDX.Direct2D1;

namespace Ibinimator.View.Command
{
    public static class ObjectCommands
    {
        public static readonly DelegateCommand<IArtContext> UnionCommand =
            CommandManager.Register<IArtContext>(Union);

        public static readonly DelegateCommand<IArtContext> IntersectionCommand =
            CommandManager.Register<IArtContext>(Intersection);

        public static readonly DelegateCommand<IArtContext> DifferenceCommand =
            CommandManager.Register<IArtContext>(Difference);

        public static readonly DelegateCommand<IArtContext> XorCommand =
            CommandManager.Register<IArtContext>(Xor);

        public static readonly DelegateCommand<IArtContext> PathifyCommand =
            CommandManager.Register<IArtContext>(Pathify);

        private static void BinaryOperation(IArtContext ctx, CombineMode operation)
        {
            var selectionManager = ctx.SelectionManager;

            if (selectionManager.Selection.Count != 2)
            {
                ctx.Status = new Status(Status.StatusType.Error,
                                        "Select exactly 2 objects to perform a boolean operation.");
                return;
            }

            if (selectionManager.Selection[0] is IGeometricLayer x &&
                selectionManager.Selection[1] is IGeometricLayer y)
                ctx.HistoryManager.Do(new BinaryOperationCommand(
                                          ctx.HistoryManager.Position + 1,
                                          new[] {x, y},
                                          operation));
        }

        private static void Difference(IArtContext manager) { BinaryOperation(manager, CombineMode.Exclude); }

        private static void Intersection(IArtContext manager)
        {
            BinaryOperation(manager, CombineMode.Intersect);
        }

        private static void Union(IArtContext manager) { BinaryOperation(manager, CombineMode.Union); }

        private static void Xor(IArtContext manager) { BinaryOperation(manager, CombineMode.Xor); }

        private static void Pathify(IArtContext ctx)
        {
            ctx.HistoryManager.Do(
                new ConvertToPathCommand(
                    ctx.HistoryManager.Position + 1,
                    ctx.SelectionManager
                       .Selection
                       .OfType<IGeometricLayer>()
                       .ToArray()));
        }
    }
}