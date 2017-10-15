using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public abstract class Resource : Model
    {
        #region ResoureScope enum

        public enum ResoureScope
        {
            Local,
            Document,
            Application
        }

        #endregion

        public ResoureScope Scope
        {
            get => Get<ResoureScope>();
            set => Set(value);
        }
    }
}