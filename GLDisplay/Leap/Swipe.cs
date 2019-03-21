using Leap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLDisplay.Leap
{
    public static class Swipe
    {
        static bool swipingRightStarted = false;
        static bool swipingLeftStarted = false;
        public static event EventHandler RightSwiped;
        public static event EventHandler LeftSwiped;

        public static void Detect(List<Hand> Hands)
        {
            if (Hands.Where(h => h.IsRight).FirstOrDefault() is Hand right)
            {
                if (!swipingRightStarted)
                    DetectSwipeStart(right, RightSwipe);

                if (swipingRightStarted)
                {
                    DetectSwipe(right, RightSwipe);
                    if (DetectSwipeStop(right, RightSwipe))
                    {
                        RightSwiped?.Invoke(null, null);
                    }
                }
            }
            else
                swipingRightStarted = false;

            if (Hands.Where(h => h.IsLeft).FirstOrDefault() is Hand left)
            {
                if (!swipingLeftStarted)
                    DetectSwipeStart(left, LeftSwipe);

                if (swipingLeftStarted)
                {
                    DetectSwipe(left, LeftSwipe);
                    if (DetectSwipeStop(left, LeftSwipe))
                    {
                        LeftSwiped?.Invoke(null, null);
                    }
                }
            }
            else
                swipingLeftStarted = false;
        }

        public static void Cancel()
        {
            swipingRightStarted = false;
            swipingLeftStarted = false;
        }

        private static void DetectSwipeStart(Hand hand, HandSwiped swipe)
        {
            if (hand.Fingers.All(f => f.IsExtended) && 
                hand.PalmNormal.Dot(swipe.Normal) > 0.5 && 
                hand.PalmVelocity.Magnitude > 500 &&
                hand.PalmVelocity.Normalized.Dot(swipe.Normal) > 0.5)
            {
                swipingLeftStarted = true;
                swipingRightStarted = false;
            }
        }

        private static void DetectSwipe(Hand hand, HandSwiped swipe)
        {
            if (hand.PalmVelocity.Normalized.Dot(swipe.Normal) > 0.5 && hand.PalmVelocity.Magnitude > 500)
            {
                swipingLeftStarted = true;
                swipingRightStarted = false;
            }
        }

        private static bool DetectSwipeStop(Hand hand, HandSwiped swipe)
        {
            if (hand.PalmVelocity.Normalized.Dot(swipe.Anti) > 0.0)//ez igazabol cancel..
            {
                swipingLeftStarted = false;
                return true;
            }
            return false;
        }

        private class HandSwiped
        {
            public readonly Vector Normal;
            public readonly Vector Anti;

            public HandSwiped(Vector n, Vector a)
            {
                Normal = n;
                Anti = a;
            }
        }

        private static readonly HandSwiped LeftSwipe = new HandSwiped(new Vector(1, 0, 0), new Vector(-1, 0, 0));
        private static readonly HandSwiped RightSwipe = new HandSwiped(new Vector(-1, 0, 0), new Vector(1, 0, 0));
    }
}
