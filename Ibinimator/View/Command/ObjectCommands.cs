using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;
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

            ctx.HistoryManager.Do(new BinaryOperationCommand(
                                      ctx.HistoryManager.Position + 1,
                                      selectionManager.Selection.OfType<IGeometricLayer>(),
                                      operation));
        }

        private static void Difference(IArtContext manager)
        {
            BinaryOperation(manager, CombineMode.Exclude);
        }

        private static void Intersection(IArtContext manager)
        {
            BinaryOperation(manager, CombineMode.Intersect);
        }

        private static void Pathify(IArtContext ctx)
        {
            ctx.HistoryManager.Do(new ConvertToPathCommand(ctx.HistoryManager.Position + 1,
                                                           ctx.SelectionManager.Selection
                                                              .OfType<IGeometricLayer>()
                                                              .ToArray()));
        }

        private static void Union(IArtContext manager)
        {
            BinaryOperation(manager, CombineMode.Union);
        }

        private static void Xor(IArtContext manager) { BinaryOperation(manager, CombineMode.Xor); }
    }
}