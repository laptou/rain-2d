using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model.DocumentGraph
{
    public interface ILayer : INotifyPropertyChanged, INotifyPropertyChanging, IRenderable
    {
        /// <summary>
        ///     Gets the world transform multiplied by the local transform: the total transform applied
        ///     to this object.
        /// </summary>
        Matrix3x2 AbsoluteTransform { get; }

        IGeometricLayer Clip { get; set; }

        /// <summary>
        ///     Gets the name of the layer to be displayed if the layer doesn't have a name.
        /// </summary>
        string DefaultName { get; }

        /// <summary>
        ///     Gets the depth in the hierarchy at which this layer is present.
        /// </summary>
        int Depth { get; }

        float Height { get; set; }

        /// <summary>
        ///     Gets the id of the layer.
        /// </summary>
        Guid Id { get; }

        ILayer Mask { get; set; }

        /// <summary>
        ///     Gets the name of the layer.
        /// </summary>
        string Name { get; set; }

        float Opacity { get; set; }

        /// <summary>
        ///     Gets the 0-based index at which this layer will be drawn (i.e., 0 means
        ///     that this will be the layer that renders at the bottom).
        /// </summary>
        int Order { get; }

        IContainerLayer Parent { get; set; }
        bool Selected { get; set; }

        /// <summary>
        ///     Gets the number of steps that this layer adds to the Order of layers above it.
        /// </summary>
        int Size { get; }

        Matrix3x2 Transform { get; }

        /// <summary>
        ///     Gets/sets whether this layer is visible in the editor.
        /// </summary>
        bool Visible { get; set; }

        float Width { get; set; }
        Matrix3x2 WorldTransform { get; }

        event EventHandler BoundsChanged;

        void ApplyTransform(Matrix3x2? local = null, Matrix3x2? global = null);

        IEnumerable<ILayer> Flatten();
        IEnumerable<ILayer> Flatten(int depth);
        RectangleF GetBounds(ICacheManager cache);

        T HitTest<T>(ICacheManager cache, Vector2 point, int minimumDepth) where T : ILayer;
    }
}