using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Koloda.DraggableCardView;
using UIKit;

namespace Koloda
{
    public enum VisibleCardsDirection
    {
        Top = 0,
        Bottom = 1
    }

    public interface IKolodaViewDataSource
    {
        int kolodaNumberOfCards(KolodaView koloda);

        DraggableCardView.DragSpeed kolodaSpeedThatCardShouldDrag(KolodaView koloda)
        {
            return default(DraggableCardView.DragSpeed);
        }

        UIView kolodaViewForCardAt(KolodaView koloda, int index)
        {
            return null;
        }

        OverlayView.OverlayView kolodaViewForCardOverlayAt(KolodaView koloda, int index);
    }
    
    public interface IKolodaViewDelegate
    {
        List<ESwipeResultDirection> kolodaallowedDirectionsForIndex(KolodaView koloda, int index)
        {
            return new List<ESwipeResultDirection>{ESwipeResultDirection.left, ESwipeResultDirection.right};
        }

        bool kolodaShouldSwipeCardAt(KolodaView koloda, int index, ESwipeResultDirection direction)
        {
            return true;
        }

        void kolodaDidSwipeCardAt(KolodaView koloda, int index, ESwipeResultDirection direction) { }

        virtual void kolodaDidRunOutOfCards(KolodaView koloda){}
        void kolodaDidSelectCardAt(KolodaView koloda, int index){}

        bool kolodaShouldApplyAppearAnimation(KolodaView koloda)
        {
            return true;
        }
        bool kolodaShouldMoveBackgroundCard(KolodaView koloda)
        {
            return true;
        }
        bool kolodaShouldTransparentizeNextCard(KolodaView koloda)
        {
            return true;
        }

        void kolodaDraggedCardWithPercentage(KolodaView koloda, float finishPercentage,
            ESwipeResultDirection direction){}

        void kolodaDidResetCard(KolodaView koloda){}

        float? kolodaSwipeThresholdRatioMargin(KolodaView koloda)
        {
            return null;
        }
        void kolodaDidShowCardAt(KolodaView koloda, int index){}

        bool kolodaShouldDragCardAt(KolodaView koloda, int index)
        {
            return true;
        }
        void kolodaPanBegan(KolodaView koloda, DraggableCardView.DraggableCardView card){}
        void kolodaPanFinished(KolodaView koloda, DraggableCardView.DraggableCardView card){}
    }

    [Register("KolodaView")]
    public class KolodaView : UIView, IDraggableCardDelegate
    {
        //namespace level

        // Default values
        private int defaultCountOfVisibleCards = 3;
        private float defaultBackgroundCardsTopMargin = 4.0f;
        private float defaultBackgroundCardsScalePercent = 0.95f;
        private float defaultBackgroundCardsLeftMargin = 8.0f;
        private float defaultBackgroundCardFrameAnimationDuration = 0.2f;
        private float defaultAppearanceAnimationDuration = 0.8f;
        private float defaultReverseAnimationDuration = 0.3f;

        // Opacity values
        private float defaultAlphaValueOpaque = 1.0f;
        private float defaultAlphaValueTransparent = 0.0f;
        private float defaultAlphaValueSemiTransparent = 0.7f;

        //end namespace level


        public KolodaView(IntPtr handle) : base(handle)
        {
            _animator = new Lazy<KolodaViewAnimator>(new KolodaViewAnimator(this));
        }

        // MARK: Public

        // Opacity values
        public float alphaValueOpaque => defaultAlphaValueOpaque;
        public float alphaValueTransparent => defaultAlphaValueTransparent;
        public float alphaValueSemiTransparent => defaultAlphaValueSemiTransparent;
        public bool shouldPassthroughTapsWhenNoVisibleCards = false;

        // Drag animation constants
        public float rotationMax;
        public float rotationAngle;
        public float scaleMin;

        // Animation durations
        public float appearanceAnimationDuration => defaultAppearanceAnimationDuration;
        public float backgroundCardFrameAnimationDuration => defaultBackgroundCardFrameAnimationDuration;
        public float reverseAnimationDuration => defaultReverseAnimationDuration;

        public int countOfVisibleCards => defaultCountOfVisibleCards;
        public float backgroundCardsTopMargin => defaultBackgroundCardsTopMargin;
        public float backgroundCardsScalePercent => defaultBackgroundCardsScalePercent;

