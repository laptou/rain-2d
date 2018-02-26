using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Imaging;
using Rain.Core.Model.Paint;

namespace Rain.Core
{
    public interface ICacheManager : IArtContextManager
    {
        #region Retrieval

        IRenderImage GetBitmap(string key);
        IBrush GetBrush(string key);
        IBrush GetFill(IFilledLayer layer);
        IRenderImage GetImage(IImageLayer layer);
        IGeometry GetGeometry(IGeometricLayer layer);
        IPen GetStroke(IStrokedLayer layer);
        ITextLayout GetTextLayout(ITextLayer layer);

        #endregion

        void LoadBitmaps(RenderContext target);
        void LoadBrushes(RenderContext target);

        void SuppressInvalidation();
        void RestoreInvalidation();

        #region Resource Management

        void ReleaseDeviceResources();
        void ReleaseResources();
        void ReleaseSceneResources();

        #endregion

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