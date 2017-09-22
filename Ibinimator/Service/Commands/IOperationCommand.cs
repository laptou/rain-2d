using System;
using System.Collections.Generic;
using Ibinimator.Shared;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.Service.Commands
{
    /// <summary>
    ///     This is an interface for "operation commands", which are different
    ///     from WPF's UI commands. Operation commands are use to store undo
    ///     state information.
    /// </summary>
    public interface IOperationCommand
    {
        string Description { get; }

        long Id { get; }
        object[] Targets { get; }

        long Time { get; }

        void Do(ArtView artView);

        void Undo(ArtView artView);
    }

    public interface IOperationCommand<out T> : IOperationCommand
    {
        new T[] Targets { get; }
    }

    public abstract class LayerCommandBase<T> : IOperationCommand<T> where T : class, ILayer
    {
        protected LayerCommandBase(long id, T[] targets)
        {
            Id = id;
            Targets = targets;
        }

        #region IOperationCommand<T> Members

        public abstract void Do(ArtView artView);

        public abstract void Undo(ArtView artView);

        public abstract string Description { get; }

        public long Id { get; }

        public T[] Targets { get; }

        public long Time { get; } = Service.Time.Now;

        object[] IOperationCommand.Targets => Targets;

        #endregion
    }

    public sealed class TransformCommand : LayerCommandBase<ILayer>
    {
        public TransformCommand(long id, ILayer[] targets, Matrix3x2 matrix) : base(id, targets)
        {
            Transform = matrix;
        }

        public override string Description => $"Transformed {Targets.Length} layer(s)";

        public Matrix3x2 Transform { get; }

        public override void Do(ArtView artView)
        {
            foreach (var layer in Targets)
                lock (layer)
                {
                    var layerTransform =
                        layer.AbsoluteTransform
                        * Transform
                        * Matrix3x2.Invert(layer.WorldTransform);
                    var delta = layerTransform.Decompose();

                    layer.Scale = delta.scale;
                    layer.Rotation = delta.rotation;
                    layer.Position = delta.translation;
                    layer.Shear = delta.skew;
                }
        }

        public override void Undo(ArtView artView)
        {
            foreach (var layer in Targets)
                lock (layer)
                {
                    var layerTransform =
                        layer.AbsoluteTransform
                        * Matrix3x2.Invert(Transform)
                        * Matrix3x2.Invert(layer.WorldTransform);
                    var delta = layerTransform.Decompose();


                    layer.Scale = delta.scale;
                    layer.Rotation = delta.rotation;
                    layer.Position = delta.translation;
                    layer.Shear = delta.skew;
                }
        }
    }

    public sealed class ApplyFillCommand : LayerCommandBase<IFilledLayer>
    {
        public ApplyFillCommand(long id, IFilledLayer[] targets,
            BrushInfo @new, BrushInfo[] old) : base(id, targets)
        {
            OldFills = old.Select(o => (BrushInfo) o?.Clone()).ToArray();
            NewFill = (BrushInfo) @new?.Clone();
        }

        public override string Description => $"Filled {Targets.Length} layer(s)";

        public BrushInfo NewFill { get; }
        public BrushInfo[] OldFills { get; }

        public override void Do(ArtView artView)
        {
            foreach (var target in Targets)
                lock (target)
                {
                    target.FillBrush = NewFill;
                }
        }

        public override void Undo(ArtView artView)
        {
            for (var i = 0; i < Targets.Length; i++)
                lock (Targets[i])
                {
                    Targets[i].FillBrush = OldFills[i];
                }
        }
    }

    public sealed class ApplyStrokeCommand : LayerCommandBase<IStrokedLayer>
    {
        public ApplyStrokeCommand(long id, IStrokedLayer[] targets,
            BrushInfo newStrokeBrush, BrushInfo[] oldStrokeBrushes,
            StrokeInfo newStrokeInfo, StrokeInfo[] oldStrokeInfos) : base(id, targets)
        {
            OldStrokes =
                oldStrokeBrushes.Zip(oldStrokeInfos,
                    (b, i) => (b?.Clone<BrushInfo>(), i?.Clone<StrokeInfo>())).ToArray();

            NewStroke = (newStrokeBrush?.Clone<BrushInfo>(), newStrokeInfo?.Clone<StrokeInfo>());
        }

        public override string Description => $"Stroked {Targets.Length} layer(s)";

        public (BrushInfo, StrokeInfo) NewStroke { get; }
        public (BrushInfo, StrokeInfo)[] OldStrokes { get; }

        public override void Do(ArtView artView)
        {
            foreach (var target in Targets)
                lock (target)
                {
                    (target.StrokeBrush, target.StrokeInfo) = NewStroke;
                }
        }

        public override void Undo(ArtView artView)
        {
            for (var i = 0; i < Targets.Length; i++)
                lock (Targets[i])
                {
                    (Targets[i].StrokeBrush, Targets[i].StrokeInfo) = OldStrokes[i];
                }
        }
    }

    public sealed class ApplyFormatCommand : LayerCommandBase<ITextLayer>
    {
        public ApplyFormatCommand(long id, ITextLayer[] targets,
            string newFontFamilyName, float newFontSize, FontStretch newFontStretch,
            FontStyle newFontStyle, FontWeight newFontWeight,
            string[] oldFontFamilyNames, float[] oldFontSizes, FontStretch[] oldFontStretches,
            FontStyle[] oldFontStyles, FontWeight[] oldFontWeights) : base(id, targets)
        {
            NewFontFamilyName = newFontFamilyName;
            NewFontSize = newFontSize;
            NewFontStretch = newFontStretch;
            NewFontStyle = newFontStyle;
            NewFontWeight = newFontWeight;

            OldFontFamilyNames = oldFontFamilyNames;
            OldFontSizes = oldFontSizes;
            OldFontStretches = oldFontStretches;
            OldFontStyles = oldFontStyles;
            OldFontWeights = oldFontWeights;
        }

        public override string Description => $"Formatted {Targets.Length} layer(s)";
        public string NewFontFamilyName { get; set; }
        public float NewFontSize { get; set; }
        public FontStretch NewFontStretch { get; set; }
        public FontStyle NewFontStyle { get; set; }
        public FontWeight NewFontWeight { get; set; }

        public string[] OldFontFamilyNames { get; set; }
        public float[] OldFontSizes { get; set; }
        public FontStretch[] OldFontStretches { get; set; }
        public FontStyle[] OldFontStyles { get; set; }
        public FontWeight[] OldFontWeights { get; set; }

        public override void Do(ArtView artView)
        {
            foreach (var target in Targets)
                lock (target)
                {
                    target.FontFamilyName = NewFontFamilyName;
                    target.FontSize = NewFontSize;
                    target.FontStretch = NewFontStretch;
                    target.FontStyle = NewFontStyle;
                    target.FontWeight = NewFontWeight;
                }
        }

        public override void Undo(ArtView artView)
        {
            for (var i = 0; i < Targets.Length; i++)
            {
                var target = Targets[i];
                lock (target)
                {
                    target.FontFamilyName = OldFontFamilyNames[i];
                    target.FontSize = OldFontSizes[i];
                    target.FontStretch = OldFontStretches[i];
                    target.FontStyle = OldFontStyles[i];
                    target.FontWeight = OldFontWeights[i];
                }
            }
        }
    }

    public sealed class ApplyFormatRangeCommand : LayerCommandBase<ITextLayer>
    {
        public ApplyFormatRangeCommand(long id,
            ITextLayer target,
            Format[] oldFormat,
            Format[] newFormat) : base(id, new[] {target})
        {
            OldFormats = oldFormat;
            NewFormats = newFormat;
        }

        public override string Description => $"Changed format of range";
        public Format[] NewFormats { get; }

        public Format[] OldFormats { get; }

        public override void Do(ArtView artView)
        {
            foreach (var target in Targets)
            {
                target.ClearFormat();

                foreach (var format in NewFormats)
                    target.SetFormat(format);
            }
        }

        public override void Undo(ArtView artView)
        {
            foreach (var target in Targets)
            {
                target.ClearFormat();

                foreach (var format in OldFormats)
                    target.SetFormat(format);
            }
        }
    }

    public sealed class InsertTextCommand : LayerCommandBase<ITextLayer>
    {
        public InsertTextCommand(long id, ITextLayer target, string text, int index)
            : base(id, new[] {target})
        {
            Text = text;
            Index = index;
        }

        public override string Description => $@"Inserted text ""{Text}""";

        public int Index { get; }

        public string Text { get; }

        public override void Do(ArtView artView)
        {
            foreach (var target in Targets)
                target.Insert(Index, Text);
        }

        public override void Undo(ArtView artView)
        {
            foreach (var target in Targets)
                target.Remove(Index, Text.Length);
        }
    }

    public sealed class RemoveTextCommand : LayerCommandBase<ITextLayer>
    {
        public RemoveTextCommand(long id, ITextLayer target, string text, int index)
            : base(id, new[] {target})
        {
            Text = text;
            Index = index;
        }

        public override string Description => $@"Removed text ""{Text}""";

        public int Index { get; }

        public string Text { get; }

        public override void Do(ArtView artView)
        {
            foreach (var target in Targets)
                target.Remove(Index, Text.Length);
        }

        public override void Undo(ArtView artView)
        {
            foreach (var target in Targets)
                target.Insert(Index, Text);
        }
    }

    public sealed class ModifyPathCommand : LayerCommandBase<Path>
    {
        public ModifyPathCommand(long id, Path target, PathNode[] nodes, 
            int index, NodeOperation operation) : base(id, new[] { target })
        {
            if(operation != NodeOperation.Add)
                throw new InvalidOperationException();

            Nodes = nodes;
            Index = index;
            Operation = operation;
        }

        public ModifyPathCommand(long id, Path target, PathNode[] nodes, 
            NodeOperation operation) : base(id, new[] { target })
        {
            if (operation != NodeOperation.Remove)
                throw new InvalidOperationException();

            Nodes = nodes;
            Operation = operation;
        }

        public ModifyPathCommand(long id, Path target, PathNode[] nodes,
            Vector2 delta, NodeOperation operation) : base(id, new[] { target })
        {
            if (operation != NodeOperation.Move &&
                operation != NodeOperation.MoveHandle1 &&
                operation != NodeOperation.MoveHandle2)
                throw new InvalidOperationException();

            if(nodes.Length > 1 &&
               operation != NodeOperation.MoveHandle1 &&
               operation != NodeOperation.MoveHandle2)
                throw new ArgumentException("Can only move one handle at a time.");

            Nodes = nodes;
            Delta = delta;
            Operation = operation;
        }

        public PathNode[] Nodes { get; }

        public int Index { get; }

        public Vector2 Delta { get; }

        public NodeOperation Operation { get; }

        public override void Do(ArtView artView)
        {
            var target = Targets[0];

            switch (Operation)
            {
                case NodeOperation.Add:
                    target.Nodes.InsertItems(Nodes, Index);
                    break;
                case NodeOperation.Remove:
                    target.Nodes.RemoveItems(Nodes);
                    break;
                case NodeOperation.Move:
                    foreach (var node in Nodes)
                        node.Position += Delta;
                    break;
                case NodeOperation.MoveHandle1:
                {
                    // braces to avoid variable scope issues for
                    // cubic
                    if (Nodes[0] is CubicPathNode cubic)
                        cubic.Control1 += Delta;
                    else if (Nodes[0] is QuadraticPathNode quadratic)
                        quadratic.Control += Delta;
                }
                    break;
                case NodeOperation.MoveHandle2:
                {
                    if (Nodes[0] is CubicPathNode cubic)
                        cubic.Control2 += Delta;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Undo(ArtView artView)
        {
            var target = Targets[0];

            switch (Operation)
            {
                case NodeOperation.Add:
                    target.Nodes.RemoveItems(Nodes);
                    break;
                case NodeOperation.Remove:
                    target.Nodes.InsertItems(Nodes, Index);
                    break;
                case NodeOperation.Move:
                    foreach (var node in Nodes)
                        node.Position -= Delta;
                    break;
                case NodeOperation.MoveHandle1:
                {
                    // braces to avoid variable scope issues for
                    // cubic
                    if (Nodes[0] is CubicPathNode cubic)
                        cubic.Control1 -= Delta;
                    else if (Nodes[0] is QuadraticPathNode quadratic)
                        quadratic.Control -= Delta;
                }
                    break;
                case NodeOperation.MoveHandle2:
                {
                    if (Nodes[0] is CubicPathNode cubic)
                        cubic.Control2 -= Delta;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string Description
        {
            get
            {
                switch (Operation)
                {
                    case NodeOperation.Add:
                        return $"Added {Nodes.Length} node(s)";
                    case NodeOperation.Remove:
                        return $"Removed {Nodes.Length} node(s)";
                    case NodeOperation.Move:
                        return $"Moved {Nodes.Length} node(s)";
                    case NodeOperation.MoveHandle1:
                    case NodeOperation.MoveHandle2:
                        return "Modified node handle";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public enum NodeOperation { Add, Remove, Move, MoveHandle1, MoveHandle2 }
    }

    public sealed class AddLayerCommand : LayerCommandBase<IContainerLayer>
    {
        public AddLayerCommand(long id, IContainerLayer target, ILayer layer) : base(id, new[] {target})
        {
            Layer = layer;
        }

        public override string Description => $"Added {Layer.DefaultName}";

        public ILayer Layer { get; }

        public override void Do(ArtView artView)
        {
            Targets[0].Add(Layer as Layer);
        }

        public override void Undo(ArtView artView)
        {
            Targets[0].Remove(Layer as Layer);
        }
    }

    public sealed class RemoveLayerCommand : LayerCommandBase<IContainerLayer>
    {
        private int _index;

        public RemoveLayerCommand(long id, IContainerLayer target, ILayer layer) : base(id, new[] {target})
        {
            Layer = layer;
        }

        public override string Description => $"Removed {Layer.DefaultName}";

        public ILayer Layer { get; }

        public override void Do(ArtView artView)
        {
            _index = Targets[0].SubLayers.IndexOf(Layer as Layer);
            Targets[0].Remove(Layer as Layer);
        }

        public override void Undo(ArtView artView)
        {
            Targets[0].Add(Layer as Layer, _index);
        }
    }

    public sealed class ChangeZIndexCommand : LayerCommandBase<ILayer>
    {
        public ChangeZIndexCommand(long id, ILayer[] targets, int delta) : base(id, targets)
        {
            Delta = delta;
        }

        public int Delta { get; }

        public override string Description => $"Changed z-index of {Targets.Length} layer(s)";

        public override void Do(ArtView artView)
        {
            foreach (var target in Targets)
            {
                var siblings = target.Parent.SubLayers;
                var index = siblings.IndexOf(target as Layer);
                siblings.Move(index, MathUtils.Clamp(0, siblings.Count - 1, index + Delta));
            }
        }

        public override void Undo(ArtView artView)
        {
            foreach (var target in Targets)
            {
                var siblings = target.Parent.SubLayers;
                var index = siblings.IndexOf(target as Layer);
                siblings.Move(index, MathUtils.Clamp(0, siblings.Count - 1, index - Delta));
            }
        }
    }

    public sealed class BinaryOperationCommand : LayerCommandBase<IGeometricLayer>
    {
        private IGeometricLayer _operand1;
        private IGeometricLayer _operand2;
        private IContainerLayer _parent1;
        private IContainerLayer _parent2;
        private Path _product;

        public BinaryOperationCommand(long id, IGeometricLayer[] targets, CombineMode operation) : base(id, targets)
        {
            if (targets.Length != 2)
                throw new ArgumentException("Binary operations can only have 2 operands.");
            Operation = operation;
        }

        public override string Description => Operation.ToString();

        public CombineMode Operation { get; }

        public override void Do(ArtView artView)
        {
            if (_product == null)
            {
                _operand1 = Targets[0];
                _operand2 = Targets[1];
                var factory = artView.Direct2DFactory;

                var xg = artView.CacheManager.GetGeometry(_operand1);
                var yg = artView.CacheManager.GetGeometry(_operand2);

                var z = new Path
                {
                    FillBrush = _operand1.FillBrush,
                    StrokeBrush = _operand1.StrokeBrush,
                    StrokeInfo = _operand1.StrokeInfo
                };

                var zSink = z.Open();

                using (var xtg = new TransformedGeometry(factory, xg, _operand1.AbsoluteTransform))
                {
                    xtg.Combine(yg, Operation, _operand2.AbsoluteTransform, 0.25f, zSink);
                }

                zSink.Close();

                (z.Scale, z.Rotation, z.Position, z.Shear) =
                    Matrix3x2.Invert(_operand1.WorldTransform).Decompose();

                _product = z;
                _parent1 = _operand1.Parent;
                _parent2 = _operand2.Parent;
            }

            _parent1.Add(_product);
            _parent1.Remove(_operand1 as Layer);
            _parent2.Remove(_operand2 as Layer);
        }

        public override void Undo(ArtView artView)
        {
            _product.Parent.Remove(_product);
            _parent1.Add(_operand1 as Layer);
            _parent2.Add(_operand2 as Layer);
        }
    }
}