        // Visible cards direction (defaults to bottom)
        public VisibleCardsDirection visibleCardsDirection = VisibleCardsDirection.Bottom;

        public bool isLoop = false;

        public int currentCardIndex { get; private set; } = 0;
        public int countOfCards { get; private set; } = 0;

        private IKolodaViewDataSource _dataSource;

        public IKolodaViewDataSource dataSource
        {
            get => _dataSource;
            set
            {
                _dataSource = value;
                setupDeck();
            }
        }

        public IKolodaViewDelegate @delegate;

        public KolodaViewAnimator animator
        {
            set { _animator = new Lazy<KolodaViewAnimator>(value); }
            get { return _animator.Value; }
        }

        public bool IsAnimating => animationSemaphore.IsAnimating;

        public bool isRunOutOfCards => visibleCards.Count == 0;


        // MARK: Private

        internal bool shouldTransparentizeNextCard => @delegate?.kolodaShouldTransparentizeNextCard(this) ?? true;

        internal KolodaAnimationSemaphore animationSemaphore = new KolodaAnimationSemaphore();

        private Lazy<KolodaViewAnimator> _animator;

        private List<DraggableCardView.DraggableCardView>
            visibleCards = new List<DraggableCardView.DraggableCardView>();

        private bool cardIsDragging
        {
            get
            {
                if (visibleCards == null || visibleCards?.Count == 0)
                {
                    return false;
                }

                var frontCard = visibleCards.First();
                return frontCard.DragBegin;
            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            if (!animationSemaphore.IsAnimating && !cardIsDragging)
            {
                LayoutDeck();
            }
        }

        // MARK: Configurations

        private void setupDeck()
        {
            //?
            var countOfCards = dataSource.kolodaNumberOfCards(this);
            if (countOfCards - currentCardIndex > 0)
            {
                var countOfNeededCards = Math.Min(countOfVisibleCards, countOfCards - currentCardIndex);

                for (int index = 0; index < countOfNeededCards; index++)
                {
                    var actualIndex = index + currentCardIndex;
                    var nextCardView = this.CreateCard(actualIndex);
                    var isTop = index == 0;
                    nextCardView.UserInteractionEnabled = isTop;
                    nextCardView.Alpha = alphaValueOpaque;
                    if (shouldTransparentizeNextCard && !isTop)
                    {
                        nextCardView.Alpha = alphaValueSemiTransparent;
                    }

                    visibleCards.Add(nextCardView);
                    if (isTop)
                    {
                        AddSubview(nextCardView);
                    }
                    else
                    {
                        InsertSubviewBelow(nextCardView, visibleCards[index - 1]);
                    }
                }

                @delegate?.kolodaallowedDirectionsForIndex(this, currentCardIndex);
            }
        }

        public void LayoutDeck()
        {
            for (var index = 0; index < visibleCards.Count; index++)
            {
                LayoutCard(visibleCards[index], index);
            }
        }

        private void LayoutCard(DraggableCardView.DraggableCardView card, int index)
        {
            if (index == 0)
            {
                card.Layer.Transform = CATransform3D.Identity;
                card.Frame = FrameForTopCard();
            }
            else
            {
                var cardParameters = BackgroundCardParametersForFrame(FrameForCard(index));
                var scale = cardParameters.Scale;
                card.Layer.Transform = CATransform3D.MakeScale(scale.Width, scale.Height, 1);
                card.Frame = cardParameters.Frame;
            }
        }

        // MARK: Frames

        public CGRect FrameForCard(int index)
        {
            var bottomOffset = 0f;
            var topOffset = backgroundCardsTopMargin * (countOfVisibleCards - 1);
            var scalePercent = backgroundCardsScalePercent;
            var width = Frame.Width * Math.Pow(scalePercent, index);
            var xOffset = (Frame.Width - width) / 2;
            var height = (Frame.Height - bottomOffset - topOffset) * Math.Pow(scalePercent, index);

            if (visibleCardsDirection == VisibleCardsDirection.Bottom)
            {
                var multiplier = index > 0 ? 1f : 0f;
                var prevCardFrame = index > 0 ? FrameForCard(Math.Max(index - 1, 0)) : CGRect.Empty;
                var yOffset = (prevCardFrame.Height - height + prevCardFrame.Y + backgroundCardsTopMargin) * multiplier;
                var frame = new CGRect(xOffset, yOffset, width, height);
                Console.WriteLine(">>>>>" + index + frame.ToString());
                return frame;
            }
            else
            {
                var multiplier = index < (countOfVisibleCards - 1) ? 1f : 0f;
                var nextCardFrame = index < (countOfVisibleCards - 1)
                    ? FrameForCard(Math.Min(index + 1, (countOfVisibleCards - 1)))
                    : CGRect.Empty;
                var yOffset = (nextCardFrame.Y + backgroundCardsTopMargin) * multiplier;
                var frame = new CGRect(xOffset, yOffset, width, height);

                Console.WriteLine(">>>>>" + index + frame.ToString());
                return frame;
            }
        }

        internal CGRect FrameForTopCard()
        {
            return FrameForCard(0);
        }

        internal (CGRect Frame, CGSize Scale) BackgroundCardParametersForFrame(CGRect initialFrame)
        {
            var finalFrame = FrameForTopCard();
            finalFrame.X = initialFrame.X;
            finalFrame.Y = initialFrame.Y;

            var scale = CGSize.Empty;
            scale.Width = initialFrame.Width / finalFrame.Width;
            scale.Height = initialFrame.Height / finalFrame.Height;

            //check ios version is not lower than 11
            return (initialFrame, scale);
        }

        internal void moveOtherCardsWithPercentage(float percentage)
        {
            if (visibleCards.Count <= 1)
            {
                return;
            }

            for (var index = 1; index < visibleCards.Count; index++)
            {
                var previousCardFrame = FrameForCard(index - 1);
                var frame = FrameForCard(index);
                var fraction = percentage / 100;

                var distanceToMoveY = (frame.Y - previousCardFrame.Y) * fraction;

                frame.Y -= distanceToMoveY;

                var distanceToMoveX = (previousCardFrame.X - frame.X) * fraction;

                frame.X += distanceToMoveX;

                var widthDelta = (previousCardFrame.Width - frame.Width) * fraction;
                var heightDelta = (previousCardFrame.Height - frame.Height) * fraction;

                frame.Width += widthDelta;
                frame.Height += heightDelta;

                var cardParameters = BackgroundCardParametersForFrame(frame);
                var scale = cardParameters.Scale;

                var card = visibleCards[index];

                card.Layer.Transform = CATransform3D.MakeScale(scale.Width, scale.Height, 1.0f);
                card.Frame = cardParameters.Frame;
            
                //For fully visible next card, when moving top card
                if (shouldTransparentizeNextCard) 
                {
                    if (index == 1)
                    {
                        card.Alpha = alphaValueSemiTransparent +
                                     (alphaValueOpaque - alphaValueSemiTransparent) * fraction;
                    }
                }
                
            }
        }

        // MARK: Animations

        private void ApplyAppearAnimation()
        {
            Alpha = 0;
            UserInteractionEnabled = false;
            animationSemaphore.Increment();
            animator.AnimateAppearance(appearanceAnimationDuration, b =>
            {    
                UserInteractionEnabled = true;                                          
                animationSemaphore.Decrement();
                LayoutDeck();
            });
        }

        public void ApplyAppearAnimationIfNeeded()
        {
            if (!animationSemaphore.IsAnimating)
            {
                var shouldApply = @delegate?.kolodaShouldApplyAppearAnimation(this) ?? false;
                if (shouldApply)
                {
                    ApplyAppearAnimation();
                }
            }
        }

        // MARK: DraggableCardDelegate


        void IDraggableCardDelegate.cardWasDraggedWithFinishPercentage(DraggableCardView.DraggableCardView card, float percentage,
            ESwipeResultDirection direction)
        {
            var shouldMove = @delegate?.kolodaShouldMoveBackgroundCard(this) ?? false;
            if (shouldMove)
            {
                moveOtherCardsWithPercentage(percentage);
            }
        }

        bool IDraggableCardDelegate.cardShouldSwipeIn(DraggableCardView.DraggableCardView card, ESwipeResultDirection direction)
        {
            return @delegate?.kolodaShouldSwipeCardAt(this, currentCardIndex, direction) ?? true;
        }

        List<ESwipeResultDirection> IDraggableCardDelegate.cardAllowedDirections(DraggableCardView.DraggableCardView card)
        {
            var index = currentCardIndex + visibleCards.IndexOf(card);
            return @delegate?.kolodaallowedDirectionsForIndex(this, index) ?? new List<ESwipeResultDirection>
                       {ESwipeResultDirection.left, ESwipeResultDirection.right};
        }

        void IDraggableCardDelegate.cardWasSwipedIn(DraggableCardView.DraggableCardView card, ESwipeResultDirection direction)
        {
            swipedAction(direction);
        }

        void IDraggableCardDelegate.cardWasReset(DraggableCardView.DraggableCardView draggableCard)
        {
            if (visibleCards.Count > 1)
            {
                animationSemaphore.Increment();
                animator.resetBackgroundCardsWithCompletion(() =>
                { 
                    animationSemaphore.Decrement();

                    for (var index = 1; index < visibleCards.Count; index++)
                    {
                        var card = visibleCards[index];
                        if (shouldTransparentizeNextCard)
                        {
                            card.Alpha = index == 0 ? alphaValueOpaque : alphaValueSemiTransparent;
                        }
                    }
                });
            }
            else
            {
                animationSemaphore.Decrement();
            }

            @delegate.kolodaDidResetCard(this);
        }

        void IDraggableCardDelegate.cardWasTapped(DraggableCardView.DraggableCardView card)
        {
            var visibleIndex = visibleCards.IndexOf(card);
            if (visibleIndex < 0)
            {
                return;
            }

            var index = currentCardIndex + visibleIndex;
            @delegate?.kolodaDidSelectCardAt(this, index);
        }

        float? IDraggableCardDelegate.cardSwipeThresholdRatioMargin(DraggableCardView.DraggableCardView card)
        {
            return @delegate?.kolodaSwipeThresholdRatioMargin(this);
        }

        bool IDraggableCardDelegate.cardShouldDrag(DraggableCardView.DraggableCardView card)
        {
            var visibleIndex = visibleCards.IndexOf(card);
            if (visibleIndex < 0) return true;
            var index = currentCardIndex + visibleIndex;
            return @delegate?.kolodaShouldDragCardAt(this, index) ?? true;
        }

        DragSpeed IDraggableCardDelegate.cardSwipeSpeedCard(DraggableCardView.DraggableCardView card)
        {
            return dataSource?.kolodaSpeedThatCardShouldDrag(this) ?? default(DragSpeed);
        }

        void IDraggableCardDelegate.cardPanBegan(DraggableCardView.DraggableCardView card)
        {
            @delegate?.kolodaPanBegan(this, card);
        }

        void IDraggableCardDelegate.cardPanFinished(DraggableCardView.DraggableCardView card)
        {
            @delegate?.kolodaPanFinished(this, card);
        }

        // MARK: Private
        private void Clear()
        {
            currentCardIndex = 0;
            foreach (var card in visibleCards)
            {
                card.RemoveFromSuperview();
            }

            visibleCards.Clear();
        }

        // MARK: Actions
        private void swipedAction(ESwipeResultDirection direction)
        {
            animationSemaphore.Increment();
            if (visibleCards.Count != 0)                            //added to avoid crash
            {
                visibleCards.RemoveAt(0);
            }

            var swipedCardIndex = currentCardIndex;
            currentCardIndex += 1;

            if (isLoop && currentCardIndex >= countOfCards && countOfCards > 0)
            {
                currentCardIndex = currentCardIndex % countOfCards;
            }

            var indexToBeShow = currentCardIndex + Math.Min(countOfVisibleCards, countOfCards) - 1;
            var realCountOfCards = dataSource?.kolodaNumberOfCards(this) ?? countOfCards;
            if (indexToBeShow < realCountOfCards ||
                (isLoop && realCountOfCards > 0 && realCountOfCards > visibleCards.Count()))
            {
                LoadNextCard();
            }

            if (visibleCards.Count != 0)
            {
                AnimateCardsAfterLoadingWithCompletion(() =>
                {
                    var _this = this;

                    var lastCard = _this.visibleCards.Last();
                    if (lastCard != null)
                    {
                        lastCard.Hidden = false;
                    }

                    _this.animationSemaphore.Decrement();
                    _this.@delegate?.kolodaDidSwipeCardAt(_this, swipedCardIndex, direction);
                    _this.@delegate?.kolodaDidShowCardAt(_this, _this.currentCardIndex);
                });
            }
            else
            {
                animationSemaphore.Decrement();
                @delegate?.kolodaDidSwipeCardAt(this, swipedCardIndex, direction);
                @delegate?.kolodaDidRunOutOfCards(this);
            }
        }

        private void LoadNextCard()
        {
            if (dataSource == null)
            {
                return;
            }

            var cardParameters = BackgroundCardParametersForFrame(FrameForCard(visibleCards.Count));
            var realCountOfCards = dataSource?.kolodaNumberOfCards(this) ?? countOfCards;
            var indexToBeMake = currentCardIndex + Math.Min(countOfVisibleCards, realCountOfCards) - 1;
            if (isLoop && indexToBeMake >= realCountOfCards)
            {
                indexToBeMake = indexToBeMake % realCountOfCards;
            }

            var lastCard = this.CreateCard(indexToBeMake, cardParameters.Frame);


            var scale = cardParameters.Scale;
            lastCard.Layer.Transform = CATransform3D.MakeScale(scale.Width, scale.Height, 1);
            lastCard.Hidden = true;
            lastCard.UserInteractionEnabled = true;

            if (visibleCards.Count != 0)
            {
                var card = visibleCards[visibleCards.Count - 1];
                InsertSubviewBelow(lastCard, card);
            }
            else
            {
                AddSubview(lastCard);
            }

            visibleCards.Add(lastCard);
        }

        private void AnimateCardsAfterLoadingWithCompletion(Action completion = null)
        {
            for (var index = 0; index < visibleCards.Count; index++)
            {
                var currentCard = visibleCards[index];

                currentCard.removeAnimations();

                currentCard.UserInteractionEnabled = index == 0;
                var cardParameters = BackgroundCardParametersForFrame(FrameForCard(index));
                Action<bool> animationCompletion = null;

                if (index != 0)
                {
                    if (shouldTransparentizeNextCard)
                    {
                        currentCard.Alpha = alphaValueSemiTransparent;
                    }
                }
                else
                {
                    animationCompletion = finished => completion?.Invoke();


                    if (shouldTransparentizeNextCard)
                    {
                        animator.applyAlphaAnimation(currentCard, alphaValueOpaque);
                    }
                    else
                    {
                        currentCard.Alpha = alphaValueOpaque;
                    }
                }

                animator.applyScaleAnimation(
                    currentCard,
                    cardParameters.Scale,
                    cardParameters.Frame,
                    backgroundCardFrameAnimationDuration,
                    animationCompletion
                );
            }
        }

        public void RevertAction(ESwipeResultDirection? direction = null)
        {
            if (currentCardIndex <= 0 || animationSemaphore.IsAnimating)
            {
                return;
            }

            if (countOfCards - currentCardIndex >= countOfVisibleCards)
            {
                if (visibleCards.Count != 0)
                {
                    var lastCard = visibleCards[visibleCards.Count - 1];
                    lastCard.RemoveFromSuperview();
                    visibleCards.RemoveAt(visibleCards.Count - 1);
                }
            }

            currentCardIndex -= 1;

            if (dataSource != null)
            {
                var firstCardView = this.CreateCard(currentCardIndex, FrameForTopCard());

                if (shouldTransparentizeNextCard)
                {
                    firstCardView.Alpha = alphaValueTransparent;
                }

                firstCardView.Delegate = this;

                AddSubview(firstCardView);
                visibleCards.Insert(0, firstCardView);

                animationSemaphore.Increment();

                animator.applyReverseAnimation(firstCardView, direction: direction, duration: reverseAnimationDuration,
                    b =>
                    {
                        animationSemaphore.Decrement();
                        @delegate.kolodaDidShowCardAt(this, currentCardIndex);
                    });
            }

            visibleCards.RemoveAt(0);
            for (var index = 0; index < visibleCards.Count; index++)
            {
                var card = visibleCards[index];
                if (shouldTransparentizeNextCard)
                {
                    card.Alpha = alphaValueSemiTransparent;
                }

                card.UserInteractionEnabled = false;
                var cardParameters = BackgroundCardParametersForFrame(FrameForCard(index + 1));
                animator.applyScaleAnimation
                (
                    card,
                    cardParameters.Scale,
                    cardParameters.Frame,
                    backgroundCardFrameAnimationDuration,
                    null
                );
            }
        }

        private void LoadMissingCards(int missingCardsCount)
        {
            if (missingCardsCount == 0)
            {
                return;
            }

            var cardsToAdd = Math.Min(missingCardsCount, countOfCards - currentCardIndex);
            var startIndex = visibleCards.Count;
            var endIndex = startIndex + cardsToAdd - 1;

            for (int index = startIndex; index <= endIndex; index++)
            {
                var nextCardView = this.GenerateCard(FrameForTopCard());
                LayoutCard(nextCardView, currentCardIndex + index);
                nextCardView.Alpha = shouldTransparentizeNextCard ? alphaValueSemiTransparent : alphaValueOpaque;

                visibleCards.Add(nextCardView);
                this.ConfigureCard(nextCardView, currentCardIndex + index);
                if (index > 0)
                {
                    InsertSubviewBelow(nextCardView, visibleCards[index - 1]);
                }
                else
                {
                    InsertSubview(nextCardView, 0);
                }
            }
        }

        public void ReconfigureCards()
        {
            if (dataSource != null) 
            {
                for (int index = 0; index < visibleCards.Count; index++)
                {
                    var card = visibleCards[index];
                    var actualIndex = currentCardIndex + index;
                    if (isLoop && actualIndex >= countOfCards)
                    {
                        actualIndex -= countOfCards;
                    }
                    this.ConfigureCard(card, actualIndex);
                }
            }
        }

        private int missingCardCount()
        {
            return Math.Min(countOfVisibleCards - visibleCards.Count,
                countOfCards - (currentCardIndex + visibleCards.Count));
        }

        // MARK: Public
        public void ReloadData()
        {
            var numberOfCards = dataSource.kolodaNumberOfCards(this); 
            if (numberOfCards == 0)
            {
                Clear();
                return;
            }

            if (currentCardIndex == 0)
            {
                Clear();
            }

            countOfCards = numberOfCards;
            if (countOfCards - (currentCardIndex + visibleCards.Count) > 0)
            {
                if (visibleCards.Count != 0)
                {
                    var missingCards = missingCardCount();
                    LoadMissingCards(missingCards);
                }
                else
                {
                    setupDeck();
                    LayoutDeck();
                    ApplyAppearAnimationIfNeeded();
                }
            }
            else
            {
                ReconfigureCards();
            }
        }

        public void Swipe(ESwipeResultDirection direction, bool force = false)
        {
            var shouldSwipe = @delegate.kolodaShouldSwipeCardAt(this, currentCardIndex, direction); //?? true;
            if (!(force || shouldSwipe))
            {
                return;
            }

            var validDirection =
                @delegate.kolodaallowedDirectionsForIndex(this, currentCardIndex).Contains(direction); //?? true;
            if (!validDirection)
            {
                return;
            }

            if (!animationSemaphore.IsAnimating)
            {
                if (visibleCards.Count > 0)
                {
                    var frontCard = visibleCards[0];
                    if (!frontCard.DragBegin)
                    {
                        if (visibleCards.Count > 1)
                        {
                            var nextCard = visibleCards[1];
                            nextCard.Alpha = shouldTransparentizeNextCard
                                ? alphaValueSemiTransparent
                                : alphaValueOpaque;
                        }

                        animationSemaphore.Increment();

                        frontCard.Swipe(direction, () => { animationSemaphore.Decrement(); });
                        frontCard.Delegate = null;
                    }
                }
            }
        }
        
        public void resetCurrentCardIndex()
        {
            Clear();
            ReloadData();
        }

        public UIView viewForCard(int index)
        {
            if (visibleCards.Count + currentCardIndex > index && index >= currentCardIndex)
            {
                return visibleCards[index - currentCardIndex].ContentView;
            }

            return null;
        }

        public override bool PointInside(CGPoint point, UIEvent uievent)
        {
            if (!shouldPassthroughTapsWhenNoVisibleCards)
            {
                return base.PointInside(point, uievent);
            }

            if (PointInside(point, uievent))
            {
                return visibleCards.Count > 0;
            }

            return false;
        }

        // MARK: Cards managing - Insertion
        private List<DraggableCardView.DraggableCardView> insertVisibleCardsWithIndexes(List<int> visibleIndexes)
        {
            var insertedCards = new List<DraggableCardView.DraggableCardView>();
            foreach (var insertionIndex in visibleIndexes)
            {
                var card = this.CreateCard(insertionIndex);
                var visibleCardIndex = insertionIndex - currentCardIndex;
                visibleCards.Insert(visibleCardIndex, card);
                if (visibleCardIndex == 0)
                {
                    card.UserInteractionEnabled = true;
                    card.Alpha = alphaValueOpaque;
                    InsertSubview(card, visibleCards.Count - 1);
                }
                else
                {
                    card.UserInteractionEnabled = false;
                    card.Alpha = shouldTransparentizeNextCard ? alphaValueSemiTransparent : alphaValueOpaque;
                    InsertSubviewBelow(card, visibleCards[visibleCardIndex - 1]);
                }

                LayoutCard(card, visibleCardIndex);
                insertedCards.Add(card);
            }

            return insertedCards;
        }

        private void removeCards(List<DraggableCardView.DraggableCardView> cards)
        {
            foreach (var card in cards)
            {
                card.Delegate = null;
                card.RemoveFromSuperview();
            }
        }

        private void removeCards(List<DraggableCardView.DraggableCardView> cards, bool animated)
        {
            visibleCards.RemoveRange(visibleCards.Count - cards.Count, cards.Count);

            if (animated)
            {
                animator.applyRemovalAnimation(
                    cards,
                    () =>
                    {
                        removeCards(cards);
                    });
            }
            else
            {
                removeCards(cards);
            }
        }

        public void insertCardAtIndexRange(List<int> indexRange, bool animated = true)
        {
            if(dataSource == null)
            {
                return;
            }

            var currentItemsCount = countOfCards;
            countOfCards = dataSource.kolodaNumberOfCards(this);

            var visibleIndexes = indexRange
                .Where(x => x >= currentCardIndex && x < currentCardIndex + countOfVisibleCards).ToList();
            visibleIndexes.Sort();
            var insertedCards = insertVisibleCardsWithIndexes(visibleIndexes);


            visibleCards.RemoveRange(0, countOfVisibleCards);
            var cardsToRemove = visibleCards;
            removeCards(cardsToRemove, animated: animated);
            animator.resetBackgroundCardsWithCompletion();
            if (animated)
            {
                animationSemaphore.Increment();
                animator.applyInsertionAnimation(insertedCards, () => { animationSemaphore.Decrement(); });
            }

            if (currentItemsCount + indexRange.Count != countOfCards)
            {
                Debug.WriteLine("Cards count after update is not equal to data source count");
            }
        }

        // MARK: Cards managing - Deletion

        private void proceedDeletionInRange(List<int> range)
        {
            var deletionIndexes = range;
            var descendingDeletionIndexes = deletionIndexes.OrderByDescending(i => i);
            foreach (var deletionIndex in descendingDeletionIndexes)
            {
                var visibleCardIndex = deletionIndex - currentCardIndex;
                var card = visibleCards[visibleCardIndex];
                card.Delegate = null;
                card.Swipe(ESwipeResultDirection.right);
                visibleCards.RemoveAt(visibleCardIndex);
            }
        }

        public void removeCardInIndexRange(List<int> indexRange, bool animated)
        {
            animationSemaphore.Increment();
            var currentItemsCount = countOfCards;
            countOfCards = dataSource.kolodaNumberOfCards(this);
            var visibleIndexes = indexRange
                .Where(x => x >= currentCardIndex && x < currentCardIndex + countOfVisibleCards).ToList();

            if (visibleIndexes.Count != 0)
            {
                proceedDeletionInRange(visibleIndexes);
            }

            currentCardIndex -= indexRange.Count(x => x < currentCardIndex);
            LoadMissingCards(missingCardCount());
            LayoutDeck();
            for (var index = 0; index < visibleCards.Count; index++)
            {
                visibleCards[index].Alpha = shouldTransparentizeNextCard && index != 0
                    ? alphaValueSemiTransparent
                    : alphaValueOpaque;
                visibleCards[index].UserInteractionEnabled = index == 0;
            }

            animationSemaphore.Decrement();

            if (currentItemsCount - indexRange.Count != countOfCards)
            {
                Debug.WriteLine("Cards count after update is not equal to data source count");
            }
        }

        // MARK: Cards managing - Reloading

        public void reloadCardsInIndexRange(List<int> indexRange)
        {
            if (dataSource == null)
            {
                return;
            }

            var visibleIndexes = indexRange
                .Where(x => x >= currentCardIndex && x < currentCardIndex + countOfVisibleCards).ToList();
            foreach (var index in visibleIndexes)
            {
                var visibleCardIndex = index - currentCardIndex;

                if (visibleCards.Count > visibleCardIndex)
                {
                    var card = visibleCards[visibleCardIndex];
                    this.ConfigureCard(card, index);
                }
            }
        }
    }
}

