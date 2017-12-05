using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Utility;

namespace Ibinimator.Service
{
    public class GuideManager
    {
        private readonly Dictionary<(int, GuideType), Guide> _guides =
            new Dictionary<(int, GuideType), Guide>();


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
                    var newDelta = MathUtils.Project(delta, MathUtils.Angle(g.Angle));
                    return (newDelta + origin, Vector2.Distance(newDelta + origin, position), g);
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

        public IEnumerable<Guide> GetGuides(GuideType type)
        {
            return _guides.Values.Where(g => g.Type.HasFlag(type)).ToList();
        }

        public void AddGuide(Guide guide) { _guides[(guide.Id, guide.Type)] = guide; }

        public void ClearVirtualGuides()
        {
            foreach (var guide in _guides.Where(g => g.Value.Virtual).ToList())
                _guides.Remove(guide.Key);
        }
    }
}