//using Leap;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace GLDisplay.Leap
//{
//    public static class Swipe
//    {
//        public static event EventHandler RightSwiped;
//        public static event EventHandler LeftSwiped;
//        private static bool swipingRightStarted = false;
//        private static bool swipingLeftStarted = false;
//        private static Vector leftSwipeDir = new Vector(1, 0, 0);
//        private static Vector rightSwipeDir = new Vector(-1, 0, 0);


//        public static void Detect(List<Hand> Hands)
//        {
//            if (Hands.Where(h => h.IsRight).FirstOrDefault() is Hand right)
//            {
//                if (!swipingRightStarted)
//                    DetectSwipeStart(right, rightSwipeDir);

//                if (swipingRightStarted)
//                {
//                    DetectSwipe(right, rightSwipeDir);
//                    if (DetectSwipeStop(right, rightSwipeDir))
//                    {
//                        RightSwiped?.Invoke(null, null);
//                    }
//                }
//            }
//            else
//                swipingRightStarted = false;

//            if (Hands.Where(h => h.IsLeft).FirstOrDefault() is Hand left)
//            {
//                if (!swipingLeftStarted)
//                    DetectSwipeStart(left, leftSwipeDir);

//                if (swipingLeftStarted)
//                {
//                    DetectSwipe(left, leftSwipeDir);
//                    if (DetectSwipeStop(left, leftSwipeDir))
//                    {
//                        LeftSwiped?.Invoke(null, null);
//                    }
//                }
//            }
//            else
//                swipingLeftStarted = false;
//        }

//        public static void Cancel()
//        {
//            swipingRightStarted = false;
//            swipingLeftStarted = false;
//        }

//        private static void DetectSwipeStart(Hand hand, Vector swipeDir)
//        {
//            if (hand.Fingers.All(f => f.IsExtended) &&
//                hand.PalmNormal.Dot(swipeDir) > 0.5 &&
//                hand.PalmVelocity.Magnitude > 500 &&
//                hand.PalmVelocity.Normalized.Dot(swipeDir) > 0.5)
//            {
//                if (hand.IsLeft)
//                {
//                    swipingLeftStarted = true;
//                    swipingRightStarted = false;
//                }
//                else if (hand.IsRight)
//                {
//                    swipingLeftStarted = false;
//                    swipingRightStarted = true;
//                }
//            }
//        }

//        private static void DetectSwipe(Hand hand, Vector swipeDir)
//        {
//            if (hand.PalmVelocity.Normalized.Dot(swipeDir) > 0.5 && hand.PalmVelocity.Magnitude > 500)
//            {
//                if (hand.IsLeft)
//                {
//                    swipingLeftStarted = true;
//                    swipingRightStarted = false;
//                }
//                else if (hand.IsRight)
//                {
//                    swipingLeftStarted = false;
//                    swipingRightStarted = true;
//                }
//            }
//        }

//        private static bool DetectSwipeStop(Hand hand, Vector swipeDir)
//        {
//            if (hand.PalmVelocity.Normalized.Dot(-swipeDir) > 0.0)//ez igazabol cancel..
//            {
//                if (hand.IsLeft)
//                    swipingLeftStarted = false;
//                else if (hand.IsRight)
//                    swipingRightStarted = false;
//                return true;
//            }
//            return false;
//        }
//    }
//}
