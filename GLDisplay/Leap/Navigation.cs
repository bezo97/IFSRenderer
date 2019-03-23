using IFSEngine;
using Leap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLDisplay.Leap
{
    public static class Navigation
    {
        public static float MaxTrans { get; private set; } = 0.7f;
        public static float MaxRotate { get; private set; } = 3.1415926f / 3.0f;
        public static bool EnableRotation { get; set; } = false;


        private static bool grabbed;
        private static (float x, float y, float z) grabCamPos;
        private static Vector grabHandPos;
        private static (float dx, float dy, float dz) grabCamDir;
        private static Vector grabHandDir;
        private static float grabPhi;
        private static float grabTheta;
        private static LeapQuaternion grabQuaternion;

        private static FixedSizedQueue xQueue = new FixedSizedQueue();
        private static FixedSizedQueue yQueue = new FixedSizedQueue();
        private static FixedSizedQueue zQueue = new FixedSizedQueue();

        private static FixedSizedQueue phiQueue = new FixedSizedQueue();
        private static FixedSizedQueue thetaQueue = new FixedSizedQueue();

        public static void UpdateCamera(Hand right, Renderer r)
        {
            if (r.Camera is Camera camera)
            {
                if (!right.Fingers.Any(f => f.IsExtended) && right.Confidence >= 0.5)
                {
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

                    if (EnableRotation)
                    {
                        (float roll, float pitch, float yaw) = right.Rotation.Multiply(grabQuaternion).GetRotations();
                        if (Math.Abs(pitch) < MaxRotate && Math.Abs(roll) < MaxRotate)
                        {
                            camera.Phi = grabPhi - phiQueue.NextAvg(pitch);
                            camera.Theta = grabTheta - thetaQueue.NextAvg(roll);
                            /*camera.Phi += pitch / 50.0f;
                            camera.Theta += roll / 50.0f;*///masik lehetoseg
                        }
                    }

                    // Vector v = (right.PalmPosition - grabHandPos) / 100.0f;
                    // v.x = -v.x;
                    // v.y = -v.y;

                    float z = (right.PalmPosition.z - grabHandPos.z) / 100.0f;
                    float x = -(right.PalmPosition.x - grabHandPos.x) / 100.0f;
                    float y = -(right.PalmPosition.y - grabHandPos.y) / 100.0f;

                    if (Math.Abs(z) < MaxTrans && Math.Abs(x) < MaxTrans && Math.Abs(y) < MaxTrans)
                    {
                        camera.Pos = (grabCamPos.x, grabCamPos.y, grabCamPos.z);
                        camera.Translate(zQueue.NextAvg(z), xQueue.NextAvg(x), yQueue.NextAvg(y));
                    }

                    r.InvalidateAccumulation();
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
        }
    }
}
