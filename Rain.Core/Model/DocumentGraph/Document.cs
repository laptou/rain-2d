using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Paint;
using Rain.Core.Utility;

namespace Rain.Core.Model.DocumentGraph
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

                Set(value, Updated);

                if (Root != null)
                {
                    Root.PropertyChanging += RootPropertyChanging;
                    Root.PropertyChanged += RootPropertyChanged;
                }
            }
        }

        public long Size => Path == null ? 0 : new FileInfo(Path).Length;

        public ObservableList<IBrushInfo> Swatches { get; set; } = new ObservableList<IBrushInfo>();

        public string Type => System.IO.Path.GetExtension(Path);

        public event PropertyChangedEventHandler Updated;

        public event PropertyChangingEventHandler Updating;

        private void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Updated?.Invoke(sender, e);
        }

        private void RootPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            Updating?.Invoke(sender, e);
        }
    }
}