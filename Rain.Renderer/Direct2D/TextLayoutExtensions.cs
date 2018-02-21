using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SharpDX.DirectWrite;

namespace Rain.Renderer.Direct2D
{
    internal static class TextLayoutExtensions
    {
        public static void SetFormat(
            this TextLayout layout, TextRange range, Action<TextRenderer.Format> callback)
        {
            var current = range.StartPosition;
            var end = range.StartPosition + range.Length;

            while (current < end)
            {
                var specifier =
                    layout.GetDrawingEffect(current, out var currentRange) as TextRenderer.Format;

                specifier = specifier == null ? new TextRenderer.Format() : specifier.Clone();

                callback(specifier);

                if (currentRange.Length < 0)
                {
                    layout.SetDrawingEffect(specifier, new TextRange(current, 1));
                    current++;

                    continue;
                }

                var currentEnd = currentRange.StartPosition + currentRange.Length;
                var currentLength = Math.Min(currentEnd, end) - current;

                layout.SetDrawingEffect(specifier, new TextRange(current, currentLength));

                current += currentLength;
            }
        }
    }
}