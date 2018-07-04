using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;

namespace Rain.Core
{
    public interface IViewManager : IArtContextManager
    {
        Document Document { get; set; }
        Vector2 Pan { get; set; }
        IContainerLayer Root { get; set; }
        Matrix3x2 Transform { get; }
        float Zoom { get; set; }

        void Render(IRenderContext target, ICacheManager cache);

        #region Updates

        event PropertyChangedEventHandler RootUpdated;
        event PropertyChangedEventHandler DocumentUpdated;

        #endregion

        #region Transformations

        Vector2 FromArtSpace(Vector2 v);
        RectangleF FromArtSpace(RectangleF v);
        Vector2 ToArtSpace(Vector2 v);
        RectangleF ToArtSpace(RectangleF v);

        #endregion
    }
}