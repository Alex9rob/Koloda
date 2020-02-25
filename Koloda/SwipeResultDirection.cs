using System;
using System.Collections.Generic;
using CoreGraphics;

namespace Koloda
{
    public enum ESwipeResultDirection
    {
        left,
        right,
        up,
        down,
        topLeft,
        topRight,
        bottomLeft,
        bottomRight
    }

    public enum VerticalPosition
    {
        top = -1,
        middle = 0,
        bottom = 1
    }

    public enum HorizontalPosition
    {
        left = -1,
        middle = 0,
        right = 1
    }


    public static class SwipeResultDirectionExtension
    {
        public static Direction swipeDirection(this ESwipeResultDirection direction)
        {
            switch (direction)
            {
                case ESwipeResultDirection.up: return Direction.up;
                case ESwipeResultDirection.down: return Direction.down;
                case ESwipeResultDirection.left: return Direction.left;
                case ESwipeResultDirection.right: return Direction.right;
                case ESwipeResultDirection.topLeft: return Direction.topLeft;
                case ESwipeResultDirection.topRight: return Direction.topRight;
                case ESwipeResultDirection.bottomLeft: return Direction.bottomLeft;
                case ESwipeResultDirection.bottomRight: return Direction.bottomRight;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public static CGPoint Point(this ESwipeResultDirection direction)
        {
            return direction.swipeDirection().point;
        }

        public static double Bearing(this ESwipeResultDirection direction)
        {
            return direction.swipeDirection().bearing;
        }

        public static CGRect BoundsRect()
        {
            var w = (int) HorizontalPosition.right - (int) HorizontalPosition.left;
            var h = (int) VerticalPosition.bottom - (int) VerticalPosition.top;
            return new CGRect(x: (int) HorizontalPosition.left, y: (int) VerticalPosition.top, width: w, height: h);
        }
    }

    public struct Direction
    {
        public HorizontalPosition HorizontalPosition;
        public VerticalPosition VerticalPosition;

        public Direction(HorizontalPosition horizontalPosition, VerticalPosition verticalPosition)
        {
            HorizontalPosition = horizontalPosition;
            VerticalPosition = verticalPosition;
        }

        public CGPoint point => new CGPoint(x: (float) HorizontalPosition, y: (float) VerticalPosition);
        public double bearing => point.bearingTo(Direction.none.point);


        public static Direction none = new Direction(HorizontalPosition.middle, VerticalPosition.middle);
        public static Direction up = new Direction(HorizontalPosition.middle, VerticalPosition.top);
        public static Direction down = new Direction(HorizontalPosition.middle, VerticalPosition.bottom);
        public static Direction left = new Direction(HorizontalPosition.left, VerticalPosition.middle);
        public static Direction right = new Direction(HorizontalPosition.right, VerticalPosition.middle);

        public static Direction topLeft = new Direction(HorizontalPosition.left, VerticalPosition.top);
        public static Direction topRight = new Direction(HorizontalPosition.right, VerticalPosition.top);
        public static Direction bottomLeft = new Direction(HorizontalPosition.left, VerticalPosition.bottom);
        public static Direction bottomRight = new Direction(HorizontalPosition.right, VerticalPosition.bottom);
    }

    public static class CGPointExtension
    {
        public static float distanceTo(this CGPoint thisPoint, CGPoint point)
        {
            return (float) Math.Sqrt(Math.Pow(point.X - thisPoint.X, 2) + Math.Pow(point.Y - thisPoint.Y, 2));
        }

        public static double bearingTo(this CGPoint thisPoint, CGPoint point)
        {
            return Math.Atan2(point.Y - thisPoint.Y, point.X - thisPoint.X);
        }

        public static float scalarProjectionWith(this CGPoint thisPoint, CGPoint point)
        {
            return thisPoint.dotProductWith(point) / point.modulo();
        }

        public static CGPoint scalarProjectionPointWith(this CGPoint thisPoint, CGPoint point)
        {
            var r = thisPoint.scalarProjectionWith(point) / point.modulo();
            return new CGPoint(point.X * r, point.Y * r);
        }

        public static float dotProductWith(this CGPoint thisPoint, CGPoint point)
        {
            return (float) (thisPoint.X * point.X + thisPoint.Y * point.Y);
        }

        public static float modulo(this CGPoint thisPoint)
        {
            return (float) Math.Sqrt(thisPoint.X * thisPoint.X + thisPoint.Y * thisPoint.Y);
        }

        public static float distanceToRect(this CGPoint thisPoint, CGRect rect)
        {
            if (rect.Contains(thisPoint))
            {
                return thisPoint.distanceTo(new CGPoint(rect.GetMidX(), rect.GetMidY()));
            }

            var dx = Math.Max(Math.Max(rect.GetMinX() - thisPoint.X, thisPoint.X - rect.GetMaxX()), 0f);
            var dy = Math.Max(Math.Max(rect.GetMinY() - thisPoint.Y, thisPoint.Y - rect.GetMaxX()), 0f);

            if (dx * dy == 0)
            {
                return (float) Math.Max(dx, dy);
            }
            else
            {
                return (float) Math.Sqrt(dx * dx + dy * dy);
            }
        }

        public static CGPoint normalizedDistanceForSize(this CGPoint thisPoint, CGSize size)
        {
            // multiplies by 2 because coordinate system is (-1,1)
            var x = 2 * (thisPoint.X / size.Width);
            var y = 2 * (thisPoint.Y / size.Height);
            return new CGPoint(x: x, y: y);
        }

        public static CGPoint normalizedPointForSize(this CGPoint thisPoint, CGSize size)
        {
            var x = (thisPoint.X / (size.Width * 0.5)) - 1;
            var y = (thisPoint.Y / (size.Height * 0.5)) - 1;
            return new CGPoint(x: x, y: y);
        }

        public static CGPoint screenPointForSize(this CGPoint thisPoint, CGSize screenSize)
        {
            var x = 0.5 * (1 + thisPoint.X) * screenSize.Width;
            var y = 0.5 * (1 + thisPoint.Y) * screenSize.Height;
            return new CGPoint(x: x, y: y);
        }

        public static CGPoint? intersectionBetweenLines(CGLine line1, CGLine line2)
        {
            var (p1, p2) = (line1.Start, line1.End);
            var (p3, p4) = (line2.Start, line2.End);

            var d = (p4.Y - p3.Y) * (p2.X - p1.X) - (p4.X - p3.X) * (p2.Y - p1.Y);
            var ua = (p4.X - p3.X) * (p1.Y - p4.Y) - (p4.Y - p3.Y) * (p1.X - p3.X);
            var ub = (p2.X - p1.X) * (p1.Y - p3.Y) - (p2.Y - p1.Y) * (p1.X - p3.X); //????
            if (d < 0)
            {
                ua = -ua;
                ub = -ub; //????
                d = -d;
            }

            if (d != 0)
            {
                return new CGPoint(x: p1.X + ua / d * (p2.X - p1.X), y: p1.Y + ua / d * (p2.Y - p1.Y));
            }

            return null;
        }
    }

    public struct CGLine
    {
        public CGPoint Start { get; }
        public CGPoint End { get; }

        public CGLine(CGPoint start, CGPoint end)
        {
            Start = start;
            End = end;
        }
    }

    public static class CGRectExtension
    {
        public static CGLine TopLine()
        {
            return new CGLine(Direction.topLeft.point, Direction.topRight.point);
        }
        
        public static CGLine  LeftLine()
        {
            return new CGLine(Direction.topLeft.point, Direction.bottomLeft.point);
        }
        public static CGLine BottomLine ()
        {
            return new CGLine(Direction.bottomLeft.point, Direction.bottomRight.point);
        }
        public static CGLine  RightLine ()
        {
            return new CGLine(Direction.topRight.point, Direction.bottomRight.point);
        }
    
        public static List<CGLine>  PerimeterLines()
        {
            return new List<CGLine>{TopLine(), LeftLine(), BottomLine(), RightLine()};
        }
    }
}
