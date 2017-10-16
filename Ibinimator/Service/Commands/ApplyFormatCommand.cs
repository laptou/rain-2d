using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Commands
{
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

        public override void Do(IArtContext artView)
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

        public override void Undo(IArtContext artView)
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
}