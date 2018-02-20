using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Commands;
using Rain.Core;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Utility;
using Rain.ViewModel;

namespace Rain.View.Command
{
    public static class SelectionCommands
    {
        public static readonly DelegateCommand<IArtContext> SelectAllCommand =
            CommandManager.Register<IArtContext>(SelectAll);

        public static readonly DelegateCommand<IArtContext> DeselectAllCommand =
            CommandManager.Register<IArtContext>(DeselectAll);

        public static readonly DelegateCommand<IArtContext> MoveToBottomCommand =
            CommandManager.Register<IArtContext>(MoveToBottom);

        public static readonly DelegateCommand<IArtContext> MoveToTopCommand =
            CommandManager.Register<IArtContext>(MoveToTop);

        public static readonly DelegateCommand<IArtContext> MoveUpCommand =
            CommandManager.Register<IArtContext>(MoveUp);

        public static readonly DelegateCommand<IArtContext> MoveDownCommand =
            CommandManager.Register<IArtContext>(MoveDown);

        public static readonly DelegateCommand<IArtContext> FlipVerticalCommand =
            CommandManager.Register<IArtContext>(FlipVertical);

        public static readonly DelegateCommand<IArtContext> FlipHorizontalCommand =
            CommandManager.Register<IArtContext>(FlipHorizontal);

        public static readonly DelegateCommand<IArtContext> RotateCounterClockwiseCommand =
            CommandManager.Register<IArtContext>(RotateCounterClockwise);

        public static readonly DelegateCommand<IArtContext> RotateClockwiseCommand =
            CommandManager.Register<IArtContext>(RotateClockwise);

        public static readonly DelegateCommand<IArtContext> AlignLeftCommand =
            CommandManager.Register<IArtContext>(AlignLeft);

        public static readonly DelegateCommand<IArtContext> AlignTopCommand =
            CommandManager.Register<IArtContext>(AlignTop);

        public static readonly DelegateCommand<IArtContext> AlignBottomCommand =
            CommandManager.Register<IArtContext>(AlignBottom);

        public static readonly DelegateCommand<IArtContext> AlignRightCommand =
            CommandManager.Register<IArtContext>(AlignRight);

        public static readonly DelegateCommand<IArtContext> AlignCenterXCommand =
            CommandManager.Register<IArtContext>(AlignCenterX);

        public static readonly DelegateCommand<IArtContext> AlignCenterYCommand =
            CommandManager.Register<IArtContext>(AlignCenterY);

        public static readonly DelegateCommand<IArtContext> GroupCommand =
            CommandManager.Register<IArtContext>(Group);

        public static readonly DelegateCommand<IArtContext> UngroupCommand =
            CommandManager.Register<IArtContext>(Ungroup);

        private static void Align(IArtContext artContext, Direction dir)
        {
            if (!artContext.SelectionManager.Selection.Any())
                return;

            var cmd = new AlignCommand(artContext.HistoryManager.Position + 1,
                                       artContext.SelectionManager.Selection.ToArray(),
                                       dir);

            artContext.HistoryManager.Do(cmd);

            artContext.SelectionManager.UpdateBounds();
        }

        private static void AlignBottom(IArtContext artContext)
        {
            Align(artContext, Direction.Down);
        }

        private static void AlignCenterX(IArtContext artContext)
        {
            Align(artContext, Direction.Horizontal);
        }

        private static void AlignCenterY(IArtContext artContext)
        {
            Align(artContext, Direction.Vertical);
        }

        private static void AlignLeft(IArtContext artContext) { Align(artContext, Direction.Left); }

        private static void AlignRight(IArtContext artContext)
        {
            Align(artContext, Direction.Right);
        }

        private static void AlignTop(IArtContext artContext) { Align(artContext, Direction.Up); }

        private static void DeselectAll(IArtContext artContext)
        {
            artContext.SelectionManager.ClearSelection();
        }

        private static void FlipHorizontal(IArtContext artContext)
        {
            artContext.SelectionManager.TransformSelection(new Vector2(-1, 1),
                                                           Vector2.Zero,
                                                           0,
                                                           0,
                                                           Vector2.One * 0.5f);
        }

        private static void FlipVertical(IArtContext artContext)
        {
            artContext.SelectionManager.TransformSelection(new Vector2(1, -1),
                                                           Vector2.Zero,
                                                           0,
                                                           0,
                                                           Vector2.One * 0.5f);
        }

        private static void Group(IArtContext ctx)
        {
            var selectionManager = ctx.SelectionManager;

            if (!selectionManager.Selection.Any())
                return;

            var command = new GroupCommand(ctx.HistoryManager.Position + 1,
                                           selectionManager.Selection.ToArray());

            ctx.HistoryManager.Do(command);
        }

        private static bool HasSelection(IArtContext artContext)
        {
            return artContext.SelectionManager?.Selection.Any() == true;
        }

        private static void MoveDown(IArtContext artContext)
        {
            var history = artContext.SelectionManager.Context.HistoryManager;

            history.Do(new ChangeZIndexCommand(history.Position + 1,
                                               artContext.SelectionManager.Selection.ToArray(),
                                               1));
        }

        private static void MoveToBottom(IArtContext artContext)
        {
            var history = artContext.SelectionManager.Context.HistoryManager;

            history.Do(new ChangeZIndexCommand(history.Position + 1,
                                               artContext.SelectionManager.Selection.ToArray(),
                                               100000000));
        }

        private static void MoveToTop(IArtContext artContext)
        {
            var history = artContext.SelectionManager.Context.HistoryManager;

            history.Do(new ChangeZIndexCommand(history.Position + 1,
                                               artContext.SelectionManager.Selection.ToArray(),
                                               -100000000));
        }

        private static void MoveUp(IArtContext artContext)
        {
            var history = artContext.SelectionManager.Context.HistoryManager;

            history.Do(new ChangeZIndexCommand(history.Position + 1,
                                               artContext.SelectionManager.Selection.ToArray(),
                                               -1));
        }

        private static void RotateClockwise(IArtContext artContext)
        {
            artContext.SelectionManager.TransformSelection(Vector2.One,
                                                           Vector2.Zero,
                                                           MathUtils.PiOverTwo,
                                                           0,
                                                           Vector2.One * 0.5f);
        }

        private static void RotateCounterClockwise(IArtContext artContext)
        {
            artContext.SelectionManager.TransformSelection(Vector2.One,
                                                           Vector2.Zero,
                                                           -MathUtils.PiOverTwo,
                                                           0,
                                                           Vector2.One * 0.5f);
        }

        private static void SelectAll(IArtContext artContext)
        {
            artContext.SelectionManager.ClearSelection();

            foreach (var layer in artContext
                                 .SelectionManager.Context.ViewManager.Root.Flatten()
                                 .Skip(1))
                layer.Selected = true;
        }

        private static void Ungroup(IArtContext ctx)
        {
            var selectionManager = ctx.SelectionManager;

            if (!selectionManager.Selection.Any())
                return;

            var command = new UngroupCommand(ctx.HistoryManager.Position + 1,
                                             selectionManager
                                                .Selection.OfType<IContainerLayer>()
                                                .ToArray());
            ctx.HistoryManager.Do(command);
        }
    }
}