using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.DirectWrite;

namespace Ibinimator.Service.Commands
{
    /// <summary>
    /// This is an interface for "operation commands", which are different 
    /// from WPF's UI commands. Operation commands are use to store undo
    /// state information.
    /// </summary>
    public interface IOperationCommand
    {
        object[] Targets { get; }

        string Description { get; }

        long Id { get; }

        long Time { get; }

        void Undo();

        void Do();
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

        object[] IOperationCommand.Targets => Targets;

        public T[] Targets { get; }

        public abstract string Description { get; }

        public long Id { get; }

        public long Time { get; } = Service.Time.Now;

        public abstract void Undo();

        public abstract void Do();
    }

    public sealed class TransformCommand : LayerCommandBase<ILayer>
    {
        public override string Description => $"Transformed {Targets.Length} layer(s)";

        public Matrix3x2 Transform { get; }

        public override void Undo()
        {
            foreach (var layer in Targets)
            {
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

        public override void Do()
        {
            foreach (var layer in Targets)
            {
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
        }

        public TransformCommand(long id, ILayer[] targets, Matrix3x2 matrix) : base(id, targets)
        {
            Transform = matrix;
        }
    }

    public sealed class ApplyFillCommand : LayerCommandBase<IFilledLayer>
    {
        public BrushInfo[] OldFills { get; }

        public BrushInfo NewFill { get; }

        public override string Description => $"Filled {Targets.Length} layer(s)";

        public override void Undo()
        {
            for (var i = 0; i < Targets.Length; i++)
                lock(Targets[i])
                    Targets[i].FillBrush = OldFills[i];
        }

        public override void Do()
        {
            foreach (var target in Targets)
                lock (target)
                    target.FillBrush = NewFill;
        }

        public ApplyFillCommand(long id, IFilledLayer[] targets, 
            BrushInfo @new, BrushInfo[] old) : base(id, targets)
        {
            OldFills = old.Select(o => (BrushInfo)o?.Clone()).ToArray();
            NewFill = (BrushInfo)@new?.Clone();
        }
    }

    public sealed class ApplyStrokeCommand : LayerCommandBase<IStrokedLayer>
    {
        public (BrushInfo, StrokeInfo)[] OldStrokes { get; }

        public (BrushInfo, StrokeInfo) NewStroke { get; }

        public override string Description => $"Stroked {Targets.Length} layer(s)";

        public override void Undo()
        {
            for (var i = 0; i < Targets.Length; i++)
                lock(Targets[i])
                    (Targets[i].StrokeBrush, Targets[i].StrokeInfo) = OldStrokes[i];
        }

        public override void Do()
        {
            foreach (var target in Targets)
                lock(target)
                    (target.StrokeBrush, target.StrokeInfo) = NewStroke;
        }
        
        public ApplyStrokeCommand(long id, IStrokedLayer[] targets,
            BrushInfo newStrokeBrush, BrushInfo[] oldStrokeBrushes,
            StrokeInfo newStrokeInfo, StrokeInfo[] oldStrokeInfos) : base(id, targets)
        {
            OldStrokes = 
                Enumerable.Zip(
                    oldStrokeBrushes, 
                    oldStrokeInfos, 
                    (b, i) => (b?.Clone<BrushInfo>(), i?.Clone<StrokeInfo>())).ToArray();

            NewStroke = (newStrokeBrush?.Clone<BrushInfo>(), newStrokeInfo?.Clone<StrokeInfo>());
        }
    }

    public sealed class ApplyFormatCommand : LayerCommandBase<ITextLayer>
    {
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

        public override string Description => $"Formatted {Targets.Length} layer(s)";

        public override void Undo()
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

        public override void Do()
        {
            foreach (var target in Targets)
            {
                lock (target)
                {
                    target.FontFamilyName = NewFontFamilyName;
                    target.FontSize = NewFontSize;
                    target.FontStretch = NewFontStretch;
                    target.FontStyle = NewFontStyle;
                    target.FontWeight = NewFontWeight;
                }
            }
        }
        
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
    }

    public sealed class ApplyFormatRangeCommand : LayerCommandBase<ITextLayer>
    {
        public Format[] NewFormats { get; }

        public Format[] OldFormats { get; }

        public override string Description => $"Changed format of range";

        public override void Undo()
        {
            foreach (var target in Targets)
            {
                target.ClearFormat();

                foreach (var format in OldFormats)
                    target.SetFormat(format);
            }
        }

        public override void Do()
        {
            foreach (var target in Targets)
            {
                target.ClearFormat();

                foreach (var format in NewFormats)
                    target.SetFormat(format);
            }
        }

        public ApplyFormatRangeCommand(long id,
            ITextLayer target,
            Format[] oldFormat,
            Format[] newFormat) : base(id, new[] {target})
        {
            OldFormats = oldFormat;
            NewFormats = newFormat;
        }
    }

    public sealed class InsertTextCommand : LayerCommandBase<ITextLayer>
    {
        public InsertTextCommand(long id, ITextLayer target, string text, int index) 
            : base(id, new[] { target })
        {
            Text = text;
            Index = index;
        }

        public string Text { get; }

        public int Index { get; }

        public override string Description => $@"Inserted text ""{Text}""";

        public override void Undo()
        {
            foreach (var target in Targets)
                target.Remove(Index, Text.Length);
        }

        public override void Do()
        {
            foreach (var target in Targets)
                target.Insert(Index, Text);
        }
    }

    public sealed class RemoveTextCommand : LayerCommandBase<ITextLayer>
    {
        public RemoveTextCommand(long id, ITextLayer target, string text, int index)
            : base(id, new[] { target })
        {
            Text = text;
            Index = index;
        }

        public string Text { get; }

        public int Index { get; }

        public override string Description => $@"Removed text ""{Text}""";

        public override void Undo()
        {
            foreach (var target in Targets)
                target.Insert(Index, Text);
        }

        public override void Do()
        {
            foreach (var target in Targets)
                target.Remove(Index, Text.Length);
        }
    }
}
