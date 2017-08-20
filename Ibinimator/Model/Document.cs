using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using SharpDX;
using IO = System.IO;

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

        [XmlIgnore]
        public string Path
        {
            get => Get<string>();
            set => Set(value);
        }

        [XmlElement("Layer")]
        public Layer Root
        {
            get => Get<Layer>();
            set
            {
                if(Root != null)
                    Root.PropertyChanged -= RootPropertyChanged;

                Set(value);

                if(Root != null)
                    Root.PropertyChanged += RootPropertyChanged;
            }
        }

        private void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Updated?.Invoke(Root, e);
        }

        public static event PropertyChangedEventHandler Updated;

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

        [XmlElement]
        public ObservableCollection<Color4> Swatches { get; set; }
            = new ObservableCollection<Color4>();

        public string Name => IO.Path.GetFileNameWithoutExtension(Path);

        public string Type => IO.Path.GetExtension(Path);

        public long Size => new FileInfo(Path).Length;
    }
}