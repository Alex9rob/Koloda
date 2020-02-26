using Foundation;
using Koloda;
using Koloda.DraggableCardView;
using Koloda.OverlayView;
using System;
using System.Collections.Generic;
using UIKit;

namespace KolodaXamarin
{
    public partial class ViewController : UIViewController, IKolodaViewDelegate, IKolodaViewDataSource
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            _btnLeft.TouchUpInside += _btnLeft_TouchUpInside;
            _btnRight.TouchUpInside += _btnRight_TouchUpInside;
            _btnUndo.TouchUpInside += _btnUndo_TouchUpInside;
        }

        private void _btnLeft_TouchUpInside(object sender, EventArgs e)
        {
            _kolodaView.Swipe(ESwipeResultDirection.left);
        }

        private void _btnRight_TouchUpInside(object sender, EventArgs e)
        {
            _kolodaView.Swipe(ESwipeResultDirection.right);
        }

        private void _btnUndo_TouchUpInside(object sender, EventArgs e)
        {
            _kolodaView.RevertAction();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.
            View.BackgroundColor = UIColor.Gray;
            _kolodaView.BackgroundColor = UIColor.LightGray;
            _kolodaView.dataSource = this;
            _kolodaView.@delegate = this;

            ModalTransitionStyle = UIModalTransitionStyle.FlipHorizontal;
        }

        // MARK: KolodaViewDelegate

        void IKolodaViewDelegate.kolodaDidRunOutOfCards(KolodaView koloda)
        {
        }

        void IKolodaViewDelegate.kolodaDidSelectCardAt(KolodaView koloda, int index)
        {
            UIApplication.SharedApplication.OpenUrl(new NSUrl("https://yalantis.com/"));
        }

        // MARK: KolodaViewDataSource
        int IKolodaViewDataSource.kolodaNumberOfCards(KolodaView koloda)
        {
            return 7;
        }

        DragSpeed IKolodaViewDataSource.kolodaSpeedThatCardShouldDrag(KolodaView koloda)
        {
            return DragSpeed.@default;
        }

        UIView IKolodaViewDataSource.kolodaViewForCardAt(KolodaView koloda, int index)
        {
            if (index % 2 != 0)
            {
                var view = new UIView();
                view.BackgroundColor = GetUIColor(index);
                return view;
            }
            else
            {
                return GetUIImageView(index);
            }          
        }

        OverlayView IKolodaViewDataSource.kolodaViewForCardOverlayAt(KolodaView koloa, int index)
        {
            return new OverlayView();
            //return NSBundle.MainBundle.LoadNib("OverlayView", this, null).GetItem<OverlayView>(0);            
        }

        private UIColor GetUIColor(int index)
        {
            var colors = new List<UIColor>
            {
                UIColor.Red,
                UIColor.Orange,
                UIColor.Yellow,
                UIColor.Green,
                UIColor.Cyan,
                UIColor.Blue,
                UIColor.Purple
            };
            index = index % colors.Count;

            return colors[index];
        }

        private UIImageView GetUIImageView(int index)
        {
            var images = new List<UIImage>
            {
                UIImage.FromBundle("Card_like_1"),
                UIImage.FromBundle("Card_like_2"),
                UIImage.FromBundle("Card_like_3"),
                UIImage.FromBundle("Card_like_4"),
                UIImage.FromBundle("Card_like_5")
            };
            index = index % images.Count;
            return new UIImageView(images[index]);
        }
    }
}