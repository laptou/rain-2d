using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public abstract class Paint : Element
    {
        public static Reference<Paint> Parse(string input)
        {
            if (TryParse(input, out var paint)) return paint;

            throw new FormatException();
        }

        public static bool TryParse(string input, out Reference<Paint> paint)
        {
            if (Color.TryParse(input, out var color))
            {
                paint = new SolidColor(color);
                return true;
            }

            if (Iri.TryParse(input, out var iri))
            {
                paint = new Reference<Paint>(iri);
                return true;
            }

            paint = null;

            return false;
        }
    }

    public class Reference<T> where T : class, IElement
    {
        private readonly Iri _location;
        private readonly T _target;

        public Reference(T target)
        {
            _target = target;
        }

        public Reference(Iri location)
        {
            _location = location;
        }

        public T Resolve(SvgContext context)
        {
            if (_target != null) return _target;

            return context[_location.Id] as T;
        }

        public static implicit operator Reference<T>(T t)
        {
            return new Reference<T>(t);
        }
    }

    public class SolidColor : Paint
    {
        public override void FromXml(XElement element, SvgContext context)
        {
            throw new NotImplementedException();
        }

        public override XElement ToXml(SvgContext svgContext)
        {
            throw new NotImplementedException();
        }

        public SolidColor()
        {
            
        }

        public SolidColor(Color color)
        {
            Color = color;
        }

        public Color Color { get; set; }

        public override string ToString()
        {
            return Color.ToString();
        }
    }

    public class AppColor : Paint
    {
        
    }
}