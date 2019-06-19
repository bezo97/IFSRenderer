//using IFSEngine;
//using Leap;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace GLDisplay.Leap
//{
//    public static class FocusDistance
//    {
//        private static FixedSizedVectorQueue palmnormalQueue = new FixedSizedVectorQueue();

//        public static void UpdateFocusDistance(Hand right, Renderer r)
//        {
//            if (right.Fingers.All(f => f.IsExtended) && right.Confidence >= 0.7)
//            {
//                Vector felenk = new Vector(0, 0, -1);
//                if (Math.Abs(palmnormalQueue.NextAvg(right.PalmNormal).Dot(felenk)) > 0.7)
//                {
//                    //float handDistance = (right.PalmPosition.z - camera.FocusDistance) / 100.0f;

//                    float handDistance = 1.0f - (right.PalmPosition.z + 360.0f) / 360.0f;

//                    r.MutateCamera(c => {
//                        c.FocusDistance = 2.0f + handDistance * 8.0f;
//                        return c;
//                    });
//                }
//            }
//        }
//    }
//}
