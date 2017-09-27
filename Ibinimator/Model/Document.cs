using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Mathematics.Interop;

namespace Ibinimator.Model
{
    public class Document : Model
    {
        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        [XmlIgnore]
        public string Path
        {
            get => Get<string>();
            set => Set(value);
        }

        [XmlElement("Layer")]
        public Group Root
        {
            get => Get<Group>();
            set
            {
                if (Root != null)
                    Root.PropertyChanged -= RootPropertyChanged;

                Set(value);

                if (Root != null)
                    Root.PropertyChanged += RootPropertyChanged;
            }
        }

        public long Size => new FileInfo(Path).Length;

        public ObservableList<BrushInfo> Swatches { get; set; }
            = new ObservableList<BrushInfo>();

        public string Type => System.IO.Path.GetExtension(Path);

        public RectangleF Bounds
        {
            get => Get<RectangleF>();
            set => Set(value);
        }

        public event PropertyChangedEventHandler Updated;

        private void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Updated?.Invoke(sender, e);
        }
    }
}