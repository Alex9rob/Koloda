using System;
using System.Collections.Generic;
using System.Linq;
using CoreAnimation;
using CoreGraphics;
using Facebook.Pop;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Koloda.DraggableCardView
{
    public enum DragSpeed
    {
        slow,
        moderate,
        @default,
        fast

    }

    public static class DragSpeedExtension
    {
        public static float GetDragSpeed(this DragSpeed dragSpeed)
        {
            switch (dragSpeed)
            {
                case DragSpeed.slow:
                    return 2.0f;
                case DragSpeed.moderate:
                    return 1.5f;
                case DragSpeed.@default:
                    return 0.8f;
                case DragSpeed.fast:
                    return 0.4f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dragSpeed), dragSpeed, null);
            }
        }
    }

    public interface IDraggableCardDelegate
    {
        void cardWasDraggedWithFinishPercentage(DraggableCardView card, float percentage,
            ESwipeResultDirection direction);

        void cardWasSwipedIn(DraggableCardView card, ESwipeResultDirection direction);
        bool cardShouldSwipeIn(DraggableCardView card, ESwipeResultDirection direction);
        void cardWasReset(DraggableCardView card);
        void cardWasTapped(DraggableCardView card);
        float? cardSwipeThresholdRatioMargin(DraggableCardView card);
        List<ESwipeResultDirection> cardAllowedDirections(DraggableCardView card);
        bool cardShouldDrag(DraggableCardView card);
        DragSpeed cardSwipeSpeedCard(DraggableCardView card);
        void cardPanBegan(DraggableCardView card);
        void cardPanFinished(DraggableCardView card);
    }

    public class DraggableCardView : UIView, IUIGestureRecognizerDelegate
    {
        //namespace level
        //Drag animation constants
        private static float defaultRotationMax = 1.0f;
        private static float defaultRotationAngle = (float) (Math.PI / 10.0);
        private static float defaultScaleMin = 0.8f;

        private CGSize screenSize = UIScreen.MainScreen.Bounds.Size;

        //Reset animation constants
        private float cardResetAnimationSpringBounciness = 10.0f;
        private float cardResetAnimationSpringSpeed = 20.0f;
        private string cardResetAnimationKey = "resetPositionAnimation";
        private float cardResetAnimationDuration = 0.2f;
        public static float cardSwipeActionAnimationDuration = DragSpeed.@default.GetDragSpeed();

        //end namespace level
        
        //Drag animation constants
        public float rotationMax { get; set; } = defaultRotationMax;
        public float RotationAngle { get; set; } = defaultRotationAngle;
        public float scaleMin { get; set; } = defaultScaleMin;


        private IDraggableCardDelegate _delegate;

        public IDraggableCardDelegate Delegate
        {
            get => _delegate;
            set
            {
                _delegate = value;
                configureSwipeSpeed();
            }
        }

        internal bool DragBegin = false;

        private OverlayView.OverlayView _overlayView;
        public UIView ContentView { get; private set; }

        private UIPanGestureRecognizer panGestureRecognizer;
        private UITapGestureRecognizer tapGestureRecognizer;
        private float animationDirectionY = 1.0f;
        private CGPoint dragDistance = CGPoint.Empty;
        private float swipePercentageMargin = 0.0f;

        //MARK: Lifecycle
        public DraggableCardView() : base(CGRect.Empty)
        {
            Setup();
        }

        public DraggableCardView(NSCoder coder) : base(coder)
        {
            Setup();
        }

        // protected DraggableCardView(NSObjectFlag t) : base(t)
        // {
        // }
        //
        // protected internal DraggableCardView(IntPtr handle) : base(handle)
        // {
        // }

        public DraggableCardView(CGRect frame) : base(frame)
        {
            Setup();
        }

        private CGRect _frame;

        public override CGRect Frame
        {
            get { return _frame; }
            set
            {
                _frame = value;
                var ratio = Delegate?.cardSwipeThresholdRatioMargin(this);
                swipePercentageMargin = ratio ?? 1.0f;
            }
        }

        // deinit {
        //     removeGestureRecognizer(panGestureRecognizer)
        //     removeGestureRecognizer(tapGestureRecognizer)
        // }

        private void Setup()
        {
            panGestureRecognizer = new UIPanGestureRecognizer(PanGestureRecognized);
            AddGestureRecognizer(panGestureRecognizer);
            panGestureRecognizer.Delegate = this;
            tapGestureRecognizer = new UITapGestureRecognizer(TapRecognized);
            tapGestureRecognizer.Delegate = this;
            tapGestureRecognizer.CancelsTouchesInView = false;
            AddGestureRecognizer(tapGestureRecognizer);
            if (_delegate != null)
            {
                cardSwipeActionAnimationDuration = _delegate.cardSwipeSpeedCard(this).GetDragSpeed();
            }
        }
        
        //MARK: Configurations
        public void configure(UIView view, OverlayView.OverlayView overlayView)
        {
            _overlayView?.RemoveFromSuperview();
            ContentView?.RemoveFromSuperview();
            if (overlayView != null)
            {
                var overlay = overlayView;

                _overlayView = overlay;
                overlay.Alpha = 0;
                AddSubview(overlay);
                configureOverlayView();
                var overlayIndex = Subviews.ToList().IndexOf(overlay); //
                InsertSubview(view, overlayIndex); //TODO: check it is correct instead of insertSubview(_:aboveSubview:)
            }
            else
            {
                AddSubview(view);
            }

            ContentView = view;
            configureContentView();
        }

        public void configureOverlayView()
        {
            if (_overlayView != null)
            {
                var overlay = _overlayView;
                overlay.TranslatesAutoresizingMaskIntoConstraints = false;

                var width = NSLayoutConstraint.Create(overlay, NSLayoutAttribute.Width, NSLayoutRelation.Equal, this,
                    NSLayoutAttribute.Width, 1.0f, 0f);
                var height = NSLayoutConstraint.Create(overlay, NSLayoutAttribute.Height, NSLayoutRelation.Equal, this,
                    NSLayoutAttribute.Height, 1.0f, 0f);
                var top = NSLayoutConstraint.Create(overlay, NSLayoutAttribute.Top, NSLayoutRelation.Equal, this,
                    NSLayoutAttribute.Top, 1.0f, 0f);
                var leading = NSLayoutConstraint.Create(overlay, NSLayoutAttribute.Leading, NSLayoutRelation.Equal,
                    this, NSLayoutAttribute.Leading, 1.0f, 0f);
                AddConstraints(new[] {width, height, top, leading});
            }
        }

        public void configureContentView()
        {
            if (ContentView != null)
            {
                var contentView = ContentView;
                contentView.TranslatesAutoresizingMaskIntoConstraints = false;
                var width = NSLayoutConstraint.Create(contentView, NSLayoutAttribute.Width, NSLayoutRelation.Equal,
                    this, NSLayoutAttribute.Width, 1.0f, 0f);
                var height = NSLayoutConstraint.Create(contentView, NSLayoutAttribute.Height, NSLayoutRelation.Equal,
                    this, NSLayoutAttribute.Height, 1.0f, 0f);
                var top = NSLayoutConstraint.Create(contentView, NSLayoutAttribute.Top, NSLayoutRelation.Equal, this,
                    NSLayoutAttribute.Top, 1.0f, 0f);
                var leading = NSLayoutConstraint.Create(contentView, NSLayoutAttribute.Leading, NSLayoutRelation.Equal,
                    this, NSLayoutAttribute.Leading, 1.0f, 0f);
                AddConstraints(new[] {width, height, top, leading});
            }
        }

        public void configureSwipeSpeed()
        {
            if (Delegate != null)
            {
                cardSwipeActionAnimationDuration = Delegate.cardSwipeSpeedCard(this).GetDragSpeed();
            }
        }
        
        //MARK: GestureRecognizers

        private void PanGestureRecognized(UIPanGestureRecognizer gestureRecognizer)
        {
            dragDistance = gestureRecognizer.TranslationInView(this);
            var touchLocation = gestureRecognizer.LocationInView(this);

            switch (gestureRecognizer.State)
            {
                case UIGestureRecognizerState.Began:
                    var firstTouchPoint = gestureRecognizer.LocationInView(this);
                    var newAnchorPoint = new CGPoint(x: firstTouchPoint.X / Bounds.Width,
                        y: firstTouchPoint.Y / Bounds.Height);
                    var oldPosition = new CGPoint(x: Bounds.Size.Width * Layer.AnchorPoint.X,
                        y: Bounds.Size.Height * Layer.AnchorPoint.Y);
                    var newPosition = new CGPoint(x: Bounds.Size.Width * newAnchorPoint.X,
                        y: Bounds.Size.Height * newAnchorPoint.Y);
                    Layer.AnchorPoint = newAnchorPoint;
                    Layer.Position = new CGPoint(x: Layer.Position.X - oldPosition.X + newPosition.X,
                        y: Layer.Position.Y - oldPosition.Y + newPosition.Y);
                    removeAnimations();

                    DragBegin = true;

                    animationDirectionY = touchLocation.Y >= Frame.Size.Height / 2 ? -1.0f : 1.0f;
                    Layer.RasterizationScale = UIScreen.MainScreen.Scale;
                    Layer.ShouldRasterize = true;
                    Delegate.cardPanBegan(this);
                    break;
                case UIGestureRecognizerState.Changed:
                    var rotationStrength = Math.Min(dragDistance.X / Frame.Width, rotationMax);
                    var rotationAngle = animationDirectionY * RotationAngle * rotationStrength;
                    var scaleStrength = 1 - ((1 - scaleMin) * Math.Abs(rotationStrength));
                    var scale = Math.Max(scaleStrength, scaleMin);

                    var transform = CATransform3D.Identity;
                    transform = transform.Scale((float) scale, (float) scale, 1);
                    transform = transform.Rotate((float) rotationAngle, 0, 0, 1);
                    transform = transform.Translate(dragDistance.X, dragDistance.Y, 0);

                    Layer.Transform = transform;

                    var percentage = dragPercentage();
                    var dragDirection = this.dragDirection();
                    updateOverlayWithFinishPercent(percentage, dragDirection);
                    if (dragDirection != null)
                    {
                        //100% - for proportion
                        _delegate?.cardWasDraggedWithFinishPercentage(this, Math.Min(Math.Abs(100 * percentage), 100),
                            dragDirection.Value);
                    }

                    break;
                case UIGestureRecognizerState.Ended:
                    swipeMadeAction();
                    _delegate?.cardPanFinished(this);
                    Layer.ShouldRasterize = false;
                    break;
                default:
                    Layer.ShouldRasterize = false;
                    resetViewPositionAndTransformations();
                    break;
            }
        }

        [Export("gestureRecognizer:shouldReceiveTouch:")]
        public bool ShouldReceiveTouch(UIGestureRecognizer recognizer, UITouch touch)
        {
            if (recognizer.Equals(tapGestureRecognizer) && touch.View is UIControl)
            {
                return false;
            }

            return true;
        }

        [Export("gestureRecognizerShouldBegin:")]
        public bool ShouldBegin(UIGestureRecognizer recognizer)
        {
            if (recognizer.Equals(panGestureRecognizer))
            {
                return _delegate?.cardShouldDrag(this) ?? true;
            }

            return true;
        }

        private void TapRecognized(UITapGestureRecognizer gestureRecognizer)
        {
            _delegate?.cardWasTapped(this);
        }

        //MARK: Private
        private List<ESwipeResultDirection> directions =>
            _delegate?.cardAllowedDirections(this) ?? new List<ESwipeResultDirection>()
                {ESwipeResultDirection.left, ESwipeResultDirection.right};

        private ESwipeResultDirection? dragDirection()
        {
            //find closest direction
            var normalizedDragPoint = dragDistance.normalizedDistanceForSize(Bounds.Size);
            (float distance, ESwipeResultDirection? direction) closest = (float.MaxValue, null);

            foreach (var direction in directions)
            {
                var distance = direction.Point().distanceTo(normalizedDragPoint);
                if (distance < closest.distance)
                {
                    closest = (distance, direction);
                }
            }

            return closest.direction;
        }

        private float dragPercentage()
        {
            if (this.dragDirection() == null)
            {
                return 0;
            }

            // normalize dragDistance then convert project closesest direction vector
            var dragDirection = this.dragDirection().Value;
            var normalizedDragPoint = dragDistance.normalizedDistanceForSize(Frame.Size);
            var swipePoint = normalizedDragPoint.scalarProjectionPointWith(dragDirection.Point());

            // rect to represent bounds of card in normalized coordinate system
            var rect = SwipeResultDirectionExtension.BoundsRect();

            // if point is outside rect, percentage of swipe in direction is over 100%
            if (!rect.Contains(swipePoint))
            {
                return 1.0f;
            }

            var centerDistance = swipePoint.distanceTo(CGPoint.Empty);
            var targetLine = new CGLine(swipePoint, CGPoint.Empty);

            // check 4 borders for intersection with line between touchpoint and center of card
            // return smallest percentage of distance to edge point or 0

            var perimeterLines = CGRectExtension.PerimeterLines();
            var intersectionPoints = new List<CGPoint>();
            foreach (var line in perimeterLines)
            {
                var intersectionPoint = CGPointExtension.intersectionBetweenLines(targetLine, line);
                if (intersectionPoint != null)
                {
                    intersectionPoints.Add(intersectionPoint.Value);
                }
            }
            var distances = intersectionPoints.Select(point => centerDistance / point.distanceTo(CGPoint.Empty)).ToList();
            return distances.Count() == 0 ? 0f : distances.Min();
        }

        private void updateOverlayWithFinishPercent(float percent, ESwipeResultDirection? direction)
        {
            _overlayView.OverlayState = direction;
            var progress = (float) Math.Max(Math.Min(percent / swipePercentageMargin, 1.0), 0);
            _overlayView.Update(progress);
        }

        private void swipeMadeAction()
        {
            var dragDirection = this.dragDirection();
            if (dragDirection != null && ShouldSwipe(dragDirection.Value) &&
                dragPercentage() >= swipePercentageMargin && directions.Contains(dragDirection.Value))
            {
                SwipeAction(dragDirection.Value);
            }
            else
            {
                resetViewPositionAndTransformations();
            }
        }

        private bool ShouldSwipe(ESwipeResultDirection direction)
        {
            return _delegate?.cardShouldSwipeIn(this, direction)?? true;
        }

        public CGPoint animationPointForDirection(ESwipeResultDirection direction)
        {
            // guard let superview = self.superview else {
            //     return .zero
            // }

            var superSize = Superview.Bounds.Size;
            var space = Math.Max(screenSize.Width, screenSize.Height);
            double x;
            double y;
            switch (direction)
            {
                case ESwipeResultDirection.left:
                case ESwipeResultDirection.right:
                    // Optimize left and right position
                    x = direction.Point().X * (superSize.Width + space);
                    y = 0.5 * superSize.Height;
                    return new CGPoint(x: x, y: y);

                default:
                    x = direction.Point().X * (superSize.Width + space);
                    y = direction.Point().Y * (superSize.Height + space);
                    return new CGPoint(x: x, y: y);
            }
        }

        public float animationRotationForDirection(ESwipeResultDirection direction)
        {
            return (float) (direction.Bearing() / 2.0f - Math.PI / 4);
        }

        private void SwipeAction(ESwipeResultDirection direction)
        {
            if (_overlayView != null)
            {
                _overlayView.OverlayState = direction;
                _overlayView.Alpha = 1.0f;
            }

            Delegate?.cardWasSwipedIn(this, direction);
            var translationAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.LayerTranslationXY);
            translationAnimation.Duration = cardSwipeActionAnimationDuration;

            translationAnimation.FromValue =
                NSValue.FromCGPoint(
                    new CGPoint()); //POPLayerGetTranslationXY(layer) - doesn't work!!!!!!!!!!!!!!!!!!!!!
            translationAnimation.ToValue = NSValue.FromCGPoint(animationPointForDirection(direction));
            translationAnimation.CompletionAction = (animation, b) => { RemoveFromSuperview(); };
            Layer.POPAddAnimation(translationAnimation, "swipeTranslationAnimation");
        }

        private void resetViewPositionAndTransformations()
        {
            _delegate?.cardWasReset(this);

            removeAnimations();

            var resetPositionAnimation = POPSpringAnimation.AnimationWithPropertyNamed(POPAnimation.LayerTranslationXY);
            resetPositionAnimation.FromValue =
                NSValue.FromCGPoint(
                    new CGPoint()); //POPLayerGetTranslationXY(layer) - doesn't work!!!!!!!!!!!!!!!!!!!!!
            resetPositionAnimation.ToValue = NSValue.FromCGPoint(CGPoint.Empty);
            resetPositionAnimation.SpringBounciness = cardResetAnimationSpringBounciness;
            resetPositionAnimation.SpringSpeed = cardResetAnimationSpringSpeed;
            resetPositionAnimation.CompletionAction = (animation, b) =>
            {
                Layer.Transform = CATransform3D.Identity;
                DragBegin = false;
            };

            Layer.POPAddAnimation(resetPositionAnimation, "resetPositionAnimation");

            var resetRotationAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.LayerRotation);
            resetRotationAnimation.FromValue = NSValue.FromCGPoint(
                new CGPoint()); //POPLayerGetRotationZ(layer) - doesn't work!!!!!!!!!!!!!!!!!!!!!
            resetRotationAnimation.ToValue = NSValue.FromCGPoint(CGPoint.Empty);
            resetRotationAnimation.Duration = cardResetAnimationDuration;

            Layer.POPAddAnimation(resetRotationAnimation, "resetRotationAnimation");

            var overlayAlphaAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.ViewAlpha);
            overlayAlphaAnimation.ToValue = new NSNumber(0.0f);
            overlayAlphaAnimation.Duration = cardResetAnimationDuration;
            overlayAlphaAnimation.CompletionAction = (animation, b) => { _overlayView.Alpha = 0; };
            _overlayView?.POPAddAnimation(overlayAlphaAnimation, "resetOverlayAnimation");

            var resetScaleAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.LayerScaleXY);
            resetScaleAnimation.ToValue = NSValue.FromCGPoint(new CGPoint(x: 1.0, y: 1.0));
            resetScaleAnimation.Duration = cardResetAnimationDuration;
            Layer.POPAddAnimation(resetScaleAnimation, "resetScaleAnimation");
        }

        //MARK: Public

        public void removeAnimations()
        {
            this.RemoveAllAnimations();
            Layer.RemoveAllAnimations();
        }

        public void Swipe(ESwipeResultDirection direction, Action completion = null)
        {
            if (!DragBegin)
            {
                _delegate?.cardWasSwipedIn(this, direction);
            }

            var swipePositionAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.LayerTranslationXY);
            swipePositionAnimation.FromValue =
                NSValue.FromCGPoint(
                    new CGPoint()); //POPLayerGetTranslationXY(layer)) - doesn't work!!!!!!!!!!!!!!!!!!!!!
            swipePositionAnimation.ToValue = NSValue.FromCGPoint(animationPointForDirection(direction));
            swipePositionAnimation.Duration = cardSwipeActionAnimationDuration;
            swipePositionAnimation.CompletionAction = (animation, b) =>
            {
                RemoveFromSuperview();
                completion?.Invoke();
            };

            Layer.POPAddAnimation(swipePositionAnimation, "swipePositionAnimation");

            var swipeRotationAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.LayerRotation);
            swipeRotationAnimation.FromValue = new NSNumber(0f);    //POPLayerGetRotationZ(layer) - doesn't work!!!!!!!!!!!!!!!!!!!!!
            swipeRotationAnimation.ToValue = new NSNumber(animationRotationForDirection(direction));
            swipeRotationAnimation.Duration = cardSwipeActionAnimationDuration;

            Layer.POPAddAnimation(swipeRotationAnimation, "swipeRotationAnimation");

            _overlayView.OverlayState = direction;
            var overlayAlphaAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.ViewAlpha);
            overlayAlphaAnimation.ToValue = new NSNumber(1.0);
            overlayAlphaAnimation.Duration = cardSwipeActionAnimationDuration;
            _overlayView.POPAddAnimation(overlayAlphaAnimation, "swipeOverlayAnimation");
        }
    }
}


