using System;
using CoreGraphics;

namespace Koloda
{
    public static class KolodaCardStorage   //extension
    {
        public static DraggableCardView.DraggableCardView CreateCard(this KolodaView kolodaView, int index, CGRect? frame = null)
        {
            var cardView = kolodaView.GenerateCard(frame ?? kolodaView.FrameForTopCard());
            kolodaView.ConfigureCard(cardView, index);

            return cardView;
        }

        public static DraggableCardView.DraggableCardView GenerateCard(this KolodaView kolodaView, CGRect frame)
        {
            var cardView = new DraggableCardView.DraggableCardView(frame);
            cardView.Delegate = kolodaView;

            return cardView;
        }

        public static void ConfigureCard(this KolodaView kolodaView, DraggableCardView.DraggableCardView card,
            int index)
        {
            var contentView = kolodaView.dataSource.kolodaViewForCardAt(kolodaView, index);
            card.configure(contentView, kolodaView.dataSource?.kolodaViewForCardOverlayAt(kolodaView, index));
            
            //Reconfigure drag animation constants from Koloda instance.
            card.rotationMax = kolodaView.rotationMax;
            card.RotationAngle = kolodaView.rotationAngle;
            card.scaleMin = kolodaView.scaleMin;
        }
    }
}
