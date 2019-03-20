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
                    DetectSwipeRightStart(right);

                if (swipingRightStarted)
                {
                    DetectSwipeRight(right);
                    if (DetectSwipeRightStop(right))
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
                    DetectSwipeLeftStart(left);

                if (swipingLeftStarted)
                {
                    DetectSwipeLeft(left);
                    if (DetectSwipeLeftStop(left))
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

        private static void DetectSwipeRightStart(Hand right)
        {
            if (right.Fingers.All(f => f.IsExtended) && right.PalmNormal.Dot(new Vector(-1, 0, 0)) > 0.5 && right.PalmVelocity.Magnitude > 500 && right.PalmVelocity.Normalized.Dot(new Vector(-1, 0, 0)) > 0.5)
            {
                swipingRightStarted = true;
                swipingLeftStarted = false;
            }
        }

        private static void DetectSwipeRight(Hand right)
        {
            if (right.PalmVelocity.Normalized.Dot(new Vector(-1, 0, 0)) > 0.5 && right.PalmVelocity.Magnitude > 500)
            {
                swipingRightStarted = true;
                swipingLeftStarted = false;
            }
        }

        private static bool DetectSwipeRightStop(Hand right)
        {
            if (right.PalmVelocity.Normalized.Dot(new Vector(1, 0, 0)) > 0.0)//ez igazabol cancel..
            {
                swipingRightStarted = false;
                return true;
            }
            return false;
        }

        private static void DetectSwipeLeftStart(Hand left)
        {
            if (left.Fingers.All(f => f.IsExtended) && left.PalmNormal.Dot(new Vector(1, 0, 0)) > 0.5 && left.PalmVelocity.Magnitude > 500 && left.PalmVelocity.Normalized.Dot(new Vector(1, 0, 0)) > 0.5)
            {
                swipingLeftStarted = true;
                swipingRightStarted = false;
            }
        }

        private static void DetectSwipeLeft(Hand left)
        {
            if (left.PalmVelocity.Normalized.Dot(new Vector(1, 0, 0)) > 0.5 && left.PalmVelocity.Magnitude > 500)
            {
                swipingLeftStarted = true;
                swipingRightStarted = false;
            }
        }

        private static bool DetectSwipeLeftStop(Hand left)
        {
            if (left.PalmVelocity.Normalized.Dot(new Vector(-1, 0, 0)) > 0.0)//ez igazabol cancel..
            {
                swipingLeftStarted = false;
                return true;
            }
            return false;
        }

    }
}
