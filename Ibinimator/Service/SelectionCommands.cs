using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.ViewModel;
using SharpDX;

namespace Ibinimator.Service
{
    public static class SelectionCommands
    {
        public static readonly DelegateCommand<ISelectionManager> SelectAllCommand = new DelegateCommand<ISelectionManager>(SelectAll, null);
        public static readonly DelegateCommand<ISelectionManager> DeselectAllCommand = new DelegateCommand<ISelectionManager>(DeselectAll, null);
        public static readonly DelegateCommand<ISelectionManager> MoveToBottomCommand = new DelegateCommand<ISelectionManager>(MoveToBottom, null);
        public static readonly DelegateCommand<ISelectionManager> MoveToTopCommand = new DelegateCommand<ISelectionManager>(MoveToTop, null);
        public static readonly DelegateCommand<ISelectionManager> MoveUpCommand = new DelegateCommand<ISelectionManager>(MoveUp, null);
        public static readonly DelegateCommand<ISelectionManager> MoveDownCommand = new DelegateCommand<ISelectionManager>(MoveDown, null);
        public static readonly DelegateCommand<ISelectionManager> FlipVerticalCommand = new DelegateCommand<ISelectionManager>(FlipVertical, null);
        public static readonly DelegateCommand<ISelectionManager> FlipHorizontalCommand = new DelegateCommand<ISelectionManager>(FlipHorizontal, null);
        public static readonly DelegateCommand<ISelectionManager> RotateCounterClockwiseCommand = new DelegateCommand<ISelectionManager>(RotateCounterClockwise, null);
        public static readonly DelegateCommand<ISelectionManager> RotateClockwiseCommand = new DelegateCommand<ISelectionManager>(RotateClockwise, null);

        private static void SelectAll(ISelectionManager selectionManager)
        {
            selectionManager.ClearSelection();

            foreach (var layer in selectionManager.Root.Flatten().Skip(1))
                layer.Selected = true;
        }

        private static void DeselectAll(ISelectionManager selectionManager)
        {
            selectionManager.ClearSelection();
        }

        private static void MoveToBottom(ISelectionManager selectionManager)
        {
            foreach (var layer in selectionManager.Selection)
            {
                var parent = layer.Parent;
                parent.Remove(layer);
                parent.Add(layer);
            }
        }

        private static void MoveToTop(ISelectionManager selectionManager)
        {
            foreach (var layer in selectionManager.Selection)
            {
                var parent = layer.Parent;
                parent.Remove(layer);
                parent.Add(layer, 0);
            }
        }

        private static void MoveUp(ISelectionManager selectionManager)
        {
            foreach (var layer in selectionManager.Selection)
            {
                var index = layer.Parent.SubLayers.IndexOf(layer);
                var parent = layer.Parent;
                parent.Remove(layer);
                parent.Add(layer, Math.Max(0, index - 1));
            }
        }

        private static void MoveDown(ISelectionManager selectionManager)
        {
            foreach (var layer in selectionManager.Selection)
            {
                var index = layer.Parent.SubLayers.IndexOf(layer);
                var parent = layer.Parent;
                parent.Remove(layer);
                parent.Add(layer, Math.Min(parent.SubLayers.Count, index + 1));
            }
        }

        private static void FlipVertical(ISelectionManager selectionManager)
        {
            selectionManager.Transform(new Vector2(1, -1), Vector2.Zero, 0, 0, Vector2.One * 0.5f);
        }

        private static void FlipHorizontal(ISelectionManager selectionManager)
        {
            selectionManager.Transform(new Vector2(-1, 1), Vector2.Zero, 0, 0, Vector2.One * 0.5f);
        }

        private static void RotateClockwise(ISelectionManager selectionManager)
        {
            selectionManager.Transform(Vector2.One, Vector2.Zero, MathUtil.PiOverTwo, 0, Vector2.One * 0.5f);
        }

        private static void RotateCounterClockwise(ISelectionManager selectionManager)
        {
            selectionManager.Transform(Vector2.One, Vector2.Zero, -MathUtil.PiOverTwo, 0, Vector2.One * 0.5f);
        }

        private static bool HasSelection(ISelectionManager selectionManager)
        {
            return selectionManager?.Selection.Count > 0;
        }
    }
}
