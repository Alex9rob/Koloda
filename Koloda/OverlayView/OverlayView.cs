using System;
using Foundation;
using UIKit;

namespace Koloda.OverlayView
{
    [Register("OverlayView")]
    public class OverlayView : UIView
    {
        public OverlayView()
        {
        }

        public OverlayView(IntPtr handle) : base(handle)
        {
        }

        public ESwipeResultDirection? OverlayState { get; set; }

        public void Update(float progress)
        {
            Alpha = progress;
        }
    }
}
