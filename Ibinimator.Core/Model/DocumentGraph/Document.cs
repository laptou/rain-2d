using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Paint;
using Ibinimator.Core.Utility;

namespace Ibinimator.Core.Model.DocumentGraph
{
    public class Document : Model
    {
        public RectangleF Bounds
        {
            get => Get<RectangleF>();
            set => Set(value);
        }

        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        public string Path
        {
            get => Get<string>();
            set => Set(value);
        }

        public IContainerLayer Root
        {
            get => Get<IContainerLayer>();
            set
            {
                if (Root != null)
                {
                    Root.PropertyChanging -= RootPropertyChanging;
                    Root.PropertyChanged -= RootPropertyChanged;
                }

                Set(value);

                if (Root != null)
                {
                    Root.PropertyChanging += RootPropertyChanging;
                    Root.PropertyChanged += RootPropertyChanged;
                }
            }
        }

        public long Size => Path == null ? 0 : new FileInfo(Path).Length;

        public ObservableList<IBrushInfo> Swatches { get; set; }
            = new ObservableList<IBrushInfo>();

        public string Type => System.IO.Path.GetExtension(Path);

        public event PropertyChangedEventHandler Updated;

        private void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Updated?.Invoke(sender, e);
        }

        public event PropertyChangingEventHandler Updating;

        private void RootPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            Updating?.Invoke(sender, e);
        }
    }
}