using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Service.Commands;
using Ibinimator.ViewModel;

namespace Ibinimator.View.Command
{
    public static class SelectionCommands
    {
        public static readonly DelegateCommand<ISelectionManager> SelectAllCommand =
            CommandManager.Register<ISelectionManager>(SelectAll);

        public static readonly DelegateCommand<ISelectionManager> DeselectAllCommand =
            CommandManager.Register<ISelectionManager>(DeselectAll);

        public static readonly DelegateCommand<ISelectionManager> MoveToBottomCommand =
            CommandManager.Register<ISelectionManager>(MoveToBottom);

        public static readonly DelegateCommand<ISelectionManager> MoveToTopCommand =
            CommandManager.Register<ISelectionManager>(MoveToTop);

        public static readonly DelegateCommand<ISelectionManager> MoveUpCommand =
            CommandManager.Register<ISelectionManager>(MoveUp);

        public static readonly DelegateCommand<ISelectionManager> MoveDownCommand =
            CommandManager.Register<ISelectionManager>(MoveDown);

        public static readonly DelegateCommand<ISelectionManager> FlipVerticalCommand =
            CommandManager.Register<ISelectionManager>(FlipVertical);

        public static readonly DelegateCommand<ISelectionManager> FlipHorizontalCommand =
            CommandManager.Register<ISelectionManager>(FlipHorizontal);

        public static readonly DelegateCommand<ISelectionManager> RotateCounterClockwiseCommand =
            CommandManager.Register<ISelectionManager>(RotateCounterClockwise);

        public static readonly DelegateCommand<ISelectionManager> RotateClockwiseCommand =
            CommandManager.Register<ISelectionManager>(RotateClockwise);

        private static void DeselectAll(ISelectionManager selectionManager)
        {
            selectionManager.ClearSelection();
        }

        private static void FlipHorizontal(ISelectionManager selectionManager)
        {
            selectionManager.Transform(new Vector2(-1, 1), Vector2.Zero, 0, 0, Vector2.One * 0.5f);
        }

        private static void FlipVertical(ISelectionManager selectionManager)
        {
            selectionManager.Transform(new Vector2(1, -1), Vector2.Zero, 0, 0, Vector2.One * 0.5f);
        }

        private static bool HasSelection(ISelectionManager selectionManager)
        {
            return selectionManager?.Selection.Count > 0;
        }

        private static void MoveDown(ISelectionManager selectionManager)
        {
            var history = selectionManager.Context.HistoryManager;

            history.Do(
                new ChangeZIndexCommand(
                    history.Position + 1,
                    selectionManager.Selection.ToArray<ILayer>(),
                    1));
        }

        private static void MoveToBottom(ISelectionManager selectionManager)
        {
            var history = selectionManager.Context.HistoryManager;

            history.Do(
                new ChangeZIndexCommand(
                    history.Position + 1,
                    selectionManager.Selection.ToArray<ILayer>(),
                    100000000));
        }

        private static void MoveToTop(ISelectionManager selectionManager)
        {
            var history = selectionManager.Context.HistoryManager;

            history.Do(
                new ChangeZIndexCommand(
                    history.Position + 1,
                    selectionManager.Selection.ToArray<ILayer>(),
                    -100000000));
        }

        private static void MoveUp(ISelectionManager selectionManager)
        {
            var history = selectionManager.Context.HistoryManager;

            history.Do(
                new ChangeZIndexCommand(
                    history.Position + 1,
                    selectionManager.Selection.ToArray<ILayer>(),
                    -1));
        }

        private static void RotateClockwise(ISelectionManager selectionManager)
        {
            selectionManager.Transform(Vector2.One, Vector2.Zero, MathUtils.PiOverTwo, 0, Vector2.One * 0.5f);
        }

        private static void RotateCounterClockwise(ISelectionManager selectionManager)
        {
            selectionManager.Transform(Vector2.One, Vector2.Zero, -MathUtils.PiOverTwo, 0, Vector2.One * 0.5f);
        }

        private static void SelectAll(ISelectionManager selectionManager)
        {
            selectionManager.ClearSelection();

            foreach (var layer in selectionManager.Context.ViewManager.Root.Flatten().Skip(1))
                layer.Selected = true;
        }
    }
}