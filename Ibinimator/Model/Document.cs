using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Ibinimator.Shared;

namespace Ibinimator.Model
{
    public class Document : Model
    {
        public Document()
        {
            LengthUnit = Unit.Pixels;
            AngleUnit = Unit.Degrees;
            TimeUnit = Unit.Frames;
        }

        [XmlAttribute]
        public Unit AngleUnit
        {
            get => Get<Unit>();
            set
            {
                if (value.GetUnitType() == UnitType.Angle)
                    Set(value);
                else
                    throw new ArgumentOutOfRangeException(nameof(value), "Not an angular unit.");
            }
        }

        [XmlAttribute]
        public Unit LengthUnit
        {
            get => Get<Unit>();
            set
            {
                if (value.GetUnitType() == UnitType.Length)
                    Set(value);
                else
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Not a length unit.");
            }
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

        public long Size => new FileInfo(Path).Length;

        public ObservableList<BrushInfo> Swatches { get; set; }
            = new ObservableList<BrushInfo>();

        [XmlAttribute]
        public Unit TimeUnit
        {
            get => Get<Unit>();
            set
            {
                if (value == Unit.Frames || value == Unit.Milliseconds)
                    Set(value);
                else
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Not a smallest-denomination Time unit.");
            }
        }

        public string Type => System.IO.Path.GetExtension(Path);

        public event PropertyChangedEventHandler Updated;

        private void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Updated?.Invoke(sender, e);
        }
    }
}