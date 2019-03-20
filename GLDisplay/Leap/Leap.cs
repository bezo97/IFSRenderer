using IFSEngine;
using Leap;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GLDisplay.Leap
{

    public class Leap
    {
        private Controller leap = new Controller();
        private Renderer r;

        public Leap(SynchronizationContext sync, Renderer r)
        {
            leap.EventContext = sync;
            leap.FrameReady += Leap_FrameReady;
            leap.ImageReady += Leap_ImageReady;
            leap.ImageRequestFailed += Leap_ImageRequestFailed;
            
            this.r = r;
        }

        private void Leap_ImageRequestFailed(object sender, ImageRequestFailedEventArgs e)
        {
            //throw new NotImplementedException();
            Debug.WriteLine("Leap_ImageRequestFailed");
        }

        private void Leap_ImageReady(object sender, ImageEventArgs e)
        {
            //throw new NotImplementedException();
        }

        /*void LeftHand(Hand left, Camera camera) 
        {
            if (!left.Fingers.Any(f => f.IsExtended) && left.Confidence >= 0.7)
            {
                Vector felenk = new Vector(0, -1, 0);
                // Debug.WriteLine(left.PalmNormal.ToString());
                if (palmnormalQueue.NextAvg(left.PalmNormal).Dot(felenk) > 0.5)
                {
                    if (!leftGrabbed)
                    {
                        savedFocusDistance = camera.FocusDistance;
                    }

                    leftGrabbed = true;
                    //megvan a gesture
                    float handDistance = (left.PalmPosition.z - savedFocusDistance) / 100.0f;

                    //handDistance = 1 - (handDistance + 360) / 360;

                    camera.FocusDistance = savedFocusDistance + handDistance * 5;
                    r.InvalidateAccumulation();
                }

            }
            else if (leftGrabbed && left.Fingers.Any(f => f.IsExtended))
            {
                leftGrabbed = false;
                palmnormalQueue.Clear();
            }
        }*/

        int hack = 0;

        private void Leap_FrameReady(object sender, FrameEventArgs e)
        {            
            if (hack > 0) 
            {
                hack--;
                return;
            }
            else
            {
                hack = 3;
            }

            Swipe.Detect(e.frame.Hands);

            if (e.frame.Hands.Where(h => h.IsRight).FirstOrDefault() is Hand right)
            {
                Navigation.UpdateCamera(right, r);
                FocusDistance.UpdateFocusDistance(right, r);
            }

            //if (e.frame.Hands.Where(h => h.IsLeft).FirstOrDefault() is Hand left)
            //TODO: edit iterators
        }
        

    }
}
