using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public abstract class Resource : Model.Model
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