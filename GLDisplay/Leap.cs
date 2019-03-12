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
        Vector grabDir = new Vector();
        bool grabbed = false;

        int hack = 0;
        private void Leap_FrameReady(object sender, FrameEventArgs e)
        {
            var right = e.frame.Hands.Where(h => h.IsRight).FirstOrDefault();
            hack--;
            if (right != null && r.Camera != null && hack <= 0 && !right.Fingers.Any(f => f.IsExtended) && right.Confidence >= 0.4)
            {
                hack = 2;

                if (!grabbed)
                {//ebben a frame ben grabbeljuk
                    grabbed = true;
                    grabCamPos = r.Camera.Pos;
                    grabHandPos = right.PalmPosition;
                    grabDir = right.Direction;
                }

                Vector subDir = (right.Direction - grabDir).Normalized;
                //r.Camera.Pos = (-right.PalmPosition.x / 100.0f, right.PalmPosition.z / 100.0f - 2.0f, -right.PalmPosition.y / 100.0f + 2.0f);
                //r.Camera.SetDirection(subDir.x, -subDir.z, subDir.y);
                r.Camera.Pos = (
                    grabCamPos.x - (right.PalmPosition.x - grabHandPos.x) / 100.0f, 
                    grabCamPos.y + (right.PalmPosition.z - grabHandPos.z) / 100.0f/* - 2.0f*/, 
                    grabCamPos.z - (right.PalmPosition.y - grabHandPos.y) / 100.0f/* + 2.0f*/
                );
                //r.Camera.SetDirection(subDir.x, -subDir.z, subDir.y);
                r.ResetAccumulation();
            }
            else if (grabbed && right != null && right.Fingers.Any(f => f.IsExtended))
            {
                grabbed = false;
            }
        }
    }
}
