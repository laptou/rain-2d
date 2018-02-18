﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model.Text
{
    public class TextInfo : Model, ITextInfo
    {
        #region ITextInfo Members

        /// <inheritdoc />
        public float Baseline
        {
            get => Get<float>();
            set => Set(value);
        }

        /// <inheritdoc />
        public string FontFamily
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <inheritdoc />
        public float FontSize
        {
            get => Get<float>();
            set => Set(value);
        }

        /// <inheritdoc />
        public FontStretch FontStretch
        {
            get => Get<FontStretch>();
            set => Set(value);
        }

        /// <inheritdoc />
        public FontStyle FontStyle
        {
            get => Get<FontStyle>();
            set => Set(value);
        }

        /// <inheritdoc />
        public FontWeight FontWeight
        {
            get => Get<FontWeight>();
            set => Set(value);
        }

        #endregion
    }
}