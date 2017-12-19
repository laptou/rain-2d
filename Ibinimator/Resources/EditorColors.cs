using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model;

namespace Ibinimator.Resources
{
    internal static class EditorColors
    {
        public static readonly Color ArtSpace = new Color(0.5f);
        public static readonly Color Artboard = new Color(1.0f);

        public static readonly Color SelectionOutline       = new Color(0.345f, 0.831f, 0.196f); // #58D432
        public static readonly Color SelectionHandle        = new Color(1f); // #FFFFFF
        public static readonly Color SelectionHandleOutline = new Color(0.345f, 0.831f, 0.196f); // #58D432
        public static readonly Color GradientHandleSelected = new Color(0.565f, 0.816f, 0.988f); // #90d0fc

        public static readonly Color Node           = new Color(1f); // #FFFFFF
        public static readonly Color NodeHover      = new Color(0.673f, 0.916f, 0.598f); // #ACEA98
        public static readonly Color NodeSelected   = new Color(0.990f, 0.738f, 0.565f); // #FCBC90
        public static readonly Color NodeClick      = new Color(0.345f, 0.831f, 0.196f); // #58D432 
        public static readonly Color NodeOutline    = new Color(0.345f, 0.831f, 0.196f); // #58D432
        public static readonly Color NodeOutlineAlt = new Color(0.980f, 0.475f, 0.129f); // #FA7921

        public static readonly Color Guide = new Color(0.357f, 0.753f, 0.922f); // #5BC0EB

        public static readonly Color TextHighlight = new Color(0.345f, 0.831f, 0.196f, 0.5f); // #58D432 
        public static readonly Color TextCaret     = new Color(0f); // #000000 
    }
}