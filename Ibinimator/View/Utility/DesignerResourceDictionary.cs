using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Ibinimator.View.Utility
{
    public class DesignerTimeResourceDictionary : ResourceDictionary
    {
        /// <summary>
        ///     Local field storing info about designtime source.
        /// </summary>
        private Uri designTimeSource;

        /// <summary>
        ///     Gets or sets the design time source.
        /// </summary>
        /// <value>
        ///     The design time source.
        /// </value>
        public Uri DesignTimeSource
        {
            get => designTimeSource;

            set
            {
                designTimeSource = value;
                if (App.IsDesigner)
                    base.Source = value;
            }
        }

        /// <summary>
        ///     Gets or sets the uniform resource identifier (URI) to load resources from.
        /// </summary>
        /// <returns>The source location of an external resource dictionary. </returns>
        public new Uri Source
        {
            get => throw new Exception("Use DesignTimeSource instead Source!");

            set => throw new Exception("Use DesignTimeSource instead Source!");
        }
    }
}