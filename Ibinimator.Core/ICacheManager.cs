using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model;
using Ibinimator.Core.Model.DocumentGraph;
using Ibinimator.Core.Model.Geometry;
using Ibinimator.Core.Model.Paint;

namespace Ibinimator.Core
{
    public interface ICacheManager : IArtContextManager
    {
        IBitmap GetBitmap(string key);
        IBrush GetBrush(string key);
        IBrush GetFill(IFilledLayer layer);
        IGeometry GetGeometry(IGeometricLayer layer);

        IPen GetStroke(IStrokedLayer layer);
        ITextLayout GetTextLayout(ITextLayer text);

        void LoadBitmaps(RenderContext target);
        void LoadBrushes(RenderContext target);
        void ReleaseDeviceResources();

        void ReleaseResources();
        void ReleaseSceneResources();

        #region Layer Lifecycle

        /// <summary>
        ///     Generates device-dependent resources and binds events for the given layer
        ///     and all of its sub-layers.
        /// </summary>
        /// <param name="layer">The layer to bind to.</param>
        void BindLayer(ILayer layer);

        /// <summary>
        ///     Removes events for the given layer and all of its sub-layers.
        /// </summary>
        /// <param name="layer">The layer to unbind from.</param>
        void UnbindLayer(ILayer layer);

        #endregion


        #region Hitboxes

        /// <summary>
        ///     Gets the untransformed boundaries of the layer (the boundaries that
        ///     the layer would occupy if it existed in a vacuum).
        /// </summary>
        /// <param name="layer">The layer whose boundaries are to be retrieved.</param>
        /// <returns>The untransformed boundaries of the layer.</returns>
        RectangleF GetBounds(ILayer layer);

        /// <summary>
        ///     Gets the absolutely transformed boundaries of the layer (the boundaries that
        ///     the layer occupies when transformed using its own local transformation, then
        ///     the transformation of each of its ancestors). These are the final boundaries
        ///     of the layer in the viewport.
        /// </summary>
        /// <param name="layer">The layer whose boundaries are to be retrieved.</param>
        /// <returns>The absolutely transformed boundaries of the layer.</returns>
        RectangleF GetAbsoluteBounds(ILayer layer);

        /// <summary>
        ///     Gets the locally transformed boundaries of the layer (the boundaries that
        ///     the layer occupies when transformed using its own local transformation).
        /// </summary>
        /// <param name="layer">The layer whose boundaries are to be retrieved.</param>
        /// <returns>The locally transformed boundaries of the layer.</returns>
        RectangleF GetRelativeBounds(ILayer layer);

        #endregion
    }
}