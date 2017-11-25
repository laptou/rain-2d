using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Ibinimator.Core.Utility;

namespace Ibinimator.Renderer.Model
{
    public class Document : Core.Model.Model
    {
        public RectangleF Bounds
        {
            get => Get<RectangleF>();
            set => Set(value);
        }

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

        public long Size => Path == null ? 0 : new FileInfo(Path).Length;

        public ObservableList<BrushInfo> Swatches { get; set; }
            = new ObservableList<BrushInfo>();

        public string Type => System.IO.Path.GetExtension(Path);

        public event PropertyChangedEventHandler Updated;

        private void RootPropertyChanged(object sender, PropertyChangedEventArgs e) { Updated?.Invoke(sender, e); }
    }
}