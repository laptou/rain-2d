using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ibinimator.View.Control
{
    /// <inheritdoc />
    /// <summary>
    /// Interaction logic for SvgImage.xaml
    /// </summary>
    public partial class SvgImage : UserControl
    {
        public SvgImage()
        {
            InitializeComponent();
        }

        public Uri Source { get; set; }

        /// <summary>Gets or sets a value that describes how an <see cref="T:System.Windows.Controls.Image" /> should be stretched to fill the destination rectangle.  </summary>
        /// <returns>One of the <see cref="T:System.Windows.Media.Stretch" /> values. The default is <see cref="F:System.Windows.Media.Stretch.Uniform" />.</returns>
        public Stretch Stretch { get; set; }

        /// <summary>Gets or sets a value that indicates how the image is scaled.  </summary>
        /// <returns>One of the <see cref="T:System.Windows.Controls.StretchDirection" /> values. The default is <see cref="F:System.Windows.Controls.StretchDirection.Both" />.</returns>
        public StretchDirection StretchDirection { get; set; }
    }
}
