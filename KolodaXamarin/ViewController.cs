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

        private List<UIImage> dataSource = new List<UIImage>
        {
            UIImage.FromBundle("Card_like_1"),
            UIImage.FromBundle("Card_like_2"),
            UIImage.FromBundle("Card_like_3"),
            UIImage.FromBundle("Card_like_4"),
            UIImage.FromBundle("Card_like_5")
        };

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
            View.BackgroundColor = UIColor.Red;
            _kolodaView.BackgroundColor = UIColor.Blue;
            _kolodaView.dataSource = this;
            _kolodaView.@delegate = this;

            ModalTransitionStyle = UIModalTransitionStyle.FlipHorizontal;
        }

        // MARK: IBActions
        //@IBAction func leftButtonTapped()
        //{
        //    kolodaView?.swipe(.left)
        //}

        //@IBAction func rightButtonTapped()
        //{
        //    kolodaView?.swipe(.right)
        //}

        //@IBAction func undoButtonTapped()
        //{
        //    kolodaView?.revertAction()
        //}

        // MARK: KolodaViewDelegate

        void IKolodaViewDelegate.kolodaDidRunOutOfCards(KolodaView koloda)
        {
            var position = _kolodaView.currentCardIndex;
            dataSource.AddRange(new List<UIImage>
        {
            UIImage.FromBundle("Card_like_1"),
            UIImage.FromBundle("Card_like_2"),
            UIImage.FromBundle("Card_like_3"),
            UIImage.FromBundle("Card_like_4"),
            UIImage.FromBundle("Card_like_5")
        });

            var indexRange = new List<int>();
            for (int i = position; i < position + 3; i++)
            {
                indexRange.Add(i);
            }
            _kolodaView.insertCardAtIndexRange(indexRange, animated: true);
        }

        void IKolodaViewDelegate.kolodaDidSelectCardAt(KolodaView koloda, int index)
        {
            UIApplication.SharedApplication.OpenUrl(new NSUrl("https://yalantis.com/"));
        }
               
        // MARK: KolodaViewDataSource
        int IKolodaViewDataSource.kolodaNumberOfCards(KolodaView koloda)
        {
            return dataSource.Count;
        }

        DragSpeed IKolodaViewDataSource.kolodaSpeedThatCardShouldDrag(KolodaView koloda)
        {
            return DragSpeed.@default;
        }

        UIView IKolodaViewDataSource.kolodaViewForCardAt(KolodaView koloda, int index)
        {
            return new UIImageView(dataSource[index]);
        }


        OverlayView IKolodaViewDataSource.kolodaViewForCardOverlayAt(KolodaView koloda, int index)
        {
            return new OverlayView();
        }
    }
}