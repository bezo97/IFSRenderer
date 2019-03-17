using IFSEngine;
using Leap;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GLDisplay
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

        (float x,float y,float z) grabCamPos;
        Vector grabHandPos = new Vector();
        (float dx, float dy, float dz) grabCamDir;
        Vector grabHandDir = new Vector();
        bool grabbed = false;
        LeapQuaternion grabQuaternion;
        float grabPhi;
        float grabTheta;
        float savedFocusDistance;
        bool leftGrabbed = false;

       
        const float maxTrans = 0.7f;
        const float maxRotate = 3.141592f / 5;

 
        FixedSizedQueue xQueue = new FixedSizedQueue();
        FixedSizedQueue yQueue = new FixedSizedQueue();
        FixedSizedQueue zQueue = new FixedSizedQueue();

        FixedSizedQueue phiQueue = new FixedSizedQueue();
        FixedSizedQueue thetaQueue = new FixedSizedQueue();

        FixedSizedVectorQueue palmnormalQueue = new FixedSizedVectorQueue();


        void RightHand(Hand right, Camera camera) 
        {
            if (!right.Fingers.Any(f => f.IsExtended) && right.Confidence >= 0.7) {
                if (!grabbed)
                {   // ebben a frame ben grabbeljuk
                    grabbed = true;
                    grabCamPos = camera.Pos;
                    grabHandPos = right.PalmPosition;
                    grabCamDir = camera.GetDirection();
                    grabHandDir = right.Direction;
                    grabPhi = camera.Phi;
                    grabTheta = camera.Theta;
                    grabQuaternion = right.Rotation.Inverse();
                }
                
                (float roll, float pitch, float yaw)  = right.Rotation.Multiply(grabQuaternion).GetRotations();

                if (Math.Abs(pitch) < maxRotate && Math.Abs(roll) < maxRotate)
                {
                    camera.Phi = grabPhi - phiQueue.NextAvg(pitch);
                    camera.Theta = grabTheta - thetaQueue.NextAvg(roll);
                }

                float z = (right.PalmPosition.z - grabHandPos.z) / 100.0f;
                float x = -(right.PalmPosition.x - grabHandPos.x) / 100.0f;
                float y = -(right.PalmPosition.y - grabHandPos.y) / 100.0f;

                if (Math.Abs(z) < maxTrans && Math.Abs(x) < maxTrans && Math.Abs(y) < maxTrans)
                {
                    camera.Pos = (grabCamPos.x, grabCamPos.y, grabCamPos.z);
                    camera.Translate(zQueue.NextAvg(z), xQueue.NextAvg(x), yQueue.NextAvg(y));
                }

                r.ResetAccumulation();
            }
            else if (grabbed && right.Fingers.Any(f => f.IsExtended))
            {
                grabbed = false;
                xQueue.Clear();
                yQueue.Clear();
                zQueue.Clear();
                phiQueue.Clear();
                thetaQueue.Clear();
            }
        }

        void LeftHand(Hand left, Camera camera) 
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
                    float handDistance = /*quuee*/(left.PalmPosition.z - savedFocusDistance) / 100.0f;

                    //handDistance = 1 - (handDistance + 360) / 360;

                    camera.FocusDistance = savedFocusDistance + handDistance * 5;
                    r.ResetAccumulation();
                }

                /*Vector balra = new Vector(-1, 0, 0);
                if (right.Direction.Dot(balra) > 0.4)
                {
                    //megvan a gesture
                }*/

            }
            else if (leftGrabbed && left.Fingers.Any(f => f.IsExtended))
            {
                leftGrabbed = false;
                palmnormalQueue.Clear();
            }
        }

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
                hack = 2;
            }
      
            if (r.Camera is Camera camera) {
                if (e.frame.Hands.Where(h => h.IsRight).FirstOrDefault() is Hand right)
                {
                    RightHand(right, camera);

                    if (!swipingRightStarted)
                        DetectSwipeRightStart(right);

                    if (swipingRightStarted)
                    {
                        DetectSwipeRight(right);
                        if(DetectSwipeRightStop(right))
                        {
                            //lapoz..
                        }
                    }

                }
                else
                    swipingRightStarted = false;
                if (e.frame.Hands.Where(h => h.IsLeft).FirstOrDefault() is Hand left)
                {
                    LeftHand(left, camera);
                }
            }         
        }

        bool swipingRightStarted;
        void DetectSwipeRightStart(Hand right)
        {
            if(right.Fingers.All(f=>f.IsExtended) && right.PalmNormal.Dot(new Vector(-1,0,0)) > 0.5 && right.PalmVelocity.Magnitude > 500 && right.PalmVelocity.Normalized.Dot(new Vector(-1, 0, 0)) > 0.5)
            {
                Debug.WriteLine("SWIPE RIGHT START");
                swipingRightStarted = true;
            }
        }

        void DetectSwipeRight(Hand right)
        {
            if (right.PalmVelocity.Normalized.Dot(new Vector(-1, 0, 0)) > 0.5 && right.PalmVelocity.Magnitude>500)
            {
                Debug.WriteLine("SWIPING RIGHT");
                swipingRightStarted = true;
            }
        }

        bool DetectSwipeRightStop(Hand right)
        {
            if (right.PalmVelocity.Normalized.Dot(new Vector(1, 0, 0)) > 0.0)//ez igazabol cancel..
            {
                Debug.WriteLine("SWIPE RIGHT STOP");
                swipingRightStarted = false;
                return true;
            }
            return false;
        }

    }
}
