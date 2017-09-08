  
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ibinimator.View
{
	public static class Icon 
	{
		public static ImageSource AlignBottom;
		public static ImageSource AlignCenterHorizontally;
		public static ImageSource AlignCenterVertically;
		public static ImageSource AlignLeft;
		public static ImageSource AlignRight;
		public static ImageSource AlignTop;
		public static ImageSource Backward;
		public static ImageSource Bucket;
		public static ImageSource Close;
		public static ImageSource Combine;
		public static ImageSource CornerBevel;
		public static ImageSource CornerMiter;
		public static ImageSource CornerRound;
		public static ImageSource Cursor;
		public static ImageSource Difference;
		public static ImageSource Divide;
		public static ImageSource EndFlat;
		public static ImageSource EndRound;
		public static ImageSource EndSquare;
		public static ImageSource Expander;
		public static ImageSource Eyedropper;
		public static ImageSource FlipHorizontally;
		public static ImageSource FlipVertically;
		public static ImageSource Forward;
		public static ImageSource Grid;
		public static ImageSource Intersection;
		public static ImageSource Keyframe;
		public static ImageSource Mask;
		public static ImageSource Minimize;
		public static ImageSource Pen;
		public static ImageSource Pencil;
		public static ImageSource Pointer;
		public static ImageSource ResizeEw;
		public static ImageSource ResizeNesw;
		public static ImageSource ResizeNs;
		public static ImageSource ResizeNwse;
		public static ImageSource Restore;
		public static ImageSource RotateCcw;
		public static ImageSource RotateCw;
		public static ImageSource Rotate;
		public static ImageSource Snap;
		public static ImageSource StrokeDash;
		public static ImageSource StrokeSolid;
		public static ImageSource Text;
		public static ImageSource ToBack;
		public static ImageSource ToFront;
		public static ImageSource Union;
		public static ImageSource Zoom;
		
		public static void Initialize() 
		{
			AlignBottom = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/align bottom.png", UriKind.Relative));
			AlignCenterHorizontally = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/align center horizontally.png", UriKind.Relative));
			AlignCenterVertically = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/align center vertically.png", UriKind.Relative));
			AlignLeft = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/align left.png", UriKind.Relative));
			AlignRight = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/align right.png", UriKind.Relative));
			AlignTop = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/align top.png", UriKind.Relative));
			Backward = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/backward.png", UriKind.Relative));
			Bucket = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/bucket.png", UriKind.Relative));
			Close = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/close.png", UriKind.Relative));
			Combine = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/combine.png", UriKind.Relative));
			CornerBevel = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/corner-bevel.png", UriKind.Relative));
			CornerMiter = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/corner-miter.png", UriKind.Relative));
			CornerRound = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/corner-round.png", UriKind.Relative));
			Cursor = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/cursor.png", UriKind.Relative));
			Difference = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/difference.png", UriKind.Relative));
			Divide = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/divide.png", UriKind.Relative));
			EndFlat = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/end-flat.png", UriKind.Relative));
			EndRound = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/end-round.png", UriKind.Relative));
			EndSquare = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/end-square.png", UriKind.Relative));
			Expander = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/expander.png", UriKind.Relative));
			Eyedropper = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/eyedropper.png", UriKind.Relative));
			FlipHorizontally = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/flip horizontally.png", UriKind.Relative));
			FlipVertically = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/flip vertically.png", UriKind.Relative));
			Forward = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/forward.png", UriKind.Relative));
			Grid = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/grid.png", UriKind.Relative));
			Intersection = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/intersection.png", UriKind.Relative));
			Keyframe = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/keyframe.png", UriKind.Relative));
			Mask = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/mask.png", UriKind.Relative));
			Minimize = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/minimize.png", UriKind.Relative));
			Pen = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/pen.png", UriKind.Relative));
			Pencil = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/pencil.png", UriKind.Relative));
			Pointer = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/pointer.png", UriKind.Relative));
			ResizeEw = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/resize-ew.png", UriKind.Relative));
			ResizeNesw = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/resize-nesw.png", UriKind.Relative));
			ResizeNs = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/resize-ns.png", UriKind.Relative));
			ResizeNwse = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/resize-nwse.png", UriKind.Relative));
			Restore = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/restore.png", UriKind.Relative));
			RotateCcw = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/rotate ccw.png", UriKind.Relative));
			RotateCw = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/rotate cw.png", UriKind.Relative));
			Rotate = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/rotate.png", UriKind.Relative));
			Snap = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/snap.png", UriKind.Relative));
			StrokeDash = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/stroke-dash.png", UriKind.Relative));
			StrokeSolid = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/stroke-solid.png", UriKind.Relative));
			Text = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/text.png", UriKind.Relative));
			ToBack = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/to back.png", UriKind.Relative));
			ToFront = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/to front.png", UriKind.Relative));
			Union = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/union.png", UriKind.Relative));
			Zoom = new BitmapImage(new Uri("/Ibinimator;component/Resources/Icon/zoom.png", UriKind.Relative));
			
		}
	}
}