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
        public static readonly DelegateCommand<IHistoryManager> UnionCommand =
            CommandManager.Register<IHistoryManager>(Union);

        public static readonly DelegateCommand<IHistoryManager> IntersectionCommand =
            CommandManager.Register<IHistoryManager>(Intersection);

        public static readonly DelegateCommand<IHistoryManager> DifferenceCommand =
            CommandManager.Register<IHistoryManager>(Difference);

        public static readonly DelegateCommand<IHistoryManager> XorCommand =
            CommandManager.Register<IHistoryManager>(Xor);

        public static readonly DelegateCommand<IHistoryManager> GroupCommand =
            CommandManager.Register<IHistoryManager>(Group);

        public static readonly DelegateCommand<IHistoryManager> UngroupCommand =
            CommandManager.Register<IHistoryManager>(Ungroup);

        private static void BinaryOperation(IHistoryManager manager, CombineMode operation)
        {
            var selectionManager = manager.Context.SelectionManager;
            if (selectionManager.Selection[0] is IGeometricLayer x &&
                selectionManager.Selection[1] is IGeometricLayer y)
                manager.Do(new BinaryOperationCommand(
                    manager.Position + 1,
                    new[] {x, y},
                    operation));
        }

        private static void Difference(IHistoryManager manager)
        {
            BinaryOperation(manager, CombineMode.Exclude);
        }

        private static void Group(IHistoryManager manager)
        {
            var selectionManager = manager.Context.SelectionManager;

            if (selectionManager.Selection.Count == 0)
                return;

            var command = new GroupCommand(
                manager.Position + 1,
                selectionManager.Selection.ToArray<ILayer>());
            manager.Do(command);
        }

        private static void Intersection(IHistoryManager manager)
        {
            BinaryOperation(manager, CombineMode.Intersect);
        }

        private static void Ungroup(IHistoryManager manager)
        {
            var selectionManager = manager.Context.SelectionManager;

            if (selectionManager.Selection.Count == 0)
                return;

            var command = new UngroupCommand(
                manager.Position + 1,
                selectionManager.Selection.OfType<IContainerLayer>().ToArray());
            manager.Do(command);
        }

        private static void Union(IHistoryManager manager)
        {
            BinaryOperation(manager, CombineMode.Union);
        }

        private static void Xor(IHistoryManager manager)
        {
            BinaryOperation(manager, CombineMode.Xor);
        }
    }
}