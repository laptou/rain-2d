﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Ibinimator.Core.Model;
using Ibinimator.Core.Model.Measurement;
using Ibinimator.Svg.Structure;

namespace Ibinimator.Svg.Paint {
    public class GradientStop : Element
    {
        public Color  Color  { get; set; }
        public Length Offset { get; set; }

        public float Opacity { get; set; } = 1;

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            Offset = LazyGet(element, "offset", Length.Zero);
            Opacity = LazyGet(element, "stop-opacity", 1f, true);
            Color = LazyGet<Color>(element, "stop-color", inherit: true);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Stop;

            LazySet(element, "offset", Offset);
            LazySet(element, "stop-opacity", Opacity * Color.Alpha, 1f);
            LazySet(element, "stop-color", Color);

            return element;
        }
    }
}