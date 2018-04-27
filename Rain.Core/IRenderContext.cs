using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Effects;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Imaging;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;

namespace Rain.Core
{
    public interface IRenderContext : IDisposable
    {
        float Height { get; }
        float Width { get; }

        void Begin(object ctx);
        void Clear(Color color);
        ISolidColorBrush CreateBrush(Color color);

        ILinearGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops, float startX, float startY, float endX, float endY);

        IRadialGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops, float centerX, float centerY, float radiusX,
            float radiusY, float focusX, float focusY);

        T CreateEffect<T>() where T : class, IEffect;
        IEffectLayer CreateEffectLayer();
        IGeometry CreateEllipseGeometry(float cx, float cy, float rx, float ry);
        IFontSource CreateFontSource();
        IGeometry CreateGeometry();
        IGeometry CreateGeometryGroup(params IGeometry[] geometries);
        IPen CreatePen(float width, IBrush brush);
        IPen CreatePen(float width, IBrush brush, IEnumerable<float> dashes);

        IPen CreatePen(
            float width, IBrush brush, IEnumerable<float> dashes, float dashOffset, LineCap lineCap,
            LineJoin lineJoin, float miterLimit);

        IGeometry CreateRectangleGeometry(float x, float y, float w, float h);
        ITextLayout CreateTextLayout();
        void DrawBitmap(IRenderImage bitmap);
        void DrawBitmap(IRenderImage bitmap, RectangleF dstRect, ScaleMode scaleMode);
        void DrawCircle(float cx, float cy, float r, IPen pen);
        void DrawCircle(Vector2 c, float r, IPen pen);
        void DrawEffectLayer(IEffectLayer layer);
        void DrawEllipse(float cx, float cy, float rx, float ry, IPen pen);
        void DrawEllipse(Vector2 c, float rx, float ry, IPen pen);

        void DrawEllipse(
            float centerX, float centerY, float radiusX, float radiusY, IPen pen, float penWidth);

        void DrawGeometry(IGeometry geometry, IPen pen);
        void DrawGeometry(IGeometry geometry, IPen pen, float width);
        void DrawLine(Vector2 v1, Vector2 v2, IPen pen);
        void DrawRectangle(float left, float top, float width, float height, IPen pen);
        void DrawRectangle(RectangleF rectangleF, IPen pen, float penWidth);
        void DrawRectangle(RectangleF rect, IPen pen);
        void DrawRectangle(Vector2 center, Vector2 radii, IPen pen);
        void End();
        void FillCircle(float cx, float cy, float r, IBrush brush);
        void FillCircle(Vector2 c, float r, IBrush brush);
        void FillEllipse(float cx, float cy, float rx, float ry, IBrush brush);
        void FillEllipse(Vector2 c, float rx, float ry, IBrush brush);
        void FillGeometry(IGeometry geometry, IBrush brush);
        void FillRectangle(float left, float top, float width, float height, IBrush brush);
        void FillRectangle(RectangleF rect, IBrush brush);
        void FillRectangle(Vector2 center, Vector2 radii, IBrush brush);
        float GetDpi();
        IRenderImage GetRenderImage(IImageFrame image);
        IRenderImage GetRenderImage(IImageFrame image, Vector2 scale, ScaleMode mode);
        T Provide<T>();
        void Transform(Matrix3x2 transform, bool absolute = false);
    }
}