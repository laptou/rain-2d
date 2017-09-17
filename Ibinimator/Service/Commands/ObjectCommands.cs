using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.ViewModel;
using SharpDX.Direct2D1;
using static Ibinimator.Service.CommandManager;

namespace Ibinimator.Service.Commands
{
    public static class ObjectCommands
    {
        public static readonly DelegateCommand<IHistoryManager> UnionCommand =
            Instance.Register<IHistoryManager>(Union);

        public static readonly DelegateCommand<IHistoryManager> IntersectionCommand =
            Instance.Register<IHistoryManager>(Intersection);

        public static readonly DelegateCommand<IHistoryManager> DifferenceCommand =
            Instance.Register<IHistoryManager>(Difference);

        public static readonly DelegateCommand<IHistoryManager> XorCommand =
            Instance.Register<IHistoryManager>(Xor);

        private static void BinaryOperation(IHistoryManager manager, CombineMode operation)
        {
            var selectionManager = manager.ArtView.SelectionManager;
            if (selectionManager.Selection[0] is IGeometricLayer x &&
                selectionManager.Selection[1] is IGeometricLayer y)
                manager.Do(new BinaryOperationCommand(
                    manager.Time + 1,
                    new[] {x, y},
                    operation));
        }

        private static void Difference(IHistoryManager manager)
        {
            BinaryOperation(manager, CombineMode.Exclude);
        }

        private static void Intersection(IHistoryManager manager)
        {
            BinaryOperation(manager, CombineMode.Intersect);
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