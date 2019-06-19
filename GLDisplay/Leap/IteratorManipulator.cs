//using IFSEngine;
//using IFSEngine.Model;
//using Leap;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace GLDisplay.Leap
//{
//    public static class IteratorManipulator
//    {
//        public static float MaxTrans { get; private set; } = 0.7f;
//        public static float MaxRotate { get; private set; } = 3.1415926f / 3.0f;
//        //private static LeapQuaternion rightQuaternion;
//        private static LeapQuaternion leftQuaternion;
//        //private static Vector rightPalmPosition;
//        private static Vector leftPalmPosition;

//        //private static bool rightGrabbed;
//        private static bool leftGrabbed;

//        private static FixedSizedVectorQueue leftPosQueue = new FixedSizedVectorQueue();
//        private static float grabpx, grabpy, grabpz;

//        private static Random rand = new Random();

//        private static int editState = 0;
//        public static int EditState {
//            get => editState;
//            private set => editState = (value + Params.Iterators.Count) % Params.Iterators.Count;
//        }

//        public static Renderer Renderer { get; set; }
//        public static IFS Params { get; set; }

//        /*private static List<Iterator> iterators = new List<Iterator>() {
//            new Iterator(
//                new Affine(0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
//                0,
//                1.0f,
//                0.1f,
//                0.0f,
//                1.0f)
//            ,
//            new Iterator(
//                new Affine(0.2f,-1.0f,0.0f , 0.8f,0.5f,0.0f , 0.1f,0.8f,0.0f , 0.0f,0.0f,1.0f),
//                1,
//                0.5f,
//                0.8f,
//                0.5f,
//                1.0f
//            ),
//            new Iterator(
//                new Affine(0.6f,0.133975f,0.0f , 0.56f,0.0f,0.0f , 0.0f,0.4f,0.0f , 0.0f,0.0f,1.0f),
//                2,
//                0.25f,
//                0.9f,
//                1.0f,
//                1.0f
//            )
//        };*/

//        private static float SummWeights;
//        public static int IteratorCount => Params?.Iterators.Count ?? 0;


//        /*private static Iterator finalit = new Iterator(
//            new Affine(0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
//            0,//linear
//            0.0f,
//            0.0f,
//            1.0f,
//            1.0f
//        );*/

//        private static Iterator Current {
//            get => Params.Iterators[EditState];
//            set => Params.Iterators[EditState] = value;
//        }

//        private static void ModifyCurrent(Func<Iterator, Iterator> func)
//        {
//            Current = func(Current);
//        }

//        private static FixedSizedVectorQueue leftPalmNormalQueue = new FixedSizedVectorQueue();

//        static IteratorManipulator()
//        {
//            Swipe.RightSwiped += (s, e) =>
//            {
//                EditState++;
//                if (EditState >= Params.Iterators.Count)
//                {
//                    EditState = 0;
//                }
//            };
//            Swipe.LeftSwiped += (s, e) =>
//            {
//                EditState--;
//                if (EditState <= 0)
//                {
//                    EditState = Params.Iterators.Count - 1;
//                }
//            };
//        }

//        public static void UpdateIterator(Hand left, Renderer r)
//        {
//            bool updateNeeded = false;
//            if (!left.Fingers.Any(f => f.IsExtended) && left.Confidence >= 0.5)
//            {
//                if (!leftGrabbed)
//                {
//                    leftGrabbed = true;
//                    leftPalmPosition = left.PalmPosition;
//                    grabpx = Current.aff.ox;
//                    grabpy = Current.aff.oy;
//                    grabpz = Current.aff.oz;
//                    leftQuaternion = left.Rotation.Inverse();
//                }

//                (float roll, float pitch, float yaw) = left.Rotation.Multiply(leftQuaternion).GetRotations();
//                if (Math.Abs(pitch) < MaxRotate && Math.Abs(roll) < MaxRotate && Math.Abs(yaw) < MaxRotate)
//                {
//                    ModifyCurrent(c => {
//                        //c.cs = pitch % 1.0f;
//                        c.ci = roll % 1.0f;
//                        return c;
//                    });
//                }

//                var v = (left.PalmPosition - leftPalmPosition) / 100.0f;
//                if (Math.Abs(v.z) < MaxTrans && Math.Abs(v.z) < MaxTrans && Math.Abs(v.z) < MaxTrans)
//                {
//                    ModifyCurrent(c => {
//                        //translate
//                        Vector tr = leftPosQueue.NextAvg(v);
//                        c.aff.ox = grabpx + tr.x; 
//                        c.aff.oy = grabpy + tr.y;
//                        c.aff.oz = grabpz + tr.z;
//                        return c;
//                    });
//                }

//                updateNeeded = true;
//            }
//            else if (leftGrabbed && left.Fingers.Any(f => f.IsExtended))
//            {
//                leftGrabbed = false;
//                // TODO
//            }
//            /*if (left.Fingers.All(f => f.IsExtended) && left.Confidence >= 0.9)
//            {
//                Vector felenk = new Vector(0, 0, -1);
//                if (Math.Abs(leftPalmNormalQueue.NextAvg(left.PalmNormal).Dot(felenk)) > 0.7)
//                {
//                    float handDistance = 1.0f - (left.PalmPosition.z + 360.0f) / 360.0f;

//                    // TODO
//                    updateNeeded = true;
//                }
//            }*/
//            if (updateNeeded)
//            {
//                r.UpdateParams(Params);
//            }
//        }
//    }
//}
