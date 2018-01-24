using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Utility;
using Ibinimator.Resources;
using Ibinimator.Service;

namespace Ibinimator.Renderer
{
    public class GuideManager
    {
        private readonly Dictionary<(int, GuideType), Guide> _guides =
            new Dictionary<(int, GuideType), Guide>();

        public bool VirtualGuidesActive => _guides.Any(g => g.Value.Virtual);

        public void ClearVirtualGuides()
        {
            foreach (var guide in _guides.Where(g => g.Value.Virtual).ToList())
                _guides.Remove(guide.Key);
        }

        public IEnumerable<Guide> GetGuides(GuideType type)
        {
            return _guides.Values.Where(g => g.Type.HasFlag(type)).ToList();
        }

        public (Vector2 Point, Guide? Guide) LinearSnap(Vector2 position, Vector2 origin)
        {
            return LinearSnap(position, origin, 0);
        }

        public (Vector2 Point, Guide? Guide) LinearSnap(
            Vector2 position,
            Vector2 origin,
            GuideType type)
        {
            var candidates = GetGuides(type);
            var delta = position - origin;

            var results = candidates
                         .Select<Guide, (Vector2 Position, float Loss, Guide? Guide)>(g =>
                                                                                      {
                                                                                          var newDelta =
                                                                                              MathUtils
                                                                                                 .Project(
                                                                                                      delta,
                                                                                                      MathUtils
                                                                                                         .Angle(
                                                                                                              g.Angle));

                                                                                          return (newDelta +
                                                                                                  origin,
                                                                                              Vector2
                                                                                                 .Distance(
                                                                                                      newDelta +
                                                                                                      origin,
                                                                                                      position)
                                                                                            , g);
                                                                                      })
                         .OrderBy(r => r.Loss)
                         .ToList();

            var result = results.Count > 0 ? results.First() : (position, 0, null);

            return (result.Position, result.Guide);
        }

        public (Vector2 Point, Guide? Guide) RadialSnap(
            Vector2 position,
            Vector2 origin,
            GuideType type)
        {
            var angle = MathUtils.Wrap(
                MathUtils.Angle(position - origin, false),
                MathUtils.Pi);

            var candidates = GetGuides(type);

            var results = candidates
                         .OrderBy(g => Math.Abs(MathUtils.Wrap(g.Angle, MathUtils.Pi) - angle))
                         .ToList();

            if (results.Count == 0) return (position, null);

            var result = results.First();

            var dist = MathUtils.Angle(result.Angle) *
                       Vector2.Distance(position, origin);

            if (Math.Abs(result.Angle) > MathUtils.PiOverTwo)
                dist = -dist;

            return (origin + dist, result);
        }

        public void Render(RenderContext target, ICacheManager cache, IViewManager view)
        {
            var fx = target.CreateEffect<IGlowEffect>();

            target.PushEffect(fx);

            foreach (var guide in GetGuides(GuideType.All))
            {
                var brush = cache.GetBrush(nameof(EditorColors.Guide));

                if (guide.Type.HasFlag(GuideType.Linear))
                {
                    var origin = guide.Origin;
                    var slope = Math.Tan(guide.Angle);
                    var diagonal = target.Height / target.Width;
                    Vector2 p1, p2;

                    if (slope > diagonal)
                    {
                        p1 = new Vector2(
                            (float) (origin.X + (origin.Y - target.Height) / slope),
                            target.Height);
                        p2 = new Vector2((float) (origin.X + origin.Y / slope), 0);
                    }
                    else
                    {
                        p1 = new Vector2(
                            target.Width,
                            (float) (origin.Y + (origin.X - target.Width) * slope));
                        p2 = new Vector2(0, (float) (origin.Y + origin.X * slope));
                    }

                    using (var pen = target.CreatePen(2, brush))
                    {
                        target.DrawLine(p1, p2, pen);
                    }
                }

                if (guide.Type.HasFlag(GuideType.Radial))
                {
                    var origin = guide.Origin;
                    var axes = new[]
                    {
                        guide.Angle,
                        guide.Angle + MathUtils.PiOverFour * 1,
                        guide.Angle + MathUtils.PiOverFour * 2,
                        guide.Angle + MathUtils.PiOverFour * 3
                    };

                    using (var pen = target.CreatePen(1, brush))
                    {
                        target.DrawEllipse(origin, 20, 20, pen);

                        foreach (var x in axes)
                            target.DrawLine(origin + MathUtils.Angle(x) * 20,
                                            origin - MathUtils.Angle(x) * 20,
                                            pen);
                    }

                    using (var pen = target.CreatePen(2, brush))
                    {
                        target.DrawLine(origin - MathUtils.Angle(-axes[2]) * 25,
                                        origin,
                                        pen);
                    }
                }
            }

            target.PopEffect();

            fx.Dispose();
        }

        public void SetGuide(Guide guide) { _guides[(guide.Id, guide.Type)] = guide; }
    }
}