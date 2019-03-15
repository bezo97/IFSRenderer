using IFSEngine;
using Leap;
using System;
using System.Collections.Generic;
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

        (float roll, float pitch, float yaw) FromQuaternion(LeapQuaternion q) {
            float roll, pitch, yaw;

            float sinr_cosp = +2.0f * (q.w * q.x + q.y * q.z);
            float cosr_cosp = +1.0f - 2.0f * (q.x * q.x + q.y * q.y);
            roll = (float)Math.Atan2(sinr_cosp, cosr_cosp);


            float CopySign(float cx, float cy)
            {
                if ((cx < 0 && cy > 0) || (cx > 0 && cy < 0))
                    return -cx;
                return cx;
            }

            // pitch (y-axis rotation)
            float sinp = +2.0f * (q.w * q.y - q.z * q.x);
            if (Math.Abs(sinp) >= 1)
                pitch = CopySign(3.141592f / 2, sinp); // use 90 degrees if out of range
            else
                pitch = (float)Math.Asin(sinp);

            // yaw (z-axis rotation)
            float siny_cosp = +2.0f * (q.w * q.z + q.x * q.y);
            float cosy_cosp = +1.0f - 2.0f * (q.y * q.y + q.z * q.z);
            yaw = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return (roll, pitch, yaw);
        }


        const float maxTrans = 0.7f;
        const float maxRotate = 3.141592f / 5;


        LeapQuaternion Inverse(LeapQuaternion q)
        {
            var c = Conjugate(q);
            var sum = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            return new LeapQuaternion(c.x / sum, c.y / sum, c.z / sum, c.w / sum);
        }

        LeapQuaternion Conjugate(LeapQuaternion q) {
            return new LeapQuaternion(q.x, -q.y, -q.z, -q.w);
        }

        public class FixedSizedQueue
        {
            Queue<float> q = new Queue<float>();

            public FixedSizedQueue() { Clear(); }

            public int Limit { get; set; } = 20;
            public void Enqueue(float obj)
            {
                q.Enqueue(obj); 
                while (q.Count > Limit) { q.Dequeue(); } ;
            }

            public float Average() {
                float mult = 1f;
                float dem = 0.95f;
                float div = 0;
                return q.Select(f => { mult *= dem; div += mult; return f * mult; }).Sum() / div;
            }

            public float NextAvg(float f) {
                Enqueue(f);
                return Average();
            }

            public void Clear() {
                q.Clear();
                for (int i = 0; i < Limit; ++i) {
                    q.Enqueue(0);
                }
            }
        }

        public class FixedSizedVectorQueue
        {
            Queue<Vector> q = new Queue<Vector>();

            public FixedSizedVectorQueue() { Clear(); }

            public int Limit { get; set; } = 20;
            public void Enqueue(Vector obj)
            {
                q.Enqueue(obj);
                while (q.Count > Limit) { q.Dequeue(); };
            }

            public Vector Average()
            {
                float mult = 1f;
                float dem = 0.95f;
                var sum = new Vector(0, 0, 0);
                float div = 0;
                foreach (var vec in q) {
                    mult *= dem;
                    sum.x += vec.x * mult;
                    sum.y += vec.y * mult;
                    sum.z += vec.z * mult;
                    div += mult;
                }
                return sum / div;
            }

            public Vector NextAvg(Vector f)
            {
                Enqueue(f);
                return Average();
            }

            public void Clear()
            {
                q.Clear();
                for (int i = 0; i < Limit; ++i)
                {
                    q.Enqueue(new Vector(0,0,0));
                }
            }
        }


        FixedSizedQueue xQueue = new FixedSizedQueue();
        FixedSizedQueue yQueue = new FixedSizedQueue();
        FixedSizedQueue zQueue = new FixedSizedQueue();

        FixedSizedQueue phiQueue = new FixedSizedQueue();
        FixedSizedQueue thetaQueue = new FixedSizedQueue();

        FixedSizedVectorQueue palmnormalQueue = new FixedSizedVectorQueue();

        int hack = 0;
        private void Leap_FrameReady(object sender, FrameEventArgs e)
        {
            var right = e.frame.Hands.Where(h => h.IsRight).FirstOrDefault();
            var left = e.frame.Hands.Where(h => h.IsLeft).FirstOrDefault();
            hack--;
            if (right != null && r.Camera != null && hack <= 0 && !right.Fingers.Any(f => f.IsExtended) && right.Confidence >= 0.7)
            {
                hack = 2;

                if (!grabbed)
                {//ebben a frame ben grabbeljuk
                    grabbed = true;
                    grabCamPos = r.Camera.Pos;
                    grabHandPos = right.PalmPosition;
                    grabCamDir = r.Camera.GetDirection();
                    grabHandDir = right.Direction;
                    grabPhi = r.Camera.Phi;
                    grabTheta = r.Camera.Theta;
                    grabQuaternion = Inverse(right.Rotation);
                }



                //V1: r.Camera.SetDirection(subDir.x, -subDir.z, subDir.y);
                /*r.Camera.SetDirection(
                    grabCamDir.dx + (right.Direction.x - grabHandDir.x) * 2.0f,
                    grabCamDir.dy + (right.Direction.y - grabHandDir.y) * 2.0f,
                    grabCamDir.dz + (right.Direction.z - grabHandDir.z) * 2.0f
                );*/
                //V2:
                (float roll, float pitch, float yaw) = FromQuaternion(right.Rotation.Multiply(grabQuaternion));

                if (Math.Abs(pitch) < maxRotate && Math.Abs(roll) < maxRotate)
                {
                    r.Camera.Phi = grabPhi - phiQueue.NextAvg(pitch);
                    r.Camera.Theta = grabTheta - thetaQueue.NextAvg(roll);
                }

          
                //TODO: roll


                //V1: r.Camera.Pos = (-right.PalmPosition.x / 100.0f, right.PalmPosition.z / 100.0f - 2.0f, -right.PalmPosition.y / 100.0f + 2.0f);
                /*V2: r.Camera.Pos = (
                    grabCamPos.x - (right.PalmPosition.x - grabHandPos.x) / 100.0f, 
                    grabCamPos.y + (right.PalmPosition.z - grabHandPos.z) / 100.0f,//-2 
                    grabCamPos.z - (right.PalmPosition.y - grabHandPos.y) / 100.0f//+2
                );*/

                float z = (right.PalmPosition.z - grabHandPos.z) / 100.0f;
                float x = -(right.PalmPosition.x - grabHandPos.x) / 100.0f;
                float y = -(right.PalmPosition.y - grabHandPos.y) / 100.0f;
              
                if (Math.Abs(z) < maxTrans && Math.Abs(x) < maxTrans && Math.Abs(y) < maxTrans)
                {
                    r.Camera.Pos = (grabCamPos.x, grabCamPos.y, grabCamPos.z);
                    r.Camera.Translate(zQueue.NextAvg(z), xQueue.NextAvg(x), yQueue.NextAvg(y));
                }

                r.ResetAccumulation();
            }
            else if (grabbed && right != null && right.Fingers.Any(f => f.IsExtended))
            {
                grabbed = false;
                xQueue.Clear();
                yQueue.Clear();
                zQueue.Clear();
                phiQueue.Clear();
                thetaQueue.Clear();
            }

            if (left != null && !left.Fingers.Any(f => f.IsExtended) && left.Confidence >= 0.7)
            {
                Vector felenk = new Vector(0, -1, 0);
                // Debug.WriteLine(left.PalmNormal.ToString());
                if(palmnormalQueue.NextAvg(left.PalmNormal).Dot(felenk) > 0.5)
                {
                    if (!leftGrabbed){
                        savedFocusDistance = r.Camera.FocusDistance;
                    }

                    leftGrabbed = true;
                    //megvan a gesture
                    float handDistance = /*quuee*/(left.PalmPosition.z - savedFocusDistance) / 100.0f;

                    //handDistance = 1 - (handDistance + 360) / 360;

                    r.Camera.FocusDistance = savedFocusDistance + handDistance * 5;
                    r.ResetAccumulation();
                }

                /*Vector balra = new Vector(-1, 0, 0);
                if (right.Direction.Dot(balra) > 0.4)
                {
                    //megvan a gesture
                }*/

            }else if (leftGrabbed && left != null && left.Fingers.Any(f => f.IsExtended)) {
                leftGrabbed = false ;
                palmnormalQueue.Clear();
            }
        }
    }
}
