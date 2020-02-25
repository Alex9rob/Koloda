using System;
using System.Collections.Generic;
using System.Linq;
using CoreAnimation;
using CoreGraphics;
using Facebook.Pop;
using Foundation;
using UIKit;

namespace Koloda
{
    public class KolodaViewAnimator
    {
        private KolodaView _koloda;

        public KolodaViewAnimator(KolodaView koloda)
        {
            _koloda = koloda;
        }

        public void AnimateAppearance(float duration, Action<bool> completion = null)
        {
            var kolodaAppearScaleAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.LayerScaleXY);

            kolodaAppearScaleAnimation.BeginTime =
                CAAnimation.CurrentMediaTime() + DraggableCardView.DraggableCardView.cardSwipeActionAnimationDuration;
            kolodaAppearScaleAnimation.Duration = duration;
            kolodaAppearScaleAnimation.FromValue = NSValue.FromCGPoint(new CGPoint(x: 0.1, y: 0.1));
            kolodaAppearScaleAnimation.ToValue = NSValue.FromCGPoint(new CGPoint(x: 1.0, y: 1.0));
            kolodaAppearScaleAnimation.CompletionAction = (animation, finished) => { completion?.Invoke(finished); };

            var kolodaAppearAlphaAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.ViewAlpha);
            kolodaAppearAlphaAnimation.BeginTime = CAAnimation.CurrentMediaTime() +
                                                   DraggableCardView.DraggableCardView.cardSwipeActionAnimationDuration;
            kolodaAppearAlphaAnimation.FromValue = new NSNumber(0.0);
            kolodaAppearAlphaAnimation.ToValue = new NSNumber(1.0);
            kolodaAppearAlphaAnimation.Duration = duration;

            _koloda?.POPAddAnimation(kolodaAppearAlphaAnimation,
                "kolodaAppearAlphaAnimation"); //vice versa keys in swift      
            _koloda?.Layer.POPAddAnimation(kolodaAppearScaleAnimation,
                "kolodaAppearScaleAnimation"); //vice versa keys in swift  
        }

        public void applyReverseAnimation(DraggableCardView.DraggableCardView card, ESwipeResultDirection? direction,
            float duration,
            Action<bool> completion = null)
        {
            var alphaAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.ViewAlpha);
            alphaAnimation.FromValue = new NSNumber(0.0);
            alphaAnimation.ToValue = new NSNumber(1.0);
            alphaAnimation.Duration = direction != null ? duration : 1.0;
            alphaAnimation.CompletionAction = (animation, finished) =>
            {
                completion?.Invoke(finished);
                card.Alpha = (nfloat) 1.0;
            };
            card.POPAddAnimation(alphaAnimation, "reverseCardAlphaAnimation");

            if (direction == null)
            {
                return;
            }

            var translationAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.LayerTranslationXY);
            translationAnimation.FromValue = NSValue.FromCGPoint(card.animationPointForDirection(direction.Value));
            translationAnimation.ToValue = NSValue.FromCGPoint(CGPoint.Empty);
            translationAnimation.Duration = duration;
            card.Layer.POPAddAnimation(translationAnimation, "reverseCardTranslationAnimation");

            var rotationAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.LayerRotation);
            rotationAnimation.FromValue = new NSNumber(card.animationRotationForDirection(direction.Value));
            rotationAnimation.ToValue = new NSNumber(0.0);
            rotationAnimation.Duration = duration;
            card.Layer.POPAddAnimation(rotationAnimation, "reverseCardRotationAnimation");
        }

        public void applyScaleAnimation(DraggableCardView.DraggableCardView card, CGSize scale, CGRect frame,
            float duration,
            Action<bool> completion = null)
        {
            var scaleAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.LayerScaleXY);
            scaleAnimation.Duration = duration;
            scaleAnimation.ToValue = NSValue.FromCGSize(scale);
            card.Layer.POPAddAnimation(scaleAnimation, "scaleAnimation");

            var frameAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.ViewFrame);
            frameAnimation.Duration = duration;
            frameAnimation.ToValue = NSValue.FromCGRect(frame);
            if (completion != null)
            {
                frameAnimation.CompletionAction = (animation, finished) => { completion.Invoke(finished); };
            }

            card.POPAddAnimation(frameAnimation, "frameAnimation");
        }

        public void applyAlphaAnimation(DraggableCardView.DraggableCardView card, float alpha, float duration = 0.2f,
            Action<bool> completion = null)
        {
            var alphaAnimation = POPBasicAnimation.AnimationWithPropertyNamed(POPAnimation.ViewAlpha);
            alphaAnimation.ToValue = new NSNumber(alpha);
            alphaAnimation.Duration = duration;
            alphaAnimation.CompletionAction = (animation, finished) => { completion?.Invoke(finished); };
            card.POPAddAnimation(alphaAnimation, "alpha");
        }

        public void applyInsertionAnimation(List<DraggableCardView.DraggableCardView> cards, Action completion = null)
        {
            var initialAlphas = cards.Select(card => card.Alpha).ToList();
            foreach (var card in cards)
            {
                card.Alpha = 0;
            }

            UIView.Animate(
                0.2,
                () =>
                {
                    for (var i = 0; i < cards.Count; i++)
                    {
                        cards[i].Alpha = initialAlphas[i];
                    }
                }, () => completion?.Invoke() //doesn't have bool in signature
            );
        }

        public void applyRemovalAnimation(List<DraggableCardView.DraggableCardView> cards, Action completion = null)
        {
            UIView.Animate(
                0.05,
                () =>
                {
                    foreach (var card in cards)
                    {
                        card.Alpha = 0;
                    }
                }, () => completion?.Invoke()
            );
        }

        public void resetBackgroundCardsWithCompletion(Action completion = null)
        {
            UIView.Animate(0.2, 0.0, UIViewAnimationOptions.CurveLinear,
                () => { _koloda?.moveOtherCardsWithPercentage(0); }, () => completion?.Invoke()
            );
        }
    }
}
