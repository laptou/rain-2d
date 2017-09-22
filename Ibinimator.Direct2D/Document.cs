using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Ibinimator.Core;
using Ibinimator.Shared;

namespace Ibinimator.Direct2D
{
    public class Document : Model
    {
        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        public string Path
        {
            get => Get<string>();
            set => Set(value);
        }

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

        public event PropertyChangedEventHandler Updated;

        private void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Updated?.Invoke(sender, e);
        }
    }
}